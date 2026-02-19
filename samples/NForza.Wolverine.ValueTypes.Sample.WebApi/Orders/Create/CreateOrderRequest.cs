using NForza.Wolverine.ValueTypes.Sample.WebApi.Customers;

namespace NForza.Wolverine.ValueTypes.Sample.WebApi.Orders.Create;

public record CreateOrderRequest(CustomerId CustomerId, OrderAmount Amount);
