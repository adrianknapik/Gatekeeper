open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Giraffe
open TodoDemo.TodoRoutes

let webApp =
    choose [
        routes
        RequestErrors.NOT_FOUND "Not found"
    ]

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)

    builder.Services.AddGiraffe() |> ignore

    let app = builder.Build()
    app.UseGiraffe webApp

    app.Run()
    0
