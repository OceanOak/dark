module Cli.Main

open System
open System.Threading.Tasks
open FSharp.Control.Tasks

open Prelude

module RT = LibExecution.RuntimeTypes
module Dval = LibExecution.Dval
module PT = LibExecution.ProgramTypes
module PT2RT = LibExecution.ProgramTypesToRuntimeTypes
module Exe = LibExecution.Execution
module PackageIDs = LibExecution.PackageIDs
module BuiltinCli = BuiltinCli.Builtin
open System.Diagnostics


// ---------------------
// Version information
// ---------------------

type VersionInfo = { hash : string; buildDate : string; inDevelopment : bool }

#if DEBUG
let inDevelopment : bool = true
#else
let inDevelopment : bool = false
#endif

open System.Reflection

// let info () =
//   let buildAttributes =
//     Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyMetadataAttribute>()
//   // This reads values created during the build in Cli.fsproj
//   // It doesn't feel like this is how it's supposed to be used, but it works. But
//   // what if we wanted more than two parameters?
//   let buildDate = buildAttributes.Key
//   let gitHash = buildAttributes.Value
//   { hash = gitHash; buildDate = buildDate; inDevelopment = inDevelopment }


// ---------------------
// Execution
// ---------------------

// TODO: de-dupe with _other_ Cli.fs
let pmBaseUrl =
  match
    System.Environment.GetEnvironmentVariable "DARK_CONFIG_PACKAGE_MANAGER_BASE_URL"
  with
  | null -> "https://packages.darklang.com"
  | var -> var
// let swPms = Stopwatch.StartNew()
// let packageManagerRT = LibPackageManager.PackageManager.rt pmBaseUrl
// let packageManagerPT = LibPackageManager.PackageManager.pt pmBaseUrl
// swPms.Stop()
// let secondsPms = float swPms.ElapsedMilliseconds / 1000.0
// debuG "Time elapsed to create packageManagers" secondsPms

// Initialize package managers and fill cache at startup
let initializePackageManagers () =
  task {
    debuG "initializePackageManagers started" ()
    let sw = Stopwatch.StartNew()

    let packageManagerRT = LibPackageManager.PackageManager.rt pmBaseUrl
    let packageManagerPT = LibPackageManager.PackageManager.pt pmBaseUrl

    do! packageManagerRT.init

    sw.Stop()
    let seconds = float sw.ElapsedMilliseconds / 1000.0
    debuG "Time elapsed to initialize package managers and fill cache" seconds

    return packageManagerRT, packageManagerPT
  }

// Initialize once at startup
let packageManagerRT, packageManagerPT = initializePackageManagers().Result

debuG "before builtins" ()
// use stopwatch
let swB = Stopwatch.StartNew()
let builtins : RT.Builtins =
  debuG "Starting builtins initialization" ()
  let result =
    LibExecution.Builtin.combine
      [ BuiltinExecution.Builtin.builtins
          BuiltinExecution.Libs.HttpClient.defaultConfig
          packageManagerPT
        BuiltinCli.Builtin.builtins
        BuiltinCliHost.Builtin.builtins
        ]

      []
  debuG "Finished builtins initialization" ()
  result
swB.Stop()
debuG "after builtins" ()
let secondsB = float swB.ElapsedMilliseconds / 1000.0
debuG "Time elapsed to create builtins" secondsB


let state () =
  let program : RT.Program =
    { canvasID = System.Guid.NewGuid()
      internalFnsAllowed = false
      dbs = Map.empty
      secrets = [] }

  let notify
    (_state : RT.ExecutionState)
    (_vm : RT.VMState)
    (_msg : string)
    (_metadata : Metadata)
    =
    // let metadata = extraMetadata state @ metadata
    // LibService.Rollbar.notify msg metadata
    uply { return () }

  let sendException
    (_ : RT.ExecutionState)
    (_ : RT.VMState)
    (metadata : Metadata)
    (exn : exn)
    =
    uply { printException "Internal error" metadata exn }

  Exe.createState
    builtins
    packageManagerRT
    Exe.noTracing
    sendException
    notify
    program




let execute (args : List<string>) : Task<RT.ExecutionResult> =
  task {
    let swState = Stopwatch.StartNew()
    let state = state ()
    swState.Stop()
    let secondsState = float swState.ElapsedMilliseconds / 1000.0
    debuG "Time elapsed to create state" secondsState
    let fnName = RT.FQFnName.fqPackage PackageIDs.Fn.Cli.executeCliCommand

    let swArgs = Stopwatch.StartNew()
    let args =
      args |> List.map RT.DString |> Dval.list RT.KTString |> NEList.singleton
    swArgs.Stop()
    let secondsArgs = float swArgs.ElapsedMilliseconds / 1000.0
    debuG "Time elapsed to create args" secondsArgs
    return! Exe.executeFunction state fnName [] args
  }

let initSerializers () =
  Json.Vanilla.allow<List<LibPackageManager.Types.ProgramTypes.PackageType.PackageType>>
    "PackageManager"
  Json.Vanilla.allow<List<LibPackageManager.Types.ProgramTypes.PackageFn.PackageFn>>
    "PackageManager"
  Json.Vanilla.allow<List<LibPackageManager.Types.ProgramTypes.PackageConstant.PackageConstant>>
    "PackageManager"
  ()

[<EntryPoint>]
let main (args : string[]) =
  try
    let sw1 = Stopwatch.StartNew()
    initSerializers ()
    sw1.Stop()
    let seconds1 = float sw1.ElapsedMilliseconds / 1000.0
    debuG "Time elapsed to init initilizers" seconds1

    let sw = Stopwatch.StartNew()
    let result = execute (Array.toList args)
    let result = result.Result
    sw.Stop()
    let seconds = float sw.ElapsedMilliseconds / 1000.0
    debuG "Time elapsed to execute in main" seconds

    NonBlockingConsole.wait ()
    let swFR = Stopwatch.StartNew()
    let result =
      match result with
      | Error(rte, callStack) ->
        let state = state ()

        let errorCallStackStr =
          (LibExecution.Execution.callStackString state callStack).Result

        match (LibExecution.Execution.runtimeErrorToString state rte).Result with
        | Ok(RT.DString s) ->
          Console.WriteLine
            $"Encountered a Runtime Error:\n{s}\n\n{errorCallStackStr}\n  "

        | Ok otherVal ->
          Console.WriteLine
            $"Encountered a Runtime Error, stringified it, but somehow a non-string was returned.\n"
          Console.WriteLine $"Runtime Error: {rte}"
          Console.WriteLine $"'Stringified':\n{otherVal}"
          Console.WriteLine $"{errorCallStackStr}"

        | Error(newErr) ->
          Console.WriteLine
            $"Encountered a Runtime Error, tried to stringify it, and then _that_ failed."
          Console.WriteLine $"Original Error: {rte}"
          Console.WriteLine $"{errorCallStackStr}"
          Console.WriteLine
            $"\nError encountered when trying to stringify:\n{newErr}"

        1
      | Ok(RT.DInt64 i) -> (int i)
      | Ok dval ->
        let output = DvalReprDeveloper.toRepr dval
        Console.WriteLine
          $"Error: main function must return an int (returned {output})"
        1
    swFR.Stop()
    let secondsFR = float swFR.ElapsedMilliseconds / 1000.0
    debuG "Time elapsed to finalize result" secondsFR
    result

  with e ->
    printException "Error starting Darklang CLI" [] e
    1
