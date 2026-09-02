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
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "SocialAPI Rest", Version = "v1" });
    // Add JWT authentication to Swagger
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter 'Bearer' [space] and then your valid token in the text input below.\r\n\r\nExample: \"Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9\"",
    };
    // Add the security definition to Swagger   
    options.AddSecurityDefinition("Bearer", securityScheme);
    var securityRequirement = new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            }
        }] = Array.Empty<string>()
    };
    // Add the security requirement to Swagger  
    options.AddSecurityRequirement(securityRequirement);
});

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

