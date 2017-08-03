module View exposing (view)

import Char
import Dict exposing (Dict)
import Set
import Json.Decode as JSD
import Json.Decode.Pipeline as JSDP
import Maybe.Extra

import Svg
import Svg.Attributes as SA

import Html
import Html.Attributes as Attrs
import Html.Events as Events
import Keyboard.Event


import Types exposing (..)
import Util exposing (deMaybe)
import Graph as G
import Canvas
import Defaults

view : Model -> Html.Html Msg
view m =
  -- TODO: recalculate this using Tasks
  let (w, h) = Util.windowSize ()
  in
    Html.div
      [Attrs.id "grid"]
      [ (Svg.svg
           [ SA.width (toString w) , SA.height (toString <| h - 60)]
           (viewCanvas m))
      -- , viewRepl m.inputValue
      , viewError m.error
      , viewLive m m.cursor
      ]

viewRepl value = Html.form [
                  Events.onSubmit (ReplSubmitMsg)
                 ] [
                  Html.input [ Attrs.id Defaults.replID
                             , Events.onInput ReplInputMsg
                             , Attrs.value value
                             , Attrs.autocomplete False
                             ] []
                 ]

viewError (msg, ts) =
  Html.div
    [Attrs.id "darkErrors"]
    (case (toString ts) of
       "0" -> [Html.text <| "Err: " ++ msg]
       ts -> [Html.text <| "Err: " ++ msg ++ " (" ++ ts ++ ")"])

viewCanvas : Model -> List (Svg.Svg Msg)
viewCanvas m =
    let allNodes = List.indexedMap (\i n -> viewNode m n i) (G.orderedNodes m)
        edges = List.map (viewEdge m) m.edges
        dragEdge = viewDragEdge m.drag m.dragPos |> Maybe.Extra.toList
        entry = viewCursor m
    in svgDefs :: svgArrowHead :: (entry ++ allNodes ++ dragEdge ++ edges)

placeHtml : Pos -> Html.Html Msg -> Svg.Svg Msg
placeHtml pos html =
  Svg.foreignObject
    [ SA.x (toString pos.x)
    , SA.y (toString pos.y)
    ]
    [ html ]

viewClick pos =
  Svg.circle [ SA.r "10"
             , SA.cx (toString pos.x)
             , SA.cy (toString pos.y)
             , SA.fill "#333"] []

viewCursor : Model -> List (Svg.Svg Msg)
viewCursor m =
  let html pos =
    let viewForm = Html.form [
                    Events.onSubmit (EntrySubmitMsg)
                   ] [
                    Html.input [ Attrs.id Defaults.entryID
                               , Events.on "keydown" entryKeyHandler
                               , Events.onInput EntryInputMsg
                               , Attrs.width 50
                               , Attrs.value m.entryValue
                               , Attrs.autocomplete False
                               ] []
                   ]

        -- inner node
        inner = Html.div
                [ Attrs.width 100
                , Attrs.class "inner"]
                [viewForm]


        -- outer node wrapper
        classes = "selection function node"

        wrapper = Html.span
                  [ Attrs.class classes
                  , Attrs.width 100]
                  [ inner ]
      in
        placeHtml pos wrapper
  in
    case m.cursor of
      Filling n pos -> [html pos, svgLine n.pos pos dragEdgeStyle]
      Creating pos -> [html pos]
      Dragging _ -> []
      Deselected -> []





nodeWidth : Node -> Int
nodeWidth n =
  let
    slimChars = Set.fromList Defaults.narrowChars
    len name =
      n
        |> nodeName
        |> String.toList
        |> List.map (\c -> if Set.member c slimChars then 0.5 else 1)
        |> List.sum
    nameMultiple = case n.tipe of
                     Datastore -> 2
                     Page -> 2.2
                     _ -> 1
    ln = [nameMultiple * len n.name]
    lf = List.map (\(n,t) -> len n + len t + 3) n.fields
    charWidth = List.foldl max 2 (ln ++ lf)
    width = charWidth * 12
  in
    round(width)

nodeHeight : Node -> Int
nodeHeight n =
  case n.tipe of
    Datastore -> Defaults.nodeHeight * ( 1 + (List.length n.fields))
    _ -> Defaults.nodeHeight

nodeSize node =
  (nodeWidth node , nodeHeight node)

nodeName n =
  let defaultParam = "◉"
      parameterTexts = List.map (\p -> case Dict.get p n.constants of
                                         Just c -> c
                                         Nothing -> defaultParam) n.parameters

  in
    String.join " " (n.name :: parameterTexts)


-- TODO: Allow selecting an edge, then highlight it and show its source and target
-- TODO: If there are default parameters, show them inline in the node body
-- TODO: could maybe use little icons to denote the params
viewNode : Model -> Node -> Int -> Html.Html Msg
viewNode m n i =
  let
      -- params
      slotHandler name = (decodeClickEvent (DragSlotStart n name))
      connected name = if G.slotIsConnected m n.id name
                       then "connected"
                       else "disconnected"
      viewParam name = Html.span
                       [ Events.on "mousedown" (slotHandler name)
                       , Attrs.title name
                       , Attrs.class (connected name)]
                       [Html.text "◉"]

      -- header
      viewHeader = Html.div
                   [Attrs.class "header"]
                     [ Html.span
                         [Attrs.class "parameters"]
                         (List.map viewParam n.parameters)
                     , Html.span
                         [Attrs.class "letter"]
                         [Html.text (G.int2letter i)]
                     ]

      -- heading
      heading = Html.span
                [ Attrs.class "name"]
                [ Html.text (nodeName n) ]

      -- fields (in list)
      viewField (name, tipe) = [ Html.text (name ++ " : " ++ tipe)
                               , Html.br [] []]
      viewFields = List.map viewField n.fields

      -- list
      list = if viewFields /= []
             then
               [Html.span
                 [Attrs.class "list"]
                 (List.concat viewFields)]
             else []

       -- width
      width = Attrs.style [("width",
                            (toString (nodeWidth n)) ++ "px")]
      -- events
      events =
        [ Events.onClick (NodeClick n)
        , Events.on "mousedown" (decodeClickEvent (DragNodeStart n))
        , Events.onMouseUp (DragSlotEnd n)]

      -- inner node
      inner = Html.div
              (width :: (Attrs.class "inner") :: events)
              (viewHeader :: heading :: list)


      -- outer node wrapper
      selected = Canvas.isSelected m n
      selectedCl = if selected then ["selected"] else []
      class = String.toLower (toString n.tipe)
      classes = String.join " " (["node", class] ++ selectedCl)

      wrapper = Html.span
                [ Attrs.class classes, width]
                [ inner ]
  in
    placeHtml n.pos wrapper

viewLive : Model -> Cursor -> Html.Html Msg
viewLive m cursor =
  let live =
        cursor
          |> Canvas.getCursorID
          |> Maybe.andThen (G.getNode m)
          |> Maybe.map .live
  in
    Html.div
      [Attrs.id "darkLive"]
      [Html.text <|
          case live of
            Just (val, tipe) -> "LiveValue: " ++ val ++ " (" ++ tipe ++ ")"
            Nothing -> "n/a"

      ]

-- Our edges should be a lineargradient from "darker" to "arrowColor". SVG
-- gradients are weird, they don't allow you specify based on the line
-- direction, but only on the absolute direction. So we define 8 linear
-- gradients, one for each 45 degree angle/direction. We define this in terms of
-- "rise over run" (eg like you'd calculate a slope). Then we translate the x,y
-- source/target positions into (rise,run) in the integer range [-1,0,1].
svgDefs =
  Svg.defs []
    [ linearGradient 0 1
    , linearGradient 1 1
    , linearGradient 1 0
    , linearGradient 1 -1
    , linearGradient 0 -1
    , linearGradient -1 -1
    , linearGradient -1 0
    , linearGradient -1 1
    ]

coord2id rise run =
  "linear-rise" ++ toString rise ++ "-run" ++ toString run


linearGradient : Int -> Int -> Svg.Svg a
linearGradient rise run =
  -- edge case, linearGradients use positive integers
  let (x1, x2) = if run == -1 then (1,0) else (0, run)
      (y1, y2) = if rise == -1 then (1,0) else (0, rise)
  in
    Svg.linearGradient
      [ SA.id (coord2id rise run)
      , SA.x1 (toString x1)
      , SA.y1 (toString y1)
      , SA.x2 (toString x2)
      , SA.y2 (toString y2)]
    [ Svg.stop [ SA.offset "0%"
               , SA.stopColor Defaults.edgeGradColor] []
    , Svg.stop [ SA.offset "100%"
               , SA.stopColor Defaults.edgeColor] []]

dragEdgeStyle =
  [ SA.strokeWidth Defaults.dragEdgeSize
  , SA.stroke Defaults.dragEdgeStrokeColor
  ]

edgeStyle x1 y1 x2 y2 =
  -- edge case: We don't want to use a vertical gradient for really tiny rises,
  -- or it'll just be one color (same for the run). 20 seems enough to avoid
  -- this, empirically.
  let rise = if y2 - y1 > 20 then 1 else if y2 - y1 < -20 then -1 else 0
      run = if x2 - x1 > 20 then 1 else if x2 - x1 < -20 then -1 else 0
      -- edge case: (0,0) is nothing; go in range.
      amendedRise = if (rise,run) == (0,0)
                    then if y2 - y1 > 0 then 1 else -1
                    else rise
  in [ SA.strokeWidth Defaults.edgeSize
     , SA.stroke ("url(#" ++ coord2id amendedRise run ++ ")")
     , SA.markerEnd "url(#triangle)"
     ]

svgLine : Pos -> Pos -> List (Svg.Attribute Msg) -> Svg.Svg Msg
svgLine p1 p2 attrs =
  -- edge case: avoid zero width/height lines, or they won't appear
  let ( x1, y1, x2_, y2_ ) = (p1.x, p1.y, p2.x, p2.y)
      x2 = if x1 == x2_ then x2_ + 1 else x2_
      y2 = if y1 == y2_ then y2_ + 1 else y2_
  in
  Svg.line
    ([ SA.x1 (toString x1)
     , SA.y1 (toString y1)
     , SA.x2 (toString x2)
     , SA.y2 (toString y2)
     ] ++ attrs)
    []

viewDragEdge : Drag -> Pos -> Maybe (Svg.Svg Msg)
viewDragEdge drag currentPos =
  case drag of
    DragNode _ _ -> Nothing
    NoDrag -> Nothing
    DragSlot node param mStartPos ->
      Just <|
        svgLine mStartPos
                currentPos
                dragEdgeStyle

viewEdge : Model -> Edge -> Svg.Svg Msg
viewEdge m {source, target, param} =
    let sourceN = G.getNodeExn m source
        targetN = G.getNodeExn m target
        targetPos = targetN.pos
        (sourceW, sourceH) = nodeSize sourceN

        pOffset = Canvas.paramOffset targetN param
        (tnx, tny) = (targetN.pos.x + pOffset.x, targetN.pos.y + pOffset.y)

        -- find the shortest line and link to there
        joins = [ (tnx, tny) -- topleft
                , (tnx + 5, tny) -- topright
                , (tnx, tny + 5) -- bottomleft
                , (tnx + 5, tny + 5) -- bottomright
                ]
        sq x = toFloat (x*x)
        -- ideally to source pos would be at the bottom of the node. But, the
        -- positioning of the node is a little bit off because css, and nodes
        -- with parameters are in different relative offsets than nodes without
        -- parameters. This makes it hard to line things up exactly.
        spos = { x = sourceN.pos.x + (sourceW // 2)
               , y = sourceN.pos.y + (sourceH // 2)}

        join = List.head
               (List.sortBy (\(x,y) -> sqrt ((sq (spos.x - x)) + (sq (spos.y - y))))
                  joins)
        (tx, ty) = deMaybe join
    in svgLine
      spos
      {x=tx,y=ty}
      (edgeStyle spos.x spos.y tx ty)

svgArrowHead =
  Svg.marker [ SA.id "triangle"
             , SA.viewBox "0 0 10 10"
             , SA.refX "4"
             , SA.refY "5"
             , SA.markerUnits "strokeWidth"
             , SA.markerWidth "4"
             , SA.markerHeight "4"
             , SA.orient "auto"
             , SA.fill Defaults.edgeColor
             ]
    [Svg.path [SA.d "M 0 0 L 5 5 L 0 10 z"] []]

decodeClickEvent : (MouseEvent -> a) -> JSD.Decoder a
decodeClickEvent fn =
  let toA : Int -> Int -> Int -> a
      toA px py button =
        fn {pos= {x=px, y=py}, button = button}
  in JSDP.decode toA
      |> JSDP.required "pageX" JSD.int
      |> JSDP.required "pageY" JSD.int
      |> JSDP.required "button" JSD.int

entryKeyHandler = JSD.map EntryKeyPress Keyboard.Event.decodeKeyboardEvent
