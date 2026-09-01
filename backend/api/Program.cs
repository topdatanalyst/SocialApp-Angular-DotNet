using System.Reflection;
using backend.api.Models;   
using backend.api.Services; 
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models; 


var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Configure MongoDB settings
builder.Services.Configure<MongoDBSettings>(builder.Configuration.GetSection("MongoDB"));
// Register the UserService         
builder.Services.AddSingleton<UserService>();
// Register Controllers 
builder.Services.AddControllers();  

// Configure JWT authentication
var jwtsecret=builder.Configuration.GetSection("JwtSecret")["SecretKey"] ??
         throw new InvalidOperationException("JWT secret key is not configured."); 
// Configure JWT authentication 
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme; 
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = true;
    options.SaveToken = true;   
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidIssuer = "https://localhost:7206",
        ValidAudience = "https://localhost:7206",
        IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(jwtsecret)),
        ClockSkew = TimeSpan.Zero,
    };
});

// Log loaded assemblies and handle ReflectionTypeLoadException
foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
{
    try
    {
        assembly.GetTypes();
    }
    catch (ReflectionTypeLoadException ex)
    {
        Console.WriteLine($"--- Assembly problematico: {assembly.FullName} ---");
        foreach (var le in ex.LoaderExceptions)
            Console.WriteLine(" -> " + le?.Message);
    }
}


builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Cors policy to allow requests from the Angular frontend
builder.Services.AddCors();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

