using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace NForza.Wolverine.ValueTypes.Integration.Tests;

public class OrderApiTests : IClassFixture<SampleApiFixture>
{
    private readonly HttpClient client;

    public OrderApiTests(SampleApiFixture fixture)
    {
        client = fixture.CreateClient();
    }

    private async Task<string> CreateCustomerAsync(string name = "TestCustomer")
    {
        var response = await client.PostAsJsonAsync("/api/customers", new { Name = name });
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        return json.GetProperty("id").GetString()!;
    }

    [Fact]
    public async Task CreateOrder_ReturnsOrderWithValueTypes()
    {
        var customerId = await CreateCustomerAsync();

        var response = await client.PostAsJsonAsync("/api/orders", new
        {
            CustomerId = customerId,
            Amount = 500
        });

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var orderId = json.GetProperty("id").GetString();
        var returnedCustomerId = json.GetProperty("customerId").GetString();
        var amount = json.GetProperty("amount").GetInt32();

        Assert.NotNull(orderId);
        Assert.True(Guid.TryParse(orderId, out _));
        Assert.Equal(customerId, returnedCustomerId);
        Assert.Equal(500, amount);
    }

    [Fact]
    public async Task GetOrder_AfterCreate_ReturnsOrder()
    {
        var customerId = await CreateCustomerAsync();

        var createResponse = await client.PostAsJsonAsync("/api/orders", new
        {
            CustomerId = customerId,
            Amount = 250
        });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var orderId = created.GetProperty("id").GetString()!;

        await Task.Delay(200);

        var getResponse = await client.GetAsync($"/api/orders/{orderId}");
        getResponse.EnsureSuccessStatusCode();

        var json = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(orderId, json.GetProperty("id").GetString());
        Assert.Equal(customerId, json.GetProperty("customerId").GetString());
        Assert.Equal(250, json.GetProperty("amount").GetInt32());
    }

    [Fact]
    public async Task GetOrder_NonExistent_Returns404()
    {
        var response = await client.GetAsync($"/api/orders/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RateOrder_UpdatesRating()
    {
        var customerId = await CreateCustomerAsync();

        var createResponse = await client.PostAsJsonAsync("/api/orders", new
        {
            CustomerId = customerId,
            Amount = 100
        });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var orderId = created.GetProperty("id").GetString()!;

        await Task.Delay(200);

        var rateResponse = await client.PostAsJsonAsync($"/api/orders/{orderId}/rate", new
        {
            Rating = 4.5
        });
        rateResponse.EnsureSuccessStatusCode();

        var json = await rateResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(4.5, json.GetProperty("rating").GetDouble());
    }

    [Fact]
    public async Task ValueTypes_RoundTrip_ThroughJson()
    {
        // This test verifies that all value types serialize/deserialize correctly
        var customerId = await CreateCustomerAsync("RoundTripTest");

        var createResponse = await client.PostAsJsonAsync("/api/orders", new
        {
            CustomerId = customerId,
            Amount = 9999
        });
        createResponse.EnsureSuccessStatusCode();

        var rawJson = await createResponse.Content.ReadAsStringAsync();

        // Verify the JSON structure contains proper value type serialization
        var json = JsonDocument.Parse(rawJson);
        var root = json.RootElement;

        // CustomerId should serialize as a GUID string
        Assert.Equal(JsonValueKind.String, root.GetProperty("customerId").ValueKind);
        Assert.True(Guid.TryParse(root.GetProperty("customerId").GetString(), out _));

        // OrderId should serialize as a GUID string
        Assert.Equal(JsonValueKind.String, root.GetProperty("id").ValueKind);
        Assert.True(Guid.TryParse(root.GetProperty("id").GetString(), out _));

        // Amount should serialize as a number
        Assert.Equal(JsonValueKind.Number, root.GetProperty("amount").ValueKind);
        Assert.Equal(9999, root.GetProperty("amount").GetInt32());
    }
}
