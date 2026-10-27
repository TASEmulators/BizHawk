using System.Runtime.InteropServices;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// Lua access to screen-reader speech and braille output, for blind accessibility. Backed by the
	/// prism screen-reader abstraction library (https://github.com/ethindp/prism, MPL-2.0), which routes
	/// to the user's active screen reader (NVDA, JAWS, Narrator/OneCore, ...) and falls back to SAPI.
	/// </summary>
	[LuaLibrary(released: true)]
	public sealed class SpeechLuaLibrary : LuaLibraryBase
	{
		public SpeechLuaLibrary(ILuaLibraries luaLibsImpl, ApiContainer apiContainer, Action<string> logOutputCallback)
			: base(luaLibsImpl, apiContainer, logOutputCallback) {}

		public override string Name => "speech";

		[LuaMethodExample("speech.say(\"Charizard, 142 of 142 HP\");")]
		[LuaMethod("say", "Speaks the given text through the active screen reader (or SAPI fallback). When interrupt is true (the default), any current speech is cancelled first.")]
		public void Say(string text, bool interrupt = true)
		{
			if (!PrismSpeech.Speak(text ?? string.Empty, interrupt, out var error)) Log($"speech.say failed: {error}");
		}

		[LuaMethodExample("speech.output(\"State 1 saved\");")]
		[LuaMethod("output", "Speaks and brailles the given text (whichever the active screen reader supports). interrupt defaults to true.")]
		public void Output(string text, bool interrupt = true)
		{
			if (!PrismSpeech.Output(text ?? string.Empty, interrupt, out var error)) Log($"speech.output failed: {error}");
		}

		[LuaMethodExample("speech.braille(\"HP 142/142\");")]
		[LuaMethod("braille", "Sends the given text to a connected braille display via the active screen reader, if supported.")]
		public void Braille(string text)
		{
			if (!PrismSpeech.Braille(text ?? string.Empty, out var error)) Log($"speech.braille failed: {error}");
		}

		[LuaMethodExample("speech.stop();")]
		[LuaMethod("stop", "Stops/silences any speech currently in progress.")]
		public void Stop()
		{
			if (!PrismSpeech.Stop(out var error)) Log($"speech.stop failed: {error}");
		}
	}

	/// <summary>
	/// Minimal P/Invoke wrapper around prism's C API (prism.dll, shipped in dll/). Initialised lazily on
	/// first use and kept for the process lifetime. If prism or a speech backend isn't available, every
	/// call becomes a no-op that returns an error string, so missing DLLs never crash EmuHawk.
	/// </summary>
	internal static class PrismSpeech
	{
		// PrismConfig is { uint8_t version }; current ABI version is PRISM_CONFIG_VERSION (2) for the
		// prism.dll we ship. (See prism.h.)
		private const byte ConfigVersion = 2;

		[StructLayout(LayoutKind.Sequential)]
		private struct PrismConfig { public byte Version; }

		[DllImport("prism", EntryPoint = "prism_init", CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr PrismInit(ref PrismConfig cfg);

		[DllImport("prism", EntryPoint = "prism_registry_acquire_best", CallingConvention = CallingConvention.Cdecl)]
		private static extern IntPtr PrismRegistryAcquireBest(IntPtr ctx);

		[DllImport("prism", EntryPoint = "prism_backend_initialize", CallingConvention = CallingConvention.Cdecl)]
		private static extern int PrismBackendInitialize(IntPtr backend);

		[DllImport("prism", EntryPoint = "prism_backend_speak", CallingConvention = CallingConvention.Cdecl)]
		private static extern int PrismBackendSpeak(IntPtr backend, [MarshalAs(UnmanagedType.LPUTF8Str)] string text, [MarshalAs(UnmanagedType.I1)] bool interrupt);

		[DllImport("prism", EntryPoint = "prism_backend_output", CallingConvention = CallingConvention.Cdecl)]
		private static extern int PrismBackendOutput(IntPtr backend, [MarshalAs(UnmanagedType.LPUTF8Str)] string text, [MarshalAs(UnmanagedType.I1)] bool interrupt);

		[DllImport("prism", EntryPoint = "prism_backend_braille", CallingConvention = CallingConvention.Cdecl)]
		private static extern int PrismBackendBraille(IntPtr backend, [MarshalAs(UnmanagedType.LPUTF8Str)] string text);

		[DllImport("prism", EntryPoint = "prism_backend_stop", CallingConvention = CallingConvention.Cdecl)]
		private static extern int PrismBackendStop(IntPtr backend);

		private const int PRISM_OK = 0;
		private const int PRISM_ERROR_ALREADY_INITIALIZED = 15; // prism_registry_acquire_best already returns an initialised backend

		private static readonly object _lock = new();
		private static bool _tried;
		private static IntPtr _backend;
		private static string _initError;

		private static bool EnsureInit(out string error)
		{
			lock (_lock)
			{
				if (_backend != IntPtr.Zero) { error = null; return true; }
				if (_tried) { error = _initError; return false; }
				_tried = true;
				try
				{
					var cfg = new PrismConfig { Version = ConfigVersion };
					var ctx = PrismInit(ref cfg);
					if (ctx == IntPtr.Zero) { _initError = "prism_init returned null"; error = _initError; return false; }

					var backend = PrismRegistryAcquireBest(ctx);
					if (backend == IntPtr.Zero) { _initError = "no speech backend available"; error = _initError; return false; }

					var rc = PrismBackendInitialize(backend);
					if (rc != PRISM_OK && rc != PRISM_ERROR_ALREADY_INITIALIZED) { _initError = $"prism_backend_initialize returned {rc}"; error = _initError; return false; }

					_backend = backend;
					error = null;
					return true;
				}
				catch (Exception ex)
				{
					_initError = ex.Message;
					error = _initError;
					return false;
				}
			}
		}

		public static bool Speak(string text, bool interrupt, out string error)
		{
			if (!EnsureInit(out error)) return false;
			try { return PrismBackendSpeak(_backend, text, interrupt) == PRISM_OK; }
			catch (Exception ex) { error = ex.Message; return false; }
		}

		public static bool Output(string text, bool interrupt, out string error)
		{
			if (!EnsureInit(out error)) return false;
			try { return PrismBackendOutput(_backend, text, interrupt) == PRISM_OK; }
			catch (Exception ex) { error = ex.Message; return false; }
		}

		public static bool Braille(string text, out string error)
		{
			if (!EnsureInit(out error)) return false;
			try { return PrismBackendBraille(_backend, text) == PRISM_OK; }
			catch (Exception ex) { error = ex.Message; return false; }
		}

		public static bool Stop(out string error)
		{
			if (!EnsureInit(out error)) return false;
			try { return PrismBackendStop(_backend) == PRISM_OK; }
			catch (Exception ex) { error = ex.Message; return false; }
		}
	}
}
