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
		private static ApiContainer _container;
		private static IExternalApiProvider Register(IEmulatorServiceProvider serviceProvider)
		{
			foreach (var api in Assembly.Load("BizHawk.Client.Common").GetTypes()
				.Concat(Assembly.Load("BizHawk.Client.ApiHawk").GetTypes())
				.Concat(Assembly.GetAssembly(typeof(ApiContainer)).GetTypes())
				.Where(t => typeof(IExternalApi).IsAssignableFrom(t) && t.IsSealed && ServiceInjector.IsAvailable(serviceProvider, t)))
			{
				var instance = (IExternalApi)Activator.CreateInstance(api);
				ServiceInjector.UpdateServices(serviceProvider, instance);
				Libraries.Add(api, instance);
			}
			_container = new ApiContainer(Libraries);
			return new BasicApiProvider(_container);
		}

		private static readonly Dictionary<Type, IExternalApi> Libraries = new Dictionary<Type, IExternalApi>();
		public static IExternalApiProvider Restart(IEmulatorServiceProvider newServiceProvider)
		{
			Libraries.Clear();
			return Register(newServiceProvider);
		}
	}
}
