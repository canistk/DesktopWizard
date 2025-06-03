using Kit2;
using System;
using System.Text;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using UnityEngine;
using System.Drawing.Imaging;
using Bitmap = System.Drawing.Bitmap;
#if DW_USE_UNSAFE
using Unity.Collections.LowLevel.Unsafe;
#endif

namespace DesktopWizard
{
    
    public static class DwUtils
    {
		/// <summary>
		/// tested on Unity 2020.3.14f1
		/// Compare the performance of this methods (DumpTexture) with random write texture
		/// and `RWStructuredBuffer<float>` in compute shader.
		/// using this method is 80x faster than `RWStructuredBuffer<float>`
		/// m0) RWStructuredBuffer<float> running on 7 FPS & 77.78ms latency per frame.
		/// m1) while DumpTexture running on 60 FPS & 0.42ms latency per frame.
		/// <see cref="https://discussions.unity.com/t/how-do-you-read-pixels-from-a-custom-render-texture/688154/6"/>
		/// </summary>
		/// <param name="src">require to readable</param>
		/// <param name="dest">require to enable random write</param>
		/// <param name="alphaCut"></param>
		/// <exception cref="System.NullReferenceException"></exception>
		/// <exception cref="System.Exception"></exception>
		public static void DumpTexture(in RenderTexture src, Texture2D dest, Color? alphaCut = null, bool push2GPU = false)
        {
			if (dest == null)
				throw new System.NullReferenceException();
            if (!dest.isReadable)
				throw new System.Exception("Texture2D must be readable.");
			if (src == null)
				throw new System.NullReferenceException();
            
            RenderTexture.active = src;
            var area = new Rect(0, 0, src.width, src.height);
			dest.ReadPixels(area, 0, 0, recalculateMipMaps: false);
            if (alphaCut.HasValue)
            {
                GL.Clear(true, true, alphaCut.Value);
			}

            if (push2GPU)
                dest.Apply(false, false);
		}

        public static void DumpTexture(in Texture2D src, ref Bitmap dest)
        {
			if (src == null)
				throw new System.NullReferenceException();
			if (!src.isReadable)
				throw new System.Exception("Texture2D must be readable.");

			/// https://learn.microsoft.com/zh-tw/dotnet/api/system.drawing.imaging.pixelformat?view=windowsdesktop-9.0&viewFallbackFrom=dotnet-plat-ext-8.0
			/// ARGB32 = 32 bits per pixel, with 8 bits for each of alpha, red, green, and blue.
			const PixelFormat _format = PixelFormat.Format32bppArgb;

			var width = src.width;
            var height = src.height;

            if (dest == null ||
                dest.Width != width ||
                dest.Height != height)
            {
                if (dest != null)
                    dest.Dispose();
                dest = new Bitmap(width, height, _format);
            }

            //var sizeDiff = src.width != dest.Width || src.height != dest.Height;
            //if (!allowReize && sizeDiff)
            //    throw new System.Exception($"Size mismatch, src={src.width}*{src.height}, dest={dest.Width}*{dest.Height}");

			BitmapData bitmapData = dest.LockBits(
				new System.Drawing.Rectangle(0, 0, dest.Width, dest.Height),
				ImageLockMode.ReadOnly, _format);

#if DW_USE_UNSAFE
			// unsafe code block direct memory access
			unsafe
			{
				const int bytesPerPixel = 4;
				var cache           = src.GetRawTextureData<byte>();
				var rawPtr          = (byte*)NativeArrayUnsafeUtility.GetUnsafeBufferPointerWithoutChecks<byte>(cache);
				var bmpPointer      = (byte*)bitmapData.Scan0;
				int textureStride   = src.width * bytesPerPixel;
				int bitmapStride    = bitmapData.Stride;
				for (int y = 0; y < height; ++y)
				{
					// U3D's y-axis is flipped, upward = y++, downward = y--
					// Window's y-axis is, upward = y--, downward = y++
					var srcY        = height - 1 - y; // image height - y;
					var srcPtr      = rawPtr + (srcY * textureStride);
					var dstPtr      = bmpPointer + (y * bitmapStride);

					for (int x = 0; x < width; ++x)
					{
						// ARGB32 <- BGRA32
						dstPtr[0] = srcPtr[3]; // A
						dstPtr[1] = srcPtr[2]; // R
						dstPtr[2] = srcPtr[1]; // G
						dstPtr[3] = srcPtr[0]; // B
						srcPtr += bytesPerPixel;
						dstPtr += bytesPerPixel;
					}
				}
			}
#else // pre-allocated byte array
			IntPtr ptr = bitmapData.Scan0;
            const int bytesPerPixel = 4;
            int stride      = bitmapData.Stride;
			byte[] dstArr   = new byte[dest.Height * stride];
			byte[] srcArr   = src.GetRawTextureData();
            for (int y = 0; y < height; ++y)
            {
                int srcY = height - 1 - y;
                for (int x = 0; x < width; ++x)
                {
                    int dstIdx = (y * stride) + (x * bytesPerPixel);
					int srcIdx = (srcY * stride) + (x * bytesPerPixel);
					dstArr[dstIdx + 0] = srcArr[srcIdx + 3];
                    dstArr[dstIdx + 1] = srcArr[srcIdx + 2];
                    dstArr[dstIdx + 2] = srcArr[srcIdx + 1];
                    dstArr[dstIdx + 3] = srcArr[srcIdx + 0];
                }
            }
            Marshal.Copy(dstArr, 0, ptr, dstArr.Length);
#endif

			dest.UnlockBits(bitmapData);
		}

		/// <summary>
		/// map <see cref="System.Windows.Forms.Keys"/>
		/// to <see cref="KeyCode"/> and output character.
		/// since C# library Windows.Input fail to import into unity3d.
		/// </summary>
		/// https://stackoverflow.com/questions/5825820/how-to-capture-the-character-on-different-locale-keyboards-in-wpf-c/5826175#5826175
		public static bool TryWin2KeyMap(System.Windows.Forms.Keys key, bool shift,
            out KeyCode code, out char ch)
        {
            code = KeyCode.None;
            ch = char.MinValue;
            switch(key)
            {
                case Keys.A: code = KeyCode.A; ch = !shift ? 'a':'A'; return true;
                case Keys.B: code = KeyCode.B; ch = !shift ? 'b':'B'; return true;
                case Keys.C: code = KeyCode.C; ch = !shift ? 'c':'C'; return true;
                case Keys.D: code = KeyCode.D; ch = !shift ? 'd':'D'; return true;
                case Keys.E: code = KeyCode.E; ch = !shift ? 'e':'E'; return true;
                case Keys.F: code = KeyCode.F; ch = !shift ? 'f':'F'; return true;
                case Keys.G: code = KeyCode.G; ch = !shift ? 'g':'G'; return true;
                case Keys.H: code = KeyCode.H; ch = !shift ? 'h':'H'; return true;
                case Keys.I: code = KeyCode.I; ch = !shift ? 'i':'I'; return true;
                case Keys.J: code = KeyCode.J; ch = !shift ? 'j':'J'; return true;
                case Keys.K: code = KeyCode.K; ch = !shift ? 'k':'K'; return true;
                case Keys.L: code = KeyCode.L; ch = !shift ? 'l':'L'; return true;
                case Keys.M: code = KeyCode.M; ch = !shift ? 'm':'M'; return true;
                case Keys.N: code = KeyCode.N; ch = !shift ? 'n':'N'; return true;
                case Keys.O: code = KeyCode.O; ch = !shift ? 'o':'O'; return true;
                case Keys.P: code = KeyCode.P; ch = !shift ? 'p':'P'; return true;
                case Keys.Q: code = KeyCode.Q; ch = !shift ? 'q':'Q'; return true;
                case Keys.R: code = KeyCode.R; ch = !shift ? 'r':'R'; return true;
                case Keys.S: code = KeyCode.S; ch = !shift ? 's':'S'; return true;
                case Keys.T: code = KeyCode.T; ch = !shift ? 't':'T'; return true;
                case Keys.U: code = KeyCode.U; ch = !shift ? 'u':'U'; return true;
                case Keys.V: code = KeyCode.V; ch = !shift ? 'v':'V'; return true;
                case Keys.W: code = KeyCode.W; ch = !shift ? 'w':'W'; return true;
                case Keys.X: code = KeyCode.X; ch = !shift ? 'x':'X'; return true;
                case Keys.Y: code = KeyCode.Y; ch = !shift ? 'y':'Y'; return true;
                case Keys.Z: code = KeyCode.Z; ch = !shift ? 'z':'Z'; return true;

                // Digits
                case Keys.D1: code = !shift ? KeyCode.Alpha1 : KeyCode.Exclaim;     ch = !shift ? '1':'!'; return true;
                case Keys.D2: code = !shift ? KeyCode.Alpha2 : KeyCode.At;          ch = !shift ? '2':'@'; return true;
                case Keys.D3: code = !shift ? KeyCode.Alpha3 : KeyCode.Hash;        ch = !shift ? '3':'#'; return true;
                case Keys.D4: code = !shift ? KeyCode.Alpha4 : KeyCode.Dollar;      ch = !shift ? '4':'$'; return true;
                case Keys.D5: code = !shift ? KeyCode.Alpha5 : KeyCode.Percent;     ch = !shift ? '5':'%'; return true;
                case Keys.D6: code = !shift ? KeyCode.Alpha6 : KeyCode.Caret;       ch = !shift ? '6':'^'; return true;
                case Keys.D7: code = !shift ? KeyCode.Alpha7 : KeyCode.Ampersand;   ch = !shift ? '7':'&'; return true;
                case Keys.D8: code = !shift ? KeyCode.Alpha8 : KeyCode.Asterisk;    ch = !shift ? '8':'*'; return true;
                case Keys.D9: code = !shift ? KeyCode.Alpha9 : KeyCode.LeftParen;   ch = !shift ? '9':'('; return true;
                case Keys.D0: code = !shift ? KeyCode.Alpha0 : KeyCode.RightParen;  ch = !shift ? '0':')'; return true;

                // Numpad Keys
                case Keys.NumPad0  : code = KeyCode.Keypad0;        ch = '0'; return true;
                case Keys.NumPad1  : code = KeyCode.Keypad1;        ch = '1'; return true;
                case Keys.NumPad2  : code = KeyCode.Keypad2;        ch = '2'; return true;
                case Keys.NumPad3  : code = KeyCode.Keypad3;        ch = '3'; return true;
                case Keys.NumPad4  : code = KeyCode.Keypad4;        ch = '4'; return true;
                case Keys.NumPad5  : code = KeyCode.Keypad5;        ch = '5'; return true;
                case Keys.NumPad6  : code = KeyCode.Keypad6;        ch = '6'; return true;
                case Keys.NumPad7  : code = KeyCode.Keypad7;        ch = '7'; return true;
                case Keys.NumPad8  : code = KeyCode.Keypad8;        ch = '8'; return true;
                case Keys.NumPad9  : code = KeyCode.Keypad9;        ch = '9'; return true;
                case Keys.Add      : code = KeyCode.KeypadPlus;     ch = '+'; return true;
                case Keys.Subtract : code = KeyCode.KeypadMinus;    ch = '-'; return true;  
                case Keys.Multiply : code = KeyCode.KeypadMultiply; ch = '*'; return true;  
                case Keys.Divide   : code = KeyCode.KeypadDivide;   ch = '/'; return true;  
                case Keys.Decimal  : code = KeyCode.KeypadPeriod;   ch = '.'; return true;
                
                // Special Characters
                case Keys.Space:            code = KeyCode.Space;           ch = ' ';   return true;
                case Keys.Tab:              code = KeyCode.Tab;             ch = '\t'; return true;
                case Keys.Enter:            code = KeyCode.Return;          ch = '\n'; return true;
                case Keys.Oemtilde:         code = !shift ? KeyCode.BackQuote   :KeyCode.Tilde;                 ch = !shift ? '`' : '~'; return true;
                case Keys.OemOpenBrackets:  code = !shift ? KeyCode.LeftBracket :KeyCode.LeftCurlyBracket;      ch = !shift ? '[' : '{'; return true;
                case Keys.OemCloseBrackets: code = !shift ? KeyCode.RightBracket:KeyCode.RightCurlyBracket;     ch = !shift ? ']' : '}'; return true;
                case Keys.OemPipe:          code = !shift ? KeyCode.Backslash   :KeyCode.Pipe;                  ch = !shift ? '\\': '|'; return true;
                case Keys.OemSemicolon:     code = !shift ? KeyCode.Semicolon   :KeyCode.Colon;                 ch = !shift ? ';' : ':'; return true;
                case Keys.Oemcomma:         code = !shift ? KeyCode.Comma       :KeyCode.Less;                  ch = !shift ? ',' : '<'; return true;
                case Keys.OemPeriod:        code = !shift ? KeyCode.Period      :KeyCode.Greater;               ch = !shift ? '.' : '>'; return true;
                case Keys.OemQuestion:      code = !shift ? KeyCode.Slash       :KeyCode.Question;              ch = !shift ? '/' : '?'; return true;
                case Keys.OemMinus:         code = !shift ? KeyCode.Minus       :KeyCode.Underscore;            ch = !shift ? '-' : '_'; return true;
                case Keys.Oemplus:          code = !shift ? KeyCode.Equals      :KeyCode.Plus;                  ch = !shift ? '=' : '+'; return true;
                case Keys.OemQuotes:        code = !shift ? KeyCode.Quote       :KeyCode.DoubleQuote;           ch = !shift ? '\'' : '\"'; return true;

                // Function Keys
                case Keys.F1:   code = KeyCode.F1;  return true;
                case Keys.F2:   code = KeyCode.F2;  return true;
                case Keys.F3:   code = KeyCode.F3;  return true;
                case Keys.F4:   code = KeyCode.F4;  return true;
                case Keys.F5:   code = KeyCode.F5;  return true;
                case Keys.F6:   code = KeyCode.F6;  return true;
                case Keys.F7:   code = KeyCode.F7;  return true;
                case Keys.F8:   code = KeyCode.F8;  return true;
                case Keys.F9:   code = KeyCode.F9;  return true;
                case Keys.F10:  code = KeyCode.F10; return true;
                case Keys.F11:  code = KeyCode.F11; return true;
                case Keys.F12:  code = KeyCode.F12; return true;

                // Arrow Keys
                case Keys.Up:       code = KeyCode.UpArrow;     return true;
                case Keys.Down:     code = KeyCode.DownArrow;   return true;
                case Keys.Left:     code = KeyCode.LeftArrow;   return true;
                case Keys.Right:    code = KeyCode.RightArrow;  return true;

                // Special Keys
                case Keys.Escape:           code = KeyCode.Escape;          return true;
                case Keys.Back:             code = KeyCode.Backspace;       return true;
                case Keys.CapsLock:         code = KeyCode.CapsLock;        return true;
                case Keys.ShiftKey:
                case Keys.Shift:
                case Keys.LShiftKey:        code = KeyCode.LeftShift;       return true;
                case Keys.RShiftKey:        code = KeyCode.RightShift;      return true;
                case Keys.ControlKey:
                case Keys.Control:
                case Keys.LControlKey:      code = KeyCode.LeftControl;     return true;
                case Keys.RControlKey:      code = KeyCode.RightControl;    return true;
                case Keys.Menu:
                case Keys.LMenu:            code = KeyCode.LeftAlt;         return true;
                case Keys.RMenu:            code = KeyCode.RightAlt;        return true;
                case Keys.LWin:             code = KeyCode.LeftWindows;     return true;
                case Keys.RWin:             code = KeyCode.RightWindows;    return true;

                // Other Keys
                case Keys.Insert:       code = KeyCode.Insert;      return true;
                case Keys.Delete:       code = KeyCode.Delete;      return true;
                case Keys.Home:         code = KeyCode.Home;        return true;
                case Keys.End:          code = KeyCode.End;         return true;
                case Keys.PageUp:       code = KeyCode.PageUp;      return true;
                case Keys.PageDown:     code = KeyCode.PageDown;    return true;
                case Keys.NumLock:      code = KeyCode.Numlock;     return true;
            }
            return false;
        }


        /// <summary>The parent folder of <see cref="Application.dataPath"/></summary>
        /// <returns></returns>
        public static string GetProjectDir()
        {
            var workDir = new DirectoryInfo(UnityEngine.Application.dataPath);
            var parentDir = workDir.Parent;
            var projectDir = parentDir.FullName;
            return projectDir;
        }

        public static bool TryGetObjectFolder(UnityEngine.Object obj, out string absolutePath, out string relativePath)
        {
            absolutePath = null;
            relativePath = null;
#if UNITY_EDITOR
            if (obj == null)
                return false;

            var file = UnityEditor.AssetDatabase.GetAssetPath(obj);
            if (file == null || file.Length == 0)
                return false;
            var osPath = System.IO.Path.GetDirectoryName(file);
            if (osPath == null || osPath.Length == 0)
                return false;

            ResolvePath(osPath, out absolutePath, out relativePath);
            return true;
#else
            return false;
#endif
        }

        public static void ResolvePath(string path, out string absPath, out string relativePath)
        {
            if (path == null)
                throw new System.NullReferenceException();

            path = path.Replace("\\", "/");
            var isRelativePath = path.StartsWith("Assets/");
            var isAbsPath = path.StartsWith(UnityEngine.Application.dataPath);
            if (isRelativePath)
            {
                var str = path.Substring(7);
                absPath = Path.Combine(UnityEngine.Application.dataPath, str).Replace("\\", "/");
                relativePath = Path.Combine("Assets", str).Replace("\\", "/");
                return;
            }
            if (isAbsPath)
            {
                var str = path.Substring(UnityEngine.Application.dataPath.Length + 1);
                absPath = Path.Combine(UnityEngine.Application.dataPath, str).Replace("\\", "/");
                relativePath = Path.Combine("Assets", str).Replace("\\", "/");
                return;
            }
            throw new System.NotImplementedException();
        }

        /// <summary>A event dispatcher helper to dispatch event with try catch block.</summary>
        /// <typeparam name="EVENT"></typeparam>
        /// <param name="evt"></param>
        /// <param name="singleDispatcher"></param>
        /// <param name="maxDepth"></param>
        /// <exception cref="System.Exception"></exception>
        public static void TryCatchDispatchEventError<EVENT>(this EVENT evt, System.Action<EVENT> singleDispatcher, int maxDepth = -1)
            where EVENT : System.MulticastDelegate
        {
            if (evt == null)
                return;
            if (singleDispatcher == null)
            {
                throw new System.Exception("Must define how dispatcher handle the event.");
            }
            var handles = evt.GetInvocationList();
            for (var i = 0; i < handles.Length; ++i)
            {
                try
                {
                    var dispatcher = handles[i] as EVENT;
                    if (dispatcher == null)
                        continue;
                    singleDispatcher?.Invoke(dispatcher);
                }
                catch (System.Exception ex)
                {
                    ex.DeepLogInvocationException(evt.GetType().Name, handles[i], maxDepth);
                }
            }
        }

        public static void DeepLogInvocationException(this Exception ex, string eventDispatcherName, System.Delegate delegatehandler, int maxDepth = -1)
        {
            ex.DeepLogInvocationException($"{eventDispatcherName} > {(delegatehandler?.Target ?? "Unknown")}", maxDepth);
        }

        /// <summary>A helper to log exception stack trace in a more readable way.</summary>
        /// <param name="ex"></param>
        /// <param name="delegateName">reference for method's name or any other message.</param>
        /// <param name="maxDepth">-1 mean no limit</param>
        public static void DeepLogInvocationException(this Exception ex, string delegateName, int maxDepth = -1)
        {
            int depth = 0;
            Exception orgEx = ex;
            List<Exception> exStack = new List<Exception>(Mathf.Max(maxDepth, 2));
            while (ex != null && ex.InnerException != null &&
                (depth++ < maxDepth || maxDepth == -1))
            {
                exStack.Add(ex);
                ex = ex.InnerException;
            }

            // Fall back when no exception was logged
            if (exStack.Count == 0)
            {
                if (TryGetException(orgEx, out var stackTraceDetail))
                {
                    UnityEngine.Debug.LogError($"{orgEx.GetType().Name} during \"{delegateName}\" > \"{orgEx.Message}\"\n\n{stackTraceDetail}\n-EOF\n");
                }
                else
                {
                    UnityEngine.Debug.LogError($"{orgEx.GetType().Name} during \"{delegateName}\" > \"{orgEx.Message}\"\n\n{orgEx.StackTrace}\n-EOF\n");
                }
            }
            else
            {
                PrintInnerException(exStack);
            }

            void PrintInnerException(List<Exception> exStack)
            {
                int i = exStack.Count;
                while (i-- > 0)
                {
                    var ev2 = exStack[i];
                    if (TryGetException(ev2, out var stackTraceDetail))
                    {
                        UnityEngine.Debug.LogError($"{ev2.GetType().Name}[{exStack.Count - i}] \"{delegateName}\" > \"{ev2.Message}\"\n\n{stackTraceDetail}\n");
                    }
                    else
                    {
                        UnityEngine.Debug.LogError($"{ev2.GetType().Name}[{exStack.Count - i}] \"{delegateName}\" > \"{ev2.Message}\"\n\n{ex.StackTrace}\n");
                    }
                }
            }

            bool TryGetException(Exception exception, out string info)
            {
                StringBuilder sb = new StringBuilder();
                StackTrace trace = new(exception, true);
                for (var k = 0; k < trace.FrameCount; ++k)
                {
                    if (TryGetFrameInfo(trace.GetFrame(k), out var line))
                    {
                        sb.AppendLine(line);
                    }
                }
                info = sb.ToString();
                return info.Length > 0;
            }

            bool TryGetFrameInfo(StackFrame frame, out string info)
            {
                info = null;
                if (frame == null)
                    return false;
                var filePath = frame.GetFileName();
                if (filePath == null || filePath.Length == 0)
                    return false;
                var fileName = System.IO.Path.GetFileName(filePath);
                var fullDir = System.IO.Path.GetDirectoryName(filePath);
                var buildInScriptIdx = fullDir.LastIndexOf("Assets");
                var shortDir = buildInScriptIdx < 0 ? $"../{fileName}" : fullDir.Substring(buildInScriptIdx);
                var lineNo = frame.GetFileLineNumber();
                var methodLong = frame.GetMethod().Name;
                var a0 = methodLong.IndexOf('<');
                var a1 = methodLong.IndexOf('>');
                var shortName = a0 != 1 && a1 != -1 ? methodLong.Substring(a0 + 1, a1 - a0 - 1) : methodLong;
                var lineStr = $"{shortDir}:{lineNo}";

                info = $"{fileName}:{Color.yellow.ToRichText(shortName)}() (at {lineStr.Hyperlink(filePath, lineNo)})";
                return true;
            }
        }

    }

    public struct DwColorScope : System.IDisposable
    {
        readonly Color oldColor;
        readonly bool hasValue;
        public DwColorScope(Color? color)
        {
            oldColor = Gizmos.color;
            hasValue = color.HasValue;
            if (color.HasValue && color.Value != default)
                Gizmos.color = color.Value;
        }

        public void Dispose()
        {
            if (hasValue)
                Gizmos.color = oldColor;
        }
    }

    public struct DwLogScope : System.IDisposable
	{
		readonly string name;
        readonly UnityEngine.Object uobj;
		public DwLogScope(string name, UnityEngine.Object uobj)
		{
			this.name = name;
			this.uobj = uobj;
			UnityEngine.Debug.Log($"[{name}] Start", uobj);
		}
		public void Dispose()
		{
			UnityEngine.Debug.Log($"[{name}] End", uobj);
		}
	}
}