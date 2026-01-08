/// Clear command - clear the terminal screen
module Cli2.Commands.Clear

open System
open Cli2.Types
open Cli2.Commands.ICommand

type ClearCommand() =
  inherit CommandBase()

  override _.Name = "clear"
  override _.Description = "Clear the terminal screen"

  override _.Execute state _args =
    Console.Clear()
    let output = CommandOutput.empty
    (output, StateUpdate { state with NeedsFullRedraw = true })

  override _.Help() =
    [ "Usage: clear"
      "Clear the terminal screen." ]

let command = ClearCommand() :> ICommand
