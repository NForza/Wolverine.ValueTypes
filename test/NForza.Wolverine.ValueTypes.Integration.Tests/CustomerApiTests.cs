using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Xunit;

namespace NForza.Wolverine.ValueTypes.Integration.Tests;

public class CustomerApiTests : IClassFixture<SampleApiFixture>
{
    private readonly HttpClient client;
    private static readonly JsonSerializerOptions json = new(JsonSerializerDefaults.Web);

    public CustomerApiTests(SampleApiFixture fixture)
    {
        client = fixture.CreateClient();
    }

    [Fact]
    public async Task CreateCustomer_ReturnsCustomerWithId()
    {
        var response = await client.PostAsJsonAsync("/api/customers", new { Name = "Alice" });

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        var id = json.GetProperty("id").GetString();
        var name = json.GetProperty("name").GetString();

        Assert.NotNull(id);
        Assert.True(Guid.TryParse(id, out var guid));
        Assert.NotEqual(Guid.Empty, guid);
        Assert.Equal("Alice", name);
    }

    [Fact]
    public async Task GetCustomer_AfterCreate_ReturnsCustomer()
    {
        // Create
        var createResponse = await client.PostAsJsonAsync("/api/customers", new { Name = "Bob" });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<JsonElement>();
        var id = created.GetProperty("id").GetString()!;

        // Small delay for event sourcing to project
        await Task.Delay(200);

        // Get
        var getResponse = await client.GetAsync($"/api/customers/{id}");
        getResponse.EnsureSuccessStatusCode();

        var json = await getResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(id, json.GetProperty("id").GetString());
        Assert.Equal("Bob", json.GetProperty("name").GetString());
    }

    [Fact]
    public async Task GetCustomer_WithInvalidGuid_Returns404OrBadRequest()
    {
        var response = await client.GetAsync("/api/customers/not-a-guid");

        Assert.True(
            response.StatusCode == HttpStatusCode.NotFound ||
            response.StatusCode == HttpStatusCode.BadRequest,
            $"Expected 404 or 400, got {response.StatusCode}");
    }

    [Fact]
    public async Task GetCustomer_WithNonExistentId_ReturnsNullContent()
    {
        var response = await client.GetAsync($"/api/customers/{Guid.NewGuid()}");

        // Wolverine returns 404 for null return values
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
