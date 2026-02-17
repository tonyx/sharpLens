namespace sharpLens.Client.Components

open System
open System.Collections.Generic
open Bolero
open Bolero.Html
open sharpLens.Client.ReflectionHelper
open Microsoft.JSInterop
open Microsoft.AspNetCore.Components

type ObjectCreator() =
    inherit Bolero.Component()

    [<Parameter>]
    member val Type = Unchecked.defaultof<Type> with get, set

    [<Parameter>]
    member val JSRuntime = Unchecked.defaultof<IJSRuntime> with get, set

    [<Parameter>]
    member val OnSubmit = Unchecked.defaultof<obj -> unit> with get, set

    member val TypeValues = Dictionary<string, obj[]>() with get, set

    override this.Render() =
        let t = this.Type
        let js = this.JSRuntime
        let onSubmit = this.OnSubmit

        match getConstructorDetails t with
        | Some(methodInfo, paramsDetails) ->
            // Ensure values array exists for this type
            if not (this.TypeValues.ContainsKey(t.FullName)) then
                let initialValues =
                    paramsDetails
                    |> List.map (fun (_, t) ->
                        if t = typeof<string> then "" :> obj
                        else if t.IsValueType then Activator.CreateInstance(t)
                        else null)
                    |> List.toArray
                this.TypeValues.[t.FullName] <- initialValues

            let values = this.TypeValues.[t.FullName]

            let inputs =
                paramsDetails
                |> List.mapi (fun i (name, paramType) ->
                    div {
                        attr.``class`` "field"

                        label {
                            attr.``class`` "label"
                            text name
                        }

                        div {
                            attr.``class`` "control"

                            input {
                                attr.``class`` "input"
                                attr.``type`` "text"
                                
                                on.change (fun e ->
                                    Console.WriteLine(sprintf "Creator Change[%d]: %A" i e.Value)
                                    values.[i] <- e.Value :> obj)

                                on.input (fun e ->
                                    Console.WriteLine(sprintf "Creator Input[%d]: %A" i e.Value)
                                    values.[i] <- e.Value :> obj)
                            }
                        }
                    })

            div {
                attr.``class`` "box"

                h2 {
                    attr.``class`` "title is-4"
                    text (sprintf "Create %s" t.Name)
                }

                forEach inputs id

                div {
                    attr.``class`` "field"

                    div {
                        attr.``class`` "control"

                        button {
                            attr.``class`` "button is-primary"

                            on.click (fun _ ->
                                try
                                    let instance = invokeConstructor methodInfo values
                                    onSubmit instance
                                with ex ->
                                    js.InvokeVoidAsync("alert", sprintf "Error creating object: %s" ex.Message)
                                    |> ignore)

                            text "Submit"
                        }
                    }
                }
            }
        | None -> div { text (sprintf "No 'New' method found for %s" t.Name) }
