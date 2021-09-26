using System;
using System.Runtime.InteropServices;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	partial class MelonDS : IInputPollable
	{
		public int LagCount
		{
			get => GetLagFrameCount();
			set => SetLagFrameCount((uint)value);
		}

		public bool IsLagFrame
		{
			get => _IsLagFrame();
			set => SetIsLagFrame(value);
		}

		public IInputCallbackSystem InputCallbacks => throw new NotImplementedException();

		[DllImport(dllPath, EntryPoint = "melonds_getlagframeflag")]
		private static extern bool _IsLagFrame();

		[DllImport(dllPath, EntryPoint = "melonds_getlagframecount")]
		private static extern int GetLagFrameCount();

		[DllImport(dllPath, EntryPoint = "melonds_setlagframeflag")]
		private static extern void SetIsLagFrame(bool isLag);
		[DllImport(dllPath, EntryPoint = "melonds_setlagframecount")]
		private static extern void SetLagFrameCount(uint count);

	}
}
