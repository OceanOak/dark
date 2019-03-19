open Tc
open Prelude
open Types
module Cmd = Tea.Cmd
module Navigation = Tea.Navigation
module TL = Toplevel

let hashUrlParams (params : (string * string) list) : string =
  let merged = List.map ~f:(fun (k, v) -> k ^ "=" ^ v) params in
  "#" ^ String.join ~sep:"&" merged


let urlFor (page : page) : string =
  let args =
    match page with
    | Architecture ->
        []
    | FocusedFn tlid ->
        [("fn", deTLID tlid)]
    | FocusedHandler tlid ->
        [("handler", deTLID tlid)]
    | FocusedDB tlid ->
        [("db", deTLID tlid)]
  in
  hashUrlParams args


let navigateTo (page : page) : msg Cmd.t = Navigation.newUrl (urlFor page)

let linkFor (page : page) (class_ : string) (content : msg Html.html list) :
    msg Html.html =
  Html.a [Html.href (urlFor page); Html.class' class_] content


let parseLocation (loc : Web.Location.location) : page option =
  let unstructured =
    loc.hash
    |> String.dropLeft ~count:1
    |> String.split ~on:"&"
    |> List.map ~f:(String.split ~on:"=")
    |> List.filterMap ~f:(fun arr ->
           match arr with [a; b] -> Some (String.toLower a, b) | _ -> None )
    |> StrDict.fromList
  in
  let architecture () = Some Architecture in
  let fn () =
    match StrDict.get ~key:"fn" unstructured with
    | Some sid ->
        Some (FocusedFn (TLID sid))
    | _ ->
        None
  in
  let handler () =
    match StrDict.get ~key:"handler" unstructured with
    | Some sid ->
        Some (FocusedHandler (TLID sid))
    | _ ->
        None
  in
  let db () =
    match StrDict.get ~key:"db" unstructured with
    | Some sid ->
        Some (FocusedDB (TLID sid))
    | _ ->
        None
  in
  fn ()
  |> Option.orElse (handler ())
  |> Option.orElse (db ())
  |> Option.orElse (architecture ())


let changeLocation (m : model) (loc : Web.Location.location) : modification =
  let mPage = parseLocation loc in
  match mPage with
  | Some (FocusedFn id) ->
    ( match Functions.find m id with
    | None ->
        DisplayError "No function with this id"
    | _ ->
        SetPage (FocusedFn id) )
  | Some (FocusedHandler id) ->
    ( match TL.get m id with
    | None ->
        DisplayError "No toplevel with this id"
    | _ ->
        SetPage (FocusedHandler id) )
  | Some (FocusedDB id) ->
    ( match TL.get m id with
    | None ->
        DisplayError "No DB with this id"
    | _ ->
        SetPage (FocusedDB id) )
  | Some Architecture ->
      SetPage Architecture
  | None ->
      NoChange


let parseCanvasName (loc : Web.Location.location) : string =
  match
    loc.pathname
    |> String.dropLeft ~count:1
    (* remove lead "/" *)
    |> String.split ~on:"/"
  with
  | "a" :: canvasName :: _ ->
      canvasName
  | _ ->
      "builtwithdark"


let splitOnEquals (s : string) : (string * bool) option =
  match String.split ~on:"=" s with
  | [] ->
      None
  | [name] ->
      Some (name, true)
  | [name; value] ->
      Some (name, value <> "0" && value <> "false")
  | _ ->
      None


let queryParams : (string * bool) list =
  let search = (Tea_navigation.getLocation ()).search in
  match String.uncons search with
  | Some ('?', rest) ->
      rest
      |> String.toLower
      |> String.split ~on:"&"
      |> List.filterMap ~f:splitOnEquals
  | _ ->
      []


let queryParamSet (name : string) : bool =
  List.find ~f:(fun (k, v) -> if k = name then v else false) queryParams
  |> Option.withDefault ~default:(name, false)
  |> Tuple2.second


let isDebugging = queryParamSet "debugger"

let isIntegrationTest = queryParamSet "integration-test"

let setPage (m : model) (oldPage : page) (newPage : page) : model =
  if oldPage = newPage
  then m
  else
    match newPage with
    | Architecture ->
        let offset =
          match m.canvasProps.lastOffset with
          | Some lo ->
              lo
          | None ->
              m.canvasProps.offset
        in
        { m with
          currentPage = newPage
        ; canvasProps = {m.canvasProps with offset; lastOffset = None} }
    | FocusedFn _ ->
        { m with
          currentPage = newPage
        ; canvasProps =
            { m.canvasProps with
              lastOffset = Some m.canvasProps.offset; offset = Defaults.origin
            }
        ; cursorState = Deselected }
    | FocusedHandler tlid | FocusedDB tlid ->
        let offset =
          let telem =
            Native.Ext.querySelector (".toplevel.tl-" ^ showTLID tlid)
          in
          match telem with
          | Some e ->
              let tsize =
                {w = Native.Ext.clientWidth e; h = Native.Ext.clientHeight e}
              in
              let tl = TL.getTL m tlid in
              let windowSize = m.canvasProps.viewportSize in
              if Viewport.isEnclosed
                   (m.canvasProps.offset, windowSize)
                   (tl.pos, tsize)
              then m.canvasProps.offset
              else Viewport.toCenteredOn tl.pos
          | None ->
              m.canvasProps.offset
        in
        let panAnimation = offset <> m.canvasProps.offset in
        { m with
          currentPage = newPage
        ; cursorState = Selecting (tlid, None)
        ; canvasProps =
            {m.canvasProps with offset; panAnimation; lastOffset = None} }


let shouldUpdateHash (m : model) (tlid : tlid) : msg Tea_cmd.t list =
  let prevTLID = tlidOf m.cursorState in
  if match prevTLID with Some tid -> tlid <> tid | None -> false
  then
    let tl = TL.getTL m tlid in
    let page = TL.asPage tl in
    [navigateTo page]
  else []
