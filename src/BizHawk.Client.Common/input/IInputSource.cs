#nullable enable

using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public interface IInputSource
	{
		InputEvent? DequeueEvent();

		KeyValuePair<string, int>[] GetAxisValues();
	}
}
