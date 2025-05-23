namespace Gatekeeper

open System
open Gatekeeper.Models

module PolicyEngine =
    // Kontextus típus a szabályok kiértékeléséhez
    type EvaluationContext = {
        Claims: Map<string, string>
        Headers: Map<string, string>
        QueryParams: Map<string, string>
        RouteParams: Map<string, string>
        Ip: string
        Timestamp: DateTime
    }

    // Egy szabály kiértékelése a kontextus alapján
    let evaluateRule (rule: Rule) (context: EvaluationContext) : bool =
        let getFieldValue (source: ContextSource) (field: string) =
            match source with
            | ContextSource.JWT -> context.Claims.TryFind field
            | ContextSource.Header -> context.Headers.TryFind field
            | ContextSource.Query -> context.QueryParams.TryFind field
            | _ -> None // Ismeretlen ContextSource esetén None

        match rule.ContextSource, rule.Field, rule.Operator, rule.Value with
        | Some source, Some field, Some operator, Some value ->
            let fieldValue = getFieldValue source field
            printfn "Evaluating rule: ID=%A, Source=%A, Field=%s, Operator=%A, Value=%s, ActualValue=%A"
                rule.Id source field operator value fieldValue
            match fieldValue with
            | Some actualValue ->
                match operator with
                | Operator.Equal -> actualValue = value
                | Operator.NotEqual -> actualValue <> value
                | Operator.GreaterThan ->
                    let fieldNum = Double.TryParse actualValue
                    let valueNum = Double.TryParse value
                    fst fieldNum && fst valueNum && snd fieldNum > snd valueNum
                | Operator.LessThan ->
                    let fieldNum = Double.TryParse actualValue
                    let valueNum = Double.TryParse value
                    fst fieldNum && fst valueNum && snd fieldNum < snd valueNum
                | _ -> false // Ismeretlen Operator esetén false
            | None ->
                printfn "Field %s not found in context for source %A" field source
                false
        | _ ->
            printfn "Invalid rule: Missing required fields (ID=%A)" rule.Id
            false

    // Összes szabály kiértékelése (AND kapcsolat)
    let evaluateRules (rules: Rule list) (context: EvaluationContext) : bool =
        if List.isEmpty rules then
            printfn "No rules matched after filtering, defaulting to deny"
            false
        else
            rules
            |> List.forall (fun rule -> evaluateRule rule context)