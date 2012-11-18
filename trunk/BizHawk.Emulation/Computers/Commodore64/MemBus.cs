using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public enum MemoryDesignation
	{
		Disabled,
		RAM,
		Basic,
		Kernal,
		IO,
		Character,
		ROMLo,
		ROMHi,
		Vic,
		Sid,
		ColorRam,
		Cia0,
		Cia1,
		Expansion0,
		Expansion1
	}

	public class MemoryLayout
	{
		public MemoryDesignation Mem1000 = MemoryDesignation.RAM;
		public MemoryDesignation Mem8000 = MemoryDesignation.RAM;
		public MemoryDesignation MemA000 = MemoryDesignation.RAM;
		public MemoryDesignation MemC000 = MemoryDesignation.RAM;
		public MemoryDesignation MemD000 = MemoryDesignation.RAM;
		public MemoryDesignation MemE000 = MemoryDesignation.RAM;
	}

	public class Memory
	{
		// chips
		public Cia cia0;
		public Cia cia1;
		public VicIINew vic;
		public Sid sid;

		// storage
		public Cartridge cart;

		// roms
		public byte[] basicRom;
		public byte[] charRom;
		public bool exRomPin = true;
		public bool gamePin = true;
		public byte[] kernalRom;
		public MemoryLayout layout;

		// ram
		public byte[] colorRam;
		public byte[] ram;
		public bool vicCharEnabled;
		public ushort vicOffset;

		// registers
		public byte busData;
		public bool readTrigger = true;
		public bool writeTrigger = true;

		// ports
		public DataPortConnector cpuPort;
		public DataPortBus cpuPortBus = new DataPortBus();

		void HandleFirmwareError(string file)
		{
			System.Windows.Forms.MessageBox.Show("the C64 core is referencing a firmware file which could not be found. Please make sure it's in your configured C64 firmwares folder. The referenced filename is: " + file);
			throw new FileNotFoundException();
		}

		public Memory(string sourceFolder, VicIINew newVic, Sid newSid, Cia newCia0, Cia newCia1)
		{
			string basicFile = "basic";
			string charFile = "chargen";
			string kernalFile = "kernal";

			string basicPath = Path.Combine(sourceFolder, basicFile);
			string charPath = Path.Combine(sourceFolder, charFile);
			string kernalPath = Path.Combine(sourceFolder, kernalFile);

			if (!File.Exists(basicPath)) HandleFirmwareError(basicFile);
			if (!File.Exists(charPath)) HandleFirmwareError(charFile);
			if (!File.Exists(kernalPath)) HandleFirmwareError(kernalFile);

			basicRom = File.ReadAllBytes(basicPath);
			charRom = File.ReadAllBytes(charPath);
			kernalRom = File.ReadAllBytes(kernalPath);

			vic = newVic;
			sid = newSid;
			cia0 = newCia0;
			cia1 = newCia1;

			cpuPort = cpuPortBus.Connect();
			cpuPortBus.AttachWriteHook(UpdateLayout);
			cia1.AttachWriteHook(0, UpdateVicOffset);
			HardReset();
		}

		public byte this[ushort index]
		{
			get
			{
				return ram[index];
			}
			set
			{
				ram[index] = value;
			}
		}

		public MemoryDesignation GetDesignation(ushort addr)
		{
			MemoryDesignation result;

			if (addr < 0x1000)
			{
				result = MemoryDesignation.RAM;
			}
			else if (addr < 0x8000)
			{
				result = layout.Mem1000;
			}
			else if (addr < 0xA000)
			{
				result = layout.Mem8000;
			}
			else if (addr < 0xC000)
			{
				result = layout.MemA000;
			}
			else if (addr < 0xD000)
			{
				result = layout.MemC000;
			}
			else if (addr < 0xE000)
			{
				result = layout.MemD000;
			}
			else
			{
				result = layout.MemE000;
			}

			if (result == MemoryDesignation.IO)
			{
				addr &= 0x0FFF;
				if (addr < 0x0400)
				{
					result = MemoryDesignation.Vic;
				}
				else if (addr < 0x0800)
				{
					result = MemoryDesignation.Sid;
				}
				else if (addr < 0x0C00)
				{
					result = MemoryDesignation.ColorRam;
				}
				else if (addr < 0x0D00)
				{
					result = MemoryDesignation.Cia0;
				}
				else if (addr < 0x0E00)
				{
					result = MemoryDesignation.Cia1;
				}
				else if (addr < 0x0F00)
				{
					result = MemoryDesignation.Expansion0;
				}
				else
				{
					result = MemoryDesignation.Expansion1;
				}
			}

			return result;
		}

		public void HardReset()
		{
			layout = new MemoryLayout();

			ram = new byte[0x10000];
			colorRam = new byte[0x1000];
			WipeMemory();

			cpuPort.Direction = 0x2F;
			cpuPort.Data = 0x37;

			UpdateVicOffset();
		}

		public byte Peek(ushort addr)
		{
			byte result;

			if (addr == 0x0000)
			{
				result = cpuPort.Direction;
			}
			else if (addr == 0x0001)
			{
				result = cpuPort.Data;
			}
			else
			{
				MemoryDesignation des = GetDesignation(addr);

				switch (des)
				{
					case MemoryDesignation.Basic:
						result = basicRom[addr & 0x1FFF];
						break;
					case MemoryDesignation.Character:
						result = charRom[addr & 0x0FFF];
						break;
					case MemoryDesignation.Vic:
						result = vic.Peek(addr & 0x3F);
						break;
					case MemoryDesignation.Sid:
						result = sid.Peek(addr & 0x1F);
						break;
					case MemoryDesignation.ColorRam:
						result = (byte)(colorRam[addr & 0x03FF] | (busData & 0xF0));
						break;
					case MemoryDesignation.Cia0:
						result = cia0.Peek(addr & 0x0F);
						break;
					case MemoryDesignation.Cia1:
						result = cia1.Peek(addr & 0x0F);
						break;
					case MemoryDesignation.Expansion0:
						result = 0;
						break;
					case MemoryDesignation.Expansion1:
						result = 0;
						break;
					case MemoryDesignation.Kernal:
						result = kernalRom[addr & 0x1FFF];
						break;
					case MemoryDesignation.RAM:
						result = ram[addr];
						break;
					case MemoryDesignation.ROMHi:
						result = cart.chips[cart.bank].data[addr & cart.chips[cart.bank].romMask];
						break;
					case MemoryDesignation.ROMLo:
						result = cart.chips[cart.bank].data[addr & cart.chips[cart.bank].romMask];
						break;
					default:
						return 0;
				}
			}

			busData = result;
			return result;
		}

		public byte PeekRam(int addr)
		{
			return ram[addr & 0xFFFF];
		}

		public void Poke(ushort addr, byte val)
		{
			return;
			/*
			if (addr == 0x0000)
			{
				cpuPort.Direction = val;
			}
			else if (addr == 0x0001)
			{
				cpuPort.Data = val;
				UpdateLayout();
			}
			else
			{
				MemoryDesignation des = GetDesignation(addr);

				switch (des)
				{
					case MemoryDesignation.Vic:
						vic.Poke(addr, val);
						break;
					case MemoryDesignation.Sid:
						sid.Poke(addr, val);
						break;
					case MemoryDesignation.ColorRam:
						colorRam[addr & 0x03FF] = (byte)(val & 0x0F);
						break;
					case MemoryDesignation.Cia0:
						cia0.Poke(addr, val);
						break;
					case MemoryDesignation.Cia1:
						cia1.Poke(addr, val);
						break;
					case MemoryDesignation.Expansion0:
						if (cart != null)
							cart.WritePort(addr, val);
						break;
					case MemoryDesignation.Expansion1:
						break;
					case MemoryDesignation.RAM:
						break;
					default:
						break;
				}

				// write through to ram
				if (des != MemoryDesignation.Disabled)
				{
					ram[addr] = val;
				}
			}
			 */
		}

		public void PokeRam(int addr, byte val)
		{
			ram[addr & 0xFFFF] = val;
		}

		public byte Read(ushort addr)
		{
			byte result;

			if (addr == 0x0000)
			{
				result = cpuPort.Direction;
			}
			else if (addr == 0x0001)
			{
				result = cpuPort.Data;
			}
			else
			{
				MemoryDesignation des = GetDesignation(addr);

				switch (des)
				{
					case MemoryDesignation.Basic:
						result = basicRom[addr & 0x1FFF];
						break;
					case MemoryDesignation.Character:
						result = charRom[addr & 0x0FFF];
						break;
					case MemoryDesignation.Vic:
						result = vic.Read(addr);
						break;
					case MemoryDesignation.Sid:
						result = sid.Read(addr);
						break;
					case MemoryDesignation.ColorRam:
						result = ReadColorRam(addr);
						break;
					case MemoryDesignation.Cia0:
						result = cia0.Read(addr);
						break;
					case MemoryDesignation.Cia1:
						result = cia1.Read(addr);
						break;
					case MemoryDesignation.Expansion0:
						if (cart != null)
							result = cart.ReadPort(addr);
						else
							result = 0;
						break;
					case MemoryDesignation.Expansion1:
						result = 0;
						break;
					case MemoryDesignation.Kernal:
						result = kernalRom[addr & 0x1FFF];
						break;
					case MemoryDesignation.RAM:
						result = ram[addr];
						break;
					case MemoryDesignation.ROMHi:
						result = cart.Read(addr);
						break;
					case MemoryDesignation.ROMLo:
						result = cart.Read(addr);
						break;
					default:
						return 0;
				}
			}

			busData = result;
			return result;
		}

		public byte ReadColorRam(ushort addr)
		{
			return (byte)((busData & 0xF0) | (colorRam[addr & 0x03FF]));
		}

		public void UpdateLayout()
		{
			byte cpuData = cpuPort.Data;
			bool loRom = ((cpuData & 0x01) != 0);
			bool hiRom = ((cpuData & 0x02) != 0);
			bool ioEnable = ((cpuData & 0x04) != 0);

			if (loRom && hiRom && gamePin && exRomPin)
			{
				layout.Mem1000 = MemoryDesignation.RAM;
				layout.Mem8000 = MemoryDesignation.RAM;
				layout.MemA000 = MemoryDesignation.Basic;
				layout.MemC000 = MemoryDesignation.RAM;
				layout.MemD000 = ioEnable ? MemoryDesignation.IO : MemoryDesignation.Character;
				layout.MemE000 = MemoryDesignation.Kernal;
			}
			else if (loRom && !hiRom && gamePin)
			{
				layout.Mem1000 = MemoryDesignation.RAM;
				layout.Mem8000 = MemoryDesignation.RAM;
				layout.MemA000 = MemoryDesignation.RAM;
				layout.MemC000 = MemoryDesignation.RAM;
				layout.MemD000 = ioEnable ? MemoryDesignation.IO : MemoryDesignation.Character;
				layout.MemE000 = MemoryDesignation.RAM;
			}
			else if (loRom && !hiRom && !exRomPin && !gamePin)
			{
				layout.Mem1000 = MemoryDesignation.RAM;
				layout.Mem8000 = MemoryDesignation.RAM;
				layout.MemA000 = MemoryDesignation.RAM;
				layout.MemC000 = MemoryDesignation.RAM;
				layout.MemD000 = ioEnable ? MemoryDesignation.IO : MemoryDesignation.RAM;
				layout.MemE000 = MemoryDesignation.RAM;
			}
			else if ((!loRom && hiRom && gamePin) || (!loRom && !hiRom && !exRomPin))
			{
				layout.Mem1000 = MemoryDesignation.RAM;
				layout.Mem8000 = MemoryDesignation.RAM;
				layout.MemA000 = MemoryDesignation.RAM;
				layout.MemC000 = MemoryDesignation.RAM;
				layout.MemD000 = ioEnable ? MemoryDesignation.IO : MemoryDesignation.Character;
				layout.MemE000 = MemoryDesignation.Kernal;
			}
			else if (!loRom && !hiRom && gamePin)
			{
				layout.Mem1000 = MemoryDesignation.RAM;
				layout.Mem8000 = MemoryDesignation.RAM;
				layout.MemA000 = MemoryDesignation.RAM;
				layout.MemC000 = MemoryDesignation.RAM;
				layout.MemD000 = MemoryDesignation.RAM;
				layout.MemE000 = MemoryDesignation.RAM;
			}
			else if (loRom && hiRom && gamePin && !exRomPin)
			{
				layout.Mem1000 = MemoryDesignation.RAM;
				layout.Mem8000 = MemoryDesignation.ROMLo;
				layout.MemA000 = MemoryDesignation.Basic;
				layout.MemC000 = MemoryDesignation.RAM;
				layout.MemD000 = ioEnable ? MemoryDesignation.IO : MemoryDesignation.Character;
				layout.MemE000 = MemoryDesignation.Kernal;
			}
			else if (!loRom && hiRom && !gamePin && !exRomPin)
			{
				layout.Mem1000 = MemoryDesignation.RAM;
				layout.Mem8000 = MemoryDesignation.RAM;
				layout.MemA000 = MemoryDesignation.ROMHi;
				layout.MemC000 = MemoryDesignation.RAM;
				layout.MemD000 = ioEnable ? MemoryDesignation.IO : MemoryDesignation.Character;
				layout.MemE000 = MemoryDesignation.Kernal;
			}
			else if (loRom && hiRom && !gamePin && !exRomPin)
			{
				layout.Mem1000 = MemoryDesignation.RAM;
				layout.Mem8000 = MemoryDesignation.ROMLo;
				layout.MemA000 = MemoryDesignation.ROMHi;
				layout.MemC000 = MemoryDesignation.RAM;
				layout.MemD000 = ioEnable ? MemoryDesignation.IO : MemoryDesignation.Character;
				layout.MemE000 = MemoryDesignation.Kernal;
			}
			else if (!gamePin && exRomPin)
			{
				layout.Mem1000 = MemoryDesignation.Disabled;
				layout.Mem8000 = MemoryDesignation.ROMLo;
				layout.MemA000 = MemoryDesignation.Disabled;
				layout.MemC000 = MemoryDesignation.Disabled;
				layout.MemD000 = MemoryDesignation.IO;
				layout.MemE000 = MemoryDesignation.ROMHi;
			}
		}

		private void UpdateVicOffset()
		{
			switch (cia1.Peek(0x00) & 0x03)
			{
				case 0:
					vicCharEnabled = false;
					vicOffset = 0xC000;
					break;
				case 1:
					vicCharEnabled = true;
					vicOffset = 0x8000;
					break;
				case 2:
					vicCharEnabled = false;
					vicOffset = 0x4000;
					break;
				default:
					vicCharEnabled = true;
					vicOffset = 0x0000;
					break;
			}
		}

		public byte VicRead(ushort addr)
		{
			addr = (ushort)(addr & 0x3FFF);
			if (vicCharEnabled && (addr >= 0x1000 && addr < 0x2000))
			{
				return charRom[addr & 0x0FFF];
			}
			else
			{
				return ram[addr | vicOffset];
			}
		}

		public void WipeMemory()
		{
			// memory is striped in sections 00/FF
			for (int i = 0; i < 0x10000; i += 0x80)
			{
				for (int j = 0; j < 0x40; j++)
					ram[i + j] = 0x00;
				for (int j = 0x40; j < 0x80; j++)
					ram[i + j] = 0xFF;
			}
			for (int i = 0; i < 0x1000; i++)
			{
				colorRam[i] = 0x0E;
			}
		}

		public void Write(ushort addr, byte val)
		{
			if (addr == 0x0000)
			{
				cpuPort.Direction = val;
			}
			else if (addr == 0x0001)
			{
				cpuPort.Data = val;
			}
			else
			{
				MemoryDesignation des = GetDesignation(addr);

				switch (des)
				{
					case MemoryDesignation.Vic:
						vic.Write(addr, val);
						break;
					case MemoryDesignation.Sid:
						sid.Write(addr, val);
						break;
					case MemoryDesignation.ColorRam:
						colorRam[addr & 0x03FF] = (byte)(val & 0x0F);
						break;
					case MemoryDesignation.Cia0:
						cia0.Write(addr, val);
						break;
					case MemoryDesignation.Cia1:
						cia1.Write(addr, val);
						break;
					case MemoryDesignation.Expansion0:
						if (cart != null)
							cart.WritePort(addr, val);
						break;
					case MemoryDesignation.Expansion1:
						break;
					case MemoryDesignation.RAM:
						break;
					default:
						break;
				}

				// write through to ram
				if (des != MemoryDesignation.Disabled)
				{
					ram[addr] = val;
				}
			}
			busData = val;
		}
	}
}
