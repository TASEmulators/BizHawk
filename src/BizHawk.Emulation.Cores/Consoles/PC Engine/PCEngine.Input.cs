namespace BizHawk.Emulation.Cores.PCEngine
{
	public partial class PCEngine
	{
		private int _selectedController;
		private byte _inputByte;

		private bool Sel => (_inputByte & 1) != 0;
		private bool Clr => (_inputByte & 2) != 0;

		private void WriteInput(byte value)
		{
			bool prevSel = Sel;
			_inputByte = value;

			if (Sel && Clr)
			{
				_selectedController = 0;
			}

			if (!Clr && !prevSel && Sel)
			{
				_selectedController++;
			}
		}

		private readonly PceControllerDeck _controllerDeck;

		private byte ReadInput()
		{
			InputCallbacks.Call();
			byte value = 0x3F;

			int player = _selectedController + 1;
			if (player < 6)
			{
				_lagged = false;
				value &= _controllerDeck.Read(player, _controller, Sel);
			}

			if (Region == "Japan")
			{
				value |= 0x40;
			}

			if (Type != NecSystemType.TurboCD && !BramEnabled)
			{
				value |= 0x80;
			}

			return value;
		}
	}
}
