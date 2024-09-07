using Marten;
using Marten.Events.Aggregation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wolverine.Attributes;
using Wolverine.Tracking;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

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
        public CreateOrderCommand(Guid orderId, ITestOutputHelper logger)
        {
            logger.WriteLine($"Created order command {orderId}");
            OrderId = orderId;
        }
        public Guid OrderId { get; init; }
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
        }

        [Transactional]
        public static void Handle(UpdateOrderValue command, IDocumentSession session)
        {
            session.Events.Append(command.OrderId, command);
        }

        [Transactional]
        public static Order? Handle(QueryOrder query, IDocumentSession session)
        {
            var order = session.Load<Order>(query.OrderId);
            return order;
        }
    }

    [Collection("With Fixture")]
    public class WithFixtureTests : IClassFixture<HostFixture>
    {
        private ITestOutputHelper _output;
        private IHost host;
        public WithFixtureTests(HostFixture fixture, ITestOutputHelper output)
        {
            _output = output;
            host = fixture.host;
        }
        [Theory]
        [Repeat(500)]
        public async void WithFixtureTest(int iteration)
        {
            //Configure my Host
            //Get the Store and clean it

            var createOrder = new CreateOrderCommand(Guid.NewGuid(), _output);
            _output.WriteLine($"Iteration {iteration}");
            _output.WriteLine($"Calling CREATE for {createOrder.OrderId}");
            await host.SendMessageAndWaitAsync(createOrder);

            var updateOrder = new UpdateOrderValue() { OrderId = createOrder.OrderId, NewValue = 2 };
            _output.WriteLine($"Calling UPDATE for {updateOrder.OrderId}");
            await host.SendMessageAndWaitAsync(updateOrder);

            var query = new QueryOrder() { OrderId = createOrder.OrderId };
            _output.WriteLine($"Calling QUERY for {query.OrderId}");
            var (session, order) = await host.InvokeMessageAndWaitAsync<Order>(query);

            Assert.NotNull(order);
            Assert.Equal(2, order.Value);
        }
    }
}