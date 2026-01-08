/// Ls command - list contents of current location
module Cli2.Commands.Ls

open System
open Cli2.Types
open Cli2.Commands.ICommand
open Cli2.PackageUtils

let listModule
  (accountID: Guid option)
  (branchID: Guid option)
  (location: PackageLocation)
  : string list =
  let modulePath = modulePathOf location
  let results = queryAllDirectDescendants accountID branchID modulePath

  let locationStr = PackageLocation.format location
  let lines = ResizeArray<string>()
  lines.Add($"Contents of {locationStr}:")
  lines.Add("")

  // Display submodules
  let currentPathLength = List.length modulePath
  let directSubmodules = getDirectSubmodules results currentPathLength

  if not (List.isEmpty directSubmodules) then
    lines.Add(getSectionHeader "module")
    for name in directSubmodules do
      lines.Add($"  {name}/")
    lines.Add("")

  // Display types
  if not (List.isEmpty results.Types) then
    lines.Add(getSectionHeader "type")
    for (loc, _typ) in results.Types do
      lines.Add($"  {loc.name}")
    lines.Add("")

  // Display values
  if not (List.isEmpty results.Values) then
    lines.Add(getSectionHeader "value")
    for (loc, _) in results.Values do
      lines.Add($"  {loc.name}")
    lines.Add("")

  // Display functions
  if not (List.isEmpty results.Functions) then
    lines.Add(getSectionHeader "function")
    for (loc, _fn) in results.Functions do
      lines.Add($"  {loc.name}")
    lines.Add("")

  if List.isEmpty directSubmodules && List.isEmpty results.Types &&
     List.isEmpty results.Values && List.isEmpty results.Functions then
    lines.Add("  (empty - no items at this location)")
    lines.Add("")

  lines |> Seq.toList

type LsCommand() =
  inherit CommandBase()

  override _.Name = "ls"
  override _.Description = "List contents of current location"
  override _.Aliases = [ "list"; "dir" ]

  override _.Execute state args =
    match args with
    | [] ->
      // List current location
      let lines = listModule state.AccountID state.CurrentBranchID state.PackageData.CurrentLocation
      let output = CommandOutput.lines lines
      (output, StateUpdate state)

    | [ pathArg ] ->
      // Navigate to path and list
      match traverse state.AccountID state.CurrentBranchID state.PackageData.CurrentLocation pathArg with
      | Error errorMsg ->
        let output = CommandOutput.error $"Cannot list: {errorMsg}"
        (output, StateUpdate state)
      | Ok newLocation ->
        match newLocation with
        | Module _ ->
          let lines = listModule state.AccountID state.CurrentBranchID newLocation
          let output = CommandOutput.lines lines
          (output, StateUpdate state)
        | Type _ | Value _ | Function _ ->
          let locationStr = PackageLocation.format newLocation
          let output = CommandOutput.lines [
            $"'{locationStr}' is not a module."
            "Use 'view' to see details of types, values, or functions."
          ]
          (output, StateUpdate state)

    | _ ->
      let output = CommandOutput.error "Usage: ls [path]"
      (output, StateUpdate state)

  override _.Complete state args =
    match args with
    | [] ->
      completePartialPath state.AccountID state.CurrentBranchID state.PackageData.CurrentLocation ""
    | [ partialPath ] ->
      completePartialPath state.AccountID state.CurrentBranchID state.PackageData.CurrentLocation partialPath
    | _ -> []

  override _.Help() =
    [ "Usage: ls [path]"
      "List contents of the current location or specified path."
      ""
      "Shows modules, types, values, and functions grouped by category."
      ""
      "Examples:"
      "  ls                - List current location"
      "  ls /              - List root packages"
      "  ls Darklang       - List Darklang package contents"
      "  ls ..             - List parent directory"
      ""
      "See also: `tree` for multi-level view, `view` for detailed view" ]

let command = LsCommand() :> ICommand
