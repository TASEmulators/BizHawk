using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BizHawk.Emulation.CPUs.Z80;
using BizHawk.Emulation.Sound;
using BizHawk.Emulation.Consoles.Sega;

namespace BizHawk.Emulation.Consoles.Coleco
{
	public partial class ColecoVision : IEmulator
	{
		public byte[] rom = new byte[2048];
		public byte[] expansion = new byte[0x4000];
		public byte[] cartridgeslot = new byte[0xFFFF]; //TODO: how big should this be?
		public Z80A cpu;
		public VDP Vdp; //adelikat: Using the SMS one for now

		public byte ReadMemory(ushort addr)
		{
			if (addr < 0x2000)
			{
				return rom[addr];
			}
			else if (addr >= 0x2000 && addr < 0x6000)
			{
				return expansion[addr];
			}
			else if (addr >= 0x6000 && addr < 0x8000)
			{
				return ram[addr & 1023];
			}
			else if (addr >= 0x8000)
			{
				return cartridgeslot[addr];
			}

			else return 0xFF;
		}

		public void WriteMemory(ushort addr, byte value)
		{
			if (addr >= 0x6000 && addr < 0x8000)
			{
				ram[addr] = value;
			}
		}

		public void HardReset()
		{
			_lagcount = 0;
			cpu = new Z80A();
			Vdp = new VDP(this, cpu, VdpMode.SMS, DisplayType);
			cpu.ReadMemory = ReadMemory;
			cpu.WriteMemory = WriteMemory;
		}

		public void FrameAdvance(bool render, bool rendersound)
		{
			_frame++;
			_islag = true;

			Vdp.ExecFrame(render);

			//if (render == false) return;
			//for (int i = 0; i < 256 * 192; i++)
			//	frameBuffer[i] = 0; //black

			if (_islag)
				_lagcount++;
		}

		public byte ReadControls()
		{
			if (CoreInputComm.InputCallback != null) CoreInputComm.InputCallback();
			byte value = 0xFF;

			if (Controller["P1 Up"]) value &= 0xFF; //TODO;
			if (Controller["P1 Down"]) value &= 0xFF; //TODO;
			if (Controller["P1 Left"]) value &= 0xFF; //TODO;
			if (Controller["P1 Right"]) value &= 0xFF; //TODO;
			//TODO: remaining buttons

			_islag = false;
			return value;

		}
	}
}
