using NForza.Wolverine.ValueTypes.Sample.WebApi.Customers;

namespace NForza.Wolverine.ValueTypes.Sample.WebApi.Orders;

public record OrderResponse(OrderId Id, CustomerId CustomerId, OrderAmount Amount, Rating? Rating);
