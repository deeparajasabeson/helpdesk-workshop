using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Helpdesk.ApiTests;

// Each HelpdeskApiFactory drops and re-creates the shared test database, so two test
// classes must not initialise their factories at the same time.
[CollectionDefinition(nameof(GetTicketByIdTests), DisableParallelization = true)]
public class GetTicketByIdCollection;

[Collection(nameof(GetTicketByIdTests))]
public class GetTicketByIdTests(HelpdeskApiFactory factory) : IClassFixture<HelpdeskApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Get_unknown_ticket_returns_404_with_NotFound_code()
    {
        var response = await _client.GetAsync($"/tickets/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        var error = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Tickets.NotFound", error.GetProperty("code").GetString());
    }
}
