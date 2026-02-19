using NForza.Wolverine.ValueTypes.Sample.WebApi.Customers;
using NForza.Wolverine.ValueTypes.Sample.WebApi.Orders.Create;
using NForza.Wolverine.ValueTypes.Sample.WebApi.Orders.Rate;

namespace NForza.Wolverine.ValueTypes.Sample.WebApi.Orders;

public class Order
{
    public OrderId Id { get; set; }
    public CustomerId CustomerId { get; set; }
    public OrderAmount Amount { get; set; }
    public Rating? Rating { get; set; }
    public int Version { get; set; }

    public void Apply(OrderCreated e)
    {
        Id = e.Id;
        CustomerId = e.CustomerId;
        Amount = e.Amount;
    }

    public void Apply(OrderRated e)
    {
        Rating = e.Rating;
    }
}
