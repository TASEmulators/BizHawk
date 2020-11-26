#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public static class ApiManager
	{
		/// <remarks>keys are impl., values are interface</remarks>
		private static readonly IReadOnlyDictionary<Type, Type> _apiTypes
			= Client.Common.ReflectionCache.Types.Concat(EmuHawk.ReflectionCache.Types)
				.Where(t => /*t.IsClass &&*/t.IsSealed) // small optimisation; api impl. types are all sealed classes
				.Select(t => (t, t.GetInterfaces().FirstOrDefault(t1 => typeof(IExternalApi).IsAssignableFrom(t1) && t1 != typeof(IExternalApi)))) // grab interface from impl. type...
				.Where(tuple => tuple.Item2 != null) // ...if we couldn't determine what it's implementing, then it's not an api impl. type
				.ToDictionary(tuple => tuple.Item1, tuple => tuple.Item2);

		private static readonly Type[] _ctorParamTypesA = { typeof(Action<string>) };

		private static readonly Type[] _ctorParamTypesB = { typeof(Action<string>), typeof(IMainFormForApi) };

		private static readonly Type[] _ctorParamTypesC = { typeof(Action<string>), typeof(IMainFormForApi), typeof(DisplayManager), typeof(InputManager), typeof(Config), typeof(IEmulator), typeof(IGameInfo) };

		/// <remarks>TODO do we need to keep references to these because of GC weirdness? --yoshi</remarks>
		private static ApiContainer? _container;

		private static ApiContainer? _luaContainer;

		private static ApiContainer Register(
			IEmulatorServiceProvider serviceProvider,
			Action<string> logCallback,
			IMainFormForApi mainForm,
			DisplayManager displayManager,
			InputManager inputManager,
			Config config,
			IEmulator emulator,
			IGameInfo game)
		{
			var libDict = _apiTypes.Keys.Where(t => ServiceInjector.IsAvailable(serviceProvider, t))
				.ToDictionary(
					t => _apiTypes[t],
					t => (IExternalApi) (
						t.GetConstructor(_ctorParamTypesC)?.Invoke(new object[] { logCallback, mainForm, displayManager, inputManager, config, emulator, game })
							?? t.GetConstructor(_ctorParamTypesB)?.Invoke(new object[] { logCallback, mainForm })
							?? t.GetConstructor(_ctorParamTypesA)?.Invoke(new object[] { logCallback })
							?? Activator.CreateInstance(t)
					)
				);
			foreach (var instance in libDict.Values) ServiceInjector.UpdateServices(serviceProvider, instance);
			return new ApiContainer(libDict);
		}

		public static IExternalApiProvider Restart(
			IEmulatorServiceProvider serviceProvider,
			IMainFormForApi mainForm,
			DisplayManager displayManager,
			InputManager inputManager,
			Config config,
			IEmulator emulator,
			IGameInfo game)
		{
			_container = Register(serviceProvider, Console.WriteLine, mainForm, displayManager, inputManager, config, emulator, game);
			ClientApi.EmuClient = _container.EmuClient;
			return new BasicApiProvider(_container);
		}

		public static ApiContainer RestartLua(
			IEmulatorServiceProvider serviceProvider,
			Action<string> logCallback,
			IMainFormForApi mainForm,
			DisplayManager displayManager,
			InputManager inputManager,
			Config config,
			IEmulator emulator,
			IGameInfo game
		) => _luaContainer = Register(serviceProvider, logCallback, mainForm, displayManager, inputManager, config, emulator, game);
	}
}
