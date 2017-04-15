using System;
using System.Runtime.InteropServices;
using LuaInterface;

// TODO - evaluate for re-entrancy problems
namespace BizHawk.Client.Common
{
	public unsafe class LuaSandbox
	{
		protected static Action<string> Logger;

		static System.Runtime.CompilerServices.ConditionalWeakTable<Lua, LuaSandbox> SandboxForThread = new System.Runtime.CompilerServices.ConditionalWeakTable<Lua, LuaSandbox>();
		public static Action<string> DefaultLogger;

		public void SetLogger(Action<string> logger)
		{
			Logger = logger;
		}

		public void SetSandboxCurrentDirectory(string dir)
		{
			CurrentDirectory = dir;
		}

		string CurrentDirectory;

		#if WINDOWS
		[DllImport("kernel32.dll", SetLastError = true)]
		static extern bool SetCurrentDirectoryW(byte* lpPathName);
		[DllImport("kernel32.dll", SetLastError=true)]
		static extern uint GetCurrentDirectoryW(uint nBufferLength, byte* pBuffer);
		#endif

		bool CoolSetCurrentDirectory(string path, string currDirSpeedHack = null)
		{
			string target = CurrentDirectory + "\\";

			// first we'll bypass it with a general hack: dont do any setting if the value's already there (even at the OS level, setting the directory can be slow)
			// yeah I know, not the smoothest move to compare strings here, in case path normalization is happening at some point
			// but you got any better ideas?
			if (currDirSpeedHack == null)
				currDirSpeedHack = CoolGetCurrentDirectory();
			if (currDirSpeedHack == path)
				return true;

			//WARNING: setting the current directory is SLOW!!! security checks for some reason.
			//so we're bypassing it with windows hacks
			#if WINDOWS
				fixed (byte* pstr = &System.Text.Encoding.Unicode.GetBytes(target + "\0")[0])
					return SetCurrentDirectoryW(pstr);
			#else
				if(System.IO.Directory.Exists(CurrentDirectory)) //race condition for great justice
				{
					Environment.CurrentDirectory = CurrentDirectory; //thats right, you can't set a directory as current that doesnt exist because .net's got to do SENSELESS SLOW-ASS SECURITY CHECKS on it and it can't do that on a NONEXISTENT DIRECTORY
					return true;
				}
				else return false
			#endif
		}

		string CoolGetCurrentDirectory()
		{
			//GUESS WHAT!
			//.NET DOES A SECURITY CHECK ON THE DIRECTORY WE JUST RETRIEVED
			//AS IF ASKING FOR THE CURRENT DIRECTORY IS EQUIVALENT TO TRYING TO ACCESS IT
			//SCREW YOU
			#if WINDOWS
				var buf = new byte[32768];
				fixed(byte* pBuf = &buf[0])
				{
					uint ret = GetCurrentDirectoryW(32767, pBuf);
					return System.Text.Encoding.Unicode.GetString(buf, 0, (int)ret*2);
				}
			#else
				return Environment.CurrentDirectory;
			#endif
		}

		void Sandbox(Action callback, Action exceptionCallback)
		{
			string savedEnvironmentCurrDir = null;
			try
			{
				savedEnvironmentCurrDir = Environment.CurrentDirectory;

				if (CurrentDirectory != null)
					CoolSetCurrentDirectory(CurrentDirectory, savedEnvironmentCurrDir);

				EnvironmentSandbox.Sandbox(callback);
			}
			catch (LuaException ex)
			{
				Console.WriteLine(ex);
				Logger(ex.ToString());
				exceptionCallback?.Invoke();
			}
			finally
			{
				if (CurrentDirectory != null)
					CoolSetCurrentDirectory(savedEnvironmentCurrDir);
			}
		}

		public static LuaSandbox CreateSandbox(Lua thread, string initialDirectory)
		{
			var sandbox = new LuaSandbox();
			SandboxForThread.Add(thread, sandbox);
			sandbox.SetSandboxCurrentDirectory(initialDirectory);
			sandbox.SetLogger(DefaultLogger);
			return sandbox;
		}

		public static LuaSandbox GetSandbox(Lua thread)
		{
			// this is just placeholder.
			// we shouldnt be calling a sandbox with no thread--construct a sandbox properly
			if (thread == null)
			{
				return new LuaSandbox();
			}

			lock (SandboxForThread)
			{
				LuaSandbox sandbox;
				if (SandboxForThread.TryGetValue(thread, out sandbox))
					return sandbox;
				else
				{
					// for now: throw exception (I want to manually creating them)
					// return CreateSandbox(thread);
					throw new InvalidOperationException("HOARY GORILLA HIJINX");
				}
			}
		}

		public static void Sandbox(Lua thread, Action callback, Action exceptionCallback = null)
		{
			GetSandbox(thread).Sandbox(callback, exceptionCallback);
		}
	}
}
