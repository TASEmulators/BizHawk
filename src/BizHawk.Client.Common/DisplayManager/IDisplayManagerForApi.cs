using System;
using System.Drawing.Text;

namespace BizHawk.Client.Common
{
	public interface IDisplayManagerForApi
	{
		PrivateFontCollection CustomFonts { get; }

		OSDManager OSD { get; }

		/// <summary>locks the lua surface called <paramref name="name"/></summary>
		/// <exception cref="InvalidOperationException">already locked, or unknown surface</exception>
		DisplaySurface LockLuaSurface(string name, bool clear = true);

		/// <summary>unlocks this DisplaySurface which had better have been locked as a lua surface</summary>
		/// <exception cref="InvalidOperationException">already unlocked</exception>
		void UnlockLuaSurface(DisplaySurface surface);
	}
}
