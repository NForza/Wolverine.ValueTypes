using NForza.Wolverine.ValueTypes.Sample.WebApi.ValueTypes;

namespace NForza.Wolverine.ValueTypes.Sample.WebApi.Domain;

public class Customer
{
    public CustomerId Id { get; set; }
    public CustomerName Name { get; set; }
    public int Version { get; set; }

    public void Apply(CustomerCreated e)
    {
        Id = e.Id;
        Name = e.Name;
    }
}
