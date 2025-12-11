#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public static class ApiManager
	{
		private static readonly List<(Type ImplType, Type InterfaceType, ConstructorInfo Ctor, Type[] CtorTypes)> _apiTypes = new();

		static ApiManager()
		{
			foreach (var implType in ReflectionCache_Biz_Cli_Com.Types
				.Where(t => /*t.IsClass &&*/t.IsSealed)) // small optimisation; api impl. types are all sealed classes
			{
				AddApiType(implType);
			}
		}

		public static void AddApiType(Type type)
		{
			var interfaceType = type.GetInterfaces().FirstOrDefault(t => typeof(IExternalApi).IsAssignableFrom(t) && t != typeof(IExternalApi));
			if (interfaceType == null) return; // if we couldn't determine what it's implementing, then it's not an api impl. type
			var ctor = type.GetConstructors().Single();
			_apiTypes.Add((type, interfaceType, ctor, ctor.GetParameters().Select(pi => pi.ParameterType).ToArray()));
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
