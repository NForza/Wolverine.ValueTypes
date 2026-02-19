using Marten;
using NForza.Wolverine.ValueTypes.Sample.WebApi.Domain;
using NForza.Wolverine.ValueTypes.Sample.WebApi.ValueTypes;
using Wolverine.Http;
using Wolverine.Marten;

namespace NForza.Wolverine.ValueTypes.Sample.WebApi.Endpoints;

public record CreateOrderRequest(CustomerId CustomerId, OrderAmount Amount);

public record RateOrderRequest(Rating Rating);

public record OrderResponse(OrderId Id, CustomerId CustomerId, OrderAmount Amount, Rating? Rating);

public static class OrderEndpoints
{
    [WolverinePost("/api/orders")]
    public static (OrderResponse, IStartStream) Post(CreateOrderRequest request)
    {
        var orderId = new OrderId();
        var @event = new OrderCreated(orderId, request.CustomerId, request.Amount);
        var startStream = MartenOps.StartStream<Order>(orderId, @event);
        return (new OrderResponse(orderId, request.CustomerId, request.Amount, null), startStream);
    }

    [WolverineGet("/api/orders/{orderId}")]
    public static async Task<OrderResponse?> Get(OrderId orderId, IQuerySession session)
    {
        var order = await session.Events.AggregateStreamAsync<Order>(orderId);
        if (order is null) return null;
        return new OrderResponse(order.Id, order.CustomerId, order.Amount, order.Rating);
    }

    [WolverinePost("/api/orders/{orderId}/rate")]
    public static async Task<OrderResponse?> Rate(
        OrderId orderId,
        RateOrderRequest request,
        IDocumentSession session)
    {
        var stream = await session.Events.FetchForWriting<Order>(orderId);
        if (stream.Aggregate is null) return null;

        var @event = new OrderRated(request.Rating);
        stream.AppendOne(@event);
        await session.SaveChangesAsync();

        return new OrderResponse(
            stream.Aggregate.Id,
            stream.Aggregate.CustomerId,
            stream.Aggregate.Amount,
            request.Rating);
    }
}
