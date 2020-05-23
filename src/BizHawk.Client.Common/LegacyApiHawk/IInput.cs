#nullable enable

using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	[LegacyApiHawk]
	public interface IInput : IExternalApi
	{
		[LegacyApiHawk]
		Dictionary<string, bool> Get();

		[LegacyApiHawk]
		Dictionary<string, object> GetMouse();
	}
}
