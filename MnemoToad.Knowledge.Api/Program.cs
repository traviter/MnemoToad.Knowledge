using MnemoToad.Knowledge.Api.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApiServices(builder.Configuration);

var app = builder.Build();

// Serves the generated OpenAPI JSON, then an interactive browsable UI on top of it, at /swagger —
// enabled in every environment (including Azure) so MnemoToad.Platform can link straight to it.
app.UseSwagger();
app.UseSwaggerUI();

app.MapControllers();

app.Run();

public partial class Program { }
