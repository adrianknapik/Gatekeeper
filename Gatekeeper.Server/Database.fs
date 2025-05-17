namespace Gatekeeper

open System
open System.Data
open Microsoft.Data.Sqlite
open Dapper
open Gatekeeper.Models

// Database-beli szabály-típus a Dapper mappinghez
[<CLIMutable>]
type DbRule = {
    Id: int64
    ContextSource: string
    Operator: string
    Field: string
    Value: string
    Endpoint: string
    HttpType: string
}

module Database =
    let connectionString = "Data Source=gatekeeper.db;"

    let createConnection () : IDbConnection =
        let conn = new SqliteConnection(connectionString)
        conn.Open()
        conn

    let initDatabase () =
        try
            use conn = createConnection()
            let command = conn.CreateCommand()
            command.CommandText <- """
                CREATE TABLE IF NOT EXISTS Rules (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    ContextSource TEXT NOT NULL,
                    Operator TEXT NOT NULL,
                    Field TEXT NOT NULL,
                    Value TEXT NOT NULL,
                    Endpoint TEXT NOT NULL DEFAULT '',
                    HttpType TEXT NOT NULL DEFAULT ''
                )
            """
            let rowsAffected = command.ExecuteNonQuery()

            // Felhasználók tábla
            let createUsersTable = """
            CREATE TABLE IF NOT EXISTS Users (
                Username     TEXT    PRIMARY KEY,
                PasswordHash TEXT    NOT NULL
            );"""
            use cmdUsers = conn.CreateCommand()
            cmdUsers.CommandText <- createUsersTable
            cmdUsers.ExecuteNonQuery() |> ignore
            with ex ->
                printfn "Failed to initialize database: %s" ex.Message
                reraise()

    let insertRule (rule: Rule) : int =
        try
            let contextSource =
                rule.ContextSource
                |> Option.map (fun cs -> cs.ToString())
                |> Option.defaultValue "JWT"
            let operator =
                rule.Operator
                |> Option.map (fun op -> op.ToString())
                |> Option.defaultValue "Equal"
            let field = rule.Field |> Option.defaultValue ""
            let value = rule.Value |> Option.defaultValue ""
            let endpoint = rule.Endpoint |> Option.defaultValue ""
            let httpType =
                rule.HttpType
                |> Option.map (fun ht -> ht.ToString())
                |> Option.defaultValue ""
            use conn = createConnection()
            let parameters = {|
                ContextSource = contextSource
                Operator = operator
                Field = field
                Value = value
                Endpoint = endpoint
                HttpType = httpType
            |}
            let newId =
                conn.ExecuteScalar<int64>(
                    "INSERT INTO Rules (ContextSource, Operator, Field, Value, Endpoint, HttpType) VALUES (@ContextSource, @Operator, @Field, @Value, @Endpoint, @HttpType); SELECT last_insert_rowid();",
                    parameters
                )
            printfn "Inserted rule with ID: %d" newId
            int newId
        with ex ->
            printfn "Failed to insert rule: %s" ex.Message
            reraise()

    let updateRule (id: int) (rule: Rule) =
        try
            let contextSource =
                rule.ContextSource
                |> Option.map (fun cs -> cs.ToString())
                |> Option.defaultValue "JWT"
            let operator =
                rule.Operator
                |> Option.map (fun op -> op.ToString())
                |> Option.defaultValue "Equal"
            let field = rule.Field |> Option.defaultValue ""
            let value = rule.Value |> Option.defaultValue ""
            let endpoint = rule.Endpoint |> Option.defaultValue ""
            let httpType =
                rule.HttpType
                |> Option.map (fun ht -> ht.ToString())
                |> Option.defaultValue ""
            use conn = createConnection()
            let parameters = {|
                ContextSource = contextSource
                Operator = operator
                Field = field
                Value = value
                Endpoint = endpoint
                HttpType = httpType
                Id = id
            |}
            let rowsAffected =
                conn.Execute(
                    "UPDATE Rules SET ContextSource = @ContextSource, Operator = @Operator, Field = @Field, Value = @Value, Endpoint = @Endpoint, HttpType = @HttpType WHERE Id = @Id;",
                    parameters
                )
            printfn "Updated rule with ID: %d, rows affected: %d" id rowsAffected
            rowsAffected
        with ex ->
            printfn "Failed to update rule: %s" ex.Message
            reraise()

    let deleteRule (id: int) : bool =
        try
            use conn = createConnection()
            let rowsAffected = conn.Execute("DELETE FROM Rules WHERE Id = @Id", {| Id = int64 id |})
            printfn "Delete rule with ID: %d, rows affected: %d" id rowsAffected
            rowsAffected > 0
        with ex ->
            printfn "Failed to delete rule: %s" ex.Message
            reraise()

    let getRules () : Rule list =
        try
            use conn = createConnection()
            let dbRules =
                conn.Query<DbRule>("SELECT Id, ContextSource, Operator, Field, Value, Endpoint, HttpType FROM Rules")
                |> Seq.toList

            printfn "Fetched %d rules from database" dbRules.Length

            dbRules
            |> List.map (fun r ->
                let contextSource =
                    match r.ContextSource with
                    | "JWT"    -> ContextSource.JWT
                    | "Header" -> ContextSource.Header
                    | "Query"  -> ContextSource.Query
                    | _        -> ContextSource.JWT
                let operator =
                    match r.Operator with
                    | "Equal"       -> Operator.Equal
                    | "NotEqual"    -> Operator.NotEqual
                    | "GreaterThan" -> Operator.GreaterThan
                    | "LessThan"    -> Operator.LessThan
                    | _             -> Operator.Equal
                let httpType =
                    match r.HttpType with
                    | "GET"    -> HttpType.GET
                    | "POST"   -> HttpType.POST
                    | "PUT"    -> HttpType.PUT
                    | "PATCH"  -> HttpType.PATCH
                    | "DELETE" -> HttpType.DELETE
                    | _        -> HttpType.GET // Alapértelmezett, ha üres vagy ismeretlen
                { Id = Some (int r.Id)
                  ContextSource = Some contextSource
                  Operator = Some operator
                  Field = Some r.Field
                  Value = Some r.Value
                  Endpoint = Some r.Endpoint
                  HttpType = Some httpType })
        with ex ->
            printfn "Failed to fetch rules: %s" ex.Message
            reraise()

    // Hozzáad egy új felhasználót a Users táblához
    let addUser (username: string) (passwordHash: string) : unit =
        use conn = new SqliteConnection(connectionString)
        conn.Open()
        let cmd = conn.CreateCommand()
        cmd.CommandText <- """
            INSERT INTO Users (Username, PasswordHash)
            VALUES ($username, $passwordHash);
        """
        cmd.Parameters.AddWithValue("$username", username) |> ignore
        cmd.Parameters.AddWithValue("$passwordHash", passwordHash) |> ignore
        cmd.ExecuteNonQuery() |> ignore

    // Lekéri az összes felhasználónevet
    let getAllUsernames () : string list =
        try
            use conn = createConnection()
            let usernames =
                conn.Query<string>("SELECT Username FROM Users")
                |> Seq.toList
            printfn "Fetched %d usernames from database" usernames.Length
            usernames
        with ex ->
            printfn "Failed to fetch usernames: %s" ex.Message
            reraise()

    // Töröl egy felhasználót, ha a felhasználónév és a jelszóhash egyezik
    let deleteUser (username: string) (passwordHash: string) : bool =
        try
            use conn = createConnection()
            let parameters = {|
                Username = username
                PasswordHash = passwordHash
            |}
            let rowsAffected =
                conn.Execute(
                    "DELETE FROM Users WHERE Username = @Username AND PasswordHash = @PasswordHash",
                    parameters
                )
            printfn "Delete user %s, rows affected: %d" username rowsAffected
            rowsAffected > 0
        with ex ->
            printfn "Failed to delete user: %s" ex.Message
            reraise()

    // Ellenőrzi, hogy létezik-e a megadott felhasználónév a táblában
    let userExists (username: string) : bool =
        use conn = new SqliteConnection(connectionString)
        conn.Open()
        let cmd = conn.CreateCommand()
        cmd.CommandText <- """
            SELECT COUNT(1) FROM Users
            WHERE Username = $username;
        """
        cmd.Parameters.AddWithValue("$username", username) |> ignore
        let count = cmd.ExecuteScalar() :?> int64
        count > 0

    // Lekér egy felhasználót (Username és PasswordHash), ha létezik
    let tryGetUser (username: string) : (string * string) option =
        use conn = new SqliteConnection(connectionString)
        conn.Open()
        let cmd = conn.CreateCommand()
        cmd.CommandText <- """
            SELECT Username, PasswordHash FROM Users
            WHERE Username = $username;
        """
        cmd.Parameters.AddWithValue("$username", username) |> ignore
        use reader = cmd.ExecuteReader()
        if reader.Read() then
            let user = reader.GetString(0)
            let hash = reader.GetString(1)
            Some (user, hash)
        else
            None