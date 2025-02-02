using Microsoft.AspNetCore.Mvc;

namespace DirRouter.Web.Routes.Drivers;

public class Endpoints
{
    private readonly IConfiguration _config;

    public Endpoints(IConfiguration config)
    {
        _config = config;
    }

    public async Task<IResult> Get([FromQuery] int? age)
    {
        await Task.CompletedTask;
        return Results.Ok($"Driver - allowed hosts: {_config["AllowedHosts"]} | {await GetResponse()} | {await GetResponse2()}");
    }
    
    private async Task<string> GetResponse() => await Task.FromResult("Driver - response from private method");

    private async Task<string> GetResponse2()
    {
        return await Task.FromResult("Driver - response from private method");
    }
}