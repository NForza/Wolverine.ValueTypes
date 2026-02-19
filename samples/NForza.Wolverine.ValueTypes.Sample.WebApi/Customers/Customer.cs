namespace NForza.Wolverine.ValueTypes.Sample.WebApi.Customers;

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
