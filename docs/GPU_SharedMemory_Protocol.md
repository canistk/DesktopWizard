# GPU Shared Memory Protocol for Unity3D and WinOverlay

## Overview
This document defines the protocol for using GPU shared memory between Unity3D and WinOverlay. The protocol leverages DirectX shared resources (`ID3D11Texture2D`) and memory-mapped files for efficient real-time image transfer with metadata synchronization. It incorporates a Ping-Pong Buffer mechanism using two alternating GPUWorker instances to handle synchronization and desynchronization issues.

---

## Key Features
- **DirectX Shared Resources**: GPU-based shared memory for high-performance image transfer using native texture handles.
- **Memory-Mapped Files**: Efficient metadata sharing using `MemoryMappedFile` for synchronization and state management.
- **Ping-Pong Buffer**: Two alternating GPUWorker instances (`GPU01`/`GPU02`) to avoid read/write conflicts.
- **Multi-Graphics API Support**: Supports DirectX 11/12, OpenGL, and Metal with appropriate alignment handling.
- **1:N Communication**: Supports one Unity3D instance and multiple WinOverlay instances.
- **FPS-Based Updates**: Configurable frame rate limiting for efficient resource usage.

---

## Architecture Components

### GPUWorker Structure
Each GPUWorker instance manages:
1. **RenderTexture**: Unity's render texture for GPU processing
2. **ShareInfo**: Metadata structure containing texture information
3. **MemoryMappedFile**: Shared memory for metadata access
4. **Native Texture Handle**: Platform-specific texture pointer for direct GPU access

### ShareInfo Metadata Layout
```csharp
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ShareInfo
{
    public IntPtr rtHandler;        // Native texture handle (platform-specific)
    public DateTime timestamp;      // UTC timestamp for synchronization
    public int width;               // Texture width in pixels
    public int height;              // Texture height in pixels  
    public int rowPitch;            // Row pitch in bytes (width * bytesPerPixel)
    public int bytesPerPixel;       // Bytes per pixel based on format
    public int totalSize;           // Total texture size in bytes
}
```

### Memory Layout Details
| Field | Size (Bytes) | Description |
|-------|--------------|-------------|
| rtHandler | 8 | Native texture handle (IntPtr) |
| timestamp | 8 | DateTime structure |
| width | 4 | Texture width |
| height | 4 | Texture height |
| rowPitch | 4 | Row pitch for texture data |
| bytesPerPixel | 4 | Bytes per pixel |
| totalSize | 4 | Total texture size |

---

## Naming Convention

### Memory-Mapped File Names
- **Format**: `DwCamera_{cameraId}_{subId}`
- **Examples**:
  - `DwCamera_0_1` - Camera 0, GPUWorker SubId 1
  - `DwCamera_0_2` - Camera 0, GPUWorker SubId 2
  - `DwCamera_1_1` - Camera 1, GPUWorker SubId 1

### Communication Channels
- **Named Pipe**: `DwCamera_Control` - For registration and control messages
- **Shared Memory**: Individual memory-mapped files per GPUWorker instance

---

## Workflow

### Unity3D (Producer)

#### Initialization
1. **Create GPUWorker Instances**:
   ```csharp
   m_RawGPU = new GPU_RawWorker(this, 0);      // SubId 0 (raw processing)
   m_GPU01 = new GPU_ComputeShaderWorker(this, 1, shader);  // SubId 1 
   m_GPU02 = new GPU_ComputeShaderWorker(this, 2, shader);  // SubId 2
   ```

2. **Register with DwConnector**:
   ```csharp
   DwConnector.Instance.Register(camera);
   // Sends RegisterCamera message via named pipe
   ```

#### Rendering Process
1. **Frame Rate Control**:
   - Check if update should be performed based on configured FPS
   - Skip rendering if frame rate limit not reached

2. **Ping-Pong Buffer Selection**:
   ```csharp
   PrepareGPU(out var gpu, out var prevSrc);
   // Alternates between GPU01 and GPU02 for ping-pong buffering
   ```

3. **Texture Capture**:
   ```csharp
   gpu.Execute(renderer, camera, width, height);
   // Renders current frame to selected GPUWorker's RenderTexture
   ```

4. **Memory Update**:
   ```csharp
   gpu.UpdateMemory();
   // Updates ShareInfo and writes to memory-mapped file
   ```

#### Memory-Mapped File Management
```csharp
private void UpdateMemory()
{
    // Update ShareInfo with current texture data
    m_ShareInfo.rtHandler = renderTexture.GetNativeDepthBufferPtr();
    m_ShareInfo.width = renderTexture.width;
    m_ShareInfo.height = renderTexture.height;
    m_ShareInfo.bytesPerPixel = GetBytesPerPixel(renderTexture.format);
    m_ShareInfo.timestamp = DateTime.UtcNow;
    
    // Handle platform-specific alignment
    var rowPitch = AlignToPowerOfTwo(width * bytesPerPixel, alignment);
    m_ShareInfo.totalSize = height * rowPitch;
    
    // Recreate memory-mapped file if size changed
    if (sizesChanged || accessor == null)
        InitializeMemoryMappedFile();
    
    // Write metadata to shared memory
    accessor.Write(0, ref m_ShareInfo);
}
```

### WinOverlay (Consumer)

#### Initialization
1. **Connect to Unity3D**:
   ```csharp
   await connector.ConnectAsync();
   // Connects to "DwCamera_Control" named pipe
   ```

2. **Register for Camera Updates**:
   - Receive `RegisterCamera` messages with camera IDs
   - Open corresponding memory-mapped files:
     - `DwCamera_{cameraId}_1`
     - `DwCamera_{cameraId}_2`

#### Reading Process
1. **Check Available Buffers**:
   ```csharp
   // Read ShareInfo from both memory-mapped files
   var shareInfo1 = ReadShareInfo("DwCamera_{cameraId}_1");
   var shareInfo2 = ReadShareInfo("DwCamera_{cameraId}_2");
   ```

2. **Select Most Recent Buffer**:
   - Compare timestamps to find the latest data
   - Open shared texture using native handle

3. **Texture Processing**:
   - Use the native texture handle to access GPU resource
   - Render or process the texture data

---

## Platform-Specific Implementation

### DirectX 11/12
```csharp
case GraphicsDeviceType.Direct3D11:
case GraphicsDeviceType.Direct3D12:
    // DirectX requires row pitch alignment (typically 256 bytes)
    rowPitch = AlignToPowerOfTwo(width * bytesPerPixel, 256);
    newTotalSize = height * rowPitch;
    break;
```

### OpenGL
```csharp
case GraphicsDeviceType.OpenGLES2:
case GraphicsDeviceType.OpenGLES3:
case GraphicsDeviceType.OpenGLCore:
    // OpenGL typically doesn't require special alignment
    rowPitch = width * bytesPerPixel;
    newTotalSize = height * rowPitch;
    break;
```

### Metal
```csharp
case GraphicsDeviceType.Metal:
    // Metal may require specific alignment (64 bytes)
    rowPitch = AlignToPowerOfTwo(rowPitch, 64);
    newTotalSize = height * rowPitch;
    break;
```

---

## Synchronization

### Temporal Synchronization
- **Timestamp-Based**: Uses `DateTime.UtcNow` for ordering frames
- **FPS Limiting**: Configurable frame rate prevents excessive updates
- **Ping-Pong Buffering**: Alternating between two buffers prevents conflicts

### Memory Safety
- **Atomic Updates**: ShareInfo structure written atomically to memory-mapped file
- **Size Change Handling**: Recreates memory-mapped file when texture size changes
- **Proper Disposal**: Ensures proper cleanup of GPU resources and memory-mapped files

---

## Error Handling

### Desynchronization
- Unity3D continues rendering using ping-pong buffer even if WinOverlay lags
- Most recent buffer is always available through timestamp comparison

### Resource Management
```csharp
protected virtual void Dispose(bool disposing)
{
    if (!IsDisposed)
    {
        if (disposing)
        {
            DisposeMemoryMappedFile();
            renderTexture?.Release();
        }
        renderTexture = null;
    }
    IsDisposed = true;
}
```

### Connection Handling
- Named pipe reconnection logic with retry mechanism
- Automatic WinOverlay process restart on connection loss
- Graceful handling of missing memory-mapped files

---

## Performance Optimizations

### Texture Format Support
The implementation supports various texture formats with optimized bytes-per-pixel calculations:
- 8-bit formats: R8 (1 byte)
- 16-bit formats: RG16, RGB565, ARGB4444 (2 bytes)
- 32-bit formats: ARGB32, BGRA32, RFloat (4 bytes)
- 64-bit formats: ARGBHalf, RGBAUShort (8 bytes)
- 128-bit formats: ARGBFloat, ARGBInt (16 bytes)

### Memory Alignment
```csharp
private int AlignToPowerOfTwo(int value, int alignment)
{
    return ((value + alignment - 1) / alignment) * alignment;
}
```

### FPS-Based Updates
```csharp
private bool ShouldPerformUpdate()
{
    var factor = 1f / Mathf.Clamp(setting.FPS, 1f, 60f);
    if (Time.realtimeSinceStartup - m_LastRenderTime < factor)
        return false;
    m_LastRenderTime = Time.realtimeSinceStartup;
    return true;
}
```

---

## Implementation Notes

### Unity3D Side
- Use `renderTexture.GetNativeDepthBufferPtr()` for native texture handle
- Handle different graphics APIs with appropriate alignment
- Implement proper disposal pattern for GPU resources
- Use ping-pong buffering with GPUWorker instances

### WinOverlay Side  
- Connect to named pipe for control messages
- Open memory-mapped files based on camera registration
- Read ShareInfo structure to get texture metadata
- Use native texture handle for direct GPU access

### Communication Protocol
- Named pipe for control messages (registration, errors, warnings)
- Memory-mapped files for high-frequency texture metadata
- JSON-based message format for control commands

---

## Future Enhancements
- **Dynamic Buffer Count**: Allow more than two buffers for higher flexibility
- **Compression**: Compress texture data to reduce memory usage  
- **Cross-Platform Vulkan Support**: Add Vulkan support for broader compatibility
- **Adaptive Quality**: Dynamic texture resolution based on performance
- **Multi-Camera Optimization**: Shared resources across multiple cameras

---

This protocol ensures efficient and synchronized data transfer between Unity3D and WinOverlay, leveraging both GPU shared resources and memory-mapped files for optimal performance under high frame rate conditions.