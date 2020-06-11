using BizHawk.Emulation.Common;

namespace  BizHawk.Client.Common
{
	/// <summary>
	/// rewires player1 controls to playerN
	/// </summary>
	internal class MultitrackRewiringControllerAdapter : IInputAdapter
	{
		public IController Source { get; set; }
		public int PlayerSource { get; set; } = -1;
		public int PlayerTargetMask { get; set; }

		public ControllerDefinition Definition => Source.Definition;

		public bool IsPressed(string button)
		{
			return Source.IsPressed(RemapButtonName(button));
		}

		public int AxisValue(string name)
		{
			return Source.AxisValue(RemapButtonName(name));
		}

		private string RemapButtonName(string button)
		{
			// Do we even have a source?
			if (PlayerSource == -1)
			{
				return button;
			}

			// See if we're being asked for a button that we know how to rewire
			var bnp = ButtonNameParser.Parse(button);

			if (bnp == null)
			{
				return button;
			}

			// Ok, this looks like a normal `P1 Button` type thing. we can handle it
			// Were we supposed to replace this one?
			int foundPlayerMask = 1 << bnp.PlayerNum;
			if ((PlayerTargetMask & foundPlayerMask) == 0)
			{
				return button;
			}

			// Ok, we were. swap out the source player and then grab his button
			bnp.PlayerNum = PlayerSource;
			return bnp.ToString();
		}
	}
}
