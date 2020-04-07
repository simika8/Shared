using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;

namespace LX.Common
{
#if EXTERN
	public
#else
	internal
#endif
	static class ConsoleManager
	{
		[Flags]
		private enum DesiredAccess : uint
		{
			GenericRead = 0x80000000,
			GenericWrite = 0x40000000,
			GenericExecute = 0x20000000,
			GenericAll = 0x10000000
		}

		private enum StdHandle : int
		{
			Input = -10,
			Output = -11,
			Error = -12
		}

		private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 4;

		[SuppressUnmanagedCodeSecurity]
		private static class NativeMethods
		{
			private const string Kernel32DllName = "kernel32.dll";

			[DllImport(Kernel32DllName, SetLastError = true)]
			[return: MarshalAs(UnmanagedType.Bool)]
			public static extern bool AllocConsole();

			[DllImport(Kernel32DllName, SetLastError = true)]
			public static extern bool FreeConsole();

			[DllImport(Kernel32DllName, SetLastError = true)]
			public static extern IntPtr GetConsoleWindow();

			[DllImport(Kernel32DllName, SetLastError = true)]
			public static extern bool SetStdHandle(StdHandle nStdHandle, IntPtr hHandle);

			[DllImport(Kernel32DllName, SetLastError = true, CharSet = CharSet.Auto)]
			public static extern IntPtr CreateFile
				(
				[MarshalAs(UnmanagedType.LPWStr)] string lpFileName
				, [MarshalAs(UnmanagedType.U4)] DesiredAccess dwDesiredAccess
				, [MarshalAs(UnmanagedType.U4)] FileShare dwShareMode
				, IntPtr lpSecurityAttributes // optional SECURITY_ATTRIBUTES struct or IntPtr.Zero
				, [MarshalAs(UnmanagedType.U4)] FileMode dwCreationDisposition
				, [MarshalAs(UnmanagedType.U4)] FileAttributes dwFlagsAndAttributes
				, IntPtr hTemplateFile
				);

			[DllImport("kernel32.dll", SetLastError = true)]
			public static extern IntPtr GetStdHandle(StdHandle nStdHandle);

			[DllImport("kernel32.dll")]
			public static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

			[DllImport("kernel32.dll")]
			public static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);
		}

		private static readonly IntPtr s_invalidHandleValue = new IntPtr(-1);

		private static bool HasConsole
			=> NativeMethods.GetConsoleWindow() != IntPtr.Zero;

		private static IntPtr GetConsoleStandardInput()
		{
			var handle = NativeMethods.CreateFile
				("CONIN$"
				, DesiredAccess.GenericRead | DesiredAccess.GenericWrite
				, FileShare.ReadWrite
				, IntPtr.Zero
				, FileMode.Open
				, FileAttributes.Normal
				, IntPtr.Zero
				);

			if (handle == s_invalidHandleValue)
			{
				throw new Win32Exception();
			}

			return handle;
		}

		private static IntPtr GetConsoleStandardOutput()
		{
			var handle = NativeMethods.CreateFile
				("CONOUT$"
				, DesiredAccess.GenericWrite | DesiredAccess.GenericWrite
				, FileShare.ReadWrite
				, IntPtr.Zero
				, FileMode.Open
				, FileAttributes.Normal
				, IntPtr.Zero
				);

			if (handle == s_invalidHandleValue)
			{
				throw new Win32Exception();
			}

			return handle;
		}

		private static void SetupCmd()
		{
			NativeMethods.SetStdHandle(StdHandle.Output, GetConsoleStandardOutput());
			NativeMethods.SetStdHandle(StdHandle.Error, GetConsoleStandardOutput());
			NativeMethods.SetStdHandle(StdHandle.Input, GetConsoleStandardInput());
		}

		/// <summary>
		/// Új parancssori ablakot indít és beállítja a standard in-/outputot és error outputot
		/// </summary>
		public static void Show()
		{
			if (HasConsole)
			{
				return;
			}

			if (!NativeMethods.AllocConsole())
			{
				throw new Win32Exception();
			}

			SetupCmd();
		}

		/// <summary>
		/// Ha van parancssoi ablak, akkor leválasztja róla a Console osztályt és felszabadítja az ablakot
		/// </summary>
		public static void Hide()
		{
			if (!HasConsole)
			{
				return;
			}

			NativeMethods.FreeConsole();
		}
		/// <summary>
		/// ld. Show() és Hide()
		/// </summary>
		public static void Toggle()
		{
			if (HasConsole)
			{
				Hide();
			}
			else
			{
				Show();
			}
		}

		/// <summary>
		/// Virtuális terminál mód engedélyezése
		/// </summary>
		public static void EnableVtMode()
		{
			var handle = NativeMethods.GetStdHandle(StdHandle.Output);
			NativeMethods.GetConsoleMode(handle, out uint mode);
			mode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;
			NativeMethods.SetConsoleMode(handle, mode);
		}

		/// <summary>
		/// Virtuális terminál mód kikapcsolása
		/// </summary>
		public static void DisableVtMode()
		{
			var handle = NativeMethods.GetStdHandle(StdHandle.Output);
			NativeMethods.GetConsoleMode(handle, out uint mode);
			mode &= ~ENABLE_VIRTUAL_TERMINAL_PROCESSING;
			NativeMethods.SetConsoleMode(handle, mode);
		}
	}
}
