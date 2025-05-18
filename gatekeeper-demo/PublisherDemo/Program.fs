module Program

open System
open System.Threading
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open Giraffe
open Giraffe.EndpointRouting
open System.Text.Json

// Models
type Book = {
    Id: int
    Title: string
    Author: string
    PublisherId: int
}

type Publisher = {
    Id: int
    Name: string
    Country: string
}

// In-memory data store
module DataStore =
    let private bookLock = obj()
    let private publisherLock = obj()
    let mutable private books = [
        { Id = 1; Title = "Go in Action"; Author = "William Kennedy"; PublisherId = 1 }
        { Id = 2; Title = "The Go Programming Language"; Author = "Alan A. A. Donovan"; PublisherId = 1 }
        { Id = 3; Title = "Introducing Go"; Author = "Caleb Doxsey"; PublisherId = 2 }
        { Id = 4; Title = "Go Web Programming"; Author = "Sau Sheong Chang"; PublisherId = 2 }
        { Id = 5; Title = "Go Programming Blueprints"; Author = "Mat Ryer"; PublisherId = 3 }
        { Id = 6; Title = "Learning Go"; Author = "Jon Bodner"; PublisherId = 3 }
        { Id = 7; Title = "Concurrency in Go"; Author = "Katherine Cox-Buday"; PublisherId = 4 }
        { Id = 8; Title = "Go Systems Programming"; Author = "Mihalis Tsoukalos"; PublisherId = 4 }
        { Id = 9; Title = "Network Programming with Go"; Author = "Adam Woodbeck"; PublisherId = 5 }
        { Id = 10; Title = "Go Design Patterns"; Author = "Mario Castro Contreras"; PublisherId = 5 }
        { Id = 11; Title = "Mastering Go"; Author = "Mihalis Tsoukalos"; PublisherId = 6 }
        { Id = 12; Title = "Hands-On High Performance with Go"; Author = "Bob Strecansky"; PublisherId = 6 }
    ]
    let mutable private publishers = [
        { Id = 1; Name = "O'Reilly Media"; Country = "USA" }
        { Id = 2; Name = "Addison-Wesley"; Country = "USA" }
        { Id = 3; Name = "Manning Publications"; Country = "USA" }
        { Id = 4; Name = "Packt Publishing"; Country = "UK" }
        { Id = 5; Name = "Apress"; Country = "USA" }
        { Id = 6; Name = "No Starch Press"; Country = "USA" }
        { Id = 7; Name = "Pearson"; Country = "USA" }
        { Id = 8; Name = "Springer"; Country = "Germany" }
        { Id = 9; Name = "Cambridge University Press"; Country = "UK" }
        { Id = 10; Name = "Typotex Kiadó"; Country = "Hungary" }
    ]
    let mutable private nextBookId = 13
    let mutable private nextPublisherId = 11

    let getAllBooks() =
        lock bookLock (fun () -> books)

    let getBookById id =
        lock bookLock (fun () -> books |> List.tryFind (fun b -> b.Id = id))

    let createBook (book: Book) =
        lock bookLock (fun () ->
            let newBook = { book with Id = nextBookId }
            nextBookId <- nextBookId + 1
            books <- newBook :: books
            newBook)

    let updateBook id (updatedBook: Book) =
        lock bookLock (fun () ->
            let index = books |> List.tryFindIndex (fun b -> b.Id = id)
            match index with
            | Some i ->
                let updated = { updatedBook with Id = id }
                books <- books.[0..i-1] @ [updated] @ books.[i+1..]
                Some updated
            | None -> None)

    let deleteBook id =
        lock bookLock (fun () ->
            let index = books |> List.tryFindIndex (fun b -> b.Id = id)
            match index with
            | Some i ->
                books <- books.[0..i-1] @ books.[i+1..]
                true
            | None -> false)

    let getAllPublishers() =
        lock publisherLock (fun () -> publishers)

    let getPublisherById id =
        lock publisherLock (fun () -> publishers |> List.tryFind (fun p -> p.Id = id))

    let createPublisher (publisher: Publisher) =
        lock publisherLock (fun () ->
            let newPublisher = { publisher with Id = nextPublisherId }
            nextPublisherId <- nextPublisherId + 1
            publishers <- newPublisher :: publishers
            newPublisher)

    let updatePublisher id (updatedPublisher: Publisher) =
        lock publisherLock (fun () ->
            let index = publishers |> List.tryFindIndex (fun p -> p.Id = id)
            match index with
            | Some i ->
                let updated = { updatedPublisher with Id = id }
                publishers <- publishers.[0..i-1] @ [updated] @ publishers.[i+1..]
                Some updated
            | None -> None)

    let deletePublisher id =
        lock publisherLock (fun () ->
            let index = publishers |> List.tryFindIndex (fun p -> p.Id = id)
            match index with
            | Some i ->
                publishers <- publishers.[0..i-1] @ publishers.[i+1..]
                true
            | None -> false)

// Handlers
let jsonOptions = JsonSerializerOptions(PropertyNamingPolicy = JsonNamingPolicy.CamelCase)

let badRequest (msg: string) : HttpHandler =
    setStatusCode 400 >=> json {| Error = msg |}

let notFound (msg: string) : HttpHandler =
    setStatusCode 404 >=> json {| Error = msg |}

let getAllBooks : HttpHandler =
    fun next ctx -> task {
        let books = DataStore.getAllBooks()
        return! json books next ctx
    }

let getBookById (id: string) : HttpHandler =
    fun next ctx -> task {
        match Int32.TryParse id with
        | true, id ->
            match DataStore.getBookById id with
            | Some book -> return! json book next ctx
            | None -> return! notFound "Book not found" next ctx
        | false, _ -> return! badRequest "Invalid book ID" next ctx
    }

let createBook : HttpHandler =
    fun next ctx -> task {
        try
            let! book = ctx.BindJsonAsync<Book>()
            let newBook = DataStore.createBook book
            ctx.SetStatusCode 201
            return! json newBook next ctx
        with
        | _ -> return! badRequest "Invalid request payload" next ctx
    }

let updateBook (id: string) : HttpHandler =
    fun next ctx -> task {
        match Int32.TryParse id with
        | true, id ->
            try
                let! updatedBook = ctx.BindJsonAsync<Book>()
                match DataStore.updateBook id updatedBook with
                | Some book -> return! json book next ctx
                | None -> return! notFound "Book not found" next ctx
            with
            | _ -> return! badRequest "Invalid request payload" next ctx
        | false, _ -> return! badRequest "Invalid book ID" next ctx
    }

let deleteBook (id: string) : HttpHandler =
    fun next ctx -> task {
        match Int32.TryParse id with
        | true, id ->
            if DataStore.deleteBook id then
                ctx.SetStatusCode 204
                return! Successful.NO_CONTENT next ctx
            else
                return! notFound "Book not found" next ctx
        | false, _ -> return! badRequest "Invalid book ID" next ctx
    }

let getAllPublishers : HttpHandler =
    fun next ctx -> task {
        let publishers = DataStore.getAllPublishers()
        return! json publishers next ctx
    }

let getPublisherById (id: string) : HttpHandler =
    fun next ctx -> task {
        match Int32.TryParse id with
        | true, id ->
            match DataStore.getPublisherById id with
            | Some publisher -> return! json publisher next ctx
            | None -> return! notFound "Publisher not found" next ctx
        | false, _ -> return! badRequest "Invalid publisher ID" next ctx
    }

let createPublisher : HttpHandler =
    fun next ctx -> task {
        try
            let! publisher = ctx.BindJsonAsync<Publisher>()
            let newPublisher = DataStore.createPublisher publisher
            ctx.SetStatusCode 201
            return! json newPublisher next ctx
        with
        | _ -> return! badRequest "Invalid request payload" next ctx
    }

let updatePublisher (id: string) : HttpHandler =
    fun next ctx -> task {
        match Int32.TryParse id with
        | true, id ->
            try
                let! updatedPublisher = ctx.BindJsonAsync<Publisher>()
                match DataStore.updatePublisher id updatedPublisher with
                | Some publisher -> return! json publisher next ctx
                | None -> return! notFound "Publisher not found" next ctx
            with
            | _ -> return! badRequest "Invalid request payload" next ctx
        | false, _ -> return! badRequest "Invalid publisher ID" next ctx
    }

let deletePublisher (id: string) : HttpHandler =
    fun next ctx -> task {
        match Int32.TryParse id with
        | true, id ->
            if DataStore.deletePublisher id then
                ctx.SetStatusCode 204
                return! Successful.NO_CONTENT next ctx
            else
                return! notFound "Publisher not found" next ctx
        | false, _ -> return! badRequest "Invalid publisher ID" next ctx
    }

// Routes
let endpoints =
    [
        GET [
            route "/api/publisher_demo/books" getAllBooks
            routef "/api/publisher_demo/books/%s" getBookById
            route "/api/publisher_demo/publishers" getAllPublishers
            routef "/api/publisher_demo/publishers/%s" getPublisherById
        ]
        POST [
            route "/api/publisher_demo/books" createBook
            route "/api/publisher_demo/publishers" createPublisher
        ]
        PUT [
            routef "/api/publisher_demo/books/%s" updateBook
            routef "/api/publisher_demo/publishers/%s" updatePublisher
        ]
        DELETE [
            routef "/api/publisher_demo/books/%s" deleteBook
            routef "/api/publisher_demo/publishers/%s" deletePublisher
        ]
    ]

// App configuration
let configureApp (app: IApplicationBuilder) =
    app.UseRouting()
       .UseGiraffe(endpoints)
       |> ignore

let configureServices (services: IServiceCollection) =
    services.AddGiraffe() |> ignore
    services.AddSingleton(jsonOptions) |> ignore

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)
    configureServices builder.Services
    let app = builder.Build()
    configureApp app
    app.RunAsync("http://0.0.0.0:8081") |> Async.AwaitTask |> Async.RunSynchronously
    0