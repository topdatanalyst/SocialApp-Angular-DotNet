using System.Reflection;
using backend.api.Models;   
using backend.api.Services; 

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Configure MongoDB settings
builder.Services.Configure<MongoDBSettings>(builder.Configuration.GetSection("MongoDB"));
// Register the UserService         
builder.Services.AddSingleton<UserService>();

builder.Services.AddControllers();  

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

