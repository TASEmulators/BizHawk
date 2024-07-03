using BizHawk.Common;

namespace BizHawk.Client.Common
{
	public static class HostCapabilityDetector
	{
		private static readonly Lazy<bool> _hasD3D11 = new(() =>
		{
			if (OSTailoredCode.IsUnixHost) return false;
			var p = OSTailoredCode.LinkedLibManager.LoadOrZero("d3d11.dll");
			if (p == IntPtr.Zero) return false;
			OSTailoredCode.LinkedLibManager.FreeByPtr(p);
			return true;
		});

		private static readonly Lazy<bool> _hasXAudio2 = new(() =>
		{
			if (OSTailoredCode.IsUnixHost)
			{
				return false;
			}

			// This should always work for anything Windows 8+ (where XAudio 2.8/2.9 is built-in)
			var libNames = new[] { "xaudio2_9.dll", "xaudio2_8.dll", "xaudio2_9redist.dll" };
			foreach (var libName in libNames)
			{
				var p = OSTailoredCode.LinkedLibManager.LoadOrZero(libName);
				if (p != IntPtr.Zero)
				{
					OSTailoredCode.LinkedLibManager.FreeByPtr(p);
					return true;
				}
			}

			return false;
		});

		public static bool HasD3D11 => _hasD3D11.Value;
		public static bool HasXAudio2 => _hasXAudio2.Value;
	}
}
