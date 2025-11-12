-- Add approval workflow support to Darklang package management
--
-- This migration adds:
-- 1. Approval status tracking for locations (approved/pending/rejected)
-- 2. Accounts table for user tracking
-- 3. Approval requests (like PRs) for grouping proposed changes
-- 4. Support for partial approvals (approve some locations, reject others)
-- 5. Branch merge tracking


-- ============================================================================
-- 1. Extend locations table with approval status
-- ============================================================================

-- Purpose:
-- - status tracks whether a location is 'approved', 'pending', or 'rejected'
-- - created_by tracks which account created this location (needed for "show my pending changes")
-- - Index helps filter by approval status during name resolution

ALTER TABLE locations ADD COLUMN status TEXT NOT NULL DEFAULT 'approved';
ALTER TABLE locations ADD COLUMN created_by TEXT NULL;

CREATE INDEX idx_locations_status ON locations(status);


-- ============================================================================
-- 2. Extend branches table with merge tracking
-- ============================================================================

-- Purpose: Track which account merged the branch (useful for audit trail
-- and "branch merged elsewhere" detection)

ALTER TABLE branches ADD COLUMN merged_by TEXT NULL;


-- ============================================================================
-- 3. Create accounts table
-- ============================================================================

-- Purpose: Track user accounts for ownership checks and approval workflow

CREATE TABLE accounts (
  id TEXT PRIMARY KEY,
  username TEXT NOT NULL UNIQUE,
  email TEXT NULL,
  created_at TIMESTAMP NOT NULL DEFAULT (datetime('now'))
);

CREATE INDEX idx_accounts_username ON accounts(username);


-- ============================================================================
-- 4. Create approval_requests table
-- ============================================================================

-- Purpose: Groups multiple location changes into one approval request (like a PR)

CREATE TABLE approval_requests (
  id TEXT PRIMARY KEY,
  created_by TEXT NOT NULL,
  created_at TIMESTAMP NOT NULL DEFAULT (datetime('now')),
  status TEXT NOT NULL DEFAULT 'pending', -- 'pending', 'approved', 'rejected'
  reviewed_by TEXT NULL,
  reviewed_at TIMESTAMP NULL,
  title TEXT NULL,
  description TEXT NULL,
  target_namespace TEXT NULL, -- Namespace owner (for filtering requests by owner)
  source_branch_id TEXT NULL, -- Link to originating branch
  FOREIGN KEY (source_branch_id) REFERENCES branches(id) ON DELETE SET NULL
);

CREATE INDEX idx_approval_requests_status ON approval_requests(status);
CREATE INDEX idx_approval_requests_created_by ON approval_requests(created_by);
CREATE INDEX idx_approval_requests_target_namespace ON approval_requests(target_namespace);
CREATE INDEX idx_approval_requests_source_branch ON approval_requests(source_branch_id);


-- ============================================================================
-- 5. Create approval_request_locations join table
-- ============================================================================

-- Purpose: Many-to-many relationship - one approval request can contain multiple
-- locations, and we can track which approval request a location belongs to.
-- Supports partial approvals: owner can approve some locations and leave others pending.

CREATE TABLE approval_request_locations (
  approval_request_id TEXT NOT NULL,
  location_id TEXT NOT NULL,
  review_status TEXT NOT NULL DEFAULT 'pending',  -- 'pending', 'approved', 'rejected'
  review_reason TEXT NULL,  -- Optional reason for rejection
  reviewed_at TIMESTAMP NULL,
  PRIMARY KEY (approval_request_id, location_id),
  FOREIGN KEY (approval_request_id) REFERENCES approval_requests(id) ON DELETE CASCADE,
  FOREIGN KEY (location_id) REFERENCES locations(location_id) ON DELETE CASCADE
);

CREATE INDEX idx_approval_request_locations_request
  ON approval_request_locations(approval_request_id);
CREATE INDEX idx_approval_request_locations_location
  ON approval_request_locations(location_id);
CREATE INDEX idx_approval_request_locations_review_status
  ON approval_request_locations(review_status);
