using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ApexPost.IntegrationTests.Integration;

public sealed class ApexPostWebFactory(string allowedIp = "127.0.0.1", string apiKey = "integration-test-secret", bool maintenanceEnabled = false) : WebApplicationFactory<Program>
{
    private readonly string _dataDirectory = Path.Combine(Path.GetTempPath(), "ApexPostTests", Guid.NewGuid().ToString("N"));
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            var values = new Dictionary<string, string?>
            {
                ["Data:Directory"] = _dataDirectory,
                ["DemoPasswords:CounterClerk"] = "CounterTestPassword!123",
                ["DemoPasswords:BranchManager"] = "ManagerTestPassword!123",
                ["DemoPasswords:Auditor"] = "AuditorTestPassword!123",
                ["DemoPasswords:Administrator"] = "AdminTestPassword!123",
                ["Integration:ApiKey"] = apiKey,
                ["Integration:PartnerWidgetOrigin"] = "https://tracking.test.example",
                ["Security:AllowedIps:0"] = allowedIp,
                ["MoneyOrders:IdentityThreshold"] = "1000",
                ["Maintenance:Enabled"] = maintenanceEnabled.ToString()
            };
            configuration.AddInMemoryCollection(values);
        });
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(_dataDirectory))
        {
            try { Directory.Delete(_dataDirectory, true); } catch (IOException) { }
        }
    }
}
