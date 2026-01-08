/// Eval command - evaluate a Dark expression
module Cli2.Commands.Eval

open System
open System.Threading.Tasks
open FSharp.Control.Tasks
open Prelude
open Cli2.Types
open Cli2.Commands.ICommand

module RT = LibExecution.RuntimeTypes
module PT = LibExecution.ProgramTypes
module PT2RT = LibExecution.ProgramTypesToRuntimeTypes
module Exe = LibExecution.Execution
module PM = LibPackageManager.PackageManager
module Parser = LibParser.Parser
module NR = LibParser.NameResolver

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

/// Evaluate a Darklang expression
let evaluateExpression
  (accountID: Guid option)
  (branchID: Guid option)
  (code: string)
  : Task<Result<string, string>> =
  task {
    try
      let pm = PM.rt
      do! pm.init

      // Parse the expression
      let! ptExpr = Parser.parseSimple accountID branchID builtins PM.pt NR.OnMissing.ThrowError "repl" code

      // Convert to runtime types
      let rtInstrs = PT2RT.Handler.toRT Map.empty ptExpr

      // Execute
      let state = createExecutionState pm
      let! result = Exe.executeExpr state rtInstrs

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

    with ex ->
      return Error $"Parse error: {ex.Message}"
  }

type EvalCommand() =
  inherit CommandBase()

  override _.Name = "eval"
  override _.Description = "Evaluate a Dark expression"

  override _.Execute state args =
    if List.isEmpty args then
      let output = CommandOutput.error "Usage: eval <expression>"
      (output, StateUpdate state)
    else
      let expression = String.Join(" ", args)
      let result = evaluateExpression state.AccountID state.CurrentBranchID expression
      match result.Result with
      | Ok outputStr ->
        let output = CommandOutput.lines [
          outputStr
        ]
        (output, StateUpdate state)
      | Error errorStr ->
        let output = CommandOutput.error errorStr
        (output, StateUpdate state)

  override _.Help() =
    [ "Usage: eval <expression>"
      "Evaluate a Dark expression and display the result."
      ""
      "Examples:"
      "  eval 1L + 2L"
      "  eval Stdlib.String.toUppercase \"hello\""
      "  eval [1L; 2L; 3L] |> Stdlib.List.map (fun x -> x * 2L)" ]

let command = EvalCommand() :> ICommand
