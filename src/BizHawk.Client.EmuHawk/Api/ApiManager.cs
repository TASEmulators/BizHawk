#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using BizHawk.API.Base;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public static class ApiManager
	{
		private static readonly Type[] CtorParamTypes = { typeof(Action<string>) };

		/// <remarks>TODO do we need to keep references to these because of GC weirdness? --yoshi</remarks>
		private static ApiContainer? _container;

		private static ApiContainer? _luaContainer;

		private static ApiContainer Register(IEmulatorServiceProvider serviceProvider, Action<string> logCallback)
		{
			var ctorParamTypes = CtorParamTypes;
			var libDict = new Dictionary<Type, IExternalApi>();
			foreach (var api in Assembly.GetAssembly(typeof(ApiSubsetContainer)).GetTypes()
				.Concat(Assembly.GetAssembly(typeof(ApiContainer)).GetTypes())
				.Where(t => /*t.IsClass && */t.IsSealed
					&& typeof(IExternalApi).IsAssignableFrom(t)
					&& !t.FullName.EndsWith("LegacyImpl") // Dumb hack to prevent crashes on startup. API will not work even with hack because I removed the implementations that were IExternalApi-only. --yoshi
					&& ServiceInjector.IsAvailable(serviceProvider, t)))
			{
				var instance = api.GetConstructor(ctorParamTypes)?.Invoke(new object[] { logCallback })
					?? Activator.CreateInstance(api);
				ServiceInjector.UpdateServices(serviceProvider, instance);
				libDict.Add(
					api.GetInterfaces().First(intf => typeof(IExternalApi).IsAssignableFrom(intf) && intf != typeof(IExternalApi)),
					(IExternalApi) instance
				);
			}
			return new ApiContainer(libDict);
		}

		public static IExternalApiProvider Restart(IEmulatorServiceProvider newServiceProvider)
			=> new BasicApiProvider(_container = Register(newServiceProvider, Console.WriteLine));

		public static ApiContainer RestartLua(IEmulatorServiceProvider newServiceProvider, Action<string> logCallback)
			=> _luaContainer = Register(newServiceProvider, logCallback);
	}
}
