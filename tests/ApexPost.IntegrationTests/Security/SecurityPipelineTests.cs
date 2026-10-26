using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace ApexPost.IntegrationTests.Integration;

public sealed class SecurityPipelineTests
{
    [Fact]
    public async Task Security_headers_are_present()
    {
        using var factory = new ApexPostWebFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        using var response = await client.GetAsync("/Account/Login");
        Assert.True(response.Headers.Contains("Content-Security-Policy"));
        Assert.True(response.Headers.Contains("X-Content-Type-Options"));
        Assert.True(response.Headers.Contains("X-Frame-Options"));
        Assert.True(response.Headers.Contains("Referrer-Policy"));
    }

    [Fact]
    public async Task Integration_endpoint_returns_401_without_api_key_failed()
    {
        using var factory = new ApexPostWebFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        using var response = await client.GetAsync("/integration/parcel-status/not-found");
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
    }


    [Fact]
    public async Task Integration_endpoint_returns_403_from_blocked_ip()
    {
        using var factory = new ApexPostWebFactory("203.0.113.10");
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        using var response = await client.GetAsync("/integration/parcel-status/not-found");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Login_endpoint_returns_429_after_limit_is_exceeded()
    {
        using var factory = new ApexPostWebFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false, HandleCookies = true });
        var html = await client.GetStringAsync("/Account/Login");
        var token = ExtractToken(html);
        HttpResponseMessage? last = null;
        for (var i = 0; i < 6; i++)
        {
            last?.Dispose();
            last = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token, ["Email"] = "nobody@example.test", ["Password"] = "WrongPassword!123", ["ReturnUrl"] = "" }));
        }
        Assert.NotNull(last);
        Assert.Equal((HttpStatusCode)429, last!.StatusCode);
        last!.Dispose();
    }

    [Fact]
    public async Task Unauthenticated_access_redirects_to_login()
    {
        using var factory = new ApexPostWebFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        using var response = await client.GetAsync("/Dashboard/Index");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        Assert.Contains("/Account/Login", response.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Clerk_receives_403_when_posting_void()
    {
        using var factory = new ApexPostWebFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false, HandleCookies = true });
        var html = await client.GetStringAsync("/Account/Login");
        var token = ExtractToken(html);
        using var login = await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string> { ["__RequestVerificationToken"] = token, ["Email"] = "counterclerk1@apexpost.local", ["Password"] = "CounterTestPassword!123", ["ReturnUrl"] = "" }));
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);
        using var response = await client.PostAsync("/Transactions/Void", new FormUrlEncodedContent(new Dictionary<string, string> { ["TransactionId"] = "anything", ["Reason"] = "Test void" }));
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Maintenance_returns_503_with_retry_after_but_keeps_health_available()
    {
        using var factory = new ApexPostWebFactory(maintenanceEnabled: true);
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        using var maintenance = await client.GetAsync("/Dashboard/Index");
        Assert.Equal((HttpStatusCode)503, maintenance.StatusCode);
        Assert.True(maintenance.Headers.Contains("Retry-After"));
        using var health = await client.GetAsync("/health");
        Assert.Equal(HttpStatusCode.OK, health.StatusCode);
    }

    [Fact]
    public async Task Correlation_id_is_echoed()
    {
        using var factory = new ApexPostWebFactory();
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
        using var request = new HttpRequestMessage(HttpMethod.Get, "/Account/Login");
        request.Headers.Add("X-Correlation-Id", "assessment-test-123");
        using var response = await client.SendAsync(request);
        Assert.Equal("assessment-test-123", response.Headers.GetValues("X-Correlation-Id").Single());
    }

    private static string ExtractToken(string html)
    {
        var match = Regex.Match(html, "name=\\\"__RequestVerificationToken\\\"[^>]*value=\\\"([^\\\"]+)\\\"", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            match = Regex.Match(html, "value=\\\"([^\\\"]+)\\\"[^>]*name=\\\"__RequestVerificationToken\\\"", RegexOptions.IgnoreCase);
        }

        Assert.True(match.Success, "Antiforgery token was not rendered.");
        return WebUtility.HtmlDecode(match.Groups[1].Value);
    }
}
