using System;

namespace WinOverlay
{
    public static class MessageTypes
    {
        public const string HEARTBEAT = "HEARTBEAT";
        public const string HEARTBEAT_ACK = "HEARTBEAT_ACK";
        public const string UPDATE_CAMERAS = "UPDATE_CAMERAS";
        public const string MOUSE_EVENT = "MOUSE_EVENT";
        public const string WINDOW_EVENT = "WINDOW_EVENT";
    }

    public class BaseMessage
    {
        public string action { get; set; }
        public string timestamp { get; set; } = DateTime.Now.ToString("O");
    }

    public class HeartbeatMessage : BaseMessage
    {
        public HeartbeatMessage()
        {
            action = MessageTypes.HEARTBEAT;
        }
    }

    public class MouseEventMessage : BaseMessage
    {
        public string cameraId { get; set; }
        public int x { get; set; }
        public int y { get; set; }
        public string button { get; set; }

        public MouseEventMessage()
        {
            action = MessageTypes.MOUSE_EVENT;
        }
    }
}