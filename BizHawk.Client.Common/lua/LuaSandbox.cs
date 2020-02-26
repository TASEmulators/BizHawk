using System;
using System.Runtime.CompilerServices;
using BizHawk.Common;
using NLua;

// TODO - evaluate for re-entrancy problems
namespace BizHawk.Client.Common
{
	public unsafe class LuaSandbox
	{
		private static readonly ConditionalWeakTable<Lua, LuaSandbox> SandboxForThread = new ConditionalWeakTable<Lua, LuaSandbox>();

		public static Action<string> DefaultLogger { get; set; }

		public void SetSandboxCurrentDirectory(string dir)
		{
			_currentDirectory = dir;
		}

		private string _currentDirectory;

		private bool CoolSetCurrentDirectory(string path, string currDirSpeedHack = null)
		{
			static string CoolGetCurrentDirectory()
			{
				if (OSTailoredCode.IsUnixHost) return Environment.CurrentDirectory;

				//HACK to bypass Windows security checks triggered by *getting* the current directory (why), which only slow us down
				var buf = new byte[32768];
				fixed (byte* pBuf = &buf[0])
					return System.Text.Encoding.Unicode.GetString(buf, 0, 2 * (int) Win32Imports.GetCurrentDirectoryW(32767, pBuf));
			}

			string target = $"{_currentDirectory}\\";

			// first we'll bypass it with a general hack: don't do any setting if the value's already there (even at the OS level, setting the directory can be slow)
			// yeah I know, not the smoothest move to compare strings here, in case path normalization is happening at some point
			// but you got any better ideas?
			if (currDirSpeedHack == null)
			{
				currDirSpeedHack = CoolGetCurrentDirectory();
			}

			if (currDirSpeedHack == path)
			{
				return true;
			}

			if (OSTailoredCode.IsUnixHost)
			{
				if (System.IO.Directory.Exists(_currentDirectory)) //TODO is this necessary with Mono? extra TODO: is this necessary with .NET Core on Windows?
				{
					Environment.CurrentDirectory = _currentDirectory;
					return true;
				}

				return false;
			}

			//HACK to bypass Windows security checks triggered by setting the current directory, which only slow us down
			fixed (byte* pstr = &System.Text.Encoding.Unicode.GetBytes($"{target}\0")[0])
				return Win32Imports.SetCurrentDirectoryW(pstr);
		}

		private void Sandbox(Action callback, Action exceptionCallback)
		{
			string savedEnvironmentCurrDir = null;
			try
			{
				savedEnvironmentCurrDir = Environment.CurrentDirectory;

				if (_currentDirectory != null)
				{
					CoolSetCurrentDirectory(_currentDirectory, savedEnvironmentCurrDir);
				}

				EnvironmentSandbox.Sandbox(callback);
			}
			catch (NLua.Exceptions.LuaException ex)
			{
				Console.WriteLine(ex);
				DefaultLogger(ex.ToString());
				exceptionCallback?.Invoke();
			}
			finally
			{
				if (_currentDirectory != null)
				{
					CoolSetCurrentDirectory(savedEnvironmentCurrDir);
				}
			}
		}

		public static LuaSandbox CreateSandbox(Lua thread, string initialDirectory)
		{
			var sandbox = new LuaSandbox();
			SandboxForThread.Add(thread, sandbox);
			sandbox.SetSandboxCurrentDirectory(initialDirectory);
			return sandbox;
		}

		/// <exception cref="InvalidOperationException">could not get sandbox reference for thread (<see cref="CreateSandbox"/> has not been called)</exception>
		public static LuaSandbox GetSandbox(Lua thread)
		{
			// this is just placeholder.
			// we shouldn't be calling a sandbox with no thread--construct a sandbox properly
			if (thread == null)
			{
				return new LuaSandbox();
			}

			lock (SandboxForThread)
			{
				if (SandboxForThread.TryGetValue(thread, out var sandbox))
				{
					return sandbox;
				}
				
				// for now: throw exception (I want to manually creating them)
				// return CreateSandbox(thread);
				throw new InvalidOperationException("HOARY GORILLA HIJINX");
			}
		}

		public static void Sandbox(Lua thread, Action callback, Action exceptionCallback = null)
		{
			GetSandbox(thread).Sandbox(callback, exceptionCallback);
		}
	}
}
