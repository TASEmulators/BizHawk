#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public static class ApiManager
	{
		private static readonly Type[] CtorParamTypesA = { typeof(Action<string>), typeof(DisplayManager), typeof(InputManager), typeof(MainForm) };

		private static readonly Type[] CtorParamTypesB = { typeof(Action<string>) };

		/// <remarks>TODO do we need to keep references to these because of GC weirdness? --yoshi</remarks>
		private static ApiContainer? _container;

		private static ApiContainer? _luaContainer;

		private static ApiContainer Register(
			MainForm mainForm,
			IEmulatorServiceProvider serviceProvider,
			Action<string> logCallback)
		{
			var libDict = new Dictionary<Type, IExternalApi>();
			foreach (var api in Assembly.GetAssembly(typeof(ApiSubsetContainer)).GetTypes()
				.Concat(Assembly.GetAssembly(typeof(ApiContainer)).GetTypes())
				.Where(t => /*t.IsClass && */t.IsSealed
					&& typeof(IExternalApi).IsAssignableFrom(t)
					&& ServiceInjector.IsAvailable(serviceProvider, t)))
			{
				var instance = api.GetConstructor(CtorParamTypesA)?.Invoke(new object[] { logCallback, GlobalWin.DisplayManager, GlobalWin.InputManager, mainForm })
					?? api.GetConstructor(CtorParamTypesB)?.Invoke(new object[] { logCallback })
					?? Activator.CreateInstance(api);
				ServiceInjector.UpdateServices(serviceProvider, instance);
				libDict.Add(
					api.GetInterfaces().First(intf => typeof(IExternalApi).IsAssignableFrom(intf) && intf != typeof(IExternalApi)),
					(IExternalApi) instance
				);
			}
			return new ApiContainer(libDict);
		}

		public static IExternalApiProvider Restart(MainForm mainForm, IEmulatorServiceProvider newServiceProvider)
			=> new BasicApiProvider(_container = Register(mainForm, newServiceProvider, Console.WriteLine));

		public static ApiContainer RestartLua(MainForm mainForm, IEmulatorServiceProvider newServiceProvider, Action<string> logCallback)
			=> _luaContainer = Register(mainForm, newServiceProvider, logCallback);
	}
}
