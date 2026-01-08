/// Branch command - manage package branches
module Cli2.Commands.Branch

open System
open Cli2.Types
open Cli2.Commands.ICommand

module Branches = LibPackageManager.Branches

type BranchCommand() =
  inherit CommandBase()

  override _.Name = "branch"
  override _.Description = "Manage package branches"
  override _.Aliases = [ "br" ]

  override _.Execute state args =
    match args with
    | [] ->
      // List branches
      match state.AccountID with
      | None ->
        let output = CommandOutput.lines [
          "Branches:"
          "* main (current)"
          ""
          "Log in with 'account login' to manage branches."
        ]
        (output, StateUpdate state)
      | Some accountID ->
        let branches = Branches.list(accountID).Result
        let lines = ResizeArray<string>()
        lines.Add("Branches:")

        // Show main first
        let currentBranchStr =
          state.CurrentBranchID
          |> Option.map (fun id -> id.ToString())
          |> Option.defaultValue ""

        if state.CurrentBranchID.IsNone then
          lines.Add("* main (current)")
        else
          lines.Add("  main")

        for branch in branches do
          let branchIdStr = branch.id.ToString()
          let isCurrent = branchIdStr = currentBranchStr
          let marker = if isCurrent then "*" else " "
          let currentText = if isCurrent then " (current)" else ""
          lines.Add($"{marker} {branch.name}{currentText}")

        lines.Add("")
        let output = CommandOutput.lines (lines |> Seq.toList)
        (output, StateUpdate state)

    | [ "create"; name ] ->
      match state.AccountID with
      | None ->
        let output = CommandOutput.error "You must be logged in to create branches. Use 'account login'."
        (output, StateUpdate state)
      | Some accountID ->
        let branch = (Branches.create accountID name).Result
        let output = CommandOutput.lines [
          $"Created branch: {name}"
          $"Branch ID: {branch.id}"
          ""
          $"Use 'branch switch {name}' to switch to this branch."
        ]
        (output, StateUpdate state)

    | [ "switch"; name ] ->
      match state.AccountID with
      | None ->
        let output = CommandOutput.error "You must be logged in to switch branches. Use 'account login'."
        (output, StateUpdate state)
      | Some accountID ->
        if name = "main" then
          let newState = { state with CurrentBranchID = None }
          let output = CommandOutput.lines [ $"Switched to branch: main" ]
          (output, StateUpdate newState)
        else
          let branchOpt = (Branches.findByName accountID name).Result
          match branchOpt with
          | None ->
            let output = CommandOutput.error $"Branch '{name}' not found."
            (output, StateUpdate state)
          | Some branch ->
            let newState = { state with CurrentBranchID = Some branch.id }
            let output = CommandOutput.lines [ $"Switched to branch: {name}" ]
            (output, StateUpdate newState)

    | [ "delete"; name ] ->
      if name = "main" then
        let output = CommandOutput.error "Cannot delete the main branch."
        (output, StateUpdate state)
      else
        let output = CommandOutput.lines [
          $"Would delete branch: {name}"
          ""
          "(Branch deletion not yet implemented)"
        ]
        (output, StateUpdate state)

    | [ "info" ] | [ "info"; _ ] ->
      match state.CurrentBranchID with
      | None ->
        let output = CommandOutput.lines [
          "Branch: main"
          "Status: Default branch"
        ]
        (output, StateUpdate state)
      | Some branchID ->
        let branchOpt = Branches.get(branchID).Result
        match branchOpt with
        | None ->
          let output = CommandOutput.error "Current branch not found."
          (output, StateUpdate state)
        | Some branch ->
          let createdStr = branch.createdAt.ToString()
          let mergedStr =
            match branch.mergedAt with
            | Some instant -> instant.ToString()
            | None -> "Not merged"

          let output = CommandOutput.lines [
            $"Branch: {branch.name}"
            $"ID: {branch.id}"
            $"Created: {createdStr}"
            $"Merged: {mergedStr}"
          ]
          (output, StateUpdate state)

    | _ ->
      let output = CommandOutput.error "Usage: branch [create|switch|delete|info] [name]"
      (output, StateUpdate state)

  override _.Help() =
    [ "Usage: branch [command] [name]"
      "Manage package branches."
      ""
      "Commands:"
      "  (none)           List all branches"
      "  create <name>    Create a new branch"
      "  switch <name>    Switch to a branch"
      "  delete <name>    Delete a branch"
      "  info [name]      Show branch details"
      ""
      "Examples:"
      "  branch                 - List all branches"
      "  branch create feature  - Create 'feature' branch"
      "  branch switch main     - Switch to main branch"
      "  branch info            - Show current branch info" ]

let command = BranchCommand() :> ICommand
