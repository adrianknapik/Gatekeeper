module Routes.Evaluate

open System
open Giraffe
open Microsoft.AspNetCore.Http
open System.IdentityModel.Tokens.Jwt
open Newtonsoft.Json
open Gatekeeper.Models
open Gatekeeper.Database
open Gatekeeper.PolicyEngine
open Gatekeeper

// DTO a bejövő JSON-hoz
type EvaluateDto = {
    [<JsonProperty("path")>]
    Path: string option
    [<JsonProperty("method")>]
    Method: string option
    [<JsonProperty("context")>]
    Context: Map<string, string> option
}

// Két Map egyesítése, az második Map prioritást kap azonos kulcsok esetén
let private mapUnion (map1: Map<'k, 'v>) (map2: Map<'k, 'v>) : Map<'k, 'v> =
    map2 |> Map.fold (fun acc key value -> Map.add key value acc) map1

// JWT kinyerése az Authorization header-ből
let private extractJwt (ctx: HttpContext) : string option =
    ctx.Request.Headers.TryGetValue("Authorization")
    |> fun (exists, value) -> if exists then Some (value.ToString().Replace("Bearer ", "")) else None

// JWT claim-ek kinyerése
let private extractJwtClaims (jwt: string) : Map<string, string> =
    try
        let handler = JwtSecurityTokenHandler()
        let token = handler.ReadJwtToken(jwt)
        token.Claims
        |> Seq.map (fun claim -> (claim.Type, claim.Value))
        |> Map.ofSeq
    with
    | ex ->
        printfn "Error parsing JWT: %s" ex.Message
        Map.empty

// Case-insensitive header kinyerés
let private getHeaderIgnoreCase (ctx: HttpContext) (headerName: string) : string =
    ctx.Request.Headers
    |> Seq.tryFind (fun kv -> kv.Key.Equals(headerName, StringComparison.OrdinalIgnoreCase))
    |> Option.map (fun kv -> kv.Value.ToString())
    |> Option.defaultValue ""

// /api/evaluate endpoint kezelője
let evaluateHandler : HttpHandler =
    fun next ctx ->
        task {
            printfn "POST /api/gk/evaluate"
            printfn "Request Headers: %A" ctx.Request.Headers
            try
                // Body deszerializálása
                let dtoTask =
                    if ctx.Request.HasJsonContentType() && ctx.Request.ContentLength.HasValue && ctx.Request.ContentLength.Value > 0L then
                        ctx.BindJsonAsync<EvaluateDto>()
                    else
                        task { return { Path = None; Method = None; Context = None } }
                
                let! dto = dtoTask
                printfn "Deserialized DTO: Path=%A, Method=%A, Context=%A" dto.Path dto.Method dto.Context

                // Kontextus összeállítása
                let jwt = extractJwt ctx
                let claims = jwt |> Option.map extractJwtClaims |> Option.defaultValue Map.empty
                let queryParams =
                    ctx.Request.Query
                    |> Seq.map (fun kv -> (kv.Key, kv.Value.ToString()))
                    |> Map.ofSeq
                let headers =
                    ctx.Request.Headers
                    |> Seq.map (fun kv -> (kv.Key, kv.Value.ToString()))
                    |> Map.ofSeq
                let additionalContext = dto.Context |> Option.defaultValue Map.empty

                let context = {
                    Claims = claims
                    Headers = headers
                    QueryParams = mapUnion queryParams additionalContext
                    RouteParams = Map.empty
                    Ip = ctx.Connection.RemoteIpAddress.ToString()
                    Timestamp = DateTime.UtcNow
                }
                printfn "Context QueryParams: %A" context.QueryParams

                // X-Forwarded-Uri és X-Forwarded-Method kinyerése
                let forwardedUri = getHeaderIgnoreCase ctx "X-Forwarded-Uri"
                let httpMethod = getHeaderIgnoreCase ctx "X-Forwarded-Method" |> fun m -> if String.IsNullOrEmpty m then "GET" else m.ToUpper()
                
                // DTO-ból származó értékek prioritása
                let finalUri = 
                    match dto.Path, forwardedUri with
                    | Some path, _ -> path
                    | None, uri when not (String.IsNullOrEmpty uri) -> uri
                    | _ -> "/unknown" // Alapértelmezett URI, ha minden hiányzik
                let finalMethod = dto.Method |> Option.map (fun m -> m.ToUpper()) |> Option.defaultValue httpMethod

                printfn "Evaluating URI: %s, Method: %s" finalUri finalMethod

                // Szabályok betöltése és szűrése
                let rules = Database.getRules ()
                printfn "All rules: %A" rules

                let filteredRules =
                    rules
                    |> List.filter (fun rule ->
                        let endpointMatch =
                            rule.Endpoint
                            |> Option.map (fun endpoint ->
                                let cleanUri = finalUri.Split('?').[0] // Query paraméterek eltávolítása
                                printfn "Checking endpoint: Rule=%s, URI=%s" endpoint cleanUri
                                cleanUri.ToLower().Contains(endpoint.ToLower())
                            )
                            |> Option.defaultValue true
                        let httpTypeMatch =
                            rule.HttpType
                            |> Option.map (fun httpType ->
                                let ruleMethod = httpType.ToString().ToUpper()
                                printfn "Checking method: Rule=%s, Method=%s" ruleMethod finalMethod
                                ruleMethod = finalMethod
                            )
                            |> Option.defaultValue true
                        endpointMatch && httpTypeMatch
                    )
                printfn "Fetched %d rules for URI: %s, Method: %s" filteredRules.Length finalUri finalMethod

                // Szabályok kiértékelése
                let result = PolicyEngine.evaluateRules filteredRules context
                printfn "Evaluation result: Allowed=%b" result

                // Válasz
                if result then
                    ctx.SetStatusCode 200
                    return! json {| Allowed = true |} next ctx
                else
                    ctx.SetStatusCode 403
                    return! json {| Allowed = false; Error = "Request denied by policy" |} next ctx
            with ex ->
                printfn "Error during evaluation: %s" ex.Message
                ctx.SetStatusCode 500
                return! json {| Error = "Internal server error: " + ex.Message |} next ctx
        }

// Evaluate endpointok összefűzése
let evaluateRoutes : HttpHandler =
    printfn "Evaluate routes initialized"
    choose [
        POST >=> route "/api/gk/evaluate" >=> evaluateHandler
    ]