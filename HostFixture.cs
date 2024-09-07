using Marten;
using Marten.Events;
using Microsoft.Extensions.Hosting;
using Weasel.Core;
using Wolverine;
using Wolverine.Marten;
using Xunit.Abstractions;

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
                        options.Connection("Host=LOCALHOST;Port=5432;Database=TESTDATA;Username=postgres;Password=mypassword");
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
                })
                .Build();
        }
        public async Task InitializeAsync()
        {
            await host.StartAsync();
            System.Diagnostics.Debug.WriteLine("Host started");
        }
        public Task DisposeAsync() => Task.CompletedTask;

    }
}
