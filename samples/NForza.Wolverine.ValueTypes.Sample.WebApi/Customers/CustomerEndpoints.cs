using Marten;
using Wolverine.Http;
using Wolverine.Marten;

namespace NForza.Wolverine.ValueTypes.Sample.WebApi.Customers;

public static class CustomerEndpoints
{
    [WolverinePost("/api/customers")]
    public static (CustomerResponse, IStartStream) Post(CreateCustomerRequest request)
    {
        var customerId = new CustomerId();
        var @event = new CustomerCreated(customerId, request.Name);
        var startStream = MartenOps.StartStream<Customer>(customerId, @event);
        return (new CustomerResponse(customerId, request.Name), startStream);
    }

    [WolverineGet("/api/customers/{customerId}")]
    public static async Task<CustomerResponse?> Get(CustomerId customerId, IQuerySession session)
    {
        var customer = await session.Events.AggregateStreamAsync<Customer>(customerId);
        if (customer is null) return null;
        return new CustomerResponse(customer.Id, customer.Name);
    }
}
