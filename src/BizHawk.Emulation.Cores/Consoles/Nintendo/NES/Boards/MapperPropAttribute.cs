using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	[AttributeUsage(AttributeTargets.Field)]
	public sealed class MapperPropAttribute : Attribute
	{
	}

	internal static class AutoMapperProps
	{
		public static void Populate(INesBoard board, NES.NESSyncSettings settings)
		{
			foreach (var fi in board.MapperProps())
			{
				var name = fi.Name;
				if (!settings.BoardProperties.ContainsKey(name))
				{
					settings.BoardProperties.Add(name, (string) Convert.ChangeType(fi.GetValue(board), typeof(string)));
				}
			}
		}

		public static void Apply(INesBoard board)
		{
			foreach (var fi in board.MapperProps())
			{
				if (!board.InitialRegisterValues.TryGetValue(fi.Name, out var value)) continue;
				try
				{
					fi.SetValue(board, Convert.ChangeType(value, fi.FieldType));
				}
				catch (Exception e) when (e is InvalidCastException || e is FormatException || e is OverflowException)
				{
					throw new InvalidOperationException("Auto Mapper Properties were in a bad format!", e);
				}
			}
		}

		/// <remarks>actually fields</remarks>
		public static IEnumerable<FieldInfo> MapperProps(this INesBoard board)
			=> board.GetType().GetFields()
				.Where(static fi => fi.GetCustomAttribute<MapperPropAttribute>(inherit: false) is not null);
	}
}
