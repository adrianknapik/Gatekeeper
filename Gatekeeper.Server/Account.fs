module Routes.Account

open System
open System.Collections.Concurrent
open Giraffe
open Microsoft.AspNetCore.Http
open Newtonsoft.Json
open Gatekeeper.Database
open Giraffe

// DTO-k a bejövő JSON-okhoz
type RegisterDto = {
    [<JsonProperty("username")>]
    Username: string
    [<JsonProperty("password")>]
    Password: string
}

type LoginDto = {
    [<JsonProperty("username")>]
    Username: string
    [<JsonProperty("password")>]
    Password: string
}

type DeleteUserDto = {
    [<JsonProperty("username")>]
    Username: string
    [<JsonProperty("password")>]
    Password: string
}

let loginHandler : HttpHandler =
    fun next ctx ->
        task {
            printfn "Login handler called for /api/gk/account/login"
            try
                let! dto = ctx.BindJsonAsync<LoginDto>()
                printfn "Deserialized DTO: Username=%s, Password=%s" dto.Username dto.Password

                match Gatekeeper.Database.tryGetUser dto.Username with
                | Some (username, storedPassword) when storedPassword = dto.Password ->
                    printfn "Login successful for username: %s" username
                    ctx.SetStatusCode 200
                    return! json {| message = "Login successful" |} next ctx
                | Some _ ->
                    printfn "Login failed for username: %s. Invalid credentials" dto.Username
                    ctx.SetStatusCode 403
                    return! json {| error = "Invalid credentials" |} next ctx
                | None ->
                    printfn "Login failed: User %s not found" dto.Username
                    ctx.SetStatusCode 403
                    return! json {| error = "Invalid credentials" |} next ctx
            with ex ->
                printfn "Error during login: %s" ex.Message
                ctx.SetStatusCode 400
                return! json {| error = "Bad request: " + ex.Message |} next ctx
        }
        
// Regisztrációs handler
let registerHandler : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            printfn "Register handler called for /api/gk/account/register"
            try
                let! dto = ctx.BindJsonAsync<RegisterDto>()
                printfn "Register attempt for username: %s" dto.Username

                if userExists dto.Username then
                    // Már létezik ilyen felhasználó
                    printfn "Registration failed: User %s already exists" dto.Username
                    ctx.SetStatusCode 400
                    return! json {| error = "User already exists" |} next ctx
                else
                    // Új felhasználó hozzáadása
                    addUser dto.Username dto.Password
                    printfn "Registration successful for username: %s" dto.Username
                    ctx.SetStatusCode 201
                    return! json {| message = "Registration successful" |} next ctx
            with ex ->
                printfn "Error during registration: %s" ex.Message
                ctx.SetStatusCode 400
                return! json {| error = ex.Message |} next ctx
        }

// Felhasználó törlése handler
let deleteUserHandler : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            printfn "Delete user handler called for /api/gk/account/delete"
            try
                let! dto = ctx.BindJsonAsync<DeleteUserDto>()
                printfn "Delete attempt for username: %s" dto.Username

                match Gatekeeper.Database.tryGetUser dto.Username with
                | Some (username, storedPassword) when storedPassword = dto.Password ->
                    let deleted = Gatekeeper.Database.deleteUser dto.Username dto.Password
                    if deleted then
                        printfn "User %s deleted successfully" dto.Username
                        ctx.SetStatusCode 200
                        return! json {| message = "User deleted successfully" |} next ctx
                    else
                        printfn "Failed to delete user %s: User not found or invalid credentials" dto.Username
                        ctx.SetStatusCode 400
                        return! json {| error = "Failed to delete user" |} next ctx
                | Some _ ->
                    printfn "Delete failed for username: %s. Invalid credentials" dto.Username
                    ctx.SetStatusCode 403
                    return! json {| error = "Invalid credentials" |} next ctx
                | None ->
                    printfn "Delete failed: User %s not found" dto.Username
                    ctx.SetStatusCode 404
                    return! json {| error = "User not found" |} next ctx
            with ex ->
                printfn "Error during user deletion: %s" ex.Message
                ctx.SetStatusCode 400
                return! json {| error = "Bad request: " + ex.Message |} next ctx
        }

// Összes felhasználónév lekérdezése handler
let getAllUsernamesHandler : HttpHandler =
    fun (next: HttpFunc) (ctx: HttpContext) ->
        task {
            printfn "Get all usernames handler called for /api/gk/account/usernames"
            try
                let usernames = Gatekeeper.Database.getAllUsernames()
                printfn "Fetched %d usernames" usernames.Length
                ctx.SetStatusCode 200
                return! json {| usernames = usernames |} next ctx
            with ex ->
                printfn "Error during fetching usernames: %s" ex.Message
                ctx.SetStatusCode 400
                return! json {| error = "Bad request: " + ex.Message |} next ctx
        }

// Az Account endpointok összefűzése
let accountRoutes : HttpHandler =
    printfn "Account routes initialized"
    choose [
        POST >=> route "/api/gk/account/register" >=> registerHandler
        POST >=> route "/api/gk/account/login" >=> loginHandler
        POST >=> route "/api/gk/account/delete" >=> deleteUserHandler
        GET >=> route "/api/gk/account/usernames" >=> getAllUsernamesHandler
    ]