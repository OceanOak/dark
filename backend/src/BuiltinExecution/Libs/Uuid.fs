module BuiltinExecution.Libs.Uuid

open LibExecution.RuntimeTypes
open Prelude
open LibExecution.Builtin.Shortcuts
module VT = LibExecution.ValueType
module Dval = LibExecution.Dval
module PackageHashes = LibExecution.PackageHashes


module ParseError =
  type ParseError = | BadFormat

  let toDT (e : ParseError) : Dval =
    let (caseName, fields) =
      match e with
      | BadFormat -> "BadFormat", []

    let typeName = FQTypeName.fqPackage PackageHashes.Type.Stdlib.uuidParseError
    DEnum(typeName, typeName, [], caseName, fields)


let fns : List<BuiltInFn> =
  [ { name = fn "uuidGenerate" 0
      typeParams = []
      parameters = [ Param.make "unit" TUnit "" ]
      returnType = TUuid
      description = "Generate a new <type Uuid> v4 according to RFC 4122"
      fn =
        (function
        | _, _, _, [ DUnit ] -> Ply(DUuid(System.Guid.NewGuid()))
        | _ -> incorrectArgs ())
      sqlSpec = NotYetImplemented
      previewable =
        // Similarly to DateTime.now, it's not particularly fun for this to change
        // when live programming, so let's keep this as Impure rather than ImpurePreviewable
        Impure
      deprecated = NotDeprecated }


    { name = fn "uuidParse" 0
      typeParams = []
      parameters = [ Param.make "uuid" TString "" ]
      returnType =
        TypeReference.result
          TUuid
          (TCustomType(
            Ok(FQTypeName.fqPackage PackageHashes.Type.Stdlib.uuidParseError),
            []
          ))
      description =
        "Parse a <type Uuid> of form {{XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX}}"
      fn =
        let typeName = FQTypeName.fqPackage PackageHashes.Type.Stdlib.uuidParseError
        let resultOk = Dval.resultOk KTUuid (KTCustomType(typeName, []))
        let resultError = Dval.resultError KTUuid (KTCustomType(typeName, []))
        (function
        | _, _, _, [ DString s ] ->
          match System.Guid.TryParse s with
          | true, x -> x |> DUuid |> resultOk |> Ply
          | _ -> ParseError.BadFormat |> ParseError.toDT |> resultError |> Ply
        | _ -> incorrectArgs ())
      sqlSpec = NotYetImplemented
      previewable = Pure
      deprecated = NotDeprecated }


    { name = fn "uuidToString" 0
      typeParams = []
      parameters = [ Param.make "uuid" TUuid "" ]
      returnType = TString
      description =
        "Stringify <param uuid> to the format XXXXXXXX-XXXX-XXXX-XXXX-XXXXXXXXXXXX"
      fn =
        (function
        | _, _, _, [ DUuid uuid ] -> Ply(DString(string uuid))
        | _ -> incorrectArgs ())
      sqlSpec = NotQueryable
      previewable = Pure
      deprecated = NotDeprecated } ]


let builtins = LibExecution.Builtin.make [] fns
