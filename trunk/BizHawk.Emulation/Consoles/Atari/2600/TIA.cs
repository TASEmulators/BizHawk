using System;
using System.Globalization;
using System.IO;
using BizHawk.Emulation.CPUs.M6507;
using System.Collections.Generic;

namespace BizHawk.Emulation.Consoles.Atari
{
	// Emulates the TIA
	public partial class TIA
	{
		MOS6507 Cpu;
		UInt32 PF; // PlayField data
		byte BKcolor, PFcolor;
		bool PFpriority = false;

		struct playerData
		{
			public byte grp;
			public byte color;
			public byte pos;
			public byte HM;
			public bool reflect;
			public bool delay;
			public byte nusiz;
		};
		struct ballMissileData
		{
			public bool enabled;
			public byte pos;
			public byte HM;
			public byte size;
			public bool reset;
			public bool delay;
		};

		ballMissileData ball;

		playerData player0;
		playerData player1;

		bool vblankEnabled = false;


		int[] frameBuffer;
		public bool frameComplete;

		List<uint[]> scanlinesBuffer = new List<uint[]> ();
		uint[] scanline = new uint[160];
		public int scanlinePos;

		int[] hmove = new int[] { 0,-1,-2,-3,-4,-5,-6,-7,-8,7,6,5,4,3,2,1 };

		UInt32[] palette = new UInt32[]{
		  0x000000, 0, 0x4a4a4a, 0, 0x6f6f6f, 0, 0x8e8e8e, 0,
		  0xaaaaaa, 0, 0xc0c0c0, 0, 0xd6d6d6, 0, 0xececec, 0,
		  0x484800, 0, 0x69690f, 0, 0x86861d, 0, 0xa2a22a, 0,
		  0xbbbb35, 0, 0xd2d240, 0, 0xe8e84a, 0, 0xfcfc54, 0,
		  0x7c2c00, 0, 0x904811, 0, 0xa26221, 0, 0xb47a30, 0,
		  0xc3903d, 0, 0xd2a44a, 0, 0xdfb755, 0, 0xecc860, 0,
		  0x901c00, 0, 0xa33915, 0, 0xb55328, 0, 0xc66c3a, 0,
		  0xd5824a, 0, 0xe39759, 0, 0xf0aa67, 0, 0xfcbc74, 0,
		  0x940000, 0, 0xa71a1a, 0, 0xb83232, 0, 0xc84848, 0,
		  0xd65c5c, 0, 0xe46f6f, 0, 0xf08080, 0, 0xfc9090, 0,
		  0x840064, 0, 0x97197a, 0, 0xa8308f, 0, 0xb846a2, 0,
		  0xc659b3, 0, 0xd46cc3, 0, 0xe07cd2, 0, 0xec8ce0, 0,
		  0x500084, 0, 0x68199a, 0, 0x7d30ad, 0, 0x9246c0, 0,
		  0xa459d0, 0, 0xb56ce0, 0, 0xc57cee, 0, 0xd48cfc, 0,
		  0x140090, 0, 0x331aa3, 0, 0x4e32b5, 0, 0x6848c6, 0,
		  0x7f5cd5, 0, 0x956fe3, 0, 0xa980f0, 0, 0xbc90fc, 0,
		  0x000094, 0, 0x181aa7, 0, 0x2d32b8, 0, 0x4248c8, 0,
		  0x545cd6, 0, 0x656fe4, 0, 0x7580f0, 0, 0x8490fc, 0,
		  0x001c88, 0, 0x183b9d, 0, 0x2d57b0, 0, 0x4272c2, 0,
		  0x548ad2, 0, 0x65a0e1, 0, 0x75b5ef, 0, 0x84c8fc, 0,
		  0x003064, 0, 0x185080, 0, 0x2d6d98, 0, 0x4288b0, 0,
		  0x54a0c5, 0, 0x65b7d9, 0, 0x75cceb, 0, 0x84e0fc, 0,
		  0x004030, 0, 0x18624e, 0, 0x2d8169, 0, 0x429e82, 0,
		  0x54b899, 0, 0x65d1ae, 0, 0x75e7c2, 0, 0x84fcd4, 0,
		  0x004400, 0, 0x1a661a, 0, 0x328432, 0, 0x48a048, 0,
		  0x5cba5c, 0, 0x6fd26f, 0, 0x80e880, 0, 0x90fc90, 0,
		  0x143c00, 0, 0x355f18, 0, 0x527e2d, 0, 0x6e9c42, 0,
		  0x87b754, 0, 0x9ed065, 0, 0xb4e775, 0, 0xc8fc84, 0,
		  0x303800, 0, 0x505916, 0, 0x6d762b, 0, 0x88923e, 0,
		  0xa0ab4f, 0, 0xb7c25f, 0, 0xccd86e, 0, 0xe0ec7c, 0,
		  0x482c00, 0, 0x694d14, 0, 0x866a26, 0, 0xa28638, 0,
		  0xbb9f47, 0, 0xd2b656, 0, 0xe8cc63, 0, 0xfce070, 0
		};

		public TIA(MOS6507 cpu, int[] frameBuffer)
		{
			Cpu = cpu;
			BKcolor = 0x00;
			this.frameBuffer = frameBuffer;
			scanlinePos = 0;
			frameComplete = false;
		}

		public void execute(int cycles)
		{
			// Ignore cycles for now, just do one cycle (three color counts/pixels)

			if (scanlinePos < 68)
			{
				scanlinePos ++;
				// HBLANK
				return;
			}

			UInt32 PFmask;

			int pixelPos = scanlinePos - 68;

			// First half of screen
			if (pixelPos < 80)
			{
				PFmask = (UInt32)(1 << ((20-1) - (byte)((pixelPos % 80) / 4)));
			}
			// Second half
			else
			{
				PFmask = (UInt32)(1 << ((byte)((pixelPos % 80) / 4)));
			}

			UInt32 color;
			color = palette[BKcolor];

			if ((PF & PFmask) != 0)
			{
				color = palette[PFcolor];
			}

			// Ball
			if (ball.enabled && pixelPos >= ball.pos && pixelPos < (ball.pos + (1 << ball.size)))
			{
				color = palette[PFcolor];
			}

			// Player 1
			if (pixelPos >= player1.pos && pixelPos < (player1.pos + 8))
			{
				byte mask = (byte)(0x80 >> (pixelPos - player1.pos));
				if (player1.reflect)
				{
					mask = reverseBits(mask);
				}
				if ((player1.grp & mask) != 0)
				{
					color = palette[player1.color];
				}
			}
			
			// Player 0
			if (pixelPos >= player0.pos && pixelPos < (player0.pos + 8))
			{
				byte mask = (byte)(0x80 >> (pixelPos - player0.pos));
				if (player0.reflect)
				{
					mask = reverseBits(mask);
				}
				if ((player0.grp & mask) != 0)
				{
					color = palette[player0.color];
				}
			}

			if ((PF & PFmask) != 0 && PFpriority == true)
			{
				color = palette[PFcolor];
			}

			if (vblankEnabled)
			{
				color = 0x000000;
			}
			scanline[pixelPos]   = color;

			scanlinePos++;
			if (scanlinePos >= 228)
			{
				scanlinesBuffer.Add(scanline);
				scanline = new uint[160];
			}
			scanlinePos %= 228;

		}

		public void outputFrame()
		{
			for (int row = 0; row < 262; row++)
			{
				for (int col = 0; col < 320; col++)
				{
					if (scanlinesBuffer.Count > row)
					{
						frameBuffer[row * 320 + col] = (int)(scanlinesBuffer[row][col / 2]);
					}
					else
					{
						frameBuffer[row * 320 + col] = 0x000000;
					}
				}
			}
		}

		public byte ReadMemory(ushort addr)
		{
			ushort maskedAddr = (ushort)(addr & 0x3f);
			Console.WriteLine("TIA read:  " + maskedAddr.ToString("x"));

			return 0x3A;
		}

		public void WriteMemory(ushort addr, byte value)
		{
			ushort maskedAddr = (ushort)(addr & 0x3f);
			Console.WriteLine("TIA write:  " + maskedAddr.ToString("x"));

			if (maskedAddr == 0x00)
			{
				if ((value & 0x02) != 0)
				{
					Console.WriteLine("TIA VSYNC On");
					// Frame is complete, output to buffer
					outputFrame();
					scanlinesBuffer.Clear();
					frameComplete = true;
					scanlinePos = 0;
				}
				else
				{
					Console.WriteLine("TIA VSYNC Off");
				}
			}
			else if (maskedAddr == 0x01)
			{
				vblankEnabled = (value & 0x02) != 0;
				if ((value & 0x02) != 0)
				{
					Console.WriteLine("TIA Vblank On");
				}
				else
				{
					Console.WriteLine("TIA Vblank Off");
				}
			}
			else if (maskedAddr == 0x02) // WSYNC
			{
				Console.WriteLine("TIA WSYNC");
				while (scanlinePos > 0)
				{
					execute(1);
				}
			}
			else if (maskedAddr == 0x06) // COLUP0
			{
				player0.color = value;
			}
			else if (maskedAddr == 0x07) // COLUP1
			{
				player1.color = value;
			}
			else if (maskedAddr == 0x08) // COLUPF
			{
				PFcolor = value;
			}
			else if (maskedAddr == 0x09) // COLUBK
			{
				BKcolor = value;
			}
			else if (maskedAddr == 0x0A) // CTRLPF
			{
				PFpriority = (value & 0x04) != 0;

				ball.size = (byte)((value & 0x30) >> 4);
			}
			else if (maskedAddr == 0x0B) // REFP0
			{
				player0.reflect = ((value & 0x04) != 0);
			}
			else if (maskedAddr == 0x0C) // REFP1
			{
				player1.reflect = ((value & 0x04) != 0);
			}
			else if (maskedAddr == 0x0D) // PF0
			{
				PF = (UInt32)((PF & 0x0FFFF) + ((reverseBits(value) & 0x0F) << 16));
			}
			else if (maskedAddr == 0x0E) // PF1
			{
				PF = (UInt32)((PF & 0xF00FF) + (value << 8));
			}
			else if (maskedAddr == 0x0F) // PF2
			{
				PF = (UInt32)((PF & 0xFFF00) + reverseBits(value));
			}
			else if (maskedAddr == 0x10) // RESP0
			{
				player0.pos = (byte)(scanlinePos - 68 + 5);
			}
			else if (maskedAddr == 0x11) // RESP1
			{
				player1.pos = (byte)(scanlinePos - 68 + 5);
			}
			else if (maskedAddr == 0x14) // RESBL
			{
				ball.pos = (byte)(scanlinePos - 68 + 4);
			}
			else if (maskedAddr == 0x1B) // GRP0
			{
				player0.grp = value;
			}
			else if (maskedAddr == 0x1C) // GRP1
			{
				player1.grp = value;
			}
			else if (maskedAddr == 0x1F) // ENABL
			{
				ball.enabled = (value & 0x02) != 0;
			}
			else if (maskedAddr == 0x20) // HMP0
			{
				player0.HM = (byte)((value & 0xF0) >> 4);
			}
			else if (maskedAddr == 0x21) // HMP1
			{
				player1.HM = (byte)((value & 0xF0) >> 4);
			}
			else if (maskedAddr == 0x24) // HMBL
			{
				ball.HM = (byte)((value & 0xF0) >> 4);
			}
			else if (maskedAddr == 0x2A) // HMOVE
			{
				player0.pos = (byte)(player0.pos + hmove[player0.HM]);
				player1.pos = (byte)(player1.pos + hmove[player1.HM]);
				ball.pos = (byte)(ball.pos + hmove[ball.HM]);

				player0.HM = 0;
				player1.HM = 0;
				ball.HM = 0;

			}
		}

		public byte reverseBits(byte value)
		{
			byte result = 0x00;
			for (int i = 0; i < 8; i++)
			{
				result = (byte)(result | (((value >> i) & 0x01 ) << (7-i)));
			}
			return result;
		}
	}
}