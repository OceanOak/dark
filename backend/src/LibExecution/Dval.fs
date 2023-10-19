/// Simple pass-through functions for creating Dvals
module LibExecution.Dval

open Prelude

open LibExecution.RuntimeTypes
module VT = ValueType


let int (i : int) = DInt(int64 i)

let int8 (i : int) = DInt8(int8 i)

let list (typ : KnownType) (list : List<Dval>) : Dval = DList(VT.known typ, list)

let dict (typ : KnownType) (entries : List<string * Dval>) : Dval =
  DDict(VT.known typ, Map entries)

let dictFromMap (typ : KnownType) (entries : Map<string, Dval>) : Dval =
  DDict(VT.known typ, entries)


/// VTTODO
/// the interpreter "throws away" any valueTypes currently,
/// so while these .option and .result functions are great in that they
/// return the correct typeArgs, they conflict with what the interpreter will do
///
/// So, to make some tests happy, let's ignore these for now.
///
/// (might need better explanation^)
let ignoreAndUseEmpty (_ignoredForNow : List<ValueType>) = []



let optionType = TypeName.fqPackage "Darklang" [ "Stdlib"; "Option" ] "Option" 0


let optionSome (innerType : KnownType) (dv : Dval) : Dval =
  DEnum(
    optionType,
    optionType,
    ignoreAndUseEmpty [ VT.known innerType ],
    "Some",
    [ dv ]
  )

let optionNone (innerType : KnownType) : Dval =
  DEnum(optionType, optionType, ignoreAndUseEmpty [ VT.known innerType ], "None", [])

let option (innerType : KnownType) (dv : Option<Dval>) : Dval =
  match dv with
  | Some dv -> optionSome innerType dv
  | None -> optionNone innerType



let resultType = TypeName.fqPackage "Darklang" [ "Stdlib"; "Result" ] "Result" 0


let resultOk (okType : KnownType) (errorType : KnownType) (dvOk : Dval) : Dval =
  DEnum(
    resultType,
    resultType,
    ignoreAndUseEmpty [ ValueType.Known okType; ValueType.Known errorType ],
    "Ok",
    [ dvOk ]
  )

let resultError
  (okType : KnownType)
  (errorType : KnownType)
  (dvError : Dval)
  : Dval =

  DEnum(
    resultType,
    resultType,
    ignoreAndUseEmpty [ ValueType.known okType; ValueType.known errorType ],
    "Error",
    [ dvError ]
  )

let result
  (okType : KnownType)
  (errorType : KnownType)
  (dv : Result<Dval, Dval>)
  : Dval =
  match dv with
  | Ok dv -> resultOk okType errorType dv
  | Error dv -> resultError okType errorType dv
