using System;
using System.IO;
using System.Linq;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	[AttributeUsage(AttributeTargets.Field)]
	public sealed class MapperPropAttribute : Attribute
	{
		public string Name { get; }

		public MapperPropAttribute(string name)
		{
			Name = name;
		}

		public MapperPropAttribute()
		{
			Name = null;
		}
	}

	internal static class AutoMapperProps
	{
		public static void Populate(INesBoard board, NES.NESSyncSettings settings)
		{
			var fields = board.GetType().GetFields();
			foreach (var field in fields)
			{
				var attrib = field.GetCustomAttributes(typeof(MapperPropAttribute), false).OfType<MapperPropAttribute>().SingleOrDefault();
				if (attrib == null)
					continue;
				string name = attrib.Name ?? field.Name;
				if (!settings.BoardProperties.ContainsKey(name))
				{
					settings.BoardProperties.Add(name, (string)Convert.ChangeType(field.GetValue(board), typeof(string)));
				}
			}
		}

		public static void Apply(INesBoard board)
		{
			var fields = board.GetType().GetFields();
			foreach (var field in fields)
			{
				var attribs = field.GetCustomAttributes(false);
				foreach (var attrib in attribs)
				{
					if (attrib is MapperPropAttribute mapperProp)
					{
						string name = mapperProp.Name ?? field.Name;

						if (board.InitialRegisterValues.TryGetValue(name, out var Value))
						{
							try
							{
								field.SetValue(board, Convert.ChangeType(Value, field.FieldType));
							}
							catch (Exception e) when (e is InvalidCastException || e is FormatException || e is OverflowException)
							{
								throw new InvalidDataException("Auto Mapper Properties were in a bad format!", e);
							}
						}

						break;
					}
				}
			}
		}
	}
}
