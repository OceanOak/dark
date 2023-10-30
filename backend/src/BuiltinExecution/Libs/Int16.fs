module BuiltinExecution.Libs.Int16

open FSharp.Control.Tasks
open System.Threading.Tasks

open System.Numerics

open Prelude
open LibExecution.RuntimeTypes
open LibExecution.Builtin.Shortcuts

module VT = ValueType
module Dval = LibExecution.Dval

let types : List<BuiltInType> = []
let constants : List<BuiltInConstant> = []

module ParseError =
  type ParseError =
    | BadFormat
    | OutOfRange

  let toDT (e : ParseError) : Dval =
    let (caseName, fields) =
      match e with
      | BadFormat -> "BadFormat", []
      | OutOfRange -> "OutOfRange", []

    let typeName = TypeName.fqPackage "Darklang" [ "Stdlib"; "Int16" ] "ParseError" 0
    DEnum(typeName, typeName, [], caseName, fields)


let fn = fn [ "Int16" ]

let fns : List<BuiltInFn> =
  [ { name = fn "mod" 0
      typeParams = []
      parameters = [ Param.make "a" TInt16 ""; Param.make "b" TInt16 "" ]
      returnType = TInt16
      description =
        "Returns the result of wrapping <param a> around so that {{0 <= res < b}}.

        The modulus <param b> must be greater than 0.

        Use <fn Int16.remainder> if you want the remainder after division, which has
        a different behavior for negative numbers."
      fn =
        (function
        | state, _, [ DInt16 v; DInt16 m ] ->
          if m = 0s then
            Int.IntRuntimeError.Error.ZeroModulus
            |> Int.IntRuntimeError.RTE.toRuntimeError
            |> raiseRTE state.caller
            |> Ply
          else if m < 0s then
            Int.IntRuntimeError.Error.NegativeModulus
            |> Int.IntRuntimeError.RTE.toRuntimeError
            |> raiseRTE state.caller
            |> Ply
          else
            let result = v % m
            let result = if result < 0s then m + result else result
            Ply(DInt16(result))
        | _ -> incorrectArgs ())
      sqlSpec = NotYetImplemented
      previewable = Pure
      deprecated = NotDeprecated }


    { name = fn "remainder" 0
      typeParams = []
      parameters = [ Param.make "value" TInt16 ""; Param.make "divisor" TInt16 "" ]
      returnType = TypeReference.result TInt16 TString
      description =
        "Returns the integer remainder left over after dividing <param value> by
        <param divisor>, as a <type Result>.

        For example, {{Int16.remainder 15 6 == Ok 3}}. The remainder will be
        negative only if {{<var value> < 0}}.

        The sign of <param divisor> doesn't influence the outcome.

        Returns an {{Error}} if <param divisor> is {{0}}."
      fn =
        let resultOk r = Dval.resultOk KTInt16 KTString r |> Ply
        (function
        | state, _, [ DInt16 v; DInt16 d ] ->
          (try
            v % d |> DInt16 |> resultOk
           with e ->
             if d = 0s then
               Int.IntRuntimeError.Error.DivideByZeroError
               |> Int.IntRuntimeError.RTE.toRuntimeError
               |> raiseRTE state.caller
               |> Ply
             else
               Exception.raiseInternal
                 "unexpected failure case in Int16.remainder"
                 [ "v", v; "d", d ]
                 e)
        | _ -> incorrectArgs ())
      sqlSpec = NotYetImplemented
      previewable = Pure
      deprecated = NotDeprecated }


    { name = fn "add" 0
      typeParams = []
      parameters = [ Param.make "a" TInt16 ""; Param.make "b" TInt16 "" ]
      returnType = TInt16
      description = "Adds two 16 bits signed integers together"
      fn =
        (function
        | state, _, [ DInt16 a; DInt16 b ] ->
          try
            let result = Checked.(+) a b
            Ply(DInt16(result))
          with :? System.OverflowException ->
            Int.IntRuntimeError.Error.OutOfRange
            |> Int.IntRuntimeError.RTE.toRuntimeError
            |> raiseRTE state.caller
            |> Ply

        | _ -> incorrectArgs ())
      sqlSpec = NotYetImplemented
      previewable = Pure
      deprecated = NotDeprecated }


    { name = fn "subtract" 0
      typeParams = []
      parameters = [ Param.make "a" TInt16 ""; Param.make "b" TInt16 "" ]
      returnType = TInt16
      description = "Subtracts two 16 bits signed integers"
      fn =
        (function
        | state, _, [ DInt16 a; DInt16 b ] ->
          try
            let result = Checked.(-) a b
            Ply(DInt16(result))
          with :? System.OverflowException ->
            Int.IntRuntimeError.Error.OutOfRange
            |> Int.IntRuntimeError.RTE.toRuntimeError
            |> raiseRTE state.caller
            |> Ply

        | _ -> incorrectArgs ())
      sqlSpec = NotYetImplemented
      previewable = Pure
      deprecated = NotDeprecated }


    { name = fn "multiply" 0
      typeParams = []
      parameters = [ Param.make "a" TInt16 ""; Param.make "b" TInt16 "" ]
      returnType = TInt16
      description = "two 16 bits signed integers"
      fn =
        (function
        | state, _, [ DInt16 a; DInt16 b ] ->
          try
            let result = Checked.(*) a b
            Ply(DInt16(result))
          with :? System.OverflowException ->
            Int.IntRuntimeError.Error.OutOfRange
            |> Int.IntRuntimeError.RTE.toRuntimeError
            |> raiseRTE state.caller
            |> Ply

        | _ -> incorrectArgs ())
      sqlSpec = NotYetImplemented
      previewable = Pure
      deprecated = NotDeprecated }


    { name = fn "divide" 0
      typeParams = []
      parameters = [ Param.make "a" TInt16 ""; Param.make "b" TInt16 "" ]
      returnType = TInt16
      description = "Divides two 16 bits signed integers"
      fn =
        (function
        | state, _, [ DInt16 a; DInt16 b ] ->
          if b = 0s then
            Int.IntRuntimeError.Error.DivideByZeroError
            |> Int.IntRuntimeError.RTE.toRuntimeError
            |> raiseRTE state.caller
            |> Ply
          else if a = int16 System.Int16.MinValue && b = -1s then
            Int.IntRuntimeError.Error.OutOfRange
            |> Int.IntRuntimeError.RTE.toRuntimeError
            |> raiseRTE state.caller
            |> Ply
          else
            let result = a / b
            if result < System.Int16.MinValue || result > System.Int16.MaxValue then
              Int.IntRuntimeError.Error.OutOfRange
              |> Int.IntRuntimeError.RTE.toRuntimeError
              |> raiseRTE state.caller
              |> Ply
            else
              Ply(DInt16(int16 result))

        | _ -> incorrectArgs ())
      sqlSpec = NotYetImplemented
      previewable = Pure
      deprecated = NotDeprecated }


    { name = fn "negate" 0
      typeParams = []
      parameters = [ Param.make "a" TInt16 "" ]
      returnType = TInt16
      description = "Returns the negation of <param a>, {{-a}}"
      fn =
        (function
        | state, _, [ DInt16 a ] ->
          if a = System.Int16.MinValue then
            Int.IntRuntimeError.Error.OutOfRange
            |> Int.IntRuntimeError.RTE.toRuntimeError
            |> raiseRTE state.caller
            |> Ply
          else
            let result = -a
            Ply(DInt16(result))

        | _ -> incorrectArgs ())
      sqlSpec = NotYetImplemented
      previewable = Pure
      deprecated = NotDeprecated }


    { name = fn "greaterThan" 0
      typeParams = []
      parameters = [ Param.make "a" TInt16 ""; Param.make "b" TInt16 "" ]
      returnType = TBool
      description = "Returns {{true}} if <param a> is greater than <param b>"
      fn =
        (function
        | _, _, [ DInt16 a; DInt16 b ] -> Ply(DBool(a > b))
        | _ -> incorrectArgs ())
      sqlSpec = NotYetImplemented
      previewable = Pure
      deprecated = NotDeprecated }


    { name = fn "greaterThanOrEqualTo" 0
      typeParams = []
      parameters = [ Param.make "a" TInt16 ""; Param.make "b" TInt16 "" ]
      returnType = TBool
      description =
        "Returns {{true}} if <param a> is greater than or equal to <param b>"
      fn =
        (function
        | _, _, [ DInt16 a; DInt16 b ] -> Ply(DBool(a >= b))
        | _ -> incorrectArgs ())
      sqlSpec = NotYetImplemented
      previewable = Pure
      deprecated = NotDeprecated }


    { name = fn "lessThan" 0
      typeParams = []
      parameters = [ Param.make "a" TInt16 ""; Param.make "b" TInt16 "" ]
      returnType = TBool
      description = "Returns {{true}} if <param a> is less than <param b>"
      fn =
        (function
        | _, _, [ DInt16 a; DInt16 b ] -> Ply(DBool(a < b))
        | _ -> incorrectArgs ())
      sqlSpec = NotYetImplemented
      previewable = Pure
      deprecated = NotDeprecated }


    { name = fn "lessThanOrEqualTo" 0
      typeParams = []
      parameters = [ Param.make "a" TInt16 ""; Param.make "b" TInt16 "" ]
      returnType = TBool
      description =
        "Returns {{true}} if <param a> is less than or equal to <param b>"
      fn =
        (function
        | _, _, [ DInt16 a; DInt16 b ] -> Ply(DBool(a <= b))
        | _ -> incorrectArgs ())
      sqlSpec = NotYetImplemented
      previewable = Pure
      deprecated = NotDeprecated }


    { name = fn "toString" 0
      typeParams = []
      parameters = [ Param.make "a" TInt16 "" ]
      returnType = TString
      description = "Stringify <param int16>"
      fn =
        (function
        | _, _, [ DInt16 a ] -> Ply(DString(string a))
        | _ -> incorrectArgs ())
      sqlSpec = NotYetImplemented
      previewable = Pure
      deprecated = NotDeprecated }


    { name = fn "toFloat" 0
      typeParams = []
      parameters = [ Param.make "a" TInt16 "" ]
      returnType = TFloat
      description = "Converts an <type Int16> to a <type Float>"
      fn =
        (function
        | _, _, [ DInt16 a ] -> Ply(DFloat(float a))
        | _ -> incorrectArgs ())
      sqlSpec = NotYetImplemented
      previewable = Pure
      deprecated = NotDeprecated }


    { name = fn "random" 0
      typeParams = []
      parameters = [ Param.make "start" TInt16 ""; Param.make "end" TInt16 "" ]
      returnType = TInt16
      description =
        "Returns a random integer16 between <param start> and <param end> (inclusive)"
      fn =
        (function
        | _, _, [ DInt16 a; DInt16 b ] ->
          let lower, upper = if a > b then (b, a) else (a, b)

          let correctRange = 1

          int lower + randomSeeded().Next(int upper - int lower + correctRange)
          |> int16
          |> DInt16
          |> Ply

        | _ -> incorrectArgs ())
      sqlSpec = NotYetImplemented
      previewable = Pure
      deprecated = NotDeprecated }


    { name = fn "parse" 0
      typeParams = []
      parameters = [ Param.make "s" TString "" ]
      returnType =
        TypeReference.result
          TInt16
          (TCustomType(
            Ok(
              FQName.Package
                { owner = "Darklang"
                  modules = [ "Stdlib"; "Int16" ]
                  name = TypeName.TypeName "ParseError"
                  version = 0 }
            ),
            []
          ))
      description = "Returns the <type Int16> value of a <type String>"
      fn =
        let resultOk = Dval.resultOk KTInt16 KTString
        let typeName = RuntimeError.name [ "Int16" ] "ParseError" 0
        let resultError = Dval.resultError KTInt16 (KTCustomType(typeName, []))
        (function
        | _, _, [ DString s ] ->
          try
            s |> System.Convert.ToInt16 |> DInt16 |> resultOk |> Ply
          with
          | :? System.OverflowException ->
            ParseError.OutOfRange |> ParseError.toDT |> resultError |> Ply

          | :? System.FormatException ->
            ParseError.BadFormat |> ParseError.toDT |> resultError |> Ply

        | _ -> incorrectArgs ())
      sqlSpec = NotYetImplemented
      previewable = Pure
      deprecated = NotDeprecated }


    { name = fn "fromInt64" 0
      typeParams = []
      parameters = [ Param.make "a" TInt "" ]
      returnType = TypeReference.option TInt16
      description =
        "Converts an int64 to a 16 bits signed integer. Returns {{None}} if the value is less than -32768 or greater than 32767"
      fn =
        (function
        | _, _, [ DInt a ] ->
          if a < int64 System.Int16.MinValue || a > int64 System.Int16.MaxValue then
            Dval.optionNone KTInt16 |> Ply
          else
            Dval.optionSome KTInt16 (DInt16(int16 a)) |> Ply
        | _ -> incorrectArgs ())
      sqlSpec = NotYetImplemented
      previewable = Pure
      deprecated = NotDeprecated }

    ]

let contents = (fns, types, constants)