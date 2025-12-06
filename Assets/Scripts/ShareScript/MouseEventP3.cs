using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
#if UNITY_EDITOR || UNITY_STANDALONE
using UnityEngine;
using UnityEngine.EventSystems;
#endif

namespace Share
{
	/// <summary>Protobuf class for mouse event data.
	/// From WinOverlay to KawaiOS</summary>
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct MouseEventP3 : IInputEvent
	{
		public const string LABEL = "MOU_";
		public static readonly byte[] labelBytes = System.Text.Encoding.UTF8.GetBytes(LABEL);
		public int Button;
		public int State; // 0 : down, 1: up, 2: move, 3: wheel
		public int Clicks;
		public int X;
		public int Y;
		public int WheelDelta;
		public float monX;
		public float monY;
		public float formX;
		public float formY;
		public bool withinForm;

#if !(UNITY_EDITOR || UNITY_STANDALONE)
		public MouseEventP3(int state, MouseEventArgs e, CameraInfo cam, bool withinForm)
		{
			Button = (int)e.Button;
			State = state;
			if (state < 0 || state > 3)
				throw new ArgumentOutOfRangeException("state", "State must be 0 (down), 1 (up), 2 (move), or 3 (wheel).");
			Clicks = e.Clicks;
			X = e.X;
			Y = e.Y;
			WheelDelta = e.Delta;
			var monPos = cam.O2M.MultiplyPoint3x4(new Vec3(X, Y, 0));
			monX = monPos.X;
			monY = monPos.Y;

			var formPos = cam.M2F.MultiplyPoint3x4(monPos);
			formX = formPos.X;
			formY = formPos.Y;

			this.withinForm = withinForm;
		}
#endif

#if UNITY_EDITOR || UNITY_STANDALONE
		public PointerEventData ConvertToPointerEventData()
		{
			//var osPos = new Vector2(X, Y);
			//var formPos = new Vector2(formX, formY);
			var monPos = new Vector2(monX, monY);
			PointerEventData ped = new PointerEventData(EventSystem.current)
			{
				button = (PointerEventData.InputButton)Button,
				position = monPos,
				pressPosition = State == 0 ? monPos : Vector2.zero,
				clickCount = Clicks,
				scrollDelta = new Vector2(0, WheelDelta),
				// Rest require calculate in system.
			};
			return ped;
		}
#endif

		public byte[] ToByteArray()
		{
			int size = Marshal.SizeOf(this);
			byte[] arr = new byte[size + 4];
			// First 4 bytes for label
			Buffer.BlockCopy(labelBytes, 0, arr, 0, 4);

			IntPtr ptr = Marshal.AllocHGlobal(size);
			Marshal.StructureToPtr(this, ptr, true);
			Marshal.Copy(ptr, arr, 4, size);
			Marshal.FreeHGlobal(ptr);
			return arr;
		}

		public static MouseEventP3 FromByteArray(ref byte[] arr)
		{
			if (!IsValid(ref arr))
				throw new ArgumentException("Byte array does not contain valid MouseEventP3 data.");

			MouseEventP3 str = new MouseEventP3();
			int size = Marshal.SizeOf(str);
			IntPtr ptr = Marshal.AllocHGlobal(size);
			Marshal.Copy(arr, 4, ptr, size); // Skip the first 4 bytes (labelBytes)
			str = (MouseEventP3)Marshal.PtrToStructure(ptr, str.GetType());
			Marshal.FreeHGlobal(ptr);
			return str;
		}

		public static bool IsValid(ref byte[] bytes)
		{
			var span = new ReadOnlySpan<byte>(bytes, 0, 4);
			return span.SequenceEqual(labelBytes);
		}
	}
}
