#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>

using namespace std;

namespace GBHawk
{
	class LR35902;
	class Timer;
	class PPU;
	class GBAudio;
	class SerialPort;
	class Mapper;
	
	class MemoryManager
	{
	public:
				
		MemoryManager()
		{

		};

		uint8_t ReadMemory(uint32_t addr);
		uint8_t PeekMemory(uint32_t addr);
		void WriteMemory(uint32_t addr, uint8_t value);
		
		#pragma region Declarations

		PPU* ppu_pntr = nullptr;
		GBAudio* psg_pntr = nullptr;
		LR35902* cpu_pntr = nullptr;
		Timer* timer_pntr = nullptr;
		SerialPort* serialport_pntr = nullptr;
		Mapper* mapper_pntr = nullptr;
		uint8_t* rom_1 = nullptr;
		uint8_t* rom_2 = nullptr;
		uint8_t* bios_rom = nullptr;

		// initialized by core loading, not savestated
		uint32_t rom_size_1;
		uint32_t rom_mapper_1;
		uint32_t rom_size_2;
		uint32_t rom_mapper_2;

		// controls are not stated
		uint8_t controller_byte_1, controller_byte_2;
		uint8_t* kb_rows;

		// State
		bool PortDEEnabled = false;
		bool lagged;
		bool start_pressed;
		bool is_GBC;
		bool GBC_compat;
		bool speed_switch, double_speed;
		bool in_vblank;
		bool GB_bios_register;
		bool HDMA_transfer;
		bool _islag;
		uint8_t addr_access;
		uint8_t REG_FFFF, REG_FF0F, REG_FF0F_OLD;
		uint8_t _scanlineCallbackLine;
		uint8_t input_register;
		uint32_t RAM_Bank;
		uint32_t VRAM_Bank;

		uint8_t IR_reg, IR_mask, IR_signal, IR_receive, IR_self;
		uint32_t IR_write;

		// several undocumented GBC Registers
		uint8_t undoc_6C, undoc_72, undoc_73, undoc_74, undoc_75, undoc_76, undoc_77;
		uint8_t controller_state;

		uint8_t ZP_RAM[0x80] = {};
		uint8_t RAM[0x8000] = {};
		uint8_t VRAM[0x10000] = {};
		uint8_t OAM[0x10000] = {};
		uint8_t cart_ram[0x8000] = {};
		uint8_t unmapped[0x400] = {};
		uint32_t _vidbuffer[160 * 144] = {};
		uint32_t color_palette[4] = { 0xFFFFFFFF , 0xFFAAAAAA, 0xFF555555, 0xFF000000 };

		uint32_t FrameBuffer[160 * 144] = {};

		#pragma endregion

		#pragma region Functions

		// NOTE: only called when checks pass that the files are correct
		void Load_BIOS(uint8_t* bios, bool GBC_console)
		{
			if (GBC_console)
			{
				bios_rom = new uint8_t[2304];
				memcpy(bios_rom, bios, 2304);
			}
			else
			{
				bios_rom = new uint8_t[256];
				memcpy(bios_rom, bios, 256);
			}
		}

		void Load_ROM(uint8_t* ext_rom_1, uint32_t ext_rom_size_1, uint32_t ext_rom_mapper_1, uint8_t* ext_rom_2, uint32_t ext_rom_size_2, uint32_t ext_rom_mapper_2)
		{
			rom_1 = new uint8_t[ext_rom_size_1];
			rom_2 = new uint8_t[ext_rom_size_2];

			memcpy(rom_1, ext_rom_1, ext_rom_size_1);
			memcpy(rom_2, ext_rom_2, ext_rom_size_2);

			rom_size_1 = ext_rom_size_1 / 0x4000;
			rom_mapper_1 = ext_rom_mapper_1;

			rom_size_2 = ext_rom_size_2 / 0x4000;
			rom_mapper_2 = ext_rom_mapper_2;
		}

		// Switch Speed (GBC only)
		uint32_t SpeedFunc(uint32_t temp)
		{
			if (is_GBC)
			{
				if (speed_switch)
				{
					speed_switch = false;
					uint32_t ret = double_speed ? 70224 * 2 : 70224 * 2; // actual time needs checking
					double_speed = !double_speed;
					return ret;
				}

				// if we are not switching speed, return 0
				return 0;
			}

			// if we are in GB mode, return 0 indicating not switching speed
			return 0;
		}

		uint8_t Read_Registers(uint32_t addr)
		{
			uint8_t ret = 0;

			switch (addr)
			{
				// Read Input
			case 0xFF00:
				_islag = false;
				ret = input_register;
				break;

				// Serial data port
			case 0xFF01:
				ret = serialport_pntr->ReadReg(addr);
				break;

				// Serial port control
			case 0xFF02:
				ret = serialport_pntr->ReadReg(addr);
				break;

				// Timer Registers
			case 0xFF04:
			case 0xFF05:
			case 0xFF06:
			case 0xFF07:
				ret = timer_pntr->ReadReg(addr);
				break;

				// Interrupt flags
			case 0xFF0F:
				ret = REG_FF0F_OLD;
				break;

				// audio regs
			case 0xFF10:
			case 0xFF11:
			case 0xFF12:
			case 0xFF13:
			case 0xFF14:
			case 0xFF16:
			case 0xFF17:
			case 0xFF18:
			case 0xFF19:
			case 0xFF1A:
			case 0xFF1B:
			case 0xFF1C:
			case 0xFF1D:
			case 0xFF1E:
			case 0xFF20:
			case 0xFF21:
			case 0xFF22:
			case 0xFF23:
			case 0xFF24:
			case 0xFF25:
			case 0xFF26:
			case 0xFF30:
			case 0xFF31:
			case 0xFF32:
			case 0xFF33:
			case 0xFF34:
			case 0xFF35:
			case 0xFF36:
			case 0xFF37:
			case 0xFF38:
			case 0xFF39:
			case 0xFF3A:
			case 0xFF3B:
			case 0xFF3C:
			case 0xFF3D:
			case 0xFF3E:
			case 0xFF3F:
				ret = psg_pntr->ReadReg(addr);
				break;

				// PPU Regs
			case 0xFF40:
			case 0xFF41:
			case 0xFF42:
			case 0xFF43:
			case 0xFF44:
			case 0xFF45:
			case 0xFF46:
			case 0xFF47:
			case 0xFF48:
			case 0xFF49:
			case 0xFF4A:
			case 0xFF4B:
				ret = ppu_pntr->ReadReg(addr);
				break;

				// Speed Control for GBC
			case 0xFF4D:
				if (GBC_compat)
				{
					ret = (uint8_t)(((double_speed ? 1 : 0) << 7) + ((speed_switch ? 1 : 0)));
				}
				else
				{
					ret = 0xFF;
				}
				break;

			case 0xFF4F: // VBK
				if (GBC_compat)
				{
					ret = (uint8_t)(0xFE | VRAM_Bank);
				}
				else
				{
					ret = 0xFF;
				}
				break;

				// Bios control register. Not sure if it is readable
			case 0xFF50:
				ret = 0xFF;
				break;

				// PPU Regs for GBC
			case 0xFF51:
			case 0xFF52:
			case 0xFF53:
			case 0xFF54:
			case 0xFF55:
				if (GBC_compat)
				{
					ret = ppu_pntr->ReadReg(addr);
				}
				else
				{
					ret = 0xFF;
				}
				break;

			case 0xFF56:
				if (GBC_compat)
				{
					// can receive data
					if ((IR_reg & 0xC0) == 0xC0)
					{
						ret = IR_reg;
					}
					else
					{
						ret = (uint8_t)(IR_reg | 2);
					}
				}
				else
				{
					ret = 0xFF;
				}
				break;

			case 0xFF68:
			case 0xFF69:
			case 0xFF6A:
			case 0xFF6B:
				if (GBC_compat)
				{
					ret = ppu_pntr->ReadReg(addr);
				}
				else
				{
					ret = 0xFF;
				}
				break;

				// Speed Control for GBC
			case 0xFF70:
				if (GBC_compat)
				{
					ret = (uint8_t)RAM_Bank;
				}
				else
				{
					ret = 0xFF;
				}
				break;

			case 0xFF6C:
				if (GBC_compat) { ret = undoc_6C; }
				else { ret = 0xFF; }
				break;

			case 0xFF72:
				if (is_GBC) { ret = undoc_72; }
				else { ret = 0xFF; }
				break;

			case 0xFF73:
				if (is_GBC) { ret = undoc_73; }
				else { ret = 0xFF; }
				break;

			case 0xFF74:
				if (GBC_compat) { ret = undoc_74; }
				else { ret = 0xFF; }
				break;

			case 0xFF75:
				if (is_GBC) { ret = undoc_75; }
				else { ret = 0xFF; }
				break;

			case 0xFF76:
				if (is_GBC) { ret = undoc_76; }
				else { ret = 0xFF; }
				break;

			case 0xFF77:
				if (is_GBC) { ret = undoc_77; }
				else { ret = 0xFF; }
				break;

				// interrupt control register
			case 0xFFFF:
				ret = REG_FFFF;
				break;

			default:
				ret = 0xFF;
				break;

			}
			return ret;
		}

		void Write_Registers(int addr, uint8_t value)
		{
			switch (addr)
			{
				// select input
			case 0xFF00:
				input_register &= 0xCF;
				input_register |= (uint8_t)(value & 0x30); // top 2 bits always 1

				// check for high to low transitions that trigger IRQs
				uint8_t contr_prev = input_register;

				input_register &= 0xF0;
				if ((input_register & 0x30) == 0x20)
				{
					input_register |= (uint8_t)(controller_state & 0xF);
				}
				else if ((input_register & 0x30) == 0x10)
				{
					input_register |= (uint8_t)((controller_state & 0xF0) >> 4);
				}
				else if ((input_register & 0x30) == 0x00)
				{
					// if both polls are set, then a bit is zero if either or both pins are zero
					uint8_t temp = (uint8_t)((controller_state & 0xF) & ((controller_state & 0xF0) >> 4));
					input_register |= temp;
				}
				else
				{
					input_register |= 0xF;
				}

				// check for interrupts
				if (((contr_prev & 8) > 0) && ((input_register & 8) == 0) ||
					((contr_prev & 4) > 0) && ((input_register & 4) == 0) ||
					((contr_prev & 2) > 0) && ((input_register & 2) == 0) ||
					((contr_prev & 1) > 0) && ((input_register & 1) == 0))
				{
					if (((REG_FFFF & 0x10) > 0)) { cpu_pntr->FlagI = true; }
					REG_FF0F |= 0x10;
				}

				break;

				// Serial data port
			case 0xFF01:
				serialport_pntr->WriteReg(addr, value);
				break;

				// Serial port control
			case 0xFF02:
				serialport_pntr->WriteReg(addr, value);
				break;

				// Timer Registers
			case 0xFF04:
			case 0xFF05:
			case 0xFF06:
			case 0xFF07:
				timer_pntr->WriteReg(addr, value);
				break;

				// Interrupt flags
			case 0xFF0F:
				REG_FF0F = (uint8_t)(0xE0 | value);

				// check if enabling any of the bits triggered an IRQ
				for (int i = 0; i < 5; i++)
				{
					if (((REG_FFFF & (1 <<i)) > 0) && ((REG_FF0F & (1 << i)) > 0))
					{
						cpu_pntr->FlagI = true;
					}
				}

				// if no bits are in common between flags and enables, de-assert the IRQ
				if (((REG_FF0F & 0x1F) & REG_FFFF) == 0) { cpu_pntr->FlagI = false; }
				break;

				// audio regs
			case 0xFF10:
			case 0xFF11:
			case 0xFF12:
			case 0xFF13:
			case 0xFF14:
			case 0xFF16:
			case 0xFF17:
			case 0xFF18:
			case 0xFF19:
			case 0xFF1A:
			case 0xFF1B:
			case 0xFF1C:
			case 0xFF1D:
			case 0xFF1E:
			case 0xFF20:
			case 0xFF21:
			case 0xFF22:
			case 0xFF23:
			case 0xFF24:
			case 0xFF25:
			case 0xFF26:
			case 0xFF30:
			case 0xFF31:
			case 0xFF32:
			case 0xFF33:
			case 0xFF34:
			case 0xFF35:
			case 0xFF36:
			case 0xFF37:
			case 0xFF38:
			case 0xFF39:
			case 0xFF3A:
			case 0xFF3B:
			case 0xFF3C:
			case 0xFF3D:
			case 0xFF3E:
			case 0xFF3F:
				psg_pntr->WriteReg(addr, value);
				break;

				// PPU Regs
			case 0xFF40:
			case 0xFF41:
			case 0xFF42:
			case 0xFF43:
			case 0xFF44:
			case 0xFF45:
			case 0xFF46:
			case 0xFF47:
			case 0xFF48:
			case 0xFF49:
			case 0xFF4A:
			case 0xFF4B:
				ppu_pntr->WriteReg(addr, value);
				break;

				// GBC compatibility register (I think)
			case 0xFF4C:
				if ((value != 0xC0) && (value != 0x80))// && (value != 0xFF) && (value != 0x04))
				{
					GBC_compat = false;

					// cpu operation is a function of hardware only
					//cpu.is_GBC = GBC_compat;
				}
				break;

				// Speed Control for GBC
			case 0xFF4D:
				if (GBC_compat)
				{
					speed_switch = (value & 1) > 0;
				}
				break;

				// VBK
			case 0xFF4F:
				if (GBC_compat && !ppu_pntr->HDMA_active)
				{
					VRAM_Bank = (uint8_t)(value & 1);
				}
				break;

				// Bios control register. Writing 1 permanently disables BIOS until a power cycle occurs
			case 0xFF50:
				// Console.WriteLine(value);
				if (GB_bios_register == 0)
				{
					GB_bios_register = value;
				}
				break;

				// PPU Regs for GBC
			case 0xFF51:
			case 0xFF52:
			case 0xFF53:
			case 0xFF54:
			case 0xFF55:
				if (GBC_compat)
				{
					ppu_pntr->WriteReg(addr, value);
				}
				break;

			case 0xFF56:
				if (is_GBC)
				{
					IR_reg = (uint8_t)((value & 0xC1) | (IR_reg & 0x3E));

					// send IR signal out
					if ((IR_reg & 0x1) == 0x1) { IR_signal = (uint8_t)(0 | IR_mask); }
					else { IR_signal = 2; }

					// receive own signal if IR on and receive on
					if ((IR_reg & 0xC1) == 0xC1) { IR_self = (uint8_t)(0 | IR_mask); }
					else { IR_self = 2; }

					IR_write = 8;
				}
				break;

			case 0xFF68:
			case 0xFF69:
			case 0xFF6A:
			case 0xFF6B:
				//if (GBC_compat)
				//{
				ppu_pntr->WriteReg(addr, value);
				//}
				break;

				// RAM Bank in GBC mode
			case 0xFF70:
				//Console.WriteLine(value);
				if (GBC_compat)
				{
					RAM_Bank = value & 7;
					if (RAM_Bank == 0) { RAM_Bank = 1; }
				}
				break;

			case 0xFF6C:
				if (GBC_compat) { undoc_6C |= (uint8_t)(value & 1); }
				break;

			case 0xFF72:
				if (is_GBC) { undoc_72 = value; }
				break;

			case 0xFF73:
				if (is_GBC) { undoc_73 = value; }
				break;

			case 0xFF74:
				if (GBC_compat) { undoc_74 = value; }
				break;

			case 0xFF75:
				if (is_GBC) { undoc_75 |= (uint8_t)(value & 0x70); }
				break;

			case 0xFF76:
				// read only
				break;

			case 0xFF77:
				// read only
				break;

				// interrupt control register
			case 0xFFFF:
				REG_FFFF = value;

				// check if enabling any of the bits triggered an IRQ
				for (int i = 0; i < 5; i++)
				{
					if (((REG_FFFF & (1 << i)) > 0) && ((REG_FF0F & (1 << i)) > 0))
					{
						cpu_pntr->FlagI = true;
					}
				}

				// if no bits are in common between flags and enables, de-assert the IRQ
				if (((REG_FF0F & 0x1F) & REG_FFFF) == 0) { cpu_pntr->FlagI = false; }
				break;

			default:
				//Console.Write(addr);
				//Console.Write(" ");
				//Console.WriteLine(value);
				break;
			}
		}

		void Register_Reset()
		{
			input_register = 0xCF; // not reading any input

			REG_FFFF = 0;
			REG_FF0F = 0xE0;
			REG_FF0F_OLD = 0xE0;

			//undocumented registers
			undoc_6C = 0xFE;
			undoc_72 = 0;
			undoc_73 = 0;
			undoc_74 = 0;
			undoc_75 = 0x8F;
			undoc_76 = 0;
			undoc_77 = 0;
		}

		#pragma endregion

		#pragma region State Save / Load

		uint8_t* SaveState(uint8_t* saver)
		{
			*saver = (uint8_t)(PortDEEnabled ? 1 : 0); saver++;
			*saver = (uint8_t)(lagged ? 1 : 0); saver++;
			*saver = (uint8_t)(start_pressed ? 1 : 0); saver++;

			std::memcpy(saver, &RAM, 0x10000); saver += 0x10000;
			std::memcpy(saver, &cart_ram, 0x8000); saver += 0x8000;

			return saver;
		}

		uint8_t* LoadState(uint8_t* loader)
		{
			PortDEEnabled = *loader == 1; loader++;
			lagged = *loader == 1; loader++;
			start_pressed = *loader == 1; loader++;

			std::memcpy(&RAM, loader, 0x10000); loader += 0x10000;
			std::memcpy(&cart_ram, loader, 0x8000); loader += 0x8000;

			return loader;
		}

		#pragma endregion
	};
}