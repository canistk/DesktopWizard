using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace WinOverlay
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct InputEvent
    {
        public byte Type;        // 0=MouseMove, 1=MouseDown, 2=MouseUp, 3=KeyDown, 4=KeyUp
        public byte CameraId;    
        public short X, Y;       
        public int Data;         // MouseButton or KeyCode
        public long Timestamp;   
    }
    
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct FrameInfo
    {
        public int FrameId;
        public float Fps;
        public byte WriteFlag;   // 0=Idle, 1=Writing
        public long Timestamp;
    }

    public struct OverlayConfig
    {
        public String CameraId;
        public Rectangle Bounds;
    }
}