/// Status command - show installation status
module Cli2.Commands.Status

open System
open System.Reflection
open Cli2.Types
open Cli2.Commands.ICommand

type StatusCommand() =
  inherit CommandBase()

  override _.Name = "status"
  override _.Description = "Show installation and connection status"

  override _.Execute state _args =
    let assembly = Assembly.GetEntryAssembly()
    let location = assembly.Location

    let branchStr =
      state.CurrentBranchID
      |> Option.map (fun id -> id.ToString().Substring(0, 8))
      |> Option.defaultValue "main"
    let locationStr = PackageLocation.format state.PackageData.CurrentLocation

    let lines = [
      "Darklang CLI Status"
      String.replicate 40 "-"
      ""
      $"Account: {state.AccountName}"
      $"Branch: {branchStr}"
      $"Location: {locationStr}"
      ""
      $"CLI Path: {location}"
      $"Platform: {Environment.OSVersion.Platform}"
      $".NET Version: {Environment.Version}"
    ]
    let output = CommandOutput.lines lines
    (output, StateUpdate state)

  override _.Help() =
    [ "Usage: status"
      "Display installation and connection status."
      ""
      "Shows:"
      "  - Current account and branch"
      "  - Current location in package tree"
      "  - CLI installation path"
      "  - Platform information" ]

let command = StatusCommand() :> ICommand
