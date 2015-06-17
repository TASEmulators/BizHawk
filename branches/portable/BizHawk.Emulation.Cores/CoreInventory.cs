using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.DiscSystem;

namespace BizHawk.Emulation.Cores
{
	/// <summary>
	/// finds and instantiates IEmulator cores
	/// </summary>
	public class CoreInventory
	{
		public class Core
		{
			public readonly string Name;
			public readonly Type Type;
			public readonly ConstructorInfo CTor;

			public Core(string Name, Type Type, ConstructorInfo CTor)
			{
				this.Name = Name;
				this.Type = Type;
				this.CTor = CTor;

				var pp = CTor.GetParameters();
				for (int i = 0; i < pp.Length ; i++)
				{
					var p = pp[i];
					string pname = p.Name.ToLowerInvariant();
					Type expectedtype;
					if (!paramtypes.TryGetValue(pname, out expectedtype))
						throw new InvalidOperationException(string.Format("Unexpected parameter name {0} in constructor for {1}", p.Name, Type));
					
					// disabling the typecheck here doesn't really hurt anything, because the Invoke call will still catch any forbidden casts
					// it does allow us to write "MySettingsType settings" instead of "object settings"
					// if (expectedtype != p.ParameterType)
					//	throw new InvalidOperationException(string.Format("Unexpected type mismatch in parameter {0} in constructor for {1}", p.Name, Type));
					parammap.Add(pname, i);
				}
			}

			// map parameter names to locations in the constructor
			private readonly Dictionary<string, int> parammap = new Dictionary<string, int>();
			// expected names and types of the parameters
			private static readonly Dictionary<string, Type> paramtypes = new Dictionary<string, Type>();

			static Core()
			{
				var pp = typeof(Core).GetMethod("Create").GetParameters();
				foreach (var p in pp)
					paramtypes.Add(p.Name.ToLowerInvariant(), p.ParameterType);
			}

			void bp(object[] parameters, string name, object value)
			{
				int i;
				if (parammap.TryGetValue(name, out i))
					parameters[i] = value;
			}

			/// <summary>
			/// instatiate an emulator core
			/// </summary>
			/// <param name="comm"></param>
			/// <param name="game"></param>
			/// <param name="rom"></param>
			/// <param name="deterministic"></param>
			/// <param name="settings"></param>
			/// <param name="syncsettings"></param>
			/// <returns></returns>
			public IEmulator Create
			(
				CoreComm comm,
				GameInfo game,
				byte[] rom,
				byte[] file,
				bool deterministic,
				object settings,
				object syncsettings
			)
			{
				object[] o = new object[parammap.Count];
				bp(o, "comm", comm);
				bp(o, "game", game);
				bp(o, "rom", rom);
				bp(o, "file", file);
				bp(o, "deterministic", deterministic);
				bp(o, "settings", settings);
				bp(o, "syncsettings", syncsettings);

				return (IEmulator)CTor.Invoke(o);
			}
		}

		private readonly Dictionary<string, List<Core>> systems = new Dictionary<string, List<Core>>();


		private void ProcessConstructor(Type type, string system, CoreAttributes coreattr, ConstructorInfo cons)
		{
			Core core = new Core(coreattr.CoreName, type, cons);
			List<Core> ss;
			if (!systems.TryGetValue(system, out ss))
			{
				ss = new List<Core>();
				systems.Add(system, ss);
			}
			ss.Add(core);
		}

		/// <summary>
		/// find a core matching a particular game.system
		/// </summary>
		/// <param name="system"></param>
		/// <returns></returns>
		public Core this[string system]
		{
			get
			{
				List<Core> ss = systems[system];
				if (ss.Count != 1)
					throw new InvalidOperationException("Ambiguous core selection!");
				return ss[0];
			}
		}

		/// <summary>
		/// find a core matching a particular game.system with a particular coreattributes.name
		/// </summary>
		/// <param name="system"></param>
		/// <param name="core"></param>
		/// <returns></returns>
		public Core this[string system, string core]
		{
			get
			{
				List<Core> ss = systems[system];
				foreach (Core c in ss)
				{
					if (c.Name == core)
						return c;
				}
				throw new InvalidOperationException("No such core!");
			}
		}

		/// <summary>
		/// find an exact core type.  slow lookup.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public Core FindByType(Type type)
		{
			foreach (List<Core> cc in systems.Values)
			{
				foreach (Core c in cc)
				{
					if (c.Type == type)
						return c;
				}
			}
			throw new InvalidOperationException("No such core!");
		}

		/// <summary>
		/// create a core inventory, collecting all IEmulators from some assembilies
		/// </summary>
		/// <param name="assys"></param>
		public CoreInventory(IEnumerable<Assembly> assys)
		{
			foreach (var assy in assys)
			{
				foreach (var typ in assy.GetTypes())
				{
					if (typ.GetInterfaces().Contains(typeof(IEmulator)))
					{
						var coreattr = typ.GetCustomAttributes(typeof(CoreAttributes), false);
						if (coreattr.Length != 1)
							throw new InvalidOperationException(string.Format("IEmulator {0} without CoreAttributes!", typ));
						var cons = typ.GetConstructors(BindingFlags.Public | BindingFlags.Instance)
							.Where(c => c.GetCustomAttributes(typeof(CoreConstructorAttribute), false).Length > 0).ToList();
						foreach(var con in cons)
						{
							foreach (string system in ((CoreConstructorAttribute)con.GetCustomAttributes(typeof(CoreConstructorAttribute), false)[0]).Systems)
							{
								ProcessConstructor(typ, system, (CoreAttributes)coreattr[0], con);
							}
						}
					}
				}
			}
		}

		public static readonly CoreInventory Instance = new CoreInventory(new[] { typeof(CoreInventory).Assembly });
	}

	[AttributeUsage(AttributeTargets.Constructor)]
	public class CoreConstructorAttribute : Attribute
	{
		public IEnumerable<string> Systems { get { return _systems; } }
		private readonly List<string> _systems = new List<string>();
		public CoreConstructorAttribute(params string[] Systems)
		{
			_systems.AddRange(Systems);
		}
	}
}
