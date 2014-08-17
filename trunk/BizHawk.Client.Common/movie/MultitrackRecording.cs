using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class MultitrackRecorder
	{
		public MultitrackRecorder()
		{
			Restart();
		}

		public MultitrackRewiringControllerAdapter MultitrackRewiringAdapter { get; private set; }

		public bool IsActive { get; set; }
		public int CurrentPlayer{ get; set; }
		public bool RecordAll { get; set; }

		/// <summary>
		/// A user friendly multitrack status
		/// </summary>
		public string Status
		{
			get
			{
				if (!IsActive)
				{
					return string.Empty;
				}

				if (RecordAll)
				{
					return "Recording All";
				}

				if (CurrentPlayer == 0)
				{
					return "Recording None";
				}

				return "Recording Player " + CurrentPlayer;
			}
		}

		public void Restart()
		{
			IsActive = false;
			CurrentPlayer = 0;
			RecordAll = false;
			MultitrackRewiringAdapter = new MultitrackRewiringControllerAdapter();
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
			if (CurrentPlayer > Global.Emulator.ControllerDefinition.PlayerCount)
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
				CurrentPlayer = Global.Emulator.ControllerDefinition.PlayerCount;
			}
		}
	}

	/// <summary>
	/// rewires player1 controls to playerN
	/// </summary>
	public class MultitrackRewiringControllerAdapter : IController
	{
		public IController Source { get; set; }
		public int PlayerSource = 1;
		public int PlayerTargetMask = 0;

		public ControllerDefinition Type { get { return Source.Type; } }
		public bool this[string button] { get { return IsPressed(button); } }

		// floats can be player number remapped just like boolbuttons
		public float GetFloat(string name) { return Source.GetFloat(RemapButtonName(name)); }

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
			int foundPlayerMask = (1 << bnp.PlayerNum);
			if ((PlayerTargetMask & foundPlayerMask) == 0)
			{
				return button;
			}

			// Ok, we were. swap out the source player and then grab his button
			bnp.PlayerNum = PlayerSource;
			return bnp.ToString();
		}

		public bool IsPressed(string button)
		{
			return Source.IsPressed(RemapButtonName(button));
		}
	}
}
