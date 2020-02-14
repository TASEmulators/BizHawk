using Jellyfish.Virtu;

namespace BizHawk.Emulation.Cores.Computers.AppleII
{
	/// <summary>
	/// Currently a default implementation of the GamePort, needs to be built for gamepad support
	/// </summary>
	public class GamePortComponent : IGamePort
	{
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
