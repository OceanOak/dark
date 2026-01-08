/// Command registry - maps command names to handlers
/// Maps to Darklang.Cli.Registry
module Cli2.Commands.Registry

open Cli2.Types
open Cli2.Commands.ICommand

// Package commands
open Cli2.Commands.Nav
open Cli2.Commands.Ls
open Cli2.Commands.View
open Cli2.Commands.Tree
open Cli2.Commands.Back
open Cli2.Commands.Search
open Cli2.Commands.Val
open Cli2.Commands.Fn
open Cli2.Commands.Type

// Source Control commands
open Cli2.Commands.Branch
open Cli2.Commands.Instance

// Execution commands
open Cli2.Commands.Run
open Cli2.Commands.Eval
open Cli2.Commands.Scripts

// Installation commands
open Cli2.Commands.Install
open Cli2.Commands.Update
open Cli2.Commands.Uninstall
open Cli2.Commands.Status
open Cli2.Commands.Version

// Utility commands
open Cli2.Commands.Clear
open Cli2.Commands.Help
open Cli2.Commands.Quit
open Cli2.Commands.Config
open Cli2.Commands.Account
open Cli2.Commands.Experiments

/// All registered commands
let allCommands: ICommand list =
  [ // Packages
    Nav.command
    Ls.command
    View.command
    Tree.command
    Back.command
    Search.command
    Val.command
    Fn.command
    Type.command
    // Source Control
    Branch.command
    Instance.command
    // Execution
    Run.command
    Eval.command
    Scripts.command
    // Installation
    Install.command
    Update.command
    Uninstall.command
    Status.command
    Version.command
    // Utilities
    Clear.command
    Help.command
    Quit.command
    Config.command
    Account.command
    Experiments.command ]

/// Command groups for help display
let commandGroups: (string * string list) list =
  [ ("Packages", [ "nav"; "ls"; "view"; "tree"; "back"; "search"; "val"; "fn"; "type" ])
    ("Source Control", [ "branch"; "instance" ])
    ("Execution", [ "run"; "eval"; "scripts" ])
    ("Installation", [ "install"; "update"; "uninstall"; "status"; "version" ])
    ("Utilities", [ "clear"; "help"; "quit"; "config"; "account"; "experiments" ]) ]

/// Find a command by name or alias
let findCommand (name: string) : ICommand option =
  allCommands
  |> List.tryFind (fun cmd ->
    cmd.Name = name || List.contains name cmd.Aliases)

/// Execute a command by name
let executeCommand (name: string) (state: Model) (args: string list) : CommandOutput * CommandResult =
  match findCommand name with
  | Some cmd -> cmd.Execute state args
  | None ->
    let output = CommandOutput.errors [
      $"Unknown command: {name}"
      "Use 'help' to see available commands."
    ]
    (output, StateUpdate state)

/// Get completions for input
let getCompletions (state: Model) (input: string) : string list =
  let parts = input.Split(' ') |> Array.toList

  match parts with
  | [] -> allCommands |> List.map (fun c -> c.Name)
  | [ partial ] ->
    // Completing command name
    let allNames =
      allCommands
      |> List.collect (fun c -> c.Name :: c.Aliases)

    if System.String.IsNullOrEmpty(partial) then
      allNames
    else
      allNames |> List.filter (fun n -> n.StartsWith(partial))
  | commandName :: args ->
    // Completing command arguments
    match findCommand commandName with
    | Some cmd -> cmd.Complete state args
    | None -> []

/// Format command list for help display
let formatDetailedCommandList () : string list =
  commandGroups
  |> List.collect (fun (groupName, cmdNames) ->
    let header = $"{groupName}:"
    let commands =
      cmdNames
      |> List.choose (fun name ->
        findCommand name
        |> Option.map (fun cmd ->
          let aliases =
            if List.isEmpty cmd.Aliases then ""
            else
              let joined = String.concat ", " cmd.Aliases
              $" ({joined})"
          $"  {cmd.Name}{aliases} - {cmd.Description}"))
    header :: commands @ [ "" ])

/// Format compact command list for welcome screen
let formatCompactCommandList () : string list =
  commandGroups
  |> List.map (fun (groupName, cmdNames) ->
    let joined = String.concat ", " cmdNames
    $"{groupName}: {joined}")
