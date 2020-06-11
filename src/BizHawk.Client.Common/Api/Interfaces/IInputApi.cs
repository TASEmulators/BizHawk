using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public interface IInputApi : IExternalApi
	{
		Dictionary<string, bool> Get();
		Dictionary<string, object> GetMouse();
	}
}
