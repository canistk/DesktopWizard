# WinOverlay

## Overview
**WinOverlay** is a component of the **KawaiOS** project. It serves as a transparent overlay application that displays images rendered by Unity3D cameras and handles mouse/keyboard events, which are sent back to Unity3D for processing.

## Features
- **Transparent Overlay**: Displays images from Unity3D cameras using shared memory.
- **Input Event Handling**: Captures mouse and keyboard events (e.g., drag & drop) and sends them back to Unity3D for further processing.
- **Command-Line Configuration**: Supports runtime configuration via command-line arguments.

## Workflow
1. **Image Rendering**:
   - Unity3D cameras write image data to memory-mapped files.
   - WinOverlay reads the data and renders it on a transparent window.

2. **Input Event Feedback**:
   - Mouse and keyboard events are captured by the overlay.
   - Events are sent back to Unity3D via named pipes for real-time interaction.

## Command-Line Arguments
| Argument    | Description                     |
|-------------|---------------------------------|
| `-cameraId` | Specifies the Unity3D camera ID.|
| `-x`        | X-coordinate of the overlay.    |
| `-y`        | Y-coordinate of the overlay.    |
| `-w`        | Width of the overlay.           |
| `-h`        | Height of the overlay.          |

## Technical Details
- **Language**: C# 7.3
- **Framework**: .NET Framework 4.7.2
- **Key Components**:
  - **MemoryMappedFile**: Used for sharing image data between Unity3D and WinOverlay.
  - **NamedPipeClientStream**: Facilitates communication of input events back to Unity3D.

## Usage
1. Start Unity3D and ensure it writes camera output to memory-mapped files.
2. Launch WinOverlay with appropriate command-line arguments.
3. Interact with the overlay; input events will be processed by Unity3D.

## Notes
- This project is tightly integrated with Unity3D and relies on its camera output and event handling system.
- Ensure Unity3D and WinOverlay are configured to use the same memory and pipe naming conventions.

---
For further details, refer to the source code or project documentation.