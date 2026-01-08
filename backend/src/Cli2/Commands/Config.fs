/// Config command - manage CLI configuration
module Cli2.Commands.Config

open System
open System.IO
open System.Text.Json
open Cli2.Types
open Cli2.Commands.ICommand

module ConfigFile =
  let configFilePath () =
    let homeDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
    let darklangDir = Path.Combine(homeDir, ".darklang")
    Path.Combine(darklangDir, "cli-config.json")

  let readConfig () : Map<string, string> =
    let path = configFilePath ()
    if File.Exists(path) then
      try
        let content = File.ReadAllText(path)
        let dict = JsonSerializer.Deserialize<System.Collections.Generic.Dictionary<string, string>>(content)
        dict |> Seq.map (fun kvp -> kvp.Key, kvp.Value) |> Map.ofSeq
      with _ -> Map.empty
    else
      Map.empty

  let writeConfig (config: Map<string, string>) =
    let path = configFilePath ()
    let dir = Path.GetDirectoryName(path)
    if not (Directory.Exists(dir)) then
      Directory.CreateDirectory(dir) |> ignore
    let dict = config |> Map.toSeq |> dict
    let json = JsonSerializer.Serialize(dict, JsonSerializerOptions(WriteIndented = true))
    File.WriteAllText(path, json)

type ConfigCommand() =
  inherit CommandBase()

  override _.Name = "config"
  override _.Description = "Manage CLI configuration"

  override _.Execute state args =
    match args with
    | [ "get"; key ] ->
      let config = ConfigFile.readConfig ()
      match Map.tryFind key config with
      | Some value ->
        let output = CommandOutput.line $"{key} = {value}"
        (output, StateUpdate state)
      | None ->
        let output = CommandOutput.error $"Configuration key not found: {key}"
        (output, StateUpdate state)

    | [ "set"; key; value ] ->
      let config = ConfigFile.readConfig ()
      let updatedConfig = Map.add key value config
      ConfigFile.writeConfig updatedConfig
      let output = CommandOutput.line $"Set {key} = {value}"
      (output, StateUpdate state)

    | [ "list" ] ->
      let output = CommandOutput.lines [
        "Common configuration keys:"
        ""
        "  sync.default_instance    - Default instance for sync service"
        "  sync.interval_seconds    - Sync check interval (default: 20)"
        "  sync.auto_start          - Auto-start sync service (default: true)"
        ""
        "Use 'config get <key>' to read a value"
        "Use 'config set <key> <value>' to set a value"
      ]
      (output, StateUpdate state)

    | _ ->
      let output = CommandOutput.error "Invalid arguments. Use: config get <key> | config set <key> <value> | config list"
      (output, StateUpdate state)

  override _.Complete _state args =
    match args with
    | [] -> [ "get"; "set"; "list" ]
    | [ "get" ] | [ "set" ] ->
      [ "sync.default_instance"
        "sync.interval_seconds"
        "sync.auto_start" ]
    | _ -> []

  override _.Help() =
    [ "Usage:"
      "  config get <key>           - Get a configuration value"
      "  config set <key> <value>   - Set a configuration value"
      "  config list                - List common configuration keys"
      ""
      "Examples:"
      "  config set sync.default_instance local"
      "  config set sync.interval_seconds 60"
      "  config get sync.default_instance" ]

let command = ConfigCommand() :> ICommand
