/// Convert from ProgramTypes to RuntimeTypes
module LibExecution.ProgramTypesToRuntimeTypes

open Prelude

// Used for conversion functions
module RT = RuntimeTypes
module VT = ValueType
module PT = ProgramTypes

module FQTypeName =
  module Package =
    let toRT (p : PT.FQTypeName.Package) : RT.FQTypeName.Package = p

  let toRT (fqtn : PT.FQTypeName.FQTypeName) : RT.FQTypeName.FQTypeName =
    match fqtn with
    | PT.FQTypeName.Package p -> RT.FQTypeName.Package(Package.toRT p)


module FQConstantName =
  module Builtin =
    let toRT (c : PT.FQConstantName.Builtin) : RT.FQConstantName.Builtin =
      { name = c.name; version = c.version }

  module Package =
    let toRT (c : PT.FQConstantName.Package) : RT.FQConstantName.Package = c

  let toRT
    (name : PT.FQConstantName.FQConstantName)
    : RT.FQConstantName.FQConstantName =
    match name with
    | PT.FQConstantName.Builtin s -> RT.FQConstantName.Builtin(Builtin.toRT s)
    | PT.FQConstantName.Package p -> RT.FQConstantName.Package(Package.toRT p)


module FQFnName =
  module Builtin =
    let toRT (s : PT.FQFnName.Builtin) : RT.FQFnName.Builtin =
      { name = s.name; version = s.version }

  module Package =
    let toRT (p : PT.FQFnName.Package) : RT.FQFnName.Package = p

  let toRT (fqfn : PT.FQFnName.FQFnName) : RT.FQFnName.FQFnName =
    match fqfn with
    | PT.FQFnName.Builtin s -> RT.FQFnName.Builtin(Builtin.toRT s)
    | PT.FQFnName.Package p -> RT.FQFnName.Package(Package.toRT p)


module NameResolutionError =
  let toRT (e : PT.NameResolutionError) : RT.NameResolutionError =
    match e with
    | PT.NameResolutionError.NotFound names -> RT.NameResolutionError.NotFound names
    | PT.NameResolutionError.InvalidName names ->
      RT.NameResolutionError.InvalidName names

module NameResolution =
  let toRT (f : 'a -> 'b) (nr : PT.NameResolution<'a>) : RT.NameResolution<'b> =
    match nr with
    | Ok x -> Ok(f x)
    | Error e -> Error(NameResolutionError.toRT e)


module TypeReference =
  let rec toRT (t : PT.TypeReference) : RT.TypeReference =
    match t with
    | PT.TUnit -> RT.TUnit

    | PT.TBool -> RT.TBool

    | PT.TInt8 -> RT.TInt8
    | PT.TUInt8 -> RT.TUInt8
    | PT.TInt16 -> RT.TInt16
    | PT.TUInt16 -> RT.TUInt16
    | PT.TInt32 -> RT.TInt32
    | PT.TUInt32 -> RT.TUInt32
    | PT.TInt64 -> RT.TInt64
    | PT.TUInt64 -> RT.TUInt64
    | PT.TInt128 -> RT.TInt128
    | PT.TUInt128 -> RT.TUInt128

    | PT.TFloat -> RT.TFloat

    | PT.TChar -> RT.TChar
    | PT.TString -> RT.TString

    | PT.TDateTime -> RT.TDateTime
    | PT.TUuid -> RT.TUuid

    | PT.TList inner -> RT.TList(toRT inner)
    | PT.TTuple(first, second, theRest) ->
      RT.TTuple(toRT first, toRT second, theRest |> List.map toRT)
    | PT.TDict typ -> RT.TDict(toRT typ)

    | PT.TCustomType(typeName, typeArgs) ->
      RT.TCustomType(
        NameResolution.toRT FQTypeName.toRT typeName,
        List.map toRT typeArgs
      )

    | PT.TVariable(name) -> RT.TVariable(name)

    | PT.TFn(paramTypes, returnType) ->
      RT.TFn(NEList.map toRT paramTypes, toRT returnType)

    | PT.TDB typ -> RT.TDB(toRT typ)


module InfixFnName =
  let toFnName (name : PT.InfixFnName) : RT.FQFnName.Builtin =
    let make = RT.FQFnName.builtin

    match name with
    | PT.ArithmeticPlus -> make "int64Add" 0
    | PT.ArithmeticMinus -> make "int64Subtract" 0
    | PT.ArithmeticMultiply -> make "int64Multiply" 0
    | PT.ArithmeticDivide -> make "floatDivide" 0
    | PT.ArithmeticModulo -> make "int64Mod" 0
    | PT.ArithmeticPower -> make "int64Power" 0
    | PT.ComparisonGreaterThan -> make "int64GreaterThan" 0
    | PT.ComparisonGreaterThanOrEqual -> make "int64GreaterThanOrEqualTo" 0
    | PT.ComparisonLessThan -> make "int64LessThan" 0
    | PT.ComparisonLessThanOrEqual -> make "int64LessThanOrEqualTo" 0
    | PT.StringConcat -> make "stringAppend" 0
    | PT.ComparisonEquals -> make "equals" 0
    | PT.ComparisonNotEquals -> make "notEquals" 0


module LetPattern =
  let rec toRT
    (symbols : Map<string, RT.Register>)
    (rc : int)
    (p : PT.LetPattern)
    : (RT.LetPattern * Map<string, RT.Register> * int) =
    match p with
    | PT.LPUnit _ -> RT.LPUnit, Map.empty, rc

    | PT.LPTuple(_, first, second, theRest) ->
      let first, symbols, rc = toRT symbols rc first
      let second, symbols, rc = toRT symbols rc second
      let (symbols, rc, theRest) =
        theRest
        |> List.fold
          (fun (symbols, rc, pats) pat ->
            let pat, symbols, rc = toRT symbols rc pat
            (symbols, rc, pats @ [ pat ]))
          (symbols, rc, [])

      RT.LPTuple(first, second, theRest), symbols, rc

    | PT.LPVariable(_, name) ->
      RT.LPVariable rc, (symbols |> Map.add name rc), rc + 1


  let toInstr
    (valueReg : RT.Register)
    (rc : int)
    (p : PT.LetPattern)
    : (RT.Instruction * Map<string, RT.Register> * int) =
    let (pat, rcAfterPat, symbols) = toRT Map.empty rc p
    RT.CheckLetPatternAndExtractVars(valueReg, pat), rcAfterPat, symbols



module MatchPattern =
  let rec toRT
    (symbols : Map<string, RT.Register>)
    (rc : int)
    (p : PT.MatchPattern)
    : (RT.MatchPattern * Map<string, RT.Register> * int) =
    match p with
    | PT.MPUnit _ -> RT.MPUnit, symbols, rc

    | PT.MPBool(_, b) -> RT.MPBool b, symbols, rc

    | PT.MPInt8(_, i) -> RT.MPInt8 i, symbols, rc
    | PT.MPUInt8(_, i) -> RT.MPUInt8 i, symbols, rc
    | PT.MPInt16(_, i) -> RT.MPInt16 i, symbols, rc
    | PT.MPUInt16(_, i) -> RT.MPUInt16 i, symbols, rc
    | PT.MPInt32(_, i) -> RT.MPInt32 i, symbols, rc
    | PT.MPUInt32(_, i) -> RT.MPUInt32 i, symbols, rc
    | PT.MPInt64(_, i) -> RT.MPInt64 i, symbols, rc
    | PT.MPUInt64(_, i) -> RT.MPUInt64 i, symbols, rc
    | PT.MPInt128(_, i) -> RT.MPInt128 i, symbols, rc
    | PT.MPUInt128(_, i) -> RT.MPUInt128 i, symbols, rc

    | PT.MPFloat(_, sign, whole, frac) ->
      RT.MPFloat(makeFloat sign whole frac), symbols, rc

    | PT.MPChar(_, c) -> RT.MPChar c, symbols, rc
    | PT.MPString(_, s) -> RT.MPString s, symbols, rc

    | PT.MPList(_, pats) ->
      let pats, symbols, rc =
        pats
        |> List.fold
          (fun (pats, symbols, rc) pat ->
            let pat, symbols, rc = toRT symbols rc pat
            (pats @ [ pat ], symbols, rc))
          ([], symbols, rc)

      RT.MPList pats, symbols, rc


    | PT.MPListCons(_, head, tail) ->
      let head, symbols, rc = toRT symbols rc head
      let tail, symbols, rc = toRT symbols rc tail
      RT.MPListCons(head, tail), symbols, rc

    | PT.MPTuple(_, first, second, theRest) ->
      let first, symbols, rc = toRT symbols rc first
      let second, symbols, rc = toRT symbols rc second
      let (symbols, rc, theRest) =
        theRest
        |> List.fold
          (fun (symbols, rc, pats) pat ->
            let pat, symbols, rc = toRT symbols rc pat
            (symbols, rc, pats @ [ pat ]))
          (symbols, rc, [])

      RT.MPTuple(first, second, theRest), symbols, rc

    | PT.MPEnum(_, caseName, fieldPats) ->
      let fieldPats, symbols, rc =
        fieldPats
        |> List.fold
          (fun (fieldPats, symbols, rc) fieldPat ->
            let pat, symbols, rc = toRT symbols rc fieldPat
            (fieldPats @ [ pat ], symbols, rc))
          ([], symbols, rc)

      RT.MPEnum(caseName, fieldPats), symbols, rc

    | PT.MPVariable(_, name) ->
      RT.MPVariable rc, (symbols |> Map.add name rc), rc + 1



module MatchCase =
  /// Compiling a MatchCase happens in two phases, because many instructions
  /// require knowing how many instructions to jump over, which we can't know
  /// until we know the basics of all the cases.
  ///
  /// This type holds all the information we gather as part of the first phase
  /// , in order of where the instrs should be at the end of the second phase.
  ///
  /// Note: not represented here, we'll also need an unconditional `JumpBy` instr
  /// , to get past all the cases. We can only determine how many instrs to jump
  /// after the first phases is complete, but it'll land at the end of these.
  type IntermediateValue =
    {
      /// jumpByFail -> instr
      /// `RT.MatchValue(valueReg, pat, jumpByFail)`
      /// (the `pat` and `valueReg` are known in the first phase)
      matchValueInstrFn : int -> RT.Instruction

      /// Evaluation of the `whenCondition` (if it exists -- might be empty)
      whenCondInstructions : List<RT.Instruction>

      /// (jumpBy) -> instr
      /// `RT.JumpByIfFalse(jumpBy, whenCondResultReg)`
      /// (`whenCondResultReg` is known in the first phase)
      whenCondJump : Option<int -> RT.Instruction>

      /// Evaluation of the RHS
      ///
      /// Includes `CopyVal(resultReg, rhsResultReg)`
      rhsInstrs : List<RT.Instruction>

      /// RC after all instructions
      ///
      /// Note: Different branches/cases will require different # of registers
      /// , so we'll end up taking the max of all the RCs
      rc : int
    }


module Expr =
  let rec toRT
    (symbols : Map<string, RT.Register>)
    (rc : int)
    (e : PT.Expr)
    : RT.Instructions =
    let justLoadDval dv : RT.Instructions =
      { registerCount = rc + 1
        instructions = [ RT.LoadVal(rc, dv) ]
        resultIn = rc }

    match e with
    | PT.EUnit _id -> justLoadDval RT.DUnit

    | PT.EBool(_id, b) -> justLoadDval (RT.DBool b)

    | PT.EInt8(_id, num) -> justLoadDval (RT.DInt8 num)
    | PT.EInt16(_id, num) -> justLoadDval (RT.DInt16 num)
    | PT.EInt32(_id, num) -> justLoadDval (RT.DInt32 num)
    | PT.EInt64(_id, num) -> justLoadDval (RT.DInt64 num)
    | PT.EInt128(_id, num) -> justLoadDval (RT.DInt128 num)
    | PT.EUInt8(_id, num) -> justLoadDval (RT.DUInt8 num)
    | PT.EUInt16(_id, num) -> justLoadDval (RT.DUInt16 num)
    | PT.EUInt32(_id, num) -> justLoadDval (RT.DUInt32 num)
    | PT.EUInt64(_id, num) -> justLoadDval (RT.DUInt64 num)
    | PT.EUInt128(_id, num) -> justLoadDval (RT.DUInt128 num)

    | PT.EFloat(_id, sign, whole, fraction) ->
      let whole = if whole = "" then "0" else whole
      let fraction = if fraction = "" then "0" else fraction
      justLoadDval (RT.DFloat(makeFloat sign whole fraction))

    | PT.EChar(_id, c) -> justLoadDval (RT.DChar c)

    | PT.EString(_id, segments) ->
      match segments with
      // if there's only one segment, just load it directly
      | [ PT.StringText text ] -> justLoadDval (RT.DString text)

      // otherwise, handle each segment separately
      // and then create a string from the parts
      | segments ->
        let (rc, instrs, segments) =
          List.fold
            (fun (rc, instrs, segments) segment ->
              match segment with
              | PT.StringText text ->
                (rc, instrs, segments @ [ RT.StringSegment.Text text ])

              | PT.StringInterpolation expr ->
                let exprInstrs = toRT symbols rc expr

                (exprInstrs.registerCount,
                 instrs @ exprInstrs.instructions,
                 segments @ [ RT.Interpolated exprInstrs.resultIn ]))
            (rc, [], [])
            segments

        { registerCount = rc + 1
          instructions = instrs @ [ RT.CreateString(rc, segments) ]
          resultIn = rc }


    | PT.EList(_id, items) ->
      let listReg = rc
      let init = (rc + 1, [], [])

      let (regCounter, instrs, itemResultRegs) =
        items
        |> List.fold
          (fun (rc, instrs, itemResultRegs) item ->
            let itemInstrs = toRT symbols rc item
            (itemInstrs.registerCount,
             instrs @ itemInstrs.instructions,
             itemResultRegs @ [ itemInstrs.resultIn ]))
          init

      { registerCount = regCounter
        instructions = instrs @ [ RT.CreateList(listReg, itemResultRegs) ]
        resultIn = listReg }


    | PT.EDict(_id, items) ->
      let dictReg = rc
      let init = (rc + 1, [], [])

      let (regCounter, instrs, entryPairs) =
        items
        |> List.fold
          (fun (rc, instrs, entryPairs) (key, value) ->
            let itemInstrs = toRT symbols rc value
            (itemInstrs.registerCount,
             instrs @ itemInstrs.instructions,
             entryPairs @ [ (key, itemInstrs.resultIn) ]))
          init

      { registerCount = regCounter
        instructions = instrs @ [ RT.CreateDict(dictReg, entryPairs) ]
        resultIn = dictReg }


    | PT.ETuple(_id, first, second, theRest) ->
      // reserve the 'first' register for the result
      let tupleReg, rc = rc, rc + 1

      let first = toRT symbols rc first
      let second = toRT symbols first.registerCount second
      let (rcAfterAll, theRestInstrs, theRestRegs) =
        theRest
        |> List.fold
          (fun (rc, instrs, resultRegs) item ->
            let itemInstrs = toRT symbols rc item
            (itemInstrs.registerCount,
             instrs @ itemInstrs.instructions,
             resultRegs @ [ itemInstrs.resultIn ]))
          (second.registerCount, [], [])

      { registerCount = rcAfterAll
        instructions =
          first.instructions
          @ second.instructions
          @ theRestInstrs
          @ [ RT.CreateTuple(tupleReg, first.resultIn, second.resultIn, theRestRegs) ]
        resultIn = tupleReg }


    // let x = 1
    | PT.ELet(_id, pat, expr, body) ->
      let exprInstrs = toRT symbols rc expr
      let patInstr, newSymbols, rcAfterPat =
        LetPattern.toInstr exprInstrs.resultIn exprInstrs.registerCount pat
      let symbols = Map.mergeFavoringRight symbols newSymbols
      let bodyInstrs = toRT symbols rcAfterPat body
      { registerCount = bodyInstrs.registerCount
        instructions =
          exprInstrs.instructions @ [ patInstr ] @ bodyInstrs.instructions
        resultIn = bodyInstrs.resultIn }


    | PT.EVariable(_id, varName) ->
      match Map.find varName symbols with
      | Some reg -> { registerCount = rc; instructions = []; resultIn = reg }
      | None ->
        // CLEANUP see note in interpreter around VarNotFound
        // (work should be done around _references_)
        { registerCount = rc + 1
          instructions = [ RT.VarNotFound(rc, varName) ]
          resultIn = rc }


    | PT.EIf(_id, cond, thenExpr, elseExpr) ->
      // We need a consistent result register,
      // so we'll create this, and copy to it at the end of each branch
      let resultReg, rc = rc, rc + 1

      let cond = toRT symbols rc cond
      let jumpIfCondFalse jumpBy = [ RT.JumpByIfFalse(jumpBy, cond.resultIn) ]

      let thenInstrs = toRT symbols cond.registerCount thenExpr
      let copyThenToResultInstr = [ RT.CopyVal(resultReg, thenInstrs.resultIn) ]

      match elseExpr with
      | None ->
        let instrs =
          [ // if `cond` is `false`, assign a fake result of unit --
            // this will be ignored anyway
            RT.LoadVal(resultReg, RT.DUnit) ]
          @ cond.instructions
          @ jumpIfCondFalse (
            // go to the first instruction past the `if`
            // (the 1 is for the copy instruction)
            List.length thenInstrs.instructions + 1
          )
          @ thenInstrs.instructions
          @ copyThenToResultInstr

        { registerCount = thenInstrs.registerCount
          instructions = instrs
          resultIn = resultReg }

      | Some elseExpr ->
        let elseInstrs = toRT symbols thenInstrs.registerCount elseExpr
        let copyToResultInstr = [ RT.CopyVal(resultReg, elseInstrs.resultIn) ]

        let instrs =
          // cond -- if cond `false`, jump to start of 'else' block
          cond.instructions
          @ jumpIfCondFalse (
            // goto the first instruction past the `if`
            // (first 1 is for the copy instruction)
            // (second 1 is for the jump instruction)
            List.length thenInstrs.instructions + 1 + 1
          )

          // then
          @ thenInstrs.instructions
          @ copyThenToResultInstr
          @ [ RT.JumpBy(List.length elseInstrs.instructions + 1) ]

          // else
          @ elseInstrs.instructions
          @ copyToResultInstr

        { registerCount = elseInstrs.registerCount
          instructions = instrs
          resultIn = resultReg }

    | PT.EPipe(id, lhs, parts) ->
      // unwrap the first 'part' of the pipeline,
      // and punt the other work for later
      match parts with
      | [] -> toRT symbols rc lhs
      | first :: parts ->
        let newLHS =
          match first with
          // `1 |> fun x -> x + 1`
          | PT.EPipeLambda(id, pats, body) ->
            PT.EApply(id, PT.ELambda(id, pats, body), [], NEList.ofList lhs [])

          // `1 |> (+) 1`
          | PT.EPipeInfix(id, infix, rhs) -> PT.EInfix(id, infix, lhs, rhs)

          // `1 |> Json.serialize<Int64>`
          | PT.EPipeFnCall(id, fnName, typeArgs, args) ->
            PT.EApply(id, PT.EFnName(id, fnName), typeArgs, NEList.ofList lhs args)

          // `1 |> Option.Some`
          | PT.EPipeEnum(id, typeName, caseName, fields) ->
            let typeArgs = [] // TODO
            PT.EEnum(id, typeName, typeArgs, caseName, [ lhs ] @ fields)

          // `1 |> myLambda`
          | PT.EPipeVariable(id, varName, args) ->
            PT.EApply(id, PT.EVariable(id, varName), [], NEList.ofList lhs args)

        toRT symbols rc (PT.EPipe(id, newLHS, parts))

    | PT.EInfix(_, PT.BinOp op, left, right) ->
      let left = toRT symbols rc left
      let right = toRT symbols left.registerCount right

      let resultReg, rcAfterResult = right.registerCount, right.registerCount + 1

      let opInstr =
        match op with
        | PT.BinOpOr -> RT.Or(resultReg, left.resultIn, right.resultIn)
        | PT.BinOpAnd -> RT.And(resultReg, left.resultIn, right.resultIn)

      { registerCount = rcAfterResult
        instructions = left.instructions @ right.instructions @ [ opInstr ]
        resultIn = resultReg }



    | PT.EInfix(_, PT.InfixFnCall infix, left, right) ->
      let left = toRT symbols rc left
      let right = toRT symbols left.registerCount right

      let infixInstr, infixRc, rcAfterInfix =
        RT.LoadVal(
          right.registerCount,
          RT.AppNamedFn
            { name = InfixFnName.toFnName infix |> RT.FQFnName.Builtin
              typeSymbolTable = Map.empty
              typeArgs = []
              argsSoFar = [] }
          |> RT.DApplicable
        ),
        right.registerCount,
        right.registerCount + 1

      let resultReg, rcAfterResult = rcAfterInfix, rcAfterInfix + 1

      { registerCount = rcAfterResult
        instructions =
          left.instructions
          @ right.instructions
          @ [ infixInstr ]
          @ [ RT.Apply(
                resultReg,
                infixRc,
                [],
                NEList.ofList left.resultIn [ right.resultIn ]
              ) ]
        resultIn = resultReg }


    // constants
    | PT.EConstant(_, Ok name) ->
      { registerCount = rc + 1
        instructions = [ RT.LoadConstant(rc, FQConstantName.toRT name) ]
        resultIn = rc }

    | PT.EConstant(_, Error nre) ->
      // CLEANUP improve (see notes for EFnName)
      { registerCount = rc
        instructions = [ RT.RaiseNRE(NameResolutionError.toRT nre) ]
        resultIn = rc }


    // functions
    | PT.EFnName(_, Ok name) ->
      let namedFn : RT.ApplicableNamedFn =
        { name = FQFnName.toRT name
          typeSymbolTable = Map.empty
          typeArgs = []
          argsSoFar = [] }

      let applicable = RT.DApplicable(RT.AppNamedFn namedFn)

      { registerCount = rc + 1
        instructions = [ RT.LoadVal(rc, applicable) ]
        resultIn = rc }

    | PT.EFnName(_, Error nre) ->
      // CLEANUP make it ok to _reference_ a bad name, so long as we don't try to `apply` it.
      { registerCount = rc
        instructions = [ RT.RaiseNRE(NameResolutionError.toRT nre) ]
        resultIn = rc }


    | PT.EApply(_id, thingToApplyExpr, typeArgs, args) ->
      // process the arguments first, so we know how many registers we need
      let (rcAfterArgs, argInstrs, argRegs) =
        args
        |> NEList.fold
          (fun (rc, instrs, argResultRegs) arg ->
            let newInstrs = toRT symbols rc arg
            (newInstrs.registerCount,
             instrs @ newInstrs.instructions,
             argResultRegs @ [ newInstrs.resultIn ]))
          (rc, [], [])

      let thingToApply = toRT symbols rcAfterArgs thingToApplyExpr

      let putResultIn = thingToApply.registerCount

      let callInstr =
        RT.Apply(
          putResultIn,
          thingToApply.resultIn,
          List.map TypeReference.toRT typeArgs,
          NEList.ofListUnsafe "" [] argRegs
        )

      { registerCount = thingToApply.registerCount + 1
        instructions = argInstrs @ thingToApply.instructions @ [ callInstr ]
        resultIn = putResultIn }


    | PT.EMatch(_id, expr, cases) ->
      // Building a `match` expression is a bit more involved than other expressions.
      // We do this in multiple phases, and have a helper type to assist.

      // First, the easy part - compile the expression we're `match`ing against.
      let expr = toRT symbols rc expr

      // Shortly, we'll compile each of the cases.
      // We'll use this `resultReg` to store the final result of the match
      // , so we have a consistent place to look for it.
      // (similar to how we handle `EIf` -- refer to that for a simpler example)
      let resultReg, rcAfterResultIsReserved =
        expr.registerCount, expr.registerCount + 1

      // We compile each `case` in two phases, because some instrs require knowing
      // how many instrs to jump over, which we can't know until we know the basics
      // of all the cases.
      //
      // See `MatchCase.IntermediateValue` for more info.
      let casesAfterFirstPhase : List<MatchCase.IntermediateValue> =
        cases
        |> List.map (fun c ->
          let (pats, patSymbols, maxRc) =
            c.pat
            |> NEList.fold
              (fun (pats, allSymbols, maxRc) pat ->
                let (newPat, patSymbols, patRc) = MatchPattern.toRT Map.empty rc pat
                // Merge symbols to maintain consistent register assignments
                let mergedSymbols = Map.mergeFavoringRight allSymbols patSymbols
                // Take max register count as patterns may need different numbers of registers
                (pats @ [newPat], mergedSymbols, max maxRc patRc))
              ([], Map.empty, rcAfterResultIsReserved)

          let mergedSymbols = Map.mergeFavoringRight symbols patSymbols

          // compile the `when` condition, if it exists, as much as we can
          let rcAfterWhenCond, whenCondInstrs, whenCondJump =
            match c.whenCondition with
            | None -> (maxRc, [], None)
            | Some whenCond ->
              let whenCond = toRT mergedSymbols maxRc whenCond
              (whenCond.registerCount,
               whenCond.instructions,
               Some(fun jumpBy -> RT.JumpByIfFalse(jumpBy, whenCond.resultIn)))

          // compile the `rhs` of the case
          let rhs = toRT mergedSymbols rcAfterWhenCond c.rhs

          // return the intermediate results, as far along as they are
          { matchValueInstrFn =
              fun jumpByFail ->
                RT.CheckMatchPatternAndExtractVars(expr.resultIn, pats, jumpByFail)
            whenCondInstructions = whenCondInstrs
            whenCondJump = whenCondJump
            rhsInstrs = rhs.instructions @ [ RT.CopyVal(resultReg, rhs.resultIn) ]
            rc = rhs.registerCount })

      let countInstrsForCase (c : MatchCase.IntermediateValue) : int =
        1 // for the `MatchValue` instruction
        + List.length c.whenCondInstructions
        + (match c.whenCondJump with
           | Some _ -> 1
           | None -> 0)
        + List.length c.rhsInstrs
        + 1 // for the `JumpBy` instruction

      let (cases, _) : List<MatchCase.IntermediateValue * int> * int =
        casesAfterFirstPhase
        |> List.map (fun c -> (c, countInstrsForCase c))
        |> List.foldRight
          // CLEANUP this works, but hurts the brain a bit.
          (fun (acc, runningTotal) (c, instrCount) ->
            let newTotal = runningTotal + instrCount
            (acc @ [ c, runningTotal ], newTotal))
          ([], 0)
      let cases = List.rev cases

      let caseInstrs =
        cases
        |> List.fold
          (fun instrs (c, instrsAfterThisCaseUntilEndOfMatch) ->
            // note: `instrsAfterThisCaseUntilEndOfMatch` does not include
            // the final MatchUnmatched instruction

            let caseInstrs =
              [ c.matchValueInstrFn (
                  countInstrsForCase c
                  // because we can skip over the MatchValue instr
                  - 1
                ) ]
              @ c.whenCondInstructions
              @ (match c.whenCondJump with
                 // jump to next case if the when condition is false
                 | Some jump -> [ jump (List.length c.rhsInstrs + 1) ]
                 | None -> [])
              @ c.rhsInstrs
              @ [ RT.JumpBy(instrsAfterThisCaseUntilEndOfMatch + 1) ]

            instrs @ caseInstrs)
          []

      let instrs = expr.instructions @ caseInstrs @ [ RT.MatchUnmatched ]

      let rcAtEnd = casesAfterFirstPhase |> List.map _.rc |> List.max

      { registerCount = rcAtEnd; instructions = instrs; resultIn = resultReg }


    // -- Records --
    | PT.ERecord(_id, Error nre, _typeArgs, _fields) ->
      let returnReg = 0 // TODO - not sure what to do here
      { registerCount = rc
        instructions = [ RT.RaiseNRE(NameResolutionError.toRT nre) ]
        resultIn = returnReg }

    | PT.ERecord(_id, Ok typeName, typeArgs, fields) ->
      let recordReg, rc = rc, rc + 1

      // CLEANUP: complain if there are no fields
      // , or maybe that should happen during interpretation?
      // - actually- is there anything _wrong_ with a fieldless record?
      let (rcAfterFields, instrs, fields) =
        fields
        |> List.fold
          (fun (rc, instrs, fieldRegs) (fieldName, fieldExpr) ->
            let field = toRT symbols rc fieldExpr
            (field.registerCount,
             instrs @ field.instructions,
             fieldRegs @ [ (fieldName, field.resultIn) ]))
          (rc, [], [])

      { registerCount = rcAfterFields
        instructions =
          instrs
          @ [ RT.CreateRecord(
                recordReg,
                FQTypeName.toRT typeName,
                List.map TypeReference.toRT typeArgs,
                fields
              ) ]
        resultIn = recordReg }


    | PT.ERecordUpdate(_id, expr, updates) ->
      let expr = toRT symbols rc expr

      let (rcAfterUpdates, updatesInstrs, updates) =
        updates
        |> NEList.fold
          (fun (rc, instrs, regs) (fieldName, fieldExpr) ->
            let update = toRT symbols rc fieldExpr
            (update.registerCount,
             instrs @ update.instructions,
             regs @ [ (fieldName, update.resultIn) ]))
          (expr.registerCount, [], [])

      let targetReg, rc = rcAfterUpdates, rcAfterUpdates + 1
      let instrs =
        expr.instructions
        @ updatesInstrs
        @ [ RT.CloneRecordWithUpdates(targetReg, expr.resultIn, updates) ]

      { registerCount = rc; instructions = instrs; resultIn = targetReg }


    | PT.ERecordFieldAccess(_id, expr, fieldName) ->
      let expr = toRT symbols rc expr

      { registerCount = expr.registerCount + 1
        instructions =
          expr.instructions
          @ [ RT.GetRecordField(expr.registerCount, expr.resultIn, fieldName) ]
        resultIn = expr.registerCount }


    // -- Enums --
    | PT.EEnum(_id, Error nre, _caseName, _typeArgs, _fields) ->
      let returnReg = 0 // CLEANUP this is just to fill the field, but meh
      { registerCount = rc
        instructions = [ RT.RaiseNRE(NameResolutionError.toRT nre) ]
        resultIn = returnReg }

    | PT.EEnum(_id, Ok typeName, typeArgs, caseName, fields) ->
      let enumReg, rc = rc, rc + 1

      let (rcAfterFields, instrs, fields) =
        fields
        |> List.fold
          (fun (rc, instrs, fieldRegs) fieldExpr ->
            let afterField = toRT symbols rc fieldExpr
            (afterField.registerCount,
             instrs @ afterField.instructions,
             fieldRegs @ [ afterField.resultIn ]))
          (rc, [], [])

      { registerCount = rcAfterFields
        instructions =
          instrs
          @ [ RT.CreateEnum(
                enumReg,
                FQTypeName.toRT typeName,
                List.map TypeReference.toRT typeArgs,
                caseName,
                fields
              ) ]
        resultIn = enumReg }


    | PT.ELambda(id, pats, body) ->
      let symbolsUsedInBody = ProgramTypesAst.symbolsUsedInExpr body
      let symbolsUsedInPats =
        pats |> NEList.toList |> List.map PT.LetPattern.symbolsUsed |> Set.unionMany
      let symbolsUsedInBodyNotDefinedInPats =
        Set.difference symbolsUsedInBody symbolsUsedInPats

      let (pats, symbolsOfNewFrameAfterPats, rcOfNewFrameAfterPats)
        : (List<RT.LetPattern> * Map<string, int> * int) =
        pats
        |> NEList.toList
        |> List.fold
          (fun (pats, symbols, rc) p ->
            let (pat, newSymbols, rcAfterPat) = LetPattern.toRT symbols rc p
            (pats @ [ pat ], Map.mergeFavoringRight symbols newSymbols, rcAfterPat))
          ([], Map.empty, 0)

      let (registersToCloseOver,
           symbolsOfNewFrameAfterOnesOnlyUsedInBody,
           rcOfNewFrame) : (List<RT.Register * RT.Register> * Map<string, int> * int) =
        symbolsUsedInBodyNotDefinedInPats
        |> Set.toList
        |> List.fold
          (fun (regs, newSymbols, rc) name ->
            match Map.tryFind name symbols with
            | Some parentReg ->
              (regs @ [ parentReg, rc ], Map.add name rc newSymbols, rc + 1)
            | None -> (regs, newSymbols, rc)) // should we raise an error here? or should we just ignore it, and let the runtime raise an error?
          ([], symbolsOfNewFrameAfterPats, rcOfNewFrameAfterPats)

      let impl : RT.LambdaImpl =
        { exprId = id
          patterns = pats |> NEList.ofListUnsafe "" []
          registersToCloseOver = registersToCloseOver
          instructions =
            toRT symbolsOfNewFrameAfterOnesOnlyUsedInBody rcOfNewFrame body }

      { registerCount = rc + 1
        instructions = [ RT.CreateLambda(rc, impl) ]
        resultIn = rc }

    | PT.EStatement(_id, expr, next) ->
      let firstExpr = toRT symbols rc expr
      let nextExpr = toRT symbols firstExpr.registerCount next

      let checkIfFirstIsUnit = [ RT.CheckIfFirstExprIsUnit(firstExpr.resultIn) ]

      { registerCount = nextExpr.registerCount
        instructions =
          firstExpr.instructions @ checkIfFirstIsUnit @ nextExpr.instructions
        resultIn = nextExpr.resultIn }



module Const =
  let rec toRT (c : PT.Const) : RT.Const =
    match c with
    | PT.Const.CUnit -> RT.CUnit

    | PT.Const.CBool b -> RT.CBool b

    | PT.Const.CInt8 i -> RT.CInt8 i
    | PT.Const.CUInt8 i -> RT.CUInt8 i
    | PT.Const.CInt16 i -> RT.CInt16 i
    | PT.Const.CUInt16 i -> RT.CUInt16 i
    | PT.Const.CInt32 i -> RT.CInt32 i
    | PT.Const.CUInt32 i -> RT.CUInt32 i
    | PT.Const.CInt64 i -> RT.CInt64 i
    | PT.Const.CUInt64 i -> RT.CUInt64 i
    | PT.Const.CInt128 i -> RT.CInt128 i
    | PT.Const.CUInt128 i -> RT.CUInt128 i

    | PT.Const.CFloat(sign, w, f) -> RT.CFloat(sign, w, f)

    | PT.Const.CChar c -> RT.CChar c
    | PT.Const.CString s -> RT.CString s

    | PT.Const.CTuple(first, second, rest) ->
      RT.CTuple(toRT first, toRT second, List.map toRT rest)
    | PT.Const.CList items -> RT.CList(List.map toRT items)
    | PT.Const.CDict entries -> RT.CDict(entries |> List.map (Tuple2.mapSecond toRT))

    | PT.Const.CEnum(typeName, caseName, fields) ->
      RT.CEnum(
        NameResolution.toRT FQTypeName.toRT typeName,
        caseName,
        List.map toRT fields
      )


module TypeDeclaration =
  module RecordField =
    let toRT (f : PT.TypeDeclaration.RecordField) : RT.TypeDeclaration.RecordField =
      { name = f.name; typ = TypeReference.toRT f.typ }

  module EnumField =
    let toRT (f : PT.TypeDeclaration.EnumField) : RT.TypeReference =
      TypeReference.toRT f.typ

  module EnumCase =
    let toRT (c : PT.TypeDeclaration.EnumCase) : RT.TypeDeclaration.EnumCase =
      { name = c.name; fields = List.map EnumField.toRT c.fields }

  module Definition =
    let toRT (d : PT.TypeDeclaration.Definition) : RT.TypeDeclaration.Definition =
      match d with
      | PT.TypeDeclaration.Definition.Alias(typ) ->
        RT.TypeDeclaration.Alias(TypeReference.toRT typ)

      | PT.TypeDeclaration.Record fields ->
        RT.TypeDeclaration.Record(NEList.map RecordField.toRT fields)

      | PT.TypeDeclaration.Enum cases ->
        RT.TypeDeclaration.Enum(NEList.map EnumCase.toRT cases)

  let toRT (t : PT.TypeDeclaration.T) : RT.TypeDeclaration.T =
    { typeParams = t.typeParams; definition = Definition.toRT t.definition }


// --
// Package stuff
// --
module PackageType =
  let toRT (t : PT.PackageType.PackageType) : RT.PackageType.PackageType =
    { id = t.id; declaration = TypeDeclaration.toRT t.declaration }

module PackageConstant =
  let toRT
    (c : PT.PackageConstant.PackageConstant)
    : RT.PackageConstant.PackageConstant =
    { id = c.id; body = Const.toRT c.body }

module PackageFn =
  module Parameter =
    let toRT (p : PT.PackageFn.Parameter) : RT.PackageFn.Parameter =
      { name = p.name; typ = TypeReference.toRT p.typ }

  let toRT (f : PT.PackageFn.PackageFn) : RT.PackageFn.PackageFn =
    { id = f.id
      body =
        let (rcAfterParams, symbols) : (int * Map<string, int>) =
          f.parameters
          |> NEList.toList
          |> List.fold
            (fun (rc, symbols) p -> (rc + 1, Map.add p.name rc symbols))
            (0, Map.empty)

        Expr.toRT symbols rcAfterParams f.body
      typeParams = f.typeParams
      parameters = f.parameters |> NEList.map Parameter.toRT
      returnType = f.returnType |> TypeReference.toRT }


module PackageManager =
  let toRT (pm : PT.PackageManager) : RT.PackageManager =
    { getType = fun id -> pm.getType id |> Ply.map (Option.map PackageType.toRT)
      getConstant =
        fun id -> pm.getConstant id |> Ply.map (Option.map PackageConstant.toRT)
      getFn = fun id -> pm.getFn id |> Ply.map (Option.map PackageFn.toRT)

      init = pm.init }


// --
// User stuff
// --
module DB =
  let toRT (db : PT.DB.T) : RT.DB.T =
    { tlid = db.tlid
      name = db.name
      version = db.version
      typ = TypeReference.toRT db.typ }

module Secret =
  let toRT (s : PT.Secret.T) : RT.Secret.T =
    { name = s.name; value = s.value; version = s.version }


// TODO: remove this eventually -- PT2RT should happen generally at dev-time
// (in any case, params should be handled differntly - by index or something, not by name as loose symbols)
module Handler =
  let toRT (inputVars : Map<string, RT.Dval>) (expr : PT.Expr) : RT.Instructions =

    let (initialInstrs, rcAfterInputVars, symbols)
      : (List<RT.Instruction> * int * Map<string, int>) =
      inputVars
      |> Map.fold
        (fun (instrs, rc, symbols) inputVarName inputVarVal ->
          let instrs = instrs @ [ RT.LoadVal(rc, inputVarVal) ]
          let symbols = Map.add inputVarName rc symbols
          (instrs, rc + 1, Map.add inputVarName rc symbols))
        ([], 0, Map.empty)

    let exprInstrs = Expr.toRT symbols rcAfterInputVars expr

    { registerCount = exprInstrs.registerCount
      instructions = initialInstrs @ exprInstrs.instructions
      resultIn = exprInstrs.resultIn }
