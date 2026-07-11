using AIKnowledgeAssistant.Application.Extensions;
using AIKnowledgeAssistant.API.Middleware;
using AIKnowledgeAssistant.Infrastructure.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ====================================
// REGISTER SERVICES - Clean Architecture Layers
// ====================================

// Get connection string from appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Where uploaded files are stored on disk. Read from configuration, and fall
// back to an "uploads" folder next to the app if not configured.
var fileStorageBasePath = builder.Configuration["FileStorage:BasePath"];
if (string.IsNullOrWhiteSpace(fileStorageBasePath))
    fileStorageBasePath = Path.Combine(builder.Environment.ContentRootPath, "uploads");

// Register Application layer services (Validators, services)
builder.Services.AddApplication();

// Register Infrastructure layer services (Database, Repositories, Authentication, Storage)
builder.Services.AddInfrastructure(connectionString, fileStorageBasePath);

// ====================================
// JWT AUTHENTICATION
// ====================================

// JWT Configuration
// This should come from IConfiguration, but for now hardcoded
var secretKey = "your-super-secret-key-must-be-at-least-32-characters-long";
var issuer = "https://ai-knowledge-assistant.company.com";
var audience = "ai-knowledge-assistant-api";

var key = Encoding.ASCII.GetBytes(secretKey);

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        // Allow reading token from Authorization header
        // Default: Bearer scheme
        options.SaveToken = true;
    });

// Add API services
builder.Services.AddControllers();
builder.Services.AddSwaggerGen();

// ====================================
// BUILD AND CONFIGURE PIPELINE
// ====================================

var app = builder.Build();

// ====================================
// ERROR HANDLING MIDDLEWARE (FIRST!)
// ====================================
// Must be first to catch all exceptions
// From all other middleware and endpoints

app.UseErrorHandling();

// Configure HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ====================================
// AUTHENTICATION & AUTHORIZATION MIDDLEWARE
// ====================================
// Order: Authentication validates JWT
//        Authorization checks [Authorize] attributes

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

