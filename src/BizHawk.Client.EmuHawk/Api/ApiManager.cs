#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public static class ApiManager
	{
		private static readonly IReadOnlyList<(Type ImplType, Type InterfaceType, ConstructorInfo Ctor, Type[] CtorTypes)> _apiTypes;

		static ApiManager()
		{
			var list = new List<(Type, Type, ConstructorInfo, Type[])>();
			foreach (var implType in ReflectionCache_Biz_Cli_Com.Types.Concat(ReflectionCache.Types)
				.Where(t => /*t.IsClass &&*/t.IsSealed)) // small optimisation; api impl. types are all sealed classes
			{
				var interfaceType = implType.GetInterfaces().FirstOrDefault(t => typeof(IExternalApi).IsAssignableFrom(t) && t != typeof(IExternalApi));
				if (interfaceType == null) continue; // if we couldn't determine what it's implementing, then it's not an api impl. type
				var ctor = implType.GetConstructors().Single();
				list.Add((implType, interfaceType, ctor, ctor.GetParameters().Select(pi => pi.ParameterType).ToArray()));
			}
			_apiTypes = list.ToArray();
		}

		private static ApiContainer? _container;

		private static ApiContainer? _luaContainer;

		private static ApiContainer Register(
			IEmulatorServiceProvider serviceProvider,
			Action<string> logCallback,
			IMainFormForApi mainForm,
			DisplayManagerBase displayManager,
			InputManager inputManager,
			IMovieSession movieSession,
			IToolLoader toolManager,
			Config config,
			IEmulator emulator,
			IGameInfo game,
			IDialogController dialogController)
		{
			var avail = new Dictionary<Type, object>
			{
				[typeof(Action<string>)] = logCallback,
				[typeof(IMainFormForApi)] = mainForm,
				[typeof(IDialogController)] = dialogController,
				[typeof(DisplayManagerBase)] = displayManager,
				[typeof(InputManager)] = inputManager,
				[typeof(IMovieSession)] = movieSession,
				[typeof(IToolLoader)] = toolManager,
				[typeof(Config)] = config,
				[typeof(IEmulator)] = emulator,
				[typeof(IGameInfo)] = game,
			};
			return new ApiContainer(_apiTypes.Where(tuple => ServiceInjector.IsAvailable(serviceProvider, tuple.ImplType))
				.ToDictionary(
					tuple => tuple.InterfaceType,
					tuple =>
					{
						var instance = tuple.Ctor.Invoke(tuple.CtorTypes.Select(t => avail[t]).ToArray());
						if (!ServiceInjector.UpdateServices(serviceProvider, instance, mayCache: true)) throw new Exception("ApiHawk impl. has required service(s) that can't be fulfilled");
						return (IExternalApi) instance;
					}));
		}

		public static IExternalApiProvider Restart(
			IEmulatorServiceProvider serviceProvider,
			IMainFormForApi mainForm,
			DisplayManagerBase displayManager,
			InputManager inputManager,
			IMovieSession movieSession,
			IToolLoader toolManager,
			Config config,
			IEmulator emulator,
			IGameInfo game,
			IDialogController dialogController)
		{
			_container?.Dispose();
			_container = Register(serviceProvider, Console.WriteLine, mainForm, displayManager, inputManager, movieSession, toolManager, config, emulator, game, dialogController);
			return new BasicApiProvider(_container);
		}

		public static ApiContainer RestartLua(
			IEmulatorServiceProvider serviceProvider,
			Action<string> logCallback,
			IMainFormForApi mainForm,
			DisplayManagerBase displayManager,
			InputManager inputManager,
			IMovieSession movieSession,
			IToolLoader toolManager,
			Config config,
			IEmulator emulator,
			IGameInfo game,
			IDialogController dialogController)
		{
			_luaContainer?.Dispose();
			_luaContainer = Register(serviceProvider, logCallback, mainForm, displayManager, inputManager, movieSession, toolManager, config, emulator, game, dialogController);
			return _luaContainer;
		}
	}
}
