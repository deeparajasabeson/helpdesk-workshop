namespace Helpdesk.Tickets.Domain.Tests;

public class PlaceholderTests
{
    [Fact]
    public void ModuleInfo_Name_IsTickets()
    {
        Assert.Equal("Tickets", Helpdesk.Tickets.Domain.ModuleInfo.Name);
    }
}
