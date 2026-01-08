/// Update module - handles state transitions
module Cli2.Update

open Cli2.Types
open Cli2.Commands.ICommand

/// Parse input into command and arguments
let parseInput (input: string) : (string * string list) option =
  let trimmed = input.Trim()
  if System.String.IsNullOrWhiteSpace(trimmed) then
    None
  else
    let parts = trimmed.Split(' ') |> Array.toList
    match parts with
    | [] -> None
    | cmd :: args -> Some(cmd, args)

/// Add command to history
let addToHistory (command: string) (prompt: PromptState) : PromptState =
  let updatedHistory =
    match prompt.CommandHistory with
    | head :: _ when head = command -> prompt.CommandHistory
    | _ -> command :: prompt.CommandHistory

  { prompt with
      CommandHistory = updatedHistory
      HistoryIndex = -1 }

/// Clear the prompt
let clearPrompt (prompt: PromptState) : PromptState =
  { prompt with
      Text = ""
      CursorPosition = 0
      HistoryIndex = -1 }

/// Execute a command and return the new state
let executeCommand (name: string) (args: string list) (model: Model) : Model * CommandOutput =
  let (output, result) = Commands.Registry.executeCommand name model args

  match result with
  | CommandResult.StateUpdate newState ->
    let argsJoined = System.String.Join(" ", args)
    let commandStr = $"{name} {argsJoined}".Trim()
    let prompt =
      model.Prompt
      |> addToHistory commandStr
      |> clearPrompt
    ({ newState with Prompt = prompt; NeedsFullRedraw = true }, output)
  | CommandResult.Exit _code ->
    ({ model with IsExiting = true }, output)

/// Process text input and potentially execute command
let processInput (input: string) (model: Model) : Model * CommandOutput option =
  match parseInput input with
  | None -> (model, None)
  | Some(name, args) ->
    let (newModel, output) = executeCommand name args model
    (newModel, Some output)

/// Navigate to a location
let navigateTo (location: PackageLocation) (model: Model) : Model =
  let newPackageData =
    { model.PackageData with
        CurrentLocation = location
        LocationHistory = model.PackageData.CurrentLocation :: model.PackageData.LocationHistory }
  { model with PackageData = newPackageData }

/// Set the current page
let setPage (page: Page) (model: Model) : Model =
  { model with CurrentPage = page; NeedsFullRedraw = true }

/// Mark the app as exiting
let exit (model: Model) : Model =
  { model with IsExiting = true }
