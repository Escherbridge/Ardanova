# ArdaNova Unity Game SDK

This SDK provides the necessary tools to integrate your Unity game with the ArdaNova ecosystem.

## Installation

1.  **Get the Shared Library**: The `ArdaNova.Domain` library (and other shared projects) will be published as a NuGet package. You can add this package to your Unity project using a tool like [NuGetForUnity](https://github.com/GlitchEnzo/NuGetForUnity).
2.  **Get the SDK**: The Unity-specific portion of the SDK will be provided as a `.unitypackage` or can be installed via the Unity Package Manager from a Git URL.

## Usage

The SDK will allow you to:
- Authenticate players with their ArdaNova identity.
- Query for project and token data.
- Award tokens to players for in-game actions.
- Facilitate in-game token exchanges.

```csharp
// Example: Using a shared model from the ArdaNova.Domain package
using ArdaNova.Domain.Models;

public class ProjectDisplay : MonoBehaviour
{
    void ShowProject(Project project)
    {
        // The 'Project' class is the exact same one used by the .NET API
        Debug.Log($"Project: {project.Title}, Status: {project.Status}");
    }
}
```

By sharing the `ArdaNova.Domain` library, we ensure that the data models and business logic are always in sync between your game and the ArdaNova backend, reducing errors and development time.
