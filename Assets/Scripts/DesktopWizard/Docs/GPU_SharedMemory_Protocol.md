# GPU Shared Memory Protocol for Unity3D and WinOverlay

## Overview
This document defines the protocol for using GPU shared memory between Unity3D and WinOverlay. The protocol leverages DirectX shared resources (`ID3D11Texture2D`) to enable efficient real-time image transfer. It also incorporates a Ping-Pong Buffer mechanism to handle synchronization and desynchronization issues.

---

## Key Features
- **DirectX Shared Resources**: GPU-based shared memory for high-performance image transfer.
- **Ping-Pong Buffer**: Two alternating buffers to avoid read/write conflicts.
- **Overwrite Oldest Buffer**: Ensures Unity3D can continue rendering even if WinOverlay lags behind.
- **1:N Communication**: Supports one Unity3D instance and multiple WinOverlay instances.

---

## Buffer Structure
Each buffer consists of:
1. **Shared Texture**: A `ID3D11Texture2D` resource for storing image data.
2. **Metadata**: A small memory-mapped file for synchronization and state management.

### Metadata Layout
| Offset | Size (Bytes) | Description                          |
|--------|--------------|--------------------------------------|
| 0      | 1            | Buffer State (0=Idle, 1=Written)    |
| 1      | 8            | Timestamp (Unix time in milliseconds) |
| 9      | 4            | Width of the texture                |
| 13     | 4            | Height of the texture               |

---

## Workflow

### Unity3D (Producer)
1. **Initialization**:
   - Create two shared textures (`ID3D11Texture2D`) with the `D3D11_RESOURCE_MISC_SHARED` flag.
   - Create metadata files for each buffer.

2. **Rendering**:
   - Check the state of each buffer:
     - If a buffer is `Idle (0)`, write the rendered frame to it.
     - If both buffers are `Written (1)`, overwrite the oldest buffer (based on the timestamp).

3. **Update Metadata**:
   - After writing to a buffer, update its metadata:
     - Set the state to `Written (1)`.
     - Update the timestamp, width, and height.

4. **Notify WinOverlay**:
   - Optionally, signal WinOverlay (e.g., via an event or named pipe) that a new frame is available.

### WinOverlay (Consumer)
1. **Initialization**:
   - Open the shared textures and metadata files.

2. **Reading**:
   - Check the state of each buffer:
     - If a buffer is `Written (1)`, read the texture.
   - If no buffer is available, wait or skip the frame.

3. **Process Data**:
   - Use the texture for rendering or other purposes.

4. **NamedPipe Notification**:
   - Notify Unity3D via NamedPipe about the buffer being read (optional).

---

## Synchronization
- **Buffer State**:
  - Unity3D writes to a buffer only if it is `Idle (0)` or if both buffers are full (overwriting the oldest).
  - WinOverlay reads from a buffer only if it is `Written (1)`.

- **Timestamp**:
  - Used to determine the oldest buffer when both are full.

- **Thread Safety**:
  - Ensure atomic updates to the buffer state and timestamp to avoid race conditions.

---

## Error Handling
- **Desynchronization**:
  - If WinOverlay lags behind, Unity3D will overwrite the oldest buffer, ensuring the latest data is always available.

- **Resource Cleanup**:
  - Both Unity3D and WinOverlay must release shared resources properly to avoid memory leaks.

---

## Implementation Notes
- **DirectX Texture Creation**:
  - Use the following flags for shared textures:
    ```cpp
    D3D11_TEXTURE2D_DESC desc = {};
    desc.MiscFlags = D3D11_RESOURCE_MISC_SHARED;
    ```

- **Metadata Access**:
  - Use `MemoryMappedFile` in C# for metadata synchronization.

- **Performance**:
  - Minimize CPU-GPU synchronization to maintain high frame rates.

---

## Example Code

### Unity3D: Writing to Shared Texture
```csharp
void WriteToBuffer(int bufferIndex, Texture2D texture) {
    // Update shared texture
    IntPtr sharedHandle = GetSharedHandle(bufferIndex);
    ID3D11Texture2D sharedTexture = OpenSharedTexture(sharedHandle);
    CopyTextureToSharedResource(texture, sharedTexture);

    // Update metadata
    UpdateMetadata(bufferIndex, texture.width, texture.height);
}
```

### WinOverlay: Reading from Shared Texture
```csharp
void ReadFromBuffer(int bufferIndex) {
    // Check buffer state
    if (GetBufferState(bufferIndex) == 1) {
        // Read shared texture
        ID3D11Texture2D sharedTexture = OpenSharedTexture(bufferIndex);
        RenderTexture(sharedTexture);
    }
}
```

---

## Future Enhancements
- **Dynamic Buffer Count**: Allow more than two buffers for higher flexibility.
- **Compression**: Compress texture data to reduce memory usage.
- **Cross-Platform Support**: Add support for OpenGL or Vulkan for non-Windows platforms.

---

This protocol ensures efficient and synchronized data transfer between Unity3D and WinOverlay, even under high frame rate and desynchronization conditions.