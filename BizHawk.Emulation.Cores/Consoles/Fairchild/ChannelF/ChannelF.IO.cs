using System;
using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.ChannelF
{
	
	public partial class ChannelF
	{
		public byte[] BIOS01 = new byte[1024];
		public byte[] BIOS02 = new byte[1024];

		public byte[] FrameBuffer = new byte[0x2000];

		public byte[] Cartridge = new byte[0x2000 - 0x800];

		public byte[] PortLatch = new byte[64];

		public byte ReadBus(ushort addr)
		{
			if (addr < 0x400)
			{
				// Rom0
				return BIOS01[addr];
			}
			else if (addr < 0x800)
			{
				// Rom1
				return BIOS02[addr - 0x400];
			}
			else if (addr < 0x2000)
			{
				// Cart
				return 0;
				return Cartridge[addr - 0x800];
			}
			else if (addr < 0x2000 + 2048)
			{
				// Framebuffer
				return FrameBuffer[addr - 0x2000];
			}
			else
			{
				return 0xFF;
			}
		}

		public void WriteBus(ushort addr, byte value)
		{
			if (addr < 0x400)
			{
				// Rom0
			}
			else if (addr < 0x800)
			{
				// Rom1
			}
			else if (addr < 0x2000)
			{
				// Cart
			}
			else if (addr < 0x2000 + 2048)
			{
				// Framebuffer
				FrameBuffer[addr - 0x2000] = value;
			}
			else
			{

			}
		}

		public byte ReadPort(ushort addr)
		{
			return 0x00;
		}

		public void WritePort(ushort addr, byte value)
		{
			PortLatch[addr] = value;

			VID_PortIN(addr, value);
		}
	}
}
