using System;
using System.Collections.Generic;

namespace BizHawk.Emulation.Consoles.Atari
{
	// Emulates the TIA
	public class oldTIA
	{
		private readonly Atari2600 core;

		public bool audioEnabled = false;
		public byte audioFreqDiv = 0;

		UInt32 PF; // PlayField data
		byte BKcolor, PFcolor;
		bool PFpriority;
		bool PFreflect;
		bool PFscore;
		bool inpt_latching;
		bool inpt4;
		bool hmoveHappened;

		struct playerData
		{
			public byte grp;
			public byte dgrp;
			public byte color;
			public byte pos;
			public byte HM;
			public bool reflect;
			public bool delay;
/*
			public byte nusiz;
*/
		};
		struct ballMissileData
		{
			public bool enabled;
			public byte pos;
			public byte HM;
			public byte size;
			/*
			public bool reset;
			public bool delay;
			*/

		};

		ballMissileData ball;

		playerData player0;
		playerData player1;

		byte player0copies;
		byte player0copy1;
		byte player0copy2;
		byte player1copies;
		byte player1copy1;
		byte player1copy2;

		byte P0_collisions;
		byte P1_collisions;
		byte M0_collisions;
		byte M1_collisions;
		byte BL_collisions;

		const byte COLP0 = 0x01;
		const byte COLP1 = 0x02;
		const byte COLM0 = 0x04;
		const byte COLM1 = 0x08;
		const byte COLPF = 0x10;
		const byte COLBL = 0x20;

		bool vblankEnabled;

		readonly int[] frameBuffer;
		public bool frameComplete;

		readonly List<uint[]> scanlinesBuffer = new List<uint[]> ();
		uint[] scanline = new uint[160];
		public int scanlinePos;

		readonly int[] hmove = new int[] { 0,-1,-2,-3,-4,-5,-6,-7,-8,7,6,5,4,3,2,1 };

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

		public oldTIA(Atari2600 core, int[] frameBuffer)
		{
			player1copy2 = 0;
			player0copy2 = 0;
			this.core = core;
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
				if (PFreflect)
				{
					PFmask = (UInt32)(1 << ((byte)((pixelPos % 80) / 4)));
				}
				else
				{
					PFmask = (UInt32)(1 << ((20 - 1) - (byte)((pixelPos % 80) / 4)));
				}
			}

			uint color = palette[BKcolor];
			byte collisions = 0;

			if ((PF & PFmask) != 0)
			{
				color = palette[PFcolor];
				if (PFscore)
				{
					if (pixelPos < 80)
					{
						color = palette[player0.color];
					}
					else
					{
						color = palette[player1.color];
					}
				}
				collisions |= COLPF;
			}

			// Ball
			if (ball.enabled && pixelPos >= ball.pos && pixelPos < (ball.pos + (1 << ball.size)))
			{
				color = palette[PFcolor];
				collisions |= COLBL;
			}

			// Player 1
			if (pixelPos >= player1.pos && pixelPos < (player1.pos + 8))
			{
				byte mask = (byte)(0x80 >> (pixelPos - player1.pos));
				if (player1.reflect)
				{
					mask = reverseBits(mask);
				}

				if (((player1.grp & mask) != 0 && !player1.delay) || ((player1.dgrp & mask) != 0 && player1.delay))
				{
					color = palette[player1.color];
					collisions |= COLP1;
				}
			}

			byte pos = (byte)(player1.pos + player1copy1);
			// Player copy 1
			if (player1copies >= 1 && pixelPos >= pos && pixelPos < (pos + 8))
			{
				byte mask = (byte)(0x80 >> (pixelPos - pos));
				if (player1.reflect)
				{
					mask = reverseBits(mask);
				}

				if (((player1.grp & mask) != 0 && !player1.delay) || ((player1.dgrp & mask) != 0 && player1.delay))
				{
					color = palette[player1.color];
					collisions |= COLP1;
				}
			}

			pos = (byte)(player1.pos + player1copy2);
			// Player copy 2
			if (player1copies >=2 && pixelPos >= pos && pixelPos < (pos + 8))
			{
				byte mask = (byte)(0x80 >> (pixelPos - pos));
				if (player1.reflect)
				{
					mask = reverseBits(mask);
				}

				if (((player1.grp & mask) != 0 && !player1.delay) || ((player1.dgrp & mask) != 0 && player1.delay))
				{
					color = palette[player1.color];
					collisions |= COLP1;
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
				if (((player0.grp & mask) != 0 && !player0.delay) || ((player0.dgrp & mask) != 0 && player0.delay))
				{
					color = palette[player0.color];
					collisions |= COLP0;
				}
			}

			pos = (byte)(player0.pos + player0copy1);
			// Player copy 1
			if (player0copies >= 1 && pixelPos >= pos && pixelPos < (pos + 8))
			{
				byte mask = (byte)(0x80 >> (pixelPos - pos));
				if (player0.reflect)
				{
					mask = reverseBits(mask);
				}

				if (((player0.grp & mask) != 0 && !player0.delay) || ((player0.dgrp & mask) != 0 && player0.delay))
				{
					color = palette[player0.color];
					collisions |= COLP0;
				}
			}

			pos = (byte)(player0.pos + player0copy2);
			// Player copy 1
			if (player0copies >= 2 && pixelPos >= pos && pixelPos < (pos + 8))
			{
				byte mask = (byte)(0x80 >> (pixelPos - pos));
				if (player0.reflect)
				{
					mask = reverseBits(mask);
				}

				if (((player0.grp & mask) != 0 && !player0.delay) || ((player0.dgrp & mask) != 0 && player0.delay))
				{
					color = palette[player0.color];
					collisions |= COLP0;
				}
			}

			if ((PF & PFmask) != 0 && PFpriority)
			{
				color = palette[PFcolor];
				if (PFscore)
				{
					if (pixelPos < 80)
					{
						color = palette[player0.color];
					}
					else
					{
						color = palette[player1.color];
					}
				}
				collisions |= COLPF;
			}

			if (vblankEnabled)
			{
				color = 0x000000;
			}

			P0_collisions |= (((collisions & COLP0) != 0) ? collisions : P0_collisions);
			P1_collisions |= (((collisions & COLP1) != 0) ? collisions : P1_collisions);
			M0_collisions |= (((collisions & COLM0) != 0) ? collisions : M0_collisions);
			M1_collisions |= (((collisions & COLM1) != 0) ? collisions : M1_collisions);
			BL_collisions |= (((collisions & COLBL) != 0) ? collisions : BL_collisions);

			if (hmoveHappened && pixelPos >= 0 && pixelPos < 8)
			{
				color = 0x000000;
			}
			if (pixelPos >= 8)
			{
				hmoveHappened = false;
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

		public byte ReadMemory(ushort addr, bool peek)
		{
			ushort maskedAddr = (ushort)(addr & 0x000F);
			Console.WriteLine("TIA read:  " + maskedAddr.ToString("x"));
			if (maskedAddr == 0x02) // CXP0FB
			{
				return (byte)((((P0_collisions & COLPF) != 0) ? 0x80 : 0x00) | (((P0_collisions & COLBL) != 0) ? 0x40 : 0x00));
			}
			else if (maskedAddr == 0x07) // CXPPMM
			{
				return (byte)((((P0_collisions & COLP1) != 0) ? 0x80 : 0x00) | (((M0_collisions & COLM1) != 0) ? 0x40 : 0x00));
			}
			else if (maskedAddr == 0x0C) // INPT4
			{
				if (inpt_latching)
				{
					if (inpt4)
					{
						inpt4 = ((core.ReadControls1(peek) & 0x08) != 0);
					}
				}
				else
				{
					inpt4 = ((core.ReadControls1(peek) & 0x08) != 0);
				}
				return (byte)(inpt4 ? 0x80 : 0x00); 

			}

			return 0x80;
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
				inpt_latching = (value & 0x40) != 0;
			}
			else if (maskedAddr == 0x02) // WSYNC
			{
				Console.WriteLine("TIA WSYNC");
				while (scanlinePos > 0)
				{
					execute(1);
				}
			}
			else if (maskedAddr == 0x04) // NUSIZ0
			{
				byte size = (byte)(value & 0x07);
				switch (size)
				{
					case 0x00:
						player0copies = 0;
						break;
					case 0x01:
						player0copies = 1;
						player0copy1 = 16;
						break;
					case 0x02:
						player0copies = 1;
						player0copy1 = 32;
						break;
					case 0x03:
						player0copies = 2;
						player0copy1 = 16;
						player0copy2 = 32;
						break;
					case 0x06:
						player0copies = 2;
						player0copy1 = 32;
						player0copy2 = 64;
						break;
				}
			}
			else if (maskedAddr == 0x05) // NUSIZ1
			{
				byte size = (byte)(value & 0x07);
				switch (size)
				{
					case 0x00:
						player1copies = 0;
						break;
					case 0x01:
						player1copies = 1;
						player1copy1 = 16;
						break;
					case 0x02:
						player1copies = 1;
						player1copy1 = 32;
						break;
					case 0x03:
						player1copies = 2;
						player1copy1 = 16;
						player1copy2 = 32;
						break;
					case 0x06:
						player1copies = 2;
						player1copy1 = 32;
						player1copy2 = 64;
						break;
				}
			}
			else if (maskedAddr == 0x06) // COLUP0
			{
				player0.color = (byte)(value & 0xFE);
			}
			else if (maskedAddr == 0x07) // COLUP1
			{
				player1.color = (byte)(value & 0xFE);
			}
			else if (maskedAddr == 0x08) // COLUPF
			{
				PFcolor = (byte)(value & 0xFE);
			}
			else if (maskedAddr == 0x09) // COLUBK
			{
				BKcolor = (byte)(value & 0xFE);
			}
			else if (maskedAddr == 0x0A) // CTRLPF
			{
				PFpriority = (value & 0x04) != 0;
				PFreflect = (value & 0x01) != 0;
				PFscore = (value & 0x02) != 0;

				ball.size = (byte)((value & 0x30) >> 4);
			}
			else if (maskedAddr == 0x0B) // REFP0
			{
				player0.reflect = ((value & 0x08) != 0);
			}
			else if (maskedAddr == 0x0C) // REFP1
			{
				player1.reflect = ((value & 0x08) != 0);
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
				PF = (PF & 0xFFF00) + reverseBits(value);
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
			else if (maskedAddr == 0x15) // AUDC0
			{
				audioEnabled = value != 0;
			}
			else if (maskedAddr == 0x17) // AUDF0
			{
				audioFreqDiv = (byte)(value + 1);
			}
			else if (maskedAddr == 0x1B) // GRP0
			{
				player0.grp = value;
				player1.dgrp = player1.grp;
			}
			else if (maskedAddr == 0x1C) // GRP1
			{
				player1.grp = value;
				player0.dgrp = player0.grp;
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
			else if (maskedAddr == 0x25) // VDELP0
			{
				player0.delay = (value & 0x01) != 0;
			}
			else if (maskedAddr == 0x26) // VDELP1
			{
				player1.delay = (value & 0x01) != 0;
			}
			else if (maskedAddr == 0x2A) // HMOVE
			{
				player0.pos = (byte)(player0.pos + hmove[player0.HM]);
				player1.pos = (byte)(player1.pos + hmove[player1.HM]);
				ball.pos = (byte)(ball.pos + hmove[ball.HM]);

				player0.HM = 0;
				player1.HM = 0;
				ball.HM = 0;

				hmoveHappened = true;
			}
			else if (maskedAddr == 0x2C) // CXCLR
			{
				P0_collisions = 0;
				P1_collisions = 0;
				M0_collisions = 0;
				M1_collisions = 0;
				BL_collisions = 0;
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