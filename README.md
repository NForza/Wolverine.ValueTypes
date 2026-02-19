# Wolverine.ValueTypes

A Roslyn source generator that produces strongly-typed value type wrappers for use with [Wolverine](https://wolverine.netlify.app/) and [Marten](https://martendb.io/).

Get rid of primitive obsession. Define a one-line value type and the generator takes care of the rest.

## Quick Start

### 1. Define your value types

```csharp
[GuidValue]
public partial record struct CustomerId;

[StringValue(2, 100)]
public partial record struct CustomerName;

[IntValue(1, 10000)]
public partial record struct OrderAmount;

[DoubleValue(0.0, 5.0)]
public partial record struct Rating;
```

That's it. The generator produces a full `record struct` for each, including:

- A single `Value` property with the underlying type
- Implicit conversion to the underlying type
- Explicit conversion from the underlying type
- `TryParse` for Wolverine HTTP route binding
- `JsonConverter` for System.Text.Json serialization
- `IsValid()` with optional min/max/regex validation
- `ToString()`, `IComparable`, `IEquatable`
- `Empty` field (for Guid and String types)

### 2. Use them in your domain

```csharp
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

public record CustomerCreated(CustomerId Id, CustomerName Name);
```

### 3. Use them in Wolverine HTTP endpoints

Value types work directly as route parameters, request/response properties, and Marten aggregate IDs:

```csharp
public record CreateCustomerRequest(CustomerName Name);
public record CustomerResponse(CustomerId Id, CustomerName Name);

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
```

### 4. Register the Wolverine extension

The generator produces a `WolverineValueTypeExtension` that registers all JSON converters. Include it in your Wolverine configuration:

```csharp
builder.Host.UseWolverine(opts =>
{
    opts.Policies.AutoApplyTransactions();
    opts.Include<NForza.Wolverine.ValueTypes.WolverineValueTypeExtension>();
});
```

## Available Attributes

| Attribute | Underlying Type | Parameters |
|-----------|----------------|------------|
| `[GuidValue]` | `Guid` | None |
| `[StringValue]` | `string` | `minimumLength`, `maximumLength`, `validationRegex` (all optional) |
| `[IntValue]` | `int` | `minimum`, `maximum` (optional) |
| `[DoubleValue]` | `double` | `minimum`, `maximum` (optional) |

## What Gets Generated

For each value type, the generator produces:

- **{Name}.g.cs** - The record struct with value semantics, conversions, parsing, and validation.
- **{Name}JsonConverter.g.cs** - A `JsonConverter<T>` for System.Text.Json serialization.

Once per project, it also generates:

- **WolverineValueTypeExtension.g.cs** - An `IWolverineExtension` that registers all generated JSON converters with Wolverine's serializer.

## Marten Compatibility

Value types work as Marten aggregate IDs. When using value type IDs with Marten event sourcing, use `FetchForWriting<T>()` instead of the `[Aggregate]` attribute to avoid code generation conflicts:

```csharp
[WolverinePost("/api/orders/{orderId}/rate")]
public static async Task<OrderResponse?> Rate(
    OrderId orderId,
    RateOrderRequest request,
    IDocumentSession session)
{
    var stream = await session.Events.FetchForWriting<Order>(orderId);
    if (stream.Aggregate is null) return null;

    stream.AppendOne(new OrderRated(request.Rating));
    await session.SaveChangesAsync();

    return new OrderResponse(
        stream.Aggregate.Id,
        stream.Aggregate.CustomerId,
        stream.Aggregate.Amount,
        request.Rating);
}
```

## Running the Example

The `samples/` folder contains a complete Wolverine WebAPI with Marten event sourcing that demonstrates all value type features.

### Run the API

```bash
docker run -d -p 5432:5432 -e POSTGRES_PASSWORD=postgres postgres:16-alpine
dotnet run --project samples/NForza.Wolverine.ValueTypes.Sample.WebApi
```

The API will be available at `http://localhost:5000`.

### Endpoints

| Method | URL | Description |
|--------|-----|-------------|
| `POST` | `/api/customers` | Create a customer. Body: `{ "name": "Alice" }` |
| `GET` | `/api/customers/{customerId}` | Get a customer by ID. |
| `POST` | `/api/orders` | Create an order. Body: `{ "customerId": "...", "amount": 500 }` |
| `GET` | `/api/orders/{orderId}` | Get an order by ID. |
| `POST` | `/api/orders/{orderId}/rate` | Rate an order. Body: `{ "rating": 4.5 }` |

### Try it out

**Bash:**

```bash
# Create a customer and capture the ID
CUSTOMER=$(curl -s -X POST http://localhost:5000/api/customers \
  -H "Content-Type: application/json" \
  -d '{"name": "Alice"}')
CUSTOMER_ID=$(echo $CUSTOMER | jq -r '.id')
echo $CUSTOMER

# Get the customer
curl -s http://localhost:5000/api/customers/$CUSTOMER_ID

# Create an order and capture the ID
ORDER=$(curl -s -X POST http://localhost:5000/api/orders \
  -H "Content-Type: application/json" \
  -d "{\"customerId\": \"$CUSTOMER_ID\", \"amount\": 500}")
ORDER_ID=$(echo $ORDER | jq -r '.id')
echo $ORDER

# Rate the order
curl -s -X POST http://localhost:5000/api/orders/$ORDER_ID/rate \
  -H "Content-Type: application/json" \
  -d '{"rating": 4.5}'
```

**PowerShell:**

```powershell
# Create a customer and capture the ID
$customer = Invoke-RestMethod -Method Post -Uri http://localhost:5000/api/customers `
  -ContentType "application/json" -Body '{"name": "Alice"}'
$customerId = $customer.id
$customer

# Get the customer
Invoke-RestMethod http://localhost:5000/api/customers/$customerId

# Create an order and capture the ID
$order = Invoke-RestMethod -Method Post -Uri http://localhost:5000/api/orders `
  -ContentType "application/json" -Body "{`"customerId`": `"$customerId`", `"amount`": 500}"
$orderId = $order.id
$order

# Rate the order
Invoke-RestMethod -Method Post -Uri http://localhost:5000/api/orders/$orderId/rate `
  -ContentType "application/json" -Body '{"rating": 4.5}'
```

### Run the tests

The integration tests use [Testcontainers](https://dotnet.testcontainers.org/) to spin up PostgreSQL automatically (requires Docker):

```bash
dotnet test
```

## License

MIT
