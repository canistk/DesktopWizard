using System;
namespace WinOverlay
{
	public static class Debug
	{
		public static void Log(string message)		=> System.Diagnostics.Debug.WriteLine(message, "INFO");
		public static void Warning(string message)	=> System.Diagnostics.Debug.WriteLine(message, "WARNING");
		public static void Error(string message)	=> System.Diagnostics.Debug.WriteLine(message, "ERROR");

	}
}