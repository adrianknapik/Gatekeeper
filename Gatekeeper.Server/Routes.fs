namespace Gatekeeper

open Giraffe
open Microsoft.AspNetCore.Http
open Gatekeeper.Models
open Gatekeeper.Database
open Newtonsoft.Json
open Newtonsoft.Json.FSharp
open Newtonsoft.Json.Converters

module Routes =
    // JSON beállítás Giggle ek
    let jsonSettings = JsonSerializerSettings() |> Serialisation.extend
    jsonSettings.Converters.Add(StringEnumConverter())
    jsonSettings.NullValueHandling <- NullValueHandling.Ignore

    // Hibakezelő
    let errorHandler (message: string) (statusCode: int) : HttpHandler =
        fun next ctx ->
            task {
                printfn "Hiba: %s, Státuszkód: %d" message statusCode
                do ctx.SetStatusCode statusCode
                do ctx.SetContentType "application/json"
                let payload = {| Error = message |}
                let json    = JsonConvert.SerializeObject(payload, jsonSettings)
                do! ctx.Response.WriteAsync(json)
                return! next ctx
            }

    // GET /api/rules
    let getRulesHandler : HttpHandler =
        fun next ctx ->
            task {
                printfn "GET /api/rules kérés fogadása"
                try
                    let rules = Database.getRules()
                    printfn "Szabályok lekérve: %d db" rules.Length
                    do ctx.SetContentType "application/json"
                    let json = JsonConvert.SerializeObject(rules, jsonSettings)
                    do! ctx.Response.WriteAsync(json)
                    return! next ctx
                with ex ->
                    printfn "GET /api/rules hiba: %s" ex.Message
                    return! errorHandler $"Failed to fetch rules: {ex.Message}" 500 next ctx
            }

    // POST /api/rules
    let createRuleHandler : HttpHandler =
        fun next ctx ->
            task {
                printfn "POST /api/rules kérés fogadása"
                try
                    let! body = ctx.ReadBodyFromRequestAsync()
                    printfn "Bemenet: %s" body
                    let rule  = JsonConvert.DeserializeObject<Rule>(body, jsonSettings)
                    printfn "Deszerializált szabály: %A" rule

                    let newId = Database.insertRule rule
                    printfn "Új szabály beszúrva: Id=%d" newId

                    do ctx.SetStatusCode 201
                    do ctx.SetContentType "application/json"
                    let json = JsonConvert.SerializeObject({| Id = newId |}, jsonSettings)
                    do! ctx.Response.WriteAsync(json)

                    return! next ctx
                with
                | :? JsonException as jex ->
                    printfn "POST /api/rules JSON hiba: %s" jex.Message
                    return! errorHandler $"Invalid JSON: {jex.Message}" 400 next ctx
                | ex ->
                    printfn "POST /api/rules hiba: %s" ex.Message
                    return! errorHandler $"Failed to create rule: {ex.Message}" 500 next ctx
            }

    // PUT /api/rules/{id}
    let updateRuleHandler (id: int) : HttpHandler =
        fun next ctx ->
            task {
                printfn "PUT /api/rules/%d kérés fogadása" id
                try
                    let! body = ctx.ReadBodyFromRequestAsync()
                    printfn "Bemenet: %s" body
                    let incoming =
                        JsonConvert.DeserializeObject<Rule>(
                            body,
                            jsonSettings
                        )
                    printfn "Deszerializált szabály: %A" incoming
                    let affected = Database.updateRule id incoming
                    printfn "Szabály frissítve: Id=%d, Érintett sorok=%d" id affected
                    if affected = 1 then
                        return! setStatusCode 204 next ctx
                    else
                        printfn "Szabály nem található: Id=%d" id
                        return! (RequestErrors.NOT_FOUND "Rule not found") next ctx
                with ex ->
                    printfn "PUT /api/rules/%d hiba: %s" id ex.Message
                    return! errorHandler $"Failed to update rule: {ex.Message}" 500 next ctx
            }

    // DELETE /api/rules/{id}
    let deleteRuleHandler (id: int) : HttpHandler =
        fun next ctx ->
            task {
                printfn "DELETE /api/rules/%d kérés fogadása" id
                try
                    let success = Database.deleteRule id
                    printfn "Szabály törölve: Id=%d, Sikeres=%b" id success
                    return! setStatusCode 204 next ctx
                with ex ->
                    printfn "DELETE /api/rules/%d hiba: %s" id ex.Message
                    return! errorHandler $"Failed to delete rule: {ex.Message}" 500 next ctx
            }

    // Routing
    let webApp : HttpHandler =
        choose [
            route "/gk/" >=> text "Gatekeeper API"
            route "/api/gk/rules" >=> choose [
                GET  >=> getRulesHandler
                POST >=> createRuleHandler
            ]
            PUT    >=> routef "/api/gk/rules/%i" updateRuleHandler
            DELETE >=> routef "/api/gk/rules/%i" deleteRuleHandler
            setStatusCode 404 >=> text "Not Found"
        ]