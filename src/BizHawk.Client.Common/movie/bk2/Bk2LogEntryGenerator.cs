using System.Linq;
using System.Text;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public static class Bk2LogEntryGenerator
	{
		/// <summary>
		/// Gets an input log entry that is considered empty. (booleans will be false, axes will be neutral)
		/// </summary>
		public static string EmptyEntry(IController source) => CreateLogEntry(source, createEmpty: true);

		/// <summary>
		/// Generates an input log entry for the current state of source
		/// </summary>
		public static string GenerateLogEntry(IController source) => CreateLogEntry(source);

		/// <summary>
		/// Generates a human readable key that will specify the names of the
		/// buttons and the order they are in. This is intended to simply be
		/// documentation of the meaning of the mnemonics and not to be used to
		/// enforce the mnemonic values
		/// </summary>
		public static string GenerateLogKey(ControllerDefinition definition)
		{
			var sb = new StringBuilder();

			foreach (var group in definition.ControlsOrdered.Where(static c => c.Count is not 0))
			{
				sb.Append('#');
				foreach ((string buttonName, _) in group)
				{
					sb.Append(buttonName).Append('|');
				}
			}

			return sb.ToString();
		}

		private static string CreateLogEntry(IController source, bool createEmpty = false)
		{
			if (!createEmpty && source.Definition.MnemonicsCache is null)
				throw new InvalidOperationException("Can't generate log entry with empty mnemonics cache");

			var sb = new StringBuilder();

			sb.Append('|');

			foreach (var group in source.Definition.ControlsOrdered)
			{
				foreach ((string buttonName, var axisSpec) in group)
				{
					if (axisSpec.HasValue)
					{
						var val = createEmpty ? axisSpec.Value.Neutral : source.AxisValue(buttonName);
						sb.Append(val.ToString().PadLeft(5, ' ')).Append(',');
					}
					else
					{
						sb.Append(!createEmpty && source.IsPressed(buttonName)
							? source.Definition.MnemonicsCache[buttonName]
							: '.');
					}
				}
				sb.Append('|');
			}

			return sb.ToString();
		}
	}
}
