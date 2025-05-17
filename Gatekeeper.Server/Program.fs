open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Giraffe
open Gatekeeper.Database
open Gatekeeper
open Gatekeeper.Models
open Newtonsoft.Json
open Newtonsoft.Json.Converters
open System
open Microsoft.Data.Sqlite
open Microsoft.AspNetCore.Http

let configureJson () =
    let jsonSettings = JsonSerializerSettings()
    jsonSettings.Converters.Add(StringEnumConverter())
    jsonSettings.Converters.Add(RuleConverter())
    jsonSettings.NullValueHandling <- NullValueHandling.Ignore
    JsonConvert.DefaultSettings <- Func<JsonSerializerSettings>(fun () -> jsonSettings)

// Új függvény az összes felhasználó számának lekérdezéséhez
let countUsers () : int64 =
    use conn = new SqliteConnection(Database.connectionString)
    conn.Open()
    let cmd = conn.CreateCommand()
    cmd.CommandText <- "SELECT COUNT(1) FROM Users"
    cmd.ExecuteScalar() :?> int64


[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)
    
    configureJson()

    // CORS konfiguráció hozzáadása specifikus eredet engedélyezésével
    builder.Services.AddCors(fun options ->
        options.AddPolicy("AllowAllOrigins", fun builder ->
            builder
                .AllowAnyOrigin()                     // Kliens eredete
                .AllowAnyMethod()                     // Engedélyezett HTTP metódusok (beleértve OPTIONS)
                .AllowAnyHeader()                     // Engedélyezett fejlécek
            |> ignore
        )
    ) |> ignore

    // Giraffe konfigurálása
    builder.Services
        .AddGiraffe()
        |> ignore

    let app = builder.Build()

    // CORS middleware engedélyezése
    app.UseCors("AllowAllOrigins") |> ignore

    // Route-ok hozzáadása
    
    // Combine all routes into one handler
    let allRoutes =
        choose [
            Routes.Evaluate.evaluateRoutes
            Routes.Account.accountRoutes
            Routes.webApp
        ]

    // Register the combined routes
    app.UseGiraffe allRoutes

    initDatabase()

    // Admin fiók seedelése csak akkor, ha az adatbázis üres
    let userCount = countUsers()
    if userCount = 0L then
        let adminEmail = Environment.GetEnvironmentVariable "ADMIN_EMAIL"
        let adminPassword = Environment.GetEnvironmentVariable "ADMIN_PASSWORD"
        if String.IsNullOrWhiteSpace adminEmail || String.IsNullOrWhiteSpace adminPassword then
            printfn "No users in database and ADMIN_EMAIL or ADMIN_PASSWORD not set, creating default admin."
            addUser "admin" (BCrypt.Net.BCrypt.HashPassword "admin")
        else
            addUser adminEmail (BCrypt.Net.BCrypt.HashPassword adminPassword)
            printfn "No users in database, created admin user '%s'." adminEmail
    else
        printfn "Users already exist in database, skipping admin creation."

    app.Run()
    0