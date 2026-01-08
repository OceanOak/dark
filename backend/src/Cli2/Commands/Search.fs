/// Search command - search for packages, types, functions, and values
module Cli2.Commands.Search

open System
open Cli2.Types
open Cli2.Commands.ICommand
open Cli2.PackageUtils

module PT = LibExecution.ProgramTypes

/// Parse search options from args
let parseSearchOptions (args: string list) : (string * PT.Search.EntityType list) =
  let mutable searchText = ""
  let mutable entityTypes = []

  for arg in args do
    match arg with
    | "-t" | "--type" -> entityTypes <- PT.Search.EntityType.Type :: entityTypes
    | "-f" | "--function" -> entityTypes <- PT.Search.EntityType.Fn :: entityTypes
    | "-v" | "--value" -> entityTypes <- PT.Search.EntityType.Value :: entityTypes
    | "-m" | "--module" -> entityTypes <- PT.Search.EntityType.Module :: entityTypes
    | "-a" | "--all" -> entityTypes <- []
    | s when not (s.StartsWith("-")) -> searchText <- s
    | _ -> ()

  (searchText, entityTypes)

type SearchCommand() =
  inherit CommandBase()

  override _.Name = "search"
  override _.Description = "Search for packages, types, functions, and values"
  override _.Aliases = [ "find"; "grep" ]

  override _.Execute state args =
    match args with
    | [] ->
      let output = CommandOutput.error "Usage: search <query> [options]"
      (output, StateUpdate state)

    | _ ->
      let (searchText, entityTypes) = parseSearchOptions args

      if String.IsNullOrWhiteSpace(searchText) then
        let output = CommandOutput.error "Usage: search <query> [options]"
        (output, StateUpdate state)
      else
        let currentPath = modulePathOf state.PackageData.CurrentLocation
        let results = searchPackages state.AccountID state.CurrentBranchID currentPath searchText entityTypes

        let pathStr = if List.isEmpty currentPath then "/" else String.Join(".", currentPath)
        let lines = ResizeArray<string>()
        lines.Add($"Search results for '{searchText}' from {pathStr}:")
        lines.Add("")

        let totalResults =
          List.length results.Submodules +
          List.length results.Types +
          List.length results.Values +
          List.length results.Functions

        if totalResults = 0 then
          lines.Add("  No results found.")
        else
          // Display submodules
          if not (List.isEmpty results.Submodules) then
            lines.Add(getSectionHeader "module")
            for path in results.Submodules |> List.take (min 10 (List.length results.Submodules)) do
              let pathStr = String.Join(".", path)
              lines.Add($"  {pathStr}")
            if List.length results.Submodules > 10 then
              let remaining = List.length results.Submodules - 10
              lines.Add($"  ... and {remaining} more")
            lines.Add("")

          // Display types
          if not (List.isEmpty results.Types) then
            lines.Add(getSectionHeader "type")
            for (loc, _) in results.Types |> List.take (min 10 (List.length results.Types)) do
              lines.Add($"  {formatPTLocation loc}")
            if List.length results.Types > 10 then
              let remaining = List.length results.Types - 10
              lines.Add($"  ... and {remaining} more")
            lines.Add("")

          // Display values
          if not (List.isEmpty results.Values) then
            lines.Add(getSectionHeader "value")
            for (loc, _) in results.Values |> List.take (min 10 (List.length results.Values)) do
              lines.Add($"  {formatPTLocation loc}")
            if List.length results.Values > 10 then
              let remaining = List.length results.Values - 10
              lines.Add($"  ... and {remaining} more")
            lines.Add("")

          // Display functions
          if not (List.isEmpty results.Functions) then
            lines.Add(getSectionHeader "function")
            for (loc, _) in results.Functions |> List.take (min 10 (List.length results.Functions)) do
              lines.Add($"  {formatPTLocation loc}")
            if List.length results.Functions > 10 then
              let remaining = List.length results.Functions - 10
              lines.Add($"  ... and {remaining} more")
            lines.Add("")

          lines.Add($"Total: {totalResults} results")

        let output = CommandOutput.lines (lines |> Seq.toList)
        (output, StateUpdate state)

  override _.Help() =
    [ "Usage: search <query> [options]"
      "Search for packages, types, functions, and values."
      ""
      "Options:"
      "  -t, --type        Search only types"
      "  -f, --function    Search only functions"
      "  -v, --value       Search only values"
      "  -m, --module      Search only modules"
      "  -a, --all         Search all (default)"
      ""
      "Examples:"
      "  search map              - Search for anything containing 'map'"
      "  search -f parse         - Search for functions containing 'parse'"
      "  search -t Result        - Search for types containing 'Result'" ]

let command = SearchCommand() :> ICommand
