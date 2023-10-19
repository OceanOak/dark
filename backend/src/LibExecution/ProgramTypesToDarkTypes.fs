module LibExecution.ProgramTypesToDarkTypes

open Prelude

open RuntimeTypes
module VT = ValueType
module PT = ProgramTypes
module NRE = LibExecution.NameResolutionError
module D = LibExecution.DvalDecoder

let languageToolsTyp
  (submodules : List<string>)
  (name : string)
  (version : int)
  : TypeName.TypeName =
  TypeName.fqPackage "Darklang" ("LanguageTools" :: submodules) name version


let ptTyp
  (submodules : List<string>)
  (name : string)
  (version : int)
  : TypeName.TypeName =
  languageToolsTyp ("ProgramTypes" :: submodules) name version

let errorsTyp
  (submodules : List<string>)
  (name : string)
  (version : int)
  : TypeName.TypeName =
  languageToolsTyp ("Errors" :: submodules) name version


// This isn't in PT but I'm not sure where else to put it...
// maybe rename this file to InternalTypesToDarkTypes?
module Sign =
  let toDT (s : Sign) : Dval =
    let typeName = languageToolsTyp [] "Sign" 0

    let (caseName, fields) =
      match s with
      | Positive -> "Positive", []
      | Negative -> "Negative", []

    DEnum(typeName, typeName, [], caseName, fields)


  let fromDT (d : Dval) : Sign =
    match d with
    //TODO: ensure that we are working with a right type
    | DEnum(_, _, [], "Positive", []) -> Positive
    | DEnum(_, _, [], "Negative", []) -> Negative
    | _ -> Exception.raiseInternal "Invalid sign" []



module FQName =
  let ownerField = D.stringField "owner"
  let modulesField = D.stringListField "modules"

  let nameField = D.field "name"
  let versionField = D.intField "version"

  module BuiltIn =
    let toDT
      (nameType : KnownType)
      (nameMapper : 'name -> Dval)
      (u : PT.FQName.BuiltIn<'name>)
      : Dval =
      let typeName = ptTyp [ "FQName" ] "BuiltIn" 0
      let fields =
        [ "modules", DList(VT.string, List.map DString u.modules)
          "name", nameMapper u.name
          "version", DInt u.version ]
      DRecord(typeName, typeName, [ VT.known nameType ], Map fields)

    let fromDT (nameMapper : Dval -> 'name) (d : Dval) : PT.FQName.BuiltIn<'name> =
      match d with
      | DRecord(_, _, _, fields) ->
        { modules = fields |> modulesField
          name = fields |> nameField |> nameMapper
          version = fields |> versionField }

      | _ -> Exception.raiseInternal "Unexpected value" []

  module UserProgram =
    let toDT
      (nameType : KnownType)
      (nameMapper : 'name -> Dval)
      (u : PT.FQName.UserProgram<'name>)
      : Dval =
      let typeName = ptTyp [ "FQName" ] "UserProgram" 0
      let fields =
        [ "modules", DList(VT.string, List.map DString u.modules)
          "name", nameMapper u.name
          "version", DInt u.version ]
      DRecord(typeName, typeName, [ VT.known nameType ], Map fields)

    let fromDT
      (nameMapper : Dval -> 'name)
      (v : Dval)
      : PT.FQName.UserProgram<'name> =
      match v with
      | DRecord(_, _, _, fields) ->
        { modules = fields |> modulesField
          name = fields |> nameField |> nameMapper
          version = fields |> versionField }

      | _ -> Exception.raiseInternal "Unexpected value" []

  module Package =
    let toDT
      (nameType : KnownType)
      (nameMapper : 'name -> Dval)
      (u : PT.FQName.Package<'name>)
      : Dval =
      let typeName = ptTyp [ "FQName" ] "Package" 0
      let fields =
        [ "owner", DString u.owner
          "modules", DList(VT.string, List.map DString u.modules)
          "name", nameMapper u.name
          "version", DInt u.version ]
      DRecord(typeName, typeName, [ VT.known nameType ], Map fields)

    let fromDT (nameMapper : Dval -> 'name) (d : Dval) : PT.FQName.Package<'name> =
      match d with
      | DRecord(_, _, _, fields) ->
        { owner = fields |> ownerField
          modules = fields |> modulesField
          name = fields |> nameField |> nameMapper
          version = fields |> versionField }
      | _ -> Exception.raiseInternal "Unexpected value" []


  let toDT
    (nameType : KnownType)
    (nameMapper : 'name -> Dval)
    (u : PT.FQName.FQName<'name>)
    : Dval =
    let typeName = ptTyp [ "FQName" ] "FQName" 0

    let (caseName, fields) =
      match u with
      | PT.FQName.UserProgram u ->
        "UserProgram", [ UserProgram.toDT nameType nameMapper u ]
      | PT.FQName.Package u -> "Package", [ Package.toDT nameType nameMapper u ]
      | PT.FQName.BuiltIn u -> "BuiltIn", [ BuiltIn.toDT nameType nameMapper u ]

    DEnum(
      typeName,
      typeName,
      Dval.ignoreAndUseEmpty [ VT.known nameType ],
      caseName,
      fields
    )

  let fromDT (nameMapper : Dval -> 'name) (d : Dval) : PT.FQName.FQName<'name> =
    match d with
    | DEnum(_, _, _typeArgsDEnumTODO, "UserProgram", [ u ]) ->
      PT.FQName.UserProgram(UserProgram.fromDT nameMapper u)
    | DEnum(_, _, _typeArgsDEnumTODO, "Package", [ u ]) ->
      PT.FQName.Package(Package.fromDT nameMapper u)
    | DEnum(_, _, _typeArgsDEnumTODO, "BuiltIn", [ u ]) ->
      PT.FQName.BuiltIn(BuiltIn.fromDT nameMapper u)
    | _ -> Exception.raiseInternal "Invalid FQName" []


module TypeName =

  module Name =
    let knownType = KTCustomType(ptTyp [ "TypeName" ] "Name" 0, [])

    let toDT (u : PT.TypeName.Name) : Dval =
      let typeName = ptTyp [ "TypeName" ] "Name" 0

      let (caseName, fields) =
        match u with
        | PT.TypeName.TypeName name -> "TypeName", [ DString name ]

      DEnum(typeName, typeName, [], caseName, fields)

    let fromDT (d : Dval) : PT.TypeName.Name =
      match d with
      | DEnum(_, _, [], "TypeName", [ DString name ]) -> PT.TypeName.TypeName(name)
      | _ -> Exception.raiseInternal "Invalid TypeName" []

  module BuiltIn =
    let toDT (u : PT.TypeName.BuiltIn) : Dval =
      FQName.BuiltIn.toDT Name.knownType Name.toDT u
    let fromDT (d : Dval) : PT.TypeName.BuiltIn = FQName.BuiltIn.fromDT Name.fromDT d

  module UserProgram =
    let toDT (u : PT.TypeName.UserProgram) : Dval =
      FQName.UserProgram.toDT Name.knownType Name.toDT u
    let fromDT (d : Dval) : PT.TypeName.UserProgram =
      FQName.UserProgram.fromDT Name.fromDT d

  module Package =
    let toDT (u : PT.TypeName.Package) : Dval =
      FQName.Package.toDT Name.knownType Name.toDT u
    let fromDT (d : Dval) : PT.TypeName.Package = FQName.Package.fromDT Name.fromDT d

  let knownType = KTCustomType(ptTyp [ "TypeName" ] "TypeName" 0, [])
  let toDT (u : PT.TypeName.TypeName) : Dval = FQName.toDT Name.knownType Name.toDT u
  let fromDT (d : Dval) : PT.TypeName.TypeName = FQName.fromDT Name.fromDT d


module FnName =

  module Name =
    let knownType = KTCustomType(ptTyp [ "FnName" ] "Name" 0, [])

    let toDT (u : PT.FnName.Name) : Dval =
      let typeName = ptTyp [ "FnName" ] "Name" 0

      let (caseName, fields) =
        match u with
        | PT.FnName.FnName name -> "FnName", [ DString name ]

      DEnum(typeName, typeName, [], caseName, fields)

    let fromDT (d : Dval) : PT.FnName.Name =
      match d with
      | DEnum(_, _, [], "FnName", [ DString name ]) -> PT.FnName.FnName(name)
      | _ -> Exception.raiseInternal "Invalid FnName" []

  module BuiltIn =
    let toDT (u : PT.FnName.BuiltIn) : Dval =
      FQName.BuiltIn.toDT Name.knownType Name.toDT u

    let fromDT (d : Dval) : PT.FnName.BuiltIn = FQName.BuiltIn.fromDT Name.fromDT d

  module UserProgram =
    let toDT (u : PT.FnName.UserProgram) : Dval =
      FQName.UserProgram.toDT Name.knownType Name.toDT u
    let fromDT (d : Dval) : PT.FnName.UserProgram =
      FQName.UserProgram.fromDT Name.fromDT d

  module Package =
    let toDT (u : PT.FnName.Package) : Dval =
      FQName.Package.toDT Name.knownType Name.toDT u
    let fromDT (d : Dval) : PT.FnName.Package = FQName.Package.fromDT Name.fromDT d

  let knownType = KTCustomType(ptTyp [ "FnName" ] "FnName" 0, [])
  let toDT (u : PT.FnName.FnName) : Dval = FQName.toDT Name.knownType Name.toDT u
  let fromDT (d : Dval) : PT.FnName.FnName = FQName.fromDT Name.fromDT d

module ConstantName =
  module Name =
    let knownType = KTCustomType(ptTyp [ "ConstantName" ] "Name" 0, [])

    let toDT (u : PT.ConstantName.Name) : Dval =
      let typeName = ptTyp [ "ConstantName" ] "Name" 0
      let (caseName, fields) =
        match u with
        | PT.ConstantName.ConstantName name -> "ConstantName", [ DString name ]
      DEnum(typeName, typeName, [], caseName, fields)

    let fromDT (d : Dval) : PT.ConstantName.Name =
      match d with
      | DEnum(_, _, [], "ConstantName", [ DString name ]) ->
        PT.ConstantName.ConstantName(name)
      | _ -> Exception.raiseInternal "Invalid ConstantName" []

  module BuiltIn =
    let toDT (u : PT.ConstantName.BuiltIn) : Dval =
      FQName.BuiltIn.toDT Name.knownType Name.toDT u

    let fromDT (d : Dval) : PT.ConstantName.BuiltIn =
      FQName.BuiltIn.fromDT Name.fromDT d

  module UserProgram =
    let toDT (u : PT.ConstantName.UserProgram) : Dval =
      FQName.UserProgram.toDT Name.knownType Name.toDT u

    let fromDT (d : Dval) : PT.ConstantName.UserProgram =
      FQName.UserProgram.fromDT Name.fromDT d

  module Package =
    let toDT (u : PT.ConstantName.Package) : Dval =
      FQName.Package.toDT Name.knownType Name.toDT u

    let fromDT (d : Dval) : PT.ConstantName.Package =
      FQName.Package.fromDT Name.fromDT d

  let knownType = KTCustomType(ptTyp [ "ConstantName" ] "ConstantName" 0, [])

  let toDT (u : PT.ConstantName.ConstantName) : Dval =
    FQName.toDT Name.knownType Name.toDT u

  let fromDT (d : Dval) : PT.ConstantName.ConstantName = FQName.fromDT Name.fromDT d

module NameResolution =
  let toDT
    (nameValueType : KnownType)
    (f : 'p -> Dval)
    (result : PT.NameResolution<'p>)
    : Dval =
    let errType = KTCustomType(NameResolutionError.RTE.Error.typeName, [])

    match result with
    | Ok name -> Dval.resultOk nameValueType errType (f name)
    | Error err -> Dval.resultError nameValueType errType (NRE.RTE.Error.toDT err)

  let fromDT (f : Dval -> 'a) (d : Dval) : PT.NameResolution<'a> =
    match d with
    | DEnum(tn, _, _typeArgsDEnumTODO, "Ok", [ v ]) when tn = Dval.resultType ->
      Ok(f v)

    | DEnum(tn, _, _typeArgsDEnumTODO, "Error", [ v ]) when tn = Dval.resultType ->
      Error(NRE.RTE.Error.fromDT v)

    | _ -> Exception.raiseInternal "Invalid NameResolution" []

module TypeReference =
  let knownType = KTCustomType(ptTyp [] "TypeReference" 0, [])

  let rec toDT (t : PT.TypeReference) : Dval =
    let (caseName, fields) =
      match t with
      | PT.TVariable name -> "TVariable", [ DString name ]

      | PT.TUnit -> "TUnit", []
      | PT.TBool -> "TBool", []
      | PT.TInt -> "TInt", []
      | PT.TInt8 -> "TInt8", []
      | PT.TFloat -> "TFloat", []
      | PT.TChar -> "TChar", []
      | PT.TString -> "TString", []
      | PT.TDateTime -> "TDateTime", []
      | PT.TUuid -> "TUuid", []
      | PT.TBytes -> "TBytes", []

      | PT.TList inner -> "TList", [ toDT inner ]

      | PT.TTuple(first, second, theRest) ->
        "TTuple",
        [ toDT first; toDT second; DList(VT.known knownType, List.map toDT theRest) ]

      | PT.TDict inner -> "TDict", [ toDT inner ]

      | PT.TCustomType(typeName, typeArgs) ->
        "TCustomType",
        [ NameResolution.toDT TypeName.knownType TypeName.toDT typeName
          DList(VT.known knownType, List.map toDT typeArgs) ]

      | PT.TDB inner -> "TDB", [ toDT inner ]

      | PT.TFn(args, ret) ->
        "TFn",
        [ DList(VT.known knownType, args |> NEList.toList |> List.map toDT)
          toDT ret ]

    let typeName = ptTyp [] "TypeReference" 0
    DEnum(typeName, typeName, [], caseName, fields)

  let rec fromDT (d : Dval) : PT.TypeReference =
    match d with
    | DEnum(_, _, [], "TVariable", [ DString name ]) -> PT.TVariable(name)

    | DEnum(_, _, [], "TUnit", []) -> PT.TUnit
    | DEnum(_, _, [], "TBool", []) -> PT.TBool
    | DEnum(_, _, [], "TInt", []) -> PT.TInt
    | DEnum(_, _, [], "TInt8", []) -> PT.TInt8
    | DEnum(_, _, [], "TFloat", []) -> PT.TFloat
    | DEnum(_, _, [], "TChar", []) -> PT.TChar
    | DEnum(_, _, [], "TString", []) -> PT.TString
    | DEnum(_, _, [], "TDateTime", []) -> PT.TDateTime
    | DEnum(_, _, [], "TUuid", []) -> PT.TUuid
    | DEnum(_, _, [], "TBytes", []) -> PT.TBytes

    | DEnum(_, _, [], "TList", [ inner ]) -> PT.TList(fromDT inner)

    | DEnum(_, _, [], "TTuple", [ first; second; DList(_vtTODO, theRest) ]) ->
      PT.TTuple(fromDT first, fromDT second, List.map fromDT theRest)

    | DEnum(_, _, [], "TDict", [ inner ]) -> PT.TDict(fromDT inner)

    | DEnum(_, _, [], "TCustomType", [ typeName; DList(_vtTODO, typeArgs) ]) ->
      PT.TCustomType(
        NameResolution.fromDT TypeName.fromDT typeName,
        List.map fromDT typeArgs
      )

    | DEnum(_, _, [], "TDB", [ inner ]) -> PT.TDB(fromDT inner)
    | DEnum(_, _, [], "TFn", [ DList(_vtTODO, head :: tail); ret ]) ->
      PT.TFn(NEList.ofList head tail |> NEList.map fromDT, fromDT ret)
    | _ -> Exception.raiseInternal "Invalid TypeReference" []


module LetPattern =
  let knownType = KTCustomType(ptTyp [] "LetPattern" 0, [])

  let rec toDT (p : PT.LetPattern) : Dval =
    let (caseName, fields) =
      match p with
      | PT.LPVariable(id, name) -> "LPVariable", [ DInt(int64 id); DString name ]
      | PT.LPUnit id -> "LPUnit", [ DInt(int64 id) ]
      | PT.LPTuple(id, first, second, theRest) ->
        "LPTuple",
        [ DInt(int64 id)
          toDT first
          toDT second
          DList(VT.known knownType, List.map toDT theRest) ]

    let typeName = ptTyp [] "LetPattern" 0
    DEnum(typeName, typeName, [], caseName, fields)


  let rec fromDT (d : Dval) : PT.LetPattern =
    match d with
    | DEnum(_, _, [], "LPVariable", [ DInt id; DString name ]) ->
      PT.LPVariable(uint64 id, name)
    | DEnum(_, _, [], "LPUnit", [ DInt id ]) -> PT.LPUnit(uint64 id)
    | DEnum(_, _, [], "LPTuple", [ DInt id; first; second; DList(_vtTODO, theRest) ]) ->
      PT.LPTuple(uint64 id, fromDT first, fromDT second, List.map fromDT theRest)
    | _ -> Exception.raiseInternal "Invalid LetPattern" []


module MatchPattern =
  let knownType = KTCustomType(ptTyp [] "MatchPattern" 0, [])

  let rec toDT (p : PT.MatchPattern) : Dval =
    let (caseName, fields) =
      match p with
      | PT.MPVariable(id, name) -> "MPVariable", [ DInt(int64 id); DString name ]

      | PT.MPUnit id -> "MPUnit", [ DInt(int64 id) ]
      | PT.MPBool(id, b) -> "MPBool", [ DInt(int64 id); DBool b ]
      | PT.MPInt(id, i) -> "MPInt", [ DInt(int64 id); DInt i ]
      | PT.MPInt8(id, i) -> "MPInt8", [ DInt(int64 id); DInt8 i ]
      | PT.MPFloat(id, sign, whole, remainder) ->

        "MPFloat",
        [ DInt(int64 id); Sign.toDT sign; DString whole; DString remainder ]
      | PT.MPChar(id, c) -> "MPChar", [ DInt(int64 id); DString c ]
      | PT.MPString(id, s) -> "MPString", [ DInt(int64 id); DString s ]

      | PT.MPList(id, inner) ->
        "MPList", [ DInt(int64 id); DList(VT.known knownType, List.map toDT inner) ]
      | PT.MPListCons(id, head, tail) ->
        "MPListCons", [ DInt(int64 id); toDT head; toDT tail ]
      | PT.MPTuple(id, first, second, theRest) ->
        "MPTuple",
        [ DInt(int64 id)
          toDT first
          toDT second
          DList(VT.known knownType, List.map toDT theRest) ]
      | PT.MPEnum(id, caseName, fieldPats) ->
        "MPEnum",
        [ DInt(int64 id)
          DString caseName
          DList(VT.known knownType, List.map toDT fieldPats) ]

    let typeName = ptTyp [] "MatchPattern" 0
    DEnum(typeName, typeName, [], caseName, fields)

  let rec fromDT (d : Dval) : PT.MatchPattern =
    match d with
    | DEnum(_, _, [], "MPVariable", [ DInt id; DString name ]) ->
      PT.MPVariable(uint64 id, name)

    | DEnum(_, _, [], "MPUnit", [ DInt id ]) -> PT.MPUnit(uint64 id)
    | DEnum(_, _, [], "MPBool", [ DInt id; DBool b ]) -> PT.MPBool(uint64 id, b)
    | DEnum(_, _, [], "MPInt", [ DInt id; DInt i ]) -> PT.MPInt(uint64 id, i)
    | DEnum(_, _, [], "MPInt8", [ DInt id; DInt8 i ]) -> PT.MPInt8(uint64 id, i)
    | DEnum(_, _, [], "MPFloat", [ DInt id; sign; DString whole; DString remainder ]) ->
      PT.MPFloat(uint64 id, Sign.fromDT sign, whole, remainder)
    | DEnum(_, _, [], "MPChar", [ DInt id; DString c ]) -> PT.MPChar(uint64 id, c)
    | DEnum(_, _, [], "MPString", [ DInt id; DString s ]) ->
      PT.MPString(uint64 id, s)

    | DEnum(_, _, [], "MPList", [ DInt id; DList(_vtTODO, inner) ]) ->
      PT.MPList(uint64 id, List.map fromDT inner)
    | DEnum(_, _, [], "MPListCons", [ DInt id; head; tail ]) ->
      PT.MPListCons(uint64 id, fromDT head, fromDT tail)
    | DEnum(_, _, [], "MPTuple", [ DInt id; first; second; DList(_vtTODO, theRest) ]) ->
      PT.MPTuple(uint64 id, fromDT first, fromDT second, List.map fromDT theRest)
    | DEnum(_,
            _,
            [],
            "MPEnum",
            [ DInt id; DString caseName; DList(_vtTODO, fieldPats) ]) ->
      PT.MPEnum(uint64 id, caseName, List.map fromDT fieldPats)
    | _ -> Exception.raiseInternal "Invalid MatchPattern" []


module BinaryOperation =
  let toDT (b : PT.BinaryOperation) : Dval =
    let (caseName, fields) =
      match b with
      | PT.BinOpAnd -> "BinOpAnd", []
      | PT.BinOpOr -> "BinOpOr", []

    let typeName = ptTyp [] "BinaryOperation" 0
    DEnum(typeName, typeName, [], caseName, fields)

  let fromDT (d : Dval) : PT.BinaryOperation =
    match d with
    | DEnum(_, _, [], "BinOpAnd", []) -> PT.BinOpAnd
    | DEnum(_, _, [], "BinOpOr", []) -> PT.BinOpOr
    | _ -> Exception.raiseInternal "Invalid BinaryOperation" []


module InfixFnName =
  let toDT (i : PT.InfixFnName) : Dval =
    let (caseName, fields) =
      match i with
      | PT.ArithmeticPlus -> "ArithmeticPlus", []
      | PT.ArithmeticMinus -> "ArithmeticMinus", []
      | PT.ArithmeticMultiply -> "ArithmeticMultiply", []
      | PT.ArithmeticDivide -> "ArithmeticDivide", []
      | PT.ArithmeticModulo -> "ArithmeticModulo", []
      | PT.ArithmeticPower -> "ArithmeticPower", []
      | PT.ComparisonGreaterThan -> "ComparisonGreaterThan", []
      | PT.ComparisonGreaterThanOrEqual -> "ComparisonGreaterThanOrEqual", []
      | PT.ComparisonLessThan -> "ComparisonLessThan", []
      | PT.ComparisonLessThanOrEqual -> "ComparisonLessThanOrEqual", []
      | PT.ComparisonEquals -> "ComparisonEquals", []
      | PT.ComparisonNotEquals -> "ComparisonNotEquals", []
      | PT.StringConcat -> "StringConcat", []

    let typeName = ptTyp [] "InfixFnName" 0
    DEnum(typeName, typeName, [], caseName, fields)

  let fromDT (d : Dval) : PT.InfixFnName =
    match d with
    | DEnum(_, _, [], "ArithmeticPlus", []) -> PT.ArithmeticPlus
    | DEnum(_, _, [], "ArithmeticMinus", []) -> PT.ArithmeticMinus
    | DEnum(_, _, [], "ArithmeticMultiply", []) -> PT.ArithmeticMultiply
    | DEnum(_, _, [], "ArithmeticDivide", []) -> PT.ArithmeticDivide
    | DEnum(_, _, [], "ArithmeticModulo", []) -> PT.ArithmeticModulo
    | DEnum(_, _, [], "ArithmeticPower", []) -> PT.ArithmeticPower
    | DEnum(_, _, [], "ComparisonGreaterThan", []) -> PT.ComparisonGreaterThan
    | DEnum(_, _, [], "ComparisonGreaterThanOrEqual", []) ->
      PT.ComparisonGreaterThanOrEqual
    | DEnum(_, _, [], "ComparisonLessThan", []) -> PT.ComparisonLessThan
    | DEnum(_, _, [], "ComparisonLessThanOrEqual", []) ->
      PT.ComparisonLessThanOrEqual
    | DEnum(_, _, [], "ComparisonEquals", []) -> PT.ComparisonEquals
    | DEnum(_, _, [], "ComparisonNotEquals", []) -> PT.ComparisonNotEquals
    | DEnum(_, _, [], "StringConcat", []) -> PT.StringConcat
    | _ -> Exception.raiseInternal "Invalid InfixFnName" []


module Infix =
  let toDT (i : PT.Infix) : Dval =
    let (caseName, fields) =
      match i with
      | PT.InfixFnCall infixFnName -> "InfixFnCall", [ InfixFnName.toDT infixFnName ]
      | PT.BinOp binOp -> "BinOp", [ BinaryOperation.toDT binOp ]

    let typeName = ptTyp [] "Infix" 0
    DEnum(typeName, typeName, [], caseName, fields)

  let fromDT (d : Dval) : PT.Infix =
    match d with
    | DEnum(_, _, [], "InfixFnCall", [ infixFnName ]) ->
      PT.InfixFnCall(InfixFnName.fromDT infixFnName)
    | DEnum(_, _, [], "BinOp", [ binOp ]) -> PT.BinOp(BinaryOperation.fromDT binOp)
    | _ -> Exception.raiseInternal "Invalid Infix" []


module StringSegment =
  let knownType = KTCustomType(ptTyp [] "StringSegment" 0, [])

  let toDT (exprToDT : PT.Expr -> Dval) (s : PT.StringSegment) : Dval =
    let (caseName, fields) =
      match s with
      | PT.StringText text -> "StringText", [ DString text ]
      | PT.StringInterpolation expr -> "StringInterpolation", [ exprToDT expr ]

    let typeName = ptTyp [] "StringSegment" 0
    DEnum(typeName, typeName, [], caseName, fields)

  let fromDT (exprFromDT : Dval -> PT.Expr) (d : Dval) : PT.StringSegment =
    match d with
    | DEnum(_, _, [], "StringText", [ DString text ]) -> PT.StringText text
    | DEnum(_, _, [], "StringInterpolation", [ expr ]) ->
      PT.StringInterpolation(exprFromDT expr)
    | _ -> Exception.raiseInternal "Invalid StringSegment" []


module PipeExpr =
  let knownType = KTCustomType(ptTyp [] "PipeExpr" 0, [])

  let toDT
    (exprKT : KnownType)
    (exprToDT : PT.Expr -> Dval)
    (s : PT.PipeExpr)
    : Dval =
    let (caseName, fields) =
      match s with
      | PT.EPipeVariable(id, varName, exprs) ->
        "EPipeVariable",
        [ DInt(int64 id)
          DString varName
          DList(VT.known exprKT, List.map exprToDT exprs) ]

      | PT.EPipeLambda(id, args, body) ->
        let variables =
          args
          |> NEList.toList
          |> List.map LetPattern.toDT
          |> Dval.list (KTTuple(VT.int, VT.string, []))
        "EPipeLambda", [ DInt(int64 id); variables; exprToDT body ]

      | PT.EPipeInfix(id, infix, expr) ->
        "EPipeInfix", [ DInt(int64 id); Infix.toDT infix; exprToDT expr ]

      | PT.EPipeFnCall(id, fnName, typeArgs, args) ->
        "EPipeFnCall",
        [ DInt(int64 id)
          NameResolution.toDT FnName.knownType FnName.toDT fnName
          DList(
            VT.known TypeReference.knownType,
            List.map TypeReference.toDT typeArgs
          )
          DList(VT.known exprKT, List.map exprToDT args) ]

      | PT.EPipeEnum(id, typeName, caseName, fields) ->
        "EPipeEnum",
        [ DInt(int64 id)
          NameResolution.toDT TypeName.knownType TypeName.toDT typeName
          DString caseName
          DList(VT.known exprKT, List.map exprToDT fields) ]


    let typeName = ptTyp [] "PipeExpr" 0
    DEnum(typeName, typeName, [], caseName, fields)


  let fromDT (exprFromDT : Dval -> PT.Expr) (d : Dval) : PT.PipeExpr =
    match d with
    | DEnum(_,
            _,
            [],
            "EPipeVariable",
            [ DInt id; DString varName; DList(_vtTODO, args) ]) ->
      PT.EPipeVariable(uint64 id, varName, args |> List.map exprFromDT)

    | DEnum(_, _, [], "EPipeLambda", [ DInt id; variables; body ]) ->
      let variables =
        match variables with
        | DList(_vtTODO, pats) ->
          pats
          |> List.map LetPattern.fromDT
          |> NEList.ofListUnsafe
            "PT2DT.PipeExpr.fromDT expected at least one bound variable in EPipeLambda"
            []
        | _ -> Exception.raiseInternal "Invalid variables" []

      PT.EPipeLambda(uint64 id, variables, exprFromDT body)

    | DEnum(_, _, [], "EPipeInfix", [ DInt id; infix; expr ]) ->
      PT.EPipeInfix(uint64 id, Infix.fromDT infix, exprFromDT expr)

    | DEnum(_,
            _,
            [],
            "EPipeFnCall",
            [ DInt id; fnName; DList(_vtTODO1, typeArgs); DList(_vtTODO2, args) ]) ->
      PT.EPipeFnCall(
        uint64 id,
        NameResolution.fromDT FnName.fromDT fnName,
        List.map TypeReference.fromDT typeArgs,
        List.map exprFromDT args
      )

    | DEnum(_,
            _,
            [],
            "EPipeEnum",
            [ DInt id; typeName; DString caseName; DList(_vtTODO, fields) ]) ->
      PT.EPipeEnum(
        uint64 id,
        NameResolution.fromDT TypeName.fromDT typeName,
        caseName,
        List.map exprFromDT fields
      )

    | _ -> Exception.raiseInternal "Invalid PipeExpr" []


module Expr =
  let knownType = KTCustomType(ptTyp [] "Expr" 0, [])

  let rec toDT (e : PT.Expr) : Dval =
    let (caseName, fields) =
      match e with
      | PT.EUnit id -> "EUnit", [ DInt(int64 id) ]

      // simple data
      | PT.EBool(id, b) -> "EBool", [ DInt(int64 id); DBool b ]
      | PT.EInt(id, i) -> "EInt", [ DInt(int64 id); DInt i ]
      | PT.EInt8(id, i) -> "EInt8", [ DInt(int64 id); DInt8 i ]
      | PT.EFloat(id, sign, whole, remainder) ->
        "EFloat",
        [ DInt(int64 id); Sign.toDT sign; DString whole; DString remainder ]

      | PT.EChar(id, c) -> "EChar", [ DInt(int64 id); DString c ]
      | PT.EString(id, segments) ->
        "EString",
        [ DInt(int64 id)
          DList(
            VT.known StringSegment.knownType,
            List.map (StringSegment.toDT toDT) segments
          ) ]

      // structures of data
      | PT.EList(id, items) ->
        "EList", [ DInt(int64 id); DList(VT.known knownType, List.map toDT items) ]

      | PT.EDict(id, pairs) ->
        "EDict",
        [ DInt(int64 id)
          DList(
            VT.tuple VT.string (VT.known knownType) [],
            pairs |> List.map (fun (k, v) -> DTuple(DString k, toDT v, []))
          ) ]

      | PT.ETuple(id, first, second, theRest) ->
        "ETuple",
        [ DInt(int64 id)
          toDT first
          toDT second
          DList(VT.known knownType, List.map toDT theRest) ]

      | PT.ERecord(id, typeName, fields) ->
        let fields =
          DList(
            VT.tuple VT.string (VT.known knownType) [],
            fields
            |> List.map (fun (name, expr) -> DTuple(DString name, toDT expr, []))
          )

        "ERecord",
        [ DInt(int64 id)
          NameResolution.toDT TypeName.knownType TypeName.toDT typeName
          fields ]

      | PT.EEnum(id, typeName, caseName, fields) ->
        "EEnum",
        [ DInt(int64 id)
          NameResolution.toDT TypeName.knownType TypeName.toDT typeName
          DString caseName
          DList(VT.known knownType, List.map toDT fields) ]

      // declaring and accessing variables
      | PT.ELet(id, lp, expr, body) ->
        "ELet", [ DInt(int64 id); LetPattern.toDT lp; toDT expr; toDT body ]

      | PT.EFieldAccess(id, expr, fieldName) ->
        "EFieldAccess", [ DInt(int64 id); toDT expr; DString fieldName ]

      | PT.EVariable(id, varName) -> "EVariable", [ DInt(int64 id); DString varName ]


      // control flow
      | PT.EIf(id, cond, thenExpr, elseExpr) ->
        "EIf",
        [ DInt(int64 id)
          toDT cond
          toDT thenExpr
          elseExpr |> Option.map toDT |> Dval.option knownType ]

      | PT.EMatch(id, arg, cases) ->
        let typeName = (ptTyp [] "MatchCase" 0)
        let cases =
          cases
          |> List.map (fun case ->

            let pattern = MatchPattern.toDT case.pat
            let whenCondition =
              case.whenCondition |> Option.map toDT |> Dval.option knownType
            let expr = toDT case.rhs
            DRecord(
              typeName,
              typeName,
              [],
              Map
                [ ("pat", pattern)
                  ("whenCondition", whenCondition)
                  ("rhs", expr) ]
            ))
          |> Dval.list (KTCustomType(typeName, []))

        "EMatch", [ DInt(int64 id); toDT arg; cases ]

      | PT.EPipe(id, expr, pipeExprs) ->
        "EPipe",
        [ DInt(int64 id)
          toDT expr
          DList(
            VT.known PipeExpr.knownType,
            List.map (PipeExpr.toDT knownType toDT) pipeExprs
          ) ]


      // function calls
      | PT.EInfix(id, infix, lhs, rhs) ->
        "EInfix", [ DInt(int64 id); Infix.toDT infix; toDT lhs; toDT rhs ]

      | PT.ELambda(id, pats, body) ->
        let variables =
          DList(
            VT.tuple VT.int VT.string [],
            pats |> NEList.toList |> List.map LetPattern.toDT
          )
        "ELambda", [ DInt(int64 id); variables; toDT body ]

      | PT.EConstant(id, name) ->
        "EConstant",
        [ DInt(int64 id)
          NameResolution.toDT ConstantName.knownType ConstantName.toDT name ]

      | PT.EApply(id, name, typeArgs, args) ->
        "EApply",
        [ DInt(int64 id)
          toDT name
          DList(
            VT.known TypeReference.knownType,
            List.map TypeReference.toDT typeArgs
          )
          DList(VT.known knownType, args |> NEList.toList |> List.map toDT) ]

      | PT.EFnName(id, name) ->
        "EFnName",
        [ DInt(int64 id); NameResolution.toDT FnName.knownType FnName.toDT name ]

      | PT.ERecordUpdate(id, record, updates) ->
        let updates =
          DList(
            VT.tuple VT.string (VT.known knownType) [],
            updates
            |> NEList.toList
            |> List.map (fun (name, expr) -> DTuple(DString name, toDT expr, []))
          )

        "ERecordUpdate", [ DInt(int64 id); toDT record; updates ]


    let typeName = ptTyp [] "Expr" 0
    DEnum(typeName, typeName, [], caseName, fields)


  let rec fromDT (d : Dval) : PT.Expr =
    match d with
    | DEnum(_, _, [], "EUnit", [ DInt id ]) -> PT.EUnit(uint64 id)

    // simple data
    | DEnum(_, _, [], "EBool", [ DInt id; DBool b ]) -> PT.EBool(uint64 id, b)
    | DEnum(_, _, [], "EInt", [ DInt id; DInt i ]) -> PT.EInt(uint64 id, i)
    | DEnum(_, _, [], "EInt8", [ DInt id; DInt8 i ]) -> PT.EInt8(uint64 id, i)
    | DEnum(_, _, [], "EFloat", [ DInt id; sign; DString whole; DString remainder ]) ->
      PT.EFloat(uint64 id, Sign.fromDT sign, whole, remainder)
    | DEnum(_, _, [], "EChar", [ DInt id; DString c ]) -> PT.EChar(uint64 id, c)
    | DEnum(_, _, [], "EString", [ DInt id; DList(_vtTODO, segments) ]) ->
      PT.EString(uint64 id, List.map (StringSegment.fromDT fromDT) segments)


    // structures of data
    | DEnum(_, _, [], "EList", [ DInt id; DList(_vtTODO, inner) ]) ->
      PT.EList(uint64 id, List.map fromDT inner)
    | DEnum(_, _, [], "EDict", [ DInt id; DList(_vtTODO, pairsList) ]) ->
      let pairs =
        pairsList
        |> List.collect (fun pair ->
          match pair with
          | DTuple(DString k, v, _) -> [ (k, fromDT v) ]
          | _ -> [])
      PT.EDict(uint64 id, pairs)


    | DEnum(_, _, [], "ETuple", [ DInt id; first; second; DList(_vtTODO, theRest) ]) ->
      PT.ETuple(uint64 id, fromDT first, fromDT second, List.map fromDT theRest)

    | DEnum(_, _, [], "ERecord", [ DInt id; typeName; DList(_vtTODO, fieldsList) ]) ->
      let fields =
        fieldsList
        |> List.collect (fun field ->
          match field with
          | DTuple(DString name, expr, _) -> [ (name, fromDT expr) ]
          | _ -> [])
      PT.ERecord(uint64 id, NameResolution.fromDT TypeName.fromDT typeName, fields)


    | DEnum(_,
            _,
            [],
            "EEnum",
            [ DInt id; typeName; DString caseName; DList(_vtTODO, fields) ]) ->
      PT.EEnum(
        uint64 id,
        NameResolution.fromDT TypeName.fromDT typeName,
        caseName,
        List.map fromDT fields
      )

    // declaring and accessing variables
    | DEnum(_, _, [], "ELet", [ DInt id; lp; expr; body ]) ->
      PT.ELet(uint64 id, LetPattern.fromDT lp, fromDT expr, fromDT body)

    | DEnum(_, _, [], "EFieldAccess", [ DInt id; expr; DString fieldName ]) ->
      PT.EFieldAccess(uint64 id, fromDT expr, fieldName)

    | DEnum(_, _, [], "EVariable", [ DInt id; DString varName ]) ->
      PT.EVariable(uint64 id, varName)

    // control flow
    | DEnum(_, _, [], "EIf", [ DInt id; cond; thenExpr; elseExpr ]) ->
      let elseExpr =
        match elseExpr with
        | DEnum(_, _, _typeArgsDEnumTODO, "Some", [ dv ]) -> Some(fromDT dv)
        | DEnum(_, _, _typeArgsDEnumTODO, "None", []) -> None
        | _ ->
          Exception.raiseInternal "Invalid else expression" [ "elseExpr", elseExpr ]
      PT.EIf(uint64 id, fromDT cond, fromDT thenExpr, elseExpr)

    | DEnum(_, _, [], "EMatch", [ DInt id; arg; DList(_vtTODO, cases) ]) ->
      let (cases : List<PT.MatchCase>) =
        cases
        |> List.collect (fun case ->
          match case with
          | DRecord(_, _, _, fields) ->
            let whenCondition =
              match Map.tryFind "whenCondition" fields with
              | Some(DEnum(_, _, _, "Some", [ value ])) -> Some(fromDT value)
              | Some(DEnum(_, _, _, "None", [])) -> None
              | _ -> None
            match Map.tryFind "pat" fields, Map.tryFind "rhs" fields with
            | Some pat, Some rhs ->
              [ { pat = MatchPattern.fromDT pat
                  whenCondition = whenCondition
                  rhs = fromDT rhs } ]
            | _ -> []
          | _ -> [])
      PT.EMatch(uint64 id, fromDT arg, cases)

    | DEnum(_, _, [], "EPipe", [ DInt id; expr; DList(_vtTODO, pipeExprs) ]) ->
      PT.EPipe(uint64 id, fromDT expr, List.map (PipeExpr.fromDT fromDT) pipeExprs)

    // function calls
    | DEnum(_, _, [], "EInfix", [ DInt id; infix; lhs; rhs ]) ->
      PT.EInfix(uint64 id, Infix.fromDT infix, fromDT lhs, fromDT rhs)

    | DEnum(_, _, [], "ELambda", [ DInt id; DList(_vtTODO, pats); body ]) ->
      let pats =
        pats
        |> List.map LetPattern.fromDT
        |> NEList.ofListUnsafe
          "PT2DT.Expr.fromDT expected at least one bound variable in ELambda"
          []
      PT.ELambda(uint64 id, pats, fromDT body)


    | DEnum(_,
            _,
            [],
            "EApply",
            [ DInt id; name; DList(_vtTODO1, typeArgs); DList(_vtTODO2, args) ]) ->
      PT.EApply(
        uint64 id,
        fromDT name,
        List.map TypeReference.fromDT typeArgs,
        args |> NEList.ofListUnsafe "EApply" [] |> NEList.map fromDT
      )

    | DEnum(_, _, [], "EFnName", [ DInt id; name ]) ->
      PT.EFnName(uint64 id, NameResolution.fromDT FnName.fromDT name)

    | DEnum(_,
            _,
            [],
            "ERecordUpdate",
            [ DInt id; record; DList(_vtTODO, head :: tail) ]) ->
      let updates =
        NEList.ofList head tail
        |> NEList.map (fun update ->
          match update with
          | DTuple(DString name, expr, _) -> (name, fromDT expr)
          | _ ->
            Exception.raiseInternal "Invalid record update" [ "update", update ])
      PT.ERecordUpdate(uint64 id, fromDT record, updates)

    | e -> Exception.raiseInternal "Invalid Expr" [ "e", e ]


module Const =
  let knownType = KTCustomType(ptTyp [] "Const" 0, [])

  let rec toDT (c : PT.Const) : Dval =
    let (caseName, fields) =
      match c with
      | PT.Const.CUnit -> "CUnit", []
      | PT.Const.CBool b -> "CBool", [ DBool b ]
      | PT.Const.CInt i -> "CInt", [ DInt i ]
      | PT.Const.CInt8 i -> "CInt8", [ DInt8 i ]
      | PT.Const.CFloat(sign, w, f) ->
        "CFloat", [ Sign.toDT sign; DString w; DString f ]
      | PT.Const.CChar c -> "CChar", [ DChar c ]
      | PT.Const.CString s -> "CString", [ DString s ]

      | PT.Const.CTuple(first, second, theRest) ->
        "CTuple",
        [ toDT first; toDT second; DList(VT.known knownType, List.map toDT theRest) ]

      | PT.Const.CEnum(typeName, caseName, fields) ->
        "CEnum",
        [ NameResolution.toDT TypeName.knownType TypeName.toDT typeName
          DString caseName
          Dval.list knownType (List.map toDT fields) ]

      | PT.Const.CList inner ->
        "CList", [ DList(VT.known knownType, List.map toDT inner) ]

      | PT.Const.CDict pairs ->
        "CDict",
        [ DList(
            VT.tuple VT.string VT.string [],
            pairs |> List.map (fun (k, v) -> DTuple(DString k, toDT v, []))
          ) ]


    let typeName = ptTyp [] "Const" 0
    DEnum(typeName, typeName, [], caseName, fields)


  let rec fromDT (d : Dval) : PT.Const =
    match d with
    | DEnum(_, _, [], "CInt", [ DInt i ]) -> PT.Const.CInt i
    | DEnum(_, _, [], "CInt8", [ DInt8 i ]) -> PT.Const.CInt8 i
    | DEnum(_, _, [], "CBool", [ DBool b ]) -> PT.Const.CBool b
    | DEnum(_, _, [], "CString", [ DString s ]) -> PT.Const.CString s
    | DEnum(_, _, [], "CChar", [ DChar c ]) -> PT.Const.CChar c
    | DEnum(_, _, [], "CFloat", [ sign; DString w; DString f ]) ->
      PT.Const.CFloat(Sign.fromDT sign, w, f)
    | DEnum(_, _, [], "CUnit", []) -> PT.Const.CUnit
    | DEnum(_, _, [], "CTuple", [ first; second; DList(_vtTODO, rest) ]) ->
      PT.Const.CTuple(fromDT first, fromDT second, List.map fromDT rest)
    | DEnum(_, _, [], "CEnum", [ typeName; DString caseName; DList(_vtTODO, fields) ]) ->
      PT.Const.CEnum(
        NameResolution.fromDT TypeName.fromDT typeName,
        caseName,
        List.map fromDT fields
      )
    | DEnum(_, _, [], "CList", [ DList(_vtTODO, inner) ]) ->
      PT.Const.CList(List.map fromDT inner)
    | DEnum(_, _, [], "CDict", [ DList(_vtTODO, pairs) ]) ->
      let pairs =
        pairs
        |> List.map (fun pair ->
          match pair with
          | DTuple(k, v, _) -> (fromDT k, fromDT v)
          | _ -> Exception.raiseInternal "Invalid pair" [])
      PT.Const.CDict(
        List.map
          (fun (k, v) ->
            (match k with
             | PT.Const.CString s -> s
             | _ -> Exception.raiseInternal "Invalid key" []),
            v)
          pairs
      )


    | _ -> Exception.raiseInternal "Invalid Const" []

module Deprecation =
  let typeName = ptTyp [] "Deprecation" 0
  let knownType = KTCustomType(typeName, [])

  let toDT
    (innerType : KnownType)
    (inner : 'a -> Dval)
    (d : PT.Deprecation<'a>)
    : Dval =
    let (caseName, fields) =
      match d with
      | PT.Deprecation.NotDeprecated -> "NotDeprecated", []
      | PT.Deprecation.RenamedTo replacement -> "RenamedTo", [ inner replacement ]
      | PT.Deprecation.ReplacedBy replacement -> "ReplacedBy", [ inner replacement ]
      | PT.Deprecation.DeprecatedBecause reason ->
        "DeprecatedBecause", [ DString reason ]
    DEnum(
      typeName,
      typeName,
      Dval.ignoreAndUseEmpty [ VT.known innerType ],
      caseName,
      fields
    )

  let fromDT (inner : Dval -> 'a) (d : Dval) : PT.Deprecation<'a> =
    match d with
    | DEnum(_, _, _typeArgsDEnumTODO, "NotDeprecated", []) ->
      PT.Deprecation.NotDeprecated
    | DEnum(_, _, _typeArgsDEnumTODO, "RenamedTo", [ replacement ]) ->
      PT.Deprecation.RenamedTo(inner replacement)
    | DEnum(_, _, _typeArgsDEnumTODO, "ReplacedBy", [ replacement ]) ->
      PT.Deprecation.ReplacedBy(inner replacement)
    | DEnum(_, _, _typeArgsDEnumTODO, "DeprecatedBecause", [ DString reason ]) ->
      PT.Deprecation.DeprecatedBecause(reason)
    | _ -> Exception.raiseInternal "Invalid Deprecation" []


module TypeDeclaration =
  module RecordField =
    let typeName = ptTyp [ "TypeDeclaration" ] "RecordField" 0
    let knownType = KTCustomType(typeName, [])

    let toDT (rf : PT.TypeDeclaration.RecordField) : Dval =
      let fields =
        [ "name", DString rf.name
          "typ", TypeReference.toDT rf.typ
          "description", DString rf.description ]
      DRecord(typeName, typeName, [], Map fields)

    let fromDT (d : Dval) : PT.TypeDeclaration.RecordField =
      match d with
      | DRecord(_, _, _, fields) ->
        { name = fields |> D.stringField "name"
          typ = fields |> D.field "typ" |> TypeReference.fromDT
          description = fields |> D.stringField "description" }
      | _ -> Exception.raiseInternal "Invalid RecordField" []

  module EnumField =
    let knownType = KTCustomType(ptTyp [ "TypeDeclaration" ] "EnumField" 0, [])

    let toDT (ef : PT.TypeDeclaration.EnumField) : Dval =
      let typeName = ptTyp [ "TypeDeclaration" ] "EnumField" 0
      let fields =
        [ "typ", TypeReference.toDT ef.typ
          "label", ef.label |> Option.map DString |> Dval.option KTString
          "description", DString ef.description ]
      DRecord(typeName, typeName, [], Map fields)

    let fromDT (d : Dval) : PT.TypeDeclaration.EnumField =
      match d with
      | DRecord(_, _, _, fields) ->
        { typ = fields |> D.field "typ" |> TypeReference.fromDT
          label =
            match Map.get "label" fields with
            | Some(DEnum(_, _, _typeArgsDEnumTODO, "Some", [ DString label ])) ->
              Some label
            | Some(DEnum(_, _, _typeArgsDEnumTODO, "None", [])) -> None
            | _ ->
              Exception.raiseInternal "Expected label to be an option of string" []
          description = fields |> D.stringField "description" }
      | _ -> Exception.raiseInternal "Invalid EnumField" []


  module EnumCase =
    let typeName = ptTyp [ "TypeDeclaration" ] "EnumCase" 0
    let knownType = KTCustomType(typeName, [])

    let toDT (ec : PT.TypeDeclaration.EnumCase) : Dval =
      let fields =
        [ "name", DString ec.name
          "fields",
          DList(VT.known EnumField.knownType, List.map EnumField.toDT ec.fields)
          "description", DString ec.description ]
      DRecord(typeName, typeName, [], Map fields)

    let fromDT (d : Dval) : PT.TypeDeclaration.EnumCase =
      match d with
      | DRecord(_, _, _, fields) ->
        { name = fields |> D.stringField "name"
          fields = fields |> D.listField "fields" |> List.map EnumField.fromDT
          description = fields |> D.stringField "description" }

      | _ -> Exception.raiseInternal "Invalid EnumCase" []


  module Definition =
    let typeName = ptTyp [ "TypeDeclaration" ] "Definition" 0

    let toDT (d : PT.TypeDeclaration.Definition) : Dval =
      let (caseName, fields) =
        match d with
        | PT.TypeDeclaration.Alias typeRef -> "Alias", [ TypeReference.toDT typeRef ]

        | PT.TypeDeclaration.Record fields ->
          "Record",
          [ DList(
              VT.known RecordField.knownType,
              fields |> NEList.toList |> List.map RecordField.toDT
            ) ]

        | PT.TypeDeclaration.Enum cases ->
          "Enum",
          [ DList(
              VT.known EnumCase.knownType,
              cases |> NEList.toList |> List.map EnumCase.toDT
            ) ]
      DEnum(typeName, typeName, [], caseName, fields)

    let fromDT (d : Dval) : PT.TypeDeclaration.Definition =
      match d with
      | DEnum(_, _, [], "Alias", [ typeRef ]) ->
        PT.TypeDeclaration.Alias(TypeReference.fromDT typeRef)

      | DEnum(_, _, [], "Record", [ DList(_vtTODO, firstField :: additionalFields) ]) ->
        PT.TypeDeclaration.Record(
          NEList.ofList firstField additionalFields |> NEList.map RecordField.fromDT
        )

      | DEnum(_, _, [], "Enum", [ DList(_vtTODO, firstCase :: additionalCases) ]) ->
        PT.TypeDeclaration.Enum(
          NEList.ofList firstCase additionalCases |> NEList.map EnumCase.fromDT
        )

      | _ -> Exception.raiseInternal "Invalid TypeDeclaration.Definition" []


  let toDT (td : PT.TypeDeclaration.T) : Dval =
    let typeName = ptTyp [ "TypeDeclaration" ] "TypeDeclaration" 0
    let fields =
      [ "typeParams", DList(VT.string, List.map DString td.typeParams)
        "definition", Definition.toDT td.definition ]
    DRecord(typeName, typeName, [], Map fields)



  let fromDT (d : Dval) : PT.TypeDeclaration.T =
    match d with
    | DRecord(_, _, _, fields) ->
      { typeParams = fields |> D.stringListField "typeParams"
        definition = fields |> D.field "definition" |> Definition.fromDT }

    | _ -> Exception.raiseInternal "Invalid TypeDeclaration" []


module Handler =
  module CronInterval =
    let toDT (ci : PT.Handler.CronInterval) : Dval =
      let (caseName, fields) =
        match ci with
        | PT.Handler.CronInterval.EveryMinute -> "EveryMinute", []
        | PT.Handler.CronInterval.EveryHour -> "EveryHour", []
        | PT.Handler.CronInterval.Every12Hours -> "Every12Hours", []
        | PT.Handler.CronInterval.EveryDay -> "EveryDay", []
        | PT.Handler.CronInterval.EveryWeek -> "EveryWeek", []
        | PT.Handler.CronInterval.EveryFortnight -> "EveryFortnight", []

      let typeName = ptTyp [ "Handler" ] "CronInterval" 0
      DEnum(typeName, typeName, [], caseName, fields)

    let fromDT (d : Dval) : PT.Handler.CronInterval =
      match d with
      | DEnum(_, _, [], "EveryMinute", []) -> PT.Handler.CronInterval.EveryMinute
      | DEnum(_, _, [], "EveryHour", []) -> PT.Handler.CronInterval.EveryHour
      | DEnum(_, _, [], "Every12Hours", []) -> PT.Handler.CronInterval.Every12Hours
      | DEnum(_, _, [], "EveryDay", []) -> PT.Handler.CronInterval.EveryDay
      | DEnum(_, _, [], "EveryWeek", []) -> PT.Handler.CronInterval.EveryWeek
      | DEnum(_, _, [], "EveryFortnight", []) ->
        PT.Handler.CronInterval.EveryFortnight
      | _ -> Exception.raiseInternal "Invalid CronInterval" []


  module Spec =
    let toDT (s : PT.Handler.Spec) : Dval =
      let (caseName, fields) =
        match s with
        | PT.Handler.Spec.HTTP(route, method) ->
          "HTTP", [ DString route; DString method ]
        | PT.Handler.Spec.Worker name -> "Worker", [ DString name ]
        | PT.Handler.Spec.Cron(name, interval) ->
          "Cron", [ DString name; CronInterval.toDT interval ]
        | PT.Handler.Spec.REPL name -> "REPL", [ DString name ]

      let typeName = ptTyp [ "Handler" ] "Spec" 0
      DEnum(typeName, typeName, [], caseName, fields)

    let fromDT (d : Dval) : PT.Handler.Spec =
      match d with
      | DEnum(_, _, [], "HTTP", [ DString route; DString method ]) ->
        PT.Handler.Spec.HTTP(route, method)
      | DEnum(_, _, [], "Worker", [ DString name ]) -> PT.Handler.Spec.Worker(name)
      | DEnum(_, _, [], "Cron", [ DString name; interval ]) ->
        PT.Handler.Spec.Cron(name, CronInterval.fromDT interval)
      | DEnum(_, _, [], "REPL", [ DString name ]) -> PT.Handler.Spec.REPL(name)
      | _ -> Exception.raiseInternal "Invalid Spec" []

  let toDT (h : PT.Handler.T) : Dval =
    let typeName = ptTyp [ "Handler" ] "Handler" 0
    let fields =
      [ "tlid", DInt(int64 h.tlid)
        "ast", Expr.toDT h.ast
        "spec", Spec.toDT h.spec ]
    DRecord(typeName, typeName, [], Map fields)


  let fromDT (d : Dval) : PT.Handler.T =
    match d with
    | DRecord(_, _, _, fields) ->
      { tlid = fields |> D.uint64Field "tlid"
        ast = fields |> D.field "ast" |> Expr.fromDT
        spec = fields |> D.field "spec" |> Spec.fromDT }
    | _ -> Exception.raiseInternal "Invalid Handler" []


module DB =
  let toDT (db : PT.DB.T) : Dval =
    let typeName = ptTyp [] "DB" 0
    let fields =
      [ "tlid", DInt(int64 db.tlid)
        "name", DString db.name
        "version", DInt db.version
        "typ", TypeReference.toDT db.typ ]
    DRecord(typeName, typeName, [], Map fields)

  let fromDT (d : Dval) : PT.DB.T =
    match d with
    | DRecord(_, _, _, fields) ->
      { tlid = fields |> D.uint64Field "tlid"
        name = fields |> D.stringField "name"
        version = fields |> D.intField "version"
        typ = fields |> D.field "typ" |> TypeReference.fromDT }
    | _ -> Exception.raiseInternal "Invalid DB" []


module UserType =
  let typeName = ptTyp [] "UserType" 0

  let toDT (ut : PT.UserType.T) : Dval =
    let fields =
      [ "tlid", DInt(int64 ut.tlid)
        "name", ut.name |> TypeName.UserProgram.toDT
        "description", ut.description |> DString
        "declaration", ut.declaration |> TypeDeclaration.toDT
        "deprecated",
        ut.deprecated |> Deprecation.toDT Deprecation.knownType TypeName.toDT ]
    DRecord(typeName, typeName, [], Map fields)

  let fromDT (d : Dval) : PT.UserType.T =
    match d with
    | DRecord(_, _, _, fields) ->
      { tlid = fields |> D.uint64Field "tlid"
        name = fields |> D.field "name" |> TypeName.UserProgram.fromDT
        declaration = fields |> D.field "declaration" |> TypeDeclaration.fromDT
        description = fields |> D.stringField "description"
        deprecated =
          fields |> D.field "deprecated" |> Deprecation.fromDT TypeName.fromDT }
    | _ -> Exception.raiseInternal "Invalid UserType" []


module UserFunction =
  module Parameter =
    let knownType = KTCustomType(ptTyp [ "UserFunction" ] "Parameter" 0, [])

    let toDT (p : PT.UserFunction.Parameter) : Dval =
      let typeName = ptTyp [ "UserFunction" ] "Parameter" 0
      let fields =
        [ "name", DString p.name
          "typ", TypeReference.toDT p.typ
          "description", DString p.description ]
      DRecord(typeName, typeName, [], Map fields)

    let fromDT (d : Dval) : PT.UserFunction.Parameter =
      match d with
      | DRecord(_, _, _, fields) ->
        { name = fields |> D.stringField "name"
          typ = fields |> D.field "typ" |> TypeReference.fromDT
          description = fields |> D.stringField "description" }
      | _ -> Exception.raiseInternal "Invalid UserFunction.Parameter" []


  let typeName = ptTyp [ "UserFunction" ] "UserFunction" 0

  let toDT (userFn : PT.UserFunction.T) : Dval =
    let fields =
      [ ("tlid", DInt(int64 userFn.tlid))
        ("name", FnName.UserProgram.toDT userFn.name)
        ("typeParams", DList(VT.string, List.map DString userFn.typeParams))
        ("parameters",
         DList(
           VT.known Parameter.knownType,
           userFn.parameters |> NEList.toList |> List.map Parameter.toDT
         ))
        ("returnType", TypeReference.toDT userFn.returnType)
        ("body", Expr.toDT userFn.body)
        ("description", DString userFn.description)
        ("deprecated",
         Deprecation.toDT FnName.knownType FnName.toDT userFn.deprecated) ]
    DRecord(typeName, typeName, [], Map fields)

  let fromDT (d : Dval) : PT.UserFunction.T =
    match d with
    | DRecord(_, _, _, fields) ->
      { tlid = fields |> D.uint64Field "tlid"
        name = fields |> D.field "name" |> FnName.UserProgram.fromDT
        typeParams = fields |> D.stringListField "typeParams"
        parameters =
          fields
          |> D.listField "parameters"
          |> List.map Parameter.fromDT
          |> NEList.ofListUnsafe "userFunction needs more than one parameter" []
        returnType = fields |> D.field "returnType" |> TypeReference.fromDT
        body = fields |> D.field "body" |> Expr.fromDT
        description = fields |> D.stringField "description"
        deprecated =
          fields |> D.field "deprecated" |> Deprecation.fromDT FnName.fromDT }
    | _ -> Exception.raiseInternal "Invalid UserFunction" []

module UserConstant =
  let toDT (userConstant : PT.UserConstant.T) : Dval =
    let typeName = ptTyp [] "UserConstant" 0
    let fields =
      [ "tlid", DInt(int64 userConstant.tlid)
        "name", ConstantName.UserProgram.toDT userConstant.name
        "body", Const.toDT userConstant.body
        "description", DString userConstant.description
        "deprecated",
        Deprecation.toDT
          ConstantName.knownType
          ConstantName.toDT
          userConstant.deprecated ]
    DRecord(typeName, typeName, [], Map fields)


  let fromDT (d : Dval) : PT.UserConstant.T =
    match d with
    | DRecord(_, _, _, fields) ->
      { tlid = fields |> D.uint64Field "tlid"
        name = fields |> D.field "name" |> ConstantName.UserProgram.fromDT
        body = fields |> D.field "body" |> Const.fromDT
        description = fields |> D.stringField "description"
        deprecated =
          fields |> D.field "deprecated" |> Deprecation.fromDT ConstantName.fromDT }
    | _ -> Exception.raiseInternal "Invalid UserConstant" []

module Secret =
  let toDT (s : PT.Secret.T) : Dval =
    let typeName = ptTyp [] "Secret" 0
    let fields =
      [ "name", DString s.name; "value", DString s.value; "version", DInt s.version ]
    DRecord(typeName, typeName, [], Map fields)

  let fromDT (d : Dval) : PT.Secret.T =
    match d with
    | DRecord(_, _, _, fields) ->
      { name = fields |> D.stringField "name"
        value = fields |> D.stringField "value"
        version = fields |> D.intField "version" }
    | _ -> Exception.raiseInternal "Invalid Secret" []


module PackageType =
  let typeName = ptTyp [] "PackageType" 0

  let toDT (p : PT.PackageType.T) : Dval =
    let fields =
      [ "tlid", DInt(int64 p.tlid)
        "id", DUuid p.id
        "name", TypeName.Package.toDT p.name
        "declaration", TypeDeclaration.toDT p.declaration
        "description", DString p.description
        "deprecated", Deprecation.toDT TypeName.knownType TypeName.toDT p.deprecated ]
    DRecord(typeName, typeName, [], Map fields)


  let fromDT (d : Dval) : PT.PackageType.T =
    match d with
    | DRecord(_, _, _, fields) ->
      { tlid = fields |> D.uint64Field "tlid"
        id = fields |> D.uuidField "id"
        name = fields |> D.field "name" |> TypeName.Package.fromDT
        declaration = fields |> D.field "declaration" |> TypeDeclaration.fromDT
        description = fields |> D.stringField "description"
        deprecated =
          fields |> D.field "deprecated" |> Deprecation.fromDT TypeName.fromDT }
    | _ -> Exception.raiseInternal "Invalid PackageType" []


module PackageFn =
  module Parameter =
    let knownType = KTCustomType(ptTyp [ "PackageFn" ] "Parameter" 0, [])

    let toDT (p : PT.PackageFn.Parameter) : Dval =
      let typeName = ptTyp [ "PackageFn" ] "Parameter" 0
      let fields =
        [ "name", DString p.name
          "typ", TypeReference.toDT p.typ
          "description", DString p.description ]
      DRecord(typeName, typeName, [], Map fields)


    let fromDT (d : Dval) : PT.PackageFn.Parameter =
      match d with
      | DRecord(_, _, _, fields) ->
        { name = fields |> D.stringField "name"
          typ = fields |> D.field "typ" |> TypeReference.fromDT
          description = fields |> D.stringField "description" }
      | _ -> Exception.raiseInternal "Invalid PackageFn.Parameter" []

  let typeName = ptTyp [ "PackageFn" ] "PackageFn" 0

  let toDT (p : PT.PackageFn.T) : Dval =
    let fields =
      [ ("tlid", DInt(int64 p.tlid))
        ("id", DUuid p.id)
        ("name", FnName.Package.toDT p.name)
        ("body", Expr.toDT p.body)
        ("typeParams", DList(VT.string, List.map DString p.typeParams))
        ("parameters",
         DList(
           VT.known Parameter.knownType,
           p.parameters |> NEList.toList |> List.map Parameter.toDT
         ))
        ("returnType", TypeReference.toDT p.returnType)
        ("description", DString p.description)
        ("deprecated", Deprecation.toDT FnName.knownType FnName.toDT p.deprecated) ]

    DRecord(typeName, typeName, [], Map fields)


  let fromDT (d : Dval) : PT.PackageFn.T =
    match d with
    | DRecord(_, _, _, fields) ->
      { tlid = fields |> D.uint64Field "tlid"
        id = fields |> D.uuidField "id"
        name = fields |> D.field "name" |> FnName.Package.fromDT
        body = fields |> D.field "body" |> Expr.fromDT
        typeParams = fields |> D.stringListField "typeParams"
        parameters =
          fields
          |> D.listField "parameters"
          |> List.map Parameter.fromDT
          |> NEList.ofListUnsafe "PackageFn.fromDT" []
        returnType = fields |> D.field "returnType" |> TypeReference.fromDT
        description = fields |> D.stringField "description"
        deprecated =
          fields |> D.field "deprecated" |> Deprecation.fromDT FnName.fromDT }
    | _ -> Exception.raiseInternal "Invalid PackageFn" []

module PackageConstant =
  let typeName = ptTyp [] "PackageConstant" 0

  let toDT (p : PT.PackageConstant.T) : Dval =
    let fields =
      [ "tlid", DInt(int64 p.tlid)
        "id", DUuid p.id
        "name", ConstantName.Package.toDT p.name
        "body", Const.toDT p.body
        "description", DString p.description
        "deprecated",
        Deprecation.toDT ConstantName.knownType ConstantName.toDT p.deprecated ]
    DRecord(typeName, typeName, [], Map fields)


  let fromDT (d : Dval) : PT.PackageConstant.T =
    match d with
    | DRecord(_, _, _, fields) ->
      { tlid = fields |> D.uint64Field "tlid"
        id = fields |> D.uuidField "id"
        name = fields |> D.field "name" |> ConstantName.Package.fromDT
        body = fields |> D.field "body" |> Const.fromDT
        description = fields |> D.stringField "description"
        deprecated =
          fields |> D.field "deprecated" |> Deprecation.fromDT ConstantName.fromDT }
    | _ -> Exception.raiseInternal "Invalid PackageConstant" []
