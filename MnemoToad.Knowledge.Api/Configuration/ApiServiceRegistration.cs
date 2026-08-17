using Microsoft.EntityFrameworkCore;
using MnemoToad.Knowledge.Api.Json;
using MnemoToad.Knowledge.Api.Swagger;
using MnemoToad.Knowledge.Data;
using MnemoToad.Knowledge.Data.Configuration;

namespace MnemoToad.Knowledge.Api.Configuration;

public static class ApiServiceRegistration
{
    public static IServiceCollection AddApiServices(IServiceCollection services, IConfiguration configuration)
    {
        // Wires up routing/model-binding/action-invocation for [ApiController] classes.
        services.AddControllers()
            .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new ScalarJsonValueConverter()));

        // Translates a ValidationException escaping any action into a 400 ProblemDetails response.
        services.AddExceptionHandler<ValidationExceptionHandler>();
        services.AddProblemDetails();

        // Lets Swashbuckle discover our controllers' routes/parameters/response types.
        services.AddEndpointsApiExplorer();
        // Registers the OpenAPI document generator (built from the explorer data above).
        // Nothing is written to disk here — the JSON is generated in memory per-request by
        // app.UseSwagger() below. Configuration lives in SwaggerGenOptionsSetup.
        services.ConfigureOptions<SwaggerGenOptionsSetup>();
        services.AddSwaggerGen();

        services.AddDbContext<AppDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Default")).UseSnakeCaseNamingConvention());
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddHealthChecks()
            .AddNpgSql(configuration.GetConnectionString("Default")!, name: "database");

        DataServiceRegistration.AddDataServices(services);

        return services;
    }
}
