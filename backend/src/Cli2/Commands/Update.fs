/// Update command - update the CLI
module Cli2.Commands.Update

open Cli2.Types
open Cli2.Commands.ICommand

type UpdateCommand() =
  inherit CommandBase()

  override _.Name = "update"
  override _.Description = "Update the CLI to the latest version"

  override _.Execute state _args =
    let output = CommandOutput.lines [
      "CLI Update"
      ""
      "To update the Darklang CLI, download the latest release from:"
      "  https://github.com/darklang/dark/releases"
      ""
      "Or rebuild from source:"
      "  git pull && ./scripts/build/compile"
      ""
      "(Automatic update not yet implemented in F# CLI)"
    ]
    (output, StateUpdate state)

  override _.Help() =
    [ "Usage: update"
      "Update the CLI to the latest version."
      ""
      "This will download and install the latest release." ]

let command = UpdateCommand() :> ICommand
