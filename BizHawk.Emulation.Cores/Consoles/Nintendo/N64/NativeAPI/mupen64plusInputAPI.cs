using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using BizHawk.Emulation.Cores.Nintendo.N64;

namespace BizHawk.Emulation.Cores.Nintendo.N64.NativeApi
{
	class mupen64plusInputApi
	{
		IntPtr InpDll;

		[DllImport("kernel32.dll")]
		public static extern IntPtr GetProcAddress(IntPtr hModule, string procedureName);// Input plugin specific

		/// <summary>
		/// Sets a callback to use when the mupen core wants controller buttons
		/// </summary>
		/// <param name="inputCallback">The delegate to use</param>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		private delegate void SetInputCallback(InputCallback inputCallback);
		SetInputCallback InpSetInputCallback;

		/// <summary>
		/// Callback to use when mupen64plus wants input
		/// </summary>
		/// <param name="i"></param>
		/// <returns></returns>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate int InputCallback(int i);
		InputCallback InpInputCallback;

		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate mupen64plusApi.m64p_error SetRumbleCallback(RumbleCallback ParamPtr);
		SetRumbleCallback InpSetRumbleCallback;

		/// <summary>
		/// This will be called every time the N64 changes
		/// rumble
		/// </summary>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void RumbleCallback(int Control, int on);
		RumbleCallback m64pRumbleCallback;

		/// <summary>
		/// Sets the controller pak type
		/// </summary>
		/// <param name="controller">Controller id</param>
		/// <param name="type">Type id according to (well documented... hurr hurr) mupen api</param>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		public delegate void SetControllerPakType(int controller, int type);
		SetControllerPakType InpSetControllerPakType;

		/// <summary>
		/// Connects and disconnects controllers
		/// </summary>
		/// <param name="controller">Controller id</param>
		/// <param name="connected">1 if controller should be connected, 0 if controller should be disconnected</param>
		[UnmanagedFunctionPointer(CallingConvention.Cdecl)]
		delegate void SetControllerConnected(int controller, int connected);
		SetControllerConnected InpSetControllerConnected;

		/// <summary>
		/// Event fired when mupen changes rumble pak status
		/// </summary>
		event RumbleCallback OnRumbleChange;

		public mupen64plusInputApi(mupen64plusApi core)
		{
			InpDll = core.AttachPlugin(mupen64plusApi.m64p_plugin_type.M64PLUGIN_INPUT,
				"mupen64plus-input-bkm.dll");

			mupen64plusApi.m64p_error result;
			InpSetInputCallback = (SetInputCallback)Marshal.GetDelegateForFunctionPointer(GetProcAddress(InpDll, "SetInputCallback"), typeof(SetInputCallback));
			InpSetRumbleCallback = (SetRumbleCallback)Marshal.GetDelegateForFunctionPointer(GetProcAddress(InpDll, "SetRumbleCallback"), typeof(SetRumbleCallback));
			InpSetControllerPakType = (SetControllerPakType)Marshal.GetDelegateForFunctionPointer(GetProcAddress(InpDll, "SetControllerPakType"), typeof(SetControllerPakType));
			InpSetControllerConnected = (SetControllerConnected)Marshal.GetDelegateForFunctionPointer(GetProcAddress(InpDll, "SetControllerConnected"), typeof(SetControllerConnected));

			m64pRumbleCallback = new RumbleCallback(FireOnRumbleChange);
			result = InpSetRumbleCallback(m64pRumbleCallback);
		}

		public void SetM64PInputCallback(InputCallback inputCallback)
		{
			InpInputCallback = inputCallback;
			InpSetInputCallback(InpInputCallback);
		}

		private void FireOnRumbleChange(int Control, int on)
		{
			if (OnRumbleChange != null)
				OnRumbleChange(Control, on);
		}

		public void SetM64PControllerPakType(int controller, N64SyncSettings.N64ControllerSettings.N64ControllerPakType type)
		{
			InpSetControllerPakType(controller, (int)type);
		}

		public void SetM64PControllerConnected(int controller, bool connected)
		{
			InpSetControllerConnected(controller, connected?1:0);
		}
	}
}
