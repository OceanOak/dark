/// Account command - manage account settings
module Cli2.Commands.Account

open System
open Cli2.Types
open Cli2.Commands.ICommand

type AccountCommand() =
  inherit CommandBase()

  override _.Name = "account"
  override _.Description = "Manage account settings"

  override _.Execute state args =
    match args with
    | [] ->
      let accountIdStr =
        state.AccountID
        |> Option.map (fun id -> id.ToString())
        |> Option.defaultValue "(not set)"

      let output = CommandOutput.lines [
        "Account Information"
        String.replicate 40 "-"
        ""
        $"Name: {state.AccountName}"
        $"ID: {accountIdStr}"
        ""
        "Commands:"
        "  account login     - Log in to your account"
        "  account logout    - Log out"
        "  account switch    - Switch to a different account"
      ]
      (output, StateUpdate state)

    | [ "login" ] ->
      let output = CommandOutput.lines [
        "Account Login"
        ""
        "(Login not yet implemented in F# CLI)"
        ""
        "This will authenticate you with the Darklang servers."
      ]
      (output, StateUpdate state)

    | [ "logout" ] ->
      let output = CommandOutput.lines [
        "Logging out..."
        ""
        "(Logout not yet implemented in F# CLI)"
      ]
      (output, StateUpdate state)

    | [ "switch"; name ] ->
      let output = CommandOutput.lines [
        $"Would switch to account: {name}"
        ""
        "(Account switching not yet implemented in F# CLI)"
      ]
      (output, StateUpdate state)

    | _ ->
      let output = CommandOutput.error "Usage: account [login|logout|switch] [name]"
      (output, StateUpdate state)

  override _.Help() =
    [ "Usage: account [command] [args]"
      "Manage your Darklang account."
      ""
      "Commands:"
      "  (none)           Show account information"
      "  login            Log in to your account"
      "  logout           Log out of your account"
      "  switch <name>    Switch to a different account"
      ""
      "Examples:"
      "  account                - Show current account"
      "  account login          - Log in"
      "  account switch myorg   - Switch to 'myorg' account" ]

let command = AccountCommand() :> ICommand
