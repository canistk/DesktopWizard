# Unity3D 與 WinOverlay 整合設計

## 概述
本文件說明 Unity3D 與 WinOverlay 之間的集中式設計架構，包含 GPU 共享記憶體、事件處理和程序間通訊機制。

---

## 架構設計

### 集中式設計
- **Unity3D**: 單一程序，管理多個 DwCamera
- **WinOverlay**: 單一程序，管理多個透明 overlay 視窗
- **通訊方式**: Named Pipe + Memory Mapped Files

### 設計優勢
1. 統一的全局滑鼠事件處理
2. 簡化的程序間通訊
3. 高效的資源管理
4. 動態 overlay 創建/銷毀

---

## GPU 共享記憶體協議

### ShareInfo 結構
```csharp
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ShareInfo
{
    public IntPtr rtHandler;        // GPU 紋理句柄
    public int width;               // 紋理寬度
    public int height;              // 紋理高度
    public int rowPitch;            // 行間距
    public int bytesPerPixel;       // 每像素位元組數
    public int totalSize;           // 總記憶體大小
}
```

### 記憶體映射命名規則
```
DwCamera_{cameraId}_{subId}
```

### Unity3D 端實作
```csharp
public class DwCamera : MonoBehaviour
{
    private MemoryMappedFile memoryMap;
    private MemoryMappedViewAccessor accessor;
    
    void ShareMemory_Update(GPUWorker gpu)
    {
        var shareInfo = new ShareInfo
        {
            rtHandler = gpu.GetTextureHandle(),
            width = renderTexture.width,
            height = renderTexture.height,
            rowPitch = gpu.GetRowPitch(),
            bytesPerPixel = 4,
            totalSize = width * height * bytesPerPixel
        };
        
        accessor.Write(0, ref shareInfo);
    }
}
```

### WinOverlay 端實作
```csharp
public class DwOverlayForm : Form
{
    private MemoryMappedFile memoryMap;
    private MemoryMappedViewAccessor accessor;
    
    private void InitializeMemoryMap(string mapName)
    {
        memoryMap = MemoryMappedFile.OpenExisting(mapName);
        accessor = memoryMap.CreateViewAccessor();
    }
    
    private void RenderFrame()
    {
        var shareInfo = accessor.ReadStruct<ShareInfo>(0);
        // 使用 GPU 句柄渲染紋理
        RenderTexture(shareInfo);
    }
}
```

---

## 程序間通訊 (IPC)

### Named Pipe 通訊
- **管道名稱**: `DwCamera_Control`
- **方向**: 雙向通訊
- **用途**: 控制指令、狀態同步、心跳檢測

### Unity3D 端 - 伺服器
```csharp
public class DwCameraManager : MonoBehaviour
{
    private NamedPipeServerStream pipeServer;
    
    void Start()
    {
        pipeServer = new NamedPipeServerStream(
            "DwCamera_Control",
            PipeDirection.InOut,
            1,
            PipeTransmissionMode.Byte,
            PipeOptions.Asynchronous
        );
        
        StartConnectionListener();
    }
    
    public static void RegisterCamera(DwCamera camera)
    {
        activeCameras[camera.uniqueId] = camera;
        SendCameraUpdate();
    }
    
    private static void SendCameraUpdate()
    {
        var message = new
        {
            action = "UPDATE_CAMERAS",
            cameras = activeCameras.Values.Select(c => new
            {
                id = c.uniqueId,
                memoryMapName = $"DwCamera_{c.cameraId}_{c.subId}",
                formSetting = c.setting
            }).ToArray()
        };
        
        SendToPipe(JsonUtility.ToJson(message));
    }
    
    private static void SendToPipe(string message)
    {
        if (pipeServer?.IsConnected == true)
        {
            var data = Encoding.UTF8.GetBytes(message);
            pipeServer.Write(data, 0, data.Length);
            pipeServer.Flush();
        }
    }
}
```

### WinOverlay 端 - 客戶端
```csharp
public class OverlayManager : ApplicationContext
{
    private NamedPipeClientStream controlPipe;
    private Dictionary<string, DwOverlayForm> overlayForms = new();
    
    private async void InitializeControlPipe()
    {
        controlPipe = new NamedPipeClientStream(".", "DwCamera_Control", PipeDirection.InOut);
        await controlPipe.ConnectAsync();
        
        _ = Task.Run(ListenForCommands);
    }
    
    private void ProcessCommand(CameraUpdateMessage message)
    {
        if (message.action == "UPDATE_CAMERAS")
        {
            UpdateOverlayForms(message.cameras);
        }
    }
    
    private void UpdateOverlayForms(CameraInfo[] cameras)
    {
        // 移除不存在的 overlay
        var currentIds = cameras.Select(c => c.id).ToHashSet();
        var toRemove = overlayForms.Keys.Where(id => !currentIds.Contains(id)).ToList();
        
        foreach (var id in toRemove)
        {
            overlayForms[id].Close();
            overlayForms.Remove(id);
        }
        
        // 新增或更新 overlay
        foreach (var camera in cameras)
        {
            if (!overlayForms.ContainsKey(camera.id))
            {
                var form = new DwOverlayForm(camera);
                overlayForms[camera.id] = form;
                form.Show();
            }
        }
    }
}
```

---

## 滑鼠事件處理

### 全局滑鼠鉤子
WinOverlay 使用 Windows Hook 捕獲全局滑鼠事件：

```csharp
public class GlobalMouseHook
{
    private const int WH_MOUSE_LL = 14;
    private LowLevelMouseProc proc = HookCallback;
    private IntPtr hookID = IntPtr.Zero;
    
    public event Action<Point> MouseMove;
    public event Action<Point, MouseButtons> MouseClick;
    
    public GlobalMouseHook()
    {
        hookID = SetHook(proc);
    }
    
    private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var hookStruct = Marshal.PtrToStructure<POINT>(lParam);
            var point = new Point(hookStruct.x, hookStruct.y);
            
            switch ((uint)wParam)
            {
                case 0x0200: // WM_MOUSEMOVE
                    MouseMove?.Invoke(point);
                    break;
                case 0x0201: // WM_LBUTTONDOWN
                    MouseClick?.Invoke(point, MouseButtons.Left);
                    break;
            }
        }
        
        return CallNextHookEx(IntPtr.Zero, nCode, wParam, lParam);
    }
}
```

### 事件分發機制
```csharp
public class OverlayManager : ApplicationContext
{
    private void OnGlobalMouseMove(Point position)
    {
        foreach (var form in overlayForms.Values)
        {
            if (form.ContainsPoint(position))
            {
                form.HandleGlobalMouseMove(position);
            }
        }
    }
    
    private void OnGlobalMouseClick(Point position, MouseButtons button)
    {
        foreach (var form in overlayForms.Values)
        {
            if (form.ContainsPoint(position))
            {
                form.HandleGlobalMouseClick(position, button);
                SendMouseEventToUnity(form.CameraId, position, button);
            }
        }
    }
    
    private void SendMouseEventToUnity(string cameraId, Point position, MouseButtons button)
    {
        var message = new
        {
            action = "MOUSE_EVENT",
            cameraId = cameraId,
            x = position.X,
            y = position.Y,
            button = button.ToString()
        };
        
        SendToPipe(JsonConvert.SerializeObject(message));
    }
    
    private void SendToPipe(string message)
    {
        if (controlPipe?.IsConnected == true)
        {
            var data = Encoding.UTF8.GetBytes(message);
            controlPipe.Write(data, 0, data.Length);
            controlPipe.Flush();
        }
    }
}
```

### Unity3D 事件接收
```csharp
public class DwForm : Form
{
    private Queue<EventPacket> m_Events = new Queue<EventPacket>();
    
    internal void ProcessEvents()
    {
        while (m_Events.Count > 0)
        {
            var evt = m_Events.Dequeue();
            evt.Resolve();
        }
    }
    
    public void ReceiveMouseEvent(MouseEventData eventData)
    {
        var evt = new MouseEventPacket(() => {
            // 處理滑鼠事件
            OnMouseEvent(eventData);
        });
        
        m_Events.Enqueue(evt);
    }
}
```

---

## 視窗事件處理

### 視窗狀態同步
```csharp
public class DwOverlayForm : Form
{
    protected override void OnMove(EventArgs e)
    {
        base.OnMove(e);
        SendWindowEventToUnity("MOVE", new { x = Left, y = Top });
    }
    
    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        SendWindowEventToUnity("RESIZE", new { width = Width, height = Height });
    }
    
    private void SendWindowEventToUnity(string eventType, object data)
    {
        var message = new
        {
            action = "WINDOW_EVENT",
            cameraId = this.cameraId,
            eventType = eventType,
            data = data
        };
        
        // 透過 Named Pipe 發送給 Unity3D
        OverlayManager.Instance.SendToPipe(JsonUtility.ToJson(message));
    }
}
```

---

## 連接狀態管理

### 三種連接狀態
1. **NotRunning**: WinOverlay 未運作
2. **Connected**: 正常連接
3. **Unresponsive**: 連接但無回應

### Unity3D 端狀態處理
```csharp
public enum OverlayConnectionState
{
    NotRunning,
    Connected,
    Unresponsive
}

public class OverlayConnectionManager : MonoBehaviour
{
    public OverlayConnectionState CurrentState { get; private set; }
    private float lastHeartbeat = 0f;
    private const float HEARTBEAT_TIMEOUT = 5f;
    
    void Update()
    {
        CheckConnectionState();
        HandleStateTransitions();
    }
    
    private void HandleStateTransitions()
    {
        switch (CurrentState)
        {
            case OverlayConnectionState.NotRunning:
                TryStartWinOverlay();
                break;
            case OverlayConnectionState.Unresponsive:
                RestartWinOverlay();
                break;
        }
    }
}
```

### WinOverlay 端心跳機制
```csharp
public class Unity3DConnector
{
    private Timer heartbeatTimer;
    
    private void StartHeartbeat()
    {
        heartbeatTimer = new Timer(SendHeartbeat, null, 0, 2000);
    }
    
    private void SendHeartbeat(object state)
    {
        var heartbeat = new { action = "HEARTBEAT", timestamp = DateTime.Now };
        SendToPipe(JsonUtility.ToJson(heartbeat));
    }
    
    private void SendToPipe(string message)
    {
        if (pipeClient?.IsConnected == true)
        {
            var data = Encoding.UTF8.GetBytes(message);
            pipeClient.Write(data, 0, data.Length);
            pipeClient.Flush();
        }
    }
}
```

---

## 效能考量

### GPU 記憶體最佳化
- 使用 Ping-Pong Buffer 避免讀寫衝突
- DirectX 紋理對齊 256 bytes
- 非同步記憶體存取

### 事件處理最佳化
- 事件佇列批次處理
- 全局鉤子最小化處理時間
- Named Pipe 非同步通訊

### 資源管理
- 適當的 MemoryMappedFile 釋放
- Form 生命週期管理
- 程序監控與自動重啟

---

## 錯誤處理

### 常見錯誤情況
1. MemoryMappedFile 不存在
2. Named Pipe 連接失敗
3. GPU 紋理句柄無效
4. WinOverlay 程序崩潰

### 恢復機制
- 自動重試連接
- 程序重啟
- 資源清理
- 錯誤日誌記錄

---

## 部署注意事項

### 系統需求
- Windows 10/11
- .NET Framework 4.7.2+
- DirectX 11+ 支援

### 安全性
- 記憶體映射檔案權限控制
- Named Pipe 存取限制
- 程序間通訊加密（可選）

### 除錯工具
- 記憶體映射檔案監控
- Named Pipe 通訊日誌
- 效能分析工具