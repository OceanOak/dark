/// Tree command - display package hierarchy in tree format
module Cli2.Commands.Tree

open System
open Cli2.Types
open Cli2.Commands.ICommand
open Cli2.PackageUtils

let rec displayTree
  (accountID: Guid option)
  (branchID: Guid option)
  (location: PackageLocation)
  (remainingDepth: int)
  (prefix: string)
  (lines: ResizeArray<string>)
  : unit =
  if remainingDepth <= 0 then ()
  else
    let modulePath = modulePathOf location
    let results = queryAllDirectDescendants accountID branchID modulePath
    let currentPathLength = List.length modulePath

    // Get submodule names
    let submoduleNames = getDirectSubmodules results currentPathLength

    // Get entity names
    let typeNames = results.Types |> List.map (fun (loc, _) -> loc.name) |> List.sort
    let valueNames = results.Values |> List.map (fun (loc, _) -> loc.name) |> List.sort
    let fnNames = results.Functions |> List.map (fun (loc, _) -> loc.name) |> List.sort

    // Combine all entities
    let allEntities =
      (submoduleNames |> List.map (fun n -> (n, "module")))
      @ (typeNames |> List.map (fun n -> (n, "type")))
      @ (valueNames |> List.map (fun n -> (n, "value")))
      @ (fnNames |> List.map (fun n -> (n, "function")))

    let entitiesCount = List.length allEntities

    allEntities
    |> List.iteri (fun index (name, entityType) ->
      let isLast = index = entitiesCount - 1
      let treeBranch = if isLast then "+-- " else "|-- "
      let fullPrefix = prefix + treeBranch
      let icon = getIcon entityType
      lines.Add($"{fullPrefix}{icon} {name}")

      // Recursively display submodules
      if entityType = "module" && remainingDepth > 1 then
        let newPrefix = prefix + (if isLast then "    " else "|   ")
        let newModulePath = modulePath @ [ name ]
        let newLocation = Module newModulePath
        displayTree accountID branchID newLocation (remainingDepth - 1) newPrefix lines
    )

type TreeCommand() =
  inherit CommandBase()

  override _.Name = "tree"
  override _.Description = "Display package hierarchy in tree format"

  override _.Execute state args =
    // Parse depth option
    let depthArg =
      args
      |> List.tryFind (fun arg -> arg.StartsWith("--depth="))
      |> Option.bind (fun arg ->
        let depthStr = arg.Substring(8)
        match Int32.TryParse(depthStr) with
        | true, d when d > 0 && d <= 10 -> Some d
        | _ -> None)
      |> Option.defaultValue 2

    // Get path arguments (non-flag args)
    let pathArgs = args |> List.filter (fun arg -> not (arg.StartsWith("--")))

    // Determine starting location
    let startLocation =
      match pathArgs with
      | [] -> state.PackageData.CurrentLocation
      | [ pathStr ] ->
        match traverse state.AccountID state.CurrentBranchID state.PackageData.CurrentLocation pathStr with
        | Ok location -> location
        | Error _ -> state.PackageData.CurrentLocation
      | _ -> state.PackageData.CurrentLocation

    let locationStr = PackageLocation.format startLocation
    let lines = ResizeArray<string>()
    lines.Add($"Package tree from {locationStr}:")
    lines.Add("")

    displayTree state.AccountID state.CurrentBranchID startLocation depthArg "" lines

    if lines.Count <= 2 then
      lines.Add("  (empty - no items at this location)")
      lines.Add("")

    let output = CommandOutput.lines (lines |> Seq.toList)
    (output, StateUpdate state)

  override _.Complete state args =
    let pathArgs = args |> List.filter (fun arg -> not (arg.StartsWith("--")))
    match pathArgs with
    | [] ->
      completePartialPath state.AccountID state.CurrentBranchID state.PackageData.CurrentLocation ""
    | [ partialPath ] ->
      completePartialPath state.AccountID state.CurrentBranchID state.PackageData.CurrentLocation partialPath
    | _ -> []

  override _.Help() =
    [ "Usage: tree [path] [--depth=N]"
      "Display package hierarchy in tree format."
      ""
      "Options:"
      "  --depth=N       Set maximum depth (1-10, default: 2)"
      ""
      "Examples:"
      "  tree                         - Show tree with depth 2"
      "  tree /                       - Show tree from root"
      "  tree Darklang                - Show Darklang package tree"
      "  tree --depth=1               - Show only direct children"
      "  tree --depth=5               - Show deeper tree structure" ]

let command = TreeCommand() :> ICommand
