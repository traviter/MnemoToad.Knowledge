using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MnemoToad.Knowledge.Data;

namespace MnemoToad.Knowledge.Tests.TestSupport;

internal sealed class MockedDbWebApplicationFactory : WebApplicationFactory<Program>
{
    public MockableAppDbContext Db { get; } = new();

    public MockedDbWebApplicationFactory()
    {
        // ConfigureAppConfiguration doesn't land in builder.Configuration before Program.cs's
        // AddApiServices(builder.Configuration) call reads it (that call runs via HostFactoryResolver
        // invoking Program.Main directly, before builder.Build() — the point WebApplicationFactory's
        // config overrides actually apply). An environment variable is read at
        // WebApplication.CreateBuilder(args) time instead, so it's guaranteed to be there in time.
        Environment.SetEnvironmentVariable(
            "ConnectionStrings__Default",
            "Host=localhost;Database=mnemotoad_knowledge_test;Username=test;Password=test");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IAppDbContext>();
            services.AddSingleton<IAppDbContext>(Db);
        });
    }
}
