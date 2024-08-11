using System.Runtime.CompilerServices;
using BizHawk.Common;
using NLua;

// TODO - evaluate for re-entrancy problems
namespace BizHawk.Client.Common
{
	public class LuaSandbox
	{
		private static readonly ConditionalWeakTable<LuaThread, LuaSandbox> SandboxForThread = new();

		public static Action<string> DefaultLogger { get; set; }

		public void SetSandboxCurrentDirectory(string dir)
		{
			_currentDirectory = dir;
		}

		private string _currentDirectory;

		/// <summary>
		/// Sets the OS's current working directory, with some speed hacks.
		/// </summary>
		/// <param name="path">The path which the current directory will be set to.</param>
		/// <param name="currDirSpeedHack">This method will not set the current directory if it is already set to path.
		/// If given, it will use this value instead of getting the current directory from the OS.</param>
		/// <returns>True if the current directory was sucessfully set. Otherwise, false.</returns>
		private bool CoolSetCurrentDirectory(string path, string currDirSpeedHack = null)
		{
			string target = $"{path}\\";

			// first we'll bypass it with a general hack: don't do any setting if the value's already there (even at the OS level, setting the directory can be slow)
			// yeah I know, not the smoothest move to compare strings here, in case path normalization is happening at some point
			// but you got any better ideas?
			currDirSpeedHack ??= OSTailoredCode.IsUnixHost ? Environment.CurrentDirectory : CWDHacks.Get();
			if (currDirSpeedHack == path) return true;

			if (!OSTailoredCode.IsUnixHost) return CWDHacks.Set(target);
			if (!System.IO.Directory.Exists(path)) return false; //TODO is this necessary with Mono? Linux is fine with the CWD being nonexistent. also, is this necessary with .NET Core on Windows? --yoshi
			Environment.CurrentDirectory = path;
			return true;
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
				var exStr = ex.ToString() + '\n';
				if (ex.InnerException is not null)
				{
					exStr += ex.InnerException.ToString() + '\n';
				}

				Console.Write(exStr);
				DefaultLogger(exStr);
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

		public static LuaSandbox CreateSandbox(LuaThread thread, string initialDirectory)
		{
			var sandbox = new LuaSandbox();
			SandboxForThread.Add(thread, sandbox);
			sandbox.SetSandboxCurrentDirectory(initialDirectory);
			return sandbox;
		}

		/// <exception cref="InvalidOperationException">could not get sandbox reference for thread (<see cref="CreateSandbox"/> has not been called)</exception>
		public static LuaSandbox GetSandbox(LuaThread thread)
		{
			// this is just placeholder.
			// we shouldn't be calling a sandbox with no thread--construct a sandbox properly
			if (thread is null)
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

		public static void Sandbox(LuaThread thread, Action callback, Action exceptionCallback = null)
		{
			GetSandbox(thread).Sandbox(callback, exceptionCallback);
		}
	}
}
