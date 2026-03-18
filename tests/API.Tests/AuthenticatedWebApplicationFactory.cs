using API.Tests.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace API.Tests;

public class AuthenticatedWebApplicationFactory : CustomWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            var jwtDescriptors = services
                .Where(d => d.ServiceType == typeof(IConfigureOptions<JwtBearerOptions>))
                .ToList();
            foreach (var d in jwtDescriptors)
                services.Remove(d);

            // services.RemoveAll<IAuthenticationSchemeProvider>();
            // services.RemoveAll<IAuthenticationHandlerProvider>();

            services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "TestScheme";
                    options.DefaultChallengeScheme = "TestScheme";
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    "TestScheme", options => { });
        });
    }
}