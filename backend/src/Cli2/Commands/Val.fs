/// Val command - create a new value
module Cli2.Commands.Val

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

/// Create a new package value by wrapping in module syntax and parsing
let createValue
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

      // Wrap in module syntax: "module Owner.Module\nlet name = expr"
      let code = $"module {modulePath}\nlet {definition}"

      // Parse using the package parser
      let! ops = Package.parse accountID branchID builtins PM.pt NR.OnMissing.ThrowError "cli-value" code

      if List.isEmpty ops then
        return Error "No value was parsed from the definition"
      else
        // Insert and apply the ops
        let! insertedCount = Inserts.insertAndApplyOps None branchID accountID ops

        if insertedCount > 0L then
          return Ok $"Created value in {modulePath}"
        else
          return Ok "Value already exists (no changes made)"

    with ex ->
      return Error $"Error: {ex.Message}"
  }

type ValCommand() =
  inherit CommandBase()

  override _.Name = "val"
  override _.Description = "Create a new package value"
  override _.Aliases = [ "value" ]

  override _.Execute state args =
    match args with
    | [] ->
      let output = CommandOutput.error "Usage: val <name> = <expression>"
      (output, StateUpdate state)

    | _ ->
      let fullText = String.Join(" ", args)
      let currentPath = modulePathOf state.PackageData.CurrentLocation

      // Need at least an owner (first element of path)
      match currentPath with
      | [] ->
        let output = CommandOutput.error "Cannot create values at root. Navigate to a module first (e.g., 'nav Darklang')"
        (output, StateUpdate state)

      | owner :: modules ->
        let result = createValue state.AccountID state.CurrentBranchID owner modules fullText
        match result.Result with
        | Ok msg ->
          let output = CommandOutput.line msg
          (output, StateUpdate state)
        | Error err ->
          let output = CommandOutput.error err
          (output, StateUpdate state)

  override _.Help() =
    [ "Usage: val <name> = <expression>"
      "Create a new package value in the current module."
      ""
      "Examples:"
      "  val pi = 3.14159"
      "  val greeting = \"Hello, World!\""
      "  val numbers = [1L; 2L; 3L; 4L; 5L]"
      ""
      "Values are immutable constants that can be referenced"
      "by other code in the package."
      ""
      "Note: You must be in a module (not at root) to create values."
      "Use 'nav' to navigate to a module first." ]

let command = ValCommand() :> ICommand
