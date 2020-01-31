using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using BizHawk.Emulation.Common;
using BizHawk.Client.ApiHawk;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public static class ApiManager
	{
		private static readonly Dictionary<Type, IExternalApi> Libraries = new Dictionary<Type, IExternalApi>();

		public static IExternalApiProvider Restart(IEmulatorServiceProvider serviceProvider)
		{
			Libraries.Clear();
			foreach (var api1 in Assembly.Load("BizHawk.Client.Common").GetTypes()
				.Concat(Assembly.Load("BizHawk.Client.ApiHawk").GetTypes())
				.Concat(Assembly.GetExecutingAssembly().GetTypes())
				.Where(t => typeof(IExternalApi).IsAssignableFrom(t) && t.IsSealed && ServiceInjector.IsAvailable(serviceProvider, t)))
			{
				var instance1 = (IExternalApi)Activator.CreateInstance(api1);
				ServiceInjector.UpdateServices(serviceProvider, instance1);
				Libraries.Add(api1, instance1);
			}
			return new BasicApiProvider(Libraries);
		}
	}
}
