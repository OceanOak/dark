/// Interactive navigation mode - TUI for browsing packages
module Cli2.NavInteractive

open System
open Cli2.Types
open Cli2.PackageUtils

module PT = LibExecution.ProgramTypes
module PM = LibPackageManager.PackageManager
module View = Cli2.Commands.View

/// Terminal escape sequences
module Terminal =
  let enterAlternateScreen () = Console.Write("\u001b[?1049h")
  let exitAlternateScreen () = Console.Write("\u001b[?1049l")
  let hideCursor () = Console.Write("\u001b[?25l")
  let showCursor () = Console.Write("\u001b[?25h")
  let clearScreen () = Console.Write("\u001b[2J\u001b[H")
  let clearLine () = Console.Write("\u001b[2K")
  let moveTo (row: int) (col: int) = Console.Write($"\u001b[{row};{col}H")

/// Navigation item
type NavItem =
  { Name: string
    EntityType: string  // "module", "type", "value", "function"
    Location: PackageLocation }

/// Display mode
type DisplayMode =
  | JustName
  | Source

/// Interactive mode
type InteractiveMode =
  | Nav
  | Search

/// Interactive navigation state
type NavState =
  { Items: NavItem list
    SelectedIndex: int
    ScrollOffset: int
    CurrentLocation: PackageLocation
    Mode: InteractiveMode
    Display: DisplayMode
    AccountID: Guid option
    BranchID: Guid option
    SearchQuery: string
    AllItems: NavItem list }

/// Build navigation items from search results
let buildNavItemsFromResults (results: SearchResults) (modulePath: string list) : NavItem list =
  let currentPathLength = List.length modulePath

  // Extract direct submodule names (same logic as getDirectSubmodules in PackageUtils)
  let submodules =
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
    |> List.map (fun name ->
      { Name = name
        EntityType = "module"
        Location = Module (modulePath @ [ name ]) })

  let types =
    results.Types
    |> List.map (fun (loc, _) ->
      { Name = loc.name
        EntityType = "type"
        Location = Type(loc.owner, loc.modules, loc.name) })

  let values =
    results.Values
    |> List.map (fun (loc, _) ->
      { Name = loc.name
        EntityType = "value"
        Location = Value(loc.owner, loc.modules, loc.name) })

  let functions =
    results.Functions
    |> List.map (fun (loc, _) ->
      { Name = loc.name
        EntityType = "function"
        Location = Function(loc.owner, loc.modules, loc.name) })

  submodules @ types @ values @ functions

/// Create interactive navigation state for a location
let buildState (accountID: Guid option) (branchID: Guid option) (location: PackageLocation) : NavState =
  let modulePath = modulePathOf location
  let results = queryAllDirectDescendants accountID branchID modulePath
  let allItems = buildNavItemsFromResults results modulePath

  { Items = allItems
    SelectedIndex = 0
    ScrollOffset = 0
    CurrentLocation = location
    Mode = Nav
    Display = JustName
    AccountID = accountID
    BranchID = branchID
    SearchQuery = ""
    AllItems = allItems }

/// Get icon for entity type
let getIcon (entityType: string) : string =
  match entityType with
  | "module" -> "[M]"
  | "type" -> "[T]"
  | "value" -> "[V]"
  | "function" -> "[F]"
  | _ -> "   "

/// Display the interactive navigation interface
let display (navState: NavState) =
  Terminal.hideCursor ()
  Terminal.clearScreen ()

  let locationStr = PackageLocation.format navState.CurrentLocation
  let viewportHeight = 12

  // Header
  let headerPrefix =
    match navState.Mode with
    | Nav -> ">"
    | Search -> "[Search]"

  let displaySuffix =
    match navState.Display with
    | JustName -> ""
    | Source -> " (Focus Mode)"

  Console.ForegroundColor <- ConsoleColor.Cyan
  Console.WriteLine($"{headerPrefix} {locationStr}{displaySuffix}")
  Console.ResetColor()
  Console.WriteLine(String.replicate 60 "-")

  // Search bar if in search mode
  match navState.Mode with
  | Search ->
    Console.WriteLine($"  {navState.SearchQuery}_")
    Console.WriteLine(String.replicate 60 "-")
  | Nav -> ()

  // Items
  let totalItems = List.length navState.Items

  if totalItems = 0 then
    Console.ForegroundColor <- ConsoleColor.DarkGray
    Console.WriteLine("  (empty)")
    Console.ResetColor()
  else
    let startIndex = navState.ScrollOffset
    let endIndex = min (startIndex + viewportHeight) totalItems

    let visibleItems =
      navState.Items
      |> List.skip startIndex
      |> List.take (endIndex - startIndex)

    visibleItems
    |> List.iteri (fun relativeIndex item ->
      let absoluteIndex = startIndex + relativeIndex
      let isSelected = absoluteIndex = navState.SelectedIndex

      let icon = getIcon item.EntityType
      let cursor = if isSelected then "> " else "  "
      let nameDisplay =
        match item.EntityType with
        | "module" -> item.Name + "/"
        | _ -> item.Name

      if isSelected then
        Console.ForegroundColor <- ConsoleColor.Green
        Console.WriteLine($"{cursor}{icon} {nameDisplay}")
        Console.ResetColor()
      else
        Console.WriteLine($"{cursor}{icon} {nameDisplay}"))

  // Focus mode: show entity details with pretty-printed source
  match navState.Display with
  | Source when totalItems > 0 ->
    match List.tryItem navState.SelectedIndex navState.Items with
    | Some selectedItem ->
      Console.WriteLine()
      Console.WriteLine(String.replicate 60 "-")

      // Get pretty-printed source based on entity type
      let pm = PM.pt
      let prettyPrinted =
        match selectedItem.Location with
        | Type(owner, modules, name) ->
          let ptLoc : PT.PackageLocation = { owner = owner; modules = modules; name = name }
          let idOpt = pm.findType(navState.AccountID, navState.BranchID, ptLoc).Result
          match idOpt with
          | None -> None
          | Some id ->
            let typeOpt = pm.getType(id).Result
            match typeOpt with
            | None -> None
            | Some typ ->
              let result = View.prettyPrintType navState.AccountID navState.BranchID typ
              match result.Result with
              | Ok s -> Some s
              | Error _ -> None

        | Function(owner, modules, name) ->
          let ptLoc : PT.PackageLocation = { owner = owner; modules = modules; name = name }
          let idOpt = pm.findFn(navState.AccountID, navState.BranchID, ptLoc).Result
          match idOpt with
          | None -> None
          | Some id ->
            let fnOpt = pm.getFn(id).Result
            match fnOpt with
            | None -> None
            | Some fn ->
              let result = View.prettyPrintFn navState.AccountID navState.BranchID fn
              match result.Result with
              | Ok s -> Some s
              | Error _ -> None

        | Value(owner, modules, name) ->
          let ptLoc : PT.PackageLocation = { owner = owner; modules = modules; name = name }
          let idOpt = pm.findValue(navState.AccountID, navState.BranchID, ptLoc).Result
          match idOpt with
          | None -> None
          | Some id ->
            let valOpt = pm.getValue(id).Result
            match valOpt with
            | None -> None
            | Some v ->
              let result = View.prettyPrintValue navState.AccountID navState.BranchID v
              match result.Result with
              | Ok s -> Some s
              | Error _ -> None

        | Module _ -> None

      match prettyPrinted with
      | Some source ->
        Console.WriteLine(source)
      | None ->
        Console.ForegroundColor <- ConsoleColor.Yellow
        Console.WriteLine($"  {PackageLocation.format selectedItem.Location}")
        Console.ResetColor()

      Console.WriteLine(String.replicate 60 "-")
    | None -> ()
  | _ -> ()

  // Help footer
  Console.WriteLine()
  Console.ForegroundColor <- ConsoleColor.DarkGray
  let helpText =
    match navState.Mode, navState.Display with
    | Nav, JustName -> "Up/Down: Navigate | Left: Up | Right: Enter | Enter: Select | Space: Focus | /: Search | Esc: Exit"
    | Nav, Source -> "Up/Down: Navigate | Left: Up | Right: Enter | Space: Unfocus | Esc: Exit"
    | Search, JustName -> "Type to search | Up/Down: Navigate | Enter: Select | Space: Focus | Esc: Cancel"
    | Search, Source -> "Type to search | Up/Down: Navigate | Space: Unfocus | Esc: Cancel"
  Console.WriteLine(helpText)
  Console.ResetColor()

/// Move selection up
let moveUp (navState: NavState) : NavState =
  let totalItems = List.length navState.Items
  if totalItems > 0 then
    let newIndex =
      if navState.SelectedIndex > 0 then
        navState.SelectedIndex - 1
      else
        totalItems - 1

    let newScrollOffset =
      if newIndex < navState.ScrollOffset then
        newIndex
      else
        navState.ScrollOffset

    { navState with SelectedIndex = newIndex; ScrollOffset = newScrollOffset }
  else
    navState

/// Move selection down
let moveDown (navState: NavState) : NavState =
  let totalItems = List.length navState.Items
  if totalItems > 0 then
    let viewportHeight = 12
    let newIndex =
      if navState.SelectedIndex < totalItems - 1 then
        navState.SelectedIndex + 1
      else
        0

    let newScrollOffset =
      if newIndex >= navState.ScrollOffset + viewportHeight then
        newIndex - viewportHeight + 1
      else
        navState.ScrollOffset

    { navState with SelectedIndex = newIndex; ScrollOffset = newScrollOffset }
  else
    navState

/// Toggle display mode
let toggleDisplay (navState: NavState) : NavState =
  let newDisplay =
    match navState.Display with
    | JustName -> Source
    | Source -> JustName
  { navState with Display = newDisplay }

/// Perform search and update items
let performSearch (navState: NavState) : NavState =
  if String.IsNullOrEmpty(navState.SearchQuery) then
    { navState with Items = navState.AllItems; SelectedIndex = 0; ScrollOffset = 0 }
  else
    // Filter items by search query
    let filteredItems =
      navState.AllItems
      |> List.filter (fun item ->
        item.Name.Contains(navState.SearchQuery, StringComparison.OrdinalIgnoreCase))
    { navState with Items = filteredItems; SelectedIndex = 0; ScrollOffset = 0 }

/// Navigate into a module
let navigateIntoModule (navState: NavState) : NavState option =
  match List.tryItem navState.SelectedIndex navState.Items with
  | Some selectedItem when selectedItem.EntityType = "module" ->
    Some (buildState navState.AccountID navState.BranchID selectedItem.Location)
  | _ -> None

/// Navigate to parent
let navigateToParent (navState: NavState) : NavState option =
  let currentPath = modulePathOf navState.CurrentLocation
  match currentPath with
  | [] -> None // Already at root
  | _ ->
    let parentPath = List.take (List.length currentPath - 1) currentPath
    Some (buildState navState.AccountID navState.BranchID (Module parentPath))

/// Result of running interactive mode
type InteractiveResult =
  | ExitNoChange
  | ExitWithLocation of PackageLocation

/// Run the interactive navigation loop
let run (accountID: Guid option) (branchID: Guid option) (startLocation: PackageLocation) : InteractiveResult =
  Terminal.enterAlternateScreen ()

  let mutable navState = buildState accountID branchID startLocation
  let mutable isDone = false
  let mutable result = ExitNoChange

  while not isDone do
    display navState
    let key = Console.ReadKey(true)

    match navState.Mode with
    | Nav ->
      match key.Key with
      | ConsoleKey.Escape ->
        isDone <- true
        result <- ExitNoChange

      | ConsoleKey.UpArrow ->
        navState <- moveUp navState

      | ConsoleKey.DownArrow ->
        navState <- moveDown navState

      | ConsoleKey.LeftArrow ->
        match navigateToParent navState with
        | Some newState -> navState <- newState
        | None -> ()

      | ConsoleKey.RightArrow ->
        match navigateIntoModule navState with
        | Some newState -> navState <- newState
        | None -> ()

      | ConsoleKey.Enter ->
        match List.tryItem navState.SelectedIndex navState.Items with
        | Some selectedItem ->
          isDone <- true
          result <- ExitWithLocation selectedItem.Location
        | None -> ()

      | ConsoleKey.Spacebar ->
        navState <- toggleDisplay navState

      | _ when key.KeyChar = '/' ->
        navState <- { navState with Mode = Search; SearchQuery = "" }

      | _ -> ()

    | Search ->
      match key.Key with
      | ConsoleKey.Escape ->
        navState <- { navState with Mode = Nav; SearchQuery = ""; Items = navState.AllItems; SelectedIndex = 0; ScrollOffset = 0 }

      | ConsoleKey.UpArrow ->
        navState <- moveUp navState

      | ConsoleKey.DownArrow ->
        navState <- moveDown navState

      | ConsoleKey.Enter ->
        match List.tryItem navState.SelectedIndex navState.Items with
        | Some selectedItem ->
          isDone <- true
          result <- ExitWithLocation selectedItem.Location
        | None -> ()

      | ConsoleKey.Spacebar ->
        navState <- toggleDisplay navState

      | ConsoleKey.Backspace ->
        if navState.SearchQuery.Length > 0 then
          navState <- { navState with SearchQuery = navState.SearchQuery.Substring(0, navState.SearchQuery.Length - 1) }
          navState <- performSearch navState

      | _ when not (Char.IsControl(key.KeyChar)) ->
        navState <- { navState with SearchQuery = navState.SearchQuery + string key.KeyChar }
        navState <- performSearch navState

      | _ -> ()

  Terminal.exitAlternateScreen ()
  Terminal.showCursor ()
  result
