using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public interface IInput : IExternalApi
	{
		IDictionary<string, bool> Get();

		IDictionary<string, dynamic> GetMouse();
	}
}
