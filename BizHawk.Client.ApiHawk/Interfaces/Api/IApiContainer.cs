using System;
using System.Collections.Generic;

namespace BizHawk.Client.ApiHawk
{
	public interface IApiContainer
	{
		Dictionary<Type, IExternalApi> Libraries { get; set; }
	}
}
