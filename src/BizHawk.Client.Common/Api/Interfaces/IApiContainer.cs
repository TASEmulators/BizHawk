using System;
using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public interface IApiContainer
	{
		Dictionary<Type, IExternalApi> Libraries { get; set; }
	}
}
