namespace Gatekeeper.Models

open System
open Newtonsoft.Json
open Newtonsoft.Json.Linq
open Newtonsoft.Json.Converters

// Kontextusforrás típusok
[<JsonConverter(typeof<StringEnumConverter>)>]
type ContextSource =
    | JWT = 0
    | Header = 1
    | Query = 2

// Összehasonlító operátorok
[<JsonConverter(typeof<StringEnumConverter>)>]
type Operator =
    | Equal = 0
    | NotEqual = 1
    | GreaterThan = 2
    | LessThan = 3

// HTTP metódus típusok
[<JsonConverter(typeof<StringEnumConverter>)>]
type HttpType =
    | GET = 0
    | POST = 1
    | PUT = 2
    | PATCH = 3
    | DELETE = 4

// Egy egyedi szabály
[<JsonConverter(typeof<RuleConverter>)>]
type Rule = {
    Id: int option
    ContextSource: ContextSource option
    Operator: Operator option
    Field: string option
    Value: string option
    Endpoint: string option
    HttpType: HttpType option
}

// Custom JSON converter a Rule típushoz
and RuleConverter() =
    inherit JsonConverter<Rule>()

    override _.WriteJson(writer: JsonWriter, value: Rule, serializer: JsonSerializer) =
        let jObj = JObject()
        value.Id |> Option.iter (fun id -> jObj.Add("Id", JToken.FromObject(id, serializer)))
        value.ContextSource |> Option.iter (fun cs -> jObj.Add("ContextSource", JToken.FromObject(cs, serializer)))
        value.Operator |> Option.iter (fun op -> jObj.Add("Operator", JToken.FromObject(op, serializer)))
        value.Field |> Option.iter (fun f -> jObj.Add("Field", JToken.FromObject(f, serializer)))
        value.Value |> Option.iter (fun v -> jObj.Add("Value", JToken.FromObject(v, serializer)))
        value.Endpoint |> Option.iter (fun e -> jObj.Add("Endpoint", JToken.FromObject(e, serializer)))
        value.HttpType |> Option.iter (fun ht -> jObj.Add("HttpType", JToken.FromObject(ht, serializer)))
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

        // ContextSource deszerializálás
        let contextSourceOption =
            match jObject.TryGetValue("ContextSource", StringComparison.OrdinalIgnoreCase) with
            | true, token ->
                try Some(token.ToObject<ContextSource>(serializer))
                with _ -> None
            | _ -> None

        // Operator deszerializálás
        let operatorOption =
            match jObject.TryGetValue("Operator", StringComparison.OrdinalIgnoreCase) with
            | true, token ->
                try Some(token.ToObject<Operator>(serializer))
                with _ -> None
            | _ -> None

        // Field deszerializálás
        let fieldOption =
            match jObject.TryGetValue("Field", StringComparison.OrdinalIgnoreCase) with
            | true, token ->
                try Some(token.ToObject<string>(serializer))
                with _ -> None
            | _ -> None

        // Value deszerializálás
        let valueOption =
            match jObject.TryGetValue("Value", StringComparison.OrdinalIgnoreCase) with
            | true, token ->
                try Some(token.ToObject<string>(serializer))
                with _ -> None
            | _ -> None

        // Endpoint deszerializálás
        let endpointOption =
            match jObject.TryGetValue("Endpoint", StringComparison.OrdinalIgnoreCase) with
            | true, token ->
                try Some(token.ToObject<string>(serializer))
                with _ -> None
            | _ -> None

        // HttpType deszerializálás
        let httpTypeOption =
            match jObject.TryGetValue("HttpType", StringComparison.OrdinalIgnoreCase) with
            | true, token ->
                try Some(token.ToObject<HttpType>(serializer))
                with _ -> None
            | _ -> None

        // Visszaadjuk az új Rule rekordot
        { Id = idOption
          ContextSource = contextSourceOption
          Operator = operatorOption
          Field = fieldOption
          Value = valueOption
          Endpoint = endpointOption
          HttpType = httpTypeOption }

    override _.CanWrite = true
    override _.CanRead = true