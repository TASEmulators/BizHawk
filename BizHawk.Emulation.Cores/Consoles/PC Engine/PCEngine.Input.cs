using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.PCEngine
{
	public partial class PCEngine
	{
		public readonly ControllerDefinition PCEngineController = new ControllerDefinition
			{
				Name = "PC Engine Controller",
				BoolButtons =
				{
					"P1 Up", "P1 Down", "P1 Left", "P1 Right", "P1 Select", "P1 Run", "P1 B2", "P1 B1",
					"P2 Up", "P2 Down", "P2 Left", "P2 Right", "P2 Select", "P2 Run", "P2 B2", "P2 B1",
					"P3 Up", "P3 Down", "P3 Left", "P3 Right", "P3 Select", "P3 Run", "P3 B2", "P3 B1",
					"P4 Up", "P4 Down", "P4 Left", "P4 Right", "P4 Select", "P4 Run", "P4 B2", "P4 B1",
					"P5 Up", "P5 Down", "P5 Left", "P5 Right", "P5 Select", "P5 Run", "P5 B2", "P5 B1"
				}
			};

		private void SetControllerButtons()
		{
			ControllerDefinition.BoolButtons.Clear();
			ControllerDefinition.FloatControls.Clear();

			for (int i = 0; i < 5; i++)
			{
				if (_syncSettings.Controllers[i].IsConnected)
				{
					ControllerDefinition.BoolButtons.AddRange(new[]
					{
						"P" + (i + 1) + " Up",
						"P" + (i + 1) + " Down",
						"P" + (i + 1) + " Left",
						"P" + (i + 1) + " Right",
						"P" + (i + 1) + " Select",
						"P" + (i + 1) + " Run",
						"P" + (i + 1) + " B1",
						"P" + (i + 1) + " B2"
					});
				}
			}
		}

		int SelectedController;
		byte InputByte;

		public bool SEL { get { return ((InputByte & 1) != 0); } }
		public bool CLR { get { return ((InputByte & 2) != 0); } }

		void WriteInput(byte value)
		{
			bool prevSEL = SEL;
			InputByte = value;

			if (SEL && CLR)
				SelectedController = 0;

			if (CLR == false && prevSEL == false && SEL == true)
				SelectedController = (SelectedController + 1);
		}

		byte ReadInput()
		{
			InputCallbacks.Call();
			byte value = 0x3F;

			int player = SelectedController + 1;
			if (player < 6)
			{
				_lagged = false;
				if (SEL == false) // return buttons
				{
					if (Controller.IsPressed("P" + player + " B1")) value &= 0xFE;
					if (Controller.IsPressed("P" + player + " B2")) value &= 0xFD;
					if (Controller.IsPressed("P" + player + " Select")) value &= 0xFB;
					if (Controller.IsPressed("P" + player + " Run")) value &= 0xF7;
				}
				else
				{
					//return directions
					if (Controller.IsPressed("P" + player + " Up")) value &= 0xFE;
					if (Controller.IsPressed("P" + player + " Right")) value &= 0xFD;
					if (Controller.IsPressed("P" + player + " Down")) value &= 0xFB;
					if (Controller.IsPressed("P" + player + " Left")) value &= 0xF7;
				}
			}

			if (Region == "Japan") value |= 0x40;

			if (Type != NecSystemType.TurboCD && BramEnabled == false)
				value |= 0x80;

			return value;
		}
	}
}
