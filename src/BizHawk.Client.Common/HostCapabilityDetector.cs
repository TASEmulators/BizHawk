using System;

using BizHawk.Common;

namespace BizHawk.Client.Common
{
	public static class HostCapabilityDetector
	{
		private static bool? _hasDirectX = null;

		public static bool HasDirectX => _hasDirectX ??= DetectDirectX();

		private static bool DetectDirectX()
		{
			if (OSTailoredCode.IsUnixHost) return false;
			var p = OSTailoredCode.LinkedLibManager.LoadOrZero("d3dx9_43.dll");
			if (p == IntPtr.Zero) return false;
			OSTailoredCode.LinkedLibManager.FreeByPtr(p);
			return true;
		}
	}
}
