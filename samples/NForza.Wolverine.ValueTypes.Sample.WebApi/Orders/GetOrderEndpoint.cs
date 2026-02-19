using Marten;
using NForza.Wolverine.ValueTypes.Sample.WebApi.Customers;
using Wolverine.Http;

namespace NForza.Wolverine.ValueTypes.Sample.WebApi.Orders;

public static class GetOrderEndpoint
{
    [WolverineGet("/api/orders/{orderId}")]
    public static async Task<OrderResponse?> Get(OrderId orderId, IQuerySession session)
    {
        var order = await session.Events.AggregateStreamAsync<Order>(orderId);
        if (order is null) return null;
        return new OrderResponse(order.Id, order.CustomerId, order.Amount, order.Rating);
    }
}
