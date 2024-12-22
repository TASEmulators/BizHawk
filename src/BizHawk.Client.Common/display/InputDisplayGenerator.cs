using System.Linq;
using System.Text;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// Generates a display friendly version of the input log entry
	/// using .bk2 mnemonics as the basis for display
	/// </summary>
	public static class Bk2InputDisplayGenerator
	{
		public static string Generate(IController source)
		{
			if (source.Definition.MnemonicsCache is null)
				throw new InvalidOperationException("Can't generate input display string with empty mnemonics cache");

			var sb = new StringBuilder();
			var players = source.Definition.ControlsOrdered;
			var playersNewOrder = players.Skip(1).Concat(players.Take(1)); // move first ("player 0") to end
			foreach ((string buttonName, AxisSpec? axisSpec) in playersNewOrder.SelectMany(static x => x))
			{
				if (axisSpec.HasValue)
				{
					int val = source.AxisValue(buttonName);

					if (val == axisSpec.Value.Neutral)
					{
						sb.Append("      ");
					}
					else
					{
						sb.Append(val.ToString().PadLeft(5, ' ')).Append(',');
					}
				}
				else
				{
					sb.Append(source.IsPressed(buttonName)
						? source.Definition.MnemonicsCache[buttonName]
						: ' ');
				}
			}

			return sb.ToString();
		}
	}
}
