using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using BizHawk.Common.ReflectionExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Client.ApiHawk;

namespace BizHawk.Client.EmuHawk

{
	public static class ApiManager
	{
		private static ApiContainer container;
		private static void Register(IEmulatorServiceProvider serviceProvider)
		{
			// Register external apis
			var apis = Assembly
				.Load("BizHawk.Client.ApiHawk")
				.GetTypes()
				.Where(t => typeof(IExternalApi).IsAssignableFrom(t))
				.Where(t => t.IsSealed)
				.Where(t => ServiceInjector.IsAvailable(serviceProvider, t))
				.Concat(Assembly
					.GetAssembly(typeof(ApiContainer))
					.GetTypes()
					.Where(t => typeof(IExternalApi).IsAssignableFrom(t))
					.Where(t => t.IsSealed)
					.Where(t => ServiceInjector.IsAvailable(serviceProvider, t)));

			foreach (var api in apis)
			{
				var instance = (IExternalApi)Activator.CreateInstance(api);
				ServiceInjector.UpdateServices(serviceProvider, instance);
				Libraries.Add(api, instance);
			}
			container = new ApiContainer(Libraries);
			GlobalWin.ApiProvider = new BasicApiProvider(container);
		}
		private static readonly Dictionary<Type, IExternalApi> Libraries = new Dictionary<Type, IExternalApi>();
		public static void Restart(IEmulatorServiceProvider newServiceProvider)
		{
			Libraries.Clear();
			Register(newServiceProvider);
		}
	}
}
