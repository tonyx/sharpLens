module sharpLens.Server.Program

open Microsoft.AspNetCore
open Microsoft.AspNetCore.Authentication.Cookies
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Bolero
open Bolero.Remoting.Server
open Bolero.Server
open Sharpino
open Sharpino.Cache
open Sharpino.Template.Models
open sharpLens
open Bolero.Templating.Server
open Sharpino.EventBroker
open Sharpino.EventBroker
open Sharpino.CommandHandler

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)

    builder.Services.AddRazorComponents().AddInteractiveServerComponents().AddInteractiveWebAssemblyComponents()
    // Sharpino Registrations
    builder.Services.AddSingleton<MessageSenders>(MessageSenders.NoSender) |> ignore

    builder.Services.AddSingleton<Sharpino.Storage.IEventStore<string>>(fun sp ->
        let config =
            sp.GetRequiredService<Microsoft.Extensions.Configuration.IConfiguration>()

        let connString = config["EventStoreConnectionString"]
        // Cast to interface
        Sharpino.PgStorage.PgEventStore connString :> Sharpino.Storage.IEventStore<string>)
    |> ignore

    builder.Services.AddSingleton<Sharpino.Template.TodoManager>(fun sp ->
        // MessageSenders from EventBroker?
        let senders = MessageSenders.NoSender
        let store = sp.GetRequiredService<Sharpino.Storage.IEventStore<string>>()

        let viewer id =
            getAggregateStorageFreshStateViewer<Todo, TodoEvents, string> store id

        new Sharpino.Template.TodoManager(senders, store, viewer))
    |> ignore
#if DEBUG
    builder.Services.AddHotReload(templateDir = __SOURCE_DIRECTORY__ + "/../sharpLens.Client")
    |> ignore
#endif

    builder.Services.AddServerSideBlazor() |> ignore

    builder.Services.AddAuthorization().AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie()
    |> ignore

    builder.Services.AddBoleroRemoting<BookService>() |> ignore
    builder.Services.AddBoleroComponents() |> ignore
#if DEBUG
    builder.Services.AddHotReload(templateDir = __SOURCE_DIRECTORY__ + "/../sharpLens.Client")
    |> ignore
#endif

    let app = builder.Build()

    if app.Environment.IsDevelopment() then
        app.UseWebAssemblyDebugging()

    app.UseAuthentication().UseStaticFiles().UseRouting().UseAuthorization().UseAntiforgery()
    |> ignore

#if DEBUG
    app.UseHotReload()
#endif
    app.MapBoleroRemoting() |> ignore

    app
        .MapRazorComponents<Index.Page>()
        .AddInteractiveServerRenderMode()
        .AddInteractiveWebAssemblyRenderMode()
        .AddAdditionalAssemblies(typeof<Client.Main.MyApp>.Assembly)
    |> ignore

    app.Run()
    0
