using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Shouldly;
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

        id.ShouldNotBeNull();
        Guid.TryParse(id, out var guid).ShouldBeTrue();
        guid.ShouldNotBe(Guid.Empty);
        name.ShouldBe("Alice");
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
        json.GetProperty("id").GetString().ShouldBe(id);
        json.GetProperty("name").GetString().ShouldBe("Bob");
    }

    [Fact]
    public async Task GetCustomer_WithInvalidGuid_Returns404OrBadRequest()
    {
        var response = await client.GetAsync("/api/customers/not-a-guid");

        new[] { HttpStatusCode.NotFound, HttpStatusCode.BadRequest }
            .ShouldContain(response.StatusCode);
    }

    [Fact]
    public async Task GetCustomer_WithNonExistentId_ReturnsNullContent()
    {
        var response = await client.GetAsync($"/api/customers/{Guid.NewGuid()}");

        // Wolverine returns 404 for null return values
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
