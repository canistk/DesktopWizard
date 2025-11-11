# GPU Texture Sharing via Memory-Mapped File

## 概述

本文件說明如何透過 **Memory-Mapped File (MMF)** 在 Unity3D 與外部 Windows Form 進程之間共享 GPU RenderTexture 的像素資料。

---

## 架構概覽

### 資料流向

```
Unity Process (Main Thread)
  ↓
GPU RenderTexture (GPU Memory)
  ↓ ReadPixels (GPU → CPU)
  ↓
byte[] (CPU Memory)
  ↓ Write to MMF
  ↓
Memory-Mapped File (Shared Memory)
  ↓ Read from MMF
  ↓
Windows Form Process
  ↓
System.Drawing.Bitmap
  ↓
Window Overlay Display
```

### 核心概念

1. **Unity 端**：使用 `Texture2D.ReadPixels()` 將 GPU 資料讀取到 CPU
2. **共享記憶體**：透過 Memory-Mapped File 傳遞像素資料
3. **Windows Form 端**：從共享記憶體讀取並建立 Bitmap

---

## 技術規格

### Memory-Mapped File 配置

#### ShareInfo MMF
- **命名規則**：`DwCamera_{cameraId}_{SubId}`
- **大小**：`Marshal.SizeOf<ShareInfo>()`
- **用途**：傳遞 metadata（寬高、格式、時間戳記等）
- **存取權限**：Unity (Write) / WinForm (Read)

#### Pixels MMF
- **命名規則**：`DwCamera_{cameraId}_{SubId}_Pixels`
- **大小**：動態調整（建議初始 10MB）
- **用途**：傳遞實際像素資料
- **存取權限**：Unity (Write) / WinForm (Read)

### ShareInfo 資料結構

```csharp
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct ShareInfo
{
    public IntPtr rtHandler;        // RenderTexture 指標 (保留供參考)
    public DateTime timestamp;      // 更新時間戳記
    public int width;               // 紋理寬度
    public int height;              // 紋理高度
    public int rowPitch;            // 每行位元組數
    public int bytesPerPixel;       // 每像素位元組數 (4 for RGBA32)
    public int totalSize;           // 總資料大小（bytes）
}
```

---

## Unity 端實作

### 相關檔案
- **核心類別**：`Assets\Scripts\DesktopWizard\GPUWorker.cs`
- **工具類別**：`Assets\Scripts\DesktopWizard\DwUtils.cs`
- **Camera 控制**：`Assets\Scripts\DesktopWizard\DwCamera.cs`

### 實作要點

#### 1. 雙緩衝機制
使用 **Ping-Pong Buffer** 避免讀寫衝突：
- `DwCamera_{id}_1` / `DwCamera_{id}_1_Pixels`
- `DwCamera_{id}_2` / `DwCamera_{id}_2_Pixels`

#### 2. Memory-Mapped File 初始化
```csharp
// ShareInfo MMF
private MemoryMappedFile mmf = null;
private MemoryMappedViewAccessor accessor = null;

// Pixels MMF (新增)
private MemoryMappedFile mmfPixels = null;
private MemoryMappedViewAccessor accessorPixels = null;
```

#### 3. UpdateMemory() 流程
1. **ReadPixels**：將 RenderTexture 讀取到 Texture2D
2. **DumpTexture**：使用 `DwUtils.DumpTexture()` 轉換為 byte[]
3. **Write to MMF**：將 byte[] 寫入 Pixels MMF
4. **Update ShareInfo**：更新 metadata 到 ShareInfo MMF

參考：
- `Assets\Scripts\DesktopWizard\DwCamera.cs` (Line 696-668 有 DumpTexture 使用範例)

#### 4. 紋理格式
- **Unity 格式**：`TextureFormat.RGBA32`
- **像素順序**：R, G, B, A (每通道 8-bit)
- **BytesPerPixel**：4

---

## Windows Form 端實作

### 相關檔案
- **主視窗**：`WinOverlay\DwForm.cs`
- **連接器**：`WinOverlay\Unity3DConnector.cs`
- **轉換器**：`WinOverlay\BitmapConverter.cs` (待建立)

### 實作要點

#### 1. BitmapConverter 類別職責
- 開啟 Pixels MMF
- 讀取 byte[] 資料
- 轉換為 `System.Drawing.Bitmap`
- 處理 Unity RGBA → Windows BGRA 格式轉換

#### 2. 像素格式轉換
```
Unity:   RGBA (R, G, B, A)
Windows: BGRA (B, G, R, A)
```

#### 3. DwForm 整合
- 在 `ConvertDirectXTextureToBitmap()` 中呼叫 BitmapConverter
- 使用 `BeginInvoke` 更新 UI 執行緒的 Bitmap
- 適當處理舊 Bitmap 的 Dispose

---

## 效能考量

### 優點
- 實作簡單，使用現有的 `DwUtils.DumpTexture()` 邏輯
- 不需要額外的 Native Plugin
- 跨進程可靠

### 缺點
- **GPU→CPU 讀取延遲**：`ReadPixels()` 約 2-5ms
- **Memory Copy 開銷**：約 1-2ms
- **總計效能**：~30-45 FPS @ 1920x1080

### 記憶體需求
**解析度範例**：
- 1920×1080 @ RGBA32 = 1920 × 1080 × 4 = ~8MB
- 建議 Pixels MMF 初始大小：10MB
- 動態調整：當尺寸改變時重新建立 MMF

---

## 雙緩衝同步機制

### Ping-Pong Buffer 原理
1. Unity 端交替寫入 Buffer 1 / Buffer 2
2. WinForm 端讀取兩個 Buffer 的 `timestamp`
3. 選擇最新的 Buffer 進行讀取

### 時間戳記比較
```csharp
// WinForm 端程式碼參考
m_GPU01.TryRead(out var shareInfo1);
m_GPU02.TryRead(out var shareInfo2);

ShareInfo latestShareInfo = 
    (shareInfo1.timestamp > shareInfo2.timestamp) 
    ? shareInfo1 
    : shareInfo2;
```

參考：`WinOverlay\DwForm.cs` 中的 `UpdateFrame()` 方法

---

## 錯誤處理

### Unity 端
- 檢查 `renderTexture` 是否為 null
- 處理 MMF 建立失敗
- Log 錯誤到 Unity Console

### Windows Form 端
- 處理 MMF 開啟失敗（檔案不存在）
- 超時重試機制（30 秒，每秒重試）
- 轉換失敗時返回 null，保持 Debug 繪製

參考：`WinOverlay\DwForm.cs` 中的 `InitializeSharedMemory()` 方法

---

## 資源管理

### Unity 端 Dispose
```csharp
protected override void Dispose(bool disposing)
{
    if (!IsDisposed)
    {
        if (disposing)
        {
            accessor?.Dispose();
            mmf?.Dispose();
            accessorPixels?.Dispose();  // 新增
            mmfPixels?.Dispose();        // 新增
            renderTexture?.Release();
        }
        // 清空參考
    }
    IsDisposed = true;
}
```

### Windows Form 端 Dispose
```csharp
protected override void Dispose(bool disposing)
{
    if (!isDisposed)
    {
        if (disposing)
        {
            renderTimer?.Stop();
            renderTimer?.Dispose();
            currentBitmap?.Dispose();
            m_Converter?.Dispose();  // 新增
            m_GPU01?.Dispose();
            m_GPU02?.Dispose();
        }
        isDisposed = true;
    }
    base.Dispose(disposing);
}
```

---

## 實作檢查清單

### Unity 端 (GPUWorker.cs)
- [ ] 新增 `mmfPixels` 和 `accessorPixels` 欄位
- [ ] 修改 `InitializeMemoryMappedFile()` 建立 Pixels MMF
- [ ] 修改 `UpdateMemory()` 實作 ReadPixels → byte[] → MMF
- [ ] 修改 `DisposeMemoryMappedFile()` 清理 Pixels MMF
- [ ] 測試 RenderTexture 格式為 RGBA32

### Windows Form 端
- [ ] 建立 `WinOverlay\BitmapConverter.cs`
- [ ] 實作 MMF 讀取邏輯
- [ ] 實作 RGBA → BGRA 轉換
- [ ] 實作 Bitmap 建立邏輯
- [ ] 修改 `DwForm.cs` 整合 BitmapConverter
- [ ] 修改 `ConvertDirectXTextureToBitmap()` 實作
- [ ] 測試記憶體洩漏

### 整合測試
- [ ] 單一 Camera 運作正常
- [ ] 多 Camera 同時運作
- [ ] 動態解析度變更
- [ ] 長時間運行無記憶體洩漏
- [ ] 效能達標（30+ FPS）

---

## 已知限制

1. **效能上限**：約 30-45 FPS，受限於 ReadPixels 和 Memory Copy
2. **記憶體使用**：每個 Camera 需要額外 ~8-10MB 共享記憶體
3. **格式支援**：目前僅支援 RGBA32 格式
4. **平台限制**：僅支援 Windows (Memory-Mapped File)

---

## 未來優化方向

如果效能需求更高，可考慮：
1. **DirectX Shared Resource**：真正的 GPU 共享（需要 Native Plugin）
2. **格式壓縮**：使用壓縮格式減少傳輸量
3. **ROI 傳輸**：只傳輸變更的區域

參考文件：`doc/DirectX_Shared_Resource_Plan.md` (方案 A)

---

## 參考資料

### Unity 文件
- [Texture2D.ReadPixels](https://docs.unity3d.com/ScriptReference/Texture2D.ReadPixels.html)
- [RenderTexture](https://docs.unity3d.com/ScriptReference/RenderTexture.html)

### Microsoft 文件
- [Memory-Mapped Files](https://docs.microsoft.com/en-us/dotnet/standard/io/memory-mapped-files)
- [MemoryMappedFile Class](https://docs.microsoft.com/en-us/dotnet/api/system.io.memorymappedfiles.memorymappedfile)

### 專案內部參考
- `Assets\Scripts\DesktopWizard\DwUtils.cs` - DumpTexture 實作
- `Assets\Scripts\DesktopWizard\DwCamera.cs` - Camera 整合範例
- `WinOverlay\DwForm.cs` - Windows Form 整合範例

---

**文件版本**：1.0  
**最後更新**：2025-01-XX  
**作者**：DesktopWizard Team
