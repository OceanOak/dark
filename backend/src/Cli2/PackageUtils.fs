/// Package utilities - common functions for package navigation and queries
module Cli2.PackageUtils

open System
open Prelude
open Cli2.Types

module PT = LibExecution.ProgramTypes
module PM = LibPackageManager.PackageManager

/// Get module path from a PackageLocation
let modulePathOf (location: PackageLocation) : string list =
  match location with
  | Module path -> path
  | Type(owner, modules, _) -> owner :: modules
  | Value(owner, modules, _) -> owner :: modules
  | Function(owner, modules, _) -> owner :: modules

/// Search results with full entity information
type SearchResults =
  { Submodules: string list list
    Types: (PT.PackageLocation * PT.PackageType.PackageType) list
    Values: (PT.PackageLocation * PT.PackageValue.PackageValue) list
    Functions: (PT.PackageLocation * PT.PackageFn.PackageFn) list }

  static member empty =
    { Submodules = []
      Types = []
      Values = []
      Functions = [] }

/// Query all direct descendants of a module path
let queryAllDirectDescendants
  (accountID: Guid option)
  (branchID: Guid option)
  (modulePath: string list)
  : SearchResults =
  let pm = PM.pt
  let query : PT.Search.SearchQuery =
    { currentModule = modulePath
      text = ""
      searchDepth = PT.Search.SearchDepth.OnlyDirectDescendants
      entityTypes = []
      exactMatch = false }

  let results = pm.search(accountID, branchID, query).Result

  { Submodules = results.submodules
    Types = results.types |> List.map (fun item -> (item.location, item.entity))
    Values = results.values |> List.map (fun item -> (item.location, item.entity))
    Functions = results.fns |> List.map (fun item -> (item.location, item.entity)) }

/// Search for items matching a text query
let searchPackages
  (accountID: Guid option)
  (branchID: Guid option)
  (modulePath: string list)
  (searchText: string)
  (entityTypes: PT.Search.EntityType list)
  : SearchResults =
  let pm = PM.pt
  let query : PT.Search.SearchQuery =
    { currentModule = modulePath
      text = searchText
      searchDepth = PT.Search.SearchDepth.AllDescendants
      entityTypes = entityTypes
      exactMatch = false }

  let results = pm.search(accountID, branchID, query).Result

  { Submodules = results.submodules
    Types = results.types |> List.map (fun item -> (item.location, item.entity))
    Values = results.values |> List.map (fun item -> (item.location, item.entity))
    Functions = results.fns |> List.map (fun item -> (item.location, item.entity)) }

/// Get direct submodule names from search results
/// Given paths like ["Darklang"; "Stdlib"; "List"], at root (currentPathLength=0)
/// we want to extract "Darklang". At Darklang (currentPathLength=1) we want "Stdlib".
let getDirectSubmodules (results: SearchResults) (currentPathLength: int) : string list =
  results.Submodules
  |> List.choose (fun path ->
    // Drop the current path prefix, then take the first element
    match List.skip currentPathLength path with
    | [] -> None
    | nextPart :: _ ->
      if String.IsNullOrEmpty(nextPart) then None
      else Some nextPart)
  |> List.distinct
  |> List.sort

/// Traverse a path from current location
let traverse
  (accountID: Guid option)
  (branchID: Guid option)
  (currentLocation: PackageLocation)
  (pathArg: string)
  : Result<PackageLocation, string> =
  // Handle special paths
  if pathArg = "/" then
    Ok (Module [])
  elif pathArg = ".." then
    let currentPath = modulePathOf currentLocation
    match currentPath with
    | [] -> Error "Already at root"
    | _ ->
      let parentPath = List.take (List.length currentPath - 1) currentPath
      Ok (Module parentPath)
  elif pathArg.StartsWith("..") then
    // Handle multiple levels up
    let parts = pathArg.Split('/') |> Array.toList
    let upCount = parts |> List.filter ((=) "..") |> List.length
    let currentPath = modulePathOf currentLocation
    if upCount > List.length currentPath then
      Error "Cannot go above root"
    else
      let newPath = List.take (List.length currentPath - upCount) currentPath
      let remainingParts = parts |> List.filter ((<>) "..") |> List.filter ((<>) "")
      Ok (Module (newPath @ remainingParts))
  elif pathArg.StartsWith("/") then
    // Absolute path
    let parts = pathArg.Substring(1).Split([| '/'; '.' |], StringSplitOptions.RemoveEmptyEntries) |> Array.toList
    Ok (Module parts)
  else
    // Relative path - check if it's a submodule, type, value, or function
    let currentPath = modulePathOf currentLocation
    let parts = pathArg.Split([| '/'; '.' |], StringSplitOptions.RemoveEmptyEntries) |> Array.toList

    // Try as submodule first
    let newPath = currentPath @ parts
    let results = queryAllDirectDescendants accountID branchID currentPath
    let submodules = getDirectSubmodules results (List.length currentPath)

    match parts with
    | [ name ] when List.contains name submodules ->
      Ok (Module (currentPath @ [ name ]))
    | [ name ] ->
      // Check if it's a type, value, or function
      let typeMatch = results.Types |> List.tryFind (fun (loc, _) -> loc.name = name)
      let valueMatch = results.Values |> List.tryFind (fun (loc, _) -> loc.name = name)
      let fnMatch = results.Functions |> List.tryFind (fun (loc, _) -> loc.name = name)

      match typeMatch, valueMatch, fnMatch with
      | Some (loc, _), _, _ -> Ok (Type(loc.owner, loc.modules, loc.name))
      | _, Some (loc, _), _ -> Ok (Value(loc.owner, loc.modules, loc.name))
      | _, _, Some (loc, _) -> Ok (Function(loc.owner, loc.modules, loc.name))
      | None, None, None ->
        // Try as nested module path
        Ok (Module newPath)
    | _ ->
      // Multi-part path - check if the last part is an entity in the parent module
      let moduleParts = List.take (List.length parts - 1) parts
      let entityName = List.last parts |> Option.defaultValue ""
      let targetModulePath = currentPath @ moduleParts

      // Query the target module for entities
      let targetResults = queryAllDirectDescendants accountID branchID targetModulePath

      let typeMatch = targetResults.Types |> List.tryFind (fun (loc, _) -> loc.name = entityName)
      let valueMatch = targetResults.Values |> List.tryFind (fun (loc, _) -> loc.name = entityName)
      let fnMatch = targetResults.Functions |> List.tryFind (fun (loc, _) -> loc.name = entityName)

      match typeMatch, valueMatch, fnMatch with
      | Some (loc, _), _, _ -> Ok (Type(loc.owner, loc.modules, loc.name))
      | _, Some (loc, _), _ -> Ok (Value(loc.owner, loc.modules, loc.name))
      | _, _, Some (loc, _) -> Ok (Function(loc.owner, loc.modules, loc.name))
      | None, None, None ->
        // Treat as module path
        Ok (Module newPath)

/// Complete a partial path
let completePartialPath
  (accountID: Guid option)
  (branchID: Guid option)
  (currentLocation: PackageLocation)
  (partial: string)
  : string list =
  let currentPath = modulePathOf currentLocation
  let results = queryAllDirectDescendants accountID branchID currentPath

  let submodules = getDirectSubmodules results (List.length currentPath)
  let types = results.Types |> List.map (fun (loc, _) -> loc.name)
  let values = results.Values |> List.map (fun (loc, _) -> loc.name)
  let functions = results.Functions |> List.map (fun (loc, _) -> loc.name)

  let allNames =
    (submodules |> List.map (fun s -> s + "/"))
    @ types
    @ values
    @ functions

  if String.IsNullOrEmpty(partial) then
    allNames
  else
    allNames |> List.filter (fun n -> n.StartsWith(partial, StringComparison.OrdinalIgnoreCase))

/// Format a display section header
let getSectionHeader (entityType: string) : string =
  let icon =
    match entityType with
    | "module" | "submodule" -> "[M]"
    | "type" -> "[T]"
    | "value" -> "[V]"
    | "function" -> "[F]"
    | _ -> "   "
  $"{icon} {entityType}s:"

/// Get icon for entity type
let getIcon (entityType: string) : string =
  match entityType with
  | "module" -> "[M]"
  | "type" -> "[T]"
  | "value" -> "[V]"
  | "function" -> "[F]"
  | _ -> "   "

/// Format a PT.PackageLocation as a string
let formatPTLocation (loc: PT.PackageLocation) : string =
  let modulePath = String.Join(".", loc.modules)
  if String.IsNullOrEmpty(modulePath) then
    $"{loc.owner}.{loc.name}"
  else
    $"{loc.owner}.{modulePath}.{loc.name}"
