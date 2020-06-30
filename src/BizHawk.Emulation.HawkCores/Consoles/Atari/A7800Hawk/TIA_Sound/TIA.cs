namespace BizHawk.Emulation.Cores.Atari.A7800Hawk
{
	// Emulates the TIA
	public sealed partial class TIA
	{
		public A7800Hawk Core { get; set; }

		public byte BusState;

		private bool _doTicks;
		public int AudioClocks; // not savestated
		public int _hsyncCnt;
		private int _capChargeStart;
		private bool _capCharging;

		private readonly Audio[] AUD = { new Audio(), new Audio() };

		// current audio register state used to sample correct positions in the scanline (clrclk 0 and 114)
		////public byte[] current_audio_register = new byte[6];
		public int LocalAudioCycles;

		public void Reset()
		{
			_hsyncCnt = 0;
			_capChargeStart = 0;
			_capCharging = false;
			AudioClocks = 0;
			_doTicks = false;
		}

		// Execute TIA cycles
		public int Execute()
		{
			LocalAudioCycles = 0;
			LocalAudioCycles += AUD[0].Cycle() / 2;
			LocalAudioCycles += AUD[1].Cycle() / 2;

			return LocalAudioCycles;
		}

		public byte ReadMemory(ushort addr, bool peek)
		{
			var maskedAddr = (ushort)(addr & 0x000F);

			if (maskedAddr == 0x00) // CXM0P
			{
				return 0;
			}

			if (maskedAddr == 0x01) // CXM1P
			{
				return 0;
			}

			if (maskedAddr == 0x02) // CXP0FB
			{
				return 0;
			}

			if (maskedAddr == 0x03) // CXP1FB
			{
				return 0;
			}

			if (maskedAddr == 0x04) // CXM0FB
			{
				return 0;
			}

			if (maskedAddr == 0x05) // CXM1FB
			{
				return 0;
			}

			if (maskedAddr == 0x06) // CXBLPF
			{
				return 0;
			}

			if (maskedAddr == 0x07) // CXPPMM
			{
				return 0;
			}

			// inputs 0-3 are measured by a charging capacitor, these inputs are used with the paddles and the keyboard
			// Changing the hard coded value will change the paddle position. The range seems to be roughly 0-56000 according to values from stella
			// 6105 roughly centers the paddle in Breakout

			//INPT0-3 are used to read 2 button joysticks as well for the A7800
			if (maskedAddr == 0x08) // INPT0
			{
				if ((Core.m6532._outputB & 0x04) == 0 && (Core.m6532._ddRb & 0x04) == 0x04)
				{
					Core._isLag = false;
					return (byte)(Core.p1_fire_2x & 0x80);
				}

				return 0;
			}

			if (maskedAddr == 0x09) // INPT1
			{
				if ((Core.m6532._outputB & 0x04) == 0 && (Core.m6532._ddRb & 0x04) == 0x04)
				{
					Core._isLag = false;
					return (byte)((Core.p1_fire_2x & 0x40)<<1);
				}

				return 0;
			}

			if (maskedAddr == 0x0A) // INPT2
			{
				if ((Core.m6532._outputB & 0x10) == 0 && (Core.m6532._ddRb & 0x10) == 0x10)
				{
					Core._isLag = false;
					return (byte)(Core.p2_fire_2x & 0x80);
				}

				return 0;
			}

			if (maskedAddr == 0x0B) // INPT3
			{
				if ((Core.m6532._outputB & 0x10) == 0 && (Core.m6532._ddRb & 0x10) == 0x10)
				{
					Core._isLag = false;
					return (byte)((Core.p2_fire_2x & 0x40)<<1);
				}

				return 0;
			}

			if (maskedAddr == 0x0C) // INPT4
			{
				Core._isLag = false;

				if (!Core.p1_is_2button)
				{
					if (!Core.p1_is_lightgun)
					{
						return Core.p1_fire;
					}

					return Core.lg_1_trigger_hit;
				}

				if ((Core.m6532._outputB & 0x04) != 0 || (Core.m6532._ddRb & 0x04) != 0x04)
				{
					return Core.p1_fire;
				}

				return 0x80;
			}

			if (maskedAddr == 0x0D) // INPT5
			{
				Core._isLag = false;
				if (!Core.p2_is_2button)
				{
					if (!Core.p2_is_lightgun)
					{
						return Core.p2_fire;
					}

					return Core.lg_2_trigger_hit;
				}

				if ((Core.m6532._outputB & 0x10) != 0 || (Core.m6532._ddRb & 0x10) != 0x10)
				{
					return Core.p2_fire;
				}

				return 0x80;
			}

			return 0;

			// some bits of the databus will be undriven when a read call is made. Our goal here is to sort out what
			// happens to the undriven pins. Most of the time, they will be in whatever state they were when previously
			// assigned in some other bus access, so let's go with that. 
			/*
			coll += (byte)(mask & BusState);

			if (!peek)
			{
				BusState = coll;
			}

			return coll;
			*/
		}

		public void WriteMemory(ushort addr, byte value, bool poke)
		{
			var maskedAddr = (ushort)(addr & 0x3f);
			if (!poke)
			{
				BusState = value;
			}

			if (maskedAddr == 0x00) // VSYNC
			{

			}
			else if (maskedAddr == 0x01) // VBLANK
			{

			}
			else if (maskedAddr == 0x02) // WSYNC
			{

			}
			else if (maskedAddr == 0x04) // NUSIZ0
			{

			}
			else if (maskedAddr == 0x05) // NUSIZ1
			{

			}
			else if (maskedAddr == 0x06) // COLUP0
			{

			}
			else if (maskedAddr == 0x07) // COLUP1
			{

			}
			else if (maskedAddr == 0x08) // COLUPF
			{

			}
			else if (maskedAddr == 0x09) // COLUBK
			{

			}
			else if (maskedAddr == 0x0A) // CTRLPF
			{

			}
			else if (maskedAddr == 0x0B) // REFP0
			{

			}
			else if (maskedAddr == 0x0C) // REFP1
			{

			}
			else if (maskedAddr == 0x0D) // PF0
			{

			}
			else if (maskedAddr == 0x0E) // PF1
			{

			}
			else if (maskedAddr == 0x0F) // PF2
			{

			}
			else if (maskedAddr == 0x10) // RESP0
			{

			}
			else if (maskedAddr == 0x11) // RESP1
			{

			}
			else if (maskedAddr == 0x12) // RESM0
			{

			}
			else if (maskedAddr == 0x13) // RESM1
			{

			}
			else if (maskedAddr == 0x14) // RESBL
			{

			}
			else if (maskedAddr == 0x15) // AUDC0
			{
				AUD[0].AUDC = (byte)(value & 15);
			}
			else if (maskedAddr == 0x16) // AUDC1
			{
				AUD[1].AUDC = (byte)(value & 15);
			}
			else if (maskedAddr == 0x17) // AUDF0
			{
				AUD[0].AUDF = (byte)((value & 31) + 1);
			}
			else if (maskedAddr == 0x18) // AUDF1
			{
				AUD[1].AUDF = (byte)((value & 31) + 1);
			}
			else if (maskedAddr == 0x19) // AUDV0
			{
				AUD[0].AUDV = (byte)(value & 15);
			}
			else if (maskedAddr == 0x1A) // AUDV1
			{
				AUD[1].AUDV = (byte)(value & 15);
			}
			else if (maskedAddr == 0x1B) // GRP0
			{

			}
			else if (maskedAddr == 0x1C) // GRP1
			{

			}
			else if (maskedAddr == 0x1D) // ENAM0
			{

			}
			else if (maskedAddr == 0x1E) // ENAM1
			{

			}
			else if (maskedAddr == 0x1F) // ENABL
			{

			}
			else if (maskedAddr == 0x20) // HMP0
			{

			}
			else if (maskedAddr == 0x21) // HMP1
			{

			}
			else if (maskedAddr == 0x22) // HMM0
			{

			}
			else if (maskedAddr == 0x23) // HMM1
			{

			}
			else if (maskedAddr == 0x24) // HMBL
			{

			}
			else if (maskedAddr == 0x25) // VDELP0
			{

			}
			else if (maskedAddr == 0x26) // VDELP1
			{

			}
			else if (maskedAddr == 0x27) // VDELBL
			{

			}
			else if (maskedAddr == 0x28) // RESMP0
			{

			}
			else if (maskedAddr == 0x29) // RESMP1
			{

			}
			else if (maskedAddr == 0x2A) // HMOVE
			{

			}
			else if (maskedAddr == 0x2B) // HMCLR
			{

			}
			else if (maskedAddr == 0x2C) // CXCLR
			{

			}
		}

		private enum AudioRegister : byte
		{
			AUDC, AUDF, AUDV
		}

		private int _frameStartCycles, _frameEndCycles;
	}
}