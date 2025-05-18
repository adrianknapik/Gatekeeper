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

// DTO a bejövő JSON-hoz (opcionális, ha a kérés body-t tartalmaz)
type EvaluateDto = {
    [<JsonProperty("context")>]
    Context: Map<string, string> option
}

// Két Map egyesítése, az második Map prioritást kap azonos kulcsok esetén
let private mapUnion (map1: Map<'k, 'v>) (map2: Map<'k, 'v>) : Map<'k, 'v> =
    map2 |> Map.fold (fun acc key value -> Map.add key value acc) map1

// JWT kinyerése az Authorization header-ből
let private extractJwt (ctx: HttpContext) : string option =
    ctx.Request.Headers.["Authorization"]
    |> Seq.tryHead
    |> Option.map (fun auth -> auth.Replace("Bearer ", ""))

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

// /api/evaluate endpoint kezelője
let evaluateHandler : HttpHandler =
    fun next ctx ->
        task {
            printfn "POST /api/gk/evaluate"
            try
                // Ellenőrizd, hogy van-e body
                let dto = 
                    if ctx.Request.HasJsonContentType() && ctx.Request.ContentLength.HasValue && ctx.Request.ContentLength.Value > 0L then
                        task {
                            let! parsedDto = ctx.BindJsonAsync<EvaluateDto>()
                            return parsedDto
                        }
                    else
                        task { return { Context = None } } // Alapértelmezett üres DTO

                let! dto = dto
                printfn "Deserialized DTO: Context=%A" dto.Context

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

                // Opcionális body context hozzáadása
                let additionalContext = dto.Context |> Option.defaultValue Map.empty

                let context = {
                    Claims = claims
                    Headers = headers
                    QueryParams = mapUnion queryParams additionalContext
                    RouteParams = Map.empty
                    Ip = ctx.Connection.RemoteIpAddress.ToString()
                    Timestamp = DateTime.UtcNow
                }

                // X-Forwarded-Uri header kinyerése
                let forwardedUri =
                    ctx.Request.Headers.["X-Forwarded-Uri"]
                    |> Seq.tryHead
                    |> Option.defaultValue ""

                // Eredeti HTTP metódus kinyerése
                let httpMethod =
                    ctx.Request.Headers.["X-Forwarded-Method"]
                    |> Seq.tryHead
                    |> Option.defaultValue "POST"
                    |> fun method -> method.ToUpper()
                printfn "Original HTTP method from X-Forwarded-Method: %s" httpMethod

                // Szabályok betöltése és szűrése
                let rules =
                    Database.getRules ()
                    |> List.filter (fun rule ->
                        let endpointMatch =
                            rule.Endpoint
                            |> Option.map (fun endpoint -> forwardedUri.Contains endpoint)
                            |> Option.defaultValue true
                        let httpTypeMatch =
                            rule.HttpType
                            |> Option.map (fun httpType -> httpType.ToString().ToUpper() = httpMethod)
                            |> Option.defaultValue true
                        endpointMatch && httpTypeMatch
                    )
                printfn "Fetched %d rules for URI: %s, Method: %s" rules.Length forwardedUri httpMethod

                // Szabályok kiértékelése
                let result = PolicyEngine.evaluateRules rules context
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