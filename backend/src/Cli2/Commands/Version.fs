/// Version command - display CLI version information
module Cli2.Commands.Version

open System
open System.Reflection
open Cli2.Types
open Cli2.Commands.ICommand

type VersionCommand() =
  inherit CommandBase()

  override _.Name = "version"
  override _.Description = "Display CLI version information"

  override _.Execute state _args =
    // Get build info from assembly metadata
    let assembly = Assembly.GetEntryAssembly()
    let buildAttributes =
      try
        assembly.GetCustomAttribute<AssemblyMetadataAttribute>()
      with _ -> null

    let version =
      if isNull buildAttributes then
        "dev"
      else
        let hash = buildAttributes.Value
        $"alpha-{hash}"

    let output = CommandOutput.lines [
      $"Darklang CLI {version}"
      $"Running on {Environment.OSVersion.Platform}"
    ]
    (output, StateUpdate state)

  override _.Help() =
    [ "Usage: version"
      "Display CLI version and installation information."
      ""
      "Shows current version, installation mode, and location." ]

let command = VersionCommand() :> ICommand
