using Marten;
using Marten.Events;
using Marten.Events.Aggregation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Weasel.Core;
using Wolverine;
using Wolverine.Attributes;
using Wolverine.Tracking;
using Wolverine.Marten;
using Microsoft.Extensions.Logging;

namespace EndToEnd
{
    //Simple Domain Object
    public record Order
    {
        public Guid Id { get; init; }
        public int Value { get; init; }
        public static Order Create(CreateOrderCommand command)
        {
            return new Order
            {
                Id = command.OrderId,
                Value = 0
            };
        }

        public static Order Apply(UpdateOrderValue command, Order order)
        {
            return order with { Value = command.NewValue };
        }
    }

    //Simple Projection of it
    public class OrderProjection : SingleStreamProjection<Order> { }


    //Commands and Queries
    public record CreateOrderCommand
    {
        public Guid OrderId { get; init; } = Guid.NewGuid();
    }

    public record UpdateOrderValue
    {
        public Guid OrderId { get; init; }
        public int NewValue { get; init; }
    }
    public record QueryOrder
    {
        public Guid OrderId { get; init; }
    }

    //All my Handlers
    [WolverineHandler]
    public static class OrderCommandHandlers
    {
        [Transactional]
        public static void Handle(CreateOrderCommand command, IDocumentSession session, ILogger logger)
        {
            logger.Log(LogLevel.Information, $"Creating for {command.OrderId}");
            session.Events.StartStream<Order>(command.OrderId, command);
            // THIS COULD ALSO WORK, BUT WHY WOULD APPEND TO A NON EXISTANT STREAM
            // WORK?
            //session.Events.Append(command.OrderId, command);
        }

        [Transactional]
        public static void Handle(UpdateOrderValue command, IDocumentSession session)
        {
            session.Events.Append(command.OrderId, command);
        }

        [Transactional]
        public static async Task<Order?> Handle(QueryOrder query, IDocumentSession session)
        {
            var order = await session.LoadAsync<Order>(query.OrderId);
            return order;
        }
    }

    public class Tests
    {
        [Fact]
        public async void Test1()
        {
            //Configure my Host
            var host = Host.CreateDefaultBuilder()
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
                    }).IntegrateWithWolverine();
                })
                .UseWolverine((options) =>
                {
                    options.UseSystemTextJsonForSerialization();
                    options.Durability.Mode = DurabilityMode.Solo;
                    options.Discovery.IncludeAssembly(typeof(Tests).Assembly);
                    options.AutoBuildMessageStorageOnStartup = true;
                    options.StubAllExternalTransports();
                })
                .Build();
            host.Start();

            //Get the Store and clean it
            var ServiceProvider = host.Services;
            var store = ServiceProvider.GetRequiredService<IDocumentStore>();
            await store.Advanced.Clean.DeleteAllDocumentsAsync();
            await store.Advanced.Clean.DeleteAllEventDataAsync();

            var createOrder = new CreateOrderCommand();
            await host.SendMessageAndWaitAsync(createOrder);

            var updateOrder = new UpdateOrderValue() { OrderId = createOrder.OrderId, NewValue = 2 };
            await host.SendMessageAndWaitAsync(updateOrder);

            var query = new QueryOrder() { OrderId = createOrder.OrderId };
            var (session,order) = await host.InvokeMessageAndWaitAsync<Order>(query);

            Assert.NotNull(order);
            Assert.Equal(2, order.Value);
        }
    }
}