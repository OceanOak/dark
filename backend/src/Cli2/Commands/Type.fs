/// Type command - create a new type
module Cli2.Commands.Type

open System
open System.Threading.Tasks
open FSharp.Control.Tasks
open Prelude
open Cli2.Types
open Cli2.Commands.ICommand
open Cli2.PackageUtils

module RT = LibExecution.RuntimeTypes
module PT = LibExecution.ProgramTypes
module PM = LibPackageManager.PackageManager
module Package = LibParser.Package
module NR = LibParser.NameResolver
module Inserts = LibPackageManager.Inserts

/// Build the builtins available for CLI execution
let builtins : RT.Builtins =
  LibExecution.Builtin.combine
    [ BuiltinCliHost.Libs.Cli.builtinsToUse
      BuiltinCliHost.Builtin.builtins
      BuiltinCli.Builtin.builtins ]
    []

/// Create a new package type by wrapping in module syntax and parsing
let createType
  (accountID: Guid option)
  (branchID: Guid option)
  (owner: string)
  (modules: string list)
  (definition: string)
  : Task<Result<string, string>> =
  task {
    try
      let pm = PM.rt
      do! pm.init

      // Build the module path
      let modulesSuffix = String.Join(".", modules)
      let modulePath =
        match modules with
        | [] -> owner
        | _ -> $"{owner}.{modulesSuffix}"

      // Wrap in module syntax: "module Owner.Module\ntype Name = definition"
      let code = $"module {modulePath}\ntype {definition}"

      // Parse using the package parser
      let! ops = Package.parse accountID branchID builtins PM.pt NR.OnMissing.ThrowError "cli-type" code

      if List.isEmpty ops then
        return Error "No type was parsed from the definition"
      else
        // Insert and apply the ops
        let! insertedCount = Inserts.insertAndApplyOps None branchID accountID ops

        if insertedCount > 0L then
          return Ok $"Created type in {modulePath}"
        else
          return Ok "Type already exists (no changes made)"

    with ex ->
      return Error $"Error: {ex.Message}"
  }

type TypeCommand() =
  inherit CommandBase()

  override _.Name = "type"
  override _.Description = "Create a new package type"
  override _.Aliases = [ "typedef" ]

  override _.Execute state args =
    match args with
    | [] ->
      let output = CommandOutput.error "Usage: type <name> = <definition>"
      (output, StateUpdate state)

    | _ ->
      let fullText = String.Join(" ", args)
      let currentPath = modulePathOf state.PackageData.CurrentLocation

      // Need at least an owner (first element of path)
      match currentPath with
      | [] ->
        let output = CommandOutput.error "Cannot create types at root. Navigate to a module first (e.g., 'nav Darklang')"
        (output, StateUpdate state)

      | owner :: modules ->
        let result = createType state.AccountID state.CurrentBranchID owner modules fullText
        match result.Result with
        | Ok msg ->
          let output = CommandOutput.line msg
          (output, StateUpdate state)
        | Error err ->
          let output = CommandOutput.error err
          (output, StateUpdate state)

  override _.Help() =
    [ "Usage: type <name> = <definition>"
      "Create a new package type in the current module."
      ""
      "Record types:"
      "  type Person = { name: String; age: Int64 }"
      ""
      "Enum types:"
      "  type Color = | Red | Green | Blue"
      "  type Option = | Some of value: 'a | None"
      ""
      "Alias types:"
      "  type UserId = Int64"
      ""
      "Types become part of your package and can be"
      "used by functions and values."
      ""
      "Note: You must be in a module (not at root) to create types."
      "Use 'nav' to navigate to a module first." ]

let command = TypeCommand() :> ICommand
