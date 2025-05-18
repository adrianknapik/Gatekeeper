namespace TodoDemo

open Giraffe
open Microsoft.AspNetCore.Http
open System.Threading
open TodoDemo // Hogy elérjük a Todo típust

module TodoRoutes =

    let mutable nextId = 11

    let mutable todos: Todo list = [
        { Id = 1; Title = "Buy milk"; IsCompleted = false }
        { Id = 2; Title = "Read a book"; IsCompleted = true }
        { Id = 3; Title = "Clean the house"; IsCompleted = false }
        { Id = 4; Title = "Walk the dog"; IsCompleted = true }
        { Id = 5; Title = "Do 10 pushups"; IsCompleted = false }
        { Id = 6; Title = "Reply to emails"; IsCompleted = false }
        { Id = 7; Title = "Prepare dinner"; IsCompleted = true }
        { Id = 8; Title = "Water the plants"; IsCompleted = false }
        { Id = 9; Title = "Fix the sink"; IsCompleted = false }
        { Id = 10; Title = "Finish F# project"; IsCompleted = false }
    ]

    let getAllTodos: HttpHandler =
        fun next ctx ->
            json todos next ctx

    let getTodoById (id: int): HttpHandler =
        fun next ctx ->
            match todos |> List.tryFind (fun t -> t.Id = id) with
            | Some todo -> json todo next ctx
            | None -> RequestErrors.NOT_FOUND $"Todo with id {id} not found" next ctx

    let createTodo: HttpHandler =
        fun next ctx ->
            task {
                let! newTodo = ctx.BindJsonAsync<Todo>()
                let todoWithId = { newTodo with Id = Interlocked.Increment(&nextId) }
                todos <- todoWithId :: todos
                return! json todoWithId next ctx
            }

    let updateTodo (id: int): HttpHandler =
        fun next ctx ->
            task {
                let! updated = ctx.BindJsonAsync<Todo>()
                match todos |> List.tryFind (fun t -> t.Id = id) with
                | Some _ ->
                    todos <- todos |> List.map (fun t -> if t.Id = id then { updated with Id = id } else t)
                    return! json updated next ctx
                | None ->
                    return! RequestErrors.NOT_FOUND $"Todo with id {id} not found" next ctx
            }

    let deleteTodo (id: int): HttpHandler =
        fun next ctx ->
            let originalCount = List.length todos
            todos <- todos |> List.filter (fun t -> t.Id <> id)
            if List.length todos < originalCount then
                Successful.OK $"Todo with id {id} deleted" next ctx
            else
                RequestErrors.NOT_FOUND $"Todo with id {id} not found" next ctx

    let routes: HttpHandler =
        choose [
            GET >=> choose [
                route "/api/todo_demo/todos" >=> getAllTodos
                routef "/api/todo_demo/todo/%i" getTodoById
            ]
            POST >=> route "/api/todo_demo/todo" >=> createTodo
            PUT >=> routef "/api/todo_demo/todo/%i" updateTodo
            DELETE >=> routef "/api/todo_demo/todo/%i" deleteTodo
        ]
