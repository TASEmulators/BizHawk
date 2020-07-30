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
		private static readonly Type[] CtorParamTypesA = { typeof(Action<string>), typeof(DisplayManager), typeof(InputManager), typeof(IMainFormForApi) };

		private static readonly Type[] CtorParamTypesB = { typeof(Action<string>) };

		private static readonly Type[] CtorParamTypesEmuClientApi = { typeof(Action<string>), typeof(DisplayManager), typeof(InputManager), typeof(IMainFormForApi), typeof(Config), typeof(IEmulator), typeof(GameInfo) };

		/// <remarks>TODO do we need to keep references to these because of GC weirdness? --yoshi</remarks>
		private static ApiContainer? _container;

		private static ApiContainer? _luaContainer;

		private static ApiContainer Register(
			IMainFormForApi mainForm,
			IEmulatorServiceProvider serviceProvider,
			Action<string> logCallback)
		{
			var libDict = new Dictionary<Type, IExternalApi>();
			foreach (var api in Assembly.GetAssembly(typeof(IEmuClientApi)).GetTypes()
				.Concat(Assembly.GetAssembly(typeof(EmuClientApi)).GetTypes())
				.Where(t => /*t.IsClass && */t.IsSealed
					&& typeof(IExternalApi).IsAssignableFrom(t)
					&& ServiceInjector.IsAvailable(serviceProvider, t)))
			{
				//TODO if extra params are ignored, we can use the same array for every ConstructorInfo.Invoke call --yoshi
				object instance;
				if (typeof(IEmuClientApi).IsAssignableFrom(api))
				{
					instance = (api.GetConstructor(CtorParamTypesEmuClientApi) ?? throw new Exception("failed to call EmuClientApi's hack-filled ctor"))
						.Invoke(new object[] { logCallback, GlobalWin.DisplayManager, GlobalWin.InputManager, mainForm, GlobalWin.Config, GlobalWin.Emulator, GlobalWin.Game });
				}
				else
				{
					instance = api.GetConstructor(CtorParamTypesA)?.Invoke(new object[] { logCallback, GlobalWin.DisplayManager, GlobalWin.InputManager, mainForm })
						?? api.GetConstructor(CtorParamTypesB)?.Invoke(new object[] { logCallback })
						?? Activator.CreateInstance(api);
				}
				ServiceInjector.UpdateServices(serviceProvider, instance);
				libDict.Add(
					api.GetInterfaces().First(intf => typeof(IExternalApi).IsAssignableFrom(intf) && intf != typeof(IExternalApi)),
					(IExternalApi) instance
				);
			}
			return new ApiContainer(libDict);
		}

		public static IExternalApiProvider Restart(IMainFormForApi mainForm, IEmulatorServiceProvider newServiceProvider)
		{
			GlobalWin.ClientApi = null;
			_container = Register(mainForm, newServiceProvider, Console.WriteLine);
			GlobalWin.ClientApi = _container.EmuClient as EmuClientApi;
			return new BasicApiProvider(_container);
		}

		public static ApiContainer RestartLua(IMainFormForApi mainForm, IEmulatorServiceProvider newServiceProvider, Action<string> logCallback)
			=> _luaContainer = Register(mainForm, newServiceProvider, logCallback);
	}
}
