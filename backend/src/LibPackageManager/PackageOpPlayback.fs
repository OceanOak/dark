/// Applies PackageOps to the DB projection tables.
/// These tables (package_types, package_values, package_functions, locations) are projections
/// of the source-of-truth package_ops table.
module LibPackageManager.PackageOpPlayback


open System.Threading.Tasks
open FSharp.Control.Tasks

open Prelude

open Microsoft.Data.Sqlite
open Fumble
open LibDB.Db

module PT = LibExecution.ProgramTypes
module PT2RT = LibExecution.ProgramTypesToRuntimeTypes
module BinarySerialization = LibBinarySerialization.BinarySerialization


/// Apply a single AddType op to the package_types table
let private applyAddType (typ : PT.PackageType.PackageType) : Task<unit> =
  task {
    let ptDef = BinarySerialization.PT.PackageType.serialize typ.id typ
    let rtDef =
      typ
      |> PT2RT.PackageType.toRT
      |> BinarySerialization.RT.PackageType.serialize typ.id

    do!
      Sql.query
        """
        INSERT OR REPLACE INTO package_types (id, pt_def, rt_def)
        VALUES (@id, @pt_def, @rt_def)
        """
      |> Sql.parameters
        [ "id", Sql.uuid typ.id
          "pt_def", Sql.bytes ptDef
          "rt_def", Sql.bytes rtDef ]
      |> Sql.executeStatementAsync
  }

/// Apply a single AddValue op to the package_values table
let private applyAddValue (value : PT.PackageValue.PackageValue) : Task<unit> =
  task {
    let ptDef = BinarySerialization.PT.PackageValue.serialize value.id value
    let rtDval =
      value
      |> PT2RT.PackageValue.toRT
      |> BinarySerialization.RT.PackageValue.serialize value.id

    do!
      Sql.query
        """
        INSERT OR REPLACE INTO package_values (id, pt_def, rt_dval)
        VALUES (@id, @pt_def, @rt_dval)
        """
      |> Sql.parameters
        [ "id", Sql.uuid value.id
          "pt_def", Sql.bytes ptDef
          "rt_dval", Sql.bytes rtDval ]
      |> Sql.executeStatementAsync
  }

/// Apply a single AddFn op to the package_functions table
let private applyAddFn (fn : PT.PackageFn.PackageFn) : Task<unit> =
  task {
    let ptDef = BinarySerialization.PT.PackageFn.serialize fn.id fn
    let rtInstrs =
      fn |> PT2RT.PackageFn.toRT |> BinarySerialization.RT.PackageFn.serialize fn.id

    do!
      Sql.query
        """
        INSERT OR REPLACE INTO package_functions (id, pt_def, rt_instrs)
        VALUES (@id, @pt_def, @rt_instrs)
        """
      |> Sql.parameters
        [ "id", Sql.uuid fn.id
          "pt_def", Sql.bytes ptDef
          "rt_instrs", Sql.bytes rtInstrs ]
      |> Sql.executeStatementAsync
  }

/// Apply a Set*Name op to the locations table
// TODO: Need to thread currentAccount through the call stack:
// - applySetName needs currentAccount parameter
// - applyOp needs to pass it to applySetName
// - applyOps needs to receive it and pass to applyOp
// - Callers of applyOps need to provide currentAccount
// Consider: Should account context be stored in a request/session object?
let private applySetName
  (branchID : Option<PT.BranchID>)
  (itemId : uuid)
  (location : PT.PackageLocation)
  (itemType : string)
  (currentAccount : string)  // NEW: current user's account name/id
  : Task<unit> =
  task {
    let modulesStr = String.concat "." location.modules

    // APPROVAL WORKFLOW: Check ownership and determine status
    // If user owns the namespace, auto-approve. Otherwise, mark as pending.
    let status =
      if location.owner = currentAccount then "approved"
      else "pending"

    // First, deprecate any existing location for this item in this branch
    // (handles moves: old location gets deprecated, new location created)
    do!
      Sql.query
        """
        UPDATE locations
        SET deprecated_at = datetime('now')
        WHERE item_id = @item_id
          AND deprecated_at IS NULL
          AND (branch_id = @branch_id OR (branch_id IS NULL AND @branch_id IS NULL))
        """
      |> Sql.parameters
        [ "item_id", Sql.uuid itemId
          "branch_id",
          (match branchID with
           | Some id -> Sql.uuid id
           | None -> Sql.dbnull) ]
      |> Sql.executeStatementAsync

    // Insert new location entry with unique location_id
    // NOW INCLUDES: status and created_by for approval workflow
    let locationId = System.Guid.NewGuid()
    do!
      Sql.query
        """
        INSERT INTO locations (location_id, item_id, branch_id, owner, modules, name, item_type, status, created_by)
        VALUES (@location_id, @item_id, @branch_id, @owner, @modules, @name, @item_type, @status, @created_by)
        """
      |> Sql.parameters
        [ "location_id", Sql.uuid locationId
          "item_id", Sql.uuid itemId
          "branch_id",
          (match branchID with
           | Some id -> Sql.uuid id
           | None -> Sql.dbnull)
          "owner", Sql.string location.owner
          "modules", Sql.string modulesStr
          "name", Sql.string location.name
          "item_type", Sql.string itemType
          "status", Sql.string status           // NEW: approval status
          "created_by", Sql.string currentAccount ]  // NEW: track creator
      |> Sql.executeStatementAsync
  }


/// Apply a single PackageOp to the projection tables
/// branchID: None = main/merged, Some(id) = branch-specific
// TODO: Add currentAccount parameter here and pass to applySetName
let applyOp
  (branchID : Option<PT.BranchID>)
  (currentAccount : string)  // NEW: needs to be passed through
  (op : PT.PackageOp)
  : Task<unit> =
  task {
    match op with
    | PT.PackageOp.AddType typ -> do! applyAddType typ
    | PT.PackageOp.AddValue value -> do! applyAddValue value
    | PT.PackageOp.AddFn fn -> do! applyAddFn fn
    | PT.PackageOp.SetTypeName(id, location) ->
      do! applySetName branchID id location "type" currentAccount  // Pass through
    | PT.PackageOp.SetValueName(id, location) ->
      do! applySetName branchID id location "value" currentAccount  // Pass through
    | PT.PackageOp.SetFnName(id, location) ->
      do! applySetName branchID id location "fn" currentAccount  // Pass through
    // TODO: Handle new ApproveLocation and RejectLocation ops (from Option 2)
    | PT.PackageOp.ApproveLocation(locationId, approvalRequestId) ->
      failwith "TODO: implement ApproveLocation op handling"
    | PT.PackageOp.RejectLocation(locationId, approvalRequestId, reason) ->
      failwith "TODO: implement RejectLocation op handling"
  }


/// Apply a list of PackageOps to the projection tables
/// This is used during package loading/reload
// TODO: Add currentAccount parameter here and pass to applyOp
let applyOps
  (branchID : Option<PT.BranchID>)
  (currentAccount : string)  // NEW: needs to be passed through
  (ops : List<PT.PackageOp>)
  : Task<unit> =
  task {
    for op in ops do
      do! applyOp branchID currentAccount op
  }
