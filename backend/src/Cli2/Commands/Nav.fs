/// Nav command - navigate between packages, modules, and entities
module Cli2.Commands.Nav

open Cli2.Types
open Cli2.Commands.ICommand
open Cli2.PackageUtils
open Cli2.NavInteractive

type NavCommand() =
  inherit CommandBase()

  override _.Name = "nav"
  override _.Description = "Navigate package space"
  override _.Aliases = [ "cd" ]

  override _.Execute state args =
    match args with
    | [] ->
      // No args - enter interactive navigation mode
      let result = run state.AccountID state.CurrentBranchID state.PackageData.CurrentLocation
      match result with
      | ExitNoChange ->
        let output = CommandOutput.empty
        (output, StateUpdate { state with NeedsFullRedraw = true })
      | ExitWithLocation newLocation ->
        let newHistory = state.PackageData.LocationHistory @ [ state.PackageData.CurrentLocation ]
        let newPackageData =
          { state.PackageData with
              CurrentLocation = newLocation
              LocationHistory = newHistory }
        let newState = { state with PackageData = newPackageData; NeedsFullRedraw = true }
        let locationStr = PackageLocation.format newLocation
        let output = CommandOutput.line $"Selected: {locationStr}"
        (output, StateUpdate newState)

    | [ pathArg ] ->
      // Navigate to path
      match traverse state.AccountID state.CurrentBranchID state.PackageData.CurrentLocation pathArg with
      | Error errorMsg ->
        let output = CommandOutput.error $"Navigation failed: {errorMsg}"
        (output, StateUpdate state)
      | Ok newLocation ->
        // Update history
        let newHistory = state.PackageData.LocationHistory @ [ state.PackageData.CurrentLocation ]
        let newPackageData =
          { state.PackageData with
              CurrentLocation = newLocation
              LocationHistory = newHistory }
        let newState = { state with PackageData = newPackageData; NeedsFullRedraw = true }
        let locationStr = PackageLocation.format newLocation
        let output = CommandOutput.line $"Changed to: {locationStr}"
        (output, StateUpdate newState)

    | _ ->
      let output = CommandOutput.error "Usage: nav [path]"
      (output, StateUpdate state)

  override _.Complete state args =
    match args with
    | [] ->
      completePartialPath state.AccountID state.CurrentBranchID state.PackageData.CurrentLocation ""
    | [ partialPath ] ->
      completePartialPath state.AccountID state.CurrentBranchID state.PackageData.CurrentLocation partialPath
    | _ -> []

  override _.Help() =
    [ "Usage: nav [path]"
      "Navigate package space - modules, types, values, and functions."
      ""
      "With path: Navigate directly to the specified location."
      "Without path: Enter interactive navigation mode."
      ""
      "Interactive mode controls:"
      "  Up/Down: Navigate items"
      "  Left: Go to parent"
      "  Right: Enter module"
      "  Enter: Select and exit"
      "  Space: Toggle focus mode"
      "  /: Enter search mode"
      "  Esc: Exit"
      ""
      "Examples:"
      "  nav                  - Enter interactive navigation"
      "  nav /                - Go to root"
      "  nav /Darklang/Stdlib - Go to absolute path from root"
      "  nav ..               - Go to parent"
      "  nav ../..            - Go to grandparent"
      "  nav Submodule        - Go to a submodule of current location"
      "  nav Stdlib/List      - Go to Stdlib.List module" ]

let command = NavCommand() :> ICommand
