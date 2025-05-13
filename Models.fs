namespace Gatekeeper.Models

open System
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open Newtonsoft.Json.Converters

/// Logikai operátorok az összevonáshoz
[<JsonConverter(typeof<StringEnumConverter>)>]
type LogicalOperator =
    | And = 0
    | Or = 1

/// Összehasonlító operátorok
[<JsonConverter(typeof<StringEnumConverter>)>]
type Operator =
    | Equal = 0
    | NotEqual = 1
    | GreaterThan = 2
    | LessThan = 3

/// Egy feltétel a szabályban
type RuleCondition = {
    Field: string option
    Operator: Operator option
    Value: string option
}

/// Egy egyedi szabály
[<JsonConverter(typeof<RuleConverter>)>]
type Rule = {
    Id: int option
    Conditions: RuleCondition list option
    LogicalOperator: LogicalOperator option
    Action: string option
}

/// Custom JSON converter a Rule típushoz
and RuleConverter() =
    inherit JsonConverter<Rule>()

    override _.WriteJson(writer: JsonWriter, value: Rule, serializer: JsonSerializer) =
        let jObj = JObject()
        value.Id |> Option.iter (fun id -> jObj.Add("Id", JToken.FromObject(id, serializer)))
        // Conditions mindig írjuk ki, üres listát is
        let condArray =
            value.Conditions
            |> Option.defaultValue []
            |> List.map (fun c ->
                let o = JObject()
                c.Field    |> Option.iter (fun f -> o.Add("Field", JToken.FromObject(f, serializer)))
                c.Operator |> Option.iter (fun op -> o.Add("Operator", JToken.FromObject(op, serializer)))
                c.Value    |> Option.iter (fun v -> o.Add("Value", JToken.FromObject(v, serializer)))
                o)
            |> JArray
        jObj.Add("Conditions", condArray)
        value.LogicalOperator |> Option.iter (fun lo -> jObj.Add("LogicalOperator", JToken.FromObject(lo, serializer)))
        value.Action          |> Option.iter (fun a  -> jObj.Add("Action", JToken.FromObject(a, serializer)))
        jObj.WriteTo(writer)

    override _.ReadJson(reader: JsonReader, _objectType: Type, _existingValue: Rule, _hasExistingValue: bool, serializer: JsonSerializer) =
        let jObject = JObject.Load(reader)
        
        // Id deszerializálás
        let idOption =
            match jObject.TryGetValue("Id", StringComparison.OrdinalIgnoreCase) with
            | true, token ->
                try Some(token.ToObject<int>(serializer))
                with _ -> None
            | _ -> None

        // Conditions kézi parse
        let conditions =
            match jObject.TryGetValue("Conditions", StringComparison.OrdinalIgnoreCase) with
            | true, (:? JArray as arr) ->
                arr
                |> Seq.cast<JToken>
                |> Seq.map (fun child ->
                    let fieldStr = child.Value<string>("Field")
                    let opStr    = child.Value<string>("Operator")
                    let valueStr = child.Value<string>("Value")
                    let op =
                        match opStr with
                        | "Equal"       -> Operator.Equal
                        | "NotEqual"    -> Operator.NotEqual
                        | "GreaterThan" -> Operator.GreaterThan
                        | "LessThan"    -> Operator.LessThan
                        | x -> failwithf "Ismeretlen operátor: %s" x
                    { Field    = Some fieldStr
                      Operator = Some op
                      Value    = Some valueStr })
                |> Seq.toList
            | _ -> []

        // LogicalOperator deszerializálás
        let logicalOpOption =
            match jObject.TryGetValue("LogicalOperator", StringComparison.OrdinalIgnoreCase) with
            | true, token ->
                try Some(token.ToObject<LogicalOperator>(serializer))
                with _ -> None
            | _ -> None

        // Action deszerializálás
        let actionOption =
            match jObject.TryGetValue("Action", StringComparison.OrdinalIgnoreCase) with
            | true, token ->
                try Some(token.ToObject<string>(serializer))
                with _ -> None
            | _ -> None

        // Visszaadjuk az új Rule rekordot
        { Id              = idOption
          Conditions      = Some conditions
          LogicalOperator = logicalOpOption
          Action          = actionOption }

    override _.CanWrite = true
    override _.CanRead  = true
