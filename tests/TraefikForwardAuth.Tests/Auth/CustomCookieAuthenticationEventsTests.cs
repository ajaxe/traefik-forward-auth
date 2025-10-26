using TraefikForwardAuth.Auth;
using Xunit.Abstractions;
using Xunit;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using Moq;
using TraefikForwardAuth.Abstractions.Services;
using Microsoft.Extensions.Options;
using TraefikForwardAuth.Configuration;
using System.Net;

namespace TraefikForwardAuth.Tests.Auth;

public class CustomCookieAuthenticationEventsTests : AppTestBase
{
    private ILogger<CustomCookieAuthenticationEvents> nullLogger;
    private Mock<IAuthService> authServiceMock;
    private Mock<IOptions<AppOptions>> optionsMock;

    public CustomCookieAuthenticationEventsTests(ITestOutputHelper output) : base(output)
    {
        this.nullLogger = NullLogger<CustomCookieAuthenticationEvents>.Instance;
        this.authServiceMock = new Mock<IAuthService>();
        this.optionsMock = new Mock<IOptions<AppOptions>>();
    }

    [Fact]
    public void ApplyServiceTokenToRedirectUri_UpdateUri_Ok()
    {
        var sut = new CustomCookieAuthenticationEvents(
            authServiceMock.Object,
            optionsMock.Object,
            nullLogger
        );
        var returnUrlParam = "returnUrl";
        var returnUrlValue = WebUtility.UrlEncode("/login/check?token=1234");
        var testUri = $"https://localhost/login?{returnUrlParam}={returnUrlValue}";
        var testServiceToken = Guid.NewGuid().ToString();

        var result = sut.ApplyServiceTokenToRedirectUri(testUri, returnUrlParam, testServiceToken);

        var expected = $"https://localhost/login?{returnUrlParam}={WebUtility.UrlEncode($"/login/check?token={testServiceToken}")}";

        Assert.Equal(expected, result);
    }
}