/*
 * UAE - The Un*x Amiga Emulator - CPU core
 *
 * Read 68000 CPU specs from file "table68k"
 *
 * Copyright 1995,1996 Bernd Schmidt
 *
 * Adaptation to Hatari by Thomas Huth
 * Adaptation to Virtual Jaguar by James Hammons
 *
 * This file is distributed under the GNU Public License, version 3 or at
 * your option any later version. Read the file GPLv3 for details.
 */


/* 2008/04/26	[NP]	Handle sz_byte for Areg as a valid srcmode if current instruction is a MOVE	*/
/*			(e.g. move.b a1,(a0) ($1089)) (fix Blood Money on Superior 65)			*/


//const char ReadCpu_fileid[] = "Hatari readcpu.c : " __DATE__ " " __TIME__;

#include <ctype.h>
#include <string.h>

#include "readcpu.h"

int nr_cpuop_funcs;

const struct mnemolookup lookuptab[] = {
	{ i_ILLG, "ILLEGAL" },
	{ i_OR, "OR" },
	{ i_CHK, "CHK" },
	{ i_CHK2, "CHK2" },
	{ i_AND, "AND" },
	{ i_EOR, "EOR" },
	{ i_ORSR, "ORSR" },
	{ i_ANDSR, "ANDSR" },
	{ i_EORSR, "EORSR" },
	{ i_SUB, "SUB" },
	{ i_SUBA, "SUBA" },
	{ i_SUBX, "SUBX" },
	{ i_SBCD, "SBCD" },
	{ i_ADD, "ADD" },
	{ i_ADDA, "ADDA" },
	{ i_ADDX, "ADDX" },
	{ i_ABCD, "ABCD" },
	{ i_NEG, "NEG" },
	{ i_NEGX, "NEGX" },
	{ i_NBCD, "NBCD" },
	{ i_CLR, "CLR" },
	{ i_NOT, "NOT" },
	{ i_TST, "TST" },
	{ i_BTST, "BTST" },
	{ i_BCHG, "BCHG" },
	{ i_BCLR, "BCLR" },
	{ i_BSET, "BSET" },
	{ i_CMP, "CMP" },
	{ i_CMPM, "CMPM" },
	{ i_CMPA, "CMPA" },
	{ i_MVPRM, "MVPRM" },
	{ i_MVPMR, "MVPMR" },
	{ i_MOVE, "MOVE" },
	{ i_MOVEA, "MOVEA" },
	{ i_MVSR2, "MVSR2" },
	{ i_MV2SR, "MV2SR" },
	{ i_SWAP, "SWAP" },
	{ i_EXG, "EXG" },
	{ i_EXT, "EXT" },
	{ i_MVMEL, "MVMEL" },
	{ i_MVMLE, "MVMLE" },
	{ i_TRAP, "TRAP" },
	{ i_MVR2USP, "MVR2USP" },
	{ i_MVUSP2R, "MVUSP2R" },
	{ i_NOP, "NOP" },
	{ i_RESET, "RESET" },
	{ i_RTE, "RTE" },
	{ i_RTD, "RTD" },
	{ i_LINK, "LINK" },
	{ i_UNLK, "UNLK" },
	{ i_RTS, "RTS" },
	{ i_STOP, "STOP" },
	{ i_TRAPV, "TRAPV" },
	{ i_RTR, "RTR" },
	{ i_JSR, "JSR" },
	{ i_JMP, "JMP" },
	{ i_BSR, "BSR" },
	{ i_Bcc, "Bcc" },
	{ i_LEA, "LEA" },
	{ i_PEA, "PEA" },
	{ i_DBcc, "DBcc" },
	{ i_Scc, "Scc" },
	{ i_DIVU, "DIVU" },
	{ i_DIVS, "DIVS" },
	{ i_MULU, "MULU" },
	{ i_MULS, "MULS" },
	{ i_ASR, "ASR" },
	{ i_ASL, "ASL" },
	{ i_LSR, "LSR" },
	{ i_LSL, "LSL" },
	{ i_ROL, "ROL" },
	{ i_ROR, "ROR" },
	{ i_ROXL, "ROXL" },
	{ i_ROXR, "ROXR" },
	{ i_ASRW, "ASRW" },
	{ i_ASLW, "ASLW" },
	{ i_LSRW, "LSRW" },
	{ i_LSLW, "LSLW" },
	{ i_ROLW, "ROLW" },
	{ i_RORW, "RORW" },
	{ i_ROXLW, "ROXLW" },
	{ i_ROXRW, "ROXRW" },

	{ i_MOVE2C, "MOVE2C" },
	{ i_MOVEC2, "MOVEC2" },
	{ i_CAS, "CAS" },
	{ i_CAS2, "CAS2" },
	{ i_MULL, "MULL" },
	{ i_DIVL, "DIVL" },
	{ i_BFTST, "BFTST" },
	{ i_BFEXTU, "BFEXTU" },
	{ i_BFCHG, "BFCHG" },
	{ i_BFEXTS, "BFEXTS" },
	{ i_BFCLR, "BFCLR" },
	{ i_BFFFO, "BFFFO" },
	{ i_BFSET, "BFSET" },
	{ i_BFINS, "BFINS" },
	{ i_PACK, "PACK" },
	{ i_UNPK, "UNPK" },
	{ i_TAS, "TAS" },
	{ i_BKPT, "BKPT" },
	{ i_CALLM, "CALLM" },
	{ i_RTM, "RTM" },
	{ i_TRAPcc, "TRAPcc" },
	{ i_MOVES, "MOVES" },
	{ i_FPP, "FPP" },
	{ i_FDBcc, "FDBcc" },
	{ i_FScc, "FScc" },
	{ i_FTRAPcc, "FTRAPcc" },
	{ i_FBcc, "FBcc" },
	{ i_FBcc, "FBcc" },
	{ i_FSAVE, "FSAVE" },
	{ i_FRESTORE, "FRESTORE" },

	{ i_CINVL, "CINVL" },
	{ i_CINVP, "CINVP" },
	{ i_CINVA, "CINVA" },
	{ i_CPUSHL, "CPUSHL" },
	{ i_CPUSHP, "CPUSHP" },
	{ i_CPUSHA, "CPUSHA" },
	{ i_MOVE16, "MOVE16" },

	{ i_MMUOP, "MMUOP" },
	{ i_ILLG, "" },
};


struct instr * table68k;


STATIC_INLINE amodes mode_from_str(const char * str)
{
	if (strncmp (str, "Dreg", 4) == 0) return Dreg;
	if (strncmp (str, "Areg", 4) == 0) return Areg;
	if (strncmp (str, "Aind", 4) == 0) return Aind;
	if (strncmp (str, "Apdi", 4) == 0) return Apdi;
	if (strncmp (str, "Aipi", 4) == 0) return Aipi;
	if (strncmp (str, "Ad16", 4) == 0) return Ad16;
	if (strncmp (str, "Ad8r", 4) == 0) return Ad8r;
	if (strncmp (str, "absw", 4) == 0) return absw;
	if (strncmp (str, "absl", 4) == 0) return absl;
	if (strncmp (str, "PC16", 4) == 0) return PC16;
	if (strncmp (str, "PC8r", 4) == 0) return PC8r;
	if (strncmp (str, "Immd", 4) == 0) return imm;

	abort();
	return 0;
}


STATIC_INLINE amodes mode_from_mr(int mode, int reg)
{
	switch (mode)
	{
		case 0: return Dreg;
		case 1: return Areg;
		case 2: return Aind;
		case 3: return Aipi;
		case 4: return Apdi;
		case 5: return Ad16;
		case 6: return Ad8r;
		case 7:
		switch (reg)
		{
			case 0: return absw;
			case 1: return absl;
			case 2: return PC16;
			case 3: return PC8r;
			case 4: return imm;
			case 5:
			case 6:
			case 7: return am_illg;
		}
	}

	abort();
	return 0;
}


static void build_insn(int insn)
{
	int find = -1;
	int variants;
	int isjmp = 0;
	struct instr_def id;
	const char * opcstr;
	int j;

	int flaglive = 0, flagdead = 0;
	id = defs68k[insn];

	/* Note: We treat anything with unknown flags as a jump. That
	   is overkill, but "the programmer" was lazy quite often, and
	   *this* programmer can't be bothered to work out what can and
	   can't trap. Usually, this will be overwritten with the gencomp
	   based information, anyway. */

	for(j=0; j<5; j++)
	{
		switch (id.flaginfo[j].flagset)
		{
		case fa_unset: break;
		case fa_isjmp: isjmp = 1; break;
		case fa_isbranch: isjmp = 1; break;
		case fa_zero: flagdead |= 1 << j; break;
		case fa_one: flagdead |= 1 << j; break;
		case fa_dontcare: flagdead |= 1 << j; break;
		case fa_unknown: isjmp = 1; flagdead = -1; goto out1;
		case fa_set: flagdead |= 1 << j; break;
		}
    }

out1:
	for(j=0; j<5; j++)
	{
		switch (id.flaginfo[j].flaguse)
		{
		case fu_unused: break;
		case fu_isjmp: isjmp = 1; flaglive |= 1 << j; break;
		case fu_maybecc: isjmp = 1; flaglive |= 1 << j; break;
		case fu_unknown: isjmp = 1; flaglive |= 1 << j; break;
		case fu_used: flaglive |= 1 << j; break;
		}
	}

	opcstr = id.opcstr;

	for(variants=0; variants<(1 << id.n_variable); variants++)
	{
		int bitcnt[lastbit];
		int bitval[lastbit];
		int bitpos[lastbit];
		int i;
		uint16_t opc = id.bits;
		uint16_t msk, vmsk;
		int pos = 0;
		int mnp = 0;
		int bitno = 0;
		char mnemonic[10];

		wordsizes sz = sz_long;
		int srcgather = 0, dstgather = 0;
		int usesrc = 0, usedst = 0;
		int srctype = 0;
		int srcpos = -1, dstpos = -1;

		amodes srcmode = am_unknown, destmode = am_unknown;
		int srcreg = -1, destreg = -1;

		for(i=0; i<lastbit; i++)
			bitcnt[i] = bitval[i] = 0;

		vmsk = 1 << id.n_variable;

		for(i=0, msk=0x8000; i<16; i++, msk >>= 1)
		{
			if (!(msk & id.mask))
			{
				int currbit = id.bitpos[bitno++];
				int bit_set;
				vmsk >>= 1;
				bit_set = (variants & vmsk ? 1 : 0);

				if (bit_set)
					opc |= msk;

				bitpos[currbit] = 15 - i;
				bitcnt[currbit]++;
				bitval[currbit] <<= 1;
				bitval[currbit] |= bit_set;
			}
		}

		if (bitval[bitj] == 0)
			bitval[bitj] = 8;

		/* first check whether this one does not match after all */
		if (bitval[bitz] == 3 || bitval[bitC] == 1)
			continue;

		if (bitcnt[bitI] && (bitval[bitI] == 0x00 || bitval[bitI] == 0xff))
			continue;

		/* bitI and bitC get copied to biti and bitc */
		if (bitcnt[bitI])
		{
			bitval[biti] = bitval[bitI]; bitpos[biti] = bitpos[bitI];
		}

		if (bitcnt[bitC])
			bitval[bitc] = bitval[bitC];

		pos = 0;
		while (opcstr[pos] && !isspace((unsigned)opcstr[pos]))
		{
			if (opcstr[pos] == '.')
			{
				pos++;

				switch (opcstr[pos])
				{
				case 'B': sz = sz_byte; break;
				case 'W': sz = sz_word; break;
				case 'L': sz = sz_long; break;
				case 'z':
					switch (bitval[bitz])
					{
					case 0: sz = sz_byte; break;
					case 1: sz = sz_word; break;
					case 2: sz = sz_long; break;
					default: abort();
					}
					break;
				default: abort();
				}
			}
			else
			{
				mnemonic[mnp] = opcstr[pos];

				if (mnemonic[mnp] == 'f')
				{
					find = -1;
					switch (bitval[bitf])
					{
					case 0: mnemonic[mnp] = 'R'; break;
					case 1: mnemonic[mnp] = 'L'; break;
					default: abort();
					}
				}

				mnp++;
			}

			pos++;
		}

		mnemonic[mnp] = 0;

		/* now, we have read the mnemonic and the size */
		while (opcstr[pos] && isspace((unsigned)opcstr[pos]))
			pos++;

		/* A goto a day keeps the D******a away. */
		if (opcstr[pos] == 0)
			goto endofline;

		/* parse the source address */
		usesrc = 1;
		switch (opcstr[pos++])
		{
		case 'D':
			srcmode = Dreg;

			switch (opcstr[pos++])
			{
			case 'r': srcreg = bitval[bitr]; srcgather = 1; srcpos = bitpos[bitr]; break;
			case 'R': srcreg = bitval[bitR]; srcgather = 1; srcpos = bitpos[bitR]; break;
			default: abort();
			}

			break;
		case 'A':
			srcmode = Areg;

			switch (opcstr[pos++])
			{
			case 'r': srcreg = bitval[bitr]; srcgather = 1; srcpos = bitpos[bitr]; break;
			case 'R': srcreg = bitval[bitR]; srcgather = 1; srcpos = bitpos[bitR]; break;
			default: abort();
			}

			switch (opcstr[pos])
			{
			case 'p': srcmode = Apdi; pos++; break;
			case 'P': srcmode = Aipi; pos++; break;
			}
			break;

		case 'L':
			srcmode = absl;
			break;
		case '#':
			switch (opcstr[pos++])
			{
			case 'z': srcmode = imm; break;
			case '0': srcmode = imm0; break;
			case '1': srcmode = imm1; break;
			case '2': srcmode = imm2; break;
			case 'i':
				srcmode = immi; srcreg = (int32_t)(int8_t)bitval[biti];

				if (CPU_EMU_SIZE < 4)
				{
					/* Used for branch instructions */
					srctype = 1;
					srcgather = 1;
					srcpos = bitpos[biti];
				}

				break;
			case 'j':
				srcmode = immi; srcreg = bitval[bitj];

				if (CPU_EMU_SIZE < 3)
				{
					/* 1..8 for ADDQ/SUBQ and rotshi insns */
					srcgather = 1;
					srctype = 3;
					srcpos = bitpos[bitj];
				}

				break;
			case 'J':
				srcmode = immi; srcreg = bitval[bitJ];

				if (CPU_EMU_SIZE < 5)
				{
					/* 0..15 */
					srcgather = 1;
					srctype = 2;
					srcpos = bitpos[bitJ];
				}

				break;
			case 'k':
				srcmode = immi; srcreg = bitval[bitk];

				if (CPU_EMU_SIZE < 3)
				{
					srcgather = 1;
					srctype = 4;
					srcpos = bitpos[bitk];
				}

				break;
			case 'K':
				srcmode = immi; srcreg = bitval[bitK];

				if (CPU_EMU_SIZE < 5)
				{
					/* 0..15 */
					srcgather = 1;
					srctype = 5;
					srcpos = bitpos[bitK];
				}

				break;
			case 'p':
				srcmode = immi; srcreg = bitval[bitK];

				if (CPU_EMU_SIZE < 5)
				{
					/* 0..3 */
					srcgather = 1;
					srctype = 7;
					srcpos = bitpos[bitp];
				}

				break;
			default: abort();
			}

			break;
		case 'd':
			srcreg = bitval[bitD];
			srcmode = mode_from_mr(bitval[bitd],bitval[bitD]);

			if (srcmode == am_illg)
				continue;

			if (CPU_EMU_SIZE < 2
				&& (srcmode == Areg || srcmode == Dreg || srcmode == Aind
				|| srcmode == Ad16 || srcmode == Ad8r || srcmode == Aipi
				|| srcmode == Apdi))
			{
				srcgather = 1;
				srcpos = bitpos[bitD];
			}

			if (opcstr[pos] == '[')
			{
				pos++;

				if (opcstr[pos] == '!')
				{
					/* exclusion */
					do
					{
						pos++;

						if (mode_from_str(opcstr + pos) == srcmode)
							goto nomatch;

						pos += 4;
					}
					while (opcstr[pos] == ',');

					pos++;
				}
				else
				{
					if (opcstr[pos + 4] == '-')
					{
						/* replacement */
						if (mode_from_str(opcstr + pos) == srcmode)
							srcmode = mode_from_str(opcstr + pos + 5);
						else
							goto nomatch;

						pos += 10;
					}
					else
					{
						/* normal */
						while(mode_from_str(opcstr + pos) != srcmode)
						{
							pos += 4;

							if (opcstr[pos] == ']')
								goto nomatch;

							pos++;
						}

						while(opcstr[pos] != ']')
							pos++;

						pos++;
						break;
					}
				}
			}

			/* Some addressing modes are invalid as destination */
			if (srcmode == imm || srcmode == PC16 || srcmode == PC8r)
				goto nomatch;

			break;
		case 's':
			srcreg = bitval[bitS];
			srcmode = mode_from_mr(bitval[bits],bitval[bitS]);

			if (srcmode == am_illg)
				continue;

			if (CPU_EMU_SIZE < 2
				&& (srcmode == Areg || srcmode == Dreg || srcmode == Aind
				|| srcmode == Ad16 || srcmode == Ad8r || srcmode == Aipi
				|| srcmode == Apdi))
			{
				srcgather = 1;
				srcpos = bitpos[bitS];
			}

			if (opcstr[pos] == '[')
			{
				pos++;

				if (opcstr[pos] == '!')
				{
					/* exclusion */
					do
					{
						pos++;

						if (mode_from_str(opcstr + pos) == srcmode)
							goto nomatch;

						pos += 4;
					}
					while (opcstr[pos] == ',');

					pos++;
				}
				else
				{
					if (opcstr[pos + 4] == '-')
					{
						/* replacement */
						if (mode_from_str(opcstr + pos) == srcmode)
							srcmode = mode_from_str(opcstr + pos + 5);
						else
							goto nomatch;

						pos += 10;
					}
					else
					{
						/* normal */
						while(mode_from_str(opcstr+pos) != srcmode)
						{
							pos += 4;

							if (opcstr[pos] == ']')
								goto nomatch;

							pos++;
						}

						while(opcstr[pos] != ']')
							pos++;

						pos++;
					}
				}
			}
			break;
		default: abort();
		}

		/* safety check - might have changed */
		if (srcmode != Areg && srcmode != Dreg && srcmode != Aind
			&& srcmode != Ad16 && srcmode != Ad8r && srcmode != Aipi
			&& srcmode != Apdi && srcmode != immi)
		{
			srcgather = 0;
		}

//		if (srcmode == Areg && sz == sz_byte)
		if (srcmode == Areg && sz == sz_byte && strcmp(mnemonic, "MOVE") != 0 )	// [NP] move.b is valid on 68000
			goto nomatch;

		if (opcstr[pos] != ',')
			goto endofline;

		pos++;

		/* parse the destination address */
		usedst = 1;

		switch (opcstr[pos++])
		{
		case 'D':
			destmode = Dreg;

			switch (opcstr[pos++])
			{
			case 'r': destreg = bitval[bitr]; dstgather = 1; dstpos = bitpos[bitr]; break;
			case 'R': destreg = bitval[bitR]; dstgather = 1; dstpos = bitpos[bitR]; break;
			default: abort();
			}

			if (dstpos < 0 || dstpos >= 32)
				abort();

			break;
		case 'A':
			destmode = Areg;

			switch (opcstr[pos++])
			{
			case 'r': destreg = bitval[bitr]; dstgather = 1; dstpos = bitpos[bitr]; break;
			case 'R': destreg = bitval[bitR]; dstgather = 1; dstpos = bitpos[bitR]; break;
			case 'x': destreg = 0; dstgather = 0; dstpos = 0; break;
			default: abort();
			}

			if (dstpos < 0 || dstpos >= 32)
				abort();

			switch (opcstr[pos])
			{
			case 'p': destmode = Apdi; pos++; break;
			case 'P': destmode = Aipi; pos++; break;
			}

			break;
		case 'L':
			destmode = absl;
			break;
		case '#':
			switch (opcstr[pos++])
			{
			case 'z': destmode = imm; break;
			case '0': destmode = imm0; break;
			case '1': destmode = imm1; break;
			case '2': destmode = imm2; break;
			case 'i': destmode = immi; destreg = (int32_t)(int8_t)bitval[biti]; break;
			case 'j': destmode = immi; destreg = bitval[bitj]; break;
			case 'J': destmode = immi; destreg = bitval[bitJ]; break;
			case 'k': destmode = immi; destreg = bitval[bitk]; break;
			case 'K': destmode = immi; destreg = bitval[bitK]; break;
			default: abort();
			}
			break;
		case 'd':
			destreg = bitval[bitD];
			destmode = mode_from_mr(bitval[bitd],bitval[bitD]);

			if (destmode == am_illg)
				continue;

			if (CPU_EMU_SIZE < 1
				&& (destmode == Areg || destmode == Dreg || destmode == Aind
				|| destmode == Ad16 || destmode == Ad8r || destmode == Aipi
				|| destmode == Apdi))
			{
				dstgather = 1;
				dstpos = bitpos[bitD];
			}

			if (opcstr[pos] == '[')
			{
				pos++;

				if (opcstr[pos] == '!')
				{
					/* exclusion */
					do
					{
						pos++;

						if (mode_from_str(opcstr + pos) == destmode)
							goto nomatch;

						pos += 4;
					}
					while (opcstr[pos] == ',');

					pos++;
				}
				else
				{
					if (opcstr[pos+4] == '-')
					{
						/* replacement */
						if (mode_from_str(opcstr + pos) == destmode)
							destmode = mode_from_str(opcstr + pos + 5);
						else
							goto nomatch;

						pos += 10;
					}
					else
					{
						/* normal */
						while(mode_from_str(opcstr + pos) != destmode)
						{
							pos += 4;

							if (opcstr[pos] == ']')
								goto nomatch;

							pos++;
						}

						while(opcstr[pos] != ']')
							pos++;

						pos++;
						break;
					}
				}
			}

			/* Some addressing modes are invalid as destination */
			if (destmode == imm || destmode == PC16 || destmode == PC8r)
				goto nomatch;

			break;
		case 's':
			destreg = bitval[bitS];
			destmode = mode_from_mr(bitval[bits], bitval[bitS]);

			if (destmode == am_illg)
			continue;
			if (CPU_EMU_SIZE < 1
				&& (destmode == Areg || destmode == Dreg || destmode == Aind
				|| destmode == Ad16 || destmode == Ad8r || destmode == Aipi
				|| destmode == Apdi))
			{
				dstgather = 1;
				dstpos = bitpos[bitS];
			}

			if (opcstr[pos] == '[')
			{
				pos++;

				if (opcstr[pos] == '!')
				{
					/* exclusion */
					do
					{
						pos++;

						if (mode_from_str(opcstr + pos) == destmode)
							goto nomatch;

						pos += 4;
					}
					while (opcstr[pos] == ',');

					pos++;
				}
				else
				{
					if (opcstr[pos+4] == '-')
					{
						/* replacement */
						if (mode_from_str(opcstr + pos) == destmode)
							destmode = mode_from_str(opcstr + pos + 5);
						else
							goto nomatch;

						pos += 10;
					}
					else
					{
						/* normal */
						while (mode_from_str(opcstr + pos) != destmode)
						{
							pos += 4;

							if (opcstr[pos] == ']')
								goto nomatch;

							pos++;
						}

						while (opcstr[pos] != ']')
							pos++;

						pos++;
					}
				}
			}
			break;
		default: abort();
		}

		/* safety check - might have changed */
		if (destmode != Areg && destmode != Dreg && destmode != Aind
			&& destmode != Ad16 && destmode != Ad8r && destmode != Aipi
			&& destmode != Apdi)
		{
			dstgather = 0;
		}

		if (destmode == Areg && sz == sz_byte)
			goto nomatch;
#if 0
		if (sz == sz_byte && (destmode == Aipi || destmode == Apdi)) {
			dstgather = 0;
		}
#endif
endofline:
		/* now, we have a match */
		if (table68k[opc].mnemo != i_ILLG)
			fprintf(stderr, "Double match: %x: %s\n", opc, opcstr);

		if (find == -1)
		{
			for(find=0; ; find++)
			{
				if (strcmp(mnemonic, lookuptab[find].name) == 0)
				{
					table68k[opc].mnemo = lookuptab[find].mnemo;
					break;
				}

				if (strlen(lookuptab[find].name) == 0)
					abort();
			}
		}
		else
		{
			table68k[opc].mnemo = lookuptab[find].mnemo;
		}

		table68k[opc].cc = bitval[bitc];

		if (table68k[opc].mnemo == i_BTST
			|| table68k[opc].mnemo == i_BSET
			|| table68k[opc].mnemo == i_BCLR
			|| table68k[opc].mnemo == i_BCHG)
		{
			sz = (destmode == Dreg ? sz_long : sz_byte);
		}

		table68k[opc].size = sz;
		table68k[opc].sreg = srcreg;
		table68k[opc].dreg = destreg;
		table68k[opc].smode = srcmode;
		table68k[opc].dmode = destmode;
		table68k[opc].spos = (srcgather ? srcpos : -1);
		table68k[opc].dpos = (dstgather ? dstpos : -1);
		table68k[opc].suse = usesrc;
		table68k[opc].duse = usedst;
		table68k[opc].stype = srctype;
		table68k[opc].plev = id.plevel;
		table68k[opc].clev = id.cpulevel;
#if 0
		for (i = 0; i < 5; i++) {
			table68k[opc].flaginfo[i].flagset = id.flaginfo[i].flagset;
			table68k[opc].flaginfo[i].flaguse = id.flaginfo[i].flaguse;
		}
#endif
		table68k[opc].flagdead = flagdead;
		table68k[opc].flaglive = flaglive;
		table68k[opc].isjmp = isjmp;

nomatch:
	/* FOO! */;
    }
}


void read_table68k(void)
{
	int i;
	table68k = (struct instr *)malloc(65536 * sizeof(struct instr));

	for(i=0; i<65536; i++)
	{
		table68k[i].mnemo = i_ILLG;
		table68k[i].handler = -1;
	}

	for(i=0; i<n_defs68k; i++)
		build_insn(i);
}


static int mismatch;


static void handle_merges (long int opcode)
{
	uint16_t smsk;
	uint16_t dmsk;
	int sbitdst, dstend;
	int srcreg, dstreg;

//0011 DDDd ddss sSSS:00:-NZ00:-----:12: MOVE.W  s,d[!Areg]
//31C3 ->
//0011 0001 1100 0011 : DDD = 0, ddd = 7, sss = 0, SSS = 3

	if (table68k[opcode].spos == -1)
	{
		sbitdst = 1;
		smsk = 0;
	}
	else
	{
		switch (table68k[opcode].stype)
		{
		case 0:
			smsk = 7; sbitdst = 8; break;
		case 1:
			smsk = 255; sbitdst = 256; break;
		case 2:
			smsk = 15; sbitdst = 16; break;
		case 3:
			smsk = 7; sbitdst = 8; break;
		case 4:
			smsk = 7; sbitdst = 8; break;
		case 5:
			smsk = 63; sbitdst = 64; break;
		case 7:
			smsk = 3; sbitdst = 4; break;
		default:
			smsk = 0; sbitdst = 0;
			abort();
			break;
		}

		smsk <<= table68k[opcode].spos;
	}

	if (table68k[opcode].dpos == -1)
	{
		dmsk = 0;
		dstend = 1;
	}
	else
	{
		dmsk = 7 << table68k[opcode].dpos;
		dstend = 8;
	}

	for(srcreg=0; srcreg<sbitdst; srcreg++)
	{
		for(dstreg=0; dstreg<dstend; dstreg++)
		{
			uint16_t code = opcode;

			code = (code & ~smsk) | (srcreg << table68k[opcode].spos);
			code = (code & ~dmsk) | (dstreg << table68k[opcode].dpos);

			/* Check whether this is in fact the same instruction.
			* The instructions should never differ, except for the
			* Bcc.(BW) case. */
			if (table68k[code].mnemo != table68k[opcode].mnemo
				|| table68k[code].size != table68k[opcode].size
				|| table68k[code].suse != table68k[opcode].suse
				|| table68k[code].duse != table68k[opcode].duse)
			{
				mismatch++;
				continue;
			}

			if (table68k[opcode].suse
				&& (table68k[opcode].spos != table68k[code].spos
				|| table68k[opcode].smode != table68k[code].smode
				|| table68k[opcode].stype != table68k[code].stype))
			{
				mismatch++;
				continue;
			}

			if (table68k[opcode].duse
				&& (table68k[opcode].dpos != table68k[code].dpos
				|| table68k[opcode].dmode != table68k[code].dmode))
			{
				mismatch++;
				continue;
			}

			if (code != opcode)
			{
				table68k[code].handler = opcode;

#if 0
if (opcode == 0x31C3 || code == 0x31C3)
{
	printf("Relocate... ($%04X->$%04X)\n", (uint16_t)opcode, code);
	printf(" handler: %08X\n", table68k[code].handler);
	printf("    dreg: %i\n", table68k[code].dreg);
	printf("    sreg: %i\n", table68k[code].sreg);
	printf("    dpos: %i\n", table68k[code].dpos);
	printf("    spos: %i\n", table68k[code].spos);
	printf("   sduse: %i\n", table68k[code].sduse);
	printf("flagdead: %i\n", table68k[code].flagdead);
	printf("flaglive: %i\n", table68k[code].flaglive);
}
#endif
/*
    long int handler;
    unsigned char dreg;
    unsigned char sreg;
    signed char dpos;
    signed char spos;
    unsigned char sduse;
    int flagdead:8, flaglive:8;
    unsigned int mnemo:8;
    unsigned int cc:4;
    unsigned int plev:2;
    unsigned int size:2;
    unsigned int smode:5;
    unsigned int stype:3;
    unsigned int dmode:5;
    unsigned int suse:1;
    unsigned int duse:1;
    unsigned int unused1:1;
    unsigned int clev:3;
    unsigned int isjmp:1;
    unsigned int unused2:4;
*/
			}
		}
	}
}


// What this really does is expand the # of handlers, which is why the
// opcode has to be passed into the opcode handler...
// E.g., $F620 maps into $F621-F627 as well; this code does this expansion.
void do_merges(void)
{
	long int opcode;
	int nr = 0;
	mismatch = 0;

	for(opcode=0; opcode<65536; opcode++)
	{
		if (table68k[opcode].handler != -1 || table68k[opcode].mnemo == i_ILLG)
			continue;

		nr++;
		handle_merges(opcode);
	}

	nr_cpuop_funcs = nr;
}


int get_no_mismatches(void)
{
	return mismatch;
}

