﻿/// Interprets Dark expressions resulting in (tasks of) Dvals
module LibExecution.Interpreter

open System.Threading.Tasks
open FSharp.Control.Tasks
open FSharp.Control.Tasks.Affine.Unsafe

open Prelude
open RuntimeTypes

/// Gathers any global data (Secrets, DBs, etc.)
/// that may be needed to evaluate an expression
let globalsFor (state : ExecutionState) : Symtable =
  let secrets =
    state.program.secrets
    |> List.map (fun (s : Secret.T) -> (s.name, DString s.value))
    |> Map.ofList

  let dbs = Map.map (fun (db : DB.T) -> DDB db.name) state.program.dbs

  Map.mergeFavoringLeft secrets dbs


let withGlobals (state : ExecutionState) (symtable : Symtable) : Symtable =
  let globals = globalsFor state
  Map.mergeFavoringRight globals symtable

module Error =
  module RT2DT = RuntimeTypesToDarkTypes

  let typeName =
    TypeName.fqPackage
      "Darklang"
      [ "LanguageTools"; "RuntimeErrors"; "Execution" ]
      "Error"
      0


  let case (caseName : string) (fields : List<Dval>) : RuntimeError =
    DEnum(typeName, typeName, caseName, fields) |> RuntimeError.executionError

  let matchExprUnmatched (matchVal : Dval) : RuntimeError =
    case "MatchExprUnmatched" [ RT2DT.Dval.toDT matchVal ]

  let matchExprPatternWrongShape : RuntimeError =
    case "MatchExprPatternWrongShape" []

  let matchExprEnumPatternWrongCount
    (caseName : string)
    (expected : int)
    (actual : int)
    : RuntimeError =
    case
      "MatchExprEnumPatternWrongCount"
      [ DString caseName; DInt expected; DInt actual ]

  let incomplete : RuntimeError = case "Incomplete" []

type Matched = Result<bool, id * RuntimeError>

let combineMatched (a : Matched) (b : Matched) : Matched =
  match a, b with
  | Ok true, Ok true -> Ok true
  | Ok false, Ok false -> Ok false
  | Ok true, Ok false -> Ok false
  | Ok false, Ok true -> Ok false
  | Error _, _ -> a
  | _, Error _ -> b

let combineMatchedList (l : Matched list) : Matched =
  l |> List.fold combineMatched (Ok true)

let dincomplete (state : ExecutionState) (id : id) =
  DError(SourceID(state.tlid, id), Error.incomplete)

// fsharplint:disable FL0039

/// Interprets an expression and reduces to a Dark value
/// (or task that should result in such)
let rec eval'
  (state : ExecutionState)
  (tst : TypeSymbolTable)
  (st : Symtable)
  (e : Expr)
  : DvalTask =
  // Design doc for execution results and previews:
  // https://www.notion.so/darklang/Live-Value-Branching-44ee705af61e416abed90917e34da48e
  // TODO remove link from code or avail document - it is either gone or hidden behind login
  let sourceID id = SourceID(state.tlid, id)
  let errStr id msg = Dval.errSStr (sourceID id) msg
  let err id rte = DError(sourceID id, rte)

  /// This function ensures any value not on the execution path is evaluated.
  let preview (tst : TypeSymbolTable) (st : Symtable) (expr : Expr) : Ply<unit> =
    uply {
      if state.tracing.realOrPreview = Preview then
        let state = { state with onExecutionPath = false }
        let! (_result : Dval) = eval state tst st expr
        return ()
    }

  let typeResolutionError
    (errorType : NameResolutionError.ErrorType)
    (typeName : TypeName.TypeName)
    : Result<'a, RuntimeError> =
    let error : NameResolutionError.Error =
      { errorType = errorType
        nameType = NameResolutionError.Type
        names = [ TypeName.toString typeName ] }
    Error(NameResolutionError.RTE.toRuntimeError error)

  let recordMaybe
    (types : Types)
    (typeName : TypeName.TypeName)
    // TypeName, typeParam list, fully-resolved (except for typeParam) field list
    : Ply<Result<TypeName.TypeName * List<string> * List<string * TypeReference>, RuntimeError>> =
    let rec inner (typeName : TypeName.TypeName) =
      uply {
        match! Types.find typeName types with
        | Some({ typeParams = outerTypeParams
                 definition = TypeDeclaration.Alias(TCustomType(Ok(innerTypeName),
                                                                outerTypeArgs)) }) ->
          // Here we have found an alias, so we need to combine the type's
          // typeArgs with the aliased type's typeParams.
          // Eg in
          //   type Var = Result<Int, String>
          // we need to combine Var's typeArgs (<Int, String>) with Result's
          // typeParams (<`Ok, `Error>)
          //
          // To do this, we use typeArgs from the alias definition
          // (outerTypeArgs) and apply them to the aliased type
          // (innerTypeName)'s params (which are returned from the lookup and
          // used as innerTypeParams below).
          // Example: suppose we have
          //   type Outer<'a> = Inner<'a, Int>
          //   type Inner<'x, 'y> = { x : 'x; y : 'y }
          // The recursive search for Inner will get:
          //   innerTypeName = "Inner"
          //   innerTypeParams = ["x"; "y"]
          //   fields = [("x", TVar "x"); ("y", TVar "y")]
          // The Outer definition provides:
          //   outerTypeArgs = [TVar "a"; TInt]
          // We combine this with innerTypeParams to get:
          //   fields = [("x", TVar "a"); ("b", TInt)]
          //   outerTypeParams = ["a"]
          // So the effective result of this is:
          //   type Outer<'a> = { x : 'a; y : Int }
          let! next = inner innerTypeName
          return
            next
            |> Result.map (fun (innerTypeName, innerTypeParams, fields) ->
              (innerTypeName,
               outerTypeParams,
               fields
               |> List.map (fun (k, v) ->
                 (k, Types.substitute innerTypeParams outerTypeArgs v))))

        | Some({ definition = TypeDeclaration.Alias(TCustomType(Error e, _)) }) ->
          return Error e

        | Some({ typeParams = typeParams
                 definition = TypeDeclaration.Record fields }) ->
          return
            Ok(
              typeName,
              typeParams,
              fields |> NEList.toList |> List.map (fun f -> f.name, f.typ)
            )

        | Some({ definition = TypeDeclaration.Alias(_) })
        | Some({ definition = TypeDeclaration.Enum _ }) ->
          return
            typeResolutionError NameResolutionError.ExpectedRecordButNot typeName

        | None -> return typeResolutionError NameResolutionError.NotFound typeName
      }
    inner typeName

  let enumMaybe
    (types : Types)
    (typeName : TypeName.TypeName)
    : Ply<Result<TypeName.TypeName * List<string> * NEList<TypeDeclaration.EnumCase>, RuntimeError>> =
    let rec inner (typeName : TypeName.TypeName) =
      uply {
        match! Types.find typeName types with
        | Some({ typeParams = outerTypeParams
                 definition = TypeDeclaration.Alias(TCustomType(Ok(innerTypeName),
                                                                outerTypeArgs)) }) ->
          let! next = inner innerTypeName
          return
            next
            |> Result.map (fun (innerTypeName, innerTypeParams, cases) ->
              (innerTypeName,
               outerTypeParams,
               cases
               |> NEList.map (fun (c : TypeDeclaration.EnumCase) ->
                 { c with
                     fields =
                       List.map
                         (Types.substitute innerTypeParams outerTypeArgs)
                         c.fields })))

        | Some({ definition = TypeDeclaration.Alias(TCustomType(Error e, _)) }) ->
          return Error e

        | Some({ typeParams = typeParams; definition = TypeDeclaration.Enum cases }) ->
          return Ok(typeName, typeParams, cases)

        | Some({ definition = TypeDeclaration.Alias _ })
        | Some({ definition = TypeDeclaration.Record _ }) ->
          return typeResolutionError NameResolutionError.ExpectedEnumButNot typeName
        | None -> return typeResolutionError NameResolutionError.NotFound typeName
      }
    inner typeName


  uply {
    match e with
    | EString(id, segments) ->
      let! result =
        segments
        |> Ply.List.foldSequentially
          (fun builtUpString seg ->
            uply {
              match builtUpString, seg with
              | Ok str, StringText(text) -> return Ok(str + text)
              | Ok str, StringInterpolation(expr) ->
                let! result = eval state tst st expr
                match result with
                | DString s -> return Ok(str + s)
                | DError _ -> return Error(result)
                | dv ->
                  let msg = "Expected string, got " + DvalReprDeveloper.toRepr dv
                  return Error(errStr id msg)
              | Error dv, _ -> return Error dv
            })
          (Ok "")
      match result with
      | Ok str -> return DString(String.normalize str)
      | Error dv -> return dv
    | EBool(_id, b) -> return DBool b
    | EInt(_id, i) -> return DInt i
    | EFloat(_id, value) -> return DFloat value
    | EUnit _id -> return DUnit
    | EChar(_id, s) -> return DChar s
    | EConstant(id, name) ->
      match name with
      | FQName.UserProgram c ->
        match state.program.constants.TryFind c with
        | None -> return errStr id $"There is no user defined constant named: {name}"
        | Some constant -> return constant.body
      | FQName.BuiltIn c ->
        match state.builtIns.constants.TryFind c with
        | None -> return errStr id $"There is no builtin constant named: {name}"
        | Some constant -> return constant.body
      | FQName.Package c ->
        match! state.packageManager.getConstant c with
        | None -> return errStr id $"There is no package constant named: {name}"
        | Some constant -> return constant.body

    | ELet(id, pattern, rhs, body) ->
      /// Returns `incomplete` traces for subpatterns of an unmatched pattern
      let traceIncompleteWithArgs id argPatterns =
        let argTraces =
          argPatterns
          |> List.map LetPattern.toID
          |> List.map (fun pId -> (pId, dincomplete state pId))

        (id, dincomplete state id) :: argTraces

      /// Does the dval 'match' the given pattern?
      ///
      /// Returns:
      /// - whether or not the expr 'matches' the pattern
      /// - new vars (name * value)
      /// - traces
      let rec checkPattern
        (dv : Dval)
        (pattern : LetPattern)
        : bool * List<string * Dval> * List<id * Dval> =
        match pattern with

        | LPVariable(id, varName) ->
          not (Dval.isFake dv), [ (varName, dv) ], [ (id, dv) ]

        | LPUnit id -> dv = DUnit, [], [ (id, DUnit) ]

        | LPTuple(id, firstPat, secondPat, theRestPat) ->
          let allPatterns = firstPat :: secondPat :: theRestPat

          match dv with
          | DTuple(first, second, theRest) ->
            let allVals = first :: second :: theRest

            if List.length allVals = List.length allPatterns then
              let (passResults, newVarResults, traceResults) =
                List.zip allVals allPatterns
                |> List.map (fun (dv, pat) -> checkPattern dv pat)
                |> List.unzip3

              let allPass = passResults |> List.forall identity
              let allVars = newVarResults |> List.collect identity
              let allSubTraces = traceResults |> List.collect identity

              if allPass then
                true, allVars, (id, dv) :: allSubTraces
              else
                false, allVars, traceIncompleteWithArgs id allPatterns @ allSubTraces
            else
              false, [], traceIncompleteWithArgs id allPatterns
          | _ -> false, [], traceIncompleteWithArgs id allPatterns

      let! rhs = eval state tst st rhs
      let passes, newDefs, traces = checkPattern rhs pattern
      let newSymtable = Map.mergeFavoringRight st (Map.ofList newDefs)

      let traceDval onExecutionPath (id, dv) =
        state.tracing.traceDval onExecutionPath id dv

      match rhs with
      | DError _ ->
        List.iter (traceDval false) traces
        return rhs
      | _ ->
        if passes then
          List.iter (traceDval state.onExecutionPath) traces
        else
          List.iter (traceDval false) traces

        let! r = eval state tst newSymtable body
        return r

    | EList(_id, exprs) ->
      let! results = Ply.List.mapSequentially (eval state tst st) exprs

      match List.tryFind Dval.isFake results with
      | Some fakeDval -> return fakeDval
      | None -> return Dval.list valueTypeTODO results


    | ETuple(_id, first, second, theRest) ->

      let! firstResult = eval state tst st first
      let! secondResult = eval state tst st second
      let! otherResults = Ply.List.mapSequentially (eval state tst st) theRest

      let allResults = [ firstResult; secondResult ] @ otherResults

      // If any element in a tuple is fake (blank, error, etc.),
      // we don't want to return a tuple, but rather the fake val.
      match List.tryFind Dval.isFake allResults with
      | Some fakeDval -> return fakeDval
      | None -> return DTuple(firstResult, secondResult, otherResults)


    | EVariable(id, name) ->
      match st.TryFind name with
      | None -> return errStr id $"There is no variable named: {name}"
      | Some other -> return other


    | ERecord(id, typeName, fields) ->
      let typeStr = TypeName.toString typeName
      let types = ExecutionState.availableTypes state

      match! recordMaybe types typeName with
      | Error e -> return err id e
      | Ok(aliasTypeName, typeParams, expected) ->
        let expectedFields = Map expected
        let! result =
          fields
          |> NEList.toList
          |> Ply.List.foldSequentially
            (fun r (k, expr) ->
              uply {
                if Dval.isFake r then
                  do! preview tst st expr
                  return r
                else if not (Map.containsKey k expectedFields) then
                  do! preview tst st expr
                  return errStr id $"Unexpected field `{k}` in {typeStr}"
                else
                  let! v = eval state tst st expr
                  if Dval.isFake v then
                    return v
                  else
                    match r with
                    | DRecord(typeName, original, m) ->
                      if Map.containsKey k m then
                        return errStr id $"Duplicate field `{k}` in {typeStr}"
                      else
                        let fieldType = Map.find k expectedFields
                        let context =
                          TypeChecker.RecordField(original, k, fieldType, None)
                        let check =
                          TypeChecker.unify context types Map.empty fieldType v
                        match! check with
                        | Ok() -> return DRecord(typeName, original, Map.add k v m)
                        | Error e -> return err id e
                    | _ -> return errStr id "Expected a record in typecheck"
              })
            (DRecord(aliasTypeName, typeName, Map.empty)) // use the alias name here
        match result with
        | DRecord(_, _, fields) ->
          if Map.count fields = Map.count expectedFields then
            return result
          else
            let expectedKeys = Map.keys expectedFields
            let key = Seq.find (fun k -> not (Map.containsKey k fields)) expectedKeys
            return errStr id $"Missing field `{key}` in {typeStr}"
        | _ -> return result

    | ERecordUpdate(id, baseRecord, updates) ->
      let! baseRecord = eval state tst st baseRecord
      match baseRecord with
      | DRecord(typeName, _, _) ->
        let typeStr = TypeName.toString typeName
        let types = ExecutionState.availableTypes state
        match! recordMaybe types typeName with
        | Error e -> return err id e
        | Ok(_, _, expected) ->
          let expectedFields = Map expected
          return!
            updates
            |> NEList.toList
            |> Ply.List.foldSequentially
              (fun r (k, expr) ->
                uply {
                  let! v = eval state tst st expr
                  match r, k, v with
                  | r, _, _ when Dval.isFake r -> return r
                  | _, _, v when Dval.isFake v -> return v
                  | _, "", _ -> return errStr id $"Empty key for value `{v}`"
                  | _, _, _ when not (Map.containsKey k expectedFields) ->
                    return errStr id $"Unexpected field `{k}` in {typeStr}"
                  | DRecord(typeName, original, m), k, v ->
                    let fieldType = Map.find k expectedFields
                    let context =
                      TypeChecker.RecordField(typeName, k, fieldType, None)
                    match!
                      TypeChecker.unify context types Map.empty fieldType v
                    with
                    | Ok() -> return DRecord(typeName, original, Map.add k v m)
                    | Error rte -> return DError(SourceID(state.tlid, id), rte)
                  | _ ->
                    return
                      errStr id "Expected a record but {typeStr} is something else"
                })
              baseRecord
      | _ -> return errStr id "Expected a record in record update"

    | EDict(_, fields) ->
      return!
        Ply.List.foldSequentially
          (fun r (k, expr) ->
            uply {
              let! v = eval state tst st expr
              match (r, k, v) with
              | r, _, _ when Dval.isFake r -> return r
              | _, _, v when Dval.isFake v -> return v
              | DDict m, k, v -> return (DDict(Map.add k v m))
              // If we haven't got a DDict we're propagating an error so let it go
              | r, _, v -> return r
            })
          (DDict Map.empty)
          fields


    | EFnName(_id, name) -> return DFnVal(NamedFn name)

    | EApply(id, fnTarget, typeArgs, exprs) ->
      match! eval' state tst st fnTarget with
      | DFnVal fnVal ->
        let! args = Ply.NEList.mapSequentially (eval state tst st) exprs
        return! applyFnVal state id fnVal typeArgs args
      | other when Dval.isFake other -> return other
      | other ->
        return
          Dval.errSStr
            (SourceID(state.tlid, id))
            $"Expected a function value, got something else: {DvalReprDeveloper.toRepr other}"


    | EFieldAccess(id, e, field) ->
      let! obj = eval state tst st e

      if field = "" then
        return errStr id "Field name is empty"
      else
        match obj with
        | DRecord(_, typeName, o) ->
          match Map.tryFind field o with
          | Some v -> return v
          | None ->
            let typeStr = TypeName.toString typeName
            return errStr id $"No field named {field} in {typeStr} record"
        | DDB _ ->
          let msg =
            $"Attempting to access field '{field}' of a Datastore "
            + "(use `DB.*` standard library functions to interact with Datastores. "
            + "Field access only work with records)"
          return errStr id msg
        | _ when Dval.isFake obj -> return obj
        | _ ->
          let msg =
            $"Attempting to access field '{field}' of a "
            + $"{DvalReprDeveloper.toTypeName obj} (field access only works with records)"
          return errStr id msg


    | ELambda(_id, parameters, body) ->
      if state.tracing.realOrPreview = Preview then
        // In case this never gets executed, add default analysis results
        parameters
        |> NEList.iter (fun (id, _name) ->
          state.tracing.traceDval false id (dincomplete state id))

        // Since we return a DBlock, it's contents may never be
        // executed. So first we execute with no context to get some
        // live values.
        let previewST =
          parameters
          |> NEList.map (fun (id, name) -> (name, dincomplete state id))
          |> NEList.toList
          |> Map.ofList
        do! preview tst previewST body

      // It is the responsibility of wherever executes the DBlock to pass in
      // args and execute the body.
      return
        DFnVal(
          Lambda
            { typeSymbolTable = tst
              symtable = st
              parameters = parameters
              body = body }
        )


    | EMatch(id, matchExpr, cases) ->
      /// Returns `incomplete` traces for subpatterns of an unmatched pattern
      let traceIncompleteWithArgs id argPatterns =
        let argTraces =
          argPatterns
          |> List.map MatchPattern.toID
          |> List.map (fun pId -> (pId, dincomplete state pId))

        (id, dincomplete state id) :: argTraces

      /// Does the dval 'match' the given pattern?
      ///
      /// Returns:
      /// - whether or not the expr 'matches' the pattern
      /// - new vars (name * value)
      /// - traces
      let rec checkPattern
        (dv : Dval)
        (pattern : MatchPattern)
        : Matched * List<string * Dval> * List<id * Dval> =
        match pattern with
        // TODO these should all fail if they're the wrong type
        | MPInt(id, i) -> Ok(dv = DInt i), [], [ (id, DInt i) ]
        | MPBool(id, b) -> Ok(dv = DBool b), [], [ (id, DBool b) ]
        | MPChar(id, c) -> Ok(dv = DChar c), [], [ (id, DChar c) ]
        | MPString(id, s) -> Ok(dv = DString s), [], [ (id, DString s) ]
        | MPFloat(id, f) -> Ok(dv = DFloat f), [], [ (id, DFloat f) ]
        | MPUnit(id) -> Ok(dv = DUnit), [], [ (id, DUnit) ]

        | MPVariable(id, varName) -> Ok true, [ (varName, dv) ], [ (id, dv) ]


        | MPEnum(id, caseName, fieldPats) ->
          let traceFields () =
            let (newVarResults, traceResults) =
              fieldPats
              |> List.map (fun fp ->
                let pID = MatchPattern.toID fp
                let (_, newVars, traces) = checkPattern (dincomplete state pID) fp
                newVars, (id, dincomplete state id) :: traces)
              |> List.unzip
            let allVars = newVarResults |> List.collect identity
            let allSubTraces = traceResults |> List.collect identity
            (allVars, (id, dincomplete state id) :: allSubTraces)

          match dv with
          | DEnum(_dTypeName, _oTypeName, dCaseName, dFields) ->
            if caseName <> dCaseName then
              let (allVars, allSubTraces) = traceFields ()
              Ok false, allVars, allSubTraces
            else if List.length dFields <> List.length fieldPats then
              let (allVars, allSubTraces) = traceFields ()
              let err =
                Error.matchExprEnumPatternWrongCount
                  dCaseName
                  (List.length fieldPats)
                  (List.length dFields)
              Error(id, err), allVars, allSubTraces
            else
              let (passResults, newVarResults, traceResults) =
                List.zip dFields fieldPats
                |> List.map (fun (dv, pat) -> checkPattern dv pat)
                |> List.unzip3

              let allPass = combineMatchedList passResults
              let allVars = newVarResults |> List.collect identity
              let allSubTraces = traceResults |> List.collect identity

              match allPass with
              | Ok true -> Ok true, allVars, (id, dv) :: allSubTraces
              | Ok false
              | Error _ ->
                allPass, allVars, (id, dincomplete state id) :: allSubTraces

          | _dv ->
            let (allVars, allSubTraces) = traceFields ()
            Error(id, Error.matchExprPatternWrongShape), allVars, allSubTraces


        | MPTuple(id, firstPat, secondPat, theRestPat) ->
          let allPatterns = firstPat :: secondPat :: theRestPat

          // TODO type error if not tuple
          match dv with
          | DTuple(first, second, theRest) ->
            let allVals = first :: second :: theRest

            // TODO type error if the wrong size
            if List.length allVals = List.length allPatterns then
              let (passResults, newVarResults, traceResults) =
                List.zip allVals allPatterns
                |> List.map (fun (dv, pat) -> checkPattern dv pat)
                |> List.unzip3

              let allPass = combineMatchedList passResults
              let allVars = newVarResults |> List.collect identity
              let allSubTraces = traceResults |> List.collect identity

              match allPass with
              | Ok true -> Ok true, allVars, (id, dv) :: allSubTraces
              | Ok false
              | Error _ ->
                allPass,
                allVars,
                traceIncompleteWithArgs id allPatterns @ allSubTraces
            else
              Ok false, [], traceIncompleteWithArgs id allPatterns
          | _ -> Ok false, [], traceIncompleteWithArgs id allPatterns


        | MPListCons(id, headPat, tailPat) ->
          match dv with
          | DList(vt, headVal :: tailVals) ->
            let (headPass, headVars, headTraces) = checkPattern headVal headPat
            let (tailPass, tailVars, tailTraces) =
              checkPattern (Dval.list vt tailVals) tailPat

            let allSubVars = headVars @ tailVars
            let allSubTraces = headTraces @ tailTraces
            let pass = combineMatched headPass tailPass
            match pass with
            | Ok true -> Ok true, allSubVars, (id, dv) :: allSubTraces
            | Ok false
            | Error _ ->
              pass, allSubVars, traceIncompleteWithArgs id [ headPat; tailPat ]

          // TODO if not a list, error
          | _ -> Ok false, [], traceIncompleteWithArgs id [ headPat; tailPat ]

        | MPList(id, pats) ->
          match dv with
          | DList(_, vals) ->
            if List.length vals = List.length pats then
              let (passResults, newVarResults, traceResults) =
                List.zip vals pats
                |> List.map (fun (dv, pat) -> checkPattern dv pat)
                |> List.unzip3

              let allPass = combineMatchedList passResults
              let allVars = newVarResults |> List.collect identity
              let allSubTraces = traceResults |> List.collect identity

              match allPass with
              | Ok true -> Ok true, allVars, (id, dv) :: allSubTraces
              | Ok false
              | Error _ ->
                allPass, allVars, traceIncompleteWithArgs id pats @ allSubTraces
            else
              Ok false, [], traceIncompleteWithArgs id pats
          // TODO if not a list, error
          | _ -> Ok false, [], traceIncompleteWithArgs id pats

      // This is to avoid checking `state.tracing.realOrPreview = Real` below.
      // If RealOrPreview gets additional branches, reconsider what to do here.
      let isRealExecution =
        match state.tracing.realOrPreview with
        | Real -> true
        | Preview -> false

      // The value we're matching against
      let! matchVal = eval state tst st matchExpr

      let mutable matchResult =
        // Even though we know it's fakeval, we still run through each pattern for analysis
        if Dval.isFake matchVal then Some matchVal else None


      for (pattern, rhsExpr) in NEList.toList cases do
        if Option.isSome matchResult && isRealExecution then
          ()
        else
          let passes, newDefs, traces = checkPattern matchVal pattern
          let newSymtable = Map.mergeFavoringRight st (Map.ofList newDefs)

          if matchResult = None && passes = Ok true then
            traces
            |> List.iter (fun (id, dv) ->
              state.tracing.traceDval state.onExecutionPath id dv)

            let! r = eval state tst newSymtable rhsExpr
            matchResult <- Some r
          // "Real" evaluations don't need to persist non-matched traces
          else if isRealExecution then
            match passes with
            | Error(id, err) -> matchResult <- Some(DError(sourceID id, err))
            | Ok _ -> ()
          else
            // If we're "previewing" (analysis), persist traces for all patterns
            traces |> List.iter (fun (id, dv) -> state.tracing.traceDval false id dv)
            do! preview tst newSymtable rhsExpr

      match matchResult with
      | Some r -> return r
      | None -> return DError(sourceID id, Error.matchExprUnmatched matchVal)


    | EIf(id, cond, thenBody, elseBody) ->
      match! eval state tst st cond with
      | DBool false ->
        do! preview tst st thenBody
        match elseBody with
        | None -> return DUnit
        | Some eb -> return! eval state tst st eb
      | DBool true ->
        let! result = eval state tst st thenBody
        match elseBody with
        | None -> ()
        | Some eb -> do! preview tst st eb
        return result
      | cond when Dval.isFake cond ->
        do! preview tst st thenBody
        match elseBody with
        | None -> ()
        | Some eb -> do! preview tst st eb
        return cond
      | _ ->
        do! preview tst st thenBody
        match elseBody with
        | None -> ()
        | Some eb -> do! preview tst st eb
        return errStr id "If only supports Booleans"


    | EOr(id, left, right) ->
      match! eval state tst st left with
      | DBool true ->
        do! preview tst st right
        return DBool true
      | DBool false ->
        match! eval state tst st right with
        | DBool true -> return DBool true
        | DBool false -> return DBool false
        | right when Dval.isFake right -> return right
        | _ -> return errStr id "|| only supports Booleans"
      | left when Dval.isFake left ->
        do! preview tst st right
        return left
      | _ ->
        do! preview tst st right
        return errStr id "|| only supports Booleans"


    | EAnd(id, left, right) ->
      match! eval state tst st left with
      | DBool false ->
        do! preview tst st right
        return DBool false
      | DBool true ->
        match! eval state tst st right with
        | DBool true -> return DBool true
        | DBool false -> return DBool false
        | right when Dval.isFake right -> return right
        | _ -> return errStr id "&& only supports Booleans"
      | left when Dval.isFake left ->
        do! preview tst st right
        return left
      | _ ->
        do! preview tst st right
        return errStr id "&& only supports Booleans"


    | EEnum(id, typeName, caseName, fields) ->
      let typeStr = TypeName.toString typeName
      let types = ExecutionState.availableTypes state

      match! enumMaybe types typeName with
      | Error e -> return err id e
      | Ok(aliasTypeName, _, cases) ->
        let case = cases |> NEList.find (fun c -> c.name = caseName)
        match case with
        | None ->
          return errStr id $"There is no case named `{caseName}` in {typeStr}"
        | Some case ->
          if case.fields.Length <> fields.Length then
            let msg =
              $"Case `{caseName}` expected {case.fields.Length} fields but got {fields.Length}"
            return errStr id msg
          else
            let fields = List.zip case.fields fields
            return!
              Ply.List.foldSequentiallyWithIndex
                (fun i r ((enumFieldType : TypeReference), expr) ->
                  uply {
                    if Dval.isFake r then
                      do! preview tst st expr
                      return r
                    else
                      let! v = eval state tst st expr
                      if Dval.isFake v then
                        return v
                      else
                        let context =
                          TypeChecker.EnumField(
                            typeName,
                            case.name,
                            i,
                            List.length fields,
                            enumFieldType,
                            None
                          )

                        match!
                          TypeChecker.unify context types Map.empty enumFieldType v
                        with
                        | Ok() ->
                          match r with
                          | DEnum(typeName, original, caseName, existing) ->
                            return
                              DEnum(
                                typeName,
                                original,
                                caseName,
                                List.append existing [ v ]
                              )
                          | _ -> return errStr id "Expected an enum"
                        | Error rte -> return DError(SourceID(state.tlid, id), rte)
                  })
                (DEnum(aliasTypeName, typeName, caseName, []))
                fields
    | EError(id, rte, exprs) ->
      let! args = Ply.List.mapSequentially (eval state tst st) exprs

      return
        args
        |> List.tryFind Dval.isFake
        |> Option.defaultValue (DError(sourceID id, rte))
  }

/// Interprets an expression and reduces to a Dark value
/// (or a task that results in a dval)
and eval
  (state : ExecutionState)
  (tst : TypeSymbolTable)
  (st : Symtable)
  (e : Expr)
  : DvalTask =
  uply {
    let! (result : Dval) = eval' state tst st e
    state.tracing.traceDval state.onExecutionPath (Expr.toID e) result
    return result
  }

/// Unwrap the dval, which we expect to be a function, and error if it's not
and applyFnVal
  (state : ExecutionState)
  (id : id)
  (fnVal : FnValImpl)
  (typeArgs : List<TypeReference>)
  (args : NEList<Dval>)
  : DvalTask =
  match fnVal with
  | Lambda l -> executeLambda state l args
  | NamedFn fn ->
    // I think we'll end up having to pass the
    // `tst` in scope here at some point?
    let tst = Map.empty
    callFn state tst id fn typeArgs args

and executeLambda
  (state : ExecutionState)
  (l : LambdaImpl)
  (args : NEList<Dval>)
  : DvalTask =

  // If one of the args is fake value used as a marker, return it instead of
  // executing. This is the same behaviour as in fn calls.
  let firstMarker = NEList.find Dval.isFake args

  match firstMarker with
  | Some dv -> Ply dv
  | None ->
    let parameters = NEList.map snd l.parameters
    // One of the reasons to take a separate list of params and args is to
    // provide this error message here. We don't have this information in
    // other places, and the alternative is just to provide incompletes
    // with no context
    let expectedLength = NEList.length l.parameters
    let actualLength = NEList.length args
    if expectedLength <> actualLength then
      Ply(
        DError(
          SourceNone,
          RuntimeError.oldError
            $"Expected {expectedLength} arguments, got {actualLength}"
        )
      )
    else
      NEList.iter
        (fun ((id, _), dv) -> state.tracing.traceDval state.onExecutionPath id dv)
        (NEList.zip l.parameters args)

      let paramSyms = NEList.zip parameters args |> NEList.toList |> Map
      // paramSyms is higher priority

      let newSymtable = Map.mergeFavoringRight l.symtable paramSyms

      eval state l.typeSymbolTable newSymtable l.body

and callFn
  (state : ExecutionState)
  (tst : TypeSymbolTable)
  (callerID : id)
  (desc : FnName.FnName)
  (typeArgs : List<TypeReference>)
  (args : NEList<Dval>)
  : DvalTask =
  uply {
    let sourceID = SourceID(state.tlid, callerID)
    let handleMissingFunction () : Dval =
      // Functions which aren't implemented in the client may have results
      // available, otherwise they just return incomplete.
      let fnRecord = (state.tlid, desc, callerID)
      let fnResult = state.tracing.loadFnResult fnRecord args

      // TODO: in an old version, we executed the lambda with a fake value to
      // give enough livevalues for the editor to autocomplete. It may be worth
      // doing this again
      match fnResult with
      | Some(result, _ts) -> result
      | None ->
        DError(
          sourceID,
          RuntimeError.oldError $"Function {FnName.toString desc} is not found"
        )

    let checkArgsLength fn : Result<unit, string> =
      let expectedTypeParamLength = List.length fn.typeParams
      let expectedArgLength = NEList.length fn.parameters

      let actualTypeArgLength = List.length typeArgs
      let actualArgLength = NEList.length args

      if
        expectedTypeParamLength = actualTypeArgLength
        && expectedArgLength = actualArgLength
      then
        Ok()
      else
        Error(
          $"{FnName.toString desc} has {expectedTypeParamLength} type parameters and {expectedArgLength} parameters, "
          + $"but here was called with {actualTypeArgLength} type arguments and {actualArgLength} arguments."
        )


    match NEList.find Dval.isFake args with
    | Some fakeArg -> return fakeArg
    | None ->
      let! fn =
        match desc with
        | FQName.BuiltIn std ->
          state.builtIns.fns.TryFind std |> Option.map builtInFnToFn |> Ply
        | FQName.UserProgram u ->
          state.program.fns.TryFind u |> Option.map userFnToFn |> Ply
        | FQName.Package pkg ->
          uply {
            let! fn = state.packageManager.getFn pkg
            return Option.map packageFnToFn fn
          }

      match fn with
      | None -> return handleMissingFunction ()
      | Some fn ->
        match checkArgsLength fn with
        | Error errMsg -> return DError(sourceID, RuntimeError.oldError errMsg)
        | Ok() ->
          let newlyBoundTypeArgs = List.zip fn.typeParams typeArgs |> Map
          let updatedTypeSymbolTable = Map.mergeFavoringRight tst newlyBoundTypeArgs
          return! execFn state updatedTypeSymbolTable desc callerID fn typeArgs args
  }



and execFn
  (state : ExecutionState)
  (tst : TypeSymbolTable)
  (fnDesc : FnName.FnName)
  (id : id)
  (fn : Fn)
  (typeArgs : List<TypeReference>)
  (args : NEList<Dval>)
  : DvalTask =
  uply {
    let sourceID = SourceID(state.tlid, id) in

    if
      state.tracing.realOrPreview = Preview
      && not state.onExecutionPath
      && Set.contains fnDesc state.callstack
    then
      // Don't recurse (including transitively!) when previewing unexecuted paths
      // in the editor. If we do, we'll recurse forever and blow the stack.
      return dincomplete state id
    else
      // CLEANUP: optimization opportunity
      let state =
        { state with
            executingFnName = Some fnDesc
            callstack = Set.add fnDesc state.callstack }

      let fnRecord = (state.tlid, fnDesc, id) in

      let types = ExecutionState.availableTypes state

      let typeArgsResolvedInFn = List.zip fn.typeParams typeArgs |> Map
      let typeSymbolTable = Map.mergeFavoringRight tst typeArgsResolvedInFn

      match! TypeChecker.checkFunctionCall types typeSymbolTable fn args with
      | Error rte -> return DError(sourceID, rte)
      | Ok() ->

        let! result =
          match fn.fn with
          | BuiltInFunction f ->
            if state.tracing.realOrPreview = Preview && fn.previewable <> Pure then
              match state.tracing.loadFnResult fnRecord args with
              | Some(result, _ts) -> Ply result
              | None -> Ply(dincomplete state id)
            else
              uply {
                let! result =
                  uply {
                    try
                      return! f (state, typeArgs, NEList.toList args)
                    with e ->
                      let context : Metadata =
                        [ "fn", fnDesc
                          "args", args
                          "typeArgs", typeArgs
                          "id", id ]
                      match e with
                      | UncaughtRuntimeError rte -> return DError(sourceID, rte)
                      | Errors.IncorrectArgs ->
                        return Errors.incorrectArgsToDError sourceID fn args
                      | Errors.FakeDvalFound dv -> return dv
                      | (:? Exception.CodeException) as e ->
                        // There errors are created by us, within the libraries, so they are
                        // safe to show to users (but not grandusers)
                        return Dval.errSStr sourceID e.Message
                      | e ->
                        // TODO could we show the user the execution id here?
                        state.reportException state context e
                        // These are arbitrary errors, and could include sensitive
                        // information, so best not to show it to the user. If we'd
                        // like to show it to the user, we should catch it and give
                        // them a known safe error.

                        return Dval.errSStr sourceID Exception.unknownErrorMessage
                  }

                // there's no point storing data we'll never ask for
                if fn.previewable <> Pure then
                  state.tracing.storeFnResult fnRecord args result

                return result
              }

          | PackageFunction(tlid, body)
          | UserProgramFunction(tlid, body) ->
            state.tracing.traceTLID tlid
            let state = { state with tlid = tlid }
            let symTable =
              fn.parameters // Lengths are checked in checkFunctionCall
              |> NEList.map2 (fun dv p -> (p.name, dv)) args
              |> Map.ofNEList
              |> withGlobals state
            eval state typeSymbolTable symTable body

        if Dval.isFake result then
          return result
        else
          match!
            TypeChecker.checkFunctionReturnType types typeSymbolTable fn result
          with
          | Error rte -> return DError(sourceID, rte)
          | Ok() -> return result
  }
