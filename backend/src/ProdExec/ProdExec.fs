/// A command to run common tasks in production. Note that most tasks could/should
/// be run by creating new functions in LibDarkInternal instead. This should be
/// used for cases where that is not appropriate.
///
/// Based on https://andrewlock.net/deploying-asp-net-core-applications-to-kubernetes-part-10-creating-an-exec-host-deployment-for-running-one-off-commands/
///
/// Run with `ProdExec --help` for usage
module ProdExec

open FSharp.Control.Tasks
open System.Threading.Tasks

open Prelude

module Telemetry = LibService.Telemetry
module Rollbar = LibService.Rollbar
module CTPusher = LibClientTypes.Pusher
module PT2RT = LibExecution.ProgramTypesToRuntimeTypes



/// Send multiple messages to Rollbar, to ensure our usage is generally OK
let triggerRollbar () : unit =
  let tags = [ "int", 6 :> obj; "string", "string"; "float", -0.6; "bool", true ]
  let prefix = "ProdExec test: "

  // Send async notification ("info" level in the rollbar UI)
  Rollbar.notify $"{prefix} notify" tags

  // Send async error
  Rollbar.sendError $"{prefix} sendError" tags

  // Send async exception
  let e = new System.Exception($"{prefix} sendException exception")
  Rollbar.sendException None tags e

/// Send a message to Rollbar that should result in a Page (notification)
/// going out
let triggerPagingRollbar () : int =
  // This one pages
  let prefix = "prodExec test: "
  // Send sync exception - do this last to wait for the others
  let e = new System.Exception($"{prefix} last ditch blocking exception")
  Rollbar.lastDitchBlockAndPage $"{prefix} last ditch block and page" e

let help () : unit =
  [ "USAGE:"
    "  ProdExec trigger-rollbar"
    "  ProdExec trigger-pageable-rollbar"
    "  ProdExec convert-st-to-rt [canvasID]"
    "  ProdExec help" ]
  |> List.join "\n"
  |> print

type Options =
  | TriggerRollbar
  | TriggerPagingRollbar
  | InvalidUsage
  | ConvertST2RT of System.Guid
  | ConvertST2RTAll
  | Help

let parse (args : string[]) : Options =
  match args with
  | [| "trigger-rollbar" |] -> TriggerRollbar
  | [| "trigger-paging-rollbar" |] -> TriggerPagingRollbar
  | [| "convert-st-to-rt"; "all" |] -> ConvertST2RTAll
  | [| "convert-st-to-rt"; canvasID |] -> ConvertST2RT(System.Guid.Parse canvasID)
  | [| "help" |] -> Help
  | _ -> InvalidUsage
let convertToRT (canvasID : CanvasID) : Task<unit> =
  task {
    let! canvas = LibCloud.Canvas.loadAll canvasID
    let _program = LibCloud.Canvas.toProgram canvas
    let _handlers =
      canvas.handlers
      |> Map.values
      |> List.map (fun h -> PT2RT.Handler.toRT Map.empty h.ast)
    return ()
  }





let run (options : Options) : Task<int> =
  task {
    // Track calls to this
    use _ = Telemetry.createRoot "ProdExec run"
    Telemetry.addTags [ "options", options ]
    Rollbar.notify "prodExec called" [ "options", string options ]

    match options with

    | TriggerRollbar ->
      triggerRollbar ()
      // The async operations go in a queue, and I don't know how to block until
      // the queue is empty.
      do! Task.Delay 5000
      return 0

    | TriggerPagingRollbar -> return triggerPagingRollbar ()

    | ConvertST2RT canvasID ->
      do! convertToRT canvasID
      return 0

    | ConvertST2RTAll ->
      let! allIDs = LibCloud.Canvas.allCanvasIDs ()
      do! Task.iterWithConcurrency 25 convertToRT allIDs
      return 0

    | Help ->
      help ()
      return 0

    | InvalidUsage ->
      print "Invalid usage!!\n"
      help ()
      return 1
  }

let initSerializers () =
  // one-off types used internally
  // we probably don't need most of these, but it's key that ProdExec doesn't ever
  // fail, so we're extra-cautious, and include _everything_.
  Json.Vanilla.allow<LibExecution.ProgramTypes.Toplevel.T> "Canvas.loadJsonFromDisk"
  Json.Vanilla.allow<LibExecution.DvalReprInternalRoundtrippable.FormatV0.Dval>
    "RoundtrippableSerializationFormatV0.Dval"
  Json.Vanilla.allow<LibCloud.Queue.NotificationData> "eventqueue storage"
  Json.Vanilla.allow<LibService.Rollbar.HoneycombJson> "Rollbar"

  // for Pusher.com payloads
  Json.Vanilla.allow<CTPusher.Payload.NewTrace> "Pusher"
  Json.Vanilla.allow<CTPusher.Payload.New404> "Pusher"
// Json.Vanilla.allow<CTPusher.Payload.AddOpV1> "Pusher"
// Json.Vanilla.allow<CTPusher.Payload.AddOpV1PayloadTooBig> "Pusher" // this is so-far unused
// Json.Vanilla.allow<CTPusher.Payload.UpdateWorkerStates> "Pusher"

[<EntryPoint>]
let main (args : string[]) : int =
  let name = "ProdExec"
  try
    printTime "Starting ProdExec"
    initSerializers ()
    LibService.Init.init name
    Telemetry.Console.loadTelemetry name Telemetry.TraceDBQueries
    let options = parse args
    let result = (run options).Result
    LibService.Init.shutdown name
    printTime "Finished ProdExec"
    result
  with e ->
    // Don't reraise or report as ProdExec is only run interactively
    printException "" [] e
    LibService.Init.shutdown name
    1
