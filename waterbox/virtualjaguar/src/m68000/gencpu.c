/*
 * UAE - The Un*x Amiga Emulator - CPU core
 *
 * MC68000 emulation generator
 *
 * This is a fairly stupid program that generates a lot of case labels that
 * can be #included in a switch statement.
 * As an alternative, it can generate functions that handle specific
 * MC68000 instructions, plus a prototype header file and a function pointer
 * array to look up the function for an opcode.
 * Error checking is bad, an illegal table68k file will cause the program to
 * call abort().
 * The generated code is sometimes sub-optimal, an optimizing compiler should
 * take care of this.
 *
 * The source for the insn timings is Markt & Technik's Amiga Magazin 8/1992.
 *
 * Copyright 1995, 1996, 1997, 1998, 1999, 2000 Bernd Schmidt
 *
 * Adaptation to Hatari and better cpu timings by Thomas Huth
 * Adaptation to Virtual Jaguar by James Hammons
 *
 * This file is distributed under the GNU Public License, version 3 or at
 * your option any later version. Read the file GPLv3 for details.
 *
 */


/* 2007/03/xx	[NP]	Use add_cycles.pl to set 'CurrentInstrCycles' in each opcode.			*/
/* 2007/04/09	[NP]	Correct CLR : on 68000, CLR reads the memory before clearing it (but we should	*/
/*			not add cycles for reading). This means CLR can give 2 wait states (one for	*/
/*			read and one for right) (clr.b $fa1b.w in Decade's Demo Main Menu).		*/
/* 2007/04/14	[NP]	- Although dest -(an) normally takes 2 cycles, this is not the case for move :	*/
/*			move dest (an), (an)+ and -(an) all take the same time (curi->dmode == Apdi)	*/
/*			(Syntax Terror Demo Reset).							*/
/*			- Scc takes 6 cycles instead of 4 if the result is true (Ventura Demo Loader).	*/
/*			- Store the family of the current opcode into OpcodeFamily : used to check	*/
/*			instruction pairing on ST into m68000.c						*/
/* 2007/04/17	[NP]	Add support for cycle accurate MULU (No Cooper Greeting Screen).		*/	
/* 2007/04/24	[NP]	BCLR #n,Dx takes 12 cycles instead of 14 if n<16 (ULM Demo Menu).		*/
/* 2007/04/25	[NP]	On ST, d8(An,Xn) and d8(PC,Xn) take 2 cycles more than the official 68000's	*/
/*			table (ULM Demo Menu).								*/
/* 2007/11/12	[NP]	Add refill_prefetch for i_ADD to fix Transbeauce 2 demo self modified code.	*/
/*			Ugly hack, we need better prefetch emulation (switch to winuae gencpu.c)	*/
/* 2007/11/25	[NP]	In i_DBcc, in case of address error, last_addr_for_exception_3 should be	*/
/*			pc+4, not pc+2 (Transbeauce 2 demo) (e.g. 'dbf d0,#$fff5').			*/
/*			This means the value pushed on the frame stack should be the address of the	*/
/*			instruction following the one generating the address error.			*/
/*			FIXME : this should be the case for i_BSR and i_BCC too	(need to check on	*/
/*			a real 68000).									*/
/* 2007/11/28	[NP]	Backport DIVS/DIVU cycles exact routines from WinUAE (original work by Jorge	*/
/*			Cwik, pasti@fxatari.com).							*/
/* 2007/12/08	[NP]	In case of CHK/CHK2 exception, PC stored on the stack wasn't pointing to the	*/
/*			next instruction but to the current CHK/CHK2 instruction (Transbeauce 2 demo).	*/
/*			We need to call 'sync_m68k_pc' before calling 'Exception'.			*/
/* 2007/12/09	[NP]	CHK.L (e.g. $4700) doesn't exist on 68000 and should be considered as an illegal*/
/*			instruction (Transbeauce 2 demo) -> change in table68k.				*/
/* 2008/01/24	[NP]	BCLR Dy,Dx takes 8 cycles instead of 10 if Dy<16 (Fullshade in Anomaly Demos).	*/
/* 2008/01/26	[NP]	On ST, d8(An,Xn) takes 2 cycles more when used with ADDA/SUBA (ULM Demo Menu)	*/
/*			but not when used with MOVE (e.g. 'move.l 0(a5,d1),(a4)' takes 26 cycles and so	*/
/*			can pair with a lsr) (Anomaly Demo Intro).					*/
/* 2008/04/26	[NP]	Handle sz_byte for Areg in genamode, as 'move.b a1,(a0)' ($1089) is possible	*/
/*			on ST (fix Blood Money on Superior 65)						*/
/* 2010/04/05	[NP]	On ST, d8(An,Xn) takes 2 cycles more (which can generate pairing).		*/
/*			Use BusCyclePenalty to properly handle the 2/4 cycles added in that case when	*/
/*			addressing mode	is Ad8r or PC8r	(ULM Demo Menu, Anomaly Demo Intro, DHS		*/
/*			Sommarhack 2010) (see m68000.h)							*/


//const char GenCpu_fileid[] = "Hatari gencpu.c : " __DATE__ " " __TIME__;

#include <ctype.h>
#include <string.h>

#include "sysdeps.h"
#include "readcpu.h"

#define BOOL_TYPE "int"

static FILE *headerfile;
static FILE *stblfile;

static int using_prefetch;
static int using_exception_3;
static int cpu_level;

char exactCpuCycles[256];   /* Space to store return string for exact cpu cycles */

long nCurInstrCycPos;  /* Stores where we have to patch in the current cycles value */

/* For the current opcode, the next lower level that will have different code.
 * Initialized to -1 for each opcode. If it remains unchanged, indicates we
 * are done with that opcode.  */
static int next_cpu_level;
static int *opcode_map;
static int *opcode_next_clev;
static int *opcode_last_postfix;
static unsigned long *counts;


static void read_counts (void)
{
    FILE *file;
    unsigned long opcode, count, total;
    char name[20];
    int nr = 0;
    memset (counts, 0, 65536 * sizeof *counts);

    file = fopen ("frequent.68k", "r");
    if (file) {
	if (fscanf (file, "Total: %lu\n", &total) == EOF) {
	    perror("read_counts");
	}
	while (fscanf (file, "%lx: %lu %s\n", &opcode, &count, name) == 3) {
	    opcode_next_clev[nr] = 4;
	    opcode_last_postfix[nr] = -1;
	    opcode_map[nr++] = opcode;
	    counts[opcode] = count;
	}
	fclose (file);
    }
    if (nr == nr_cpuop_funcs)
	return;
    for (opcode = 0; opcode < 0x10000; opcode++) {
	if (table68k[opcode].handler == -1 && table68k[opcode].mnemo != i_ILLG
	    && counts[opcode] == 0)
	{
	    opcode_next_clev[nr] = 4;
	    opcode_last_postfix[nr] = -1;
	    opcode_map[nr++] = opcode;
	    counts[opcode] = count;
	}
    }
    if (nr != nr_cpuop_funcs)
	abort ();
}

static char endlabelstr[80];
static int endlabelno = 0;
static int need_endlabel;

static int n_braces = 0;
static int m68k_pc_offset = 0;
static int insn_n_cycles;

static void start_brace (void)
{
    n_braces++;
    printf ("{");
}

static void close_brace (void)
{
    assert (n_braces > 0);
    n_braces--;
    printf ("}");
}

static void finish_braces (void)
{
    while (n_braces > 0)
	close_brace ();
}

static void pop_braces (int to)
{
    while (n_braces > to)
	close_brace ();
}

static int bit_size (int size)
{
    switch (size) {
     case sz_byte: return 8;
     case sz_word: return 16;
     case sz_long: return 32;
     default: abort ();
    }
    return 0;
}

static const char *bit_mask (int size)
{
    switch (size) {
     case sz_byte: return "0xff";
     case sz_word: return "0xffff";
     case sz_long: return "0xffffffff";
     default: abort ();
    }
    return 0;
}

static const char *gen_nextilong (void)
{
    static char buffer[80];
    int r = m68k_pc_offset;
    m68k_pc_offset += 4;

    insn_n_cycles += 8;

    if (using_prefetch)
	sprintf (buffer, "get_ilong_prefetch(%d)", r);
    else
	sprintf (buffer, "get_ilong(%d)", r);
    return buffer;
}

static const char *gen_nextiword (void)
{
    static char buffer[80];
    int r = m68k_pc_offset;
    m68k_pc_offset += 2;

    insn_n_cycles += 4;

    if (using_prefetch)
	sprintf (buffer, "get_iword_prefetch(%d)", r);
    else
	sprintf (buffer, "get_iword(%d)", r);
    return buffer;
}

static const char *gen_nextibyte (void)
{
    static char buffer[80];
    int r = m68k_pc_offset;
    m68k_pc_offset += 2;

    insn_n_cycles += 4;

    if (using_prefetch)
	sprintf (buffer, "get_ibyte_prefetch(%d)", r);
    else
	sprintf (buffer, "get_ibyte(%d)", r);
    return buffer;
}

static void fill_prefetch_0 (void)
{
    if (using_prefetch)
	printf ("fill_prefetch_0 ();\n");
}

static void fill_prefetch_2 (void)
{
    if (using_prefetch)
	printf ("fill_prefetch_2 ();\n");
}

static void sync_m68k_pc(void)
{
	if (m68k_pc_offset == 0)
		return;

	printf("m68k_incpc(%d);\n", m68k_pc_offset);

	switch (m68k_pc_offset)
	{
	case 0:
	/*fprintf (stderr, "refilling prefetch at 0\n"); */
		break;
	case 2:
		fill_prefetch_2();
		break;
	default:
		fill_prefetch_0();
		break;
	}

	m68k_pc_offset = 0;
}

/* getv == 1: fetch data; getv != 0: check for odd address. If movem != 0,
 * the calling routine handles Apdi and Aipi modes.
 * gb-- movem == 2 means the same thing but for a MOVE16 instruction */
static void genamode(amodes mode, const char * reg, wordsizes size,
	const char * name, int getv, int movem)
{
	start_brace();
	switch (mode)
	{
	case Dreg:
	if (movem)
		abort ();
	if (getv == 1)
		switch (size) {
		case sz_byte:
		printf ("\tint8_t %s = m68k_dreg(regs, %s);\n", name, reg);
		break;
		case sz_word:
		printf ("\tint16_t %s = m68k_dreg(regs, %s);\n", name, reg);
		break;
		case sz_long:
		printf ("\tint32_t %s = m68k_dreg(regs, %s);\n", name, reg);
		break;
		default:
		abort ();
		}
	return;
	case Areg:
	if (movem)
		abort ();
	if (getv == 1)
		switch (size) {
		case sz_byte:				// [NP] Areg with .b is possible in MOVE source */
		printf ("\tint8_t %s = m68k_areg(regs, %s);\n", name, reg);
		break;
		case sz_word:
		printf ("\tint16_t %s = m68k_areg(regs, %s);\n", name, reg);
		break;
		case sz_long:
		printf ("\tint32_t %s = m68k_areg(regs, %s);\n", name, reg);
		break;
		default:
		abort ();
		}
	return;
	case Aind:
	printf ("\tuint32_t %sa = m68k_areg(regs, %s);\n", name, reg);
	break;
	case Aipi:
	printf ("\tuint32_t %sa = m68k_areg(regs, %s);\n", name, reg);
	break;
	case Apdi:
	insn_n_cycles += 2;
	switch (size) {
	case sz_byte:
		if (movem)
		printf ("\tuint32_t %sa = m68k_areg(regs, %s);\n", name, reg);
		else
		printf ("\tuint32_t %sa = m68k_areg(regs, %s) - areg_byteinc[%s];\n", name, reg, reg);
		break;
	case sz_word:
		printf ("\tuint32_t %sa = m68k_areg(regs, %s) - %d;\n", name, reg, movem ? 0 : 2);
		break;
	case sz_long:
		printf ("\tuint32_t %sa = m68k_areg(regs, %s) - %d;\n", name, reg, movem ? 0 : 4);
		break;
	default:
		abort ();
	}
	break;
	case Ad16:
	printf ("\tuint32_t %sa = m68k_areg(regs, %s) + (int32_t)(int16_t)%s;\n", name, reg, gen_nextiword ());
	break;
	case Ad8r:
	insn_n_cycles += 2;
	if (cpu_level > 1) {
		if (next_cpu_level < 1)
		next_cpu_level = 1;
		sync_m68k_pc ();
		start_brace ();
		/* This would ordinarily be done in gen_nextiword, which we bypass.  */
		insn_n_cycles += 4;
		printf ("\tuint32_t %sa = get_disp_ea_020(m68k_areg(regs, %s), next_iword());\n", name, reg);
	} else {
		printf ("\tuint32_t %sa = get_disp_ea_000(m68k_areg(regs, %s), %s);\n", name, reg, gen_nextiword ());
	}
	printf ("\tBusCyclePenalty += 2;\n");

	break;
	case PC16:
	printf ("\tuint32_t %sa = m68k_getpc () + %d;\n", name, m68k_pc_offset);
	printf ("\t%sa += (int32_t)(int16_t)%s;\n", name, gen_nextiword ());
	break;
	case PC8r:
	insn_n_cycles += 2;
	if (cpu_level > 1) {
		if (next_cpu_level < 1)
		next_cpu_level = 1;
		sync_m68k_pc ();
		start_brace ();
		/* This would ordinarily be done in gen_nextiword, which we bypass.  */
		insn_n_cycles += 4;
		printf ("\tuint32_t tmppc = m68k_getpc();\n");
		printf ("\tuint32_t %sa = get_disp_ea_020(tmppc, next_iword());\n", name);
	} else {
		printf ("\tuint32_t tmppc = m68k_getpc() + %d;\n", m68k_pc_offset);
		printf ("\tuint32_t %sa = get_disp_ea_000(tmppc, %s);\n", name, gen_nextiword ());
	}
	printf ("\tBusCyclePenalty += 2;\n");

	break;
	case absw:
	printf ("\tuint32_t %sa = (int32_t)(int16_t)%s;\n", name, gen_nextiword ());
	break;
	case absl:
	printf ("\tuint32_t %sa = %s;\n", name, gen_nextilong ());
	break;
	case imm:
	if (getv != 1)
		abort ();
	switch (size) {
	case sz_byte:
		printf ("\tint8_t %s = %s;\n", name, gen_nextibyte ());
		break;
	case sz_word:
		printf ("\tint16_t %s = %s;\n", name, gen_nextiword ());
		break;
	case sz_long:
		printf ("\tint32_t %s = %s;\n", name, gen_nextilong ());
		break;
	default:
		abort ();
	}
	return;
	case imm0:
	if (getv != 1)
		abort ();
	printf ("\tint8_t %s = %s;\n", name, gen_nextibyte ());
	return;
	case imm1:
	if (getv != 1)
		abort ();
	printf ("\tint16_t %s = %s;\n", name, gen_nextiword ());
	return;
	case imm2:
	if (getv != 1)
		abort ();
	printf ("\tint32_t %s = %s;\n", name, gen_nextilong ());
	return;
	case immi:
	if (getv != 1)
		abort ();
	printf ("\tuint32_t %s = %s;\n", name, reg);
	return;
	default:
	abort ();
	}

	/* We get here for all non-reg non-immediate addressing modes to
	* actually fetch the value. */

	if (using_exception_3 && getv != 0 && size != sz_byte) {	    
	printf ("\tif ((%sa & 1) != 0) {\n", name);
	printf ("\t\tlast_fault_for_exception_3 = %sa;\n", name);
	printf ("\t\tlast_op_for_exception_3 = opcode;\n");
	printf ("\t\tlast_addr_for_exception_3 = m68k_getpc() + %d;\n", m68k_pc_offset);
	printf ("\t\tException(3, 0, M68000_EXC_SRC_CPU);\n");
	printf ("\t\tgoto %s;\n", endlabelstr);
	printf ("\t}\n");
	need_endlabel = 1;
	start_brace ();
	}

	if (getv == 1) {
	switch (size) {
	case sz_byte: insn_n_cycles += 4; break;
	case sz_word: insn_n_cycles += 4; break;
	case sz_long: insn_n_cycles += 8; break;
	default: abort ();
	}
	start_brace ();
	switch (size) {
	case sz_byte: printf ("\tint8_t %s = m68k_read_memory_8(%sa);\n", name, name); break;
	case sz_word: printf ("\tint16_t %s = m68k_read_memory_16(%sa);\n", name, name); break;
	case sz_long: printf ("\tint32_t %s = m68k_read_memory_32(%sa);\n", name, name); break;
	default: abort ();
	}
	}

	/* We now might have to fix up the register for pre-dec or post-inc
	* addressing modes. */
	if (!movem)
	switch (mode) {
	case Aipi:
		switch (size) {
		case sz_byte:
		printf ("\tm68k_areg(regs, %s) += areg_byteinc[%s];\n", reg, reg);
		break;
		case sz_word:
		printf ("\tm68k_areg(regs, %s) += 2;\n", reg);
		break;
		case sz_long:
		printf ("\tm68k_areg(regs, %s) += 4;\n", reg);
		break;
		default:
		abort ();
		}
		break;
	case Apdi:
		printf ("\tm68k_areg (regs, %s) = %sa;\n", reg, name);
		break;
	default:
		break;
	}
}

static void genastore (const char *from, amodes mode, const char *reg,
                       wordsizes size, const char *to)
{
    switch (mode) {
     case Dreg:
	switch (size) {
	 case sz_byte:
	    printf ("\tm68k_dreg(regs, %s) = (m68k_dreg(regs, %s) & ~0xff) | ((%s) & 0xff);\n", reg, reg, from);
	    break;
	 case sz_word:
	    printf ("\tm68k_dreg(regs, %s) = (m68k_dreg(regs, %s) & ~0xffff) | ((%s) & 0xffff);\n", reg, reg, from);
	    break;
	 case sz_long:
	    printf ("\tm68k_dreg(regs, %s) = (%s);\n", reg, from);
	    break;
	 default:
	    abort ();
	}
	break;
     case Areg:
	switch (size) {
	 case sz_word:
	    fprintf (stderr, "Foo\n");
	    printf ("\tm68k_areg(regs, %s) = (int32_t)(int16_t)(%s);\n", reg, from);
	    break;
	 case sz_long:
	    printf ("\tm68k_areg(regs, %s) = (%s);\n", reg, from);
	    break;
	 default:
	    abort ();
	}
	break;
     case Aind:
     case Aipi:
     case Apdi:
     case Ad16:
     case Ad8r:
     case absw:
     case absl:
     case PC16:
     case PC8r:
	if (using_prefetch)
	    sync_m68k_pc ();
	switch (size) {
	 case sz_byte:
	    insn_n_cycles += 4;
	    printf ("\tm68k_write_memory_8(%sa,%s);\n", to, from);
	    break;
	 case sz_word:
	    insn_n_cycles += 4;
	    if (cpu_level < 2 && (mode == PC16 || mode == PC8r))
		abort ();
	    printf ("\tm68k_write_memory_16(%sa,%s);\n", to, from);
	    break;
	 case sz_long:
	    insn_n_cycles += 8;
	    if (cpu_level < 2 && (mode == PC16 || mode == PC8r))
		abort ();
	    printf ("\tm68k_write_memory_32(%sa,%s);\n", to, from);
	    break;
	 default:
	    abort ();
	}
	break;
     case imm:
     case imm0:
     case imm1:
     case imm2:
     case immi:
	abort ();
	break;
     default:
	abort ();
    }
}


static void genmovemel (uint16_t opcode)
{
    char getcode[100];
    int bMovemLong = (table68k[opcode].size == sz_long);
    int size = bMovemLong ? 4 : 2;

    if (bMovemLong) {
	strcpy (getcode, "m68k_read_memory_32(srca)");
    } else {
	strcpy (getcode, "(int32_t)(int16_t)m68k_read_memory_16(srca)");
    }

    printf ("\tuint16_t mask = %s;\n", gen_nextiword ());
    printf ("\tunsigned int dmask = mask & 0xff, amask = (mask >> 8) & 0xff;\n");
    printf ("\tretcycles = 0;\n");
    genamode (table68k[opcode].dmode, "dstreg", table68k[opcode].size, "src", 2, 1);
    start_brace ();
    printf ("\twhile (dmask) { m68k_dreg(regs, movem_index1[dmask]) = %s;"
            " srca += %d; dmask = movem_next[dmask]; retcycles+=%d; }\n",
	    getcode, size, (bMovemLong ? 8 : 4));
    printf ("\twhile (amask) { m68k_areg(regs, movem_index1[amask]) = %s;"
            " srca += %d; amask = movem_next[amask]; retcycles+=%d; }\n",
	    getcode, size, (bMovemLong ? 8 : 4));

    if (table68k[opcode].dmode == Aipi)
	printf ("\tm68k_areg(regs, dstreg) = srca;\n");

    /* Better cycles - experimental! (Thothy) */
    switch(table68k[opcode].dmode)
    {
      case Aind:  insn_n_cycles=12; break;
      case Aipi:  insn_n_cycles=12; break;
      case Ad16:  insn_n_cycles=16; break;
      case Ad8r:  insn_n_cycles=18; break;
      case absw:  insn_n_cycles=16; break;
      case absl:  insn_n_cycles=20; break;
      case PC16:  insn_n_cycles=16; break;
      case PC8r:  insn_n_cycles=18; break;
    }
    sprintf(exactCpuCycles," return (%i+retcycles);", insn_n_cycles);
}

static void genmovemle (uint16_t opcode)
{
    char putcode[100];
    int bMovemLong = (table68k[opcode].size == sz_long);
    int size = bMovemLong ? 4 : 2;

    if (bMovemLong) {
	strcpy (putcode, "m68k_write_memory_32(srca,");
    } else {
	strcpy (putcode, "m68k_write_memory_16(srca,");
    }

    printf ("\tuint16_t mask = %s;\n", gen_nextiword ());
    printf ("\tretcycles = 0;\n");
    genamode (table68k[opcode].dmode, "dstreg", table68k[opcode].size, "src", 2, 1);
    if (using_prefetch)
	sync_m68k_pc ();

    start_brace ();
    if (table68k[opcode].dmode == Apdi) {
        printf ("\tuint16_t amask = mask & 0xff, dmask = (mask >> 8) & 0xff;\n");
        printf ("\twhile (amask) { srca -= %d; %s m68k_areg(regs, movem_index2[amask]));"
                " amask = movem_next[amask]; retcycles+=%d; }\n",
                size, putcode, (bMovemLong ? 8 : 4));
        printf ("\twhile (dmask) { srca -= %d; %s m68k_dreg(regs, movem_index2[dmask]));"
                " dmask = movem_next[dmask]; retcycles+=%d; }\n",
                size, putcode, (bMovemLong ? 8 : 4));
        printf ("\tm68k_areg(regs, dstreg) = srca;\n");
    } else {
        printf ("\tuint16_t dmask = mask & 0xff, amask = (mask >> 8) & 0xff;\n");
        printf ("\twhile (dmask) { %s m68k_dreg(regs, movem_index1[dmask])); srca += %d;"
                " dmask = movem_next[dmask]; retcycles+=%d; }\n",
                putcode, size, (bMovemLong ? 8 : 4));
        printf ("\twhile (amask) { %s m68k_areg(regs, movem_index1[amask])); srca += %d;"
                " amask = movem_next[amask]; retcycles+=%d; }\n",
                putcode, size, (bMovemLong ? 8 : 4));
    }

    /* Better cycles - experimental! (Thothy) */
    switch(table68k[opcode].dmode)
    {
      case Aind:  insn_n_cycles=8; break;
      case Apdi:  insn_n_cycles=8; break;
      case Ad16:  insn_n_cycles=12; break;
      case Ad8r:  insn_n_cycles=14; break;
      case absw:  insn_n_cycles=12; break;
      case absl:  insn_n_cycles=16; break;
    }
    sprintf(exactCpuCycles," return (%i+retcycles);", insn_n_cycles);
}


static void duplicate_carry (void)
{
    printf ("\tCOPY_CARRY;\n");
}

typedef enum
{
  flag_logical_noclobber, flag_logical, flag_add, flag_sub, flag_cmp, flag_addx, flag_subx, flag_zn,
  flag_av, flag_sv
}
flagtypes;

static void genflags_normal (flagtypes type, wordsizes size, const char *value,
                             const char *src, const char *dst)
{
    char vstr[100], sstr[100], dstr[100];
    char usstr[100], udstr[100];
    char unsstr[100], undstr[100];

    switch (size) {
     case sz_byte:
	strcpy (vstr, "((int8_t)(");
	strcpy (usstr, "((uint8_t)(");
	break;
     case sz_word:
	strcpy (vstr, "((int16_t)(");
	strcpy (usstr, "((uint16_t)(");
	break;
     case sz_long:
	strcpy (vstr, "((int32_t)(");
	strcpy (usstr, "((uint32_t)(");
	break;
     default:
	abort ();
    }
    strcpy (unsstr, usstr);

    strcpy (sstr, vstr);
    strcpy (dstr, vstr);
    strcat (vstr, value);
    strcat (vstr, "))");
    strcat (dstr, dst);
    strcat (dstr, "))");
    strcat (sstr, src);
    strcat (sstr, "))");

    strcpy (udstr, usstr);
    strcat (udstr, dst);
    strcat (udstr, "))");
    strcat (usstr, src);
    strcat (usstr, "))");

    strcpy (undstr, unsstr);
    strcat (unsstr, "-");
    strcat (undstr, "~");
    strcat (undstr, dst);
    strcat (undstr, "))");
    strcat (unsstr, src);
    strcat (unsstr, "))");

    switch (type) {
     case flag_logical_noclobber:
     case flag_logical:
     case flag_zn:
     case flag_av:
     case flag_sv:
     case flag_addx:
     case flag_subx:
	break;

     case flag_add:
	start_brace ();
	printf ("uint32_t %s = %s + %s;\n", value, dstr, sstr);
	break;
     case flag_sub:
     case flag_cmp:
	start_brace ();
	printf ("uint32_t %s = %s - %s;\n", value, dstr, sstr);
	break;
    }

    switch (type) {
     case flag_logical_noclobber:
     case flag_logical:
     case flag_zn:
	break;

     case flag_add:
     case flag_sub:
     case flag_addx:
     case flag_subx:
     case flag_cmp:
     case flag_av:
     case flag_sv:
	start_brace ();
	printf ("\t" BOOL_TYPE " flgs = %s < 0;\n", sstr);
	printf ("\t" BOOL_TYPE " flgo = %s < 0;\n", dstr);
	printf ("\t" BOOL_TYPE " flgn = %s < 0;\n", vstr);
	break;
    }

    switch (type) {
     case flag_logical:
	printf ("\tCLEAR_CZNV;\n");
	printf ("\tSET_ZFLG (%s == 0);\n", vstr);
	printf ("\tSET_NFLG (%s < 0);\n", vstr);
	break;
     case flag_logical_noclobber:
	printf ("\tSET_ZFLG (%s == 0);\n", vstr);
	printf ("\tSET_NFLG (%s < 0);\n", vstr);
	break;
     case flag_av:
	printf ("\tSET_VFLG ((flgs ^ flgn) & (flgo ^ flgn));\n");
	break;
     case flag_sv:
	printf ("\tSET_VFLG ((flgs ^ flgo) & (flgn ^ flgo));\n");
	break;
     case flag_zn:
	printf ("\tSET_ZFLG (GET_ZFLG & (%s == 0));\n", vstr);
	printf ("\tSET_NFLG (%s < 0);\n", vstr);
	break;
     case flag_add:
	printf ("\tSET_ZFLG (%s == 0);\n", vstr);
	printf ("\tSET_VFLG ((flgs ^ flgn) & (flgo ^ flgn));\n");
	printf ("\tSET_CFLG (%s < %s);\n", undstr, usstr);
	duplicate_carry ();
	printf ("\tSET_NFLG (flgn != 0);\n");
	break;
     case flag_sub:
	printf ("\tSET_ZFLG (%s == 0);\n", vstr);
	printf ("\tSET_VFLG ((flgs ^ flgo) & (flgn ^ flgo));\n");
	printf ("\tSET_CFLG (%s > %s);\n", usstr, udstr);
	duplicate_carry ();
	printf ("\tSET_NFLG (flgn != 0);\n");
	break;
     case flag_addx:
	printf ("\tSET_VFLG ((flgs ^ flgn) & (flgo ^ flgn));\n"); /* minterm SON: 0x42 */
	printf ("\tSET_CFLG (flgs ^ ((flgs ^ flgo) & (flgo ^ flgn)));\n"); /* minterm SON: 0xD4 */
	duplicate_carry ();
	break;
     case flag_subx:
	printf ("\tSET_VFLG ((flgs ^ flgo) & (flgo ^ flgn));\n"); /* minterm SON: 0x24 */
	printf ("\tSET_CFLG (flgs ^ ((flgs ^ flgn) & (flgo ^ flgn)));\n"); /* minterm SON: 0xB2 */
	duplicate_carry ();
	break;
     case flag_cmp:
	printf ("\tSET_ZFLG (%s == 0);\n", vstr);
	printf ("\tSET_VFLG ((flgs != flgo) && (flgn != flgo));\n");
	printf ("\tSET_CFLG (%s > %s);\n", usstr, udstr);
	printf ("\tSET_NFLG (flgn != 0);\n");
	break;
    }
}

static void genflags (flagtypes type, wordsizes size, const char *value,
                      const char *src, const char *dst)
{
    /* Temporarily deleted 68k/ARM flag optimizations.  I'd prefer to have
       them in the appropriate m68k.h files and use just one copy of this
       code here.  The API can be changed if necessary.  */
#ifdef OPTIMIZED_FLAGS
    switch (type) {
     case flag_add:
     case flag_sub:
	start_brace ();
	printf ("\tuint32_t %s;\n", value);
	break;

     default:
	break;
    }

    /* At least some of those casts are fairly important! */
    switch (type) {
     case flag_logical_noclobber:
	printf ("\t{uint32_t oldcznv = GET_CZNV & ~(FLAGVAL_Z | FLAGVAL_N);\n");
	if (strcmp (value, "0") == 0) {
	    printf ("\tSET_CZNV (olcznv | FLAGVAL_Z);\n");
	} else {
	    switch (size) {
	     case sz_byte: printf ("\toptflag_testb ((int8_t)(%s));\n", value); break;
	     case sz_word: printf ("\toptflag_testw ((int16_t)(%s));\n", value); break;
	     case sz_long: printf ("\toptflag_testl ((int32_t)(%s));\n", value); break;
	    }
	    printf ("\tIOR_CZNV (oldcznv);\n");
	}
	printf ("\t}\n");
	return;
     case flag_logical:
	if (strcmp (value, "0") == 0) {
	    printf ("\tSET_CZNV (FLAGVAL_Z);\n");
	} else {
	    switch (size) {
	     case sz_byte: printf ("\toptflag_testb ((int8_t)(%s));\n", value); break;
	     case sz_word: printf ("\toptflag_testw ((int16_t)(%s));\n", value); break;
	     case sz_long: printf ("\toptflag_testl ((int32_t)(%s));\n", value); break;
	    }
	}
	return;

     case flag_add:
	switch (size) {
	 case sz_byte: printf ("\toptflag_addb (%s, (int8_t)(%s), (int8_t)(%s));\n", value, src, dst); break;
	 case sz_word: printf ("\toptflag_addw (%s, (int16_t)(%s), (int16_t)(%s));\n", value, src, dst); break;
	 case sz_long: printf ("\toptflag_addl (%s, (int32_t)(%s), (int32_t)(%s));\n", value, src, dst); break;
	}
	return;

     case flag_sub:
	switch (size) {
	 case sz_byte: printf ("\toptflag_subb (%s, (int8_t)(%s), (int8_t)(%s));\n", value, src, dst); break;
	 case sz_word: printf ("\toptflag_subw (%s, (int16_t)(%s), (int16_t)(%s));\n", value, src, dst); break;
	 case sz_long: printf ("\toptflag_subl (%s, (int32_t)(%s), (int32_t)(%s));\n", value, src, dst); break;
	}
	return;

     case flag_cmp:
	switch (size) {
	 case sz_byte: printf ("\toptflag_cmpb ((int8_t)(%s), (int8_t)(%s));\n", src, dst); break;
	 case sz_word: printf ("\toptflag_cmpw ((int16_t)(%s), (int16_t)(%s));\n", src, dst); break;
	 case sz_long: printf ("\toptflag_cmpl ((int32_t)(%s), (int32_t)(%s));\n", src, dst); break;
	}
	return;
	
     default:
	break;
    }
#endif

    genflags_normal (type, size, value, src, dst);
}

static void force_range_for_rox (const char *var, wordsizes size)
{
    /* Could do a modulo operation here... which one is faster? */
    switch (size) {
     case sz_long:
	printf ("\tif (%s >= 33) %s -= 33;\n", var, var);
	break;
     case sz_word:
	printf ("\tif (%s >= 34) %s -= 34;\n", var, var);
	printf ("\tif (%s >= 17) %s -= 17;\n", var, var);
	break;
     case sz_byte:
	printf ("\tif (%s >= 36) %s -= 36;\n", var, var);
	printf ("\tif (%s >= 18) %s -= 18;\n", var, var);
	printf ("\tif (%s >= 9) %s -= 9;\n", var, var);
	break;
    }
}

static const char *cmask (wordsizes size)
{
    switch (size) {
     case sz_byte: return "0x80";
     case sz_word: return "0x8000";
     case sz_long: return "0x80000000";
     default: abort ();
    }
}

static int source_is_imm1_8 (struct instr *i)
{
    return i->stype == 3;
}



static void gen_opcode (unsigned long int opcode)
{
#if 0
    char *amodenames[] = { "Dreg", "Areg", "Aind", "Aipi", "Apdi", "Ad16", "Ad8r",
         "absw", "absl", "PC16", "PC8r", "imm", "imm0", "imm1", "imm2", "immi", "am_unknown", "am_illg"};
#endif

    struct instr *curi = table68k + opcode;
    insn_n_cycles = 4;

    /* Store the family of the instruction (used to check for pairing on ST)
     * and leave some space for patching in the current cycles later */
    printf ("\tOpcodeFamily = %d; CurrentInstrCycles =     \n", curi->mnemo);
    nCurInstrCycPos = ftell(stdout) - 5;

    start_brace ();
    m68k_pc_offset = 2;

    switch (curi->plev) {
    case 0: /* not privileged */
	break;
    case 1: /* unprivileged only on 68000 */
	if (cpu_level == 0)
	    break;
	if (next_cpu_level < 0)
	    next_cpu_level = 0;

	/* fall through */
    case 2: /* priviledged */
	printf ("if (!regs.s) { Exception(8,0,M68000_EXC_SRC_CPU); goto %s; }\n", endlabelstr);
	need_endlabel = 1;
	start_brace ();
	break;
    case 3: /* privileged if size == word */
	if (curi->size == sz_byte)
	    break;
	printf ("if (!regs.s) { Exception(8,0,M68000_EXC_SRC_CPU); goto %s; }\n", endlabelstr);
	need_endlabel = 1;
	start_brace ();
	break;
    }

    /* Build the opcodes: */
    switch (curi->mnemo) {
    case i_OR:
    case i_AND:
    case i_EOR:
        genamode (curi->smode, "srcreg", curi->size, "src", 1, 0);
        genamode (curi->dmode, "dstreg", curi->size, "dst", 1, 0);
        printf ("\tsrc %c= dst;\n", curi->mnemo == i_OR ? '|' : curi->mnemo == i_AND ? '&' : '^');
        genflags (flag_logical, curi->size, "src", "", "");
        genastore ("src", curi->dmode, "dstreg", curi->size, "dst");
        if(curi->size==sz_long && curi->dmode==Dreg)
         {
          insn_n_cycles += 2;
          if(curi->smode==Dreg || curi->smode==Areg || (curi->smode>=imm && curi->smode<=immi))
            insn_n_cycles += 2;
         }
#if 0
        /* Output the CPU cycles: */
        fprintf(stderr,"MOVE, size %i: ",curi->size);
        fprintf(stderr," %s ->",amodenames[curi->smode]);
        fprintf(stderr," %s ",amodenames[curi->dmode]);
        fprintf(stderr," Cycles: %i\n",insn_n_cycles);
#endif
        break;
    case i_ORSR:
    case i_EORSR:
        printf ("\tMakeSR();\n");
        genamode (curi->smode, "srcreg", curi->size, "src", 1, 0);
        if (curi->size == sz_byte) {
            printf ("\tsrc &= 0xFF;\n");
        }
        printf ("\tregs.sr %c= src;\n", curi->mnemo == i_EORSR ? '^' : '|');
        printf ("\tMakeFromSR();\n");
        insn_n_cycles = 20;
        break;
    case i_ANDSR:
        printf ("\tMakeSR();\n");
        genamode (curi->smode, "srcreg", curi->size, "src", 1, 0);
        if (curi->size == sz_byte) {
            printf ("\tsrc |= 0xFF00;\n");
        }
        printf ("\tregs.sr &= src;\n");
        printf ("\tMakeFromSR();\n");
        insn_n_cycles = 20;
        break;
    case i_SUB:
	genamode (curi->smode, "srcreg", curi->size, "src", 1, 0);
	genamode (curi->dmode, "dstreg", curi->size, "dst", 1, 0);
	start_brace ();
	genflags (flag_sub, curi->size, "newv", "src", "dst");
	genastore ("newv", curi->dmode, "dstreg", curi->size, "dst");
        if(curi->size==sz_long && curi->dmode==Dreg)
         {
          insn_n_cycles += 2;
          if(curi->smode==Dreg || curi->smode==Areg || (curi->smode>=imm && curi->smode<=immi))
            insn_n_cycles += 2;
         }
	break;
    case i_SUBA:
	genamode (curi->smode, "srcreg", curi->size, "src", 1, 0);
	genamode (curi->dmode, "dstreg", sz_long, "dst", 1, 0);
	start_brace ();
	printf ("\tuint32_t newv = dst - src;\n");
	genastore ("newv", curi->dmode, "dstreg", sz_long, "dst");
        if(curi->size==sz_long && curi->smode!=Dreg && curi->smode!=Areg && !(curi->smode>=imm && curi->smode<=immi))
          insn_n_cycles += 2;
         else
          insn_n_cycles += 4;
	break;
    case i_SUBX:
	genamode (curi->smode, "srcreg", curi->size, "src", 1, 0);
	genamode (curi->dmode, "dstreg", curi->size, "dst", 1, 0);
	start_brace ();
	printf ("\tuint32_t newv = dst - src - (GET_XFLG ? 1 : 0);\n");
	genflags (flag_subx, curi->size, "newv", "src", "dst");
	genflags (flag_zn, curi->size, "newv", "", "");
	genastore ("newv", curi->dmode, "dstreg", curi->size, "dst");
        if(curi->smode==Dreg && curi->size==sz_long)
          insn_n_cycles=8;
        if(curi->smode==Apdi)
         {
          if(curi->size==sz_long)
            insn_n_cycles=30;
           else
            insn_n_cycles=18;
         }
	break;
    case i_SBCD:
	genamode (curi->smode, "srcreg", curi->size, "src", 1, 0);
	genamode (curi->dmode, "dstreg", curi->size, "dst", 1, 0);
	start_brace ();
	printf ("\tuint16_t newv_lo = (dst & 0xF) - (src & 0xF) - (GET_XFLG ? 1 : 0);\n");
	printf ("\tuint16_t newv_hi = (dst & 0xF0) - (src & 0xF0);\n");
	printf ("\tuint16_t newv, tmp_newv;\n");
	printf ("\tint bcd = 0;\n");
	printf ("\tnewv = tmp_newv = newv_hi + newv_lo;\n");
	printf ("\tif (newv_lo & 0xF0) { newv -= 6; bcd = 6; };\n");
	printf ("\tif ((((dst & 0xFF) - (src & 0xFF) - (GET_XFLG ? 1 : 0)) & 0x100) > 0xFF) { newv -= 0x60; }\n");
	printf ("\tSET_CFLG ((((dst & 0xFF) - (src & 0xFF) - bcd - (GET_XFLG ? 1 : 0)) & 0x300) > 0xFF);\n");
	duplicate_carry ();
	genflags (flag_zn, curi->size, "newv", "", "");
	printf ("\tSET_VFLG ((tmp_newv & 0x80) != 0 && (newv & 0x80) == 0);\n");
	genastore ("newv", curi->dmode, "dstreg", curi->size, "dst");
	if(curi->smode==Dreg)  insn_n_cycles=6;
	if(curi->smode==Apdi)  insn_n_cycles=18;
	break;
    case i_ADD:
	genamode (curi->smode, "srcreg", curi->size, "src", 1, 0);
	genamode (curi->dmode, "dstreg", curi->size, "dst", 1, 0);
	start_brace ();
	printf("\trefill_prefetch (m68k_getpc(), 2);\n");	// FIXME [NP] For Transbeauce 2 demo, need better prefetch emulation
	genflags (flag_add, curi->size, "newv", "src", "dst");
	genastore ("newv", curi->dmode, "dstreg", curi->size, "dst");
        if(curi->size==sz_long && curi->dmode==Dreg)
         {
          insn_n_cycles += 2;
          if(curi->smode==Dreg || curi->smode==Areg || (curi->smode>=imm && curi->smode<=immi))
            insn_n_cycles += 2;
         }
	break;
    case i_ADDA:
	genamode (curi->smode, "srcreg", curi->size, "src", 1, 0);
	genamode (curi->dmode, "dstreg", sz_long, "dst", 1, 0);
	start_brace ();
	printf ("\tuint32_t newv = dst + src;\n");
	genastore ("newv", curi->dmode, "dstreg", sz_long, "dst");
        if(curi->size==sz_long && curi->smode!=Dreg && curi->smode!=Areg && !(curi->smode>=imm && curi->smode<=immi))
          insn_n_cycles += 2;
         else
          insn_n_cycles += 4;
	break;
    case i_ADDX:
	genamode (curi->smode, "srcreg", curi->size, "src", 1, 0);
	genamode (curi->dmode, "dstreg", curi->size, "dst", 1, 0);
	start_brace ();
	printf ("\tuint32_t newv = dst + src + (GET_XFLG ? 1 : 0);\n");
	genflags (flag_addx, curi->size, "newv", "src", "dst");
	genflags (flag_zn, curi->size, "newv", "", "");
	genastore ("newv", curi->dmode, "dstreg", curi->size, "dst");
        if(curi->smode==Dreg && curi->size==sz_long)
          insn_n_cycles=8;
        if(curi->smode==Apdi)
         {
          if(curi->size==sz_long)
            insn_n_cycles=30;
           else
            insn_n_cycles=18;
         }
	break;
    case i_ABCD:
	genamode (curi->smode, "srcreg", curi->size, "src", 1, 0);
	genamode (curi->dmode, "dstreg", curi->size, "dst", 1, 0);
	start_brace ();
	printf ("\tuint16_t newv_lo = (src & 0xF) + (dst & 0xF) + (GET_XFLG ? 1 : 0);\n");
	printf ("\tuint16_t newv_hi = (src & 0xF0) + (dst & 0xF0);\n");
	printf ("\tuint16_t newv, tmp_newv;\n");
	printf ("\tint cflg;\n");
	printf ("\tnewv = tmp_newv = newv_hi + newv_lo;");
	printf ("\tif (newv_lo > 9) { newv += 6; }\n");
	printf ("\tcflg = (newv & 0x3F0) > 0x90;\n");
	printf ("\tif (cflg) newv += 0x60;\n");
	printf ("\tSET_CFLG (cflg);\n");
	duplicate_carry ();
	genflags (flag_zn, curi->size, "newv", "", "");
	printf ("\tSET_VFLG ((tmp_newv & 0x80) == 0 && (newv & 0x80) != 0);\n");
	genastore ("newv", curi->dmode, "dstreg", curi->size, "dst");
	if(curi->smode==Dreg)  insn_n_cycles=6;
	if(curi->smode==Apdi)  insn_n_cycles=18;
	break;
    case i_NEG:
	genamode (curi->smode, "srcreg", curi->size, "src", 1, 0);
	start_brace ();
	genflags (flag_sub, curi->size, "dst", "src", "0");
	genastore ("dst", curi->smode, "srcreg", curi->size, "src");
        if(curi->size==sz_long && curi->smode==Dreg)  insn_n_cycles += 2;
	break;
    case i_NEGX:
	genamode (curi->smode, "srcreg", curi->size, "src", 1, 0);
	start_brace ();
	printf ("\tuint32_t newv = 0 - src - (GET_XFLG ? 1 : 0);\n");
	genflags (flag_subx, curi->size, "newv", "src", "0");
	genflags (flag_zn, curi->size, "newv", "", "");
	genastore ("newv", curi->smode, "srcreg", curi->size, "src");
        if(curi->size==sz_long && curi->smode==Dreg)  insn_n_cycles += 2;
	break;
    case i_NBCD:
	genamode (curi->smode, "srcreg", curi->size, "src", 1, 0);
	start_brace ();
	printf ("\tuint16_t newv_lo = - (src & 0xF) - (GET_XFLG ? 1 : 0);\n");
	printf ("\tuint16_t newv_hi = - (src & 0xF0);\n");
	printf ("\tuint16_t newv;\n");
	printf ("\tint cflg;\n");
	printf ("\tif (newv_lo > 9) { newv_lo -= 6; }\n");
	printf ("\tnewv = newv_hi + newv_lo;");
	printf ("\tcflg = (newv & 0x1F0) > 0x90;\n");
	printf ("\tif (cflg) newv -= 0x60;\n");
	printf ("\tSET_CFLG (cflg);\n");
	duplicate_carry();
	genflags (flag_zn, curi->size, "newv", "", "");
	genastore ("newv", curi->smode, "srcreg", curi->size, "src");
	if(curi->smode==Dreg)  insn_n_cycles += 2;
	break;
    case i_CLR:
	genamode (curi->smode, "srcreg", curi->size, "src", 2, 0);

	/* [NP] CLR does a read before the write only on 68000 */
	/* but there's no cycle penalty for doing the read */
	if ( curi->smode != Dreg )			// only if destination is memory
	  {
	    if (curi->size==sz_byte)
	      printf ("\tint8_t src = m68k_read_memory_8(srca);\n");
	    else if (curi->size==sz_word)
	      printf ("\tint16_t src = m68k_read_memory_16(srca);\n");
	    else if (curi->size==sz_long)
	      printf ("\tint32_t src = m68k_read_memory_32(srca);\n");
	  }

	genflags (flag_logical, curi->size, "0", "", "");
	genastore ("0", curi->smode, "srcreg", curi->size, "src");
        if(curi->size==sz_long)
        {
          if(curi->smode==Dreg)
            insn_n_cycles += 2;
           else
            insn_n_cycles += 4;
        }
        if(curi->smode!=Dreg)
          insn_n_cycles += 4;
	break;
    case i_NOT:
	genamode (curi->smode, "srcreg", curi->size, "src", 1, 0);
	start_brace ();
	printf ("\tuint32_t dst = ~src;\n");
	genflags (flag_logical, curi->size, "dst", "", "");
	genastore ("dst", curi->smode, "srcreg", curi->size, "src");
        if(curi->size==sz_long && curi->smode==Dreg)  insn_n_cycles += 2;
	break;
    case i_TST:
	genamode (curi->smode, "srcreg", curi->size, "src", 1, 0);
	genflags (flag_logical, curi->size, "src", "", "");
	break;
    case i_BTST:
	genamode (curi->smode, "srcreg", curi->size, "src", 1, 0);
	genamode (curi->dmode, "dstreg", curi->size, "dst", 1, 0);
	if (curi->size == sz_byte)
	    printf ("\tsrc &= 7;\n");
	else
	    printf ("\tsrc &= 31;\n");
	printf ("\tSET_ZFLG (1 ^ ((dst >> src) & 1));\n");
        if(curi->dmode==Dreg)  insn_n_cycles += 2;
	break;
    case i_BCHG:
	genamode (curi->smode, "srcreg", curi->size, "src", 1, 0);
	genamode (curi->dmode, "dstreg", curi->size, "dst", 1, 0);
	if (curi->size == sz_byte)
	    printf ("\tsrc &= 7;\n");
	else
	    printf ("\tsrc &= 31;\n");
	printf ("\tdst ^= (1 << src);\n");
	printf ("\tSET_ZFLG (((uint32_t)dst & (1 << src)) >> src);\n");
	genastore ("dst", curi->dmode, "dstreg", curi->size, "dst");
        if(curi->dmode==Dreg)  insn_n_cycles += 4;
	break;
    case i_BCLR:
	genamode (curi->smode, "srcreg", curi->size, "src", 1, 0);
	genamode (curi->dmode, "dstreg", curi->size, "dst", 1, 0);
	if (curi->size == sz_byte)
	    printf ("\tsrc &= 7;\n");
	else
	    printf ("\tsrc &= 31;\n");
	printf ("\tSET_ZFLG (1 ^ ((dst >> src) & 1));\n");
	printf ("\tdst &= ~(1 << src);\n");
	genastore ("dst", curi->dmode, "dstreg", curi->size, "dst");
        if(curi->dmode==Dreg)  insn_n_cycles += 6;
	/* [NP] BCLR #n,Dx takes 12 cycles instead of 14 if n<16 */
        if((curi->smode==imm1) && (curi->dmode==Dreg))
	    printf ("\tif ( src < 16 ) { m68k_incpc(4); return 12; }\n");
	/* [NP] BCLR Dy,Dx takes 8 cycles instead of 10 if Dy<16 */
        if((curi->smode==Dreg) && (curi->dmode==Dreg))
	    printf ("\tif ( src < 16 ) { m68k_incpc(2); return 8; }\n");
	break;
    case i_BSET:
	genamode (curi->smode, "srcreg", curi->size, "src", 1, 0);
	genamode (curi->dmode, "dstreg", curi->size, "dst", 1, 0);
	if (curi->size == sz_byte)
	    printf ("\tsrc &= 7;\n");
	else
	    printf ("\tsrc &= 31;\n");
	printf ("\tSET_ZFLG (1 ^ ((dst >> src) & 1));\n");
	printf ("\tdst |= (1 << src);\n");
	genastore ("dst", curi->dmode, "dstreg", curi->size, "dst");
        if(curi->dmode==Dreg)  insn_n_cycles += 4;
	break;
    case i_CMPM:
    case i_CMP:
	genamode (curi->smode, "srcreg", curi->size, "src", 1, 0);
	genamode (curi->dmode, "dstreg", curi->size, "dst", 1, 0);
	start_brace ();
	genflags (flag_cmp, curi->size, "newv", "src", "dst");
        if(curi->size==sz_long && curi->dmode==Dreg)
          insn_n_cycles += 2;
	break;
    case i_CMPA:
	genamode (curi->smode, "srcreg", curi->size, "src", 1, 0);
	genamode (curi->dmode, "dstreg", sz_long, "dst", 1, 0);
	start_brace ();
	genflags (flag_cmp, sz_long, "newv", "src", "dst");
        insn_n_cycles += 2;
	break;
	/* The next two are coded a little unconventional, but they are doing
	 * weird things... */
    case i_MVPRM:
	genamode (curi->smode, "srcreg", curi->size, "src", 1, 0);

	printf ("\tuint32_t memp = m68k_areg(regs, dstreg) + (int32_t)(int16_t)%s;\n", gen_nextiword ());
	if (curi->size == sz_word) {
	    printf ("\tm68k_write_memory_8(memp, src >> 8); m68k_write_memory_8(memp + 2, src);\n");
	} else {
	    printf ("\tm68k_write_memory_8(memp, src >> 24); m68k_write_memory_8(memp + 2, src >> 16);\n");
	    printf ("\tm68k_write_memory_8(memp + 4, src >> 8); m68k_write_memory_8(memp + 6, src);\n");
	}
        if(curi->size==sz_long)  insn_n_cycles=24;  else  insn_n_cycles=16;
	break;
    case i_MVPMR:
	printf ("\tuint32_t memp = m68k_areg(regs, srcreg) + (int32_t)(int16_t)%s;\n", gen_nextiword ());
	genamode (curi->dmode, "dstreg", curi->size, "dst", 2, 0);
	if (curi->size == sz_word) {
	    printf ("\tuint16_t val = (m68k_read_memory_8(memp) << 8) + m68k_read_memory_8(memp + 2);\n");
	} else {
	    printf ("\tuint32_t val = (m68k_read_memory_8(memp) << 24) + (m68k_read_memory_8(memp + 2) << 16)\n");
	    printf ("              + (m68k_read_memory_8(memp + 4) << 8) + m68k_read_memory_8(memp + 6);\n");
	}
	genastore ("val", curi->dmode, "dstreg", curi->size, "dst");
        if(curi->size==sz_long)  insn_n_cycles=24;  else  insn_n_cycles=16;
	break;
    case i_MOVE:
	genamode (curi->smode, "srcreg", curi->size, "src", 1, 0);
	genamode (curi->dmode, "dstreg", curi->size, "dst", 2, 0);

	/* [NP] genamode counts 2 cycles if dest is -(An), this is wrong. */
	/* For move dest (An), (An)+ and -(An) take the same time */
	/* (for other instr, dest -(An) really takes 2 cycles more) */
	if ( curi->dmode == Apdi )
	  insn_n_cycles -= 2;			/* correct the wrong cycle count for -(An) */

	genflags (flag_logical, curi->size, "src", "", "");
	genastore ("src", curi->dmode, "dstreg", curi->size, "dst");
	break;
    case i_MOVEA:
	genamode (curi->smode, "srcreg", curi->size, "src", 1, 0);
	genamode (curi->dmode, "dstreg", curi->size, "dst", 2, 0);
	if (curi->size == sz_word) {
	    printf ("\tuint32_t val = (int32_t)(int16_t)src;\n");
	} else {
	    printf ("\tuint32_t val = src;\n");
	}
	genastore ("val", curi->dmode, "dstreg", sz_long, "dst");
	break;
    case i_MVSR2:  /* Move from SR */
	genamode (curi->smode, "srcreg", sz_word, "src", 2, 0);
	printf ("\tMakeSR();\n");
	if (curi->size == sz_byte)
	    genastore ("regs.sr & 0xff", curi->smode, "srcreg", sz_word, "src");
	else
	    genastore ("regs.sr", curi->smode, "srcreg", sz_word, "src");
        if (curi->smode==Dreg)  insn_n_cycles += 2;  else  insn_n_cycles += 4;
	break;
    case i_MV2SR:  /* Move to SR */
	genamode (curi->smode, "srcreg", sz_word, "src", 1, 0);
	if (curi->size == sz_byte)
	    printf ("\tMakeSR();\n\tregs.sr &= 0xFF00;\n\tregs.sr |= src & 0xFF;\n");
	else {
	    printf ("\tregs.sr = src;\n");
	}
	printf ("\tMakeFromSR();\n");
        insn_n_cycles += 8;
	break;
    case i_SWAP:
	genamode (curi->smode, "srcreg", sz_long, "src", 1, 0);
	start_brace ();
	printf ("\tuint32_t dst = ((src >> 16)&0xFFFF) | ((src&0xFFFF)<<16);\n");
	genflags (flag_logical, sz_long, "dst", "", "");
	genastore ("dst", curi->smode, "srcreg", sz_long, "src");
	break;
    case i_EXG:
	genamode (curi->smode, "srcreg", curi->size, "src", 1, 0);
	genamode (curi->dmode, "dstreg", curi->size, "dst", 1, 0);
	genastore ("dst", curi->smode, "srcreg", curi->size, "src");
	genastore ("src", curi->dmode, "dstreg", curi->size, "dst");
        insn_n_cycles = 6;
	break;
    case i_EXT:
	genamode (curi->smode, "srcreg", sz_long, "src", 1, 0);
	start_brace ();
	switch (curi->size) {
	case sz_byte: printf ("\tuint32_t dst = (int32_t)(int8_t)src;\n"); break;
	case sz_word: printf ("\tuint16_t dst = (int16_t)(int8_t)src;\n"); break;
	case sz_long: printf ("\tuint32_t dst = (int32_t)(int16_t)src;\n"); break;
	default: abort ();
	}
	genflags (flag_logical,
		  curi->size == sz_word ? sz_word : sz_long, "dst", "", "");
	genastore ("dst", curi->smode, "srcreg",
		   curi->size == sz_word ? sz_word : sz_long, "src");
	break;
    case i_MVMEL:
	genmovemel (opcode);
	break;
    case i_MVMLE:
	genmovemle (opcode);
	break;
    case i_TRAP:
	genamode (curi->smode, "srcreg", curi->size, "src", 1, 0);
	sync_m68k_pc ();
	printf ("\tException(src+32,0,M68000_EXC_SRC_CPU);\n");
	m68k_pc_offset = 0;
	break;
    case i_MVR2USP:
	genamode (curi->smode, "srcreg", curi->size, "src", 1, 0);
	printf ("\tregs.usp = src;\n");
	break;
    case i_MVUSP2R:
	genamode (curi->smode, "srcreg", curi->size, "src", 2, 0);
	genastore ("regs.usp", curi->smode, "srcreg", curi->size, "src");
	break;
    case i_RESET:
//JLH:Not needed	printf ("\tcustomreset();\n");
        insn_n_cycles = 132;    /* I am not so sure about this!? - Thothy */
	break;
    case i_NOP:
	break;
    case i_STOP:
	genamode (curi->smode, "srcreg", curi->size, "src", 1, 0);
	printf ("\tregs.sr = src;\n");
	printf ("\tMakeFromSR();\n");
	printf ("\tm68k_setstopped(1);\n");
        insn_n_cycles = 4;
	break;
    case i_RTE:
	if (cpu_level == 0) {
	    genamode (Aipi, "7", sz_word, "sr", 1, 0);
	    genamode (Aipi, "7", sz_long, "pc", 1, 0);
	    printf ("\tregs.sr = sr; m68k_setpc_rte(pc);\n");
	    fill_prefetch_0 ();
	    printf ("\tMakeFromSR();\n");
	} else {
	    int old_brace_level = n_braces;
	    if (next_cpu_level < 0)
		next_cpu_level = 0;
	    printf ("\tuint16_t newsr; uint32_t newpc; for (;;) {\n");
	    genamode (Aipi, "7", sz_word, "sr", 1, 0);
	    genamode (Aipi, "7", sz_long, "pc", 1, 0);
	    genamode (Aipi, "7", sz_word, "format", 1, 0);
	    printf ("\tnewsr = sr; newpc = pc;\n");
	    printf ("\tif ((format & 0xF000) == 0x0000) { break; }\n");
	    printf ("\telse if ((format & 0xF000) == 0x1000) { ; }\n");
	    printf ("\telse if ((format & 0xF000) == 0x2000) { m68k_areg(regs, 7) += 4; break; }\n");
	    printf ("\telse if ((format & 0xF000) == 0x8000) { m68k_areg(regs, 7) += 50; break; }\n");
	    printf ("\telse if ((format & 0xF000) == 0x9000) { m68k_areg(regs, 7) += 12; break; }\n");
	    printf ("\telse if ((format & 0xF000) == 0xa000) { m68k_areg(regs, 7) += 24; break; }\n");
	    printf ("\telse if ((format & 0xF000) == 0xb000) { m68k_areg(regs, 7) += 84; break; }\n");
	    printf ("\telse { Exception(14,0,M68000_EXC_SRC_CPU); goto %s; }\n", endlabelstr);
	    printf ("\tregs.sr = newsr; MakeFromSR();\n}\n");
	    pop_braces (old_brace_level);
	    printf ("\tregs.sr = newsr; MakeFromSR();\n");
	    printf ("\tm68k_setpc_rte(newpc);\n");
	    fill_prefetch_0 ();
	    need_endlabel = 1;
	}
	/* PC is set and prefetch filled. */
	m68k_pc_offset = 0;
        insn_n_cycles = 20;
	break;
    case i_RTD:
	genamode (Aipi, "7", sz_long, "pc", 1, 0);
	genamode (curi->smode, "srcreg", curi->size, "offs", 1, 0);
	printf ("\tm68k_areg(regs, 7) += offs;\n");
	printf ("\tm68k_setpc_rte(pc);\n");
	fill_prefetch_0 ();
	/* PC is set and prefetch filled. */
	m68k_pc_offset = 0;
	break;
    case i_LINK:
	genamode (Apdi, "7", sz_long, "old", 2, 0);
	genamode (curi->smode, "srcreg", sz_long, "src", 1, 0);
	genastore ("src", Apdi, "7", sz_long, "old");
	genastore ("m68k_areg(regs, 7)", curi->smode, "srcreg", sz_long, "src");
	genamode (curi->dmode, "dstreg", curi->size, "offs", 1, 0);
	printf ("\tm68k_areg(regs, 7) += offs;\n");
	break;
    case i_UNLK:
	genamode (curi->smode, "srcreg", curi->size, "src", 1, 0);
	printf ("\tm68k_areg(regs, 7) = src;\n");
	genamode (Aipi, "7", sz_long, "old", 1, 0);
	genastore ("old", curi->smode, "srcreg", curi->size, "src");
	break;
    case i_RTS:
	printf ("\tm68k_do_rts();\n");
	fill_prefetch_0 ();
	m68k_pc_offset = 0;
        insn_n_cycles = 16;
	break;
    case i_TRAPV:
	sync_m68k_pc ();
	printf ("\tif (GET_VFLG) { Exception(7,m68k_getpc(),M68000_EXC_SRC_CPU); goto %s; }\n", endlabelstr);
	need_endlabel = 1;
	break;
    case i_RTR:
	printf ("\tMakeSR();\n");
	genamode (Aipi, "7", sz_word, "sr", 1, 0);
	genamode (Aipi, "7", sz_long, "pc", 1, 0);
	printf ("\tregs.sr &= 0xFF00; sr &= 0xFF;\n");
	printf ("\tregs.sr |= sr; m68k_setpc(pc);\n");
	fill_prefetch_0 ();
	printf ("\tMakeFromSR();\n");
	m68k_pc_offset = 0;
        insn_n_cycles = 20;
	break;
    case i_JSR:
	genamode (curi->smode, "srcreg", curi->size, "src", 0, 0);
	printf ("\tuint32_t oldpc = m68k_getpc () + %d;\n", m68k_pc_offset);
	if (using_exception_3) {
	    printf ("\tif (srca & 1) {\n");
	    printf ("\t\tlast_addr_for_exception_3 = oldpc;\n");
	    printf ("\t\tlast_fault_for_exception_3 = srca;\n");
	    printf ("\t\tlast_op_for_exception_3 = opcode; Exception(3,0,M68000_EXC_SRC_CPU); goto %s;\n", endlabelstr);
	    printf ("\t}\n");
	    need_endlabel = 1;
	}
	printf ("\tm68k_do_jsr(m68k_getpc() + %d, srca);\n", m68k_pc_offset);
	fill_prefetch_0 ();
	m68k_pc_offset = 0;
        switch(curi->smode)
         {
          case Aind:  insn_n_cycles=16; break;
          case Ad16:  insn_n_cycles=18; break;
          case Ad8r:  insn_n_cycles=22; break;
          case absw:  insn_n_cycles=18; break;
          case absl:  insn_n_cycles=20; break;
          case PC16:  insn_n_cycles=18; break;
          case PC8r:  insn_n_cycles=22; break;
         }
	break;
    case i_JMP:
	genamode (curi->smode, "srcreg", curi->size, "src", 0, 0);
	if (using_exception_3) {
	    printf ("\tif (srca & 1) {\n");
	    printf ("\t\tlast_addr_for_exception_3 = m68k_getpc() + 6;\n");
	    printf ("\t\tlast_fault_for_exception_3 = srca;\n");
	    printf ("\t\tlast_op_for_exception_3 = opcode; Exception(3,0,M68000_EXC_SRC_CPU); goto %s;\n", endlabelstr);
	    printf ("\t}\n");
	    need_endlabel = 1;
	}
	printf ("\tm68k_setpc(srca);\n");
	fill_prefetch_0 ();
	m68k_pc_offset = 0;
        switch(curi->smode)
         {
          case Aind:  insn_n_cycles=8; break;
          case Ad16:  insn_n_cycles=10; break;
          case Ad8r:  insn_n_cycles=14; break;
          case absw:  insn_n_cycles=10; break;
          case absl:  insn_n_cycles=12; break;
          case PC16:  insn_n_cycles=10; break;
          case PC8r:  insn_n_cycles=14; break;
         }
	break;
    case i_BSR:
	genamode (curi->smode, "srcreg", curi->size, "src", 1, 0);
	printf ("\tint32_t s = (int32_t)src + 2;\n");
	if (using_exception_3) {
	    printf ("\tif (src & 1) {\n");
	    printf ("\t\tlast_addr_for_exception_3 = m68k_getpc() + 2;\n");	// [NP] FIXME should be +4, not +2 (same as DBcc) ?
	    printf ("\t\tlast_fault_for_exception_3 = m68k_getpc() + s;\n");
	    printf ("\t\tlast_op_for_exception_3 = opcode; Exception(3,0,M68000_EXC_SRC_CPU); goto %s;\n", endlabelstr);
	    printf ("\t}\n");
	    need_endlabel = 1;
	}
	printf ("\tm68k_do_bsr(m68k_getpc() + %d, s);\n", m68k_pc_offset);
	fill_prefetch_0 ();
	m68k_pc_offset = 0;
        insn_n_cycles = 18;
	break;
    case i_Bcc:
	if (curi->size == sz_long) {
	    if (cpu_level < 2) {
		printf ("\tm68k_incpc(2);\n");
		printf ("\tif (!cctrue(%d)) goto %s;\n", curi->cc, endlabelstr);
		printf ("\t\tlast_addr_for_exception_3 = m68k_getpc() + 2;\n");
		printf ("\t\tlast_fault_for_exception_3 = m68k_getpc() + 1;\n");
		printf ("\t\tlast_op_for_exception_3 = opcode; Exception(3,0,M68000_EXC_SRC_CPU); goto %s;\n", endlabelstr);
		need_endlabel = 1;
	    } else {
		if (next_cpu_level < 1)
		    next_cpu_level = 1;
	    }
	}
	genamode (curi->smode, "srcreg", curi->size, "src", 1, 0);
	printf ("\tif (!cctrue(%d)) goto didnt_jump;\n", curi->cc);
	if (using_exception_3) {
	    printf ("\tif (src & 1) {\n");
	    printf ("\t\tlast_addr_for_exception_3 = m68k_getpc() + 2;\n");	// [NP] FIXME should be +4, not +2 (same as DBcc) ?
	    printf ("\t\tlast_fault_for_exception_3 = m68k_getpc() + 2 + (int32_t)src;\n");
	    printf ("\t\tlast_op_for_exception_3 = opcode; Exception(3,0,M68000_EXC_SRC_CPU); goto %s;\n", endlabelstr);
	    printf ("\t}\n");
	    need_endlabel = 1;
	}
	printf ("\tm68k_incpc ((int32_t)src + 2);\n");
	fill_prefetch_0 ();
	printf ("\treturn 10;\n");
	printf ("didnt_jump:;\n");
	need_endlabel = 1;
	insn_n_cycles = (curi->size == sz_byte) ? 8 : 12;
	break;
    case i_LEA:
	genamode (curi->smode, "srcreg", curi->size, "src", 0, 0);
	genamode (curi->dmode, "dstreg", curi->size, "dst", 2, 0);
	genastore ("srca", curi->dmode, "dstreg", curi->size, "dst");
        /* Set correct cycles: According to the M68K User Manual, LEA takes 12
         * cycles in Ad8r and PC8r mode, but it takes 14 (or 16) cycles on a real ST: */
        if (curi->smode == Ad8r || curi->smode == PC8r)
          insn_n_cycles = 14;
	break;
    case i_PEA:
	genamode (curi->smode, "srcreg", curi->size, "src", 0, 0);
	genamode (Apdi, "7", sz_long, "dst", 2, 0);
	genastore ("srca", Apdi, "7", sz_long, "dst");
	/* Set correct cycles: */
        switch(curi->smode)
         {
          case Aind:  insn_n_cycles=12; break;
          case Ad16:  insn_n_cycles=16; break;
          /* Note: according to the M68K User Manual, PEA takes 20 cycles for
           * the Ad8r mode, but on a real ST, it takes 22 (or 24) cycles! */
          case Ad8r:  insn_n_cycles=22; break;
          case absw:  insn_n_cycles=16; break;
          case absl:  insn_n_cycles=20; break;
          case PC16:  insn_n_cycles=16; break;
          /* Note: PEA with PC8r takes 20 cycles according to the User Manual,
           * but it takes 22 (or 24) cycles on a real ST: */
          case PC8r:  insn_n_cycles=22; break;
         }
	break;
    case i_DBcc:
	genamode (curi->smode, "srcreg", curi->size, "src", 1, 0);
	genamode (curi->dmode, "dstreg", curi->size, "offs", 1, 0);

	printf ("\tif (!cctrue(%d)) {\n\t", curi->cc);
	genastore ("(src-1)", curi->smode, "srcreg", curi->size, "src");

	printf ("\t\tif (src) {\n");
	if (using_exception_3) {
	    printf ("\t\t\tif (offs & 1) {\n");
	    printf ("\t\t\tlast_addr_for_exception_3 = m68k_getpc() + 2 + 2;\n");	// [NP] last_addr is pc+4, not pc+2
	    printf ("\t\t\tlast_fault_for_exception_3 = m68k_getpc() + 2 + (int32_t)offs + 2;\n");
	    printf ("\t\t\tlast_op_for_exception_3 = opcode; Exception(3,0,M68000_EXC_SRC_CPU); goto %s;\n", endlabelstr);
	    printf ("\t\t}\n");
	    need_endlabel = 1;
	}
	printf ("\t\t\tm68k_incpc((int32_t)offs + 2);\n");
	fill_prefetch_0 ();
	printf ("\t\t\treturn 10;\n");
	printf ("\t\t} else {\n\t\t\t");
        {
         int tmp_offset = m68k_pc_offset;
         sync_m68k_pc();              /* not so nice to call it here... */
         m68k_pc_offset = tmp_offset;
        }
        printf ("\t\t\treturn 14;\n");
        printf ("\t\t}\n");
	printf ("\t}\n");
	insn_n_cycles = 12;
	need_endlabel = 1;
	break;
    case i_Scc:
	genamode (curi->smode, "srcreg", curi->size, "src", 2, 0);
	start_brace ();
	printf ("\tint val = cctrue(%d) ? 0xff : 0;\n", curi->cc);
	genastore ("val", curi->smode, "srcreg", curi->size, "src");
        if (curi->smode!=Dreg)  insn_n_cycles += 4;
	else
	  {					/* [NP] if result is TRUE, we return 6 instead of 4 */
	    printf ("\tif (val) { m68k_incpc(2) ; return 4+2; }\n");
	  }
	break;
    case i_DIVU:
	printf ("\tuint32_t oldpc = m68k_getpc();\n");
	genamode (curi->smode, "srcreg", sz_word, "src", 1, 0);
	genamode (curi->dmode, "dstreg", sz_long, "dst", 1, 0);
	sync_m68k_pc ();
	/* Clear V flag when dividing by zero - Alcatraz Odyssey demo depends
	 * on this (actually, it's doing a DIVS).  */
	printf ("\tif (src == 0) { SET_VFLG (0); Exception (5, oldpc,M68000_EXC_SRC_CPU); goto %s; } else {\n", endlabelstr);
	printf ("\tuint32_t newv = (uint32_t)dst / (uint32_t)(uint16_t)src;\n");
	printf ("\tuint32_t rem = (uint32_t)dst %% (uint32_t)(uint16_t)src;\n");
	/* The N flag appears to be set each time there is an overflow.
	 * Weird. */
	printf ("\tif (newv > 0xffff) { SET_VFLG (1); SET_NFLG (1); SET_CFLG (0); } else\n\t{\n");
	genflags (flag_logical, sz_word, "newv", "", "");
	printf ("\tnewv = (newv & 0xffff) | ((uint32_t)rem << 16);\n");
	genastore ("newv", curi->dmode, "dstreg", sz_long, "dst");
	printf ("\t}\n");
	printf ("\t}\n");
//	insn_n_cycles += 136;
	printf ("\tretcycles = getDivu68kCycles((uint32_t)dst, (uint16_t)src);\n");
        sprintf(exactCpuCycles," return (%i+retcycles);", insn_n_cycles);
	need_endlabel = 1;
	break;
    case i_DIVS:
	printf ("\tuint32_t oldpc = m68k_getpc();\n");
	genamode (curi->smode, "srcreg", sz_word, "src", 1, 0);
	genamode (curi->dmode, "dstreg", sz_long, "dst", 1, 0);
	sync_m68k_pc ();
	printf ("\tif (src == 0) { SET_VFLG (0); Exception(5,oldpc,M68000_EXC_SRC_CPU); goto %s; } else {\n", endlabelstr);
	printf ("\tint32_t newv = (int32_t)dst / (int32_t)(int16_t)src;\n");
	printf ("\tuint16_t rem = (int32_t)dst %% (int32_t)(int16_t)src;\n");
	printf ("\tif ((newv & 0xffff8000) != 0 && (newv & 0xffff8000) != 0xffff8000) { SET_VFLG (1); SET_NFLG (1); SET_CFLG (0); } else\n\t{\n");
	printf ("\tif (((int16_t)rem < 0) != ((int32_t)dst < 0)) rem = -rem;\n");
	genflags (flag_logical, sz_word, "newv", "", "");
	printf ("\tnewv = (newv & 0xffff) | ((uint32_t)rem << 16);\n");
	genastore ("newv", curi->dmode, "dstreg", sz_long, "dst");
	printf ("\t}\n");
	printf ("\t}\n");
//	insn_n_cycles += 154;
	printf ("\tretcycles = getDivs68kCycles((int32_t)dst, (int16_t)src);\n");
        sprintf(exactCpuCycles," return (%i+retcycles);", insn_n_cycles);
	need_endlabel = 1;
	break;
    case i_MULU:
	genamode (curi->smode, "srcreg", sz_word, "src", 1, 0);
	genamode (curi->dmode, "dstreg", sz_word, "dst", 1, 0);
	start_brace ();
	printf ("\tuint32_t newv = (uint32_t)(uint16_t)dst * (uint32_t)(uint16_t)src;\n");
	genflags (flag_logical, sz_long, "newv", "", "");
	genastore ("newv", curi->dmode, "dstreg", sz_long, "dst");
	/* [NP] number of cycles is 38 + 2n + ea time ; n is the number of 1 bits in src */
	insn_n_cycles += 38-4;			/* insn_n_cycles is already initialized to 4 instead of 0 */
	printf ("\twhile (src) { if (src & 1) retcycles++; src = (uint16_t)src >> 1; }\n");
        sprintf(exactCpuCycles," return (%i+retcycles*2);", insn_n_cycles);
	break;
    case i_MULS:
	genamode (curi->smode, "srcreg", sz_word, "src", 1, 0);
	genamode (curi->dmode, "dstreg", sz_word, "dst", 1, 0);
	start_brace ();
	printf ("\tuint32_t newv = (int32_t)(int16_t)dst * (int32_t)(int16_t)src;\n");
	printf ("\tuint32_t src2;\n");
	genflags (flag_logical, sz_long, "newv", "", "");
	genastore ("newv", curi->dmode, "dstreg", sz_long, "dst");
	/* [NP] number of cycles is 38 + 2n + ea time ; n is the number of 01 or 10 patterns in src expanded to 17 bits */
	insn_n_cycles += 38-4;			/* insn_n_cycles is already initialized to 4 instead of 0 */
	printf ("\tsrc2 = ((uint32_t)src) << 1;\n");
	printf ("\twhile (src2) { if ( ( (src2 & 3) == 1 ) || ( (src2 & 3) == 2 ) ) retcycles++; src2 >>= 1; }\n");
        sprintf(exactCpuCycles," return (%i+retcycles*2);", insn_n_cycles);
	break;
    case i_CHK:
	printf ("\tuint32_t oldpc = m68k_getpc();\n");
	genamode (curi->smode, "srcreg", curi->size, "src", 1, 0);
	genamode (curi->dmode, "dstreg", curi->size, "dst", 1, 0);
	sync_m68k_pc ();
	printf ("\tif ((int32_t)dst < 0) { SET_NFLG (1); Exception(6,oldpc,M68000_EXC_SRC_CPU); goto %s; }\n", endlabelstr);
	printf ("\telse if (dst > src) { SET_NFLG (0); Exception(6,oldpc,M68000_EXC_SRC_CPU); goto %s; }\n", endlabelstr);
	need_endlabel = 1;
        insn_n_cycles += 6;
	break;

    case i_CHK2:
	printf ("\tuint32_t oldpc = m68k_getpc();\n");
	genamode (curi->smode, "srcreg", curi->size, "extra", 1, 0);
	genamode (curi->dmode, "dstreg", curi->size, "dst", 2, 0);
	printf ("\t{int32_t upper,lower,reg = regs.regs[(extra >> 12) & 15];\n");
	switch (curi->size) {
	case sz_byte:
	    printf ("\tlower=(int32_t)(int8_t)m68k_read_memory_8(dsta); upper = (int32_t)(int8_t)m68k_read_memory_8(dsta+1);\n");
	    printf ("\tif ((extra & 0x8000) == 0) reg = (int32_t)(int8_t)reg;\n");
	    break;
	case sz_word:
	    printf ("\tlower=(int32_t)(int16_t)m68k_read_memory_16(dsta); upper = (int32_t)(int16_t)m68k_read_memory_16(dsta+2);\n");
	    printf ("\tif ((extra & 0x8000) == 0) reg = (int32_t)(int16_t)reg;\n");
	    break;
	case sz_long:
	    printf ("\tlower=m68k_read_memory_32(dsta); upper = m68k_read_memory_32(dsta+4);\n");
	    break;
	default:
	    abort ();
	}
	printf ("\tSET_ZFLG (upper == reg || lower == reg);\n");
	printf ("\tSET_CFLG (lower <= upper ? reg < lower || reg > upper : reg > upper || reg < lower);\n");
	sync_m68k_pc ();
	printf ("\tif ((extra & 0x800) && GET_CFLG) { Exception(6,oldpc,M68000_EXC_SRC_CPU); goto %s; }\n}\n", endlabelstr);
	need_endlabel = 1;
	break;

    case i_ASR:
	genamode (curi->smode, "srcreg", curi->size, "cnt", 1, 0);
	genamode (curi->dmode, "dstreg", curi->size, "data", 1, 0);
	start_brace ();
	switch (curi->size) {
	case sz_byte: printf ("\tuint32_t val = (uint8_t)data;\n"); break;
	case sz_word: printf ("\tuint32_t val = (uint16_t)data;\n"); break;
	case sz_long: printf ("\tuint32_t val = data;\n"); break;
	default: abort ();
	}
	printf ("\tuint32_t sign = (%s & val) >> %d;\n", cmask (curi->size), bit_size (curi->size) - 1);
	printf ("\tcnt &= 63;\n");
        printf ("\tretcycles = cnt;\n");
	printf ("\tCLEAR_CZNV;\n");
	printf ("\tif (cnt >= %d) {\n", bit_size (curi->size));
	printf ("\t\tval = %s & (uint32_t)-sign;\n", bit_mask (curi->size));
	printf ("\t\tSET_CFLG (sign);\n");
	duplicate_carry ();
	if (source_is_imm1_8 (curi))
	    printf ("\t} else {\n");
	else
	    printf ("\t} else if (cnt > 0) {\n");
	printf ("\t\tval >>= cnt - 1;\n");
	printf ("\t\tSET_CFLG (val & 1);\n");
	duplicate_carry ();
	printf ("\t\tval >>= 1;\n");
	printf ("\t\tval |= (%s << (%d - cnt)) & (uint32_t)-sign;\n",
		bit_mask (curi->size),
		bit_size (curi->size));
	printf ("\t\tval &= %s;\n", bit_mask (curi->size));
	printf ("\t}\n");
	genflags (flag_logical_noclobber, curi->size, "val", "", "");
	genastore ("val", curi->dmode, "dstreg", curi->size, "data");
        if(curi->size==sz_long)
            strcpy(exactCpuCycles," return (8+retcycles*2);");
          else
            strcpy(exactCpuCycles," return (6+retcycles*2);");
	break;
    case i_ASL:
	genamode (curi->smode, "srcreg", curi->size, "cnt", 1, 0);
	genamode (curi->dmode, "dstreg", curi->size, "data", 1, 0);
	start_brace ();
	switch (curi->size) {
	case sz_byte: printf ("\tuint32_t val = (uint8_t)data;\n"); break;
	case sz_word: printf ("\tuint32_t val = (uint16_t)data;\n"); break;
	case sz_long: printf ("\tuint32_t val = data;\n"); break;
	default: abort ();
	}
	printf ("\tcnt &= 63;\n");
        printf ("\tretcycles = cnt;\n");
	printf ("\tCLEAR_CZNV;\n");
	printf ("\tif (cnt >= %d) {\n", bit_size (curi->size));
	printf ("\t\tSET_VFLG (val != 0);\n");
	printf ("\t\tSET_CFLG (cnt == %d ? val & 1 : 0);\n",
		bit_size (curi->size));
	duplicate_carry ();
	printf ("\t\tval = 0;\n");
	if (source_is_imm1_8 (curi))
	    printf ("\t} else {\n");
	else
	    printf ("\t} else if (cnt > 0) {\n");
	printf ("\t\tuint32_t mask = (%s << (%d - cnt)) & %s;\n",
		bit_mask (curi->size),
		bit_size (curi->size) - 1,
		bit_mask (curi->size));
	printf ("\t\tSET_VFLG ((val & mask) != mask && (val & mask) != 0);\n");
	printf ("\t\tval <<= cnt - 1;\n");
	printf ("\t\tSET_CFLG ((val & %s) >> %d);\n", cmask (curi->size), bit_size (curi->size) - 1);
	duplicate_carry ();
	printf ("\t\tval <<= 1;\n");
	printf ("\t\tval &= %s;\n", bit_mask (curi->size));
	printf ("\t}\n");
	genflags (flag_logical_noclobber, curi->size, "val", "", "");
	genastore ("val", curi->dmode, "dstreg", curi->size, "data");
        if(curi->size==sz_long)
            strcpy(exactCpuCycles," return (8+retcycles*2);");
          else
            strcpy(exactCpuCycles," return (6+retcycles*2);");
	break;
    case i_LSR:
	genamode (curi->smode, "srcreg", curi->size, "cnt", 1, 0);
	genamode (curi->dmode, "dstreg", curi->size, "data", 1, 0);
	start_brace ();
	switch (curi->size) {
	case sz_byte: printf ("\tuint32_t val = (uint8_t)data;\n"); break;
	case sz_word: printf ("\tuint32_t val = (uint16_t)data;\n"); break;
	case sz_long: printf ("\tuint32_t val = data;\n"); break;
	default: abort ();
	}
	printf ("\tcnt &= 63;\n");
        printf ("\tretcycles = cnt;\n");
	printf ("\tCLEAR_CZNV;\n");
	printf ("\tif (cnt >= %d) {\n", bit_size (curi->size));
	printf ("\t\tSET_CFLG ((cnt == %d) & (val >> %d));\n",
		bit_size (curi->size), bit_size (curi->size) - 1);
	duplicate_carry ();
	printf ("\t\tval = 0;\n");
	if (source_is_imm1_8 (curi))
	    printf ("\t} else {\n");
	else
	    printf ("\t} else if (cnt > 0) {\n");
	printf ("\t\tval >>= cnt - 1;\n");
	printf ("\t\tSET_CFLG (val & 1);\n");
	duplicate_carry ();
	printf ("\t\tval >>= 1;\n");
	printf ("\t}\n");
	genflags (flag_logical_noclobber, curi->size, "val", "", "");
	genastore ("val", curi->dmode, "dstreg", curi->size, "data");
        if(curi->size==sz_long)
            strcpy(exactCpuCycles," return (8+retcycles*2);");
          else
            strcpy(exactCpuCycles," return (6+retcycles*2);");
	break;
    case i_LSL:
	genamode (curi->smode, "srcreg", curi->size, "cnt", 1, 0);
	genamode (curi->dmode, "dstreg", curi->size, "data", 1, 0);
	start_brace ();
	switch (curi->size) {
	case sz_byte: printf ("\tuint32_t val = (uint8_t)data;\n"); break;
	case sz_word: printf ("\tuint32_t val = (uint16_t)data;\n"); break;
	case sz_long: printf ("\tuint32_t val = data;\n"); break;
	default: abort ();
	}
	printf ("\tcnt &= 63;\n");
        printf ("\tretcycles = cnt;\n");
	printf ("\tCLEAR_CZNV;\n");
	printf ("\tif (cnt >= %d) {\n", bit_size (curi->size));
	printf ("\t\tSET_CFLG (cnt == %d ? val & 1 : 0);\n",
		bit_size (curi->size));
	duplicate_carry ();
	printf ("\t\tval = 0;\n");
	if (source_is_imm1_8 (curi))
	    printf ("\t} else {\n");
	else
	    printf ("\t} else if (cnt > 0) {\n");
	printf ("\t\tval <<= (cnt - 1);\n");
	printf ("\t\tSET_CFLG ((val & %s) >> %d);\n", cmask (curi->size), bit_size (curi->size) - 1);
	duplicate_carry ();
	printf ("\t\tval <<= 1;\n");
	printf ("\tval &= %s;\n", bit_mask (curi->size));
	printf ("\t}\n");
	genflags (flag_logical_noclobber, curi->size, "val", "", "");
	genastore ("val", curi->dmode, "dstreg", curi->size, "data");
        if(curi->size==sz_long)
            strcpy(exactCpuCycles," return (8+retcycles*2);");
          else
            strcpy(exactCpuCycles," return (6+retcycles*2);");
	break;
    case i_ROL:
	genamode (curi->smode, "srcreg", curi->size, "cnt", 1, 0);
	genamode (curi->dmode, "dstreg", curi->size, "data", 1, 0);
	start_brace ();
	switch (curi->size) {
	case sz_byte: printf ("\tuint32_t val = (uint8_t)data;\n"); break;
	case sz_word: printf ("\tuint32_t val = (uint16_t)data;\n"); break;
	case sz_long: printf ("\tuint32_t val = data;\n"); break;
	default: abort ();
	}
	printf ("\tcnt &= 63;\n");
        printf ("\tretcycles = cnt;\n");
	printf ("\tCLEAR_CZNV;\n");
	if (source_is_imm1_8 (curi))
	    printf ("{");
	else
	    printf ("\tif (cnt > 0) {\n");
	printf ("\tuint32_t loval;\n");
	printf ("\tcnt &= %d;\n", bit_size (curi->size) - 1);
	printf ("\tloval = val >> (%d - cnt);\n", bit_size (curi->size));
	printf ("\tval <<= cnt;\n");
	printf ("\tval |= loval;\n");
	printf ("\tval &= %s;\n", bit_mask (curi->size));
	printf ("\tSET_CFLG (val & 1);\n");
	printf ("}\n");
	genflags (flag_logical_noclobber, curi->size, "val", "", "");
	genastore ("val", curi->dmode, "dstreg", curi->size, "data");
        if(curi->size==sz_long)
            strcpy(exactCpuCycles," return (8+retcycles*2);");
          else
            strcpy(exactCpuCycles," return (6+retcycles*2);");
	break;
    case i_ROR:
	genamode (curi->smode, "srcreg", curi->size, "cnt", 1, 0);
	genamode (curi->dmode, "dstreg", curi->size, "data", 1, 0);
	start_brace ();
	switch (curi->size) {
	case sz_byte: printf ("\tuint32_t val = (uint8_t)data;\n"); break;
	case sz_word: printf ("\tuint32_t val = (uint16_t)data;\n"); break;
	case sz_long: printf ("\tuint32_t val = data;\n"); break;
	default: abort ();
	}
	printf ("\tcnt &= 63;\n");
        printf ("\tretcycles = cnt;\n");
	printf ("\tCLEAR_CZNV;\n");
	if (source_is_imm1_8 (curi))
	    printf ("{");
	else
	    printf ("\tif (cnt > 0) {");
	printf ("\tuint32_t hival;\n");
	printf ("\tcnt &= %d;\n", bit_size (curi->size) - 1);
	printf ("\thival = val << (%d - cnt);\n", bit_size (curi->size));
	printf ("\tval >>= cnt;\n");
	printf ("\tval |= hival;\n");
	printf ("\tval &= %s;\n", bit_mask (curi->size));
	printf ("\tSET_CFLG ((val & %s) >> %d);\n", cmask (curi->size), bit_size (curi->size) - 1);
	printf ("\t}\n");
	genflags (flag_logical_noclobber, curi->size, "val", "", "");
	genastore ("val", curi->dmode, "dstreg", curi->size, "data");
        if(curi->size==sz_long)
            strcpy(exactCpuCycles," return (8+retcycles*2);");
          else
            strcpy(exactCpuCycles," return (6+retcycles*2);");
	break;
    case i_ROXL:
	genamode (curi->smode, "srcreg", curi->size, "cnt", 1, 0);
	genamode (curi->dmode, "dstreg", curi->size, "data", 1, 0);
	start_brace ();
	switch (curi->size) {
	case sz_byte: printf ("\tuint32_t val = (uint8_t)data;\n"); break;
	case sz_word: printf ("\tuint32_t val = (uint16_t)data;\n"); break;
	case sz_long: printf ("\tuint32_t val = data;\n"); break;
	default: abort ();
	}
	printf ("\tcnt &= 63;\n");
        printf ("\tretcycles = cnt;\n");
	printf ("\tCLEAR_CZNV;\n");
	if (source_is_imm1_8 (curi))
	    printf ("{");
	else {
	    force_range_for_rox ("cnt", curi->size);
	    printf ("\tif (cnt > 0) {\n");
	}
	printf ("\tcnt--;\n");
	printf ("\t{\n\tuint32_t carry;\n");
	printf ("\tuint32_t loval = val >> (%d - cnt);\n", bit_size (curi->size) - 1);
	printf ("\tcarry = loval & 1;\n");
	printf ("\tval = (((val << 1) | GET_XFLG) << cnt) | (loval >> 1);\n");
	printf ("\tSET_XFLG (carry);\n");
	printf ("\tval &= %s;\n", bit_mask (curi->size));
	printf ("\t} }\n");
	printf ("\tSET_CFLG (GET_XFLG);\n");
	genflags (flag_logical_noclobber, curi->size, "val", "", "");
	genastore ("val", curi->dmode, "dstreg", curi->size, "data");
        if(curi->size==sz_long)
            strcpy(exactCpuCycles," return (8+retcycles*2);");
          else
            strcpy(exactCpuCycles," return (6+retcycles*2);");
	break;
    case i_ROXR:
	genamode (curi->smode, "srcreg", curi->size, "cnt", 1, 0);
	genamode (curi->dmode, "dstreg", curi->size, "data", 1, 0);
	start_brace ();
	switch (curi->size) {
	case sz_byte: printf ("\tuint32_t val = (uint8_t)data;\n"); break;
	case sz_word: printf ("\tuint32_t val = (uint16_t)data;\n"); break;
	case sz_long: printf ("\tuint32_t val = data;\n"); break;
	default: abort ();
	}
	printf ("\tcnt &= 63;\n");
        printf ("\tretcycles = cnt;\n");
	printf ("\tCLEAR_CZNV;\n");
	if (source_is_imm1_8 (curi))
	    printf ("{");
	else {
	    force_range_for_rox ("cnt", curi->size);
	    printf ("\tif (cnt > 0) {\n");
	}
	printf ("\tcnt--;\n");
	printf ("\t{\n\tuint32_t carry;\n");
	printf ("\tuint32_t hival = (val << 1) | GET_XFLG;\n");
	printf ("\thival <<= (%d - cnt);\n", bit_size (curi->size) - 1);
	printf ("\tval >>= cnt;\n");
	printf ("\tcarry = val & 1;\n");
	printf ("\tval >>= 1;\n");
	printf ("\tval |= hival;\n");
	printf ("\tSET_XFLG (carry);\n");
	printf ("\tval &= %s;\n", bit_mask (curi->size));
	printf ("\t} }\n");
	printf ("\tSET_CFLG (GET_XFLG);\n");
	genflags (flag_logical_noclobber, curi->size, "val", "", "");
	genastore ("val", curi->dmode, "dstreg", curi->size, "data");
        if(curi->size==sz_long)
            strcpy(exactCpuCycles," return (8+retcycles*2);");
          else
            strcpy(exactCpuCycles," return (6+retcycles*2);");
	break;
    case i_ASRW:
	genamode (curi->smode, "srcreg", curi->size, "data", 1, 0);
	start_brace ();
	switch (curi->size) {
	case sz_byte: printf ("\tuint32_t val = (uint8_t)data;\n"); break;
	case sz_word: printf ("\tuint32_t val = (uint16_t)data;\n"); break;
	case sz_long: printf ("\tuint32_t val = data;\n"); break;
	default: abort ();
	}
	printf ("\tuint32_t sign = %s & val;\n", cmask (curi->size));
	printf ("\tuint32_t cflg = val & 1;\n");
	printf ("\tval = (val >> 1) | sign;\n");
	genflags (flag_logical, curi->size, "val", "", "");
	printf ("\tSET_CFLG (cflg);\n");
	duplicate_carry ();
	genastore ("val", curi->smode, "srcreg", curi->size, "data");
	break;
    case i_ASLW:
	genamode (curi->smode, "srcreg", curi->size, "data", 1, 0);
	start_brace ();
	switch (curi->size) {
	case sz_byte: printf ("\tuint32_t val = (uint8_t)data;\n"); break;
	case sz_word: printf ("\tuint32_t val = (uint16_t)data;\n"); break;
	case sz_long: printf ("\tuint32_t val = data;\n"); break;
	default: abort ();
	}
	printf ("\tuint32_t sign = %s & val;\n", cmask (curi->size));
	printf ("\tuint32_t sign2;\n");
	printf ("\tval <<= 1;\n");
	genflags (flag_logical, curi->size, "val", "", "");
	printf ("\tsign2 = %s & val;\n", cmask (curi->size));
	printf ("\tSET_CFLG (sign != 0);\n");
	duplicate_carry ();

	printf ("\tSET_VFLG (GET_VFLG | (sign2 != sign));\n");
	genastore ("val", curi->smode, "srcreg", curi->size, "data");
	break;
    case i_LSRW:
	genamode (curi->smode, "srcreg", curi->size, "data", 1, 0);
	start_brace ();
	switch (curi->size) {
	case sz_byte: printf ("\tuint32_t val = (uint8_t)data;\n"); break;
	case sz_word: printf ("\tuint32_t val = (uint16_t)data;\n"); break;
	case sz_long: printf ("\tuint32_t val = data;\n"); break;
	default: abort ();
	}
	printf ("\tuint32_t carry = val & 1;\n");
	printf ("\tval >>= 1;\n");
	genflags (flag_logical, curi->size, "val", "", "");
	printf ("SET_CFLG (carry);\n");
	duplicate_carry ();
	genastore ("val", curi->smode, "srcreg", curi->size, "data");
	break;
    case i_LSLW:
	genamode (curi->smode, "srcreg", curi->size, "data", 1, 0);
	start_brace ();
	switch (curi->size) {
	case sz_byte: printf ("\tuint8_t val = data;\n"); break;
	case sz_word: printf ("\tuint16_t val = data;\n"); break;
	case sz_long: printf ("\tuint32_t val = data;\n"); break;
	default: abort ();
	}
	printf ("\tuint32_t carry = val & %s;\n", cmask (curi->size));
	printf ("\tval <<= 1;\n");
	genflags (flag_logical, curi->size, "val", "", "");
	printf ("SET_CFLG (carry >> %d);\n", bit_size (curi->size) - 1);
	duplicate_carry ();
	genastore ("val", curi->smode, "srcreg", curi->size, "data");
	break;
    case i_ROLW:
	genamode (curi->smode, "srcreg", curi->size, "data", 1, 0);
	start_brace ();
	switch (curi->size) {
	case sz_byte: printf ("\tuint8_t val = data;\n"); break;
	case sz_word: printf ("\tuint16_t val = data;\n"); break;
	case sz_long: printf ("\tuint32_t val = data;\n"); break;
	default: abort ();
	}
	printf ("\tuint32_t carry = val & %s;\n", cmask (curi->size));
	printf ("\tval <<= 1;\n");
	printf ("\tif (carry)  val |= 1;\n");
	genflags (flag_logical, curi->size, "val", "", "");
	printf ("SET_CFLG (carry >> %d);\n", bit_size (curi->size) - 1);
	genastore ("val", curi->smode, "srcreg", curi->size, "data");
	break;
    case i_RORW:
	genamode (curi->smode, "srcreg", curi->size, "data", 1, 0);
	start_brace ();
	switch (curi->size) {
	case sz_byte: printf ("\tuint8_t val = data;\n"); break;
	case sz_word: printf ("\tuint16_t val = data;\n"); break;
	case sz_long: printf ("\tuint32_t val = data;\n"); break;
	default: abort ();
	}
	printf ("\tuint32_t carry = val & 1;\n");
	printf ("\tval >>= 1;\n");
	printf ("\tif (carry) val |= %s;\n", cmask (curi->size));
	genflags (flag_logical, curi->size, "val", "", "");
	printf ("SET_CFLG (carry);\n");
	genastore ("val", curi->smode, "srcreg", curi->size, "data");
	break;
    case i_ROXLW:
	genamode (curi->smode, "srcreg", curi->size, "data", 1, 0);
	start_brace ();
	switch (curi->size) {
	case sz_byte: printf ("\tuint8_t val = data;\n"); break;
	case sz_word: printf ("\tuint16_t val = data;\n"); break;
	case sz_long: printf ("\tuint32_t val = data;\n"); break;
	default: abort ();
	}
	printf ("\tuint32_t carry = val & %s;\n", cmask (curi->size));
	printf ("\tval <<= 1;\n");
	printf ("\tif (GET_XFLG) val |= 1;\n");
	genflags (flag_logical, curi->size, "val", "", "");
	printf ("SET_CFLG (carry >> %d);\n", bit_size (curi->size) - 1);
	duplicate_carry ();
	genastore ("val", curi->smode, "srcreg", curi->size, "data");
	break;
    case i_ROXRW:
	genamode (curi->smode, "srcreg", curi->size, "data", 1, 0);
	start_brace ();
	switch (curi->size) {
	case sz_byte: printf ("\tuint8_t val = data;\n"); break;
	case sz_word: printf ("\tuint16_t val = data;\n"); break;
	case sz_long: printf ("\tuint32_t val = data;\n"); break;
	default: abort ();
	}
	printf ("\tuint32_t carry = val & 1;\n");
	printf ("\tval >>= 1;\n");
	printf ("\tif (GET_XFLG) val |= %s;\n", cmask (curi->size));
	genflags (flag_logical, curi->size, "val", "", "");
	printf ("SET_CFLG (carry);\n");
	duplicate_carry ();
	genastore ("val", curi->smode, "srcreg", curi->size, "data");
	break;
    case i_MOVEC2:
	genamode (curi->smode, "srcreg", curi->size, "src", 1, 0);
	start_brace ();
	printf ("\tint regno = (src >> 12) & 15;\n");
	printf ("\tuint32_t *regp = regs.regs + regno;\n");
	printf ("\tif (! m68k_movec2(src & 0xFFF, regp)) goto %s;\n", endlabelstr);
	break;
    case i_MOVE2C:
	genamode (curi->smode, "srcreg", curi->size, "src", 1, 0);
	start_brace ();
	printf ("\tint regno = (src >> 12) & 15;\n");
	printf ("\tuint32_t *regp = regs.regs + regno;\n");
	printf ("\tif (! m68k_move2c(src & 0xFFF, regp)) goto %s;\n", endlabelstr);
	break;
    case i_CAS:
    {
	int old_brace_level;
	genamode (curi->smode, "srcreg", curi->size, "src", 1, 0);
	genamode (curi->dmode, "dstreg", curi->size, "dst", 1, 0);
	start_brace ();
	printf ("\tint ru = (src >> 6) & 7;\n");
	printf ("\tint rc = src & 7;\n");
	genflags (flag_cmp, curi->size, "newv", "m68k_dreg(regs, rc)", "dst");
	printf ("\tif (GET_ZFLG)");
	old_brace_level = n_braces;
	start_brace ();
	genastore ("(m68k_dreg(regs, ru))", curi->dmode, "dstreg", curi->size, "dst");
	pop_braces (old_brace_level);
	printf ("else");
	start_brace ();
	printf ("m68k_dreg(regs, rc) = dst;\n");
	pop_braces (old_brace_level);
    }
    break;
    case i_CAS2:
	genamode (curi->smode, "srcreg", curi->size, "extra", 1, 0);
	printf ("\tuint32_t rn1 = regs.regs[(extra >> 28) & 15];\n");
	printf ("\tuint32_t rn2 = regs.regs[(extra >> 12) & 15];\n");
	if (curi->size == sz_word) {
	    int old_brace_level = n_braces;
	    printf ("\tuint16_t dst1 = m68k_read_memory_16(rn1), dst2 = m68k_read_memory_16(rn2);\n");
	    genflags (flag_cmp, curi->size, "newv", "m68k_dreg(regs, (extra >> 16) & 7)", "dst1");
	    printf ("\tif (GET_ZFLG) {\n");
	    genflags (flag_cmp, curi->size, "newv", "m68k_dreg(regs, extra & 7)", "dst2");
	    printf ("\tif (GET_ZFLG) {\n");
	    printf ("\tm68k_write_memory_16(rn1, m68k_dreg(regs, (extra >> 22) & 7));\n");
	    printf ("\tm68k_write_memory_16(rn1, m68k_dreg(regs, (extra >> 6) & 7));\n");
	    printf ("\t}}\n");
	    pop_braces (old_brace_level);
	    printf ("\tif (! GET_ZFLG) {\n");
	    printf ("\tm68k_dreg(regs, (extra >> 22) & 7) = (m68k_dreg(regs, (extra >> 22) & 7) & ~0xffff) | (dst1 & 0xffff);\n");
	    printf ("\tm68k_dreg(regs, (extra >> 6) & 7) = (m68k_dreg(regs, (extra >> 6) & 7) & ~0xffff) | (dst2 & 0xffff);\n");
	    printf ("\t}\n");
	} else {
	    int old_brace_level = n_braces;
	    printf ("\tuint32_t dst1 = m68k_read_memory_32(rn1), dst2 = m68k_read_memory_32(rn2);\n");
	    genflags (flag_cmp, curi->size, "newv", "m68k_dreg(regs, (extra >> 16) & 7)", "dst1");
	    printf ("\tif (GET_ZFLG) {\n");
	    genflags (flag_cmp, curi->size, "newv", "m68k_dreg(regs, extra & 7)", "dst2");
	    printf ("\tif (GET_ZFLG) {\n");
	    printf ("\tm68k_write_memory_32(rn1, m68k_dreg(regs, (extra >> 22) & 7));\n");
	    printf ("\tm68k_write_memory_32(rn1, m68k_dreg(regs, (extra >> 6) & 7));\n");
	    printf ("\t}}\n");
	    pop_braces (old_brace_level);
	    printf ("\tif (! GET_ZFLG) {\n");
	    printf ("\tm68k_dreg(regs, (extra >> 22) & 7) = dst1;\n");
	    printf ("\tm68k_dreg(regs, (extra >> 6) & 7) = dst2;\n");
	    printf ("\t}\n");
	}
	break;
    case i_MOVES:		/* ignore DFC and SFC because we have no MMU */
    {
	int old_brace_level;
	genamode (curi->smode, "srcreg", curi->size, "extra", 1, 0);
	printf ("\tif (extra & 0x800)\n");
	old_brace_level = n_braces;
	start_brace ();
	printf ("\tuint32_t src = regs.regs[(extra >> 12) & 15];\n");
	genamode (curi->dmode, "dstreg", curi->size, "dst", 2, 0);
	genastore ("src", curi->dmode, "dstreg", curi->size, "dst");
	pop_braces (old_brace_level);
	printf ("else");
	start_brace ();
	genamode (curi->dmode, "dstreg", curi->size, "src", 1, 0);
	printf ("\tif (extra & 0x8000) {\n");
	switch (curi->size) {
	case sz_byte: printf ("\tm68k_areg(regs, (extra >> 12) & 7) = (int32_t)(int8_t)src;\n"); break;
	case sz_word: printf ("\tm68k_areg(regs, (extra >> 12) & 7) = (int32_t)(int16_t)src;\n"); break;
	case sz_long: printf ("\tm68k_areg(regs, (extra >> 12) & 7) = src;\n"); break;
	default: abort ();
	}
	printf ("\t} else {\n");
	genastore ("src", Dreg, "(extra >> 12) & 7", curi->size, "");
	printf ("\t}\n");
	pop_braces (old_brace_level);
    }
    break;
    case i_BKPT:		/* only needed for hardware emulators */
	sync_m68k_pc ();
	printf ("\top_illg(opcode);\n");
	break;
    case i_CALLM:		/* not present in 68030 */
	sync_m68k_pc ();
	printf ("\top_illg(opcode);\n");
	break;
    case i_RTM:		/* not present in 68030 */
	sync_m68k_pc ();
	printf ("\top_illg(opcode);\n");
	break;
    case i_TRAPcc:
	if (curi->smode != am_unknown && curi->smode != am_illg)
	    genamode (curi->smode, "srcreg", curi->size, "dummy", 1, 0);
	printf ("\tif (cctrue(%d)) { Exception(7,m68k_getpc(),M68000_EXC_SRC_CPU); goto %s; }\n", curi->cc, endlabelstr);
	need_endlabel = 1;
	break;
    case i_DIVL:
	sync_m68k_pc ();
	start_brace ();
	printf ("\tuint32_t oldpc = m68k_getpc();\n");
	genamode (curi->smode, "srcreg", curi->size, "extra", 1, 0);
	genamode (curi->dmode, "dstreg", curi->size, "dst", 1, 0);
	sync_m68k_pc ();
	printf ("\tm68k_divl(opcode, dst, extra, oldpc);\n");
	break;
    case i_MULL:
	genamode (curi->smode, "srcreg", curi->size, "extra", 1, 0);
	genamode (curi->dmode, "dstreg", curi->size, "dst", 1, 0);
	sync_m68k_pc ();
	printf ("\tm68k_mull(opcode, dst, extra);\n");
	break;
    case i_BFTST:
    case i_BFEXTU:
    case i_BFCHG:
    case i_BFEXTS:
    case i_BFCLR:
    case i_BFFFO:
    case i_BFSET:
    case i_BFINS:
	genamode (curi->smode, "srcreg", curi->size, "extra", 1, 0);
	genamode (curi->dmode, "dstreg", sz_long, "dst", 2, 0);
	start_brace ();
	printf ("\tint32_t offset = extra & 0x800 ? m68k_dreg(regs, (extra >> 6) & 7) : (extra >> 6) & 0x1f;\n");
	printf ("\tint width = (((extra & 0x20 ? m68k_dreg(regs, extra & 7) : extra) -1) & 0x1f) +1;\n");
	if (curi->dmode == Dreg) {
	    printf ("\tuint32_t tmp = m68k_dreg(regs, dstreg) << (offset & 0x1f);\n");
	} else {
	    printf ("\tuint32_t tmp,bf0,bf1;\n");
	    printf ("\tdsta += (offset >> 3) | (offset & 0x80000000 ? ~0x1fffffff : 0);\n");
	    printf ("\tbf0 = m68k_read_memory_32(dsta);bf1 = m68k_read_memory_8(dsta+4) & 0xff;\n");
	    printf ("\ttmp = (bf0 << (offset & 7)) | (bf1 >> (8 - (offset & 7)));\n");
	}
	printf ("\ttmp >>= (32 - width);\n");
	printf ("\tSET_NFLG (tmp & (1 << (width-1)) ? 1 : 0);\n");
	printf ("\tSET_ZFLG (tmp == 0); SET_VFLG (0); SET_CFLG (0);\n");
	switch (curi->mnemo) {
	case i_BFTST:
	    break;
	case i_BFEXTU:
	    printf ("\tm68k_dreg(regs, (extra >> 12) & 7) = tmp;\n");
	    break;
	case i_BFCHG:
	    printf ("\ttmp = ~tmp;\n");
	    break;
	case i_BFEXTS:
	    printf ("\tif (GET_NFLG) tmp |= width == 32 ? 0 : (-1 << width);\n");
	    printf ("\tm68k_dreg(regs, (extra >> 12) & 7) = tmp;\n");
	    break;
	case i_BFCLR:
	    printf ("\ttmp = 0;\n");
	    break;
	case i_BFFFO:
	    printf ("\t{ uint32_t mask = 1 << (width-1);\n");
	    printf ("\twhile (mask) { if (tmp & mask) break; mask >>= 1; offset++; }}\n");
	    printf ("\tm68k_dreg(regs, (extra >> 12) & 7) = offset;\n");
	    break;
	case i_BFSET:
	    printf ("\ttmp = 0xffffffff;\n");
	    break;
	case i_BFINS:
	    printf ("\ttmp = m68k_dreg(regs, (extra >> 12) & 7);\n");
	    printf ("\tSET_NFLG (tmp & (1 << (width - 1)) ? 1 : 0);\n");
	    printf ("\tSET_ZFLG (tmp == 0);\n");
	    break;
	default:
	    break;
	}
	if (curi->mnemo == i_BFCHG
	    || curi->mnemo == i_BFCLR
	    || curi->mnemo == i_BFSET
	    || curi->mnemo == i_BFINS)
	    {
		printf ("\ttmp <<= (32 - width);\n");
		if (curi->dmode == Dreg) {
		    printf ("\tm68k_dreg(regs, dstreg) = (m68k_dreg(regs, dstreg) & ((offset & 0x1f) == 0 ? 0 :\n");
		    printf ("\t\t(0xffffffff << (32 - (offset & 0x1f))))) |\n");
		    printf ("\t\t(tmp >> (offset & 0x1f)) |\n");
		    printf ("\t\t(((offset & 0x1f) + width) >= 32 ? 0 :\n");
		    printf (" (m68k_dreg(regs, dstreg) & ((uint32_t)0xffffffff >> ((offset & 0x1f) + width))));\n");
		} else {
		    printf ("\tbf0 = (bf0 & (0xff000000 << (8 - (offset & 7)))) |\n");
		    printf ("\t\t(tmp >> (offset & 7)) |\n");
		    printf ("\t\t(((offset & 7) + width) >= 32 ? 0 :\n");
		    printf ("\t\t (bf0 & ((uint32_t)0xffffffff >> ((offset & 7) + width))));\n");
		    printf ("\tm68k_write_memory_32(dsta,bf0 );\n");
		    printf ("\tif (((offset & 7) + width) > 32) {\n");
		    printf ("\t\tbf1 = (bf1 & (0xff >> (width - 32 + (offset & 7)))) |\n");
		    printf ("\t\t\t(tmp << (8 - (offset & 7)));\n");
		    printf ("\t\tm68k_write_memory_8(dsta+4,bf1);\n");
		    printf ("\t}\n");
		}
	    }
	break;
    case i_PACK:
	if (curi->smode == Dreg) {
	    printf ("\tuint16_t val = m68k_dreg(regs, srcreg) + %s;\n", gen_nextiword ());
	    printf ("\tm68k_dreg(regs, dstreg) = (m68k_dreg(regs, dstreg) & 0xffffff00) | ((val >> 4) & 0xf0) | (val & 0xf);\n");
	} else {
	    printf ("\tuint16_t val;\n");
	    printf ("\tm68k_areg(regs, srcreg) -= areg_byteinc[srcreg];\n");
	    printf ("\tval = (uint16_t)m68k_read_memory_8(m68k_areg(regs, srcreg));\n");
	    printf ("\tm68k_areg(regs, srcreg) -= areg_byteinc[srcreg];\n");
	    printf ("\tval = (val | ((uint16_t)m68k_read_memory_8(m68k_areg(regs, srcreg)) << 8)) + %s;\n", gen_nextiword ());
	    printf ("\tm68k_areg(regs, dstreg) -= areg_byteinc[dstreg];\n");
	    printf ("\tm68k_write_memory_8(m68k_areg(regs, dstreg),((val >> 4) & 0xf0) | (val & 0xf));\n");
	}
	break;
    case i_UNPK:
	if (curi->smode == Dreg) {
	    printf ("\tuint16_t val = m68k_dreg(regs, srcreg);\n");
	    printf ("\tval = (((val << 4) & 0xf00) | (val & 0xf)) + %s;\n", gen_nextiword ());
	    printf ("\tm68k_dreg(regs, dstreg) = (m68k_dreg(regs, dstreg) & 0xffff0000) | (val & 0xffff);\n");
	} else {
	    printf ("\tuint16_t val;\n");
	    printf ("\tm68k_areg(regs, srcreg) -= areg_byteinc[srcreg];\n");
	    printf ("\tval = (uint16_t)m68k_read_memory_8(m68k_areg(regs, srcreg));\n");
	    printf ("\tval = (((val << 4) & 0xf00) | (val & 0xf)) + %s;\n", gen_nextiword ());
	    printf ("\tm68k_areg(regs, dstreg) -= areg_byteinc[dstreg];\n");
	    printf ("\tm68k_write_memory_8(m68k_areg(regs, dstreg),val);\n");
	    printf ("\tm68k_areg(regs, dstreg) -= areg_byteinc[dstreg];\n");
	    printf ("\tm68k_write_memory_8(m68k_areg(regs, dstreg),val >> 8);\n");
	}
	break;
    case i_TAS:
	genamode (curi->smode, "srcreg", curi->size, "src", 1, 0);
	genflags (flag_logical, curi->size, "src", "", "");
	printf ("\tsrc |= 0x80;\n");
	genastore ("src", curi->smode, "srcreg", curi->size, "src");
        if( curi->smode!=Dreg )  insn_n_cycles += 2;
	break;
    case i_FPP:
	genamode (curi->smode, "srcreg", curi->size, "extra", 1, 0);
	sync_m68k_pc ();
	printf ("\tfpp_opp(opcode,extra);\n");
	break;
    case i_FDBcc:
	genamode (curi->smode, "srcreg", curi->size, "extra", 1, 0);
	sync_m68k_pc ();
	printf ("\tfdbcc_opp(opcode,extra);\n");
	break;
    case i_FScc:
	genamode (curi->smode, "srcreg", curi->size, "extra", 1, 0);
	sync_m68k_pc ();
	printf ("\tfscc_opp(opcode,extra);\n");
	break;
    case i_FTRAPcc:
	sync_m68k_pc ();
	start_brace ();
	printf ("\tuint32_t oldpc = m68k_getpc();\n");
	if (curi->smode != am_unknown && curi->smode != am_illg)
	    genamode (curi->smode, "srcreg", curi->size, "dummy", 1, 0);
	sync_m68k_pc ();
	printf ("\tftrapcc_opp(opcode,oldpc);\n");
	break;
    case i_FBcc:
	sync_m68k_pc ();
	start_brace ();
	printf ("\tuint32_t pc = m68k_getpc();\n");
	genamode (curi->dmode, "srcreg", curi->size, "extra", 1, 0);
	sync_m68k_pc ();
	printf ("\tfbcc_opp(opcode,pc,extra);\n");
	break;
    case i_FSAVE:
	sync_m68k_pc ();
	printf ("\tfsave_opp(opcode);\n");
	break;
    case i_FRESTORE:
	sync_m68k_pc ();
	printf ("\tfrestore_opp(opcode);\n");
	break;

     case i_CINVL:
     case i_CINVP:
     case i_CINVA:
     case i_CPUSHL:
     case i_CPUSHP:
     case i_CPUSHA:
	break;
     case i_MOVE16:
	if ((opcode & 0xfff8) == 0xf620) {
	    /* MOVE16 (Ax)+,(Ay)+ */
	    printf ("\tuint32_t mems = m68k_areg(regs, srcreg) & ~15, memd;\n");
	    printf ("\tdstreg = (%s >> 12) & 7;\n", gen_nextiword());
	    printf ("\tmemd = m68k_areg(regs, dstreg) & ~15;\n");
	    printf ("\tm68k_write_memory_32(memd, m68k_read_memory_32(mems));\n");
	    printf ("\tm68k_write_memory_32(memd+4, m68k_read_memory_32(mems+4));\n");
	    printf ("\tm68k_write_memory_32(memd+8, m68k_read_memory_32(mems+8));\n");
	    printf ("\tm68k_write_memory_32(memd+12, m68k_read_memory_32(mems+12));\n");
	    printf ("\tif (srcreg != dstreg)\n");
	    printf ("\tm68k_areg(regs, srcreg) += 16;\n");
	    printf ("\tm68k_areg(regs, dstreg) += 16;\n");
	} else {
	    /* Other variants */
	    genamode (curi->smode, "srcreg", curi->size, "mems", 0, 2);
	    genamode (curi->dmode, "dstreg", curi->size, "memd", 0, 2);
	    printf ("\tmemsa &= ~15;\n");
	    printf ("\tmemda &= ~15;\n");
	    printf ("\tm68k_write_memory_32(memda, m68k_read_memory_32(memsa));\n");
	    printf ("\tm68k_write_memory_32(memda+4, m68k_read_memory_32(memsa+4));\n");
	    printf ("\tm68k_write_memory_32(memda+8, m68k_read_memory_32(memsa+8));\n");
	    printf ("\tm68k_write_memory_32(memda+12, m68k_read_memory_32(memsa+12));\n");
	    if ((opcode & 0xfff8) == 0xf600)
		printf ("\tm68k_areg(regs, srcreg) += 16;\n");
	    else if ((opcode & 0xfff8) == 0xf608)
		printf ("\tm68k_areg(regs, dstreg) += 16;\n");
	}
	break;

    case i_MMUOP:
	genamode (curi->smode, "srcreg", curi->size, "extra", 1, 0);
	sync_m68k_pc ();
	printf ("\tmmu_op(opcode,extra);\n");
	break;
    default:
	abort ();
	break;
    }
    finish_braces ();
    sync_m68k_pc ();
}

static void generate_includes(FILE * f)
{
//JLH:no	fprintf(f, "#include \"sysdeps.h\"\n");
//JLH:no	fprintf(f, "#include \"hatari-glue.h\"\n");
//JLH:no	fprintf(f, "#include \"maccess.h\"\n");
//JLH:no	fprintf(f, "#include \"memory.h\"\n");
//JLH:no	fprintf(f, "#include \"newcpu.h\"\n");
	fprintf(f, "#include \"cpudefs.h\"\n");
	fprintf(f, "#include \"cpuextra.h\"\n");
	fprintf(f, "#include \"inlines.h\"\n");
	fprintf(f, "#include \"cputbl.h\"\n");
	fprintf(f, "#define CPUFUNC(x) x##_ff\n"
		"#ifdef NOFLAGS\n"
		"#include \"noflags.h\"\n"
		"#endif\n");
}

// JLH: Since this is stuff that should be generated in a file that creates
// constants, it's in here now. :-P
static void GenerateTables(FILE * f)
{
	int i, j;

	fprintf(f, "\nconst int areg_byteinc[] = { 1, 1, 1, 1, 1, 1, 1, 2 };\n");
	fprintf(f, "const int imm8_table[]   = { 8, 1, 2, 3, 4, 5, 6, 7 };\n\n");
	fprintf(f, "const int movem_index1[256] = {\n");

	for(i=0; i<256; i++)
	{
		for(j=0; j<8; j++)
			if (i & (1 << j))
				break;

		fprintf(f, "0x%02X, ", j);

		if ((i % 16) == 15)
			fprintf(f, "\n");
	}

	fprintf(f, "};\n\n");
	fprintf(f, "const int movem_index2[256] = {\n");

	for(i=0; i<256; i++)
	{
		for(j=0; j<8; j++)
			if (i & (1 << j))
				break;

		fprintf(f, "0x%02X, ", 7 - j);

		if ((i % 16) == 15)
			fprintf(f, "\n");
	}

	fprintf(f, "};\n\n");
	fprintf(f, "const int movem_next[256] = {\n");

	for(i=0; i<256; i++)
	{
		for(j=0; j<8; j++)
			if (i & (1 << j))
				break;

		fprintf(f, "0x%02X, ", i & (~(1 << j)));

		if ((i % 16) == 15)
			fprintf(f, "\n");
	}

	fprintf(f, "};\n\n");
}

static int postfix;

static void generate_one_opcode (int rp)
{
    int i;
    uint16_t smsk, dmsk;
    long int opcode = opcode_map[rp];

    exactCpuCycles[0] = 0;  /* Default: not used */

    if (table68k[opcode].mnemo == i_ILLG
	|| table68k[opcode].clev > cpu_level)
	return;

    for (i = 0; lookuptab[i].name[0]; i++) {
	if (table68k[opcode].mnemo == lookuptab[i].mnemo)
	    break;
    }

    if (table68k[opcode].handler != -1)
	return;

    if (opcode_next_clev[rp] != cpu_level) {
	fprintf (stblfile, "{ CPUFUNC(op_%lx_%d), 0, %ld }, /* %s */\n", opcode, opcode_last_postfix[rp],
		 opcode, lookuptab[i].name);
	return;
    }
    fprintf (stblfile, "{ CPUFUNC(op_%lx_%d), 0, %ld }, /* %s */\n", opcode, postfix, opcode, lookuptab[i].name);
    fprintf (headerfile, "extern cpuop_func op_%lx_%d_nf;\n", opcode, postfix);
    fprintf (headerfile, "extern cpuop_func op_%lx_%d_ff;\n", opcode, postfix);
    printf ("unsigned long CPUFUNC(op_%lx_%d)(uint32_t opcode) /* %s */\n{\n", opcode, postfix, lookuptab[i].name);

    switch (table68k[opcode].stype) {
     case 0: smsk = 7; break;
     case 1: smsk = 255; break;
     case 2: smsk = 15; break;
     case 3: smsk = 7; break;
     case 4: smsk = 7; break;
     case 5: smsk = 63; break;
     case 7: smsk = 3; break;
     default: abort ();
    }
    dmsk = 7;

    next_cpu_level = -1;
    if (table68k[opcode].suse
	&& table68k[opcode].smode != imm && table68k[opcode].smode != imm0
	&& table68k[opcode].smode != imm1 && table68k[opcode].smode != imm2
	&& table68k[opcode].smode != absw && table68k[opcode].smode != absl
	&& table68k[opcode].smode != PC8r && table68k[opcode].smode != PC16)
    {
	if (table68k[opcode].spos == -1) {
	    if (((int) table68k[opcode].sreg) >= 128)
		printf ("\tuint32_t srcreg = (int32_t)(int8_t)%d;\n", (int) table68k[opcode].sreg);
	    else
		printf ("\tuint32_t srcreg = %d;\n", (int) table68k[opcode].sreg);
	} else {
	    char source[100];
	    int pos = table68k[opcode].spos;

	    if (pos)
		sprintf (source, "((opcode >> %d) & %d)", pos, smsk);
	    else
		sprintf (source, "(opcode & %d)", smsk);

	    if (table68k[opcode].stype == 3)
		printf ("\tuint32_t srcreg = imm8_table[%s];\n", source);
	    else if (table68k[opcode].stype == 1)
		printf ("\tuint32_t srcreg = (int32_t)(int8_t)%s;\n", source);
	    else
		printf ("\tuint32_t srcreg = %s;\n", source);
	}
    }
    if (table68k[opcode].duse
	/* Yes, the dmode can be imm, in case of LINK or DBcc */
	&& table68k[opcode].dmode != imm && table68k[opcode].dmode != imm0
	&& table68k[opcode].dmode != imm1 && table68k[opcode].dmode != imm2
	&& table68k[opcode].dmode != absw && table68k[opcode].dmode != absl)
    {
	if (table68k[opcode].dpos == -1) {
	    if (((int) table68k[opcode].dreg) >= 128)
		printf ("\tuint32_t dstreg = (int32_t)(int8_t)%d;\n", (int) table68k[opcode].dreg);
	    else
		printf ("\tuint32_t dstreg = %d;\n", (int) table68k[opcode].dreg);
	} else {
	    int pos = table68k[opcode].dpos;
#if 0
	    /* Check that we can do the little endian optimization safely.  */
	    if (pos < 8 && (dmsk >> (8 - pos)) != 0)
		abort ();
#endif	    
	    if (pos)
		printf ("\tuint32_t dstreg = (opcode >> %d) & %d;\n",
			pos, dmsk);
	    else
		printf ("\tuint32_t dstreg = opcode & %d;\n", dmsk);
	}
    }
    need_endlabel = 0;
    endlabelno++;
    sprintf (endlabelstr, "endlabel%d", endlabelno);
    if(table68k[opcode].mnemo==i_ASR || table68k[opcode].mnemo==i_ASL || table68k[opcode].mnemo==i_LSR || table68k[opcode].mnemo==i_LSL
       || table68k[opcode].mnemo==i_ROL || table68k[opcode].mnemo==i_ROR || table68k[opcode].mnemo==i_ROXL || table68k[opcode].mnemo==i_ROXR
       || table68k[opcode].mnemo==i_MVMEL || table68k[opcode].mnemo==i_MVMLE
       || table68k[opcode].mnemo==i_MULU || table68k[opcode].mnemo==i_MULS
       || table68k[opcode].mnemo==i_DIVU || table68k[opcode].mnemo==i_DIVS )
      printf("\tunsigned int retcycles = 0;\n");
    gen_opcode (opcode);
    if (need_endlabel)
	printf ("%s: ;\n", endlabelstr);

    if (strlen(exactCpuCycles) > 0)
	printf("%s\n",exactCpuCycles);
    else
	printf ("return %d;\n", insn_n_cycles);
    /* Now patch in the instruction cycles at the beginning of the function: */
    fseek(stdout, nCurInstrCycPos, SEEK_SET);
    printf("%d;", insn_n_cycles);
    fseek(stdout, 0, SEEK_END);

    printf ("}\n");
    opcode_next_clev[rp] = next_cpu_level;
    opcode_last_postfix[rp] = postfix;
}

static void generate_func(void)
{
	int i, j, rp;

	using_prefetch = 0;
	using_exception_3 = 0;
//JLH:
//	for(i=0; i<6; i++)
//For some reason, this doesn't work properly. Seems something is making a bad
//assumption somewhere.
//and it's probably in opcode_next_clev[rp]...
	for(i=4; i<6; i++)
	{
		cpu_level = 4 - i;

		//JLH
		for(rp=0; rp<nr_cpuop_funcs; rp++)
			opcode_next_clev[rp] = 0;

		if (i == 5)
		{
			cpu_level = 0;
			using_prefetch = 1;
			using_exception_3 = 1;
	
			for(rp=0; rp<nr_cpuop_funcs; rp++)
				opcode_next_clev[rp] = 0;
		}

		postfix = i;
		fprintf(stblfile, "const struct cputbl CPUFUNC(op_smalltbl_%d)[] = {\n", postfix);

		/* sam: this is for people with low memory (eg. me :)) */
		printf("\n"
			"#if !defined(PART_1) && !defined(PART_2) && "
			"!defined(PART_3) && !defined(PART_4) && "
			"!defined(PART_5) && !defined(PART_6) && "
			"!defined(PART_7) && !defined(PART_8)"
			"\n"
			"#define PART_1 1\n"
			"#define PART_2 1\n"
			"#define PART_3 1\n"
			"#define PART_4 1\n"
			"#define PART_5 1\n"
			"#define PART_6 1\n"
			"#define PART_7 1\n"
			"#define PART_8 1\n"
			"#endif\n\n");

		rp = 0;

		for(j=1; j<=8; ++j)
		{
			int k = (j * nr_cpuop_funcs) / 8;
			printf("#ifdef PART_%d\n", j);

			for(; rp<k; rp++)
				generate_one_opcode(rp);

			printf ("#endif\n\n");
		}

		fprintf(stblfile, "{ 0, 0, 0 }};\n");
	}
}

int main(int argc, char ** argv)
{
	read_table68k();
	do_merges();

	opcode_map = (int *)malloc(sizeof(int) * nr_cpuop_funcs);
	opcode_last_postfix = (int *)malloc(sizeof(int) * nr_cpuop_funcs);
	opcode_next_clev = (int *)malloc(sizeof(int) * nr_cpuop_funcs);
	counts = (unsigned long *)malloc(65536 * sizeof(unsigned long));
	read_counts();

	/* It would be a lot nicer to put all in one file (we'd also get rid of
	 * cputbl.h that way), but cpuopti can't cope.  That could be fixed, but
	 * I don't dare to touch the 68k version.  */

	headerfile = fopen("cputbl.h", "wb");
	stblfile = fopen("cpustbl.c", "wb");

	if (freopen("cpuemu.c", "wb", stdout) == NULL)
	{
		perror("cpuemu.c");
		return -1;
	}

	generate_includes(stdout);
	generate_includes(stblfile);

	GenerateTables(stdout);

	generate_func();

	free(table68k);
	return 0;
}
