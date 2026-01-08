/// View module - renders output and prompts
module Cli2.View

open Cli2.Types

/// Format the welcome message
let formatWelcome (accountName: string) : string list =
  let commandList = Commands.Registry.formatCompactCommandList ()
  [ $"Hello {accountName}! Let's write some software with Darklang."
    ""
    "Available commands:" ]
  @ commandList
  @ [ ""
      "Type 'help' for detailed command info" ]

/// Format the prompt prefix
let formatPromptPrefix (state: Model) : string =
  let branch =
    match state.CurrentBranchID with
    | Some id -> $"[{id.ToString().Substring(0, 8)}] "
    | None -> ""

  let location = PackageLocation.format state.PackageData.CurrentLocation
  $"{branch}{location}> "
