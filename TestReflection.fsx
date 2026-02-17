open System
open Microsoft.FSharp.Reflection

type TodoCommands =
    | Activate of DateTime
    | Complete of DateTime
    | Comment of DateTime * string

let t = typeof<TodoCommands>
let cases = FSharpType.GetUnionCases t

for case in cases do
    printfn "Case: %s" case.Name
    let fields = case.GetFields()

    for f in fields do
        printfn "  Field: %s, Type: %s" f.Name f.PropertyType.Name

    if case.Name = "Comment" then
        // Simulate CommandExecutor
        let values: obj[] = Array.zeroCreate fields.Length
        // assume values[0] is DateTime, values[1] is String
        if fields.Length = 2 then
            values.[0] <- DateTime.Now :> obj
            values.[1] <- "Test Comment" :> obj

            try
                let instance = FSharpValue.MakeUnion(case, values)
                printfn "Created instance: %A" instance

                match instance :?> TodoCommands with
                | Comment(dt, txt) -> printfn "Matched: %A, %s" dt txt
                | _ -> printfn "Matched something else"
            with ex ->
                printfn "Error making union: %A" ex
        else
            printfn "Unexpected field count: %d" fields.Length
