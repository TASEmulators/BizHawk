using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores
{
	/// <summary>
	/// finds and instantiates IEmulator cores
	/// </summary>
	public class CoreInventory
	{
		private readonly Dictionary<string, List<Core>> _systems = new Dictionary<string, List<Core>>();

		public class Core
		{
			private class RomGameFake : IRomGame
			{
				public byte[] RomData { get; set; }
				public byte[] FileData { get; set; }
				public string Extension { get; set; }
			}
			// expected names and types of the parameters
			private static readonly Dictionary<string, Type> ParamTypes = new Dictionary<string, Type>();

			// map parameter names to locations in the constructor
			private readonly Dictionary<string, int> _paramMap = new Dictionary<string, int>();

			private readonly bool _useCoreLoadParameters;

			static Core()
			{
				var pp = typeof(Core).GetMethod("Create")?.GetParameters();
				if (pp != null)
				{
					foreach (var p in pp)
					{
						ParamTypes.Add(p.Name.ToLowerInvariant(), p.ParameterType);
					}
				}
			}

			public Core(string name, Type type, ConstructorInfo ctor)
			{
				Name = name;
				Type = type;
				CTor = ctor;

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
					if (!ParamTypes.TryGetValue(pName, out _))
					{
						throw new InvalidOperationException($"Unexpected parameter name {p.Name} in constructor for {Type}");
					}
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

			public string Name { get; }
			public Type Type { get; }
			public ConstructorInfo CTor { get; }
			public Type SettingsType { get; } = typeof(object);
			public Type SyncSettingsType { get; } = typeof(object);

			private void Bp(object[] parameters, string name, object value)
			{
				if (_paramMap.TryGetValue(name, out var i))
				{
					parameters[i] = value;
				}
			}

			/// <summary>
			/// Instantiate an emulator core
			/// </summary>
			public IEmulator Create
			(
				CoreComm comm,
				GameInfo game,
				byte[] rom,
				byte[] file,
				bool deterministic,
				object settings,
				object syncSettings,
				string extension
			)
			{
				if (_useCoreLoadParameters)
				{
					var paramType = typeof(CoreLoadParameters<,>).MakeGenericType(new[] { SettingsType, SyncSettingsType });
					// TODO: clean this up
					dynamic param = Activator.CreateInstance(paramType);
					param.Comm = comm;
					param.Game = game;
					param.Settings = (dynamic)settings;
					param.SyncSettings = (dynamic)syncSettings;
					param.Roms.Add(new RomGameFake
					{
						RomData = rom,
						FileData = file,
						Extension = extension
					});
					param.DeterministicEmulationRequested = deterministic;
					return (IEmulator)CTor.Invoke(new object[] { param });
				}
				object[] o = new object[_paramMap.Count];
				Bp(o, "comm", comm);
				Bp(o, "game", game);
				Bp(o, "rom", rom);
				Bp(o, "file", file);
				Bp(o, "deterministic", deterministic);
				Bp(o, "settings", settings);
				Bp(o, "syncsettings", syncSettings);
				Bp(o, "extension", extension);

				return (IEmulator)CTor.Invoke(o);
			}
		}

		private void ProcessConstructor(Type type, string system, CoreAttribute coreAttr, ConstructorInfo cons)
		{
			Core core = new Core(coreAttr.CoreName, type, cons);
			if (!_systems.TryGetValue(system, out var ss))
			{
				ss = new List<Core>();
				_systems.Add(system, ss);
			}

			ss.Add(core);
		}

		/// <summary>
		/// find a core matching a particular game.system
		/// </summary>
		public Core this[string system]
		{
			get
			{
				List<Core> ss = _systems[system];
				if (ss.Count != 1)
				{
					throw new InvalidOperationException("Ambiguous core selection!");
				}

				return ss[0];
			}
		}

		/// <summary>
		/// find a core matching a particular game.system with a particular CoreAttributes.Name
		/// </summary>
		public Core this[string system, string core]
		{
			get
			{
				List<Core> ss = _systems[system];
				foreach (Core c in ss)
				{
					if (c.Name == core)
					{
						return c;
					}
				}

				throw new InvalidOperationException("No such core!");
			}
		}

		/// <summary>
		/// create a core inventory, collecting all IEmulators from some assemblies
		/// </summary>
		public CoreInventory(IEnumerable<Assembly> assys)
		{
			foreach (var assy in assys)
			{
				foreach (var typ in assy.GetTypes())
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
							foreach (string system in ((CoreConstructorAttribute)con.GetCustomAttributes(typeof(CoreConstructorAttribute), false)[0]).Systems)
							{
								ProcessConstructor(typ, system, (CoreAttribute)coreAttr[0], con);
							}
						}
					}
				}
			}
		}

		public static readonly CoreInventory Instance = new CoreInventory(new[] { typeof(CoreInventory).Assembly });
	}

	[AttributeUsage(AttributeTargets.Constructor)]
	public sealed class CoreConstructorAttribute : Attribute
	{
		private readonly List<string> _systems = new List<string>();

		/// <remarks>TODO neither array nor <see cref="IEnumerable{T}"/> is the correct collection to be using here, try <see cref="IReadOnlyList{T}"/>/<see cref="IReadOnlyCollection{T}"/> instead</remarks>
		public CoreConstructorAttribute(string[] systems)
		{
			_systems.AddRange(systems);
		}

		public CoreConstructorAttribute(string system)
		{
			_systems.Add(system);
		}

		public IEnumerable<string> Systems => _systems;
	}
}
