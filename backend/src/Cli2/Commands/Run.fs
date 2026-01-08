/// Run command - execute Darklang code
module Cli2.Commands.Run

open System
open System.Threading.Tasks
open FSharp.Control.Tasks
open Prelude
open Cli2.Types
open Cli2.Commands.ICommand
open Cli2.PackageUtils

module RT = LibExecution.RuntimeTypes
module PT = LibExecution.ProgramTypes
module Dval = LibExecution.Dval
module Exe = LibExecution.Execution
module PM = LibPackageManager.PackageManager

/// Build the builtins available for CLI execution
let builtins : RT.Builtins =
  LibExecution.Builtin.combine
    [ BuiltinCliHost.Libs.Cli.builtinsToUse
      BuiltinCliHost.Builtin.builtins
      BuiltinCli.Builtin.builtins ]
    []

/// Create execution state for running code
let createExecutionState (packageManager: RT.PackageManager) : RT.ExecutionState =
  let program : RT.Program =
    { canvasID = System.Guid.NewGuid()
      internalFnsAllowed = false
      dbs = Map.empty
      secrets = [] }

  let notify
    (_state: RT.ExecutionState)
    (_vm: RT.VMState)
    (_msg: string)
    (_metadata: Metadata)
    =
    uply { return () }

  let sendException
    (_: RT.ExecutionState)
    (_: RT.VMState)
    (metadata: Metadata)
    (exn: exn)
    =
    uply { printException "Internal error" metadata exn }

  Exe.createState builtins packageManager Exe.noTracing sendException notify program

/// Execute a function by name
let executeFunction
  (fnLocation: PackageLocation)
  (args: string list)
  : Task<Result<string, string>> =
  task {
    let pm = PM.rt
    do! pm.init

    match fnLocation with
    | Function(owner, modules, name) ->
      let ptLoc : PT.PackageLocation = { owner = owner; modules = modules; name = name }
      let! fnIdOpt = PM.pt.findFn(None, None, ptLoc)

      match fnIdOpt with
      | None ->
        return Error $"Function not found: {PackageLocation.format fnLocation}"
      | Some fnId ->
        let state = createExecutionState pm
        let fnName = RT.FQFnName.Package fnId

        // Convert string args to individual Dval arguments
        let dvalArgs =
          match args with
          | [] ->
            // No args - pass unit
            NEList.singleton RT.DUnit
          | first :: rest ->
            // Pass each string as a separate argument
            NEList.ofList (RT.DString first) (List.map RT.DString rest)

        let! result = Exe.executeFunction state fnName [] dvalArgs

        match result with
        | Ok dval ->
          let output = DvalReprDeveloper.toRepr dval
          return Ok output
        | Error(rte, callStack) ->
          let! callStackStr = Exe.callStackString state callStack
          let! errorStr = Exe.runtimeErrorToString None None state rte
          match errorStr with
          | Ok (RT.DString s) ->
            return Error $"Runtime error: {s}\n{callStackStr}"
          | _ ->
            return Error $"Runtime error (could not stringify)\n{callStackStr}"

    | _ ->
      return Error "Can only run functions, not types, values, or modules"
  }

type RunCommand() =
  inherit CommandBase()

  override _.Name = "run"
  override _.Description = "Execute a Darklang function"

  override _.Execute state args =
    match args with
    | [] ->
      let output = CommandOutput.error "Usage: run <function> [args...]"
      (output, StateUpdate state)

    | fnPath :: fnArgs ->
      match traverse state.AccountID state.CurrentBranchID state.PackageData.CurrentLocation fnPath with
      | Error errorMsg ->
        let output = CommandOutput.error $"Cannot find function: {errorMsg}"
        (output, StateUpdate state)
      | Ok location ->
        match location with
        | Function _ ->
          let result = executeFunction location fnArgs
          match result.Result with
          | Ok outputStr ->
            let output = CommandOutput.lines [
              "Result:"
              outputStr
            ]
            (output, StateUpdate state)
          | Error errorStr ->
            let output = CommandOutput.error errorStr
            (output, StateUpdate state)
        | _ ->
          let output = CommandOutput.error $"'{fnPath}' is not a function. Use 'view' to see its contents."
          (output, StateUpdate state)

  override _.Complete state args =
    match args with
    | [] ->
      completePartialPath state.AccountID state.CurrentBranchID state.PackageData.CurrentLocation ""
    | [ partialPath ] ->
      completePartialPath state.AccountID state.CurrentBranchID state.PackageData.CurrentLocation partialPath
    | _ -> []

  override _.Help() =
    [ "Usage: run <function> [args...]"
      "Execute a Darklang function."
      ""
      "Arguments are passed as strings to the function."
      ""
      "Examples:"
      "  run Stdlib.String.length \"hello\""
      "  run myPackage.Utils.greet \"World\""
      "  run Darklang.Stdlib.List.head" ]

let command = RunCommand() :> ICommand
