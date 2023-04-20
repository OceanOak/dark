module LibExperimentalStdLib.LibExperiments

open System.Threading.Tasks
open FSharp.Control.Tasks

open Prelude
open Tablecloth
open LibExecution.RuntimeTypes

module Errors = LibExecution.Errors

let fn = FQFnName.stdlibFnName

let incorrectArgs = Errors.incorrectArgs

/// This makes extra careful that we're only accessing files where we expect to
/// find files, and that we're not checking outside these directories
///
/// Note: none of these are async because System.IO is not async
module RestrictedFileIO =
  type Mode =
    | Check
    | Dir
    | Read

  module Config =
    type Root = | BackendStatic

    let dir (root : Root) : string =
      match root with
      | BackendStatic -> "/home/dark/app/backend/static/"

  let checkFilename (root : Config.Root) (mode : Mode) (f : string) =
    let dir = Config.dir root
    let f : string = $"{dir}{f}"

    let debug (name : string) (value : bool) =
      if value then print $"checkFilename failed: {name}: {value}"
      value

    if (f.Contains ".." |> debug "dots"
        || f.Contains "~" |> debug "tilde"
        || f.EndsWith "." |> debug "ends dot"
        || (mode <> Dir && f.EndsWith "/") |> debug "ends slash"
        || (not (dir.EndsWith "/")) |> debug "dir no slash"
        || f.EndsWith "etc/passwd" |> debug "etc"
        // being used wrong
        || f.EndsWith "//" |> debug "double slash"
        // check for irregular file
        || (mode = Read
            && (System.IO.File.GetAttributes f <> System.IO.FileAttributes.Normal)
            && (System.IO.File.GetAttributes f <> System.IO.FileAttributes.ReadOnly))
           |> debug "irreg") then
      Exception.raiseInternal "FILE SECURITY VIOLATION" [ "file", f ]
    else
      f

  let fileExists root f : bool =
    let f = checkFilename root Check f
    System.IO.File.Exists f

  let readfile (root : Config.Root) (f : string) : string =
    f |> checkFilename root Read |> System.IO.File.ReadAllText

  let readfileBytes (root : Config.Root) (f : string) : byte [] =
    f |> checkFilename root Read |> System.IO.File.ReadAllBytes

  let tryReadFile (root : Config.Root) (f : string) : string option =
    if fileExists root f then
      f |> checkFilename root Read |> System.IO.File.ReadAllText |> Some
    else
      None


let fns : List<BuiltInFn> =
  [ { name = fn "Experiments" "parseAndExecuteExpr" 0
      typeParams = []
      parameters =
        [ Param.make "code" TString ""; Param.make "userInputs" (TDict TString) "" ]
      returnType = TResult(TString, TString)
      description =
        "Parses and executes arbitrary Dark code in the context of the current canvas."
      fn =
        // I had these tests in internal.tests for a bit, but that test runner
        // no longer has 'access' to the fn, so removed them. If we move them
        // out of 'LibExperimentalStdLib' then we can add them back in.

        //module ParseAndExecuteExpr =
        //  Experiments.parseAndExecuteExpr "1 + 2" Dict.empty_v0 = Ok "3"
        //  Experiments.parseAndExecuteExpr "a" { a = 3 } = Ok "3"
        //  Experiments.parseAndExecuteExpr """let a = 3 in a + b""" { b = 2 } = Ok "5"
        //  //Experiments.parseAndExecuteExpr """(HttpClient.request "get" "https://example.com" [] Bytes.empty) |> Test.unwrap |> (fun response -> response.statusCode)""" Dict.empty = 200

        function
        | state, _, [ DString code; DDict userInputs ] ->
          uply {
            // TODO: return an appropriate error if this fails
            let expr = Parser.RuntimeTypes.parseExprWithTypes Map.empty code

            let symtable = LibExecution.Interpreter.withGlobals state userInputs

            // TODO: return an appropriate error if this fails
            let! evalResult = LibExecution.Interpreter.eval state symtable expr

            return
              LibExecution.DvalReprDeveloper.toRepr evalResult
              |> DString
              |> Ok
              |> DResult
          }
        | _ -> incorrectArgs ()
      sqlSpec = NotQueryable
      previewable = Impure
      deprecated = NotDeprecated }


    { name = fn "Experiments" "parseAndSerializeExpr" 0
      typeParams = []
      parameters = [ Param.make "code" TString "" ]
      returnType = TResult(TString, TString)
      description = "Parses Dark code and serializes the result to JSON."
      fn =
        function
        | _, _, [ DString code ] ->
          uply {
            let expr = Parser.ProgramTypes.parseExprWithTypes Map.empty code
            let serializedExpr = Json.Vanilla.serialize expr
            return serializedExpr |> DString |> Ok |> DResult
          }
        | _ -> incorrectArgs ()
      sqlSpec = NotQueryable
      previewable = Impure
      deprecated = NotDeprecated }


    { name = fn "Experiments" "readFromStaticDir" 0
      typeParams = []
      parameters = [ Param.make "path" TString "" ]
      returnType = TResult(TBytes, TString)
      description =
        "Reads a file at backend/static/<param path>, and returns its contents as Bytes wrapped in a Result"
      fn =
        (function
        | _, _, [ DString path ] ->
          uply {
            try
              let contents =
                RestrictedFileIO.readfileBytes
                  RestrictedFileIO.Config.BackendStatic
                  path
              return DResult(Ok(DBytes contents))
            with
            | e -> return DResult(Error(DString($"Error reading file: {e.Message}")))
          }
        | _ -> incorrectArgs ())
      sqlSpec = NotQueryable
      previewable = Impure
      deprecated = NotDeprecated } ]
