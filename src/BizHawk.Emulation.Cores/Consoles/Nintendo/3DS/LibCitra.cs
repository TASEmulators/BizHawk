using System;
using System.Runtime.InteropServices;

using BizHawk.BizInvoke;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo._3DS
{
	public abstract class LibCitra
	{
		private const CallingConvention cc = CallingConvention.Cdecl;

		[UnmanagedFunctionPointer(cc)]
		public delegate bool GetBooleanSettingCallback(string label);
		
		[UnmanagedFunctionPointer(cc)]
		public delegate ulong GetIntegerSettingCallback(string label);
		
		[UnmanagedFunctionPointer(cc)]
		public delegate double GetFloatSettingCallback(string label);
		
		[UnmanagedFunctionPointer(cc)]
		public delegate void GetStringSettingCallback(string label, IntPtr buffer, int bufferSize);

		[StructLayout(LayoutKind.Sequential)]
		public struct ConfigCallbackInterface
		{
			public GetBooleanSettingCallback GetBoolean;
			public GetIntegerSettingCallback GetInteger;
			public GetFloatSettingCallback GetFloat;
			public GetStringSettingCallback GetString;
		}
		
		[UnmanagedFunctionPointer(cc)]
		public delegate IntPtr RequestGLContextCallback();

		[UnmanagedFunctionPointer(cc)]
		public delegate void ReleaseGLContextCallback(IntPtr context);

		[UnmanagedFunctionPointer(cc)]
		public delegate void ActivateGLContextCallback(IntPtr context);

		[UnmanagedFunctionPointer(cc)]
		public delegate IntPtr GetGLProcAddressCallback(string proc);

		[StructLayout(LayoutKind.Sequential)]
		public struct GLCallbackInterface
		{
			public RequestGLContextCallback RequestGLContext;
			public ReleaseGLContextCallback ReleaseGLContext;
			public ActivateGLContextCallback ActivateGLContext;
			public GetGLProcAddressCallback GetGLProcAddress;
		}

		public enum Buttons
		{
			A,
			B,
			X,
			Y,
			Up,
			Down,
			Left,
			Right,
			L,
			R,
			Start,
			Select,
			Debug,
			Gpio14,
			ZL,
			ZR,
			Home,
		}

		public enum AnalogSticks
		{
			CirclePad,
			CStick,
		}

		public enum Axes
		{
			X,
			Y
		}

		[UnmanagedFunctionPointer(cc)]
		public delegate bool GetButtonCallback(Buttons button);

		[UnmanagedFunctionPointer(cc)]
		public delegate void GetAxisCallback(AnalogSticks stick, out float x, out float y);

		[UnmanagedFunctionPointer(cc)]
		public delegate bool GetTouchCallback(out float x, out float y);

		[UnmanagedFunctionPointer(cc)]
		public delegate void GetMotionCallback(out float accelX, out float accelY, out float accelZ, out float gyroX, out float gyroY, out float gyroZ);

		[StructLayout(LayoutKind.Sequential)]
		public struct InputCallbackInterface
		{
			public GetButtonCallback GetButton;
			public GetAxisCallback GetAxis;
			public GetTouchCallback GetTouch;
			public GetMotionCallback GetMotion;
		}

		[BizImport(cc, Compatibility = true)]
		public abstract IntPtr Citra_CreateContext(
			ref ConfigCallbackInterface configCallbackInterface,
			ref GLCallbackInterface glCallbackInterface,
			ref InputCallbackInterface inputCallbackInterface);

		[BizImport(cc)]
		public abstract void Citra_DestroyContext(IntPtr context);

		[BizImport(cc)]
		public abstract bool Citra_InstallCIA(IntPtr context, string ciaPath, bool force, byte[] messageBuffer, int messageBufferLen);

		[BizImport(cc)]
		public abstract bool Citra_LoadROM(IntPtr context, string romPath, byte[] errorMessageBuffer, int errorMessageBufferLen);

		[BizImport(cc)]
		public abstract void Citra_RunFrame(IntPtr context);

		[BizImport(cc)]
		public abstract void Citra_Reset(IntPtr context);

		[BizImport(cc)]
		public abstract void Citra_GetVideoDimensions(IntPtr context, out int width, out int height);

		[BizImport(cc)]
		public abstract int Citra_GetGLTexture(IntPtr context);

		[BizImport(cc)]
		public abstract void Citra_ReadFrameBuffer(IntPtr context, int[] buffer);

		[BizImport(cc)]
		public abstract void Citra_GetAudio(IntPtr context, out IntPtr buffer, out int frames);

		[BizImport(cc)]
		public abstract void Citra_ReloadConfig(IntPtr context);

		[BizImport(cc)]
		public abstract int Citra_StartSaveState(IntPtr context);

		[BizImport(cc)]
		public abstract void Citra_FinishSaveState(IntPtr context, byte[] buffer);

		[BizImport(cc)]
		public abstract void Citra_LoadState(IntPtr context, byte[] buffer, int stateLen);
	}
}
