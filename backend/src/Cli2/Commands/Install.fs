/// Install command - install the CLI globally
module Cli2.Commands.Install

open Cli2.Types
open Cli2.Commands.ICommand

type InstallCommand() =
  inherit CommandBase()

  override _.Name = "install"
  override _.Description = "Install the CLI globally"

  override _.Execute state _args =
    let output = CommandOutput.lines [
      "CLI Installation"
      ""
      "To install the Darklang CLI globally, run:"
      ""
      "  # On Linux/macOS:"
      "  sudo cp $(which dark) /usr/local/bin/dark"
      ""
      "  # Or add the current directory to PATH:"
      "  export PATH=\"$PATH:$(pwd)\""
      ""
      "(Automatic installation not yet implemented in F# CLI)"
    ]
    (output, StateUpdate state)

  override _.Help() =
    [ "Usage: install"
      "Install the CLI globally on your system."
      ""
      "This will copy the CLI to a location in your PATH,"
      "making it available from any directory." ]

let command = InstallCommand() :> ICommand
