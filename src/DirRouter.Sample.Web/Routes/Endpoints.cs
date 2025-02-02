using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace DirRouter.Web.Routes;

public class Endpoints(IConfiguration config)
{
    public async Task<IResult> Get()
    {
        await Task.CompletedTask;
        return Results.Ok("ROOT Get");
    }
    
    public async Task<IResult> Post([FromBody] object request)
    {
        await Task.CompletedTask;
        var ser = JsonSerializer.Serialize(request);
        return Results.Ok($"Post: {ser}");
    }
}