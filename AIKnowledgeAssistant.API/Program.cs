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

// Ollama (local AI) settings, from the "Ollama" config section (with defaults).
var ollamaSettings = new AIKnowledgeAssistant.Infrastructure.AI.OllamaSettings();
builder.Configuration.GetSection("Ollama").Bind(ollamaSettings);
builder.Services.AddSingleton(ollamaSettings);

// Register Application layer services (Validators, services)
builder.Services.AddApplication();

// Register Infrastructure layer services (Database, Repositories, Authentication, Storage)
builder.Services.AddInfrastructure(connectionString, fileStorageBasePath);

// ====================================
// JWT AUTHENTICATION
// ====================================

// Settings come from the "Jwt" configuration section (appsettings.json, or
// environment variables / user-secrets in production) — never hardcoded.
var jwtSettings = new AIKnowledgeAssistant.Infrastructure.Authentication.JwtSettings();
builder.Configuration.GetSection("Jwt").Bind(jwtSettings);
jwtSettings.Validate();
builder.Services.AddSingleton(jwtSettings);

// UTF8 to match the token generator (JwtTokenGenerator) so both sides derive
// the exact same signing-key bytes.
var key = Encoding.UTF8.GetBytes(jwtSettings.SecretKey);

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
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
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

// CORS: allow the Angular dev app (http://localhost:4200) to call this API.
// Browsers block cross-origin calls unless the server opts in like this.
const string FrontendCorsPolicy = "AllowFrontend";
builder.Services.AddCors(options =>
{
    options.AddPolicy(FrontendCorsPolicy, policy =>
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials());
});

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
else
{
    // Force HTTPS only outside local dev (avoids self-signed cert / CORS
    // friction when the Angular app calls the API over plain http locally).
    app.UseHttpsRedirection();
}

// CORS must run before authentication so preflight (OPTIONS) requests succeed.
app.UseCors(FrontendCorsPolicy);

// ====================================
// AUTHENTICATION & AUTHORIZATION MIDDLEWARE
// ====================================
// Order: Authentication validates JWT
//        Authorization checks [Authorize] attributes

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

