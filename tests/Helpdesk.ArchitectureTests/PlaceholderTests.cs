namespace Helpdesk.ArchitectureTests;

public class PlaceholderTests
{
    [Fact]
    public void ModuleInfo_Name_IsSharedKernel()
    {
        Assert.Equal("SharedKernel", Helpdesk.SharedKernel.ModuleInfo.Name);
    }
}
