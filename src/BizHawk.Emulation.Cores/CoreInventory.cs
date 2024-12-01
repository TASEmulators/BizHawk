using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;

using BizHawk.Common.CollectionExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores
{
	/// <summary>
	/// finds and instantiates IEmulator cores
	/// </summary>
	public class CoreInventory
	{
		private readonly Dictionary<string, List<Core>> _systems = new Dictionary<string, List<Core>>();

		/// <summary>keys are system IDs; values are core/ctor info for all that system's cores</summary>
		public IReadOnlyDictionary<string, List<Core>> AllCores => _systems;

		public readonly IReadOnlyCollection<Core> SystemsFlat;

		public class Core
		{
			private class RomGameFake : IRomAsset
			{
				public byte[] RomData { get; set; }
				public byte[] FileData { get; set; }
				public string Extension { get; set; }
				public string RomPath { get; set; }
				public GameInfo Game { get; set; }
			}

			// map parameter names to locations in the constructor
			private readonly Dictionary<string, int> _paramMap = new Dictionary<string, int>();
			// If true, this is a new style constructor that takes a CoreLoadParameters object
			private readonly bool _useCoreLoadParameters;

			public Core(Type type, CoreConstructorAttribute consAttr, CoreAttribute coreAttr, ConstructorInfo ctor)
			{
				Name = coreAttr.CoreName;
				Type = type;
				CTor = ctor;
				Priority = consAttr.Priority;
				CoreAttr = coreAttr;

				var pp = CTor.GetParameters();
				if (pp.Length == 1
					&& pp[0].ParameterType.IsGenericType
					&& pp[0].ParameterType.GetGenericTypeDefinition() == typeof(CoreLoadParameters<,>)
				)
				{
					_useCoreLoadParameters = true;
					SettingsType = pp[0].ParameterType.GetGenericArguments()[0];
					SyncSettingsType = pp[0].ParameterType.GetGenericArguments()[1];
					return;
				}
				for (int i = 0; i < pp.Length ; i++)
				{
					var p = pp[i];
					string pName = p.Name.ToLowerInvariant();
					if (pName == "settings")
					{
						if (p.ParameterType == typeof(object))
							throw new InvalidOperationException($"Setting and SyncSetting constructor parameters for {type} must be annotated with the actual type");
						SettingsType = p.ParameterType;
					}
					else if (pName == "syncsettings")
					{
						if (p.ParameterType == typeof(object))
							throw new InvalidOperationException($"Setting and SyncSetting constructor parameters for {type} must be annotated with the actual type");
						SyncSettingsType = p.ParameterType;
					}
					_paramMap.Add(pName, i);
				}
			}

			/// <summary>
			/// (hopefully) a CoreNames value
			/// </summary>
			/// <value></value>
			public string Name { get; }
			public Type Type { get; }
			public ConstructorInfo CTor { get; }
			public CorePriority Priority { get; }
			public CoreAttribute CoreAttr { get; }
			public Type SettingsType { get; } = typeof(object);
			public Type SyncSettingsType { get; } = typeof(object);

			private void Bp(object[] parameters, string name, object value)
			{
				if (_paramMap.TryGetValue(name, out var i))
				{
					parameters[i] = value;
				}
			}

			private IEmulator CreateUsingCoreLoadParameters(ICoreInventoryParameters cip)
			{
				var paramType = typeof(CoreLoadParameters<,>).MakeGenericType(SettingsType, SyncSettingsType);
				// TODO: clean this up
				dynamic param = Activator.CreateInstance(paramType);
				param.Comm = cip.Comm;
				param.Game = cip.Game;
				param.Settings = (dynamic)cip.FetchSettings(Type, SettingsType);
				param.SyncSettings = (dynamic)cip.FetchSyncSettings(Type, SyncSettingsType);
				param.Roms = cip.Roms;
				param.Discs = cip.Discs;
				param.DeterministicEmulationRequested = cip.DeterministicEmulationRequested;
				return (IEmulator)CTor.Invoke(new object[] { param });
			}

			private IEmulator CreateUsingLegacyConstructorParameters(ICoreInventoryParameters cip)
			{
				// cores using the old constructor parameters can only take a single rom, so assume that here
				object[] o = new object[_paramMap.Count];
				Bp(o, "comm", cip.Comm);
				Bp(o, "game", cip.Game);
				Bp(o, "rom", cip.Roms[0].RomData);
				Bp(o, "file", cip.Roms[0].FileData);
				Bp(o, "deterministic", cip.DeterministicEmulationRequested);
				Bp(o, "settings", cip.FetchSettings(Type, SettingsType));
				Bp(o, "syncsettings", cip.FetchSyncSettings(Type, SyncSettingsType));
				Bp(o, "extension", cip.Roms[0].Extension);

				return (IEmulator)CTor.Invoke(o);
			}

			/// <summary>
			/// Instantiate an emulator core
			/// </summary>
			public IEmulator Create(ICoreInventoryParameters cip)
			{
				try
				{
					return _useCoreLoadParameters
						? CreateUsingCoreLoadParameters(cip)
						: CreateUsingLegacyConstructorParameters(cip);
				}
				catch (TargetInvocationException e)
				{
					// When an exception occurs inside ConstructorInfo.Invoke,
					// we always want to expose the exception the core actually threw,
					// and not the implementation detail that reflected construction was used.
					ExceptionDispatchInfo.Capture(e.InnerException).Throw();
					throw; // Needed only for flow analysis -- CSC doesn't know that ExceptionDispatchInfo.Throw() never returns
				}
			}
		}

		public IEnumerable<Core> GetCores(string system)
		{
			_ = _systems.TryGetValue(system, out var cores);
			return cores ?? Enumerable.Empty<Core>();
		}

		/// <summary>
		/// create a core inventory, collecting all IEmulators from some assemblies
		/// </summary>
		public CoreInventory(IEnumerable<IEnumerable<Type>> assys)
		{
			var systemsFlat = new Dictionary<Type, Core>();
			void ProcessConstructor(Type type, CoreConstructorAttribute consAttr, CoreAttribute coreAttr, ConstructorInfo cons)
			{
				var core = new Core(type, consAttr, coreAttr, cons);
				_systems.GetValueOrPutNew(consAttr.System).Add(core);
				systemsFlat[type] = core;
			}
			foreach (var assy in assys)
			{
				foreach (var typ in assy)
				{
					if (!typ.IsAbstract && typ.GetInterfaces().Contains(typeof(IEmulator)))
					{
						var coreAttr = typ.GetCustomAttributes(typeof(CoreAttribute), false);
						if (coreAttr.Length != 1)
							throw new InvalidOperationException($"{nameof(IEmulator)} {typ} without {nameof(CoreAttribute)}s!");
						var cons = typ.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
							.Where(c => c.GetCustomAttributes(typeof(CoreConstructorAttribute), false).Length > 0);
						foreach(var con in cons)
						{
							foreach (var consAttr in con.GetCustomAttributes(typeof(CoreConstructorAttribute), false).Cast<CoreConstructorAttribute>())
							{
								ProcessConstructor(typ, consAttr, (CoreAttribute)coreAttr[0], con);
							}
						}
					}
				}
			}
			SystemsFlat = systemsFlat.Values;
		}

		public static readonly CoreInventory Instance = new CoreInventory(new[] { Emulation.Cores.ReflectionCache.Types });
	}

	public enum CorePriority
	{
		/// <summary>
		/// The gamedb has requested this core for this game
		/// </summary>
		GameDbPreference = -300,
		/// <summary>
		/// The user has indicated in preferences that this is their favorite core
		/// </summary>
		UserPreference = -200,

		/// <summary>
		/// A very good core that should be preferred over normal cores.  Don't use this?
		/// </summary>
		High = -100,

		/// <summary>
		/// Most cores should use this
		/// </summary>
		Normal = 0,
		/// <summary>
		/// Experimental, special use, or garbage core
		/// </summary>
		Low = 100,
		/// <summary>
		/// TODO:  Do we need this?  Does it need a better name?
		/// </summary>
		SuperLow = 200,
	}

	[AttributeUsage(AttributeTargets.Constructor, AllowMultiple = true)]
	public sealed class CoreConstructorAttribute : Attribute
	{
		public string System { get; }
		public CoreConstructorAttribute(string system)
		{
			System = system;
		}
		public CorePriority Priority { get; set; }
	}

	/// <summary>
	/// What CoreInventory needs to synthesize CoreLoadParameters for a core
	/// </summary>
	public interface ICoreInventoryParameters
	{
		CoreComm Comm { get; }
		GameInfo Game { get; }
		List<IRomAsset> Roms { get; }
		List<IDiscAsset> Discs { get; }
		bool DeterministicEmulationRequested { get; }
		object FetchSettings(Type emulatorType, Type settingsType);
		object FetchSyncSettings(Type emulatorType, Type syncSettingsType);
	}
}
