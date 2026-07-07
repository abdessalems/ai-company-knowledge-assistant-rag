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

// Register Infrastructure layer services (Database, Repositories, Authentication)
builder.Services.AddInfrastructure(connectionString);

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
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();

// ====================================
// BUILD AND CONFIGURE PIPELINE
// ====================================

var app = builder.Build();

// Configure HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ====================================
// MIDDLEWARE ORDER IS IMPORTANT
// ====================================
// 1. Authentication: Validates JWT tokens
// 2. Authorization: Checks [Authorize] attributes
// Order: Authentication BEFORE Authorization!

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

