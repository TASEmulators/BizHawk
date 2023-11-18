using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace NLua
{
	public class LuaGlobalEntry
	{
		/// <summary>
		/// Type at time of registration.
		/// </summary>
		public Type Type { get; }

		public string Path { get; }

		/// <summary>
		/// List of global properties 'owned' by this entry.
		/// If this entry is removed all these globals should be removed as well.
		/// </summary>
		public readonly List<string> linkedGlobals = new();

		public LuaGlobalEntry(Type type, string path)
		{
			Type = type;
			Path = path;
		}
	}

	public class LuaGlobals
	{
		private readonly List<string> _globals = new();
		private readonly List<LuaGlobalEntry> _knownTypes = new();

		public bool _globalsSorted;

		public int MaximumRecursion { get; set; } = 2;

		public IEnumerable<string> Globals
		{
			get
			{
				// Only sort list when necessary
				if (!_globalsSorted)
				{
					_globals.Sort();
					_globalsSorted = true;
				}

				return _globals;
			}
		}

		public bool Contains(string fullPath)
			=> _globals.Contains(fullPath);

		public void RemoveGlobal(string path)
		{
			var knownType = GetKnownType(path);
			if (knownType != null)
			{
				// We need to clean up the globals
				foreach (var dependent in knownType.linkedGlobals)
				{
					_globals.Remove(dependent);
				}

				_knownTypes.Remove(knownType);
			}
		}

		private LuaGlobalEntry GetKnownType(string path)
			=> _knownTypes.Find(x => x.Path.Equals(path, StringComparison.Ordinal));

		public void RegisterGlobal(string path, Type type, int recursionCounter)
		{
			var knownType = GetKnownType(path);
			if (knownType != null)
			{
				if (type == knownType.Type)
				{
					// Object is set to same value so no need to update
					return;
				}

				// Path changed type so we should clean up all known globals associated with the type
				RemoveGlobal(path);
			}

			RegisterPath(path, type, recursionCounter);

			// List will need to be sorted on next access
			_globalsSorted = false;
		}

		private void RegisterPath(string path, Type type, int recursionCounter, LuaGlobalEntry entry = null)
		{
			// If the type is a global method, list it directly
			if (type == typeof(LuaFunction))
			{
				RegisterLuaFunction(path, entry);
			}
			// If the type is a class or an interface and recursion hasn't been running too long, list the members
			else if ((type.IsClass || type.IsInterface) && type != typeof(string) && recursionCounter < MaximumRecursion)
			{
				RegisterClassOrInterface(path, type, recursionCounter, entry);
			}
			else
			{
				RegisterPrimitive(path, entry);
			}
		}

		private void RegisterLuaFunction(string path, LuaGlobalEntry entry = null)
		{
			// Format for easy method invocation
			_globals.Add(path + "(");
			entry?.linkedGlobals.Add(path);
		}

		private void RegisterPrimitive(string path, LuaGlobalEntry entry = null)
		{
			_globals.Add(path);
			entry?.linkedGlobals.Add(path);
		}

		private void RegisterClassOrInterface(string path, Type type, int recursionCounter, LuaGlobalEntry entry = null)
		{
			if (entry == null)
			{
				entry = new(type, path);
				_knownTypes.Add(entry);
			}

			foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
			{
				var name = method.Name;
				// Exclude some generic .NET methods that wouldn't be very useful in Lua
				if (name != "GetType" && name != "GetHashCode" && name != "Equals" &&
				    name != "ToString" && name != "Clone" && name != "Dispose" &&
				    name != "GetEnumerator" && name != "CopyTo" &&
				    !name.StartsWith("get_", StringComparison.Ordinal) &&
				    !name.StartsWith("set_", StringComparison.Ordinal) &&
				    !name.StartsWith("add_", StringComparison.Ordinal) &&
				    !name.StartsWith("remove_", StringComparison.Ordinal))
				{
					// Format for easy method invocation
					var command = path + ":" + name + "(";

					if (method.GetParameters().Length == 0)
						command += ")";

					_globals.Add(command);
					entry.linkedGlobals.Add(command);
				}
			}

			foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
			{
				// Go into recursion for members
				RegisterPath(path + "." + field.Name, field.FieldType, recursionCounter + 1, entry);
			}

			foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
			{
				// Exclude some generic .NET properties that wouldn't be very useful in Lua
				if (property.Name != "Item")
				{
					// Go into recursion for members
					RegisterPath(path + "." + property.Name, property.PropertyType, recursionCounter + 1, entry);
				}
			}
		}
	}
}

