namespace Jellyfish.Virtu
{
	public interface IGamePort
	{
		bool ReadButton0();
		bool ReadButton1();
		bool ReadButton2();

		bool Paddle0Strobe { get; }
		bool Paddle1Strobe { get; }
		bool Paddle2Strobe { get; }
		bool Paddle3Strobe { get; }

		void TriggerTimers();
	}
}
