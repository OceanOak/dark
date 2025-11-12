/// Functions for managing approval requests and approval workflow
module LibPackageManager.Approvals

open System.Threading.Tasks
open FSharp.Control.Tasks

open Prelude

open Microsoft.Data.Sqlite
open Fumble
open LibDB.Db

module PT = LibExecution.ProgramTypes


// --
// Query pending locations
// --

/// Get all pending locations created by the current account
let listPendingLocations (currentAccount : string) : Task<List<string>> =
  task {
    // TODO: Implement query
    // SELECT location_id FROM locations
    // WHERE status = 'pending' AND created_by = @currentAccount
    //   AND deprecated_at IS NULL
    return! failwith "TODO: implement listPendingLocations"
  }


/// Group pending locations by their target namespace (owner)
/// Returns: List of (namespace, List<locationId>)
/// This helps organize pending changes by who needs to approve them
let groupPendingLocationsByNamespace
  (currentAccount : string)
  : Task<List<string * List<string>>> =
  task {
    // TODO: Implement query
    // SELECT owner, GROUP_CONCAT(location_id) FROM locations
    // WHERE status = 'pending' AND created_by = @currentAccount
    //   AND deprecated_at IS NULL
    // GROUP BY owner
    return! failwith "TODO: implement groupPendingLocationsByNamespace"
  }


// --
// Create approval requests
// --

/// Create an approval request for a set of locations
/// All locations should be for the same target namespace
let createApprovalRequest
  (currentAccount : string)
  (targetNamespace : string)
  (locationIds : List<string>)
  (title : Option<string>)
  (description : Option<string>)
  (sourceBranchId : Option<PT.BranchID>)
  : Task<PT.ApprovalRequest> =
  task {
    // TODO: Implement
    // 1. Validate all locationIds exist and are pending
    // 2. Validate all locations have owner = targetNamespace
    // 3. INSERT INTO approval_requests
    // 4. INSERT INTO approval_request_locations for each locationId
    // 5. Return created ApprovalRequest

    let requestId = System.Guid.NewGuid()

    // Example structure:
    // do! Sql.query
    //   """
    //   INSERT INTO approval_requests
    //     (id, created_by, status, target_namespace, source_branch_id, title, description)
    //   VALUES (@id, @created_by, 'open', @target_namespace, @source_branch_id, @title, @description)
    //   """
    //   |> Sql.parameters [...]
    //   |> Sql.executeStatementAsync
    //
    // for locationId in locationIds do
    //   do! Sql.query
    //     """
    //     INSERT INTO approval_request_locations
    //       (approval_request_id, location_id, review_status)
    //     VALUES (@request_id, @location_id, 'pending')
    //     """
    //     |> Sql.parameters [...]
    //     |> Sql.executeStatementAsync

    return! failwith "TODO: implement createApprovalRequest"
  }


/// Auto-create approval requests for all pending locations
/// Groups by namespace and creates one request per namespace
let createApprovalRequestsForAllPending
  (currentAccount : string)
  (sourceBranchId : Option<PT.BranchID>)
  : Task<List<PT.ApprovalRequest>> =
  task {
    // TODO: Implement
    // 1. Get grouped pending locations
    // 2. For each namespace, create an approval request
    // 3. Return list of created requests

    let! grouped = groupPendingLocationsByNamespace currentAccount

    // Example:
    // let! requests =
    //   grouped
    //   |> List.map (fun (namespace, locationIds) ->
    //     createApprovalRequest
    //       currentAccount
    //       namespace
    //       locationIds
    //       (Some $"Changes to {namespace}")
    //       None
    //       sourceBranchId)
    //   |> Task.WhenAll

    return! failwith "TODO: implement createApprovalRequestsForAllPending"
  }


// --
// Review approval requests
// --

/// Get an approval request by ID
let getApprovalRequest (requestId : uuid) : Task<Option<PT.ApprovalRequest>> =
  task {
    // TODO: Implement query
    // SELECT * FROM approval_requests WHERE id = @requestId
    return! failwith "TODO: implement getApprovalRequest"
  }


/// Get all locations in an approval request
let getApprovalRequestLocations
  (requestId : uuid)
  : Task<List<PT.ApprovalRequestLocation>> =
  task {
    // TODO: Implement query
    // SELECT * FROM approval_request_locations
    // WHERE approval_request_id = @requestId
    return! failwith "TODO: implement getApprovalRequestLocations"
  }


/// List incoming approval requests (for namespaces you own)
/// TODO: Need to determine namespace ownership - how do we know which namespaces currentAccount owns?
/// Options:
/// - accounts table has owned_namespaces field
/// - separate namespace_owners table
/// - query locations for owner = currentAccount
let listIncomingRequests (currentAccount : string) : Task<List<PT.ApprovalRequest>> =
  task {
    // TODO: Implement query
    // SELECT DISTINCT ar.* FROM approval_requests ar
    // WHERE ar.target_namespace = @currentAccount
    //   AND ar.status = 'open'
    // ORDER BY ar.created_at DESC
    return! failwith "TODO: implement listIncomingRequests"
  }


/// List outgoing approval requests (requests you created)
let listOutgoingRequests (currentAccount : string) : Task<List<PT.ApprovalRequest>> =
  task {
    // TODO: Implement query
    // SELECT * FROM approval_requests
    // WHERE created_by = @currentAccount
    // ORDER BY created_at DESC
    return! failwith "TODO: implement listOutgoingRequests"
  }


// --
// Approve/Reject locations
// --

/// Approve specific locations from an approval request
/// This enables partial approval - owner can approve some and leave others pending
let approveLocations
  (requestId : uuid)
  (locationIds : List<string>)
  (reviewedBy : string)
  : Task<unit> =
  task {
    // TODO: Implement
    // 1. Verify reviewedBy owns the target namespace
    // 2. UPDATE locations SET status = 'approved' WHERE location_id IN locationIds
    // 3. UPDATE approval_request_locations
    //    SET review_status = 'approved', reviewed_at = now()
    //    WHERE approval_request_id = requestId AND location_id IN locationIds
    // 4. Check if all locations in request are decided
    // 5. If yes, UPDATE approval_requests SET status = 'completed'

    return! failwith "TODO: implement approveLocations"
  }


/// Reject specific locations from an approval request
let rejectLocations
  (requestId : uuid)
  (locationIds : List<string>)
  (reviewedBy : string)
  (reason : Option<string>)
  : Task<unit> =
  task {
    // TODO: Implement
    // 1. Verify reviewedBy owns the target namespace
    // 2. UPDATE locations SET status = 'rejected' WHERE location_id IN locationIds
    // 3. UPDATE approval_request_locations
    //    SET review_status = 'rejected', review_reason = reason, reviewed_at = now()
    //    WHERE approval_request_id = requestId AND location_id IN locationIds
    // 4. Check if all locations in request are decided
    // 5. If yes, UPDATE approval_requests SET status = 'completed'

    return! failwith "TODO: implement rejectLocations"
  }


/// Approve ALL locations in a request at once
let approveAllLocations
  (requestId : uuid)
  (reviewedBy : string)
  : Task<unit> =
  task {
    // TODO: Implement
    // 1. Get all location_ids from approval_request_locations WHERE approval_request_id = requestId
    // 2. Call approveLocations with all locationIds

    return! failwith "TODO: implement approveAllLocations"
  }


/// Reject ALL locations in a request at once
let rejectAllLocations
  (requestId : uuid)
  (reviewedBy : string)
  (reason : Option<string>)
  : Task<unit> =
  task {
    // TODO: Implement
    // 1. Get all location_ids from approval_request_locations WHERE approval_request_id = requestId
    // 2. Call rejectLocations with all locationIds

    return! failwith "TODO: implement rejectAllLocations"
  }


// --
// Helper functions
// --

/// Check if an approval request is completed (all locations have been decided)
let isRequestCompleted (requestId : uuid) : Task<bool> =
  task {
    // TODO: Implement query
    // SELECT COUNT(*) FROM approval_request_locations
    // WHERE approval_request_id = @requestId AND review_status = 'pending'
    // Returns true if count = 0

    return! failwith "TODO: implement isRequestCompleted"
  }


/// Update request status to 'completed' if all locations are decided
let updateRequestStatusIfCompleted (requestId : uuid) : Task<unit> =
  task {
    let! completed = isRequestCompleted requestId
    if completed then
      // TODO: Implement
      // UPDATE approval_requests SET status = 'completed' WHERE id = requestId
      return! failwith "TODO: implement status update"
    else
      return ()
  }


// --
// Validation helpers
// --

// NAMESPACE OWNERSHIP DESIGN:
//
// THE CORE QUESTION:
// When checking: if location.owner = currentAccount then "approved" else "pending"
// How do we know what namespaces currentAccount owns?
//
// OPTION 1: Simple String Match (location.owner == currentAccount)
// - Namespace name IS the account name
// - Check: "Stripe" == currentAccount
// Pros: Dead simple, no additional tables, direct 1:1 mapping
// Cons: Can't have teams, can't delegate, one account = one namespace only
// Example: Account "stachu" can only own "Stachu.*"
//
// OPTION 2: Accounts Table with Owned Namespaces
// - accounts.owned_namespaces = '["Stachu", "MyCompany", "Acme"]' (JSON array)
// - Check: List.contains location.owner (parse owned_namespaces)
// Pros: One account owns multiple namespaces, flexible
// Cons: JSON in SQL (not great for querying), schema updates when namespaces change
//
// OPTION 3: Separate Namespace Owners Table (RECOMMENDED)
// CREATE TABLE namespace_owners (
//   namespace TEXT NOT NULL,
//   account_id TEXT NOT NULL,
//   granted_at TIMESTAMP NOT NULL,
//   PRIMARY KEY (namespace, account_id)
// )
// Pros: Proper relational design, easy to query, supports teams (multiple owners),
//       can add roles later, track when ownership granted
// Cons: More complex, need to manage this table
// Example:
//   namespace: "Darklang", account_id: "darklang-team"
//   namespace: "Darklang", account_id: "paul"  -- Multiple owners!
//   namespace: "Stripe",   account_id: "stripe-eng"
//
// OPTION 4: Inferred from Existing Locations
// - Query: SELECT 1 FROM locations WHERE owner = @namespace AND created_by = @account
// - "You own it if you created something in it"
// Pros: No additional storage, emerges from usage
// Cons: Edge cases, bootstrap problems, can't pre-claim, hard to transfer
//
// OPTION 5: Hybrid - Default to String Match + Explicit Table
// - First check: location.owner = currentAccount (fast path)
// - Second check: Query namespace_owners table (explicit grants)
// Pros: Simple case automatic, complex cases handled, flexible
// Cons: Two checks to maintain, precedence could be confusing
//
// RECOMMENDATION: Option 3 (namespace_owners table)
// - Teams are essential (multiple people need to own "MyCompany")
// - Clean relational design
// - Future-proof for roles/permissions
// - Explicit ownership declaration
//
// OPEN QUESTIONS:
// 1. Should namespace names be globally unique? (probably yes)
// 2. Who can create new namespaces?
//    - Option A: First-come-first-served (anyone can claim)
//    - Option B: Admins only
//    - Option C: Request + approval
// 3. Can namespaces be transferred? (yes - just update namespace_owners)
// 4. Multiple owners per namespace? (yes - teams need this)
// 5. Is "Darklang" namespace special/reserved? (probably)
// 6. Sub-namespaces: If I own "MyCompany", do I auto-own "MyCompany.Stripe"?
//    (probably separate entries needed)
//
// PROPOSED IMPLEMENTATION:
// See next migration file for namespace_owners table schema
// For now, using simple string match until table exists

/// Verify that the reviewer owns the target namespace
let verifyNamespaceOwnership
  (namespace : string)
  (reviewer : string)
  : Task<bool> =
  task {
    // TODO: Implement proper ownership check
    // Once namespace_owners table exists:
    // SELECT 1 FROM namespace_owners
    // WHERE namespace = @namespace AND account_id = @reviewer

    // For now: Simple string match
    return namespace = reviewer
  }


/// Validate that all locations exist and are pending
let validateLocationsExist (locationIds : List<string>) : Task<bool> =
  task {
    // TODO: Implement query
    // SELECT COUNT(*) FROM locations
    // WHERE location_id IN locationIds AND status = 'pending' AND deprecated_at IS NULL
    // Returns true if count = length(locationIds)

    return! failwith "TODO: implement validateLocationsExist"
  }


/// Validate that all locations belong to the same namespace
let validateSameNamespace
  (locationIds : List<string>)
  (expectedNamespace : string)
  : Task<bool> =
  task {
    // TODO: Implement query
    // SELECT COUNT(DISTINCT owner) FROM locations WHERE location_id IN locationIds
    // Returns true if count = 1 AND that owner = expectedNamespace

    return! failwith "TODO: implement validateSameNamespace"
  }
