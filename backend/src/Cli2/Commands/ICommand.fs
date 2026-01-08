/// Command interface and base types
/// Maps to Darklang.Cli.Registry.CommandHandler
module Cli2.Commands.ICommand

open Cli2.Types

/// Output from a command
type CommandOutput =
  { Lines: string list
    IsError: bool }

module CommandOutput =
  let empty = { Lines = []; IsError = false }
  let line s = { Lines = [ s ]; IsError = false }
  let lines ls = { Lines = ls; IsError = false }
  let error s = { Lines = [ s ]; IsError = true }
  let errors ls = { Lines = ls; IsError = true }

/// Result of executing a command
type CommandResult =
  | StateUpdate of Model
  | Exit of exitCode: int

/// Interface for CLI commands
type ICommand =
  /// The primary name of the command
  abstract member Name: string

  /// Short description for help listings
  abstract member Description: string

  /// Alternative names for this command
  abstract member Aliases: string list

  /// Execute the command with the given state and arguments
  abstract member Execute: state: Model -> args: string list -> CommandOutput * CommandResult

  /// Get tab completions for the given partial arguments
  abstract member Complete: state: Model -> args: string list -> string list

  /// Get detailed help text
  abstract member Help: unit -> string list

/// Base class with default implementations
[<AbstractClass>]
type CommandBase() =
  abstract member Name: string
  abstract member Description: string

  abstract member Aliases: string list
  default _.Aliases = []

  abstract member Execute: Model -> string list -> CommandOutput * CommandResult

  abstract member Complete: Model -> string list -> string list
  default _.Complete _ _ = []

  abstract member Help: unit -> string list
  default x.Help() = [ $"{x.Name} - {x.Description}" ]

  interface ICommand with
    member x.Name = x.Name
    member x.Description = x.Description
    member x.Aliases = x.Aliases
    member x.Execute state args = x.Execute state args
    member x.Complete state args = x.Complete state args
    member x.Help() = x.Help()
