// See README.md
module LibPackageManager.ExternalTypesToProgramTypes

open System.Threading.Tasks
open FSharp.Control.Tasks

open Prelude

module RT = LibExecution.RuntimeTypes
module PT = LibExecution.ProgramTypes
module PT2RT = LibExecution.ProgramTypesToRuntimeTypes
module PT2DT = LibExecution.ProgramTypesToDarkTypes

open Types

module EPT = ProgramTypes

module NameResolutionError =
  let toPT (err : NameResolutionError) : PT.NameResolutionError =
    match err with
    | NotFound names -> PT.NameResolutionError.NotFound names
    | InvalidName names -> PT.NameResolutionError.InvalidName names

  let fromPT (err : PT.NameResolutionError) : NameResolutionError =
    match err with
    | PT.NameResolutionError.NotFound names -> NotFound names
    | PT.NameResolutionError.InvalidName names -> InvalidName names


module NameResolution =
  let toPT (f : 's -> 'p) (result : EPT.NameResolution<'s>) : PT.NameResolution<'p> =
    match result with
    | Ok name -> Ok(f name)
    | Error err -> Error(NameResolutionError.toPT err)

  let fromPT (f : 'p -> 's) (result : PT.NameResolution<'p>) : EPT.NameResolution<'s> =
    match result with
    | Ok name -> Ok(f name)
    | Error err -> Error(NameResolutionError.fromPT err)


module Sign =
  let toPT (s : Sign) : Prelude.Sign =
    match s with
    | Positive -> Prelude.Positive
    | Negative -> Prelude.Negative

  let fromPT (s : Prelude.Sign) : Sign =
    match s with
    | Prelude.Positive -> Positive
    | Prelude.Negative -> Negative

module TypeName =
  module Package =
    let toPT (p : EPT.FQTypeName.Package) : PT.FQTypeName.Package = p

    let fromPT (p : PT.FQTypeName.Package) : EPT.FQTypeName.Package = p

  let toPT (fqfn : EPT.FQTypeName.FQTypeName) : PT.FQTypeName.FQTypeName =
    match fqfn with
    | EPT.FQTypeName.Package p -> PT.FQTypeName.Package(Package.toPT p)

  let fromPT (fqfn : PT.FQTypeName.FQTypeName) : EPT.FQTypeName.FQTypeName =
    match fqfn with
    | PT.FQTypeName.Package p -> EPT.FQTypeName.Package(Package.fromPT p)


module FnName =
  module Builtin =
    let toPT (b : EPT.FQFnName.Builtin) : PT.FQFnName.Builtin =
      { name = b.name; version = b.version }

    let fromPT (b : PT.FQFnName.Builtin) : EPT.FQFnName.Builtin =
      { name = b.name; version = b.version }

  module Package =
    let toPT (p : EPT.FQFnName.Package) : PT.FQFnName.Package = p

    let fromPT (p : PT.FQFnName.Package) : EPT.FQFnName.Package = p

  let toPT (fqfn : EPT.FQFnName.FQFnName) : PT.FQFnName.FQFnName =
    match fqfn with
    | EPT.FQFnName.Builtin s -> PT.FQFnName.Builtin(Builtin.toPT s)
    | EPT.FQFnName.Package p -> PT.FQFnName.Package(Package.toPT p)

  let fromPT (fqfn : PT.FQFnName.FQFnName) : EPT.FQFnName.FQFnName =
    match fqfn with
    | PT.FQFnName.Builtin s -> EPT.FQFnName.Builtin(Builtin.fromPT s)
    | PT.FQFnName.Package p -> EPT.FQFnName.Package(Package.fromPT p)

module ConstantName =
  module Builtin =
    let toPT (b : EPT.FQConstantName.Builtin) : PT.FQConstantName.Builtin =
      { name = b.name; version = b.version }

    let fromPT (b : PT.FQConstantName.Builtin) : EPT.FQConstantName.Builtin =
      { name = b.name; version = b.version }

  module Package =
    let toPT (p : EPT.FQConstantName.Package) : PT.FQConstantName.Package = p

    let fromPT (p : PT.FQConstantName.Package) : EPT.FQConstantName.Package = p

  let toPT
    (fqfn : EPT.FQConstantName.FQConstantName)
    : PT.FQConstantName.FQConstantName =
    match fqfn with
    | EPT.FQConstantName.Builtin s -> PT.FQConstantName.Builtin(Builtin.toPT s)
    | EPT.FQConstantName.Package p -> PT.FQConstantName.Package(Package.toPT p)

  let fromPT
    (fqfn : PT.FQConstantName.FQConstantName)
    : EPT.FQConstantName.FQConstantName =
    match fqfn with
    | PT.FQConstantName.Builtin s -> EPT.FQConstantName.Builtin(Builtin.fromPT s)
    | PT.FQConstantName.Package p -> EPT.FQConstantName.Package(Package.fromPT p)


module InfixFnName =
  let toPT (name : ProgramTypes.InfixFnName) : PT.InfixFnName =
    match name with
    | EPT.ArithmeticPlus -> PT.ArithmeticPlus
    | EPT.ArithmeticMinus -> PT.ArithmeticMinus
    | EPT.ArithmeticMultiply -> PT.ArithmeticMultiply
    | EPT.ArithmeticDivide -> PT.ArithmeticDivide
    | EPT.ArithmeticModulo -> PT.ArithmeticModulo
    | EPT.ArithmeticPower -> PT.ArithmeticPower
    | EPT.ComparisonGreaterThan -> PT.ComparisonGreaterThan
    | EPT.ComparisonGreaterThanOrEqual -> PT.ComparisonGreaterThanOrEqual
    | EPT.ComparisonLessThan -> PT.ComparisonLessThan
    | EPT.ComparisonLessThanOrEqual -> PT.ComparisonLessThanOrEqual
    | EPT.ComparisonEquals -> PT.ComparisonEquals
    | EPT.ComparisonNotEquals -> PT.ComparisonNotEquals
    | EPT.StringConcat -> PT.StringConcat

  let fromPT (name : PT.InfixFnName) : ProgramTypes.InfixFnName =
    match name with
    | PT.ArithmeticPlus -> EPT.ArithmeticPlus
    | PT.ArithmeticMinus -> EPT.ArithmeticMinus
    | PT.ArithmeticMultiply -> EPT.ArithmeticMultiply
    | PT.ArithmeticDivide -> EPT.ArithmeticDivide
    | PT.ArithmeticModulo -> EPT.ArithmeticModulo
    | PT.ArithmeticPower -> EPT.ArithmeticPower
    | PT.ComparisonGreaterThan -> EPT.ComparisonGreaterThan
    | PT.ComparisonGreaterThanOrEqual -> EPT.ComparisonGreaterThanOrEqual
    | PT.ComparisonLessThan -> EPT.ComparisonLessThan
    | PT.ComparisonLessThanOrEqual -> EPT.ComparisonLessThanOrEqual
    | PT.ComparisonEquals -> EPT.ComparisonEquals
    | PT.ComparisonNotEquals -> EPT.ComparisonNotEquals
    | PT.StringConcat -> EPT.StringConcat

module TypeReference =
  let rec toPT (t : EPT.TypeReference) : PT.TypeReference =
    match t with
    | EPT.TUnit -> PT.TUnit
    | EPT.TBool -> PT.TBool
    | EPT.TInt8 -> PT.TInt8
    | EPT.TUInt8 -> PT.TUInt8
    | EPT.TInt16 -> PT.TInt16
    | EPT.TUInt16 -> PT.TUInt16
    | EPT.TInt32 -> PT.TInt32
    | EPT.TUInt32 -> PT.TUInt32
    | EPT.TInt64 -> PT.TInt64
    | EPT.TUInt64 -> PT.TUInt64
    | EPT.TInt128 -> PT.TInt128
    | EPT.TUInt128 -> PT.TUInt128
    | EPT.TFloat -> PT.TFloat
    | EPT.TChar -> PT.TChar
    | EPT.TString -> PT.TString
    | EPT.TDateTime -> PT.TDateTime
    | EPT.TUuid -> PT.TUuid
    | EPT.TTuple(first, second, theRest) ->
      PT.TTuple(toPT first, toPT second, List.map toPT theRest)
    | EPT.TList typ -> PT.TList(toPT typ)
    | EPT.TDict typ -> PT.TDict(toPT typ)
    | EPT.TCustomType(t, typeArgs) ->
      PT.TCustomType(NameResolution.toPT TypeName.toPT t, List.map toPT typeArgs)
    | EPT.TVariable(name) -> PT.TVariable(name)
    | EPT.TFn(paramTypes, returnType) ->
      PT.TFn(NEList.map toPT paramTypes, toPT returnType)
    | EPT.TDB typ -> PT.TDB(toPT typ)

  let rec fromPT (t : PT.TypeReference) : EPT.TypeReference =
    match t with
    | PT.TUnit -> EPT.TUnit
    | PT.TBool -> EPT.TBool
    | PT.TInt8 -> EPT.TInt8
    | PT.TUInt8 -> EPT.TUInt8
    | PT.TInt16 -> EPT.TInt16
    | PT.TUInt16 -> EPT.TUInt16
    | PT.TInt32 -> EPT.TInt32
    | PT.TUInt32 -> EPT.TUInt32
    | PT.TInt64 -> EPT.TInt64
    | PT.TUInt64 -> EPT.TUInt64
    | PT.TInt128 -> EPT.TInt128
    | PT.TUInt128 -> EPT.TUInt128
    | PT.TFloat -> EPT.TFloat
    | PT.TChar -> EPT.TChar
    | PT.TString -> EPT.TString
    | PT.TDateTime -> EPT.TDateTime
    | PT.TUuid -> EPT.TUuid
    | PT.TTuple(first, second, theRest) ->
      EPT.TTuple(fromPT first, fromPT second, List.map fromPT theRest)
    | PT.TList typ -> EPT.TList(fromPT typ)
    | PT.TDict typ -> EPT.TDict(fromPT typ)
    | PT.TCustomType(t, typeArgs) ->
      EPT.TCustomType(NameResolution.fromPT TypeName.fromPT t, List.map fromPT typeArgs)
    | PT.TVariable(name) -> EPT.TVariable(name)
    | PT.TFn(paramTypes, returnType) ->
      EPT.TFn(NEList.map fromPT paramTypes, fromPT returnType)
    | PT.TDB typ -> EPT.TDB(fromPT typ)

module BinaryOperation =
  let toPT (binop : EPT.BinaryOperation) : PT.BinaryOperation =
    match binop with
    | EPT.BinOpAnd -> PT.BinOpAnd
    | EPT.BinOpOr -> PT.BinOpOr

  let fromPT (binop : PT.BinaryOperation) : EPT.BinaryOperation =
    match binop with
    | PT.BinOpAnd -> EPT.BinOpAnd
    | PT.BinOpOr -> EPT.BinOpOr

module Infix =
  let toPT (infix : EPT.Infix) : PT.Infix =
    match infix with
    | EPT.InfixFnCall(fn) -> PT.InfixFnCall(InfixFnName.toPT fn)
    | EPT.BinOp binop -> PT.BinOp(BinaryOperation.toPT binop)

  let fromPT (infix : PT.Infix) : EPT.Infix =
    match infix with
    | PT.InfixFnCall(fn) -> EPT.InfixFnCall(InfixFnName.fromPT fn)
    | PT.BinOp binop -> EPT.BinOp(BinaryOperation.fromPT binop)

module LetPattern =
  let rec toPT (p : EPT.LetPattern) : PT.LetPattern =
    match p with
    | EPT.LPVariable(id, str) -> PT.LPVariable(id, str)
    | EPT.LPTuple(id, first, second, theRest) ->
      PT.LPTuple(id, toPT first, toPT second, List.map toPT theRest)

  let rec fromPT (p : PT.LetPattern) : EPT.LetPattern =
    match p with
    | PT.LPVariable(id, str) -> EPT.LPVariable(id, str)
    | PT.LPTuple(id, first, second, theRest) ->
      EPT.LPTuple(id, fromPT first, fromPT second, List.map fromPT theRest)
    | PT.LPUnit id -> EPT.LPVariable(id, "")

module MatchPattern =
  let rec toPT (p : EPT.MatchPattern) : PT.MatchPattern =
    match p with
    | EPT.MPVariable(id, str) -> PT.MPVariable(id, str)
    | EPT.MPEnum(id, caseName, fieldPats) ->
      PT.MPEnum(id, caseName, List.map toPT fieldPats)
    | EPT.MPInt64(id, i) -> PT.MPInt64(id, i)
    | EPT.MPUInt64(id, i) -> PT.MPUInt64(id, i)
    | EPT.MPInt8(id, i) -> PT.MPInt8(id, i)
    | EPT.MPUInt8(id, i) -> PT.MPUInt8(id, i)
    | EPT.MPInt16(id, i) -> PT.MPInt16(id, i)
    | EPT.MPUInt16(id, i) -> PT.MPUInt16(id, i)
    | EPT.MPInt32(id, i) -> PT.MPInt32(id, i)
    | EPT.MPUInt32(id, i) -> PT.MPUInt32(id, i)
    | EPT.MPInt128(id, i) -> PT.MPInt128(id, i)
    | EPT.MPUInt128(id, i) -> PT.MPUInt128(id, i)
    | EPT.MPBool(id, b) -> PT.MPBool(id, b)
    | EPT.MPChar(id, c) -> PT.MPChar(id, c)
    | EPT.MPString(id, s) -> PT.MPString(id, s)
    | EPT.MPFloat(id, s, w, f) -> PT.MPFloat(id, Sign.toPT s, w, f)
    | EPT.MPUnit id -> PT.MPUnit id
    | EPT.MPTuple(id, first, second, theRest) ->
      PT.MPTuple(id, toPT first, toPT second, List.map toPT theRest)
    | EPT.MPList(id, pats) -> PT.MPList(id, List.map toPT pats)
    | EPT.MPListCons(id, head, tail) -> PT.MPListCons(id, toPT head, toPT tail)

  let rec fromPT (p : PT.MatchPattern) : EPT.MatchPattern =
    match p with
    | PT.MPVariable(id, str) -> EPT.MPVariable(id, str)
    | PT.MPEnum(id, caseName, fieldPats) ->
      EPT.MPEnum(id, caseName, List.map fromPT fieldPats)
    | PT.MPInt64(id, i) -> EPT.MPInt64(id, i)
    | PT.MPUInt64(id, i) -> EPT.MPUInt64(id, i)
    | PT.MPInt8(id, i) -> EPT.MPInt8(id, i)
    | PT.MPUInt8(id, i) -> EPT.MPUInt8(id, i)
    | PT.MPInt16(id, i) -> EPT.MPInt16(id, i)
    | PT.MPUInt16(id, i) -> EPT.MPUInt16(id, i)
    | PT.MPInt32(id, i) -> EPT.MPInt32(id, i)
    | PT.MPUInt32(id, i) -> EPT.MPUInt32(id, i)
    | PT.MPInt128(id, i) -> EPT.MPInt128(id, i)
    | PT.MPUInt128(id, i) -> EPT.MPUInt128(id, i)
    | PT.MPBool(id, b) -> EPT.MPBool(id, b)
    | PT.MPChar(id, c) -> EPT.MPChar(id, c)
    | PT.MPString(id, s) -> EPT.MPString(id, s)
    | PT.MPFloat(id, s, w, f) -> EPT.MPFloat(id, Sign.fromPT s, w, f)
    | PT.MPUnit id -> EPT.MPUnit id
    | PT.MPTuple(id, first, second, theRest) ->
      EPT.MPTuple(id, fromPT first, fromPT second, List.map fromPT theRest)
    | PT.MPList(id, pats) -> EPT.MPList(id, List.map fromPT pats)
    | PT.MPListCons(id, head, tail) -> EPT.MPListCons(id, fromPT head, fromPT tail)

module Expr =
  let rec toPT (e : EPT.Expr) : PT.Expr =
    match e with
    | EPT.EChar(id, char) -> PT.EChar(id, char)
    | EPT.EInt64(id, num) -> PT.EInt64(id, num)
    | EPT.EUInt64(id, num) -> PT.EUInt64(id, num)
    | EPT.EInt8(id, num) -> PT.EInt8(id, num)
    | EPT.EUInt8(id, num) -> PT.EUInt8(id, num)
    | EPT.EInt16(id, num) -> PT.EInt16(id, num)
    | EPT.EUInt16(id, num) -> PT.EUInt16(id, num)
    | EPT.EInt32(id, num) -> PT.EInt32(id, num)
    | EPT.EUInt32(id, num) -> PT.EUInt32(id, num)
    | EPT.EInt128(id, num) -> PT.EInt128(id, num)
    | EPT.EUInt128(id, num) -> PT.EUInt128(id, num)
    | EPT.EString(id, segment) -> PT.EString(id, List.map stringSegmentToPT segment)
    | EPT.EFloat(id, sign, whole, fraction) ->
      PT.EFloat(id, Sign.toPT sign, whole, fraction)
    | EPT.EBool(id, b) -> PT.EBool(id, b)
    | EPT.EUnit id -> PT.EUnit id
    | EPT.EConstant(id, name) ->
      PT.EConstant(id, NameResolution.toPT ConstantName.toPT name)
    | EPT.EVariable(id, var) -> PT.EVariable(id, var)
    | EPT.ERecordFieldAccess(id, obj, fieldname) ->
      PT.ERecordFieldAccess(id, toPT obj, fieldname)
    | EPT.EApply(id, name, typeArgs, args) ->
      PT.EApply(
        id,
        toPT name,
        List.map TypeReference.toPT typeArgs,
        NEList.map toPT args
      )
    | EPT.ELambda(id, pats, body) ->
      PT.ELambda(id, NEList.map LetPattern.toPT pats, toPT body)
    | EPT.ELet(id, pat, rhs, body) ->
      PT.ELet(id, LetPattern.toPT pat, toPT rhs, toPT body)
    | EPT.EIf(id, cond, thenExpr, elseExpr) ->
      PT.EIf(id, toPT cond, toPT thenExpr, Option.map toPT elseExpr)
    | EPT.EList(id, exprs) -> PT.EList(id, List.map toPT exprs)
    | EPT.ETuple(id, first, second, theRest) ->
      PT.ETuple(id, toPT first, toPT second, List.map toPT theRest)
    | EPT.ERecord(id, typeName, typeArgs, fields) ->
      PT.ERecord(
        id,
        NameResolution.toPT TypeName.toPT typeName,
        List.map TypeReference.toPT typeArgs,
        List.map (Tuple2.mapSecond toPT) fields
      )
    | EPT.ERecordUpdate(id, record, updates) ->
      PT.ERecordUpdate(
        id,
        toPT record,
        updates |> NEList.map (fun (name, expr) -> (name, toPT expr))
      )
    | EPT.EPipe(pipeID, expr1, rest) ->
      PT.EPipe(pipeID, toPT expr1, List.map pipeExprToPT rest)
    | EPT.EEnum(id, typeName, typeArgs, caseName, exprs) ->
      PT.EEnum(
        id,
        NameResolution.toPT TypeName.toPT typeName,
        List.map TypeReference.toPT typeArgs,
        caseName,
        List.map toPT exprs
      )
    | EPT.EMatch(id, mexpr, cases) ->
      PT.EMatch(id, toPT mexpr, List.map matchCaseToPT cases)
    | EPT.EInfix(id, infix, arg1, arg2) ->
      PT.EInfix(id, Infix.toPT infix, toPT arg1, toPT arg2)
    | EPT.EDict(id, pairs) -> PT.EDict(id, List.map (Tuple2.mapSecond toPT) pairs)
    | EPT.EFnName(id, name) -> PT.EFnName(id, NameResolution.toPT FnName.toPT name)

  and stringSegmentToPT (segment : EPT.StringSegment) : PT.StringSegment =
    match segment with
    | EPT.StringText text -> PT.StringText text
    | EPT.StringInterpolation expr -> PT.StringInterpolation(toPT expr)

  and pipeExprToPT (pipeExpr : EPT.PipeExpr) : PT.PipeExpr =
    match pipeExpr with
    | EPT.EPipeVariable(id, name, exprs) ->
      PT.EPipeVariable(id, name, List.map toPT exprs)
    | EPT.EPipeLambda(id, pats, body) ->
      PT.EPipeLambda(id, NEList.map LetPattern.toPT pats, toPT body)
    | EPT.EPipeInfix(id, infix, first) ->
      PT.EPipeInfix(id, Infix.toPT infix, toPT first)
    | EPT.EPipeFnCall(id, fnName, typeArgs, args) ->
      PT.EPipeFnCall(
        id,
        NameResolution.toPT FnName.toPT fnName,
        List.map TypeReference.toPT typeArgs,
        List.map toPT args
      )
    | EPT.EPipeEnum(id, typeName, caseName, fields) ->
      PT.EPipeEnum(
        id,
        NameResolution.toPT TypeName.toPT typeName,
        caseName,
        List.map toPT fields
      )

  and matchCaseToPT (case : EPT.MatchCase) : PT.MatchCase =
    { pat = MatchPattern.toPT case.pat
      whenCondition = Option.map toPT case.whenCondition
      rhs = toPT case.rhs }


  let rec fromPT (e : PT.Expr) : EPT.Expr =
    match e with
    | PT.EChar(id, char) -> EPT.EChar(id, char)
    | PT.EInt64(id, num) -> EPT.EInt64(id, num)
    | PT.EUInt64(id, num) -> EPT.EUInt64(id, num)
    | PT.EInt8(id, num) -> EPT.EInt8(id, num)
    | PT.EUInt8(id, num) -> EPT.EUInt8(id, num)
    | PT.EInt16(id, num) -> EPT.EInt16(id, num)
    | PT.EUInt16(id, num) -> EPT.EUInt16(id, num)
    | PT.EInt32(id, num) -> EPT.EInt32(id, num)
    | PT.EUInt32(id, num) -> EPT.EUInt32(id, num)
    | PT.EInt128(id, num) -> EPT.EInt128(id, num)
    | PT.EUInt128(id, num) -> EPT.EUInt128(id, num)
    | PT.EString(id, segment) -> EPT.EString(id, List.map stringSegmentFromPT segment)
    | PT.EFloat(id, sign, whole, fraction) ->
      EPT.EFloat(id, Sign.fromPT sign, whole, fraction)
    | PT.EBool(id, b) -> EPT.EBool(id, b)
    | PT.EUnit id -> EPT.EUnit id
    | PT.EConstant(id, name) ->
      EPT.EConstant(id, NameResolution.fromPT ConstantName.fromPT name)
    | PT.EVariable(id, var) -> EPT.EVariable(id, var)
    | PT.ERecordFieldAccess(id, obj, fieldname) ->
      EPT.ERecordFieldAccess(id, fromPT obj, fieldname)
    | PT.EApply(id, name, typeArgs, args) ->
      EPT.EApply(
        id,
        fromPT name,
        List.map TypeReference.fromPT typeArgs,
        NEList.map fromPT args
      )
    | PT.ELambda(id, pats, body) ->
      EPT.ELambda(id, NEList.map LetPattern.fromPT pats, fromPT body)
    | PT.ELet(id, pat, rhs, body) ->
      EPT.ELet(id, LetPattern.fromPT pat, fromPT rhs, fromPT body)
    | PT.EIf(id, cond, thenExpr, elseExpr) ->
      EPT.EIf(id, fromPT cond, fromPT thenExpr, Option.map fromPT elseExpr)
    | PT.EList(id, exprs) -> EPT.EList(id, List.map fromPT exprs)
    | PT.ETuple(id, first, second, theRest) ->
      EPT.ETuple(id, fromPT first, fromPT second, List.map fromPT theRest)
    | PT.ERecord(id, typeName, typeArgs, fields) ->
      EPT.ERecord(
        id,
        NameResolution.fromPT TypeName.fromPT typeName,
        List.map TypeReference.fromPT typeArgs,
        List.map (Tuple2.mapSecond fromPT) fields
      )
    | PT.ERecordUpdate(id, record, updates) ->
      EPT.ERecordUpdate(
        id,
        fromPT record,
        updates |> NEList.map (fun (name, expr) -> (name, fromPT expr))
      )
    | PT.EPipe(pipeID, expr1, rest) ->
      EPT.EPipe(pipeID, fromPT expr1, List.map pipeExprFromPT rest)
    | PT.EEnum(id, typeName, typeArgs, caseName, exprs) ->
      EPT.EEnum(
        id,
        NameResolution.fromPT TypeName.fromPT typeName,
        List.map TypeReference.fromPT typeArgs,
        caseName,
        List.map fromPT exprs
      )
    | PT.EMatch(id, mexpr, cases) ->
      EPT.EMatch(id, fromPT mexpr, List.map matchCaseFromPT cases)
    | PT.EInfix(id, infix, arg1, arg2) ->
      EPT.EInfix(id, Infix.fromPT infix, fromPT arg1, fromPT arg2)
    | PT.EDict(id, pairs) -> EPT.EDict(id, List.map (Tuple2.mapSecond fromPT) pairs)
    | PT.EFnName(id, name) -> EPT.EFnName(id, NameResolution.fromPT FnName.fromPT name)


  and stringSegmentFromPT (segment : PT.StringSegment) : EPT.StringSegment =
    match segment with
    | PT.StringText text -> EPT.StringText text
    | PT.StringInterpolation expr -> EPT.StringInterpolation(fromPT expr)


  and pipeExprFromPT (pipeExpr : PT.PipeExpr) : EPT.PipeExpr =
    match pipeExpr with
    | PT.EPipeVariable(id, name, exprs) ->
      EPT.EPipeVariable(id, name, List.map fromPT exprs)
    | PT.EPipeLambda(id, pats, body) ->
      EPT.EPipeLambda(id, NEList.map LetPattern.fromPT pats, fromPT body)
    | PT.EPipeInfix(id, infix, first) ->
      EPT.EPipeInfix(id, Infix.fromPT infix, fromPT first)
    | PT.EPipeFnCall(id, fnName, typeArgs, args) ->
      EPT.EPipeFnCall(
        id,
        NameResolution.fromPT FnName.fromPT fnName,
        List.map TypeReference.fromPT typeArgs,
        List.map fromPT args
      )
    | PT.EPipeEnum(id, typeName, caseName, fields) ->
      EPT.EPipeEnum(
        id,
        NameResolution.fromPT TypeName.fromPT typeName,
        caseName,
        List.map fromPT fields
      )


  and matchCaseFromPT (case : PT.MatchCase) : EPT.MatchCase =
    { pat = MatchPattern.fromPT case.pat
      whenCondition = Option.map fromPT case.whenCondition
      rhs = fromPT case.rhs }

module Deprecation =
  let toPT
    (f : 'name1 -> 'name2)
    (d : EPT.Deprecation<'name1>)
    : PT.Deprecation<'name2> =
    match d with
    | EPT.NotDeprecated -> PT.NotDeprecated
    | EPT.RenamedTo name -> PT.RenamedTo(f name)
    | EPT.ReplacedBy name -> PT.ReplacedBy(f name)
    | EPT.DeprecatedBecause reason -> PT.DeprecatedBecause reason

  let fromPT
    (f : 'name2 -> 'name1)
    (d : PT.Deprecation<'name2>)
    : EPT.Deprecation<'name1> =
    match d with
    | PT.NotDeprecated -> EPT.NotDeprecated
    | PT.RenamedTo name -> EPT.RenamedTo(f name)
    | PT.ReplacedBy name -> EPT.ReplacedBy(f name)
    | PT.DeprecatedBecause reason -> EPT.DeprecatedBecause reason

module TypeDeclaration =
  module RecordField =
    let toPT (f : EPT.TypeDeclaration.RecordField) : PT.TypeDeclaration.RecordField =
      { name = f.name; typ = TypeReference.toPT f.typ; description = f.description }

    let fromPT (f : PT.TypeDeclaration.RecordField) : EPT.TypeDeclaration.RecordField =
      { name = f.name; typ = TypeReference.fromPT f.typ; description = f.description }

  module EnumField =
    let toPT (f : EPT.TypeDeclaration.EnumField) : PT.TypeDeclaration.EnumField =
      { typ = TypeReference.toPT f.typ
        label = f.label
        description = f.description }

    let fromPT (f : PT.TypeDeclaration.EnumField) : EPT.TypeDeclaration.EnumField =
      { typ = TypeReference.fromPT f.typ
        label = f.label
        description = f.description }

  module EnumCase =
    let toPT (c : EPT.TypeDeclaration.EnumCase) : PT.TypeDeclaration.EnumCase =
      { name = c.name
        fields = List.map EnumField.toPT c.fields
        description = c.description }

    let fromPT (c : PT.TypeDeclaration.EnumCase) : EPT.TypeDeclaration.EnumCase =
      { name = c.name
        fields = List.map EnumField.fromPT c.fields
        description = c.description }

  module Definition =
    let toPT (d : EPT.TypeDeclaration.Definition) : PT.TypeDeclaration.Definition =
      match d with
      | EPT.TypeDeclaration.Alias typ ->
        PT.TypeDeclaration.Alias(TypeReference.toPT typ)
      | EPT.TypeDeclaration.Record fields ->
        PT.TypeDeclaration.Record(NEList.map RecordField.toPT fields)
      | EPT.TypeDeclaration.Enum cases ->
        PT.TypeDeclaration.Enum(NEList.map EnumCase.toPT cases)

    let fromPT (d : PT.TypeDeclaration.Definition) : EPT.TypeDeclaration.Definition =
      match d with
      | PT.TypeDeclaration.Alias typ ->
        EPT.TypeDeclaration.Alias(TypeReference.fromPT typ)
      | PT.TypeDeclaration.Record fields ->
        EPT.TypeDeclaration.Record(NEList.map RecordField.fromPT fields)
      | PT.TypeDeclaration.Enum cases ->
        EPT.TypeDeclaration.Enum(NEList.map EnumCase.fromPT cases)

  let toPT (d : EPT.TypeDeclaration.TypeDeclaration) : PT.TypeDeclaration.T =
    { typeParams = d.typeParams; definition = Definition.toPT d.definition }

  let fromPT (d : PT.TypeDeclaration.T) : EPT.TypeDeclaration.TypeDeclaration =
    { typeParams = d.typeParams; definition = Definition.fromPT d.definition }

module PackageFn =
  module Name =
    let toPT (n : EPT.PackageFn.Name) : PT.PackageFn.Name =
      { owner = n.owner; modules = n.modules; name = n.name }

    let fromPT (n : PT.PackageFn.Name) : EPT.PackageFn.Name =
      { owner = n.owner; modules = n.modules; name = n.name }

  module Parameter =
    let toPT (p : EPT.PackageFn.Parameter) : PT.PackageFn.Parameter =
      { name = p.name; typ = TypeReference.toPT p.typ; description = p.description }

    let fromPT (p : PT.PackageFn.Parameter) : EPT.PackageFn.Parameter =
      { name = p.name; typ = TypeReference.fromPT p.typ; description = p.description }

  let toPT (fn : EPT.PackageFn.PackageFn) : PT.PackageFn.PackageFn =
    { id = fn.id
      name = Name.toPT fn.name
      parameters = NEList.map Parameter.toPT fn.parameters
      returnType = TypeReference.toPT fn.returnType
      description = fn.description
      deprecated = Deprecation.toPT FnName.toPT fn.deprecated
      body = Expr.toPT fn.body
      typeParams = fn.typeParams }

  let fromPT (fn : PT.PackageFn.PackageFn) : EPT.PackageFn.PackageFn =
    { id = fn.id
      name = Name.fromPT fn.name
      parameters = NEList.map Parameter.fromPT fn.parameters
      returnType = TypeReference.fromPT fn.returnType
      description = fn.description
      deprecated = Deprecation.fromPT FnName.fromPT fn.deprecated
      body = Expr.fromPT fn.body
      typeParams = fn.typeParams }

module PackageType =
  module Name =
    let toPT (n : EPT.PackageType.Name) : PT.PackageType.Name =
      { owner = n.owner; modules = n.modules; name = n.name }

    let fromPT (n : PT.PackageType.Name) : EPT.PackageType.Name =
      { owner = n.owner; modules = n.modules; name = n.name }

  let toPT (pt : EPT.PackageType.PackageType) : PT.PackageType.PackageType =
    { id = pt.id
      name = Name.toPT pt.name
      description = pt.description
      declaration = TypeDeclaration.toPT pt.declaration
      deprecated = Deprecation.toPT TypeName.toPT pt.deprecated }

  let fromPT (pt : PT.PackageType.PackageType) : EPT.PackageType.PackageType =
    { id = pt.id
      name = Name.fromPT pt.name
      description = pt.description
      declaration = TypeDeclaration.fromPT pt.declaration
      deprecated = Deprecation.fromPT TypeName.fromPT pt.deprecated }

module Const =
  let rec toPT (c : EPT.Const) : PT.Const =
    match c with
    | EPT.CInt64 i -> PT.CInt64 i
    | EPT.CUInt64 i -> PT.CUInt64 i
    | EPT.CInt8 i -> PT.CInt8 i
    | EPT.CUInt8 i -> PT.CUInt8 i
    | EPT.CInt16 i -> PT.CInt16 i
    | EPT.CUInt16 i -> PT.CUInt16 i
    | EPT.CInt32 i -> PT.CInt32 i
    | EPT.CUInt32 i -> PT.CUInt32 i
    | EPT.CInt128 i -> PT.CInt128 i
    | EPT.CUInt128 i -> PT.CUInt128 i
    | EPT.CBool b -> PT.CBool b
    | EPT.CString s -> PT.CString s
    | EPT.CChar c -> PT.CChar c
    | EPT.CFloat(s, w, f) -> PT.CFloat(Sign.toPT s, w, f)
    | EPT.CUnit -> PT.CUnit
    | EPT.CTuple(first, second, rest) ->
      PT.CTuple(toPT first, toPT second, List.map toPT rest)
    | EPT.CEnum(typeName, caseName, fields) ->
      PT.CEnum(
        NameResolution.toPT TypeName.toPT typeName,
        caseName,
        List.map toPT fields
      )
    | EPT.CList l -> PT.CList(List.map toPT l)
    | EPT.CDict pairs -> PT.CDict(List.map (Tuple2.mapSecond toPT) pairs)

  let rec fromPT (c : PT.Const) : EPT.Const =
    match c with
    | PT.CInt64 i -> EPT.CInt64 i
    | PT.CUInt64 i -> EPT.CUInt64 i
    | PT.CInt8 i -> EPT.CInt8 i
    | PT.CUInt8 i -> EPT.CUInt8 i
    | PT.CInt16 i -> EPT.CInt16 i
    | PT.CUInt16 i -> EPT.CUInt16 i
    | PT.CInt32 i -> EPT.CInt32 i
    | PT.CUInt32 i -> EPT.CUInt32 i
    | PT.CInt128 i -> EPT.CInt128 i
    | PT.CUInt128 i -> EPT.CUInt128 i
    | PT.CBool b -> EPT.CBool b
    | PT.CString s -> EPT.CString s
    | PT.CChar c -> EPT.CChar c
    | PT.CFloat(s, w, f) -> EPT.CFloat(Sign.fromPT s, w, f)
    | PT.CUnit -> EPT.CUnit
    | PT.CTuple(first, second, rest) ->
      EPT.CTuple(fromPT first, fromPT second, List.map fromPT rest)
    | PT.CEnum(typeName, caseName, fields) ->
      EPT.CEnum(
        NameResolution.fromPT TypeName.fromPT typeName,
        caseName,
        List.map fromPT fields
      )
    | PT.CList l -> EPT.CList(List.map fromPT l)
    | PT.CDict pairs -> EPT.CDict(List.map (Tuple2.mapSecond fromPT) pairs)


module PackageConstant =
  module Name =
    let toPT (n : EPT.PackageConstant.Name) : PT.PackageConstant.Name =
      { owner = n.owner; modules = n.modules; name = n.name }

    let fromPT (n : PT.PackageConstant.Name) : EPT.PackageConstant.Name =
      { owner = n.owner; modules = n.modules; name = n.name }

  let toPT
    (c : EPT.PackageConstant.PackageConstant)
    : PT.PackageConstant.PackageConstant =
    { id = c.id
      name = Name.toPT c.name
      description = c.description
      deprecated = Deprecation.toPT ConstantName.toPT c.deprecated
      body = Const.toPT c.body }

  let fromPT
    (c : PT.PackageConstant.PackageConstant)
    : EPT.PackageConstant.PackageConstant =
    { id = c.id
      name = Name.fromPT c.name
      description = c.description
      deprecated = Deprecation.fromPT ConstantName.fromPT c.deprecated
      body = Const.fromPT c.body }
