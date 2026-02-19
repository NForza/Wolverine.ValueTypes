using NForza.Wolverine.ValueTypes.Sample.WebApi.Customers;
using Wolverine.Http;
using Wolverine.Marten;

namespace NForza.Wolverine.ValueTypes.Sample.WebApi.Orders.Create;

public static class CreateOrderEndpoint
{
    [WolverinePost("/api/orders")]
    public static (OrderResponse, IStartStream) Post(CreateOrderRequest request)
    {
        var orderId = new OrderId();
        var @event = new OrderCreated(orderId, request.CustomerId, request.Amount);
        var startStream = MartenOps.StartStream<Order>(orderId, @event);
        return (new OrderResponse(orderId, request.CustomerId, request.Amount, null), startStream);
    }
}
