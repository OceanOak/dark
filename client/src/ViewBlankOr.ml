open Tc
open Prelude
open Types

(* Dark *)
module B = Blank
module TL = Toplevel

type htmlConfig =
  (* Add this class (can be done multiple times) *)
  | WithClass of string
  (* when you click this node, select this pointer *)
  | ClickSelectAs of id
  | ClickSelect
  (* highlight this node as if it were ID *)
  | MouseoverAs of id
  | Mouseover
  (* use this as ID for Mouseover, ClickSelect *)
  | WithID of id
  (* show a featureflag *)
  | WithFF
  (* show an 'edit function' link *)
  | WithEditFn of tlid
  (* will end up in error rail *)
  | WithROP
  (* editable *)
  | Enterable

let wc (s : string) : htmlConfig = WithClass s

let idConfigs : htmlConfig list = [ClickSelect; Mouseover]

let atom : htmlConfig = wc "atom"

let nested : htmlConfig = wc "nested"

let renderLiveValue (vs : ViewUtils.viewState) (id : id option) : string =
  let cursorLiveValue =
    match id with
    | Some (ID id) ->
        StrDict.get ~key:id vs.currentResults.liveValues
    | _ ->
        None
  in
  match cursorLiveValue with
  | Some dv ->
      Runtime.toRepr dv
  | _ ->
      (* I made this say "loading" because the CSS structure makes it hard to
       * put a spinner in here *)
      "<loading>"


let viewFeatureFlag : msg Html.html =
  Html.div
    [ Html.class' "flag"
    ; ViewUtils.eventNoPropagation ~key:"sff" "click" (fun _ ->
          StartFeatureFlag ) ]
    [ViewUtils.fontAwesome "flag"]


let viewEditFn (tlid : tlid) (hasFlagAlso : bool) : msg Html.html =
  let rightOffset = if hasFlagAlso then "-34px" else "-16px" in
  Html.a
    [ Html.class' "edit-fn"
    ; Vdom.styles [("right", rightOffset)]
    ; Html.href (Url.urlFor (FocusedFn tlid)) ]
    [ViewUtils.fontAwesome "edit"]


let viewCreateFn : msg Html.html =
  Html.div
    [ Html.class' "exfun"
    ; ViewUtils.eventNoPropagation ~key:"ef" "click" (fun _ -> ExtractFunction)
    ]
    [ViewUtils.svgIconFn "white"]


(* Create a Html.div for this ID, incorporating all ID-related data, *)
(* such as whether it's selected, appropriate events, mouseover, etc. *)
let div
    (vs : ViewUtils.viewState)
    (configs : htmlConfig list)
    (content : msg Html.html list) : msg Html.html =
  let getFirst fn = configs |> List.filterMap ~f:fn |> List.head in
  (* Extract config *)
  let thisID =
    getFirst (fun a -> match a with WithID id -> Some id | _ -> None)
  in
  let clickAs =
    getFirst (fun a ->
        match a with
        | ClickSelectAs id ->
            Some id
        | ClickSelect ->
            thisID
        | _ ->
            None )
  in
  let mouseoverAs =
    getFirst (fun a ->
        match a with
        | MouseoverAs id ->
            Some id
        | Mouseover ->
            thisID
        | _ ->
            None )
  in
  let classes =
    configs
    |> List.filterMap ~f:(fun a ->
           match a with WithClass c -> Some c | _ -> None )
  in
  let showFeatureFlag = List.member ~value:WithFF configs in
  let showROP = List.member ~value:WithROP configs in
  let editFn =
    getFirst (fun a -> match a with WithEditFn id -> Some id | _ -> None)
  in
  let isCommandTarget =
    match vs.cursorState with
    | SelectingCommand (_, id) ->
        thisID = Some id
    | _ ->
        false
  in
  let selectedID =
    match vs.cursorState with Selecting (_, Some id) -> Some id | _ -> None
  in
  let selected = thisID = selectedID && Option.isSome thisID in
  let displayLivevalue =
    (thisID = idOf vs.cursorState || showROP)
    && Option.isSome thisID
    && vs.showLivevalue
  in
  let mouseoverClass =
    let targetted =
      mouseoverAs = Option.map ~f:Tuple2.second vs.hovering
      && Option.isSome mouseoverAs
    in
    if targetted
    then
      if List.any ~f:(( = ) FluidInputModel) vs.testVariants
      then
        if List.any ~f:(fun c -> c = Enterable) configs
        then ["mouseovered-enterable"]
        else if idOf vs.cursorState = thisID
        then []
        else ["mouseovered-selectable-fluid"]
      else ["mouseovered-selectable"]
    else []
  in
  let idClasses =
    match thisID with Some id -> ["blankOr"; "id-" ^ deID id] | _ -> []
  in
  let allClasses =
    classes
    @ idClasses
    @ (if displayLivevalue then ["display-livevalue"] else [])
    @ (if selected then ["selected"] else [])
    @ (if isCommandTarget then ["commandTarget"] else [])
    @ mouseoverClass
  in
  let classAttr = Html.class' (String.join ~sep:" " allClasses) in
  let events =
    match clickAs with
    | Some id ->
        [ ViewUtils.eventNoPropagation
            "click"
            ~key:("bcc-" ^ showTLID vs.tl.id ^ "-" ^ showID id)
            (fun x -> BlankOrClick (vs.tl.id, id, x))
        ; ViewUtils.eventNoPropagation
            "dblclick"
            ~key:("bcdc-" ^ showTLID vs.tl.id ^ "-" ^ showID id)
            (fun x -> BlankOrDoubleClick (vs.tl.id, id, x))
        ; ViewUtils.eventNoPropagation
            "mouseenter"
            ~key:("me-" ^ showTLID vs.tl.id ^ "-" ^ showID id)
            (fun x -> BlankOrMouseEnter (vs.tl.id, id, x))
        ; ViewUtils.eventNoPropagation
            "mouseleave"
            ~key:("ml-" ^ showTLID vs.tl.id ^ "-" ^ showID id)
            (fun x -> BlankOrMouseLeave (vs.tl.id, id, x)) ]
    | _ ->
        (* Rather than relying on property lengths changing, we should use
         * noProp to indicate that the property at idx N has changed. *)
        [Vdom.noProp; Vdom.noProp; Vdom.noProp; Vdom.noProp]
  in
  let liveValueAttr =
    if displayLivevalue
    then Vdom.attribute "" "data-live-value" (renderLiveValue vs thisID)
    else Vdom.noProp
  in
  let copyButton =
    if displayLivevalue
    then
      [ Html.div
          [ ViewUtils.eventNoPropagation
              "click"
              ~key:("copylivevalue-" ^ showTLID vs.tl.id ^ "-")
              (fun _ -> ClipboardCopyLivevalue (renderLiveValue vs thisID)) ]
          [ViewUtils.fontAwesome "copy"] ]
    else []
  in
  let featureFlagHtml = if showFeatureFlag then [viewFeatureFlag] else [] in
  let editFnHtml =
    match editFn with
    | Some editFn_ ->
        [viewEditFn editFn_ showFeatureFlag]
    | None ->
        if showFeatureFlag then [viewCreateFn] else [Vdom.noNode]
  in
  let rightSideHtml =
    if selected
    then
      [ Html.div
          [Html.class' "expr-actions"]
          (featureFlagHtml @ editFnHtml @ copyButton) ]
    else []
  in
  let idAttr =
    match thisID with Some i -> Html.id (showID i) | _ -> Vdom.noProp
  in
  let attrs = idAttr :: liveValueAttr :: classAttr :: events in
  Html.div
  (* if the id of the blank_or changes, this whole node should be redrawn
     * without any further diffing. there's no good reason for the Vdom/Dom node
     * to be re-used for a different blank_or *)
    ~unique:(thisID |> Option.map ~f:showID |> Option.withDefault ~default:"")
    attrs
    (content @ rightSideHtml)


let text (vs : ViewUtils.viewState) (c : htmlConfig list) (str : string) :
    msg Html.html =
  div vs c [Html.text str]


let keyword (vs : ViewUtils.viewState) (c : htmlConfig list) (name : string) :
    msg Html.html =
  text vs (atom :: wc "keyword" :: wc name :: c) name


let tipe (vs : ViewUtils.viewState) (c : htmlConfig list) (t : tipe) :
    msg Html.html =
  text vs c (Runtime.tipe2str t)


let withFeatureFlag (vs : ViewUtils.viewState) (v : 'a blankOr) :
    htmlConfig list =
  if idOf vs.cursorState = Some (B.toID v) then [WithFF] else []


let withEditFn (vs : ViewUtils.viewState) (v : nExpr blankOr) : htmlConfig list
    =
  if idOf vs.cursorState = Some (B.toID v)
  then
    match v with
    | F (_, FnCall (F (_, name), _, _)) ->
      ( match List.find ~f:(Functions.sameName name) vs.ufns with
      | Some fn ->
          [WithEditFn fn.ufTLID]
      | _ ->
          [] )
    | _ ->
        []
  else []


let withROP (rail : sendToRail) : htmlConfig list =
  if rail = Rail then [WithROP] else []


let getLiveValue (lvs : lvDict) (ID id : id) : dval option =
  StrDict.get ~key:id lvs


let placeHolderFor (vs : ViewUtils.viewState) (id : id) (pt : pointerType) :
    string =
  let paramPlaceholder =
    vs.tl
    |> TL.asHandler
    |> Option.map ~f:(fun x -> x.ast)
    |> Option.andThen ~f:(fun ast ->
           match AST.getParamIndex ast id with
           | Some (name, index) ->
             ( match Autocomplete.findFunction vs.ac name with
             | Some {fnParameters} ->
                 List.getAt ~index fnParameters
             | None ->
                 None )
           | _ ->
               None )
    |> Option.map ~f:(fun p ->
           p.paramName ^ ": " ^ Runtime.tipe2str p.paramTipe ^ "" )
    |> Option.withDefault ~default:""
  in
  match pt with
  | VarBind ->
      "varname"
  | EventName ->
    ( match vs.handlerSpace with
    | HSHTTP ->
        "route"
    | HSCron ->
        "event name"
    | HSOther ->
        "event name"
    | HSEmpty ->
        "event name" )
  | EventModifier ->
    ( match vs.handlerSpace with
    | HSHTTP ->
        "verb"
    | HSCron ->
        "event interval"
    | HSOther ->
        "event modifier"
    | HSEmpty ->
        "event modifier" )
  | EventSpace ->
      "event space"
  | Expr ->
      paramPlaceholder
  | Field ->
      "fieldname"
  | Key ->
      "keyname"
  | DBName ->
      "db name"
  | DBColName ->
      "db field name"
  | DBColType ->
      "db type"
  | FFMsg ->
      "flag name"
  | FnName ->
      "function name"
  | ParamName ->
      "param name"
  | ParamTipe ->
      "param type"
  | Pattern ->
      "pattern"
  | ConstructorName ->
      "constructor name"
  | FnCallName ->
      "function name"


let viewBlankOr
    (htmlFn : ViewUtils.viewState -> htmlConfig list -> 'a -> msg Html.html)
    (pt : pointerType)
    (vs : ViewUtils.viewState)
    (c : htmlConfig list)
    (bo : 'a blankOr) : msg Html.html =
  let wID id = [WithID id] in
  let drawBlank id =
    div
      vs
      ([WithClass "blank"] @ c @ wID id)
      [Html.text (placeHolderFor vs id pt)]
  in
  let drawFilled id fill =
    let configs = wID id @ c in
    htmlFn vs configs fill
  in
  let thisText =
    match bo with
    | F (id, fill) ->
        drawFilled id fill
    | Blank id ->
        drawBlank id
  in
  match vs.cursorState with
  | Entering (Filling (_, thisID)) ->
      let id = B.toID bo in
      if id = thisID
      then
        if vs.showEntry
        then
          let allowStringEntry =
            if pt = Expr then StringEntryAllowed else StringEntryNotAllowed
          in
          let stringEntryWidth =
            if vs.tooWide
            then StringEntryShortWidth
            else StringEntryNormalWidth
          in
          let placeholder = placeHolderFor vs id pt in
          div
            vs
            (c @ wID id)
            [ ViewEntry.entryHtml
                allowStringEntry
                stringEntryWidth
                placeholder
                vs.ac ]
        else Html.text vs.ac.value
      else thisText
  | SelectingCommand (_, id) ->
      if id = B.toID bo
      then
        Html.div
          [Html.class' "selecting-command"]
          [ thisText
          ; ViewEntry.entryHtml
              StringEntryNotAllowed
              StringEntryNormalWidth
              "command"
              vs.ac ]
      else thisText
  | _ ->
      thisText


let viewText
    (pt : pointerType)
    (vs : ViewUtils.viewState)
    (c : htmlConfig list)
    (str : string blankOr) : msg Html.html =
  viewBlankOr text pt vs c str


let viewTipe
    (pt : pointerType)
    (vs : ViewUtils.viewState)
    (c : htmlConfig list)
    (str : tipe blankOr) : msg Html.html =
  viewBlankOr tipe pt vs c str
