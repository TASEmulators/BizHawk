namespace BizHawk.Emulation.Cores.PCEngine
{
	public partial class PCEngine
	{
		private int SelectedController;
		private byte InputByte;

		public bool SEL => (InputByte & 1) != 0;
		public bool CLR => (InputByte & 2) != 0;

		private void WriteInput(byte value)
		{
			bool prevSEL = SEL;
			InputByte = value;

			if (SEL && CLR)
			{
				SelectedController = 0;
			}

			if (CLR == false && prevSEL == false && SEL == true)
			{
				SelectedController = (SelectedController + 1);
			}
		}

		private readonly PceControllerDeck _controllerDeck;

		private byte ReadInput()
		{
			InputCallbacks.Call();
			byte value = 0x3F;

			int player = SelectedController + 1;
			if (player < 6)
			{
				_lagged = false;
				value &= _controllerDeck.Read(player, _controller, SEL);
			}

			if (Region == "Japan")
			{
				value |= 0x40;
			}

			if (Type != NecSystemType.TurboCD && BramEnabled == false)
			{
				value |= 0x80;
			}

			return value;
		}
	}
}
