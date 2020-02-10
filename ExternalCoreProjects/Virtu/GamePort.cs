namespace Jellyfish.Virtu
{
	public sealed class GamePort : MachineComponent
	{
		// ReSharper disable once UnusedMember.Global
		public GamePort() { }

		public GamePort(Machine machine) :
			base(machine)
		{
		}

		public bool ReadButton0() => Keyboard.WhiteAppleDown;
		public bool ReadButton1() => Keyboard.BlackAppleDown;
		public bool ReadButton2() => false;

		public bool Paddle0Strobe => false;
		public bool Paddle1Strobe => false;
		public bool Paddle2Strobe => false;
		public bool Paddle3Strobe => false;

		public void TriggerTimers() { }
	}
}
