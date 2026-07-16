/// Browser (WASM) host for the Darklang REPL.
///
/// The parser and runtime are the real ones (LibParser + LibExecution). This
/// file is the thin shell: it boots an execution state and exposes `Eval` (per
/// REPL line) plus `LoadPackagesFromUrl` (called once at startup to load the
/// package snapshot into an in-memory package manager, so `Stdlib.*` resolves).
module Darklang.Wasm.Repl

open System.Threading.Tasks
open Microsoft.JSInterop

open Prelude

module PT = LibExecution.ProgramTypes
module RT = LibExecution.RuntimeTypes
module P = LibParser.Parser
module WT = LibParser.WrittenTypes
module WT2PT = LibParser.WrittenTypesToProgramTypes
module PT2RT = LibExecution.ProgramTypesToRuntimeTypes
module NR = LibParser.NameResolver
module Exe = LibExecution.Execution
module BS = LibSerialization.Binary.Serialization

let private builtins : RT.Builtins = Builtins.Pure.Builtin.builtins ()

// Starts empty; replaced with the loaded snapshot once LoadPackagesFromUrl runs.
let mutable private pm : PT.PackageManager = PT.PackageManager.empty
let mutable private stateOpt : RT.ExecutionState option = None

let private buildState () : RT.ExecutionState =
  let pmRT = PT2RT.PackageManager.toRT builtins.values pm
  let program : RT.Program = { dbs = Map.empty }
  Exe.createState
    builtins
    pmRT
    Exe.noTracing
    RT.consoleReporter
    RT.consoleNotifier
    PT.mainBranchId
    program

let private getState () : RT.ExecutionState =
  match stateOpt with
  | Some s -> s
  | None ->
    let s = buildState ()
    stateOpt <- Some s
    s

/// Load the package snapshot (all PackageOps, framed as
/// [36-byte ascii uuid][4-byte LE length][op blob]) into an in-memory PM.
[<JSInvokable>]
let LoadPackagesFromUrl (snapshotUrl : string) : Task =
  task {
    use http = new System.Net.Http.HttpClient()
    let! bytes = http.GetByteArrayAsync(snapshotUrl)
    let ops = ResizeArray<PT.PackageOp>()
    let mutable i = 0
    while i < bytes.Length do
      if bytes.Length - i < 40 then
        invalidArg
          (nameof snapshotUrl)
          "package snapshot has a truncated frame header"

      let idStr = System.Text.Encoding.ASCII.GetString(bytes, i, 36)
      let len = System.BitConverter.ToInt32(bytes, i + 36)
      if len < 0 || bytes.Length - i - 40 < len then
        invalidArg (nameof snapshotUrl) "package snapshot has a truncated frame body"

      let blob = Array.sub bytes (i + 40) len
      ops.Add(BS.PT.PackageOp.deserialize (System.Guid.Parse idStr) blob)
      i <- i + 40 + len

    // CLEANUP: replace this snapshot seam with a hosted PT/RT PackageManager.
    // Resolve names against a versioned branch index, fetch immutable package
    // items by hash, and cache them in the browser. Until that service exists,
    // the generated snapshot keeps the browser runtime self-contained.
    pm <- LibDB.PackageManager.createInMemory (List.ofSeq ops)
    stateOpt <- Some(buildState ()) // rebuild the state with the loaded PM
  }
  :> Task

[<CLIMutable>]
type EvalResult = { result : string; error : bool }

let private success (result : string) : EvalResult =
  { result = result; error = false }

let private failure (result : string) : EvalResult =
  { result = result; error = true }

/// A side-effect-free dispatcher readiness probe for browser startup.
[<JSInvokable>]
let Ready () : bool = true

/// Render a runtime error via the Dark error pretty-printer — same message as
/// `dark eval`. Falls back to a debug repr if the pretty-printer itself fails.
let private renderError
  (s : RT.ExecutionState)
  (rte : RT.RuntimeError.Error)
  : Task<string> =
  task {
    match! Exe.runtimeErrorToString s rte with
    | Ok(RT.DString msg) -> return msg
    | _ -> return $"Runtime error: %A{rte}"
  }

/// Parse → lower → execute → format. Called from JS per REPL line.
[<JSInvokable>]
let Eval (source : string) : Task<EvalResult> =
  task {
    try
      let r = P.parse source
      match r.parsed, r.diagnostics with
      | Some(WT.SourceFile { declarations = []; exprsToEval = [ e ] }), [] ->
        let s = getState ()
        let ctx : WT2PT.Context =
          { currentFnName = None
            isInFunction = false
            argMap = Map.empty
            localBindings = Set.empty }
        let! pt =
          WT2PT.Expr.toPT builtins pm NR.OnMissing.Allow PT.mainBranchId [] ctx e
          |> Ply.toTask
        let instrs = PT2RT.Expr.toRT Map.empty 0 None pt
        let! result = Exe.executeExpr s instrs
        match result with
        | Ok dval ->
          // The Dark pretty-printer, run on the VM against the loaded snapshot —
          // same rendering as `dark eval`. Falls back to a marked debug repr.
          let! repr = Exe.dvalToRepr s dval
          return success repr
        | Error(e, _callStack) ->
          let! msg = renderError s e
          return failure msg
      | _, [] -> return failure "(expected a single expression)"
      | _, diags ->
        return
          diags
          |> List.map (P.renderDiagnostic source)
          |> String.concat "\n"
          |> failure
    with
    // Parsing/lowering can raise (e.g. deep VM failures) — the CLI's eval
    // builtin guards the same pipeline the same way. Render instead of letting
    // the exception reject the JS interop promise.
    | RT.RuntimeErrorException(_, rte) ->
      let! msg = renderError (getState ()) rte
      return failure msg
    | e -> return failure $"Internal error: {e.Message}"
  }
