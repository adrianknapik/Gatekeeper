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
                printfn "Error: %s, Status code: %d" message statusCode
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
                printfn "GET /api/rules"
                try
                    let rules = Database.getRules()
                    do ctx.SetContentType "application/json"
                    let json = JsonConvert.SerializeObject(rules, jsonSettings)
                    do! ctx.Response.WriteAsync(json)
                    return! next ctx
                with ex ->
                    return! errorHandler $"Failed to fetch rules: {ex.Message}" 500 next ctx
            }

    // POST /api/rules
    let createRuleHandler : HttpHandler =
        fun next ctx ->
            task {
                printfn "POST /api/rules"
                try
                    let! body = ctx.ReadBodyFromRequestAsync()
                    let rule  = JsonConvert.DeserializeObject<Rule>(body, jsonSettings)

                    let newId = Database.insertRule rule

                    do ctx.SetStatusCode 201
                    do ctx.SetContentType "application/json"
                    let json = JsonConvert.SerializeObject({| Id = newId |}, jsonSettings)
                    do! ctx.Response.WriteAsync(json)

                    return! next ctx
                with
                | :? JsonException as jex ->
                    return! errorHandler $"Invalid JSON: {jex.Message}" 400 next ctx
                | ex ->
                    return! errorHandler $"Failed to create rule: {ex.Message}" 500 next ctx
            }

    // PUT /api/rules/{id}
    let updateRuleHandler (id: int) : HttpHandler =
        fun next ctx ->
            task {
                printfn "PUT /api/rules/%d " id
                try
                    let! body = ctx.ReadBodyFromRequestAsync()
                    let incoming =
                        JsonConvert.DeserializeObject<Rule>(
                            body,
                            jsonSettings
                        )
                    let affected = Database.updateRule id incoming
                    if affected = 1 then
                        return! setStatusCode 204 next ctx
                    else
                        return! (RequestErrors.NOT_FOUND "Rule not found") next ctx
                with ex ->
                    return! errorHandler $"Failed to update rule: {ex.Message}" 500 next ctx
            }

    // DELETE /api/rules/{id}
    let deleteRuleHandler (id: int) : HttpHandler =
        fun next ctx ->
            task {
                printfn "DELETE /api/rules/%d " id
                try
                    let success = Database.deleteRule id
                    return! setStatusCode 204 next ctx
                with ex ->
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