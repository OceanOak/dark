/// Main entry point for the CLI
/// Uses console I/O for main prompt (like Darklang CLI)
/// Terminal.Gui reserved for interactive navigation mode
module Cli2.Program

open System

open Cli2.Types
open Cli2.Update
open Cli2.View
open Cli2.Commands.ICommand

module PM = LibPackageManager.PackageManager

/// Print command output to console
let printOutput (output: CommandOutput) =
  if output.IsError then
    Console.ForegroundColor <- ConsoleColor.Red

  for line in output.Lines do
    Console.WriteLine(line)

  if output.IsError then
    Console.ResetColor()

/// Simple readline with history support
let readLineWithHistory (prompt: string) (history: string list) : string * string list =
  Console.Write(prompt)

  let mutable input = ""
  let mutable historyIndex = -1
  let mutable savedInput = ""
  let mutable cursorPos = 0
  let mutable isDone = false

  while not isDone do
    let key = Console.ReadKey(true)

    match key.Key with
    | ConsoleKey.Enter ->
      Console.WriteLine()
      isDone <- true

    | ConsoleKey.Backspace when cursorPos > 0 ->
      input <- input.Remove(cursorPos - 1, 1)
      cursorPos <- cursorPos - 1
      Console.Write("\r" + prompt + input + " ")
      Console.SetCursorPosition(prompt.Length + cursorPos, Console.CursorTop)

    | ConsoleKey.UpArrow ->
      if historyIndex < history.Length - 1 then
        if historyIndex = -1 then savedInput <- input
        historyIndex <- historyIndex + 1
        input <- history[historyIndex]
        cursorPos <- input.Length
        Console.Write("\r" + prompt + input + "          ")
        Console.Write("\r" + prompt + input)

    | ConsoleKey.DownArrow ->
      if historyIndex >= 0 then
        historyIndex <- historyIndex - 1
        input <- if historyIndex = -1 then savedInput else history[historyIndex]
        cursorPos <- input.Length
        Console.Write("\r" + prompt + input + "          ")
        Console.Write("\r" + prompt + input)

    | ConsoleKey.LeftArrow when cursorPos > 0 ->
      cursorPos <- cursorPos - 1
      Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop)

    | ConsoleKey.RightArrow when cursorPos < input.Length ->
      cursorPos <- cursorPos + 1
      Console.SetCursorPosition(Console.CursorLeft + 1, Console.CursorTop)

    | ConsoleKey.Tab ->
      let completions = Commands.Registry.getCompletions (Model.init()) input
      match completions with
      | [ single ] ->
        input <- single + " "
        cursorPos <- input.Length
        Console.Write("\r" + prompt + input)
      | multiple when not (List.isEmpty multiple) ->
        Console.WriteLine()
        let joined = String.Join(" ", multiple)
        Console.WriteLine(joined)
        Console.Write(prompt + input)
      | _ -> ()

    | _ when not (Char.IsControl(key.KeyChar)) ->
      input <- input.Insert(cursorPos, string key.KeyChar)
      cursorPos <- cursorPos + 1
      Console.Write("\r" + prompt + input)
      if cursorPos < input.Length then
        Console.SetCursorPosition(prompt.Length + cursorPos, Console.CursorTop)

    | _ -> ()

  let newHistory =
    if String.IsNullOrWhiteSpace(input) then history
    elif history.Length > 0 && history[0] = input then history
    else input :: history

  (input, newHistory)

/// Run the interactive loop (MVU style)
let rec runLoop (model: Model) : int =
  if model.IsExiting then
    0
  else
    let prompt = formatPromptPrefix model
    let (input, newHistory) = readLineWithHistory prompt model.Prompt.CommandHistory

    if String.IsNullOrWhiteSpace(input) then
      runLoop model
    else
      let newPrompt = { model.Prompt with CommandHistory = newHistory }
      let modelWithHistory = { model with Prompt = newPrompt }

      let (newModel, outputOpt) = processInput input modelWithHistory

      match outputOpt with
      | Some output -> printOutput output
      | None -> ()

      runLoop newModel

/// Run the interactive CLI
let runInteractive () : int =
  // Initialize the package manager
  PM.rt.init.Result

  let model = Model.init ()

  // Print welcome message
  for line in formatWelcome model.AccountName do
    Console.WriteLine(line)
  Console.WriteLine()

  runLoop model

/// Execute a single command non-interactively
let executeNonInteractive (args: string list) : int =
  // Initialize the package manager
  PM.rt.init.Result

  let model = Model.init ()
  let command = String.Join(" ", args)

  match parseInput command with
  | None ->
    printfn "No command provided"
    1
  | Some(name, cmdArgs) ->
    let (output, result) = Commands.Registry.executeCommand name model cmdArgs
    printOutput output

    match result with
    | CommandResult.StateUpdate _ -> 0
    | CommandResult.Exit code -> code

[<EntryPoint>]
let main (argv: string[]) : int =
  let args = Array.toList argv

  match args with
  | [] ->
    // No args - run interactive mode
    runInteractive ()
  | "--help" :: _
  | "-h" :: _ ->
    printfn "Darklang CLI"
    printfn ""
    printfn "Usage:"
    printfn "  dark              Start interactive mode"
    printfn "  dark <command>    Execute a command and exit"
    printfn "  dark --help       Show this help"
    printfn ""
    printfn "Commands:"
    for line in Commands.Registry.formatCompactCommandList () do
      printfn "  %s" line
    0
  | _ ->
    // Execute command directly
    executeNonInteractive args
