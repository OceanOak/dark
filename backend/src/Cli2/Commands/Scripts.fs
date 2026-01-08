/// Scripts command - manage and run scripts
module Cli2.Commands.Scripts

open System
open System.IO
open Cli2.Types
open Cli2.Commands.ICommand

module ScriptStorage =
  let scriptsDir () =
    let homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
    let darklangDir = Path.Combine(homeDir, ".darklang")
    Path.Combine(darklangDir, "scripts")

  let ensureDir () =
    let dir = scriptsDir ()
    if not (Directory.Exists(dir)) then
      Directory.CreateDirectory(dir) |> ignore
    dir

  let listScripts () =
    let dir = ensureDir ()
    if Directory.Exists(dir) then
      Directory.GetFiles(dir, "*.dark")
      |> Array.map Path.GetFileNameWithoutExtension
      |> Array.toList
    else
      []

  let getScriptPath (name: string) =
    let dir = ensureDir ()
    Path.Combine(dir, $"{name}.dark")

  let readScript (name: string) =
    let path = getScriptPath name
    if File.Exists(path) then
      Some (File.ReadAllText(path))
    else
      None

  let writeScript (name: string) (content: string) =
    let path = getScriptPath name
    File.WriteAllText(path, content)

  let deleteScript (name: string) =
    let path = getScriptPath name
    if File.Exists(path) then
      File.Delete(path)
      true
    else
      false

type ScriptsCommand() =
  inherit CommandBase()

  override _.Name = "scripts"
  override _.Description = "Manage and run scripts"

  override _.Execute state args =
    match args with
    | [] | [ "list" ] ->
      let scripts = ScriptStorage.listScripts ()
      if List.isEmpty scripts then
        let output = CommandOutput.lines [
          "No scripts found."
          ""
          "Use 'scripts add <name>' to create a new script."
        ]
        (output, StateUpdate state)
      else
        let lines = ResizeArray<string>()
        lines.Add("Available scripts:")
        for script in scripts do
          lines.Add($"  {script}")
        lines.Add("")
        lines.Add("Use 'scripts run <name>' to execute a script.")
        let output = CommandOutput.lines (lines |> Seq.toList)
        (output, StateUpdate state)

    | [ "view"; name ] ->
      match ScriptStorage.readScript name with
      | None ->
        let output = CommandOutput.error $"Script not found: {name}"
        (output, StateUpdate state)
      | Some content ->
        let lines = [
          $"Script: {name}"
          String.replicate 40 "-"
          content
        ]
        let output = CommandOutput.lines lines
        (output, StateUpdate state)

    | [ "run"; name ] ->
      match ScriptStorage.readScript name with
      | None ->
        let output = CommandOutput.error $"Script not found: {name}"
        (output, StateUpdate state)
      | Some _content ->
        // For now, show what would be run
        let output = CommandOutput.lines [
          $"Would run script: {name}"
          ""
          "(Script execution not yet implemented in F# CLI)"
        ]
        (output, StateUpdate state)

    | [ "delete"; name ] ->
      if ScriptStorage.deleteScript name then
        let output = CommandOutput.line $"Deleted script: {name}"
        (output, StateUpdate state)
      else
        let output = CommandOutput.error $"Script not found: {name}"
        (output, StateUpdate state)

    | _ ->
      let output = CommandOutput.error "Usage: scripts [list|view|run|delete] [name]"
      (output, StateUpdate state)

  override _.Complete _state args =
    match args with
    | [] -> [ "list"; "view"; "run"; "delete" ]
    | [ "view" ] | [ "run" ] | [ "delete" ] ->
      ScriptStorage.listScripts ()
    | [ "view"; partial ] | [ "run"; partial ] | [ "delete"; partial ] ->
      ScriptStorage.listScripts ()
      |> List.filter (fun name -> name.StartsWith(partial))
    | _ -> []

  override _.Help() =
    [ "Usage:"
      "  scripts                    - List all scripts"
      "  scripts list               - List all scripts"
      "  scripts view <name>        - View script contents"
      "  scripts run <name>         - Run a script"
      "  scripts delete <name>      - Delete a script"
      ""
      "Scripts are stored in ~/.darklang/scripts/"
      ""
      "Examples:"
      "  scripts list"
      "  scripts view myScript"
      "  scripts run myScript"
      "  scripts delete myScript" ]

let command = ScriptsCommand() :> ICommand
