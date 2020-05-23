using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using BizHawk.Emulation.Common;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk

{
	public static class ApiManager
	{
		/// <remarks>TODO do we need to keep this reference around? --yoshi</remarks>
		private static ApiContainer _container;

		private static IExternalApiProvider Register(IEmulatorServiceProvider serviceProvider)
		{
			foreach (var api in Assembly.GetAssembly(typeof(ApiSubsetContainer)).GetTypes()
				.Concat(Assembly.GetAssembly(typeof(ApiContainer)).GetTypes())
				.Where(t => /*t.IsClass && */t.IsSealed && typeof(IExternalApi).IsAssignableFrom(t) && ServiceInjector.IsAvailable(serviceProvider, t)))
			{
				var instance = (IExternalApi)Activator.CreateInstance(api);
				ServiceInjector.UpdateServices(serviceProvider, instance);
				Libraries.Add(api.GetInterfaces().First(intf => typeof(IExternalApi).IsAssignableFrom(intf) && intf != typeof(IExternalApi)), instance);
			}
			_container = new ApiContainer(Libraries);
			return new BasicApiProvider(Libraries);
		}

		private static readonly Dictionary<Type, IExternalApi> Libraries = new Dictionary<Type, IExternalApi>();

		public static IExternalApiProvider Restart(IEmulatorServiceProvider newServiceProvider)
		{
			Libraries.Clear();
			return Register(newServiceProvider);
		}
	}
}
