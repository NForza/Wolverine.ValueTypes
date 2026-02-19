using Marten;
using Wolverine.Http;

namespace NForza.Wolverine.ValueTypes.Sample.WebApi.Orders.Rate;

public static class RateOrderEndpoint
{
    [WolverinePost("/api/orders/{orderId}/rate")]
    public static async Task<OrderResponse?> Post(
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
