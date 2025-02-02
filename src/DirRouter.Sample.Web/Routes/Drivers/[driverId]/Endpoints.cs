using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DirRouter.Web.Routes.Drivers._driverId_;

[Authorize(Roles = "Manager")]
public class Endpoints(IConfiguration config, INameService nameService)
{
    public async Task<IResult> Get(string driverId, [FromQuery] int? age)
    {
        await Task.CompletedTask;
        return Results.Ok($"Driver/driverId - You just got get: {driverId}, age: {age}, allowed hosts: {config["AllowedHosts"]}, your name is {await nameService.GetName()}");
    }
    
    [Authorize(Roles = "Admin")]
    public async Task<IResult> Post([FromBody] object request)
    {
        await Task.CompletedTask;
        var ser = JsonSerializer.Serialize(request);
        return Results.Ok($"Driver - You just posted: {ser}");
    }
}