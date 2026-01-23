# ArdaNova Godot Game SDK

This SDK provides the necessary tools to integrate your Godot game with the ArdaNova ecosystem.

## Installation

1.  **Get the Shared Library**: The `ArdaNova.Domain` library (and other shared projects) will be published as a NuGet package. Since Godot 4 has built-in NuGet support, you can add this package directly within the Godot editor.
2.  **Get the SDK**: The Godot-specific portion of the SDK will be provided as a Godot Addon, available from the Asset Library or as a direct download.

## Usage

The SDK will allow you to:
- Authenticate players with their ArdaNova identity.
- Query for project and token data.
- Award tokens to players for in-game actions.
- Facilitate in-game token exchanges.

```csharp
// Example: Using a shared model from the ArdaNova.Domain package in a C# script
using ArdaNova.Domain.Models;
using Godot;

public partial class ProjectDisplayNode : Node
{
    public void ShowProject(Project project)
    {
        // The 'Project' class is the exact same one used by the .NET API
        GD.Print($"Project: {project.Title}, Status: {project.Status}");
    }
}
```

By sharing the `ArdaNova.Domain` library, we ensure that the data models and business logic are always in sync between your game and the ArdaNova backend, reducing errors and development time.
