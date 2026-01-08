/// Instance command - manage package instances
module Cli2.Commands.Instance

open System
open Cli2.Types
open Cli2.Commands.ICommand

module Instances = LibPackageManager.Instances

type InstanceCommand() =
  inherit CommandBase()

  override _.Name = "instance"
  override _.Description = "Manage package instances"
  override _.Aliases = [ "inst" ]

  override _.Execute state args =
    match args with
    | [] | [ "list" ] ->
      // List instances
      let instances = Instances.list().Result
      let lines = ResizeArray<string>()
      lines.Add("Known Darklang Instances:")
      lines.Add(String.replicate 40 "-")
      lines.Add("")

      if List.isEmpty instances then
        lines.Add("  No instances registered.")
        lines.Add("")
        lines.Add("  Use 'instance add <name> <url>' to register an instance.")
      else
        for instance in instances do
          lines.Add($"  {instance.name}")
          lines.Add($"    URL: {instance.url}")
          lines.Add($"    ID:  {instance.id}")
          lines.Add("")

      lines.Add("")
      let output = CommandOutput.lines (lines |> Seq.toList)
      (output, StateUpdate state)

    | [ "add"; name; url ] ->
      // Check if instance already exists
      let existingOpt = Instances.getByName(name).Result
      match existingOpt with
      | Some _ ->
        let output = CommandOutput.error $"Instance '{name}' already exists."
        (output, StateUpdate state)
      | None ->
        let instance = (Instances.add name url).Result
        let output = CommandOutput.lines [
          $"Added instance: {name}"
          $"URL: {url}"
          $"ID: {instance.id}"
        ]
        (output, StateUpdate state)

    | [ "remove"; name ] ->
      let instanceOpt = Instances.getByName(name).Result
      match instanceOpt with
      | None ->
        let output = CommandOutput.error $"Instance '{name}' not found."
        (output, StateUpdate state)
      | Some instance ->
        Instances.remove(instance.id).Result
        let output = CommandOutput.lines [ $"Removed instance: {name}" ]
        (output, StateUpdate state)

    | [ "info"; name ] ->
      let instanceOpt = Instances.getByName(name).Result
      match instanceOpt with
      | None ->
        let output = CommandOutput.error $"Instance '{name}' not found."
        (output, StateUpdate state)
      | Some instance ->
        let output = CommandOutput.lines [
          $"Instance: {instance.name}"
          $"URL: {instance.url}"
          $"ID: {instance.id}"
        ]
        (output, StateUpdate state)

    | [ "sync" ] | [ "sync"; _ ] ->
      let targetName =
        match args with
        | [ "sync"; name ] -> Some name
        | _ -> None

      let output = CommandOutput.lines [
        "Sync Package Operations"
        ""
        match targetName with
        | Some name -> $"Would sync with instance: {name}"
        | None -> "Would sync with all registered instances"
        ""
        "(Instance sync not yet fully implemented)"
        ""
        "This will synchronize package operations between instances."
      ]
      (output, StateUpdate state)

    | _ ->
      let output = CommandOutput.error "Usage: instance [list|add|remove|info|sync] [args]"
      (output, StateUpdate state)

  override _.Help() =
    [ "Usage: instance [command] [args]"
      "Manage Darklang instances for package synchronization."
      ""
      "Commands:"
      "  (none)              List all registered instances"
      "  list                List all registered instances"
      "  add <name> <url>    Register a new instance"
      "  remove <name>       Unregister an instance"
      "  info <name>         Show instance details"
      "  sync [name]         Sync packages with instance(s)"
      ""
      "Examples:"
      "  instance                       - List all instances"
      "  instance add prod https://...  - Register production instance"
      "  instance remove prod           - Unregister instance"
      "  instance sync prod             - Sync with production" ]

let command = InstanceCommand() :> ICommand
