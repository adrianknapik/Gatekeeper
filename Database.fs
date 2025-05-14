namespace Gatekeeper

open System
open System.Data
open Microsoft.Data.Sqlite

open Dapper
open Gatekeeper.Models
open Newtonsoft.Json

/// Database-beli szabály-típus a Dapper mappinghez
[<CLIMutable>]
type DbRule = {
    Id: int64
    ConditionsJson: string
    LogicalOperator: string
    Action: string
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
                    ConditionsJson TEXT NOT NULL,
                    LogicalOperator TEXT NOT NULL,
                    Action TEXT NOT NULL
                )
            """
            let rowsAffected = command.ExecuteNonQuery()
            printfn "Database initialized, rows affected: %d" rowsAffected
        with ex ->
            printfn "Failed to initialize database: %s" ex.Message
            reraise()

    let insertRule (rule: Rule) : int =
        try
            let conditions = rule.Conditions |> Option.defaultValue []
            let logicalOperator =
                rule.LogicalOperator
                |> Option.map (fun op -> op.ToString())
                |> Option.defaultValue "And"
            let action = rule.Action |> Option.defaultValue "Deny"
            let conditionsJson = JsonConvert.SerializeObject(conditions)
            use conn = createConnection()
            let parameters = {|
                ConditionsJson   = conditionsJson
                LogicalOperator = logicalOperator
                Action           = action
            |}
            let newId =
                conn.ExecuteScalar<int64>(
                    "INSERT INTO Rules (ConditionsJson, LogicalOperator, Action) VALUES (@ConditionsJson, @LogicalOperator, @Action); SELECT last_insert_rowid();",
                    parameters
                )
            printfn "Inserted rule with ID: %d" newId
            int newId
        with ex ->
            printfn "Failed to insert rule: %s" ex.Message
            reraise()

    // Visszatér az érintett sorok számával
    let updateRule (id:int) (rule:Rule) =
        try
            let conditions = rule.Conditions |> Option.defaultValue []
            let logicalOperator =
                rule.LogicalOperator
                |> Option.map (fun op -> op.ToString())
                |> Option.defaultValue "And"
            let action = rule.Action |> Option.defaultValue "Deny"
            let conditionsJson = JsonConvert.SerializeObject(conditions)
            use conn = createConnection()
            let parameters = {|
                ConditionsJson = conditionsJson
                LogicalOperator = logicalOperator
                Action = action
                Id = id
            |}

            let value = conn.ExecuteScalar<int64>(
                    "UPDATE Rules SET ConditionsJson = @ConditionsJson, LogicalOperator = @LogicalOperator, Action = @Action WHERE Id = @Id;",
                    parameters
                )

            printfn "Updated rule with ID: %d" id
            int id
        with ex ->
            printfn "Failed to insert rule: %s" ex.Message
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
                conn.Query<DbRule>("SELECT Id, ConditionsJson, LogicalOperator, Action FROM Rules")
                |> Seq.toList

            printfn "Fetched %d rules from database" dbRules.Length

            dbRules
            |> List.map (fun r ->
                // JSON-ből a listát a custom converterrel
                let settings = JsonConvert.DefaultSettings.Invoke()
                let conditions = JsonConvert.DeserializeObject<RuleCondition list>(r.ConditionsJson, settings)
                let logicalOp =
                    match r.LogicalOperator with
                    | "And" -> LogicalOperator.And
                    | "Or"  -> LogicalOperator.Or
                    | _      -> LogicalOperator.And
                { Id              = Some (int r.Id)
                  Conditions      = Some conditions
                  LogicalOperator = Some logicalOp
                  Action          = Some r.Action })
        with ex ->
            printfn "Failed to fetch rules: %s" ex.Message
            reraise()