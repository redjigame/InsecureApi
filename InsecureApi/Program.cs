using Microsoft.Data.Sqlite;
using System.Text;
using System.Security.Cryptography;
using InsecureApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader()
            .WithExposedHeaders("*");
    });
});

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCors();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

DatabaseHelper.InitializeDatabase();

Console.WriteLine("🚨 ATTENTION: Cette API est VOLONTAIREMENT INSÉCURISÉE pour les tests SAST 🚨");
Console.WriteLine("NE JAMAIS utiliser ce code en production !");

app.Run();
