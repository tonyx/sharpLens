namespace sharpLens.Client.Components

open Bolero
open Bolero.Html
open sharpLens.Client.ReflectionHelper

type ObjectBrowser =
    static member Render(objects: seq<obj>, actions: (string * (obj -> unit)) list) =
        let objectsList = objects |> Seq.toList

        if objectsList.IsEmpty then
            div { "No items to display." }
        else
            let firstObj = objectsList.Head
            let props = getProperties firstObj |> List.map fst
            let hasActions = not actions.IsEmpty

            div {
                attr.``class`` "object-browser"

                table {
                    attr.``class`` "table is-striped is-fullwidth is-hoverable"

                    thead {
                        tr {
                            forEach props <| fun prop -> th { text prop }

                            if hasActions then
                                th { "Actions" }
                        }
                    }

                    tbody {
                        forEach objectsList
                        <| fun obj ->
                            let objProps = getProperties obj

                            tr {
                                forEach objProps <| fun (_, value) -> td { text value }

                                if hasActions then
                                    td {
                                        div {
                                            attr.``class`` "buttons"

                                            forEach actions
                                            <| fun (label, action) ->
                                                button {
                                                    attr.``class`` "button is-small is-info"
                                                    on.click (fun _ -> action obj)
                                                    text label
                                                }
                                        }
                                    }
                            }
                    }
                }
            }
