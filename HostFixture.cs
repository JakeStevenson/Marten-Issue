using JasperFx.Core;
using Marten;
using Marten.Events;
using Marten.Exceptions;
using Microsoft.Extensions.Hosting;
using Weasel.Core;
using Oakton;
using Wolverine;
using Wolverine.Marten;
using Wolverine.ErrorHandling;
using Xunit.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace EndToEnd
{
    public class HostFixture : IAsyncLifetime
    {
        public IHost host;
        public HostFixture()
        {
            host = Host.CreateDefaultBuilder()
                .ConfigureServices((context, services) =>
                {
                    services.AddMarten(options =>
                    {
                        options.Connection("Host=LOCALHOST;Port=5432;Database=TESTDATA;Username=postgres;Password=mypassword;Include Error Detail=True");
                        options.AutoCreateSchemaObjects = AutoCreate.CreateOrUpdate;
                        options.Events.StreamIdentity = StreamIdentity.AsGuid;
                        options.Projections.Add<OrderProjection>(Marten.Events.Projections.ProjectionLifecycle.Inline);
                        options.DisableNpgsqlLogging = true;
                        options.UseSystemTextJsonForSerialization();
                    })
                    .UseLightweightSessions()
                    .IntegrateWithWolverine();
                })
                .UseWolverine((options) =>
                {
                    options.UseSystemTextJsonForSerialization();
                    options.Durability.Mode = DurabilityMode.Solo;
                    options.AutoBuildMessageStorageOnStartup = true;
                    options.StubAllExternalTransports();
                    //I'm not seeing this make a difference.
                    options.OnException<ExistingStreamIdCollisionException>()
                        .RetryOnce()
                        .Then.RetryWithCooldown(100.Milliseconds(), 250.Milliseconds(), 1000.Milliseconds(), 5000.Seconds())
                        .Then.Discard();
                })
                .ApplyOaktonExtensions()
                .Build();
        }
        public async Task InitializeAsync()
        {
            await host.StartAsync();
            System.Diagnostics.Debug.WriteLine("Host started");

            //I moved this out of the individual runs so I can examine the database afterward
            // and observe if the projections are correct for my assertion failures,
            // or events are processed when I get collisions
            var ServiceProvider = host.Services;
            var store = ServiceProvider.GetRequiredService<IDocumentStore>();
            await store.Advanced.Clean.DeleteAllDocumentsAsync();
            await store.Advanced.Clean.DeleteAllEventDataAsync();
            System.Diagnostics.Debug.WriteLine("Data Cleaned");
        }
        public Task DisposeAsync() => Task.CompletedTask;

    }
}
