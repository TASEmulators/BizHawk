using System;
using System.Runtime.InteropServices;
using BizHawk.Emulation.Cores.Nintendo.N64;

namespace BizHawk.Emulation.Cores.Nintendo.N64.NativeApi
{
	class mupen64plusAudioApi
	{
		/// <summary>
		/// Handle to native audio plugin
		/// </summary>
		private IntPtr AudDll;

		[DllImport("kernel32.dll")]
		public static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);

		/// <summary>
		/// Gets the size of the mupen64plus audio buffer
		/// </summary>
		/// <returns>The size of the mupen64plus audio buffer</returns>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate int GetBufferSize();
		GetBufferSize dllGetBufferSize;

		/// <summary>
		/// Gets the audio buffer from mupen64plus, and then clears it
		/// </summary>
		/// <param name="dest">The buffer to fill with samples</param>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void ReadAudioBuffer(short[] dest);
		ReadAudioBuffer dllReadAudioBuffer;

		/// <summary>
		/// Gets the current audio rate from mupen64plus
		/// </summary>
		/// <returns>The current audio rate</returns>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate int GetAudioRate();
		GetAudioRate dllGetAudioRate;

		/// <summary>
		/// Loads native functions and attaches itself to the core
		/// </summary>
		/// <param name="core">Core with loaded core api</param>
		public mupen64plusAudioApi(mupen64plusApi core)
		{
			AudDll = core.AttachPlugin(mupen64plusApi.m64p_plugin_type.M64PLUGIN_AUDIO,
				"mupen64plus-audio-bkm.dll");

			// Connect dll functions
			dllGetBufferSize = (GetBufferSize)Marshal.GetDelegateForFunctionPointer(GetProcAddress(AudDll, "GetBufferSize"), typeof(GetBufferSize));
			dllReadAudioBuffer = (ReadAudioBuffer)Marshal.GetDelegateForFunctionPointer(GetProcAddress(AudDll, "ReadAudioBuffer"), typeof(ReadAudioBuffer));
			dllGetAudioRate = (GetAudioRate)Marshal.GetDelegateForFunctionPointer(GetProcAddress(AudDll, "GetAudioRate"), typeof(GetAudioRate));
		}

		/// <summary>
		/// Returns currently used sampling rate
		/// </summary>
		/// <returns></returns>
		public uint GetSamplingRate()
		{
			return (uint)dllGetAudioRate();
		}

		/// <summary>
		/// Returns size of bytes currently in the audio buffer
		/// </summary>
		/// <returns></returns>
		public int GetAudioBufferSize()
		{
			return dllGetBufferSize();
		}

		/// <summary>
		/// Returns bytes currently in the audiobuffer
		/// Afterwards audio buffer is cleared
		/// buffer.Length must be greater than GetAudioBufferSize()
		/// </summary>
		/// <param name="buffer"></param>
		public void GetAudioBuffer(short[] buffer)
		{
			dllReadAudioBuffer(buffer);
		}
	}
}
