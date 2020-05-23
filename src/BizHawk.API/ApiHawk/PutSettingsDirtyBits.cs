using System;

namespace BizHawk.API.ApiHawk
{
	/// <remarks>
	/// note: this is a bit of a frail API. If a frontend wants a new flag, cores won't know to yea or nay it<br/>
	/// this could be solved by adding a KnownSettingsDirtyBits on the settings interface
	/// or, in a pinch, the same thing could be done with THESE flags, so that the interface doesn't
	/// change but newly-aware cores can simply manifest that they know about more bits, in the same variable they return the bits in
	/// </remarks>
	[Flags]
	public enum PutSettingsDirtyBits : ulong
	{
		None = 0,
		RebootCore = 1,
		ScreenLayoutChanged = 2,
	}
}
