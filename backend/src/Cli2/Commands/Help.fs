/// Help command implementation
module Cli2.Commands.Help

open Cli2.Types
open Cli2.Commands.ICommand

type HelpCommand() =
  inherit CommandBase()

  override _.Name = "help"
  override _.Description = "Show help for commands"
  override _.Aliases = [ "?"; "commands" ]

  override _.Execute state args =
    match args with
    | [] ->
      // Show general help - will be filled in by Registry
      let output = CommandOutput.line "Use 'help <command>' for detailed help on a specific command."
      (output, StateUpdate state)
    | [ commandName ] ->
      // Show help for specific command - delegate to Registry
      let output = CommandOutput.line $"Help for '{commandName}' (TODO: implement)"
      (output, StateUpdate state)
    | _ ->
      let output = CommandOutput.error "Usage: help [command]"
      (output, StateUpdate state)

  override _.Complete _state args =
    match args with
    | [] -> [] // Return command names - filled by Registry
    | [ _partial ] -> [] // Filter command names by partial
    | _ -> []

  override _.Help() =
    [ "help - Show help for commands"
      ""
      "Usage:"
      "  help           Show list of all commands"
      "  help <command> Show detailed help for a specific command"
      ""
      "Aliases: ?, commands" ]

let command = HelpCommand() :> ICommand
