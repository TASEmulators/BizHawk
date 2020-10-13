using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// Filters input for things called Up and Down while considering the client's AllowUD_LR option. 
	/// This is a bit gross but it is unclear how to do it more nicely
	/// </summary>
	public class UdlrControllerAdapter : IInputAdapter
	{
		public IController Source { get; set; }

		public IVGamepadDef Definition => Source.Definition;

		public bool AllowUdlr { get; set; }

		public bool IsPressed(string button)
		{
			if (AllowUdlr)
			{
				return Source.IsPressed(button);
			}

			// " C " is for N64 "P1 C Up" and the like, which should not be subject to mutexing
			// regarding the unpressing and UDLR logic...... don't think about it. don't question it. don't look at it.
			if (button.Contains(" C "))
			{
				return Source.IsPressed(button);
			}

			if (button.Contains("Down"))
			{
				var prefix = button.SubstringBeforeOrNull("Down");
				string other = $"{prefix}Up";
				return Source.IsPressed(button) && !Source.IsPressed(other);
			}

			if (button.Contains("Up"))
			{
				var prefix = button.SubstringBeforeOrNull("Up");
				string other = $"{prefix}Down";
				return Source.IsPressed(button) && !Source.IsPressed(other);
			}

			if (button.Contains("Right"))
			{
				var prefix = button.SubstringBeforeOrNull("Right");
				string other = $"{prefix}Left";
				return Source.IsPressed(button) && !Source.IsPressed(other);
			}

			if (button.Contains("Left"))
			{
				var prefix = button.SubstringBeforeOrNull("Left");
				string other = $"{prefix}Right";
				return Source.IsPressed(button) && !Source.IsPressed(other);
			}

			return Source.IsPressed(button);
		}

		// The float format implies no U+D and no L+R no matter what, so just passthru
		public int AxisValue(string name) => Source.AxisValue(name);
	}
}
