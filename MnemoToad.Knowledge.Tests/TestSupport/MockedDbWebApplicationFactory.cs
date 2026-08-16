using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MnemoToad.Knowledge.Data;

namespace MnemoToad.Knowledge.Tests.TestSupport;

internal sealed class MockedDbWebApplicationFactory : WebApplicationFactory<Program>
{
    public MockableAppDbContext Db { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Default"] = "Host=localhost;Database=mnemotoad_knowledge_test;Username=test;Password=test"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IAppDbContext>();
            services.AddSingleton<IAppDbContext>(Db);
        });
    }
}
