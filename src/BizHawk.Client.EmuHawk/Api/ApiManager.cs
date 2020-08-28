#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public static class ApiManager
	{
		/// <remarks>keys are impl., values are interface</remarks>
		private static readonly IReadOnlyDictionary<Type, Type> _apiTypes
			= Assembly.GetAssembly(typeof(IEmuClientApi)).GetTypes()
				.Concat(Assembly.GetAssembly(typeof(EmuClientApi)).GetTypesWithoutLoadErrors())
				.Where(t => /*t.IsClass &&*/t.IsSealed) // small optimisation; api impl. types are all sealed classes
				.Select(t => (t, t.GetInterfaces().FirstOrDefault(t1 => typeof(IExternalApi).IsAssignableFrom(t1) && t1 != typeof(IExternalApi)))) // grab interface from impl. type...
				.Where(tuple => tuple.Item2 != null) // ...if we couldn't determine what it's implementing, then it's not an api impl. type
				.ToDictionary(tuple => tuple.Item1, tuple => tuple.Item2);

		private static readonly Type[] _ctorParamTypesA = { typeof(Action<string>) };

		private static readonly Type[] _ctorParamTypesB = { typeof(Action<string>), typeof(IMainFormForApi) };

		private static readonly Type[] _ctorParamTypesC = { typeof(Action<string>), typeof(IMainFormForApi), typeof(DisplayManager), typeof(InputManager), typeof(Config), typeof(IEmulator), typeof(GameInfo) };

		/// <remarks>TODO do we need to keep references to these because of GC weirdness? --yoshi</remarks>
		private static ApiContainer? _container;

		private static ApiContainer? _luaContainer;

		private static ApiContainer Register(
			IMainFormForApi mainForm,
			IEmulatorServiceProvider serviceProvider,
			Action<string> logCallback)
		{
			var libDict = _apiTypes.Keys.Where(t => ServiceInjector.IsAvailable(serviceProvider, t))
				.ToDictionary(
					t => _apiTypes[t],
					t => (IExternalApi) (
						t.GetConstructor(_ctorParamTypesC)?.Invoke(new object[] { logCallback, mainForm, GlobalWin.DisplayManager, GlobalWin.InputManager, GlobalWin.Config, GlobalWin.Emulator, GlobalWin.Game })
							?? t.GetConstructor(_ctorParamTypesB)?.Invoke(new object[] { logCallback, mainForm })
							?? t.GetConstructor(_ctorParamTypesA)?.Invoke(new object[] { logCallback })
							?? Activator.CreateInstance(t)
					)
				);
			foreach (var instance in libDict.Values) ServiceInjector.UpdateServices(serviceProvider, instance);
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
