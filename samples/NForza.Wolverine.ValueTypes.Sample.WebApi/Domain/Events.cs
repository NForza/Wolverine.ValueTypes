using NForza.Wolverine.ValueTypes.Sample.WebApi.ValueTypes;

namespace NForza.Wolverine.ValueTypes.Sample.WebApi.Domain;

public record CustomerCreated(CustomerId Id, CustomerName Name);

public record OrderCreated(OrderId Id, CustomerId CustomerId, OrderAmount Amount);

public record OrderRated(Rating Rating);
