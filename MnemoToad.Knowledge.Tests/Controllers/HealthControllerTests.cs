using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MnemoToad.Knowledge.Api.Controllers;
using Moq;
using NUnit.Framework;

namespace MnemoToad.Knowledge.Tests.Controllers;

[TestFixture]
public class HealthControllerTests
{
    private static HealthReport BuildReport(HealthStatus checkStatus) =>
        new(
            new Dictionary<string, HealthReportEntry>
            {
                ["database"] = new HealthReportEntry(
                    checkStatus,
                    description: checkStatus == HealthStatus.Unhealthy ? "connection failed" : null,
                    duration: TimeSpan.Zero,
                    exception: null,
                    data: null)
            },
            totalDuration: TimeSpan.Zero);

    private static Mock<HealthCheckService> MockService(HealthReport report)
    {
        var mock = new Mock<HealthCheckService>();
        mock.Setup(s => s.CheckHealthAsync(null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(report);
        return mock;
    }

    [Test]
    public async Task Get_WhenDatabaseHealthy_ReturnsOkWithHealthyStatus()
    {
        var report = BuildReport(HealthStatus.Healthy);
        var controller = new HealthController(MockService(report).Object);

        var result = await controller.Get();

        var ok = result as OkObjectResult;
        Assert.That(ok, Is.Not.Null);
        var status = ok!.Value?.GetType().GetProperty("status")?.GetValue(ok.Value);
        Assert.That(status, Is.EqualTo("Healthy"));
    }

    [Test]
    public async Task Get_WhenDatabaseUnreachable_ReturnsServiceUnavailableWithUnhealthyStatus()
    {
        var report = BuildReport(HealthStatus.Unhealthy);
        var controller = new HealthController(MockService(report).Object);

        var result = await controller.Get();

        var objectResult = result as ObjectResult;
        Assert.That(objectResult, Is.Not.Null);
        Assert.That(objectResult!.StatusCode, Is.EqualTo(StatusCodes.Status503ServiceUnavailable));
        var status = objectResult.Value?.GetType().GetProperty("status")?.GetValue(objectResult.Value);
        Assert.That(status, Is.EqualTo("Unhealthy"));
    }
}
