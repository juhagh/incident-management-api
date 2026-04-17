using System.Text;
using Application.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace API.Tests;

using Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;


public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string TestConnectionString = "DataSource=:memory:";
    private const string TestJwtKey = "test-ci-secret-key-at-least-32-characters-long";
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        
        builder.ConfigureServices(services =>
        {
            // Register SQLite
            var connection = new SqliteConnection(TestConnectionString);
            connection.Open();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(connection));
            
            // Re-register IApplicationDbContext pointing to SQLite context
            services.AddScoped<IApplicationDbContext>(sp =>
                sp.GetRequiredService<ApplicationDbContext>());
            
            var sp = services.BuildServiceProvider();
            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            db.Database.EnsureCreated();
            db.Set<Domain.Entities.Incident>().Add(
                Domain.Entities.Incident.Create(
                    "Test Incident",
                    "Seeded for integration test",
                    Domain.Enums.IncidentSeverity.Critical,
                    1));
            db.SaveChanges();
            
            // Replace the JWT bearer MW config
            services.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme, options =>
                {
                    options.TokenValidationParameters.IssuerSigningKey =
                        new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(TestJwtKey));
                });
        });
    }
}