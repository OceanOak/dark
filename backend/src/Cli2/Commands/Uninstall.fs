/// Uninstall command - uninstall the CLI
module Cli2.Commands.Uninstall

open Cli2.Types
open Cli2.Commands.ICommand

type UninstallCommand() =
  inherit CommandBase()

  override _.Name = "uninstall"
  override _.Description = "Uninstall the CLI"

  override _.Execute state _args =
    let output = CommandOutput.lines [
      "CLI Uninstall"
      ""
      "To uninstall the Darklang CLI:"
      ""
      "  # Remove the binary:"
      "  sudo rm /usr/local/bin/dark"
      ""
      "  # Remove config (optional):"
      "  rm -rf ~/.darklang"
      ""
      "(Automatic uninstall not yet implemented in F# CLI)"
    ]
    (output, StateUpdate state)

  override _.Help() =
    [ "Usage: uninstall"
      "Uninstall the CLI from your system."
      ""
      "This will remove the CLI binary and optionally"
      "remove configuration files." ]

let command = UninstallCommand() :> ICommand
