module sharpLens.Client.Main

open System
open Elmish
open Bolero
open Bolero.Html
open Bolero.Remoting
open Bolero.Remoting.Client
open Bolero.Templating.Client
open sharpLens.Client.Components


/// Routing endpoints definition.
type Page =
    | [<EndPoint "/">] Home
    | [<EndPoint "/data">] BooksData
    | [<EndPoint "/counter">] Counter
    | [<EndPoint "/todo">] Todo
    | [<EndPoint "/events/{id}">] EventBrowser of id: int
    | [<EndPoint "/create-todo">] CreateTodo

/// The Elmish application's model.
type Model =
    { page: Page
      counter: int
      books: Book[] option
      error: string option
      username: string
      password: string
      signedInAs: option<string>
      signInFailed: bool
      // Add a dummy todo for demonstration
      todo: Sharpino.Template.Models.Todo
      todos: Sharpino.Template.Models.Todo list option
      selectedTodo: Sharpino.Template.Models.Todo option
      todoEvents: (Guid * Sharpino.Template.Models.Todo option * Sharpino.Template.Models.TodoEvents[]) option
      currentEventJson: string option }

and Book =
    { title: string
      author: string
      publishDate: DateTime
      isbn: string }

let initModel =
    { page = Home
      counter = 0
      books = None
      error = None
      username = ""
      password = ""
      signedInAs = None
      signInFailed = false
      todo = Sharpino.Template.Models.Todo.New "Learn Bolero"
      todos = None
      selectedTodo = None
      todoEvents = None
      currentEventJson = None }

/// Remote service definition.
type BookService =
    {
        /// Get the list of all books in the collection.
        getBooks: unit -> Async<Book[]>

        /// Add a book in the collection.
        addBook: Book -> Async<unit>

        /// Remove a book from the collection, identified by its ISBN.
        removeBookByIsbn: string -> Async<unit>

        /// Sign into the application.
        signIn: string * string -> Async<option<string>>

        /// Get the user's name, or None if they are not authenticated.
        getUsername: unit -> Async<string>

        /// Sign out from the application.
        signOut: unit -> Async<unit>

        /// Get an event by ID.
        getEvent: int -> Async<string option>

        /// Add a todo
        addTodo: Sharpino.Template.Models.Todo -> Async<unit>

        /// Get all todos
        getTodos: unit -> Async<Sharpino.Template.Models.Todo[]>

        /// Execute a command on a todo
        executeTodoCommand: Guid * Sharpino.Template.Models.TodoCommands -> Async<Result<unit, string>>

        /// Get events for a todo (Snapshot Json Option, Events)
        getTodoEvents: Guid -> Async<string option * Sharpino.Template.Models.TodoEvents[]>
    }

    interface IRemoteService with
        member this.BasePath = "/books"

/// The Elmish application's update messages.
type Message =
    | SetPage of Page
    | Increment
    | Decrement
    | SetCounter of int
    | GetBooks
    | GotBooks of Book[]
    | SetUsername of string
    | SetPassword of string
    | ClearLoginForm
    | GetSignedInAs
    | RecvSignedInAs of option<string>
    | SendSignIn
    | RecvSignIn of option<string>
    | SendSignOut
    | RecvSignOut
    | Error of exn
    | ClearError
    | GetEvent of int
    | GotEvent of string option
    | AddTodo of obj // Receive as obj from ObjectCreator, cast inside update if needed
    | TodoAdded
    | GetTodosList
    | GotTodosList of Sharpino.Template.Models.Todo[]
    | SelectTodo of Sharpino.Template.Models.Todo
    | ExecuteTodoCommand of obj
    | TodoCommandExecuted of Result<unit, string>
    | GetTodoEvents of Guid
    | GotTodoEvents of Guid * string option * Sharpino.Template.Models.TodoEvents[]

let update remote message model =
    let onSignIn =
        function
        | Some _ -> Cmd.batch [ Cmd.ofMsg GetBooks; Cmd.ofMsg ClearLoginForm ]
        | None -> Cmd.none

    match message with
    | SetPage page ->
        let cmd =
            match page with
            | EventBrowser id -> Cmd.ofMsg (GetEvent id)
            | Todo -> Cmd.ofMsg GetTodosList
            | _ -> Cmd.none

        { model with page = page }, cmd

    | Increment ->
        { model with
            counter = model.counter + 1 },
        Cmd.none
    | Decrement ->
        { model with
            counter = model.counter - 1 },
        Cmd.none
    | SetCounter value -> { model with counter = value }, Cmd.none

    | GetBooks ->
        let cmd = Cmd.OfAsync.either remote.getBooks () GotBooks Error
        { model with books = None }, cmd
    | GotBooks books -> { model with books = Some books }, Cmd.none

    | SetUsername s -> { model with username = s }, Cmd.none
    | SetPassword s -> { model with password = s }, Cmd.none
    | ClearLoginForm ->
        { model with
            username = ""
            password = "" },
        Cmd.none
    | GetSignedInAs -> model, Cmd.OfAuthorized.either remote.getUsername () RecvSignedInAs Error
    | RecvSignedInAs username -> { model with signedInAs = username }, onSignIn username
    | SendSignIn -> model, Cmd.OfAsync.either remote.signIn (model.username, model.password) RecvSignIn Error
    | RecvSignIn username ->
        { model with
            signedInAs = username
            signInFailed = Option.isNone username },
        onSignIn username
    | SendSignOut -> model, Cmd.OfAsync.either remote.signOut () (fun () -> RecvSignOut) Error
    | RecvSignOut ->
        { model with
            signedInAs = None
            signInFailed = false },
        Cmd.none

    | Error RemoteUnauthorizedException ->
        { model with
            error = Some "You have been logged out."
            signedInAs = None },
        Cmd.none
    | Error exn -> { model with error = Some exn.Message }, Cmd.none
    | ClearError -> { model with error = None }, Cmd.none

    | GetEvent id ->
        let cmd = Cmd.OfAsync.either remote.getEvent id GotEvent Error
        { model with currentEventJson = None }, cmd
    | GotEvent json -> { model with currentEventJson = json }, Cmd.none

    | AddTodo todoObj ->
        let todo = todoObj :?> Sharpino.Template.Models.Todo
        let cmd = Cmd.OfAsync.perform remote.addTodo todo (fun () -> TodoAdded)
        model, cmd
    | TodoAdded -> { model with page = Todo }, Cmd.ofMsg GetTodosList

    | GetTodosList ->
        let cmd = Cmd.OfAsync.either remote.getTodos () GotTodosList Error
        model, cmd

    | GotTodosList todos ->
        { model with
            todos = Some(Array.toList todos) },
        Cmd.none

    | SelectTodo todo ->
        { model with
            selectedTodo = Some todo
            todoEvents = None },
        Cmd.none

    | ExecuteTodoCommand commandObj ->
        let command = commandObj :?> Sharpino.Template.Models.TodoCommands

        match model.selectedTodo with
        | Some todo ->
            let cmd =
                Cmd.OfAsync.perform remote.executeTodoCommand (todo.TodoId.Value, command) TodoCommandExecuted

            { model with selectedTodo = None }, cmd
        | None -> model, Cmd.none

    | TodoCommandExecuted result ->
        match result with
        | Ok() -> { model with error = None }, Cmd.ofMsg GetTodosList
        | Result.Error msg -> { model with error = Some msg }, Cmd.none

    | GetTodoEvents id ->
        let cmd =
            Cmd.OfAsync.perform remote.getTodoEvents id (fun (snap, events) -> GotTodoEvents(id, snap, events))

        { model with
            selectedTodo = None
            todoEvents = None },
        cmd

    | GotTodoEvents(id, snapshotJson, events) ->
        let snapshot =
            match snapshotJson with
            | Some json ->
                match Sharpino.Template.Models.Todo.Deserialize json with
                | Ok s -> Some s
                | _ -> None
            | None -> None

        { model with
            todoEvents = Some(id, snapshot, events) },
        Cmd.none

/// Connects the routing system to the Elmish application.
let router = Router.infer SetPage (fun model -> model.page)

type Main = Template<"wwwroot/main.html">

let homePage model dispatch = Main.Home().Elt()

let counterPage model dispatch =
    Main
        .Counter()
        .Decrement(fun _ -> dispatch Decrement)
        .Increment(fun _ -> dispatch Increment)
        .Value(model.counter, fun v -> dispatch (SetCounter v))
        .Elt()

let dataPage model (username: string) dispatch =
    Main
        .Data()
        .Reload(fun _ -> dispatch GetBooks)
        .Username(username)
        .SignOut(fun _ -> dispatch SendSignOut)
        .Rows(
            cond model.books
            <| function
                | None -> Main.EmptyData().Elt()
                | Some books ->
                    forEach books
                    <| fun book ->
                        tr {
                            td { book.title }
                            td { book.author }
                            td { book.publishDate.ToString("yyyy-MM-dd") }
                            td { book.isbn }
                        }
        )
        .Elt()

let signInPage model dispatch =
    Main
        .SignIn()
        .Username(model.username, fun s -> dispatch (SetUsername s))
        .Password(model.password, fun s -> dispatch (SetPassword s))
        .SignIn(fun _ -> dispatch SendSignIn)
        .ErrorNotification(
            cond model.signInFailed
            <| function
                | false -> empty ()
                | true ->
                    Main
                        .ErrorNotification()
                        .HideClass("is-hidden")
                        .Text("Sign in failed. Use any username and the password \"password\".")
                        .Elt()
        )
        .Elt()

let todoPage (js: Microsoft.JSInterop.IJSRuntime) model dispatch =
    div {
        h1 { "Todos" }

        button {
            attr.``class`` "button is-primary"
            on.click (fun _ -> dispatch GetTodosList)
            "Refresh"
        }

        div {
            attr.``class`` "columns"

            div {
                attr.``class`` "column"

                cond model.todos
                <| function
                    | None -> p { "Loading..." }
                    | Some todos ->
                        ObjectBrowser.Render(
                            todos |> List.map (fun x -> x :> obj),
                            [ "Command", (fun obj -> dispatch (SelectTodo(obj :?> Sharpino.Template.Models.Todo)))
                              "History",
                              (fun obj ->
                                  let todo = obj :?> Sharpino.Template.Models.Todo
                                  dispatch (GetTodoEvents todo.TodoId.Value)) ]
                        )
            }

            div {
                attr.``class`` "column"

                cond model.selectedTodo
                <| function
                    | None ->
                        cond model.todoEvents
                        <| function
                            | None -> empty ()
                            | Some(id, snapshot, events) ->
                                div {
                                    h2 { "Event History" }

                                    match snapshot with
                                    | Some s ->
                                        div {
                                            h4 { "Initial Snapshot" }
                                            ObjectBrowser.Render([ s :> obj ], [])
                                            hr { }
                                        }
                                    | None -> empty ()

                                    if events.Length = 0 then
                                        p { "No events found." }
                                    else
                                        h4 { "Events" }
                                        ObjectBrowser.Render(events |> Array.map (fun x -> x :> obj), [])
                                }
                    | Some todo ->
                        div {
                            h2 { sprintf "Selected: %s" todo.Text }

                            comp<CommandExecutor> {
                                "CommandType" => typeof<Sharpino.Template.Models.TodoCommands>
                                "JSRuntime" => js
                                "OnSubmit" => (fun cmd -> dispatch (ExecuteTodoCommand cmd))
                            }
                        }
            }
        }
    }

let eventBrowserPage model (id: int) dispatch =
    div {
        h1 { "Event Browser" }

        div {
            attr.``class`` "buttons"

            button {
                attr.``class`` "button"
                on.click (fun _ -> dispatch (SetPage(EventBrowser(max 1 (id - 1)))))
                "Previous"
            }

            button {
                attr.``class`` "button"
                on.click (fun _ -> dispatch (SetPage(EventBrowser(id + 1))))
                "Next"
            }
        }

        h2 { sprintf "Event ID: %d" id }

        cond model.currentEventJson
        <| function
            | Some json -> pre { code { json } }
            | None -> p { "Event not found or loading..." }
    }

let menuItem (model: Model) (page: Page) (text: string) =
    Main.MenuItem().Active(if model.page = page then "is-active" else "").Url(router.Link page).Text(text).Elt()

let view (js: Microsoft.JSInterop.IJSRuntime) model dispatch =
    Main()
        .Menu(
            concat {
                menuItem model Home "Home"
                menuItem model Todo "Todo"
                menuItem model CreateTodo "Create Todo"
            }
        )
        .Body(
            cond model.page
            <| function
                | Home -> homePage model dispatch
                | Counter -> counterPage model dispatch
                | BooksData ->
                    cond model.signedInAs
                    <| function
                        | Some username -> dataPage model username dispatch
                        | None -> signInPage model dispatch
                | Todo -> todoPage js model dispatch
                | EventBrowser id -> eventBrowserPage model id dispatch
                | CreateTodo ->
                    div {
                        h1 { "Create Todo" }

                        comp<ObjectCreator> {
                            "Type" => typeof<Sharpino.Template.Models.Todo>
                            "JSRuntime" => js
                            "OnSubmit" => (fun obj -> dispatch (AddTodo obj))
                        }
                    }
        )
        .Error(
            cond model.error
            <| function
                | None -> empty ()
                | Some err -> Main.ErrorNotification().Text(err).Hide(fun _ -> dispatch ClearError).Elt()
        )
        .Elt()

type MyApp() =
    inherit ProgramComponent<Model, Message>()

    override _.CssScope = CssScopes.MyApp

    override this.Program =
        let bookService = this.Remote<BookService>()
        let update = update bookService
        let view = view this.JSRuntime

        Program.mkProgram (fun _ -> initModel, Cmd.ofMsg GetSignedInAs) update view
        |> Program.withRouter router
#if DEBUG
        |> Program.withHotReload
#endif
