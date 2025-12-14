using System;
using System.Collections.Generic;
using System.IO.MemoryMappedFiles;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Share;

namespace WinOverlay
{

	/// <summary>
	/// Provides functionality to access and read camera matrix information from a memory-mapped file.
	/// </summary>
	/// <remarks>This class is designed to interact with a memory-mapped file that contains camera matrix
	/// data. It allows reading the data in a thread-safe manner and ensures proper resource management.</remarks>
	public class WOCameraShare // TODO: IDisposable
	{
		private MemoryMappedFile mmf;
		private MemoryMappedViewAccessor accessor;
		private readonly string m_Name;
		private bool IsInitialized => mmf != null && accessor != null;
		public WOCameraShare(string mmfName, CancellationToken token)
		{
			this.m_Name = mmfName;
			Task.Run(() => WaitForInit(token));
		}
		private async void WaitForInit(CancellationToken token)
		{
			mmf?.Dispose();
			accessor?.Dispose();
			while (mmf == null &&
				!token.IsCancellationRequested)
			{
				try
				{
					mmf = MemoryMappedFile.OpenExisting(m_Name, MemoryMappedFileRights.ReadWrite);
				}
				catch (System.IO.FileNotFoundException)
				{
					await Task.Delay(100, token);
					continue;
				}
				catch (Exception ex)
				{
					Console.WriteLine($"[{m_Name}] Error opening memory-mapped file. {ex.Message}");
					return;
				}
			}

			if (mmf != null)
			{
				accessor = mmf.CreateViewAccessor(0, 0, MemoryMappedFileAccess.Read);
			}
		}

		const int MaxLength = 1024 * 1024; // 1 MB limit for safety
		byte[] m_Buffer = new byte[MaxLength];

		public bool TryRead(out CameraInfo info)
		{
			if (!IsInitialized)
			{
				info = default;
				return false;
			}
			try
			{
				// check if 4 bytes are available for length prefix
				if (accessor.Capacity < 4)
				{
					info = default;
					return false;
				}

				// Read the actual data
				accessor.ReadArray(0, m_Buffer, 0, 4);
				if (!CameraInfo.IsValid(ref m_Buffer))
				{
					info = default;
					return false;
				}

				accessor.ReadArray(4, m_Buffer, 4, CameraInfo.ByteArraySize);
			}
			catch
			{
				info = default;
				return false;
			}

			try
			{
				// ReadOnlySpan<byte> span = new ReadOnlySpan<byte>(m_Buffer, 0, length);
				// Deserialize CameraInfo from bytes
				// info = CameraInfo.Parser.ParseFrom(span);
				info = CameraInfo.FromByteArray(ref m_Buffer);

				return true;
			}
			catch (Exception ex)
			{
				// Console print Hex dump of m_Buffer
				var hex = BitConverter.ToString(m_Buffer, 0, Math.Min(64, m_Buffer.Length)).Replace("-", " ");
				Console.WriteLine($"[{m_Name}] Error parsing CameraInfo: {ex.Message}\nHex Dump: {hex}");
				info = default;
				return false;
			}
		}
	}

}
