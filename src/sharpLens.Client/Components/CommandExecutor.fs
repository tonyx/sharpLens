namespace sharpLens.Client.Components

open System
open System.Collections.Generic
open Bolero
open Bolero.Html
open sharpLens.Client.ReflectionHelper
open Microsoft.FSharp.Reflection
open Microsoft.JSInterop
open Microsoft.AspNetCore.Components

type CommandExecutor() =
    inherit Bolero.Component()

    [<Parameter>]
    member val CommandType = Unchecked.defaultof<Type> with get, set

    [<Parameter>]
    member val JSRuntime = Unchecked.defaultof<IJSRuntime> with get, set

    [<Parameter>]
    member val OnSubmit = Unchecked.defaultof<obj -> unit> with get, set

    member val CaseValues = Dictionary<string, obj[]>() with get, set

    override this.Render() =
        let t = this.CommandType
        let js = this.JSRuntime
        let onSubmit = this.OnSubmit
        let cases = getUnionCases t

        div {
            attr.``class`` "command-executor"
            h3 { "Execute Command" }

            if cases.IsEmpty then
                p { "No commands found." }
            else
                forEach cases
                <| fun (caseInfo, fields) ->
                    // Ensure values array exists for this case
                    if not (this.CaseValues.ContainsKey(caseInfo.Name)) then
                        let initialValues =
                            fields
                            |> Array.map (fun (_, fType) ->
                                if fType = typeof<string> then
                                    "" :> obj
                                else if fType = typeof<DateTime> then
                                    DateTime.Now :> obj
                                else if fType.IsValueType then
                                    Activator.CreateInstance(fType)
                                else
                                    null)

                        this.CaseValues.[caseInfo.Name] <- initialValues

                    let values = this.CaseValues.[caseInfo.Name]

                    div {
                        attr.``class`` "box command-box"
                        h4 { caseInfo.Name }

                        let inputs =
                            fields
                            |> Array.mapi (fun i (name, paramType) ->
                                div {
                                    attr.``class`` "field"

                                    label {
                                        attr.``class`` "label"
                                        text name
                                    }

                                    div {
                                        attr.``class`` "control"

                                        if paramType = typeof<DateTime> then
                                            input {
                                                attr.``class`` "input"
                                                attr.``type`` "datetime-local"
                                                // simplistic date binding
                                                on.change (fun e ->
                                                    match DateTime.TryParse(e.Value :?> string) with
                                                    | true, d -> values.[i] <- d :> obj
                                                    | _ -> ())
                                            }
                                        else
                                            input {
                                                attr.``class`` "input"
                                                attr.``type`` "text"
                                                // Combine both for robustness and logging
                                                on.change (fun e ->
                                                    Console.WriteLine(sprintf "Change[%d]: %A" i e.Value)
                                                    values.[i] <- e.Value :> obj)

                                                on.input (fun e ->
                                                    Console.WriteLine(sprintf "Input[%d]: %A" i e.Value)
                                                    values.[i] <- e.Value :> obj)
                                            }
                                    }
                                })
                            |> Array.toList

                        forEach inputs id

                        div {
                            attr.``class`` "control"

                            button {
                                attr.``class`` "button is-info"

                                on.click (fun _ ->
                                    try
                                        Console.WriteLine(sprintf "Executing command: %s" caseInfo.Name)

                                        values
                                        |> Array.iteri (fun i v -> Console.WriteLine(sprintf "Value[%d]: %A" i v))

                                        let command = FSharpValue.MakeUnion(caseInfo, values)
                                        onSubmit command
                                    with ex ->
                                        js.InvokeVoidAsync("alert", sprintf "Error executing command: %s" ex.Message)
                                        |> ignore)

                                text "Execute"
                            }
                        }
                    }
        }
