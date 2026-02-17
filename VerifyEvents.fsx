#r "nuget: Npgsql"

open System
open Npgsql

let connString = "Host=127.0.0.1;Port=5434;Database=sharpino;User Id=sharpino;Password=password"

async {
    try
        use conn = new NpgsqlConnection(connString)
        do! conn.OpenAsync() |> Async.AwaitTask
        printfn "Connected to database."

        use cmdCount = new NpgsqlCommand("SELECT COUNT(*) FROM events_01_Todo", conn)
        let! countObj = cmdCount.ExecuteScalarAsync() |> Async.AwaitTask
        let count = countObj :?> int64
        printfn "Total events before insert: %d" count

        if count = 0L then
            printfn "Table is empty. Inserting a sample event..."
            let aggId = Guid.NewGuid()
            let eventJson = """{"Case":"TodoAdded","Fields":[{"id":"1","description":"Test Event"}]}"""
            
            use cmdInsert = new NpgsqlCommand("INSERT INTO events_01_Todo (aggregate_id, event, timestamp) VALUES (@aggId, @event, @ts) RETURNING id", conn)
            cmdInsert.Parameters.AddWithValue("aggId", aggId) |> ignore
            cmdInsert.Parameters.AddWithValue("event", eventJson) |> ignore
            cmdInsert.Parameters.AddWithValue("ts", DateTime.Now) |> ignore
            
            let! newId = cmdInsert.ExecuteScalarAsync() |> Async.AwaitTask
            printfn "Inserted event with ID: %O" newId
        
        // Now try to read it back
        use cmdFirst = new NpgsqlCommand("SELECT id, event FROM events_01_Todo ORDER BY id LIMIT 1", conn)
        use! reader = cmdFirst.ExecuteReaderAsync() |> Async.AwaitTask
        if reader.Read() then
            let id = reader.GetInt32(0)
            let eventContent = reader.GetString(1) // Verifying it is read as string
            printfn "Successfully read Event ID: %d" id
            printfn "Event Content: %s" eventContent
        else
            printfn "Could not read event back."
            
    with ex ->
        printfn "Error: %A" ex
}
|> Async.RunSynchronously
