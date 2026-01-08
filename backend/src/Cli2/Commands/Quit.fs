/// Quit command implementation
module Cli2.Commands.Quit

open Cli2.Types
open Cli2.Commands.ICommand

type QuitCommand() =
  inherit CommandBase()

  override _.Name = "quit"
  override _.Description = "Exit the CLI"
  override _.Aliases = [ "exit"; "q" ]

  override _.Execute _state _args =
    let output = CommandOutput.line "Goodbye!"
    (output, Exit 0)

  override _.Help() =
    [ "quit - Exit the CLI"
      ""
      "Aliases: exit, q" ]

let command = QuitCommand() :> ICommand
