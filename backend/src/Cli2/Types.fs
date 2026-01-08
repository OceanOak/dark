/// Core types for the CLI application
module Cli2.Types

open System

/// Package location - where we are in the package hierarchy
type PackageLocation =
  | Module of path: string list
  | Type of owner: string * modules: string list * name: string
  | Value of owner: string * modules: string list * name: string
  | Function of owner: string * modules: string list * name: string

module PackageLocation =
  let root = Module []

  let format (loc: PackageLocation) : string =
    match loc with
    | Module [] -> "/"
    | Module path -> String.Join(".", path)
    | Type(owner, modules, name) ->
      let modulePath =
        if List.isEmpty modules then ""
        else String.Join(".", modules) + "."
      $"{owner}.{modulePath}{name}"
    | Value(owner, modules, name) ->
      let modulePath =
        if List.isEmpty modules then ""
        else String.Join(".", modules) + "."
      $"{owner}.{modulePath}{name}"
    | Function(owner, modules, name) ->
      let modulePath =
        if List.isEmpty modules then ""
        else String.Join(".", modules) + "."
      $"{owner}.{modulePath}{name}"

/// Prompt state for text input
type PromptState =
  { Text: string
    CursorPosition: int
    CommandHistory: string list
    HistoryIndex: int
    SavedPrompt: string }

module PromptState =
  let init () =
    { Text = ""
      CursorPosition = 0
      CommandHistory = []
      HistoryIndex = -1
      SavedPrompt = "" }

/// Package navigation state
type PackageState =
  { CurrentLocation: PackageLocation
    LocationHistory: PackageLocation list }

module PackageState =
  let init () =
    { CurrentLocation = PackageLocation.root
      LocationHistory = [] }

/// Current page/view of the application
type Page =
  | MainPrompt
  | InteractiveNav
  | Experiments

/// Main application state
type Model =
  { IsExiting: bool
    Prompt: PromptState
    NeedsFullRedraw: bool
    PackageData: PackageState
    CurrentPage: Page
    AccountName: string
    AccountID: Guid option
    CurrentBranchID: Guid option }

module Model =
  let init () =
    let accountName =
      Environment.GetEnvironmentVariable("DARK_ACCOUNT")
      |> Option.ofObj
      |> Option.defaultValue "Darklang"

    let branchId =
      Environment.GetEnvironmentVariable("DARK_BRANCH")
      |> Option.ofObj
      |> Option.bind (fun s ->
        match Guid.TryParse(s) with
        | true, guid -> Some guid
        | false, _ -> None)

    { IsExiting = false
      Prompt = PromptState.init ()
      NeedsFullRedraw = true
      PackageData = PackageState.init ()
      CurrentPage = MainPrompt
      AccountName = accountName
      AccountID = None
      CurrentBranchID = branchId }
