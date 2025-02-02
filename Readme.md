# DirRouter - Directory Based C# Router

Converts this directory structure:
```
    Routes/
        Endpoints.cs // Get(), Post()
        Drivers/
            Endpoints.cs // Get(), Post()
                [driverId]/
                    Endpoints.cs // Get(), Post()
```
To these endpoints:
```
    GET /
    POST /
    GET Drivers
    POST Drivers
    GET Drivers/123
    POST Drivers/123
```

Inspired by Next.js pages router

## Setup
1. Install `DriRouter` NuGet
2. ```
    builder.Services.AddControllers()
   ...
   app.MapControllers()
   ```
3. Create `Routes` directory in the project
4. Create `Endpoints.cs` files within the `Routes` directory
5. Add methods: `Get()`, `Post()`, `Put()` or `Delete()` inside `Endpoints.cs`

You can also take a look at the `DirRouter.Sample.Web` project

## How it works?
It uses a source generator to convert each `Endpoints.cs` file into a controller, 
converting each `Get()`, `Post()`, `Put()` or `Delete()` method into an actual HTTP endpoint.

If you want to add dynamic path parameters (e.g. `/drivers/{driverId}`), just create a directory [driverId] with 
`Endpoints.cs` inside of it.

You can apply attributes (e.g. `[Authorize]`) to the `Endpoints.cs` class, and it will be transferred to generated controller.

## Why?
I like collocation of code, a directory based router which automatically creates the URLs just makes sense to me.

###
License MIT