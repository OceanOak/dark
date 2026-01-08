/// Back command - navigate back in history
module Cli2.Commands.Back

open Cli2.Types
open Cli2.Commands.ICommand

type BackCommand() =
  inherit CommandBase()

  override _.Name = "back"
  override _.Description = "Navigate back to previous location"

  override _.Execute state _args =
    match List.tryLast state.PackageData.LocationHistory with
    | Some previousLocation ->
      let pathStr = PackageLocation.format previousLocation
      let newHistory =
        if List.isEmpty state.PackageData.LocationHistory then []
        else state.PackageData.LocationHistory |> List.take (List.length state.PackageData.LocationHistory - 1)
      let newPackageData =
        { state.PackageData with
            CurrentLocation = previousLocation
            LocationHistory = newHistory }
      let newState = { state with PackageData = newPackageData; NeedsFullRedraw = true }
      let output = CommandOutput.line $"Back to: {pathStr}"
      (output, StateUpdate newState)

    | None ->
      let output = CommandOutput.line "No previous location in history"
      (output, StateUpdate state)

  override _.Help() =
    [ "Usage: back"
      "Navigate back to the previous location in navigation history."
      ""
      "Similar to a browser's back button, this undoes your last 'nav' command."
      "History is built up when you use 'nav' to move between locations."
      ""
      "Example:"
      "  nav /Darklang/Stdlib    (moves to Stdlib, adds current to history)"
      "  back                    (returns to previous location)" ]

let command = BackCommand() :> ICommand
