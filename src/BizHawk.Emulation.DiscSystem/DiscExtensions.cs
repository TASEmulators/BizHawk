using System;

namespace BizHawk.Emulation.DiscSystem
{
	public static class DiscExtensions
	{
		public static Disc Create(this DiscType type, string path, Action<string> errorCallback)
		{
			return Disc.Create(type, path, errorCallback);
		}

	
	}
}
