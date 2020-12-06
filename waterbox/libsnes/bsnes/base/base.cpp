#include "base.hpp"

#include "snes/snes.hpp"

CDLInfo cdlInfo;

void CDLInfo::dorom(uint32_t addr)
{
	blocks[eCDLog_AddrType_CARTROM_DB][addr] = SNES::cpu.regs.db;
	blocks[eCDLog_AddrType_CARTROM_D][addr*2+0] = SNES::cpu.regs.d;
	blocks[eCDLog_AddrType_CARTROM_D][addr*2+1] = SNES::cpu.regs.d>>8;
}
