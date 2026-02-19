using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Shouldly;
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

        orderId.ShouldNotBeNull();
        Guid.TryParse(orderId, out _).ShouldBeTrue();
        returnedCustomerId.ShouldBe(customerId);
        amount.ShouldBe(500);
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
        json.GetProperty("id").GetString().ShouldBe(orderId);
        json.GetProperty("customerId").GetString().ShouldBe(customerId);
        json.GetProperty("amount").GetInt32().ShouldBe(250);
    }

    [Fact]
    public async Task GetOrder_NonExistent_Returns404()
    {
        var response = await client.GetAsync($"/api/orders/{Guid.NewGuid()}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
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
        json.GetProperty("rating").GetDouble().ShouldBe(4.5);
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
        root.GetProperty("customerId").ValueKind.ShouldBe(JsonValueKind.String);
        Guid.TryParse(root.GetProperty("customerId").GetString(), out _).ShouldBeTrue();

        // OrderId should serialize as a GUID string
        root.GetProperty("id").ValueKind.ShouldBe(JsonValueKind.String);
        Guid.TryParse(root.GetProperty("id").GetString(), out _).ShouldBeTrue();

        // Amount should serialize as a number
        root.GetProperty("amount").ValueKind.ShouldBe(JsonValueKind.Number);
        root.GetProperty("amount").GetInt32().ShouldBe(9999);
    }
}
