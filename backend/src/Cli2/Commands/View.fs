/// View command - view detailed information about entities
module Cli2.Commands.View

open System
open System.Threading.Tasks
open FSharp.Control.Tasks
open Prelude
open Cli2.Types
open Cli2.Commands.ICommand
open Cli2.PackageUtils

module PT = LibExecution.ProgramTypes
module RT = LibExecution.RuntimeTypes
module PM = LibPackageManager.PackageManager
module Exe = LibExecution.Execution
module PT2DT = LibExecution.ProgramTypesToDarkTypes
module Dval = LibExecution.Dval

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

  let notify _ _ _ _ = uply { return () }
  let sendException _ _ (metadata: Metadata) (exn: exn) =
    uply { printException "Internal error" metadata exn }

  Exe.createState builtins packageManager Exe.noTracing sendException notify program

/// Create PrettyPrinter context Dval
let createPrettyPrinterContext
  (accountID: Guid option)
  (branchID: Guid option)
  (contextTypeId: Guid)
  : RT.Dval =
  let contextTypeName = RT.FQTypeName.Package contextTypeId

  // Build the context record matching PrettyPrinter.ProgramTypes.Context
  // currentFunction is Option<(String * Dict<Int64>)> - a tuple type
  let tupleType = RT.KTTuple(RT.ValueType.Known RT.KTString, RT.ValueType.Known (RT.KTDict (RT.ValueType.Known RT.KTInt64)), [])
  let fields = Map.ofList [
    ("accountID", PT2DT.AccountID.optionToDT accountID)
    ("branchID", PT2DT.BranchID.optionToDT branchID)
    ("currentModule", Dval.optionNone RT.KTString)
    ("currentFunction", Dval.optionNone tupleType)
  ]

  RT.DRecord(contextTypeName, contextTypeName, [], fields)

/// Pretty print a PackageFn using Darklang's PrettyPrinter
let prettyPrintFn
  (accountID: Guid option)
  (branchID: Guid option)
  (fn: PT.PackageFn.PackageFn)
  : Task<Result<string, string>> =
  task {
    try
      let pm = PM.rt
      do! pm.init

      // Look up the PrettyPrinter.ProgramTypes.packageFn function
      let prettyPrintLoc : PT.PackageLocation =
        { owner = "Darklang"
          modules = ["PrettyPrinter"; "ProgramTypes"]
          name = "packageFn" }

      // Look up the Context type
      let contextTypeLoc : PT.PackageLocation =
        { owner = "Darklang"
          modules = ["PrettyPrinter"; "ProgramTypes"]
          name = "Context" }

      let! fnIdOpt = PM.pt.findFn(accountID, branchID, prettyPrintLoc)
      let! contextTypeIdOpt = PM.pt.findType(accountID, branchID, contextTypeLoc)

      match fnIdOpt, contextTypeIdOpt with
      | None, _ ->
        return Error "PrettyPrinter.ProgramTypes.packageFn not found"
      | _, None ->
        return Error "PrettyPrinter.ProgramTypes.Context type not found"
      | Some fnId, Some contextTypeId ->
        let state = createExecutionState pm

        // Convert PackageFn to Dval
        let fnDval = PT2DT.PackageFn.toDT fn

        // Create context
        let ctxDval = createPrettyPrinterContext accountID branchID contextTypeId

        // Execute the pretty printer function
        let fnName = RT.FQFnName.Package fnId
        let args = NEList.ofList ctxDval [ fnDval ]

        let! result = Exe.executeFunction state fnName [] args

        match result with
        | Ok (RT.DString s) -> return Ok s
        | Ok dval ->
          let repr = DvalReprDeveloper.toRepr dval
          return Ok repr
        | Error(rte, callStack) ->
          let! callStackStr = Exe.callStackString state callStack
          let! errorStr = Exe.runtimeErrorToString accountID branchID state rte
          match errorStr with
          | Ok (RT.DString s) -> return Error $"Error: {s}\n{callStackStr}"
          | _ -> return Error $"Error (could not stringify)\n{callStackStr}"

    with ex ->
      return Error $"Exception: {ex.Message}"
  }

/// Pretty print a PackageType using Darklang's PrettyPrinter
let prettyPrintType
  (accountID: Guid option)
  (branchID: Guid option)
  (typ: PT.PackageType.PackageType)
  : Task<Result<string, string>> =
  task {
    try
      let pm = PM.rt
      do! pm.init

      let prettyPrintLoc : PT.PackageLocation =
        { owner = "Darklang"
          modules = ["PrettyPrinter"; "ProgramTypes"]
          name = "packageType" }

      let! fnIdOpt = PM.pt.findFn(accountID, branchID, prettyPrintLoc)

      match fnIdOpt with
      | None ->
        return Error "PrettyPrinter.ProgramTypes.packageType not found"
      | Some fnId ->
        let state = createExecutionState pm
        let typDval = PT2DT.PackageType.toDT typ
        let fnName = RT.FQFnName.Package fnId

        // packageType takes (accountID, branchID, typ)
        let args =
          NEList.ofList
            (PT2DT.AccountID.optionToDT accountID)
            [ PT2DT.BranchID.optionToDT branchID; typDval ]

        let! result = Exe.executeFunction state fnName [] args

        match result with
        | Ok (RT.DString s) -> return Ok s
        | Ok dval -> return Ok (DvalReprDeveloper.toRepr dval)
        | Error(_rte, callStack) ->
          let! callStackStr = Exe.callStackString state callStack
          return Error $"Error during pretty print\n{callStackStr}"

    with ex ->
      return Error $"Exception: {ex.Message}"
  }

/// Pretty print a PackageValue using Darklang's PrettyPrinter
let prettyPrintValue
  (accountID: Guid option)
  (branchID: Guid option)
  (value: PT.PackageValue.PackageValue)
  : Task<Result<string, string>> =
  task {
    try
      let pm = PM.rt
      do! pm.init

      let prettyPrintLoc : PT.PackageLocation =
        { owner = "Darklang"
          modules = ["PrettyPrinter"; "ProgramTypes"]
          name = "packageValue" }

      // Look up the Context type
      let contextTypeLoc : PT.PackageLocation =
        { owner = "Darklang"
          modules = ["PrettyPrinter"; "ProgramTypes"]
          name = "Context" }

      let! fnIdOpt = PM.pt.findFn(accountID, branchID, prettyPrintLoc)
      let! contextTypeIdOpt = PM.pt.findType(accountID, branchID, contextTypeLoc)

      match fnIdOpt, contextTypeIdOpt with
      | None, _ ->
        return Error "PrettyPrinter.ProgramTypes.packageValue not found"
      | _, None ->
        return Error "PrettyPrinter.ProgramTypes.Context type not found"
      | Some fnId, Some contextTypeId ->
        let state = createExecutionState pm
        let valDval = PT2DT.PackageValue.toDT value
        let ctxDval = createPrettyPrinterContext accountID branchID contextTypeId
        let fnName = RT.FQFnName.Package fnId
        let args = NEList.ofList ctxDval [ valDval ]

        let! result = Exe.executeFunction state fnName [] args

        match result with
        | Ok (RT.DString s) -> return Ok s
        | Ok dval -> return Ok (DvalReprDeveloper.toRepr dval)
        | Error(_rte, callStack) ->
          let! callStackStr = Exe.callStackString state callStack
          return Error $"Error during pretty print\n{callStackStr}"

    with ex ->
      return Error $"Exception: {ex.Message}"
  }

let viewEntity
  (accountID: Guid option)
  (branchID: Guid option)
  (location: PackageLocation)
  : string list =
  let pm = PM.pt
  let lines = ResizeArray<string>()

  match location with
  | Module path ->
    let results = queryAllDirectDescendants accountID branchID path
    let locationStr = PackageLocation.format location
    lines.Add(locationStr)
    lines.Add(String.replicate (String.length locationStr) "=")
    lines.Add("")

    // Display submodules
    let currentPathLength = List.length path
    let directSubmodules = getDirectSubmodules results currentPathLength

    if not (List.isEmpty directSubmodules) then
      lines.Add(getSectionHeader "submodule")
      for name in directSubmodules do
        lines.Add($"  {name}/")
      lines.Add("")

    // Display types
    if not (List.isEmpty results.Types) then
      lines.Add(getSectionHeader "type")
      for (loc, _) in results.Types do
        lines.Add($"  {loc.name}")
      lines.Add("")

    // Display values
    if not (List.isEmpty results.Values) then
      lines.Add(getSectionHeader "value")
      for (loc, _) in results.Values do
        lines.Add($"  {loc.name}")
      lines.Add("")

    // Display functions
    if not (List.isEmpty results.Functions) then
      lines.Add(getSectionHeader "function")
      for (loc, _) in results.Functions do
        lines.Add($"  {loc.name}")
      lines.Add("")

  | Type(owner, modules, name) ->
    let ptLoc : PT.PackageLocation = { owner = owner; modules = modules; name = name }
    let idOpt = pm.findType(accountID, branchID, ptLoc).Result
    match idOpt with
    | None ->
      lines.Add($"Type '{PackageLocation.format location}' not found.")
    | Some id ->
      let typeOpt = pm.getType(id).Result
      match typeOpt with
      | None ->
        lines.Add($"Type '{PackageLocation.format location}' not found.")
      | Some typ ->
        // Try to pretty print using Darklang's PrettyPrinter
        let result = prettyPrintType accountID branchID typ
        match result.Result with
        | Ok prettyPrinted ->
          lines.Add(prettyPrinted)
        | Error err ->
          // Show error for debugging
          lines.Add($"[Pretty print error: {err}]")
          // Fallback to basic display
          let locationStr = PackageLocation.format location
          lines.Add($"type {locationStr}")
          lines.Add("")
          if not (String.IsNullOrEmpty typ.description) then
            lines.Add($"Description: {typ.description}")

  | Function(owner, modules, name) ->
    let ptLoc : PT.PackageLocation = { owner = owner; modules = modules; name = name }
    let idOpt = pm.findFn(accountID, branchID, ptLoc).Result
    match idOpt with
    | None ->
      lines.Add($"Function '{PackageLocation.format location}' not found.")
    | Some id ->
      let fnOpt = pm.getFn(id).Result
      match fnOpt with
      | None ->
        lines.Add($"Function '{PackageLocation.format location}' not found.")
      | Some fn ->
        // Try to pretty print using Darklang's PrettyPrinter
        let result = prettyPrintFn accountID branchID fn
        match result.Result with
        | Ok prettyPrinted ->
          lines.Add(prettyPrinted)
        | Error err ->
          // Show error for debugging
          lines.Add($"[Pretty print error: {err}]")
          // Fallback to basic display
          let locationStr = PackageLocation.format location
          lines.Add($"let {locationStr}")
          lines.Add("")
          if not (String.IsNullOrEmpty fn.description) then
            lines.Add($"Description: {fn.description}")

  | Value(owner, modules, name) ->
    let ptLoc : PT.PackageLocation = { owner = owner; modules = modules; name = name }
    let idOpt = pm.findValue(accountID, branchID, ptLoc).Result
    match idOpt with
    | None ->
      lines.Add($"Value '{PackageLocation.format location}' not found.")
    | Some id ->
      let valOpt = pm.getValue(id).Result
      match valOpt with
      | None ->
        lines.Add($"Value '{PackageLocation.format location}' not found.")
      | Some v ->
        // Try to pretty print using Darklang's PrettyPrinter
        let result = prettyPrintValue accountID branchID v
        match result.Result with
        | Ok prettyPrinted ->
          lines.Add(prettyPrinted)
        | Error err ->
          // Show error for debugging
          lines.Add($"[Pretty print error: {err}]")
          // Fallback to basic display
          let locationStr = PackageLocation.format location
          lines.Add($"let {locationStr} = ...")
          if not (String.IsNullOrEmpty v.description) then
            lines.Add($"Description: {v.description}")

  lines |> Seq.toList

type ViewCommand() =
  inherit CommandBase()

  override _.Name = "view"
  override _.Description = "View detailed information about an entity"

  override _.Execute state args =
    match args with
    | [] ->
      // View current location
      let lines = viewEntity state.AccountID state.CurrentBranchID state.PackageData.CurrentLocation
      let output = CommandOutput.lines lines
      (output, StateUpdate state)

    | [ pathArg ] ->
      // Navigate to path and view
      match traverse state.AccountID state.CurrentBranchID state.PackageData.CurrentLocation pathArg with
      | Error errorMsg ->
        let output = CommandOutput.error $"Cannot view: {errorMsg}"
        (output, StateUpdate state)
      | Ok newLocation ->
        let lines = viewEntity state.AccountID state.CurrentBranchID newLocation
        let output = CommandOutput.lines lines
        (output, StateUpdate state)

    | _ ->
      let output = CommandOutput.error "Usage: view [path]"
      (output, StateUpdate state)

  override _.Complete state args =
    match args with
    | [] ->
      completePartialPath state.AccountID state.CurrentBranchID state.PackageData.CurrentLocation ""
    | [ partialPath ] ->
      completePartialPath state.AccountID state.CurrentBranchID state.PackageData.CurrentLocation partialPath
    | _ -> []

  override _.Help() =
    [ "Usage: view [path]"
      "View detailed information about functions, types, values, or modules."
      ""
      "With path: View the specified entity with syntax highlighting."
      "Without path: View current location."
      ""
      "Examples:"
      "  view                    - View current location"
      "  view List.head          - View the List.head function"
      "  view Option             - View the Option type"
      "  view Stdlib.List        - View contents of Stdlib.List module" ]

let command = ViewCommand() :> ICommand
