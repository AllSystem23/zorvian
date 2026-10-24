using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace Zorvian.Tests.Integration;

/// <summary>
/// La factory de pruebas tolera ÚNICAMENTE la carrera de teardown de MassTransit
/// (ChannelClosedException al detener el bus in-memory) que xUnit reporta como
/// "Test Class Cleanup Failure" marcando como fallidos tests que pasaron.
/// Cualquier otra excepción de dispose debe seguir propagándose.
/// </summary>
public class CustomWebApplicationFactoryShutdownTests
{
    [Fact]
    public void IsBusShutdownException_ChannelClosed_IsTolerated()
    {
        Assert.True(CustomWebApplicationFactory.IsBusShutdownException(
            new ChannelClosedException("The channel has been closed.")));
    }

    [Fact]
    public void IsBusShutdownException_AggregateWithChannelClosed_IsTolerated()
    {
        var aggregate = new AggregateException(
            "One or more hosted services failed to stop.",
            new ChannelClosedException("The channel has been closed."));

        Assert.True(CustomWebApplicationFactory.IsBusShutdownException(aggregate));
    }

    [Fact]
    public void IsBusShutdownException_AggregateWithForeignWrapperOnly_IsNotTolerated()
    {
        // El AggregateException es solo contenedor: aquí su único hijo es una
        // excepción ajena que envuelve el ruido → se propaga (nada real se traga).
        var aggregate = new AggregateException(
            new InvalidOperationException("host failed", new ChannelClosedException("closed")));

        Assert.False(CustomWebApplicationFactory.IsBusShutdownException(aggregate));
    }

    [Fact]
    public void IsBusShutdownException_WrapperWithChannelClosedInside_IsNotTolerated()
    {
        // Solo el ruido conocido se tolera: un envoltorio ajeno (aquí uno de prueba)
        // hace que la excepción se propague, para no tragarse fallas reales.
        var wrapped = new InvalidOperationException("host failed", new ChannelClosedException("closed"));

        Assert.False(CustomWebApplicationFactory.IsBusShutdownException(wrapped));
    }

    [Fact]
    public void IsBusShutdownException_AggregateMixingShutdownNoiseAndRealFailure_IsNotTolerated()
    {
        var aggregate = new AggregateException(
            new ChannelClosedException("The channel has been closed."),
            new InvalidOperationException("real shutdown failure"));

        Assert.False(CustomWebApplicationFactory.IsBusShutdownException(aggregate));
    }

    [Fact]
    public void IsBusShutdownException_OtherExceptions_AreNotTolerated()
    {
        Assert.False(CustomWebApplicationFactory.IsBusShutdownException(null));
        Assert.False(CustomWebApplicationFactory.IsBusShutdownException(new InvalidOperationException("boom")));
        Assert.False(CustomWebApplicationFactory.IsBusShutdownException(
            new AggregateException(new KeyNotFoundException("missing"))));
        Assert.False(CustomWebApplicationFactory.IsBusShutdownException(
            new InvalidOperationException("wrap", new InvalidOperationException("inner"))));
    }
    [Fact]
    public void Dispose_WithoutHost_DoesNotThrow()
    {
        using var factory = new CustomWebApplicationFactory();

        factory.Dispose();
    }

    [Fact]
    public async Task DisposeAsync_WhenBusStopsWithChannelClosed_DoesNotFailTheTests()
    {
        // Reproduce la carrera real de MassTransit al detener el host de pruebas:
        // un hosted service que lanza ChannelClosedException durante StopAsync.
        var factory = new StopThrowingFactory(() => new ChannelClosedException("The channel has been closed."));
        _ = factory.Services; // arranca el host (es perezoso hasta que se toca)

        var exception = await Record.ExceptionAsync(async () => await factory.DisposeAsync());

        Assert.Null(exception);
    }

    [Fact]
    public async Task DisposeAsync_WhenHostStopsWithAnotherException_StillThrows()
    {
        // El filtro no debe tragarse fallos reales de teardown.
        var factory = new StopThrowingFactory(() => new InvalidOperationException("real shutdown failure"));
        _ = factory.Services; // arranca el host (es perezoso hasta que se toca)

        var exception = await Record.ExceptionAsync(async () => await factory.DisposeAsync());

        Assert.NotNull(exception);
    }

    [Fact]
    public async Task Dispose_WhenBusStopsWithChannelClosed_DoesNotFailTheTests()
    {
        // Camino síncrono: xUnit descarta la fixture vía IDisposable.Dispose()
        // (Dispose() → Dispose(bool) → DisposeAsync()), que es el stack de CI.
        var factory = new StopThrowingFactory(() => new ChannelClosedException("The channel has been closed."));
        _ = factory.Services;

        var exception = await Record.ExceptionAsync(() =>
        {
            factory.Dispose();
            return Task.CompletedTask;
        });

        Assert.Null(exception);
    }

    private sealed class StopThrowingFactory(Func<Exception> stopException) : CustomWebApplicationFactory
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            base.ConfigureWebHost(builder);

            builder.ConfigureServices(services =>
                services.AddSingleton<IHostedService>(new StopThrowingHostedService(stopException)));
        }
    }

    private sealed class StopThrowingHostedService(Func<Exception> stopException) : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        public Task StopAsync(CancellationToken cancellationToken) => throw stopException();
    }
}