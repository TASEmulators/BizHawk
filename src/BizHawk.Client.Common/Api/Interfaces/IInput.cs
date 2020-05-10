using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public interface IInput : IExternalApi
	{
		Dictionary<string, bool> Get();
		Dictionary<string, object> GetMouse();
	}
}
