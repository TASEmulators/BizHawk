// Meteor - A Nintendo Gameboy Advance emulator
// Copyright (C) 2009-2011 Philippe Daouadi
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#include "ameteor/cpu.hpp"
#include "ameteor/bios.hpp"
#include "globals.hpp"
#include "cpu_globals.hpp"
#include "ameteor.hpp"

#include "debug.hpp"

#include <cstring>

namespace AMeteor
{
	Cpu::Cpu ()
	{
		Reset();
	}

	void Cpu::Reset ()
	{
		std::memset(&m_st, 0, sizeof(m_st));
		R(15) = 0x00000004;
		m_st.icpsr.mode = M_SVC;
		m_st.icpsr.fiq_d = true;
		m_st.icpsr.irq_d = true;
	}

	void Cpu::SoftReset ()
	{
		std::memset(&m_st, 0, sizeof(m_st));
		R(13) = 0x03007F00;
		R(15) = 0x08000004;
		m_st.irq_r[0] = 0x03007FA0; // R13
		m_st.svc_r[0] = 0x03007FE0; // R13
		m_st.icpsr.mode = 0x1F;
		m_st.icpsr.fiq_d = true;
	}

	void Cpu::UpdateICpsr ()
	{
		m_st.icpsr.mode       = m_st.cpsr.b.mode;
		m_st.icpsr.irq_d      = m_st.cpsr.b.irq_d;
		m_st.icpsr.fiq_d      = m_st.cpsr.b.fiq_d;
		m_st.icpsr.thumb      = m_st.cpsr.b.thumb;
		m_st.icpsr.s_overflow = m_st.cpsr.b.s_overflow;
		m_st.icpsr.f_overflow = m_st.cpsr.b.f_overflow;
		m_st.icpsr.f_carry    = m_st.cpsr.b.f_carry;
		m_st.icpsr.f_zero     = m_st.cpsr.b.f_zero;
		m_st.icpsr.f_sign     = m_st.cpsr.b.f_sign;
	}

	void Cpu::UpdateCpsr ()
	{
		m_st.cpsr.b.mode       = m_st.icpsr.mode;
		m_st.cpsr.b.irq_d      = m_st.icpsr.irq_d;
		m_st.cpsr.b.fiq_d      = m_st.icpsr.fiq_d;
		m_st.cpsr.b.thumb      = m_st.icpsr.thumb;
		m_st.cpsr.b.s_overflow = m_st.icpsr.s_overflow;
		m_st.cpsr.b.f_overflow = m_st.icpsr.f_overflow;
		m_st.cpsr.b.f_carry    = m_st.icpsr.f_carry;
		m_st.cpsr.b.f_zero     = m_st.icpsr.f_zero;
		m_st.cpsr.b.f_sign     = m_st.icpsr.f_sign;
	}

	void Cpu::SwitchToMode (uint8_t newmode)
	{
		SaveMode(m_st.icpsr.mode);

		switch (newmode)
		{
			case 0x10: // User (non-privileged)
			case 0x1F: // System (privileged 'User' mode)
				switch (m_st.icpsr.mode)
				{
					case 0x10:
					case 0x1F:
					case 0x11:
						std::memcpy(m_st.r + 8, m_st.usr_r, sizeof(m_st.usr_r));
						break;
					default:
						std::memcpy(m_st.r + 13, m_st.usr_r + 5, 2*4);
						break;
				}
				break;
			case 0x11: // FIQ
				std::memcpy(m_st.r + 8, m_st.fiq_r, sizeof(m_st.fiq_r));
				break;
			case 0x12: // IRQ
				std::memcpy(m_st.r + 13, m_st.irq_r, sizeof(m_st.irq_r));
				break;
			case 0x13: // Supervisor (SWI)
				std::memcpy(m_st.r + 13, m_st.svc_r, sizeof(m_st.svc_r));
				break;
			case 0x17: // Abort
				std::memcpy(m_st.r + 13, m_st.abt_r, sizeof(m_st.abt_r));
				break;
			case 0x1B: // Undefined
				std::memcpy(m_st.r + 13, m_st.und_r, sizeof(m_st.und_r));
				break;
			default:
				met_abort("Unknown CPU mode : " << IOS_ADD << (int)newmode);
				break;
		}

		UpdateCpsr();
		m_st.spsr.dw = m_st.cpsr.dw;
		m_st.icpsr.mode = newmode;
	}

	void Cpu::SwitchModeBack ()
	{
		// oldmode is the mode on which we want to switch back
		uint8_t oldmode = m_st.spsr.b.mode, curmode = m_st.icpsr.mode;
		// we don't care if the spsr of the mode we are using is modified
		SaveMode(curmode);

		m_st.cpsr.dw = m_st.spsr.dw;
		UpdateICpsr();
		CheckInterrupt();
		switch (oldmode)
		{
			case 0x10: // User (non-privileged)
			case 0x1F: // System (privileged 'User' mode)
				switch (curmode)
				{
					case 0x10:
					case 0x1F:
					case 0x11:
						std::memcpy(m_st.r + 8, m_st.usr_r, sizeof(m_st.usr_r));
						break;
					default:
						std::memcpy(m_st.r + 13, m_st.usr_r + 5, 2*4);
						break;
				}
				break;
			case 0x11: // FIQ
				std::memcpy(m_st.r + 8, m_st.fiq_r, sizeof(m_st.fiq_r));
				m_st.spsr.dw = m_st.fiq_spsr.dw;
				break;
			case 0x12: // IRQ
				std::memcpy(m_st.r + 13, m_st.irq_r, sizeof(m_st.irq_r));
				m_st.spsr.dw = m_st.irq_spsr.dw;
				break;
			case 0x13: // Supervisor (SWI)
				std::memcpy(m_st.r + 13, m_st.svc_r, sizeof(m_st.svc_r));
				m_st.spsr.dw = m_st.svc_spsr.dw;
				break;
			case 0x17: // Abort
				std::memcpy(m_st.r + 13, m_st.abt_r, sizeof(m_st.abt_r));
				m_st.spsr.dw = m_st.abt_spsr.dw;
				break;
			case 0x1B: // Undefined
				std::memcpy(m_st.r + 13, m_st.und_r, sizeof(m_st.und_r));
				m_st.spsr.dw = m_st.und_spsr.dw;
				break;
			default:
				met_abort("Unknown CPU mode : " << IOS_ADD << (int)oldmode);
				break;
		}
	}

	void Cpu::SaveMode (uint8_t mode)
	{
		switch (mode)
		{
			case 0x10: // User (non-privileged)
			case 0x1F: // System (privileged 'User' mode)
				std::memcpy(m_st.usr_r, m_st.r + 8, sizeof(m_st.usr_r));
				break;
			case 0x11: // FIQ
				std::memcpy(m_st.fiq_r, m_st.r + 8, sizeof(m_st.fiq_r));
				m_st.fiq_spsr.dw = m_st.spsr.dw;
				break;
			case 0x12: // IRQ
				std::memcpy(m_st.irq_r, m_st.r + 13, sizeof(m_st.irq_r));
				m_st.irq_spsr.dw = m_st.spsr.dw;
				break;
			case 0x13: // Supervisor (SWI)
				std::memcpy(m_st.svc_r, m_st.r + 13, sizeof(m_st.svc_r));
				m_st.svc_spsr.dw = m_st.spsr.dw;
				break;
			case 0x17: // Abort
				std::memcpy(m_st.abt_r, m_st.r + 13, sizeof(m_st.abt_r));
				m_st.abt_spsr.dw = m_st.spsr.dw;
				break;
			case 0x1B: // Undefined
				std::memcpy(m_st.und_r, m_st.r + 13, sizeof(m_st.und_r));
				m_st.und_spsr.dw = m_st.spsr.dw;
				break;
			default:
				met_abort("Unknown CPU mode : " << IOS_ADD << (int)mode);
				break;
		}
	}

	void Cpu::Interrupt ()
	{
		// Switch mode
		SwitchToMode(0x12); // IRQ
		// Save PC
		R(14) = R(15);
		// FIXME : why ? this seems to be USELESS ! (look at bios irq end)
		if (m_st.icpsr.thumb)
			R(14) += 2;
		// Switch to ARM
		m_st.icpsr.thumb = false;
		// Disable IRQ
		m_st.icpsr.irq_d = true;
		SetInterrupt(false);
		// Branch on 0x18
		R(15) = 0x1C;
	}

	void Cpu::SoftwareInterrupt ()
	{
		// Switch mode
		SwitchToMode(0x13); // Supervisor
		// Save PC
		R(14) = R(15) - (m_st.icpsr.thumb ? 2 : 4);
		// Switch to ARM
		m_st.icpsr.thumb = false;
		// Disable IRQ
		m_st.icpsr.irq_d = true;
		SetInterrupt(false);
		// Branch on 0x8
		R(15) = 0xC;
	}

	// TODO put this in Bios, no ?
	void Cpu::SoftwareInterrupt (uint32_t comment)
	{
		if (true)
		{
			char buff[256];
			int pos = 0;
			pos += sprintf (buff + pos, "SWI %02xh : ", comment);
			switch (comment) // no one cares about the sound driver
			{
			case 0x00: pos += sprintf (buff + pos, "SoftReset");
				break;
			case 0x01: pos += sprintf (buff + pos, "RegisterRamReset"); // todo: display flags
				break;
			case 0x02: pos += sprintf (buff + pos, "Halt");
				break;
			case 0x03: pos += sprintf (buff + pos, "Stop");
				break;
			case 0x04: pos += sprintf (buff + pos, "IntrWait"); // todo: display flags
				break;
			case 0x05: pos += sprintf (buff + pos, "VBlankIntrWait");
				break;
			case 0x06: pos += sprintf (buff + pos, "Div: ");
				pos += sprintf (buff + pos, "%08xh/%08xh", R(0), R(1));
				break;
			case 0x07: pos += sprintf (buff + pos, "DivArm: ");
				pos += sprintf (buff + pos, "%08xh/%08xh", R(1), R(0));
				break;
			case 0x08: pos += sprintf (buff + pos, "Sqrt: ");
				pos += sprintf (buff + pos, "sqrt(%08xh)", R(0));
				break;
			case 0x09: pos += sprintf (buff + pos, "ArcTan: ");
				pos += sprintf (buff + pos, "atan(%04xh)", R(0) & 0xffff);
				break;
			case 0x0a: pos += sprintf (buff + pos, "ArcTan2: ");
				pos += sprintf (buff + pos, "atan2(%04xh,%04xh)", R(1) & 0xffff, R(0) & 0xffff);
				break;
			case 0x0b: pos += sprintf (buff + pos, "CpuSet: ");
				pos += sprintf (buff + pos, "src=[%08xh], dst=[%08xh], wcnt=%xh, fixed=%c, size=%d", R(0), R(1), R(2) & 0x1fffff, R(2) & 0x1000000 ? 'Y' : 'N', R(2) & 0x4000000 ? 32 : 16);
				break;
			case 0x0c: pos += sprintf (buff + pos, "CpuFastSet: ");
				pos += sprintf (buff + pos, "src=[%08xh], dst=[%08xh], wcnt=%xh, fixed=%c", R(0), R(1), R(2) & 0x1fffff, R(2) & 0x1000000 ? 'Y' : 'N');
				break;
			case 0x0d: pos += sprintf (buff + pos, "GetBiosChecksum");
				break;
			case 0x0e: pos += sprintf (buff + pos, "BgAffineSet: ");
				pos += sprintf (buff + pos, "src=[%08xh], dst=[%08xh], cnt=%d", R(0), R(1), R(2));
				break;
			case 0x0f: pos += sprintf (buff + pos, "ObjAffineSet: ");
				pos += sprintf (buff + pos, "src=[%08xh], dst=[%08xh], cnt=%d, offs=%d", R(0), R(1), R(2), R(3));
				break;
			case 0x10: pos += sprintf (buff + pos, "BitUnPack: ");
				pos += sprintf (buff + pos, "src=[%08xh], dst=[%08xh], info=[%08xh]", R(0), R(1), R(2));
				break;
			case 0x11: pos += sprintf (buff + pos, "LZ77UnCompWram: ");
				pos += sprintf (buff + pos, "src=[%08xh], dst=[%08xh]", R(0), R(1));
				break;
			case 0x12: pos += sprintf (buff + pos, "LZ77UnCompVram: ");
				pos += sprintf (buff + pos, "src=[%08xh], dst=[%08xh]", R(0), R(1));
				break;
			case 0x13: pos += sprintf (buff + pos, "HuffUnComp: ");
				pos += sprintf (buff + pos, "src=[%08xh], dst=[%08xh]", R(0), R(1));
				break;
			case 0x14: pos += sprintf (buff + pos, "RLUnCompWram: ");
				pos += sprintf (buff + pos, "src=[%08xh], dst=[%08xh]", R(0), R(1));
				break;
			case 0x15: pos += sprintf (buff + pos, "RLUnCompVram: ");
				pos += sprintf (buff + pos, "src=[%08xh], dst=[%08xh]", R(0), R(1));
				break;
			case 0x16: pos += sprintf (buff + pos, "Diff8bitUnFilterWram: ");
				pos += sprintf (buff + pos, "src=[%08xh], dst=[%08xh]", R(0), R(1));
				break;
			case 0x17: pos += sprintf (buff + pos, "Diff8bitUnFilterVram: ");
				pos += sprintf (buff + pos, "src=[%08xh], dst=[%08xh]", R(0), R(1));
				break;
			case 0x18: pos += sprintf (buff + pos, "Diff16bitUnFilter: ");
				pos += sprintf (buff + pos, "src=[%08xh], dst=[%08xh]", R(0), R(1));
				break;
			case 0x19: pos += sprintf (buff + pos, "SoundBias");
				break;
			case 0x1a: pos += sprintf (buff + pos, "SoundDriverInit");
				break;
			case 0x1b: pos += sprintf (buff + pos, "SoundDriverMode");
				break;
			case 0x1c: pos += sprintf (buff + pos, "SoundDriverMain");
				break;
			case 0x1d: pos += sprintf (buff + pos, "SoundDriverVSync");
				break;
			case 0x1e: pos += sprintf (buff + pos, "SoundChannelClear");
				break;
			case 0x1f: pos += sprintf (buff + pos, "MidiKey2Freq");
				break;
			case 0x20: pos += sprintf (buff + pos, "SoundWhatever0");
				break;
			case 0x21: pos += sprintf (buff + pos, "SoundWhatever1");
				break;
			case 0x22: pos += sprintf (buff + pos, "SoundWhatever2");
				break;
			case 0x23: pos += sprintf (buff + pos, "SoundWhatever3");
				break;
			case 0x24: pos += sprintf (buff + pos, "SoundWhatever4");
				break;
			case 0x25: pos += sprintf (buff + pos, "MultiBoot: ");
				pos += sprintf (buff + pos, "mbp=[%08xh], mode=%d", R(0), R(1));
				break;
			case 0x26: pos += sprintf (buff + pos, "HardReset");
				break;
			case 0x27: pos += sprintf (buff + pos, "CustomHalt: ");
				pos += sprintf (buff + pos, "val=%02xh", R(2) & 0xff);
				break;
			case 0x28: pos += sprintf (buff + pos, "SoundDriverVSyncOff");
				break;
			case 0x29: pos += sprintf (buff + pos, "SoundDriverVSyncOnn");
				break;
			case 0x2a: pos += sprintf (buff + pos, "SoundGetJumpList");
				break;
			default: pos += sprintf (buff + pos, "UNKNOWN");
				break;
			}
			pos += sprintf (buff + pos, "\n");
			print_bizhawk(buff);
		}
		if (MEM.HasBios())
			SoftwareInterrupt();
		else
			switch (comment)
			{
				case 0x00:
					Bios::SoftReset();
					break;
				case 0x01:
					Bios::RegisterRamReset();
					break;
				case 0x02:
					Bios::Halt();
					break;
				case 0x04:
				case 0x05:
					SoftwareInterrupt();
					break;
				case 0x06:
					Bios::Div();
					break;
				case 0x07:
					Bios::DivArm();
					break;
				case 0x08:
					Bios::Sqrt();
					break;
				case 0x09:
					Bios::ArcTan();
					break;
				case 0x0A:
					Bios::ArcTan2();
					break;
				case 0x0B:
					Bios::CpuSet();
					break;
				case 0x0C:
					Bios::CpuFastSet();
					break;
				case 0x0E:
					Bios::BgAffineSet();
					break;
				case 0x0F:
					Bios::ObjAffineSet();
					break;
				case 0x11:
					Bios::LZ77UnCompWram();
					break;
				case 0x12:
					Bios::LZ77UnCompVram();
					break;
				case 0x13:
					Bios::HuffUnComp();
					break;
				case 0x14:
					Bios::RLUnCompWram();
					break;
				case 0x15:
					Bios::RLUnCompVram();
					break;
				default:
					met_abort("Unknown software interrupt : " << IOS_ADD << comment);
					break;
			}
	}

	bool Cpu::SaveState (std::ostream& stream)
	{
		SS_WRITE_VAR(m_st);

		return true;
	}

	bool Cpu::LoadState (std::istream& stream)
	{
		SS_READ_VAR(m_st);

		CheckInterrupt();

		return true;
	}
}
