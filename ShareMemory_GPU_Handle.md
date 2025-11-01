# ShareMemory GPU Handle Protocol

## 核心結構

### ShareInfo 結構體
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

## 記憶體映射檔案

### 命名規則
```
DwCamera_{cameraId}_{subId}
```

### 建立流程
1. 計算紋理記憶體需求
2. 建立 MemoryMappedFile
3. 寫入 ShareInfo 到映射檔案開頭

## GPU 平台處理

### DirectX (D3D11/D3D12)
- 行間距對齊: 256 bytes
- 句柄類型: ID3D11Resource/ID3D12Resource

### OpenGL
- 行間距對齊: 無特殊要求
- 句柄類型: GL Texture name (整數)

### Metal
- 行間距對齊: 64 bytes
- 句柄類型: id<MTLTexture>

## 紋理格式支援

### 常用格式
- ARGB32: 4 bytes/pixel
- BGRA32: 4 bytes/pixel
- RFloat: 4 bytes/pixel
- ARGBHalf: 8 bytes/pixel
- ARGBFloat: 16 bytes/pixel

## WinOverlay 讀取實作

### C# 讀取端
```csharp
// 開啟記憶體映射檔案
var mmf = MemoryMappedFile.OpenExisting("DwCamera_0_0");
var accessor = mmf.CreateViewAccessor(0, Marshal.SizeOf<ShareInfo>());

// 讀取 ShareInfo
ShareInfo info;
accessor.Read(0, out info);

// 使用 GPU 句柄存取紋理資料
// 實作依平台而定
```

### 關鍵實作點
1. 確認 GPU 平台類型
2. 正確解析紋理句柄
3. 處理行間距對齊
4. 同步存取控制

## 更新機制

### DwCamera 端
- `ShareMemory_Update(GPUWorker gpu)` 更新記憶體
- `GPUWorker.UpdateMemory()` 寫入 ShareInfo

### 觸發條件
- 紋理尺寸變更
- GPU 工作器初始化
- 每幀渲染完成後

## 注意事項

1. **平台相依性**: 不同 GPU API 的句柄類型不同
2. **記憶體對齊**: DirectX/Metal 需要特殊對齊
3. **同步問題**: 讀寫端需要適當的同步機制
4. **資源釋放**: 確保 MemoryMappedFile 正確釋放