namespace API.Tests;

using Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;


public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            var connection = new SqliteConnection("DataSource=:memory:");
            connection.Open();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(connection));

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
        });
    }
}