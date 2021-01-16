using System;
using System.Drawing.Text;

namespace BizHawk.Client.Common
{
	public interface IDisplayManagerForApi
	{
		PrivateFontCollection CustomFonts { get; }

		OSDManager OSD { get; }

		/// <summary>locks the surface with ID <paramref name="surfaceID"/></summary>
		/// <exception cref="InvalidOperationException">already locked, or unknown surface</exception>
		DisplaySurface LockApiHawkSurface(DisplaySurfaceID surfaceID, bool clear = true);

		/// <summary>unlocks the given <paramref name="surface"/>, which must be a locked surface produced by <see cref="LockApiHawkSurface"/></summary>
		/// <exception cref="InvalidOperationException">already unlocked</exception>
		void UnlockApiHawkSurface(DisplaySurface surface);
	}
}
