namespace sharpLens.Server

open System
open System.IO
open System.Text.Json
open System.Text.Json.Serialization
open Microsoft.AspNetCore.Hosting
open Bolero
open Bolero.Remoting
open Bolero.Remoting.Server
open sharpLens

type BookService(ctx: IRemoteContext, env: IWebHostEnvironment, todoManager: Sharpino.Template.TodoManager) =
    inherit RemoteHandler<Client.Main.BookService>()

    let books =
        let json = Path.Combine(env.ContentRootPath, "data/books.json") |> File.ReadAllText
        JsonSerializer.Deserialize<Client.Main.Book[]>(json) |> ResizeArray

    override this.Handler =
        { getBooks = ctx.Authorize <| fun () -> async { return books.ToArray() }

          addBook = ctx.Authorize <| fun book -> async { books.Add(book) }

          removeBookByIsbn =
            ctx.Authorize
            <| fun isbn -> async { books.RemoveAll(fun b -> b.isbn = isbn) |> ignore }

          signIn =
            fun (username, password) ->
                async {
                    if password = "password" then
                        do! ctx.HttpContext.AsyncSignIn(username, TimeSpan.FromDays(365.))
                        return Some username
                    else
                        return None
                }

          signOut = fun () -> async { return! ctx.HttpContext.AsyncSignOut() }

          getUsername = ctx.Authorize <| fun () -> async { return ctx.HttpContext.User.Identity.Name }

          getEvent =
            fun eventId ->
                async {
                    let connectionString =
                        ctx.HttpContext.RequestServices.GetService(
                            typeof<Microsoft.Extensions.Configuration.IConfiguration>
                        )
                        :?> Microsoft.Extensions.Configuration.IConfiguration
                        |> fun c -> c["EventStoreConnectionString"]

                    use conn = new Npgsql.NpgsqlConnection(connectionString)
                    do! conn.OpenAsync() |> Async.AwaitTask

                    use cmd =
                        new Npgsql.NpgsqlCommand("SELECT event FROM events_01_Todo WHERE id = @id", conn)

                    cmd.Parameters.AddWithValue("id", eventId) |> ignore
                    use! reader = cmd.ExecuteReaderAsync() |> Async.AwaitTask

                    if reader.Read() then
                        return Some(reader.GetString(0))
                    else
                        return None
                }

          addTodo =
            fun (todo: Sharpino.Template.Models.Todo) ->
                async {
                    match todoManager.AddTodo todo with
                    | Ok _ -> return ()
                    | Error e -> failwithf "Failed to add todo: %A" e
                }
          getTodos =
            fun () ->
                async {
                    let! todos = todoManager.GetTodosAsync() |> Async.AwaitTask

                    match todos with
                    | Ok t -> return t |> List.toArray
                    | Error e -> return failwithf "Failed to get todos: %A" e
                }

          executeTodoCommand =
            fun (id: Guid, command: Sharpino.Template.Models.TodoCommands) ->
                async {
                    match todoManager.ExecuteCommand (Sharpino.Template.Commons.TodoId id) command with
                    | Ok _ -> return Ok()
                    | Error e -> return Error(sprintf "Failed to execute command: %A" e)
                }

          getTodoEvents =
            fun (id: Guid) ->
                async {
                    let connectionString =
                        ctx.HttpContext.RequestServices.GetService(
                            typeof<Microsoft.Extensions.Configuration.IConfiguration>
                        )
                        :?> Microsoft.Extensions.Configuration.IConfiguration
                        |> fun c -> c["EventStoreConnectionString"]

                    use conn = new Npgsql.NpgsqlConnection(connectionString)
                    do! conn.OpenAsync() |> Async.AwaitTask

                    // Fetch Snapshot (limit 1 to get the first one)
                    use cmdSnapshot =
                        new Npgsql.NpgsqlCommand(
                            "SELECT snapshot FROM snapshots_01_Todo WHERE aggregate_id = @id ORDER BY id ASC LIMIT 1",
                            conn
                        )

                    cmdSnapshot.Parameters.AddWithValue("id", id) |> ignore
                    use! readerSnapshot = cmdSnapshot.ExecuteReaderAsync() |> Async.AwaitTask

                    let snapshotJson =
                        if readerSnapshot.Read() then
                            Some(readerSnapshot.GetString(0))
                        else
                            None

                    readerSnapshot.Close()

                    // Fetch Events
                    use cmd =
                        new Npgsql.NpgsqlCommand(
                            "SELECT event FROM events_01_Todo WHERE aggregate_id = @id ORDER BY id ASC",
                            conn
                        )

                    cmd.Parameters.AddWithValue("id", id) |> ignore
                    use! reader = cmd.ExecuteReaderAsync() |> Async.AwaitTask

                    let events = ResizeArray<Sharpino.Template.Models.TodoEvents>()

                    while reader.Read() do
                        let json = reader.GetString(0)

                        match Sharpino.Template.Models.TodoEvents.Deserialize json with
                        | Ok e -> events.Add(e)
                        | Error _ -> () // skip or handle error?

                    return (snapshotJson, events.ToArray())
                } }
