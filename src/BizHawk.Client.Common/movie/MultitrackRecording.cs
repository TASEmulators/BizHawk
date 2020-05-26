using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class MultitrackRecorder
	{
		public MultitrackRecorder()
		{
			Restart(1);
		}

		public bool IsActive { get; set; }
		public int CurrentPlayer { get; private set; }
		public int PlayerCount { get; private set; }
		public bool RecordAll { get; private set; }

		/// <summary>
		/// Gets a user friendly multi-track status
		/// </summary>
		public string Status
		{
			get
			{
				if (!IsActive)
				{
					return "";
				}

				if (RecordAll)
				{
					return "Recording All";
				}

				if (CurrentPlayer == 0)
				{
					return "Recording None";
				}

				return $"Recording Player {CurrentPlayer}";
			}
		}

		public void Restart(int playerCount)
		{
			PlayerCount = playerCount;
			IsActive = false;
			CurrentPlayer = 0;
			RecordAll = false;
		}

		public void SelectAll()
		{
			CurrentPlayer = 0;
			RecordAll = true;
		}

		public void SelectNone()
		{
			RecordAll = false;
			CurrentPlayer = 0;
		}

		public void Increment()
		{
			RecordAll = false;
			CurrentPlayer++;
			if (CurrentPlayer > PlayerCount)
			{
				CurrentPlayer = 1;
			}
		}

		public void Decrement()
		{
			RecordAll = false;
			CurrentPlayer--;
			if (CurrentPlayer < 1)
			{
				CurrentPlayer = PlayerCount;
			}
		}
	}

	/// <summary>
	/// rewires player1 controls to playerN
	/// </summary>
	public class MultitrackRewiringControllerAdapter : IController
	{
		public IController Source { get; set; }
		public int PlayerSource { get; set; } = 1;
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

	public class ButtonNameParser
	{
		public static ButtonNameParser Parse(string button)
		{
			// See if we're being asked for a button that we know how to rewire
			var parts = button.Split(' ');

			if (parts.Length < 2)
			{
				return null;
			}

			if (parts[0][0] != 'P')
			{
				return null;
			}

			if (!int.TryParse(parts[0].Substring(1), out var player))
			{
				return null;
			}

			return new ButtonNameParser
			{
				PlayerNum = player,
				ButtonPart = button.Substring(parts[0].Length + 1)
			};
		}

		public int PlayerNum { get; set; }
		public string ButtonPart { get; private set; }

		public override string ToString() => $"P{PlayerNum} {ButtonPart}";
	}
}
