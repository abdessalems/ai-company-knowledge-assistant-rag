using AIKnowledgeAssistant.Infrastructure.Extensions;

var builder = WebApplication.CreateBuilder(args);

// ====================================
// REGISTER SERVICES - Clean Architecture Layers
// ====================================

// Get connection string from appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

// Register Infrastructure layer services (Database, Repositories)
builder.Services.AddInfrastructure(connectionString);

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
app.UseAuthorization();
app.MapControllers();

app.Run();

