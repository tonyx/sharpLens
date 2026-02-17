#r "nuget: Sharpino, 4.7.5"
#r "nuget: FsToolkit.ErrorHandling"
#r "nuget: FSharp.SystemTextJson"

open System

try
    // Load assembly by accessing a known type
    let t = typeof<Sharpino.Core.Aggregate<_>>
    let asm = t.Assembly

    printfn "Scanning assembly: %s" asm.FullName

    let findTypes (pattern: string) =
        asm.GetTypes()
        |> Array.filter (fun t -> (t.Name: string).IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0)
        |> Array.iter (fun t -> printfn "Found: %s (Namespace: %s)" t.Name t.Namespace)

    printfn "--- MessageSenders ---"
    findTypes "MessageSender"

    printfn "--- PgStorage ---"
    findTypes "PgStorage"

    printfn "--- StateViewer ---"
    findTypes "StateViewer"

    printfn "--- Storage ---"
    findTypes "Storage"

    printfn "--- Cache ---"
    findTypes "Cache"

with ex ->
    printfn "Error: %A" ex
