using NForza.Wolverine.ValueTypes.Sample.WebApi.Customers;

namespace NForza.Wolverine.ValueTypes.Sample.WebApi.Orders.Create;

public record OrderCreated(OrderId Id, CustomerId CustomerId, OrderAmount Amount);
