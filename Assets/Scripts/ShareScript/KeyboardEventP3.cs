using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Share
{
	/// <summary>Protobuf class for keyboard event data.
	/// From WinOverlay to KawaiOS</summary>
	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct KeyboardEventP3 : IInputEvent
	{
		public const string LABEL = "KEY_";
		public static readonly byte[] labelBytes = System.Text.Encoding.UTF8.GetBytes(LABEL);
		public int KeyCode;
		public bool Alt;
		public bool Control;
		public bool Shift;
		public bool Handled;
		public bool SuppressKeyPress;
		public bool IsKeyUp;

		public KeyboardEventP3(KeyEventArgs e, bool isKeyUp)
		{
			KeyCode = (int)e.KeyCode;
			Alt = e.Alt;
			Control = e.Control;
			Shift = e.Shift;
			Handled = e.Handled;
			SuppressKeyPress = e.SuppressKeyPress;
			IsKeyUp = isKeyUp;
		}

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

		public static KeyboardEventP3 FromByteArray(ref byte[] arr)
		{
			if (!IsValid(ref arr))
				throw new ArgumentException("Byte array does not contain valid KeyboardEventP3 data.");

			KeyboardEventP3 str = new KeyboardEventP3();
			int size = Marshal.SizeOf(str);
			IntPtr ptr = Marshal.AllocHGlobal(size);
			Marshal.Copy(arr, 4, ptr, size); // Skip the first 4 bytes (labelBytes)
			str = (KeyboardEventP3)Marshal.PtrToStructure(ptr, str.GetType());
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
