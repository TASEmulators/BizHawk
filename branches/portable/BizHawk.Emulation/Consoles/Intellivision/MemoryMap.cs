using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Intellivision
{
	public sealed partial class Intellivision
	{
		private const ushort UNMAPPED = 0xFFFF;

		private byte[] ScratchpadRam = new byte[240];
		private ushort[] SystemRam = new ushort[352];
		private ushort[] ExecutiveRom = new ushort[4096]; // TODO: Intellivision II support?
		private byte[] GraphicsRom = new byte[2048];
		private byte[] GraphicsRam = new byte[512];

		public ushort ReadMemory(ushort addr)
		{
			ushort? cart = Cart.ReadCart(addr);
			ushort? stic = Stic.ReadSTIC(addr);
			ushort? psg = Psg.ReadPSG(addr);
			ushort? core = null;

			switch (addr & 0xF000)
			{
				case 0x0000:
					if (addr <= 0x007F)
					{
						// STIC.
						break;
					}
					else if (addr <= 0x00FF)
					{
						// Unoccupied.
						break;
					}
					else if (addr <= 0x01EF)
					{
						core = (ushort)(ScratchpadRam[addr - 0x0100] & 0x00FF);
					}
					else if (addr <= 0x01FF)
					{
						// PSG.
						break;
					}
					else if (addr <= 0x035F)
					{
						core = SystemRam[addr - 0x0200];
					}
					else if (addr <= 0x03FF)
					{
						// TODO: Garbage values for Intellivision II.
						break;
					}
					else if (addr <= 0x04FF)
					{
						// TODO: Additional EXEC ROM for Intellivision II.
						break;
					}
					break;
				case 0x1000:
					core = (ushort)(ExecutiveRom[addr - 0x1000] & 0x3FF);
					break;
				case 0x3000:
					if (addr <= 0x37FF)
					{
						// TODO: OK only during VBlank Period 2.
						core = (byte)(GraphicsRom[addr - 0x3000] & 0x00FF);
					}
					else if (addr <= 0x39FF)
					{
						// TODO: OK only during VBlank Period 2.
						core = (byte)(GraphicsRam[addr - 0x3800] & 0x00FF);
					}
					else if (addr <= 0x3BFF)
					{
						// TODO: OK only during VBlank Period 2.
						core = (byte)(GraphicsRam[addr - 0x3A00] & 0x00FF);
					}
					else if (addr <= 0x3DFF)
					{
						// TODO: OK only during VBlank Period 2.
						core = (byte)(GraphicsRam[addr - 0x3C00] & 0x00FF);
					}
					else
					{
						// TODO: OK only during VBlank Period 2.
						core = (byte)(GraphicsRam[addr - 0x3E00] & 0x00FF);
					}
					break;
				case 0x7000:
					if (addr <= 0x77FF)
					{
						// Available to cartridges.
						break;
					}
					else if (addr <= 0x79FF)
					{
						// Write-only Graphics RAM.
						break;
					}
					else if (addr <= 0x7BFF)
					{
						// Write-only Graphics RAM.
						break;
					}
					else if (addr <= 0x7DFF)
					{
						// Write-only Graphics RAM.
						break;
					}
					else
					{
						// Write-only Graphics RAM.
						break;
					}
				case 0xB000:
					if (addr <= 0xB7FF)
					{
						// Available to cartridges.
						break;
					}
					else if (addr <= 0xB9FF)
					{
						// Write-only Graphics RAM.
						break;
					}
					else if (addr <= 0xBBFF)
					{
						// Write-only Graphics RAM.
						break;
					}
					else if (addr <= 0xBDFF)
					{
						// Write-only Graphics RAM.
						break;
					}
					else
					{
						// Write-only Graphics RAM.
						break;
					}
				case 0xF000:
					if (addr <= 0xF7FF)
					{
						// Available to cartridges.
						break;
					}
					else if (addr <= 0xF9FF)
					{
						// Write-only Graphics RAM.
						break;
					}
					else if (addr <= 0xFBFF)
					{
						// Write-only Graphics RAM.
						break;
					}
					else if (addr <= 0xFDFF)
					{
						// Write-only Graphics RAM.
						break;
					}
					else
					{
						// Write-only Graphics RAM.
						break;
					}
			}

			if (CoreComm.MemoryCallbackSystem.HasRead)
			{
				CoreComm.MemoryCallbackSystem.TriggerRead(addr);
			}

			if (cart != null)
			{
				return (ushort)cart;
			}
			else if (stic != null)
			{
				return (ushort)stic;
			}
			else if (psg != null)
			{
				return (ushort)psg;
			}
			else if (core != null)
			{
				return (ushort)core;
			}
			return UNMAPPED;
		}

		public bool WriteMemory(ushort addr, ushort value)
		{
			bool cart = Cart.WriteCart(addr, value);
			bool stic = Stic.WriteSTIC(addr, value);
			bool psg = Psg.WritePSG(addr, value);
			switch (addr & 0xF000)
			{
				case 0x0000:
					if (addr <= 0x007F)
					{
						// STIC.
						break;
					}
					else if (addr <= 0x00FF)
					{
						// Unoccupied.
						break;
					}
					else if (addr <= 0x01EF)
					{
						ScratchpadRam[addr - 0x0100] = (byte)(value & 0x00FF);
						return true;
					}
					else if (addr <= 0x01FF)
					{
						// PSG.
						break;
					}
					else if (addr <= 0x035F)
					{
						SystemRam[addr - 0x0200] = value;
						return true;
					}
					else if (addr <= 0x03FF)
					{
						// Read-only garbage values for Intellivision II.
						break;
					}
					else if (addr <= 0x04FF)
					{
						// Read-only additional EXEC ROM for Intellivision II.
						break;
					}
					break;
				case 0x1000:
					// Read-only Executive ROM.
					break;
				case 0x3000:
					if (addr <= 0x37FF)
					{
						// Read-only Graphics ROM.
						break;
					}
					else if (addr <= 0x39FF)
					{
						// TODO: OK only during VBlank Period 2.
						GraphicsRam[addr - 0x3800] = (byte)(value & 0x00FF);
						return true;
					}
					else if (addr <= 0x3BFF)
					{
						// TODO: OK only during VBlank Period 2.
						GraphicsRam[addr - 0x3A00] = (byte)(value & 0x00FF);
						return true;
					}
					else if (addr <= 0x3DFF)
					{
						// TODO: OK only during VBlank Period 2.
						GraphicsRam[addr - 0x3C00] = (byte)(value & 0x00FF);
						return true;
					}
					else
					{
						// TODO: OK only during VBlank Period 2.
						GraphicsRam[addr - 0x3E00] = (byte)(value & 0x00FF);
						return true;
					}
				case 0x7000:
					if (addr <= 0x77FF)
					{
						// Available to cartridges.
						break;
					}
					else if (addr <= 0x79FF)
					{
						// TODO: OK only during VBlank Period 2.
						GraphicsRam[addr & 0x01FF] = (byte)(value & 0x00FF);
						return true;
					}
					else if (addr <= 0x7BFF)
					{
						// TODO: OK only during VBlank Period 2.
						GraphicsRam[addr & 0x01FF] = (byte)(value & 0x00FF);
						return true;
					}
					else if (addr <= 0x7DFF)
					{
						// TODO: OK only during VBlank Period 2.
						GraphicsRam[addr & 0x01FF] = (byte)(value & 0x00FF);
						return true;
					}
					else
					{
						// TODO: OK only during VBlank Period 2.
						GraphicsRam[addr & 0x01FF] = (byte)(value & 0x00FF);
						return true;
					}
				case 0x9000:
				case 0xA000:
				case 0xB000:
					if (addr <= 0xB7FF)
					{
						// Available to cartridges.
						break;
					}
					else if (addr <= 0xB9FF)
					{
						// TODO: OK only during VBlank Period 2.
						GraphicsRam[addr - 0xB800] = (byte)(value & 0x00FF);
						return true;
					}
					else if (addr <= 0xBBFF)
					{
						// TODO: OK only during VBlank Period 2.
						GraphicsRam[addr - 0xBA00] = (byte)(value & 0x00FF);
						return true;
					}
					else if (addr <= 0xBDFF)
					{
						// TODO: OK only during VBlank Period 2.
						GraphicsRam[addr - 0xBC00] = (byte)(value & 0x00FF);
						return true;
					}
					else
					{
						// TODO: OK only during VBlank Period 2.
						GraphicsRam[addr - 0xBE00] = (byte)(value & 0x00FF);
						return true;
					}
				case 0xF000:
					if (addr <= 0xF7FF)
					{
						// Available to cartridges.
						break;
					}
					else if (addr <= 0xF9FF)
					{
						// TODO: OK only during VBlank Period 2.
						GraphicsRam[addr - 0xF800] = (byte)(value & 0x00FF);
						return true;
					}
					else if (addr <= 0xFBFF)
					{
						// TODO: OK only during VBlank Period 2.
						GraphicsRam[addr - 0xFA00] = (byte)(value & 0x00FF);
						return true;
					}
					else if (addr <= 0xFDFF)
					{
						// TODO: OK only during VBlank Period 2.
						GraphicsRam[addr - 0xFC00] = (byte)(value & 0x00FF);
						return true;
					}
					else
					{
						// TODO: OK only during VBlank Period 2.
						GraphicsRam[addr - 0xFE00] = (byte)(value & 0x00FF);
						return true;
					}
			}

			if (CoreComm.MemoryCallbackSystem.HasWrite)
			{
				CoreComm.MemoryCallbackSystem.TriggerWrite(addr);
			}

			return (cart || stic || psg);
		}
	}
}
