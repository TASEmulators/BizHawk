using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Cores.Nintendo.N64.NativeApi
{
	internal class mupen64plusAudioApi
	{
		/// <summary>
		/// Handle to native audio plugin
		/// </summary>
		private IntPtr AudDll;

		/// <summary>
		/// Gets the size of the mupen64plus audio buffer
		/// </summary>
		/// <returns>The size of the mupen64plus audio buffer</returns>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate int GetBufferSize();

		private readonly GetBufferSize dllGetBufferSize;

		/// <summary>
		/// Gets the audio buffer from mupen64plus, and then clears it
		/// </summary>
		/// <param name="dest">The buffer to fill with samples</param>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void ReadAudioBuffer(short[] dest);

		private readonly ReadAudioBuffer dllReadAudioBuffer;

		/// <summary>
		/// Gets the current audio rate from mupen64plus
		/// </summary>
		/// <returns>The current audio rate</returns>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate int GetAudioRate();

		private readonly GetAudioRate dllGetAudioRate;

		/// <summary>
		/// Loads native functions and attaches itself to the core
		/// </summary>
		/// <param name="core">Core with loaded core api</param>
		public mupen64plusAudioApi(mupen64plusApi core)
		{
			T GetAudioDelegate<T>(string proc) where T : Delegate => mupen64plusApi.GetTypedDelegate<T>(AudDll, proc);

			AudDll = core.AttachPlugin(mupen64plusApi.m64p_plugin_type.M64PLUGIN_AUDIO,
				"mupen64plus-audio-bkm.dll");

			// Connect dll functions
			dllGetBufferSize = GetAudioDelegate<GetBufferSize>("GetBufferSize");
			dllReadAudioBuffer = GetAudioDelegate<ReadAudioBuffer>("ReadAudioBuffer");
			dllGetAudioRate = GetAudioDelegate<GetAudioRate>("GetAudioRate");
		}

		/// <summary>
		/// Returns currently used sampling rate
		/// </summary>
		public int GetSamplingRate()
		{
			return dllGetAudioRate();
		}

		/// <summary>
		/// Returns size of bytes currently in the audio buffer
		/// </summary>
		public int GetAudioBufferSize()
		{
			return dllGetBufferSize();
		}

		/// <summary>
		/// Returns bytes currently in the audiobuffer
		/// Afterwards audio buffer is cleared
		/// buffer.Length must be greater than GetAudioBufferSize()
		/// </summary>
		public void GetAudioBuffer(short[] buffer)
		{
			dllReadAudioBuffer(buffer);
		}
	}
}
