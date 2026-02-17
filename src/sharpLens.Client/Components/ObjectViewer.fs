namespace sharpLens.Client.Components

open Bolero
open Bolero.Html
open sharpLens.Client.ReflectionHelper

type ObjectViewer =
    static member Render(obj: obj) =
        let props = getProperties obj

        div {
            attr.``class`` "object-viewer"

            table {
                attr.``class`` "table is-striped is-fullwidth"

                thead {
                    tr {
                        th { text "Property" }
                        th { text "Value" }
                    }
                }

                tbody {
                    forEach props
                    <| fun (name, value) ->
                        tr {
                            td { text name }
                            td { text value }
                        }
                }
            }
        }
