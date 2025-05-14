open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Giraffe
open Gatekeeper.Database
open Gatekeeper
open Gatekeeper.Models
open Newtonsoft.Json
open Newtonsoft.Json.Converters
open System

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)
    
    // JSON szerializációs beállítások (opcionálisan itt definiálható, de Routes.fs-ben használjuk)
    let jsonSettings = JsonSerializerSettings()
    jsonSettings.Converters.Add(StringEnumConverter())
    jsonSettings.Converters.Add(RuleConverter())
    jsonSettings.NullValueHandling <- NullValueHandling.Ignore

    JsonConvert.DefaultSettings <- Func<JsonSerializerSettings>( fun () -> jsonSettings)

    // Giraffe konfigurálása szerializáció nélkül
    builder.Services
        .AddGiraffe()
        |> ignore

    let app = builder.Build()
    app.UseGiraffe(Routes.webApp)
    initDatabase()
    app.Run()
    0