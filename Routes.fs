namespace Gatekeeper

open Giraffe
open Microsoft.AspNetCore.Http
open Gatekeeper.Models
open Gatekeeper.Database
open Newtonsoft.Json
open Newtonsoft.Json.FSharp
open Newtonsoft.Json.Converters

module Routes =
    // JSON szerializációs beállítások
    let jsonSettings = JsonSerializerSettings() |> Serialisation.extend
    jsonSettings.Converters.Add(StringEnumConverter()) 
    jsonSettings.NullValueHandling <- NullValueHandling.Ignore

    let errorHandler (message: string) : HttpHandler =
        fun next ctx ->
            task {
                ctx.SetStatusCode 400
                ctx.SetContentType "application/json"
                let response = JsonConvert.SerializeObject({| Error = message |}, jsonSettings)
                do! ctx.Response.WriteAsync(response)
                return! next ctx
            }

    let getRulesHandler : HttpHandler =
        fun next ctx ->
            task {
                try
                    let rules = Database.getRules()
                    ctx.SetContentType "application/json"
                    let response = JsonConvert.SerializeObject(rules, jsonSettings)
                    do! ctx.Response.WriteAsync(response)
                    return! next ctx
                with
                | ex -> return! errorHandler $"Failed to fetch rules: {ex.Message}" next ctx
            }

    let createRuleHandler : HttpHandler =
        fun next ctx ->
            task {
                try
                    // JSON testreszöveg olvasása
                    let! json = ctx.ReadBodyFromRequestAsync()
                    printfn "Received JSON: %s" json
                    // Deszerializáció Newtonsoft.Json-nal
                    let rule = JsonConvert.DeserializeObject<Rule>(json, jsonSettings)
                    printfn "Deserialized Rule:"
                    printfn "  Id: %A" rule.Id
                    printfn "  Conditions: %A" rule.Conditions
                    match rule.Conditions with
                    | Some conditions when not (List.isEmpty conditions) ->
                        conditions |> List.iteri (fun i cond ->
                            printfn "  Condition %d: Field=%A, Operator=%A, Value=%A" 
                                i cond.Field cond.Operator cond.Value)
                    | _ -> ()
                    printfn "  LogicalOperator: %A" rule.LogicalOperator
                    printfn "  Action: %A" rule.Action
                    // Validáció
                    match rule with
                    | { Conditions = None } -> 
                        return! errorHandler "Conditions cannot be null" next ctx
                    | { Conditions = Some conditions } when List.isEmpty conditions -> 
                        return! errorHandler "Conditions cannot be empty" next ctx
                    | { Action = None } -> 
                        return! errorHandler "Action cannot be null" next ctx
                    | { LogicalOperator = None } -> 
                        return! errorHandler "LogicalOperator cannot be null" next ctx
                    | _ ->
                        let newId = Database.insertRule rule
                        ctx.SetStatusCode 201
                        ctx.SetContentType "application/json"
                        let response = JsonConvert.SerializeObject({| Id = newId |}, jsonSettings)
                        do! ctx.Response.WriteAsync(response)
                        return! next ctx
                with
                | :? JsonException as jsonEx ->
                    printfn "JSON deserialization error: %s" jsonEx.Message
                    return! errorHandler $"Invalid JSON format: {jsonEx.Message}" next ctx
                | ex -> 
                    printfn "Unexpected error: %s" ex.Message
                    return! errorHandler $"Failed to create rule: {ex.Message}" next ctx
            }

    let webApp : HttpHandler =
        choose [
            route "/" >=> text "Gatekeeper API"
            route "/api/rules" >=> choose [
                GET >=> getRulesHandler
                POST >=> createRuleHandler
            ]
            setStatusCode 404 >=> text "Not Found"
        ]