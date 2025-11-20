#nullable enable

using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public interface IPhysicalInputSource
	{
		InputEvent? DequeueEvent();

		KeyValuePair<string, int>[] GetAxisValues();
	}
}
