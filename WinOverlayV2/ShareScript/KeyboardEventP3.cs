using System;
using System.Runtime.InteropServices;
#if UNITY_EDITOR || UNITY_STANDALONE
#else
using System.Windows.Input;
#endif

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

#if UNITY_EDITOR || UNITY_STANDALONE
		// Unity constructor using Windows.Forms.KeyEventArgs
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
#else
		// WPF constructor using System.Windows.Input.Key
		public KeyboardEventP3(Key key, ModifierKeys modifiers, bool isKeyUp)
		{
			KeyCode = KeyInterop.VirtualKeyFromKey(key);
			Alt = (modifiers & ModifierKeys.Alt) != 0;
			Control = (modifiers & ModifierKeys.Control) != 0;
			Shift = (modifiers & ModifierKeys.Shift) != 0;
			Handled = false;
			SuppressKeyPress = false;
			IsKeyUp = isKeyUp;
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
			if (bytes == null || bytes.Length < 4)
				return false;
			for (int i = 0; i < 4; i++)
			{
				if (bytes[i] != labelBytes[i])
					return false;
			}
			return true;
		}
	}
}
