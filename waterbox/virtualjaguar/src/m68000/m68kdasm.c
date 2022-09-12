//
// m68kdasm.c: 68000 instruction disassembly
//
// Originally part of the UAE 68000 cpu core
// by Bernd Schmidt
//
// Adapted to Virtual Jaguar by James Hammons
//
// This file is distributed under the GNU Public License, version 3 or at your
// option any later version. Read the file GPLv3 for details.
//

#include <string.h>
#include "cpudefs.h"
#include "cpuextra.h"
#include "inlines.h"
#include "readcpu.h"


// Stuff from m68kinterface.c
extern unsigned long IllegalOpcode(uint32_t opcode);
extern cpuop_func * cpuFunctionTable[65536];

// Prototypes
void HandleMovem(char * output, uint16_t data, int direction);

// Local "global" variables
static long int m68kpc_offset;

#if 0
#define get_ibyte_1(o) get_byte(regs.pc + (regs.pc_p - regs.pc_oldp) + (o) + 1)
#define get_iword_1(o) get_word(regs.pc + (regs.pc_p - regs.pc_oldp) + (o))
#define get_ilong_1(o) get_long(regs.pc + (regs.pc_p - regs.pc_oldp) + (o))
#else
#define get_ibyte_1(o) m68k_read_memory_8(regs.pc + (o) + 1)
#define get_iword_1(o) m68k_read_memory_16(regs.pc + (o))
#define get_ilong_1(o) m68k_read_memory_32(regs.pc + (o))
#endif


//int32_t ShowEA(FILE * f, int reg, amodes mode, wordsizes size, char * buf)
int32_t ShowEA(int mnemonic, int reg, amodes mode, wordsizes size, char * buf)
{
	uint16_t dp;
	int8_t disp8;
	int16_t disp16;
	int r;
	uint32_t dispreg;
	uint32_t addr;
	int32_t offset = 0;
	char buffer[80];

	switch (mode)
	{
	case Dreg:
		sprintf(buffer,"D%d", reg);
		break;
	case Areg:
		sprintf(buffer,"A%d", reg);
		break;
	case Aind:
		sprintf(buffer,"(A%d)", reg);
		break;
	case Aipi:
		sprintf(buffer,"(A%d)+", reg);
		break;
	case Apdi:
		sprintf(buffer,"-(A%d)", reg);
		break;
	case Ad16:
		disp16 = get_iword_1(m68kpc_offset); m68kpc_offset += 2;
		addr = m68k_areg(regs,reg) + (int16_t)disp16;
		sprintf(buffer,"(A%d,$%X) == $%lX", reg, disp16 & 0xFFFF,
			(unsigned long)addr);
		break;
	case Ad8r:
		dp = get_iword_1(m68kpc_offset); m68kpc_offset += 2;
		disp8 = dp & 0xFF;
		r = (dp & 0x7000) >> 12;
		dispreg = (dp & 0x8000 ? m68k_areg(regs,r) : m68k_dreg(regs,r));

		if (!(dp & 0x800))
			dispreg = (int32_t)(int16_t)(dispreg);

		dispreg <<= (dp >> 9) & 3;

		if (dp & 0x100)
		{
			int32_t outer = 0, disp = 0;
			int32_t base = m68k_areg(regs,reg);
			char name[10];
			sprintf (name,"A%d, ",reg);
			if (dp & 0x80) { base = 0; name[0] = 0; }
			if (dp & 0x40) dispreg = 0;
			if ((dp & 0x30) == 0x20) { disp = (int32_t)(int16_t)get_iword_1(m68kpc_offset); m68kpc_offset += 2; }
			if ((dp & 0x30) == 0x30) { disp = get_ilong_1(m68kpc_offset); m68kpc_offset += 4; }
			base += disp;

			if ((dp & 0x3) == 0x2) { outer = (int32_t)(int16_t)get_iword_1(m68kpc_offset); m68kpc_offset += 2; }
			if ((dp & 0x3) == 0x3) { outer = get_ilong_1(m68kpc_offset); m68kpc_offset += 4; }

			if (!(dp & 4)) base += dispreg;
			if (dp & 3) base = m68k_read_memory_32(base);
			if (dp & 4) base += dispreg;

			addr = base + outer;
			sprintf(buffer,"(%s%c%d.%c*%d+%ld)+%ld == $%lX", name,
				dp & 0x8000 ? 'A' : 'D', (int)r, dp & 0x800 ? 'L' : 'W',
				1 << ((dp >> 9) & 3),
				(long)disp, (long)outer, (unsigned long)addr);
		}
		else
		{
			addr = m68k_areg(regs,reg) + (int32_t)((int8_t)disp8) + dispreg;
			sprintf (buffer,"(A%d, %c%d.%c*%d, $%X) == $%lX", reg,
				dp & 0x8000 ? 'A' : 'D', (int)r, dp & 0x800 ? 'L' : 'W',
				1 << ((dp >> 9) & 3), disp8, (unsigned long)addr);
		}
		break;
	case PC16:
		addr = m68k_getpc() + m68kpc_offset;
		disp16 = get_iword_1(m68kpc_offset); m68kpc_offset += 2;
		addr += (int16_t)disp16;
		sprintf(buffer,"(PC, $%X) == $%lX", disp16 & 0xFFFF, (unsigned long)addr);
		break;
	case PC8r:
		addr = m68k_getpc() + m68kpc_offset;
		dp = get_iword_1(m68kpc_offset); m68kpc_offset += 2;
		disp8 = dp & 0xFF;
		r = (dp & 0x7000) >> 12;
		dispreg = dp & 0x8000 ? m68k_areg(regs,r) : m68k_dreg(regs,r);

		if (!(dp & 0x800))
			dispreg = (int32_t)(int16_t)(dispreg);

		dispreg <<= (dp >> 9) & 3;

		if (dp & 0x100)
		{
			int32_t outer = 0,disp = 0;
			int32_t base = addr;
			char name[10];
			sprintf (name,"PC, ");
			if (dp & 0x80) { base = 0; name[0] = 0; }
			if (dp & 0x40) dispreg = 0;
			if ((dp & 0x30) == 0x20) { disp = (int32_t)(int16_t)get_iword_1(m68kpc_offset); m68kpc_offset += 2; }
			if ((dp & 0x30) == 0x30) { disp = get_ilong_1(m68kpc_offset); m68kpc_offset += 4; }
			base += disp;

			if ((dp & 0x3) == 0x2)
			{
				outer = (int32_t)(int16_t)get_iword_1(m68kpc_offset);
				m68kpc_offset += 2;
			}

			if ((dp & 0x3) == 0x3)
			{
				outer = get_ilong_1(m68kpc_offset);
				m68kpc_offset += 4;
			}

			if (!(dp & 4)) base += dispreg;
			if (dp & 3) base = m68k_read_memory_32(base);
			if (dp & 4) base += dispreg;

			addr = base + outer;
			sprintf(buffer,"(%s%c%d.%c*%d+%ld)+%ld == $%lX", name,
				dp & 0x8000 ? 'A' : 'D', (int)r, dp & 0x800 ? 'L' : 'W',
				1 << ((dp >> 9) & 3), (long)disp, (long)outer, (unsigned long)addr);
		}
		else
		{
			addr += (int32_t)((int8_t)disp8) + dispreg;
			sprintf(buffer,"(PC, %c%d.%c*%d, $%X) == $%lX", dp & 0x8000 ? 'A' : 'D',
				(int)r, dp & 0x800 ? 'L' : 'W',  1 << ((dp >> 9) & 3),
				disp8, (unsigned long)addr);
		}
		break;
	case absw:
		sprintf(buffer,"$%lX", (unsigned long)(int32_t)(int16_t)get_iword_1(m68kpc_offset));
		m68kpc_offset += 2;
		break;
	case absl:
		sprintf(buffer,"$%lX", (unsigned long)get_ilong_1(m68kpc_offset));
		m68kpc_offset += 4;
		break;
	case imm:
		switch (size)
		{
		case sz_byte:
			sprintf(buffer,"#$%X", (unsigned int)(get_iword_1(m68kpc_offset) & 0xFF));
			m68kpc_offset += 2;
			break;
		case sz_word:
			sprintf(buffer,"#$%X", (unsigned int)(get_iword_1(m68kpc_offset) & 0xFFFF));
			m68kpc_offset += 2;
			break;
		case sz_long:
			sprintf(buffer,"#$%lX", (unsigned long)(get_ilong_1(m68kpc_offset)));
			m68kpc_offset += 4;
			break;
		default:
			break;
		}
		break;
	case imm0:
		offset = (int32_t)(int8_t)get_iword_1(m68kpc_offset);
		m68kpc_offset += 2;
		sprintf(buffer,"#$%X", (unsigned int)(offset & 0xFF));
		break;
	case imm1:
		offset = (int32_t)(int16_t)get_iword_1(m68kpc_offset);
		m68kpc_offset += 2;

		if (mnemonic == i_MVMEL)
			HandleMovem(buffer, offset, 0);
		else if (mnemonic == i_MVMLE)
			HandleMovem(buffer, offset, 1);
		else
			sprintf(buffer,"#$%X", (unsigned int)(offset & 0xFFFF));

		break;
	case imm2:
		offset = (int32_t)get_ilong_1(m68kpc_offset);
		m68kpc_offset += 4;
		sprintf(buffer,"#$%lX", (unsigned long)(offset & 0xFFFFFFFF));
		break;
	case immi:
		offset = (int32_t)(int8_t)(reg & 0xFF);
		sprintf(buffer,"#$%lX", (unsigned long)(offset & 0xFFFFFFFF));
		break;
	default:
		break;
	}

//	if (buf == 0)
//		fprintf(f, "%s", buffer);
//	else
	strcat(buf, buffer);

	return offset;
}


void HandleMovem(char * output, uint16_t data, int direction)
{
	uint16_t ascending[16] = {
		0x0001, 0x0002, 0x0004, 0x0008, 0x0010, 0x0020, 0x0040, 0x0080,
		0x0100, 0x0200, 0x0400, 0x0800, 0x1000, 0x2000, 0x4000, 0x8000 };
	uint16_t descending[16] = {
		0x8000, 0x4000, 0x2000, 0x1000, 0x0800, 0x0400, 0x0200, 0x0100,
		0x0080, 0x0040, 0x0020, 0x0010, 0x0008, 0x0004, 0x0002, 0x0001 };

	int i, j, first, runLength, firstPrint = 1;
	char buf[16];
	uint16_t * bitMask;

	bitMask = (direction ? descending : ascending);
	output[0] = 0;

	// Handle D0-D7...
	for(i=0; i<8; i++)
	{
		if (data & bitMask[i])
		{
			first = i;
			runLength = 0;

			for(j=i+1; j<8 && (data & bitMask[j]); j++)
				runLength++;

			i += runLength;

			if (firstPrint)
				firstPrint = 0;
			else
				strcat(output, "/");

			sprintf(buf, "D%d", first);
			strcat(output, buf);

			if (runLength > 0)
			{
				sprintf(buf, "-D%d", first + runLength);
				strcat(output, buf);
			}
		}
	}

	// Handle A0-A7...
	for(i=0; i<8; i++)
	{
		if (data & bitMask[i + 8])
		{
			first = i;
			runLength = 0;

			for(j=i+1; j<8 && (data & bitMask[j+8]); j++)
				runLength++;

			i += runLength;

			if (firstPrint)
				firstPrint = 0;
			else
				strcat(output, "/");

			sprintf(buf, "A%d", first);
			strcat(output, buf);

			if (runLength > 0)
			{
				sprintf(buf, "-A%d", first + runLength);
				strcat(output, buf);
			}
		}
	}
}


unsigned int M68KDisassemble(char * output, uint32_t addr)
{
	char f[256], str[256];
	char src[256], dst[256];
	static const char * const ccnames[] =
		{ "RA","RN","HI","LS","CC","CS","NE","EQ",
		  "VC","VS","PL","MI","GE","LT","GT","LE" };

	str[0] = 0;
	output[0] = 0;
	uint32_t newpc = 0;
	m68kpc_offset = addr - m68k_getpc();
	long int pcOffsetSave = m68kpc_offset;
	int opwords;
	char instrname[20];
	const struct mnemolookup * lookup;

	uint32_t opcode = get_iword_1(m68kpc_offset);
	m68kpc_offset += 2;

	if (cpuFunctionTable[opcode] == IllegalOpcode)
		opcode = 0x4AFC;

	struct instr * dp = table68k + opcode;

	for(lookup=lookuptab; lookup->mnemo!=dp->mnemo; lookup++)
		;

	strcpy(instrname, lookup->name);
	char * ccpt = strstr(instrname, "cc");

	if (ccpt)
		strncpy(ccpt, ccnames[dp->cc], 2);

	sprintf(f, "%s", instrname);
	strcat(str, f);

	switch (dp->size)
	{
	case sz_byte: strcat(str, ".B\t"); break;
	case sz_word: strcat(str, ".W\t"); break;
	case sz_long: strcat(str, ".L\t"); break;
	default: strcat(str, "\t"); break;
	}

	// Get source and destination operands (if any)
	src[0] = dst[0] = f[0] = 0;

	if (dp->suse)
		newpc = m68k_getpc() + m68kpc_offset
			+ ShowEA(dp->mnemo, dp->sreg, dp->smode, dp->size, src);

	if (dp->duse)
		newpc = m68k_getpc() + m68kpc_offset
			+ ShowEA(dp->mnemo, dp->dreg, dp->dmode, dp->size, dst);

	// Handle execptions to the standard rules
	if (dp->mnemo == i_BSR || dp->mnemo == i_Bcc)
		sprintf(f, "$%lX", (long)newpc);
	else if (dp->mnemo == i_DBcc)
		sprintf(f, "%s, $%lX", src, (long)newpc);
	else if (dp->mnemo == i_MVMEL)
		sprintf(f, "%s, %s", dst, src);
	else
		sprintf(f, "%s%s%s", src, (dp->suse && dp->duse ? ", " : ""), dst);

	strcat(str, f);

	if (ccpt)
	{
		sprintf(f, " (%s)", (cctrue(dp->cc) ? "true" : "false"));
		strcat(str, f);
	}

	// Add byte(s) display to front of disassembly
	long int numberOfBytes = m68kpc_offset - pcOffsetSave;

	for(opwords=0; opwords<5; opwords++)
	{
		if (((opwords + 1) * 2) <= numberOfBytes)
			sprintf(f, "%04X ", get_iword_1(pcOffsetSave + opwords * 2));
		else
			sprintf(f, "     ");

		strcat(output, f);
	}

	strcat(output, str);

	return numberOfBytes;
}


//
// Disassemble one instruction at pc and store in str_buff
//
unsigned int m68k_disassemble(char * str_buff, unsigned int pc, unsigned int cpu_type)
{
	return M68KDisassemble(str_buff, pc);
}

