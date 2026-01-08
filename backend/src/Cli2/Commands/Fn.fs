/// Fn command - create a new function
module Cli2.Commands.Fn

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

/// Create a new package function by wrapping in module syntax and parsing
let createFunction
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

      // Wrap in module syntax: "module Owner.Module\nlet name params = body"
      let code = $"module {modulePath}\nlet {definition}"

      // Parse using the package parser
      let! ops = Package.parse accountID branchID builtins PM.pt NR.OnMissing.ThrowError "cli-fn" code

      if List.isEmpty ops then
        return Error "No function was parsed from the definition"
      else
        // Insert and apply the ops
        let! insertedCount = Inserts.insertAndApplyOps None branchID accountID ops

        if insertedCount > 0L then
          return Ok $"Created function in {modulePath}"
        else
          return Ok "Function already exists (no changes made)"

    with ex ->
      return Error $"Error: {ex.Message}"
  }

type FnCommand() =
  inherit CommandBase()

  override _.Name = "fn"
  override _.Description = "Create a new package function"
  override _.Aliases = [ "function"; "def" ]

  override _.Execute state args =
    match args with
    | [] ->
      let output = CommandOutput.error "Usage: fn <name> (<params>) : <returnType> = <body>"
      (output, StateUpdate state)

    | _ ->
      let fullText = String.Join(" ", args)
      let currentPath = modulePathOf state.PackageData.CurrentLocation

      // Need at least an owner (first element of path)
      match currentPath with
      | [] ->
        let output = CommandOutput.error "Cannot create functions at root. Navigate to a module first (e.g., 'nav Darklang')"
        (output, StateUpdate state)

      | owner :: modules ->
        let result = createFunction state.AccountID state.CurrentBranchID owner modules fullText
        match result.Result with
        | Ok msg ->
          let output = CommandOutput.line msg
          (output, StateUpdate state)
        | Error err ->
          let output = CommandOutput.error err
          (output, StateUpdate state)

  override _.Help() =
    [ "Usage: fn <name> (<params>) : <returnType> = <body>"
      "Create a new package function in the current module."
      ""
      "Examples:"
      "  fn add (x: Int64) (y: Int64) : Int64 = x + y"
      "  fn greet (name: String) : String = \"Hello, \" ++ name"
      "  fn double (x: Int64) : Int64 = x * 2L"
      ""
      "Functions become part of your package and can be"
      "called by other code."
      ""
      "Note: You must be in a module (not at root) to create functions."
      "Use 'nav' to navigate to a module first." ]

let command = FnCommand() :> ICommand
