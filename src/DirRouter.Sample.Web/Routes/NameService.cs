namespace DirRouter.Web.Routes;

public interface INameService
{
    Task<string> GetName();
}

public class NameService : INameService
{
    public async Task<string> GetName()
    {
        await Task.CompletedTask;
        return "Daniel";
    }
}