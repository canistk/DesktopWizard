using System;

namespace WinOverlay
{
    public class BaseMessage
    {
        public string action { get; set; }
        public string timestamp { get; set; } = DateTime.Now.ToString("O");
    }

    public class MouseEventMessage : BaseMessage
    {
        public string cameraId { get; set; }
        public int x { get; set; }
        public int y { get; set; }
        public string button { get; set; }

        public MouseEventMessage()
        {
        }
    }
}