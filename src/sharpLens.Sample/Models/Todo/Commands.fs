namespace Sharpino.Template.Models

open Sharpino.Template.Models
open Sharpino.Template.Commons
open Sharpino.Core
open System.Text.Json
open FsToolkit.ErrorHandling
open System

type TodoCommands =
    | Activate of DateTime
    | Complete of DateTime
    | Comment of DateTime * string

    interface AggregateCommand<Todo, TodoEvents> with
        member this.Execute todo =
            match this with
            | Activate dateTime -> todo.Activate dateTime |> Result.map (fun s -> (s, [ Activated dateTime ]))

            | Complete dateTime -> todo.Complete dateTime |> Result.map (fun s -> (s, [ Completed dateTime ]))

            | Comment(dateTime, comment) ->
                todo.Comment(dateTime, comment)
                |> Result.map (fun s -> (s, [ Commented(dateTime, comment) ]))

        member this.Undoer = None
