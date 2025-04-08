using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace TraefikForwardAuth.Tests;

public abstract class AppTestBase
{
    protected IServiceCollection ServiceIoC { get; }
    protected IServiceProvider ServiceProvider { get; }
    public ITestOutputHelper OutputHelper { get; }

    protected AppTestBase(ITestOutputHelper output)
    {
        OutputHelper = output;
    }
}