# DwForm Implementation for WinOverlay

## Overview

The `DwForm` class has been implemented to handle window overlay mode in the WinOverlay project. It creates overlay windows that display GPU-rendered content from Unity3D cameras using the GPU Shared Memory Protocol.

## Features

- **Window Overlay Mode**: Creates transparent overlay windows for each registered camera
- **GPU Shared Memory Integration**: Reads texture data from memory-mapped files using the ShareInfo structure
- **Ping-Pong Buffer Support**: Automatically selects the most recent buffer from the two available GPUWorker instances
- **Real-time Display**: Updates at ~60 FPS to display the latest rendered content
- **Dynamic Sizing**: Automatically adjusts window size based on texture dimensions
- **Center Positioning**: Windows are positioned at the center of the primary screen

## Implementation Details

### DwForm Class
- **Location**: `WinOverlay/DwForm.cs`
- **Purpose**: Creates overlay windows for displaying camera content
- **Key Methods**:
  - `InitializeSharedMemory()`: Connects to memory-mapped files (`DwCamera_{cameraId}_1` and `DwCamera_{cameraId}_2`)
  - `UpdateFrame()`: Reads ShareInfo from both buffers and selects the most recent one
  - `CreateBitmapFromNativeTexture()`: Converts native texture handle to displayable bitmap (placeholder implementation)

### OverlayManager Integration
- **Location**: `WinOverlay/OverlayManager.cs`
- **Changes**: 
  - Added `Dictionary<string, DwForm> m_ActiveCameras` to track active camera forms
  - `RegisterCamera()`: Creates and shows new DwForm instances
  - `UnregisterCamera()`: Closes and disposes DwForm instances

### ShareInfo Structure
- **Location**: `WinOverlay/DataStructures.cs`
- **Added**: ShareInfo struct matching the GPU Shared Memory Protocol specification

## Usage Flow

1. **Unity3D Side**: 
   - Camera registers via named pipe with `RegisterCamera` message
   - Creates memory-mapped files: `DwCamera_{cameraId}_1` and `DwCamera_{cameraId}_2`
   - Writes ShareInfo metadata and texture handles to shared memory

2. **WinOverlay Side**:
   - OverlayManager receives `RegisterCamera` message
   - Creates new DwForm instance for the camera ID
   - DwForm connects to the memory-mapped files
   - Displays overlay window with rendered content

## Current Limitations & TODOs

### Placeholder Implementation
The current `CreateBitmapFromNativeTexture()` method creates a gradient placeholder image instead of reading the actual DirectX texture. To implement actual texture reading:

```csharp
// TODO: Implement actual DirectX texture reading using shareInfo.rtHandler
// - Use D3D11 APIs to open shared resource handle
// - Copy texture data to CPU-accessible memory
// - Convert to Bitmap for display
```

### Future Enhancements
1. **DirectX Integration**: Implement actual texture reading from `shareInfo.rtHandler`
2. **Position Control**: Add support for custom window positioning
3. **Multi-Monitor Support**: Handle overlay placement across multiple monitors
4. **Performance Optimization**: Reduce CPU-GPU copying overhead
5. **Error Recovery**: Better handling of connection losses and memory mapping failures

## File Structure

```
WinOverlay/
??? DwForm.cs              # New overlay window class
??? OverlayManager.cs      # Updated to manage DwForm instances
??? DataStructures.cs      # Updated with ShareInfo structure
??? Unity3DConnector.cs    # Existing communication layer
??? Program.cs             # Application entry point
```

## Testing

To test the implementation:

1. Build and run the WinOverlay application
2. From Unity3D, send a `RegisterCamera` message with a camera ID
3. The overlay window should appear at the center of the screen
4. The window displays a gradient placeholder with camera information
5. Send `UnregisterCamera` to close the overlay window

The implementation is now ready for integration with the actual DirectX texture reading functionality.