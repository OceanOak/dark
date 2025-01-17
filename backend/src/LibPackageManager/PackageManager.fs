module LibPackageManager.PackageManager

open System.Threading.Tasks
open FSharp.Control.Tasks
open System.Collections.Concurrent

open Prelude
open Microsoft.Data.Sqlite

module RT = LibExecution.RuntimeTypes
module PT = LibExecution.ProgramTypes
module PT2RT = LibExecution.ProgramTypesToRuntimeTypes
module BinarySerialization = LibBinarySerialization.BinarySerialization

open LibPackageManager.Types

type RTCacheType =
  | RTFunction of RT.PackageFn.PackageFn
  | RTType of RT.PackageType.PackageType
  | RTConstant of RT.PackageConstant.PackageConstant

type PTCacheType =
  | PTFunction of PT.PackageFn.PackageFn
  | PTType of PT.PackageType.PackageType
  | PTConstant of PT.PackageConstant.PackageConstant

module PackageCache =
  let private rtCache = ConcurrentDictionary<uuid, RTCacheType>()
  let private ptCache = ConcurrentDictionary<uuid, PTCacheType>()

  let private getAllDefinitions (kind: string) : Ply<(uuid * byte[]) list> =
    uply {
      use connection = new SqliteConnection("Data Source=sql-db/test.db")
      do! connection.OpenAsync()
      use command = connection.CreateCommand()
      command.CommandText <- $"SELECT id, definition FROM {kind}"

      use! reader = command.ExecuteReaderAsync()
      let mutable results = []
      while! reader.ReadAsync() do
        let id = reader.GetGuid(0)
        let bytes = reader.GetFieldValue<byte[]>(1)
        results <- (id, bytes) :: results
      return results
    }

  let prefillCache () =
    debuG "PackageCache.prefillCache called" ()
    // debuG "PackageCache.prefillCache called from" (System.Environment.StackTrace)
    uply {
      let! types = getAllDefinitions "package_types_v0"
      let! fns = getAllDefinitions "package_functions_v0"
      let! constants = getAllDefinitions "package_constants_v0"

      for (id, bytes) in types do
        try
          let t = BinarySerialization.PackageType.deserialize id bytes
          let rt = t |> PT2RT.PackageType.toRT
          rtCache.TryAdd(id, RTType rt) |> ignore<bool>
          ptCache.TryAdd(id, PTType t) |> ignore<bool>
        with _ -> ()

      for (id, bytes) in fns do
        try
          let fn = BinarySerialization.PackageFn.deserialize id bytes
          let rt = fn |> PT2RT.PackageFn.toRT
          rtCache.TryAdd(id, RTFunction rt) |> ignore<bool>
          ptCache.TryAdd(id, PTFunction fn) |> ignore<bool>
        with _ -> ()

      for (id, bytes) in constants do
        try
          let c = BinarySerialization.PackageConstant.deserialize id bytes
          let rt = c |> PT2RT.PackageConstant.toRT
          rtCache.TryAdd(id, RTConstant rt) |> ignore<bool>
          ptCache.TryAdd(id, PTConstant c) |> ignore<bool>
        with _ -> ()

      return ()
    }


  let getRTType (id: uuid) : Ply<Option<RT.PackageType.PackageType>> =
    uply {
      match rtCache.TryGetValue(id) with
      | true, RTType t -> return Some t
      | _ -> return None
    }

  let getRTFn (id: uuid) : Ply<Option<RT.PackageFn.PackageFn>> =
    uply {
      match rtCache.TryGetValue(id) with
      | true, RTFunction f -> return Some f
      | _ -> return None
    }

  let getRTConstant (id: uuid) : Ply<Option<RT.PackageConstant.PackageConstant>> =
    uply {
      match rtCache.TryGetValue(id) with
      | true, RTConstant c -> return Some c
      | _ -> return None
    }

  let getPTType (id: uuid) : Ply<Option<PT.PackageType.PackageType>> =
    uply {
      match ptCache.TryGetValue(id) with
      | true, PTType t -> return Some t
      | _ -> return None
    }

  let getPTFn (id: uuid) : Ply<Option<PT.PackageFn.PackageFn>> =
    uply {
      match ptCache.TryGetValue(id) with
      | true, PTFunction f -> return Some f
      | _ -> return None
    }

  let getPTConstant (id: uuid) : Ply<Option<PT.PackageConstant.PackageConstant>> =
    uply {
      match ptCache.TryGetValue(id) with
      | true, PTConstant c -> return Some c
      | _ -> return None
    }

let findByName
  (dbPath: string)
  (kind: string)
  (owner: string)
  (modules: List<string>)
  (name: string)
  : Ply<Option<uuid>> =
  uply {
    use connection = new SqliteConnection($"Data Source={dbPath}")
    do! connection.OpenAsync()
    use command = connection.CreateCommand()

    let modulesStr = String.concat "." modules

    command.CommandText <- $"SELECT id FROM {kind} WHERE name = @name AND owner = @owner AND modules = @modules"
    command.Parameters.AddWithValue("@name", name) |> ignore<SqliteParameter>
    command.Parameters.AddWithValue("@owner", owner) |> ignore<SqliteParameter>
    command.Parameters.AddWithValue("@modules", modulesStr) |> ignore<SqliteParameter>

    use! reader = command.ExecuteReaderAsync()
    let! hasRow = reader.ReadAsync()

    if hasRow then
      let id = reader.GetGuid(0)
      return Some id
    else
      return None
  }

let rt (_baseUrl : string) : RT.PackageManager =
  {
    getType = PackageCache.getRTType
    getFn = PackageCache.getRTFn
    getConstant = PackageCache.getRTConstant
    init = uply { do! PackageCache.prefillCache() }
  }

let pt (_baseUrl : string) : PT.PackageManager =
  {
    findType =
      (fun (name : PT.PackageType.Name) ->
        findByName "sql-db/test.db" "package_types_v0" name.owner name.modules name.name)

    findConstant =
      (fun (name : PT.PackageConstant.Name) ->
        findByName "sql-db/test.db" "package_constants_v0" name.owner name.modules name.name)

    findFn =
      (fun (name : PT.PackageFn.Name) ->
        findByName "sql-db/test.db" "package_functions_v0" name.owner name.modules name.name)

    getType = PackageCache.getPTType
    getFn = PackageCache.getPTFn
    getConstant = PackageCache.getPTConstant

    getAllFnNames =
      (fun () ->
        uply {
          let dbPath = "sql-db/test.db"
          use connection = new SqliteConnection($"Data Source={dbPath}")
          do! connection.OpenAsync()
          use command = connection.CreateCommand()
          command.CommandText <- "SELECT id FROM package_functions_v0"

          use! reader = command.ExecuteReaderAsync()
          let mutable results = []
          while! reader.ReadAsync() do
            results <- reader.GetString(0) :: results

          return results
        })

    init = uply { return () }
  }
