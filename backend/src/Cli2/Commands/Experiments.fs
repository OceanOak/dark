/// Experiments command - experimental features
module Cli2.Commands.Experiments

open Cli2.Types
open Cli2.Commands.ICommand

type ExperimentsCommand() =
  inherit CommandBase()

  override _.Name = "experiments"
  override _.Description = "Access experimental features"

  override _.Execute state _args =
    let output = CommandOutput.lines [
      "Experimental Features"
      ""
      "The experiments menu provides access to:"
      "  - UI component catalog"
      "  - Interactive demos"
      "  - Debug tools"
      ""
      "(Experiments mode not yet implemented in F# CLI)"
      ""
      "Available in the Darklang CLI when using 'nav' without arguments."
    ]
    (output, StateUpdate state)

  override _.Help() =
    [ "Usage: experiments"
      "Access experimental features and demos."
      ""
      "This launches an interactive menu with:"
      "  - UI component catalog"
      "  - Interactive demos"
      "  - Debug and development tools" ]

let command = ExperimentsCommand() :> ICommand
