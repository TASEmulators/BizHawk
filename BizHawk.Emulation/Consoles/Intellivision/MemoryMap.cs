using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Intellivision
{
	public sealed partial class Intellivision
	{
		private const string INVALID = "Invalid memory address.";
		private const string UNOCCUPIED = "Unoccupied memory address.";
		private const string UNOCCUPIED_CART_READ = "Unoccupied memory address available to cartridge not mapped as"
			+ " readable.";
		private const string UNOCCUPIED_CART_WRITE = "Unoccupied memory address available to cartridge not mapped as"
			+ " writable.";
		private const string READ_ONLY_STIC = "This STIC Register alias is read-only.";
		private const string READ_ONLY_EXEC = "Executive ROM is read-only.";
		private const string READ_ONLY_GROM = "Graphics ROM is read-only.";
		private const string WRITE_ONLY_STIC = "This STIC Register alias is write-only.";
		private const string GARBAGE = "Memory address contains a garbage value, perhaps only in the Intellivision II.";
		private const string INTV2_EXEC = "Additional EXEC ROM, Intellivision II only.";

		private ushort[] STIC_Registers = new ushort[64];
		private ushort[] Scratchpad_RAM = new ushort[240];
		private ushort[] PSG_Registers = new ushort[16];
		private ushort[] System_RAM = new ushort[352];
		private ushort[] Executive_ROM = new ushort[4096]; // TODO: Intellivision II support?
		private ushort[] Graphics_ROM = new ushort[2048];
		private ushort[] Graphics_RAM = new ushort[512];
		private ushort[] Cartridge = new ushort[56320]; // TODO: Resize as cartridge mapping grows.

		public ushort ReadMemory(ushort addr)
		{
			ushort? cart = ReadCart(addr);
			ushort core;
			switch (addr & 0xF000)
			{
				case 0x0000:
					if (addr <= 0x003F)
						// TODO: OK only during VBlank Period 1.
						core = STIC_Registers[addr];
					else if (addr <= 0x007F)
						// TODO: OK only during VBlank Period 2.
						core = STIC_Registers[addr & 0x003F];
					else if (addr <= 0x00FF)
						throw new ArgumentException(UNOCCUPIED);
					else if (addr <= 0x01EF)
						core = Scratchpad_RAM[addr & 0x00EF];
					else if (addr <= 0x01FF)
						core = PSG_Registers[addr & 0x000F];
					else if (addr <= 0x035F)
						core = System_RAM[addr & 0x015F];
					else if (addr <= 0x03FF)
						// TODO: Intellivision II support?
						throw new ArgumentException(GARBAGE);
					else if (addr <= 0x04FF)
						// TODO: Actually map cartridge (on all but Intellivision II) RAM / ROM to decide which path to take.
						if (false)
							// TODO: Intellivision II support?
							throw new ArgumentException(INTV2_EXEC);
						else
							core = Cartridge[addr & 0x00FF];
					else if (addr <= 0x06FF)
						// TODO: Actually map cartridge RAM / ROM to decide which path to take.
						if (false)
							throw new ArgumentException(UNOCCUPIED_CART_READ);
						else
							core = Cartridge[addr & 0x02FF];
					else if (addr <= 0x0CFF)
						// TODO: Actually map cartridge (only if no Intellivoice) RAM / ROM to decide which path to take.
						if (false)
							throw new ArgumentException(UNOCCUPIED_CART_READ);
						else
							core = Cartridge[addr & 0x08FF];
					else
						// TODO: Actually map cartridge RAM / ROM to decide which path to take.
						if (false)
							throw new ArgumentException(UNOCCUPIED_CART_READ);
						else
							core = Cartridge[addr & 0x0BFF];
					break;
				case 0x1000:
					core = Executive_ROM[addr & 0x0FFF];
					break;
				case 0x2000:
					// TODO: Actually map cartridge (only if no ECS) RAM / ROM to decide which path to take.
					if (false)
						throw new ArgumentException(UNOCCUPIED_CART_READ);
					else
						core = Cartridge[(addr & 0x0FFF) + 0x0C00];
					break;
				case 0x3000:
					if (addr <= 0x37FF)
						// TODO: OK only during VBlank Period 2.
						core = Graphics_ROM[addr & 0x07FF];
					else if (addr <= 0x39FF)
						// TODO: OK only during VBlank Period 2.
						core = Graphics_RAM[addr & 0x01FF];
					else if (addr <= 0x3BFF)
						// TODO: OK only during VBlank Period 2.
						core = Graphics_RAM[addr & 0x01FF];
					else if (addr <= 0x3DFF)
						// TODO: OK only during VBlank Period 2.
						core = Graphics_RAM[addr & 0x01FF];
					else
						// TODO: OK only during VBlank Period 2.
						core = Graphics_RAM[addr & 0x01FF];
					break;
				case 0x4000:
					if (addr <= 0x403F)
					{
						// TODO: Actually map cartridge (only if no ECS) RAM / ROM to decide which path to take.
						if (true)
						{
							// TODO: OK only during VBlank Period 1.
							if (addr == 0x4021)
								// TODO: Switch into Color Stack mode.
								core = STIC_Registers[0x0021];
							else
								throw new ArgumentException(WRITE_ONLY_STIC);
						}
						else
							core = Cartridge[(addr & 0x003F) + 0x1C00];
					}
					else if (addr <= 0x47FF)
					{
						// TODO: Actually map cartridge (only if no ECS) RAM / ROM to decide which path to take.
						if (false)
							throw new ArgumentException(UNOCCUPIED_CART_READ);
						else
							core = Cartridge[(addr & 0x07FF) + 0x1C00];
					}
					else if (addr == 0x4800)
					{
						// TODO: Actually map cartridge RAM / ROM to decide which path to take.
						if (false)
							throw new ArgumentException(UNOCCUPIED_CART_READ);
						else
							core = Cartridge[0x2400];
					}
					else
					{
						// TODO: Actually map cartridge RAM / ROM to decide which path to take.
						if (false)
							throw new ArgumentException(UNOCCUPIED_CART_READ);
						else
							core = Cartridge[(addr & 0x0FFF) + 0x1C00];
					}
					break;
				case 0x5000:
				case 0x6000:
					if (addr <= 0x5014)
					{
						// TODO: Actually map cartridge RAM / ROM to decide which path to take.
						if (false)
							throw new ArgumentException(UNOCCUPIED_CART_READ);
						else
							core = Cartridge[(addr & 0x0014) + 0x2C00];
					}
					else
					{
						// TODO: Actually map cartridge RAM / ROM to decide which path to take.
						if (false)
							throw new ArgumentException(UNOCCUPIED_CART_READ);
						else
							core = Cartridge[(addr & 0x1FFF) + 0x2C00];
					}
					break;
				case 0x7000:
					if (addr == 0x7000)
					{
						/*
						 TODO: Actually map cartridge (only if no ECS) RAM (confuses EXEC boot sequence) / ROM to decide
						 which path to take.
						*/
						if (false)
							throw new ArgumentException(UNOCCUPIED_CART_READ);
						else
							core = Cartridge[0x04C00];
					}
					else if (addr <= 0x77FF)
					{
						// TODO: Actually map cartridge (only if no ECS) RAM / ROM to decide which path to take.
						if (false)
							throw new ArgumentException(UNOCCUPIED_CART_READ);
						else
							core = Cartridge[(addr & 0x07FF) + 0x4C00];
					}
					else if (addr <= 0x79FF)
					{
						/*
						 TODO: Actually map cartridge (only if no ECS) RAM (Do not because of GRAM alias) / ROM to decide
						 which path to take.
						*/
						if (false)
							throw new ArgumentException(UNOCCUPIED_CART_READ);
						else
							core = Cartridge[(addr & 0x09FF) + 0x4C00];
					}
					else if (addr <= 0x7BFF)
					{
						/*
						 TODO: Actually map cartridge (only if no ECS) RAM (Do not because of GRAM alias) / ROM to decide
						 which path to take.
						*/
						if (false)
							throw new ArgumentException(UNOCCUPIED_CART_READ);
						else
							core = Cartridge[(addr & 0x0BFF) + 0x4C00];
					}
					else if (addr <= 0x7DFF)
					{
						/*
						 TODO: Actually map cartridge (only if no ECS) RAM (Do not because of GRAM alias) / ROM to decide
						 which path to take.
						*/
						if (false)
							throw new ArgumentException(UNOCCUPIED_CART_READ);
						else
							core = Cartridge[(addr & 0x0DFF) + 0x4C00];
					}
					else
					{
						/*
						 TODO: Actually map cartridge (only if no ECS) RAM (Do not because of GRAM alias) / ROM to decide
						 which path to take.
						*/
						if (false)
							throw new ArgumentException(UNOCCUPIED_CART_READ);
						else
							core = Cartridge[(addr & 0x0FFF) + 0x4C00];
					}
					break;
				case 0x8000:
					if (addr <= 0x803F)
					{
						// TODO: Actually map cartridge RAM / ROM to decide which path to take.
						if (true)
						{
							// TODO: OK only during VBlank Period 1.
							if (addr == 0x8021)
								// TODO: Switch into Color Stack mode.
								core = STIC_Registers[0x0021];
							else
								throw new ArgumentException(WRITE_ONLY_STIC);
						}
						else
							core = Cartridge[(addr & 0x003F) + 0x5C00];
					}
					else
					{
						// TODO: Actually map cartridge RAM / ROM to decide which path to take.
						if (false)
							throw new ArgumentException(UNOCCUPIED_CART_READ);
						else
							core = Cartridge[(addr & 0x0FFF) + 0x5C00];
					}
					break;
				case 0x9000:
				case 0xA000:
				case 0xB000:
					if (addr <= 0xB7FF)
					{
						// TODO: Actually map cartridge RAM / ROM to decide which path to take.
						if (false)
							throw new ArgumentException(UNOCCUPIED_CART_READ);
						else
							core = Cartridge[(addr & 0x27FF) + 0x6C00];
					}
					else if (addr <= 0xB9FF)
					{
						/*
						 TODO: Actually map cartridge RAM (Do not because of GRAM alias) / ROM to decide which path to take.
						*/
						if (false)
							throw new ArgumentException(UNOCCUPIED_CART_READ);
						else
							core = Cartridge[(addr & 0x29FF) + 0x6C00];
					}
					else if (addr <= 0xBBFF)
					{
						/*
						 TODO: Actually map cartridge RAM (Do not because of GRAM alias) / ROM to decide which path to take.
						*/
						if (false)
							throw new ArgumentException(UNOCCUPIED_CART_READ);
						else
							core = Cartridge[(addr & 0x2BFF) + 0x6C00];
					}
					else if (addr <= 0xBDFF)
					{
						/*
						 TODO: Actually map cartridge RAM (Do not because of GRAM alias) / ROM to decide which path to take.
						*/
						if (false)
							throw new ArgumentException(UNOCCUPIED_CART_READ);
						else
							core = Cartridge[(addr & 0x2DFF) + 0x6C00];
					}
					else
					{
						/*
						 TODO: Actually map cartridge RAM (Do not because of GRAM alias) / ROM to decide which path to take.
						*/
						if (false)
							throw new ArgumentException(UNOCCUPIED_CART_READ);
						else
							core = Cartridge[(addr & 0x2FFF) + 0x6C00];
					}
					break;
				case 0xC000:
					if (addr <= 0xC03F)
					{
						// TODO: Actually map cartridge RAM / ROM to decide which path to take.
						if (true)
						{
							// TODO: OK only during VBlank Period 1.
							if (addr == 0xC021)
								// TODO: Switch into Color Stack mode.
								core = STIC_Registers[0x0021];
							else
								throw new ArgumentException(WRITE_ONLY_STIC);
						}
						else
							core = Cartridge[(addr & 0x003F) + 0x9C00];
					}
					else
					{
						// TODO: Actually map cartridge RAM / ROM to decide which path to take.
						if (false)
							throw new ArgumentException(UNOCCUPIED_CART_READ);
						else
							core = Cartridge[(addr & 0x0FFF) + 0x9C00];
					}
					break;
				case 0xD000:
					// TODO: Actually map cartridge RAM / ROM to decide which path to take.
					if (false)
						throw new ArgumentException(UNOCCUPIED_CART_READ);
					else
						core = Cartridge[(addr & 0x0FFF) + 0xAC00];
					break;
				case 0xE000:
					// TODO: Actually map cartridge (only if no ECS) RAM / ROM to decide which path to take.
					if (false)
						throw new ArgumentException(UNOCCUPIED_CART_READ);
					else
						core = Cartridge[(addr & 0x0FFF) + 0xBC00];
					break;
				case 0xF000:
					if (addr <= 0xF7FF)
					{
						// TODO: Actually map cartridge RAM / ROM to decide which path to take.
						if (false)
							throw new ArgumentException(UNOCCUPIED_CART_READ);
						else
							core = Cartridge[(addr & 0x07FF) + 0xCC00];
					}
					else if (addr <= 0xF9FF)
					{
						/*
						 TODO: Actually map cartridge RAM (Do not because of GRAM alias) / ROM to decide which path to take.
						*/
						if (false)
							throw new ArgumentException(UNOCCUPIED_CART_READ);
						else
							core = Cartridge[(addr & 0x09FF) + 0xCC00];
					}
					else if (addr <= 0xFBFF)
					{
						/*
						 TODO: Actually map cartridge RAM (Do not because of GRAM alias) / ROM to decide which path to take.
						*/
						if (false)
							throw new ArgumentException(UNOCCUPIED_CART_READ);
						else
							core = Cartridge[(addr & 0x0BFF) + 0xCC00];
					}
					else if (addr <= 0xFDFF)
					{
						/*
						 TODO: Actually map cartridge RAM (Do not because of GRAM alias) / ROM to decide which path to take.
						*/
						if (false)
							throw new ArgumentException(UNOCCUPIED_CART_READ);
						else
							core = Cartridge[(addr & 0x0DFF) + 0xCC00];
					}
					else
					{
						/*
						 TODO: Actually map cartridge RAM (Do not because of GRAM alias) / ROM to decide which path to take.
						*/
						if (false)
							throw new ArgumentException(UNOCCUPIED_CART_READ);
						else
							core = Cartridge[(addr & 0x0FFF) + 0xCC00];
					}
					break;
				default:
					throw new ArgumentException(INVALID);
			}
			/*
			TODO: Fix Intellicart hook.
			if (cart != null)
				return (ushort)cart;
			*/
			return core;
		}

		public void WriteMemory(ushort addr, ushort value)
		{
			WriteCart(addr, value);
			switch (addr & 0xF000)
			{
				case 0x0000:
					if (addr <= 0x003F)
						STIC_Registers[addr] = value;
					else if (addr <= 0x007F)
						throw new ArgumentException(READ_ONLY_STIC);
					else if (addr <= 0x00FF)
						throw new ArgumentException(UNOCCUPIED);
					else if (addr <= 0x01EF)
						Scratchpad_RAM[addr & 0x00EF] = value;
					else if (addr <= 0x01FF)
						PSG_Registers[addr & 0x000F] = value;
					else if (addr <= 0x035F)
						System_RAM[addr & 0x015F] = value;
					else if (addr <= 0x03FF)
						// TODO: Intellivision II support?
						throw new ArgumentException(GARBAGE);
					else if (addr <= 0x04FF)
						// TODO: Actually map cartridge (on all but Intellivision II) RAM / ROM to decide which path to take.
						if (false)
							// TODO: Intellivision II support?
							throw new ArgumentException(INTV2_EXEC);
						else
							Cartridge[addr & 0x00FF] = value;
					else if (addr <= 0x06FF)
						// TODO: Actually map cartridge RAM / ROM to decide which path to take.
						if (false)
							throw new ArgumentException(UNOCCUPIED_CART_WRITE);
						else
							Cartridge[addr & 0x02FF] = value;
					else if (addr <= 0x0CFF)
						// TODO: Actually map cartridge (only if no Intellivoice) RAM / ROM to decide which path to take.
						if (false)
							throw new ArgumentException(UNOCCUPIED_CART_WRITE);
						else
							Cartridge[addr & 0x08FF] = value;
					else
						// TODO: Actually map cartridge RAM / ROM to decide which path to take.
						if (false)
							throw new ArgumentException(UNOCCUPIED_CART_WRITE);
						else
							Cartridge[addr & 0x0BFF] = value;
					break;
				case 0x1000:
					throw new ArgumentException(READ_ONLY_EXEC);
				case 0x2000:
					// TODO: Actually map cartridge (only if no ECS) RAM / ROM to decide which path to take.
					if (false)
						throw new ArgumentException(UNOCCUPIED_CART_WRITE);
					else
						Cartridge[(addr & 0x0FFF) + 0x0C00] = value;
					break;
				case 0x3000:
					if (addr <= 0x37FF)
						throw new ArgumentException(READ_ONLY_GROM);
					else if (addr <= 0x39FF)
						// TODO: OK only during VBlank Period 2.
						Graphics_RAM[addr & 0x01FF] = value;
					else if (addr <= 0x3BFF)
						// TODO: OK only during VBlank Period 2.
						Graphics_RAM[addr & 0x01FF] = value;
					else if (addr <= 0x3DFF)
						// TODO: OK only during VBlank Period 2.
						Graphics_RAM[addr & 0x01FF] = value;
					else
						// TODO: OK only during VBlank Period 2.
						Graphics_RAM[addr & 0x01FF] = value;
					break;
				case 0x4000:
					if (addr <= 0x403F)
					{
						// TODO: Actually map cartridge (only if no ECS) RAM / ROM to decide which path to take.
						if (true)
							// TODO: OK only during VBlank Period 1.
							STIC_Registers[addr & 0x003F] = value;
						else
							Cartridge[(addr & 0x003F) + 0x1C00] = value;
					}
					else if (addr <= 0x47FF)
					{
						// TODO: Actually map cartridge (only if no ECS) RAM / ROM to decide which path to take.
						if (false)
							throw new ArgumentException(UNOCCUPIED_CART_WRITE);
						else
							Cartridge[(addr & 0x07FF) + 0x1C00] = value;
					}
					else if (addr == 0x4800)
					{
						// TODO: Actually map cartridge (only if boot ROM at $7000) RAM / ROM to decide which path to take.
						if (false)
							throw new ArgumentException(UNOCCUPIED_CART_WRITE);
						else
							Cartridge[0x2400] = value;
					}
					else
					{
						// TODO: Actually map cartridge RAM / ROM to decide which path to take.
						if (false)
							throw new ArgumentException(UNOCCUPIED_CART_WRITE);
						else
							Cartridge[(addr & 0x0FFF) + 0x1C00] = value;
					}
					break;
				case 0x5000:
				case 0x6000:
					if (addr <= 0x5014)
					{
						/*
						 TODO: Actually map (only if boot ROM at $4800 or $7000) cartridge RAM / ROM to decide which path to
						 take.
						*/
						if (false)
							throw new ArgumentException(UNOCCUPIED_CART_WRITE);
						else
							Cartridge[(addr & 0x0014) + 0x2C00] = value;
					}
					else
					{
						if (false)
							throw new ArgumentException(UNOCCUPIED_CART_WRITE);
						else
							Cartridge[(addr & 0x1FFF) + 0x2C00] = value;
					}
					break;
				case 0x7000:
					if (addr == 0x7000)
					{
						/*
						 TODO: Actually map cartridge (only if no ECS) RAM (confuses EXEC boot sequence) / ROM to decide
						 which path to take.
						*/
						if (false)
							throw new ArgumentException(UNOCCUPIED_CART_WRITE);
						else
							Cartridge[0x04C00] = value;
					}
					else if (addr <= 0x77FF)
					{
						// TODO: Actually map cartridge (only if no ECS) RAM / ROM to decide which path to take.
						if (false)
							throw new ArgumentException(UNOCCUPIED_CART_WRITE);
						else
							Cartridge[(addr & 0x07FF) + 0x4C00] = value;
					}
					else if (addr <= 0x79FF)
					{
						/*
						 TODO: Actually map cartridge (only if no ECS) RAM (Do not because of GRAM alias) / ROM to decide
						 which path to take.
						*/
						if (true)
							// TODO: OK only during VBlank Period 2.
							Graphics_RAM[addr & 0x01FF] = value;
						else
							Cartridge[(addr & 0x09FF) + 0x4C00] = value;
					}
					else if (addr <= 0x7BFF)
					{
						/*
						 TODO: Actually map cartridge (only if no ECS) RAM (Do not because of GRAM alias) / ROM to decide
						 which path to take.
						*/
						if (true)
							// TODO: OK only during VBlank Period 2.
							Graphics_RAM[addr & 0x01FF] = value;
						else
							Cartridge[(addr & 0x0BFF) + 0x4C00] = value;
					}
					else if (addr <= 0x7DFF)
					{
						/*
						 TODO: Actually map cartridge (only if no ECS) RAM (Do not because of GRAM alias) / ROM to decide
						 which path to take.
						*/
						if (true)
							// TODO: OK only during VBlank Period 2.
							Graphics_RAM[addr & 0x01FF] = value;
						else
							Cartridge[(addr & 0x0DFF) + 0x4C00] = value;
					}
					else
					{
						/*
						 TODO: Actually map cartridge (only if no ECS) RAM (Do not because of GRAM alias) / ROM to decide
						 which path to take.
						*/
						if (true)
							// TODO: OK only during VBlank Period 2.
							Graphics_RAM[addr & 0x01FF] = value;
						else
							Cartridge[(addr & 0x0FFF) + 0x4C00] = value;
					}
					break;
				case 0x8000:
					if (addr <= 0x803F)
					{
						// TODO: Actually map cartridge RAM / ROM to decide which path to take.
						if (true)
							// TODO: OK only during VBlank Period 1.
							STIC_Registers[addr & 0x003F] = value;
						else
							Cartridge[(addr & 0x003F) + 0x5C00] = value;
					}
					else
					{
						// TODO: Actually map cartridge RAM / ROM to decide which path to take.
						if (false)
							throw new ArgumentException(UNOCCUPIED_CART_WRITE);
						else
							Cartridge[(addr & 0x0FFF) + 0x5C00] = value;
					}
					break;
				case 0x9000:
				case 0xA000:
				case 0xB000:
					if (addr <= 0xB7FF)
					{
						// TODO: Actually map cartridge RAM / ROM to decide which path to take.
						if (false)
							throw new ArgumentException(UNOCCUPIED_CART_WRITE);
						else
							Cartridge[(addr & 0x27FF) + 0x6C00] = value;
					}
					else if (addr <= 0xB9FF)
					{
						/*
						 TODO: Actually map cartridge RAM (Do not because of GRAM alias) / ROM to decide which path to take.
						*/
						if (true)
							// TODO: OK only during VBlank Period 2.
							Graphics_RAM[addr & 0x01FF] = value;
						else
							Cartridge[(addr & 0x29FF) + 0x6C00] = value;
					}
					else if (addr <= 0xBBFF)
					{
						/*
						 TODO: Actually map cartridge RAM (Do not because of GRAM alias) / ROM to decide which path to take.
						*/
						if (true)
							// TODO: OK only during VBlank Period 2.
							Graphics_RAM[addr & 0x01FF] = value;
						else
							Cartridge[(addr & 0x2BFF) + 0x6C00] = value;
					}
					else if (addr <= 0xBDFF)
					{
						/*
						 TODO: Actually map cartridge RAM (Do not because of GRAM alias) / ROM to decide which path to take.
						*/
						if (true)
							// TODO: OK only during VBlank Period 2.
							Graphics_RAM[addr & 0x01FF] = value;
						else
							Cartridge[(addr & 0x2DFF) + 0x6C00] = value;
					}
					else
					{
						/*
						 TODO: Actually map cartridge RAM (Do not because of GRAM alias) / ROM to decide which path to take.
						*/
						if (true)
							// TODO: OK only during VBlank Period 2.
							Graphics_RAM[addr & 0x01FF] = value;
						else
							Cartridge[(addr & 0x2FFF) + 0x6C00] = value;
					}
					break;
				case 0xC000:
					if (addr <= 0x803F)
					{
						// TODO: Actually map cartridge RAM / ROM to decide which path to take.
						if (true)
							// TODO: OK only during VBlank Period 1.
							STIC_Registers[addr & 0x003F] = value;
						else
							Cartridge[(addr & 0x003F) + 0x9C00] = value;
					}
					else
					{
						// TODO: Actually map cartridge RAM / ROM to decide which path to take.
						if (false)
							throw new ArgumentException(UNOCCUPIED_CART_WRITE);
						else
							Cartridge[(addr & 0x0FFF) + 0x9C00] = value;
					}
					break;
				case 0xD000:
					// TODO: Actually map cartridge RAM / ROM to decide which path to take.
					if (false)
						throw new ArgumentException(UNOCCUPIED_CART_WRITE);
					else
						Cartridge[(addr & 0x0FFF) + 0xAC00] = value;
					break;
				case 0xE000:
					// TODO: Actually map cartridge (only if no ECS) RAM / ROM to decide which path to take.
					if (false)
						throw new ArgumentException(UNOCCUPIED_CART_WRITE);
					else
						Cartridge[(addr & 0x0FFF) + 0xBC00] = value;
					break;
				case 0xF000:
					if (addr <= 0xF7FF)
					{
						// TODO: Actually map cartridge RAM / ROM to decide which path to take.
						if (false)
							throw new ArgumentException(UNOCCUPIED_CART_WRITE);
						else
							Cartridge[(addr & 0x07FF) + 0xCC00] = value;
					}
					else if (addr <= 0xF9FF)
					{
						/*
						 TODO: Actually map cartridge RAM (Do not because of GRAM alias) / ROM to decide which path to take.
						*/
						if (true)
							// TODO: OK only during VBlank Period 2.
							Graphics_RAM[addr & 0x01FF] = value;
						else
							Cartridge[(addr & 0x09FF) + 0xCC00] = value;
					}
					else if (addr <= 0xFBFF)
					{
						/*
						 TODO: Actually map cartridge RAM (Do not because of GRAM alias) / ROM to decide which path to take.
						*/
						if (true)
							// TODO: OK only during VBlank Period 2.
							Graphics_RAM[addr & 0x01FF] = value;
						else
							Cartridge[(addr & 0x0BFF) + 0xCC00] = value;
					}
					else if (addr <= 0xFDFF)
					{
						/*
						 TODO: Actually map cartridge RAM (Do not because of GRAM alias) / ROM to decide which path to take.
						*/
						if (true)
							// TODO: OK only during VBlank Period 2.
							Graphics_RAM[addr & 0x01FF] = value;
						else
							Cartridge[(addr & 0x0DFF) + 0xCC00] = value;
					}
					else
					{
						/*
						 TODO: Actually map cartridge RAM (Do not because of GRAM alias) / ROM to decide which path to take.
						*/
						if (true)
							// TODO: OK only during VBlank Period 2.
							Graphics_RAM[addr & 0x01FF] = value;
						else
							Cartridge[(addr & 0x0FFF) + 0xCC00] = value;
					}
					break;
				default:
					throw new ArgumentException(INVALID);
			}
		}
	}
}
