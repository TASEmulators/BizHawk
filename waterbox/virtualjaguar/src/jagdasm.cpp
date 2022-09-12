//
// Jaguar RISC Disassembly
//
// Originally by David Raingeard
// GCC/SDL port by Niels Wagenaar (Linux/WIN32) and Carwin Jones (BeOS)
// Minor cleanups by James Hammons
// (C) 2012 Underground Software
//
// JLH = James Hammons <jlhamm@acm.org>
//
// Who  When        What
// ---  ----------  -------------------------------------------------------------
// JLH  06/01/2012  Created this log (long overdue! ;-)
// JLH  01/23/2013  Beautifying of disassembly, including hex digits of opcodes
//                  and operands
//

#include "jagdasm.h"

#include <stdio.h>
#include "jaguar.h"

#define ROPCODE(a) JaguarReadWord(a)

uint8_t convert_zero[32] =
{ 32,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31 };

const char * condition[32] =
{
	"",
	"nz,",
	"z,",
	"???,",
	"nc,",
	"nc nz,",
	"nc z,",
	"???,",

	"c,",
	"c nz,",
	"c z,",
	"???,",
	"???,",
	"???,",
	"???,",
	"???,",

	"???,",
	"???,",
	"???,",
	"???,",
	"nn,",
	"nn nz,",
	"nn z,",
	"???,",

	"n,",
	"n nz,",
	"n z,",
	"???,",
	"???,",
	"???,",
	"???,",
	"never,"
};



char * signed_16bit(int16_t val)
{
	static char temp[10];

	if (val < 0)
		sprintf(temp, "-$%X", -val);
	else
		sprintf(temp, "$%X", val);

	return temp;
}


unsigned dasmjag(int dsp_type, char * bufferOut, unsigned pc)
{
	char buffer[64];
	int op = ROPCODE(pc);
	int reg1 = (op >> 5) & 31;
	int reg2 = op & 31;
	int size = 2;
	pc += 2;

	switch (op >> 10)
	{
		case 0:		sprintf(buffer, "ADD     R%02d,R%02d", reg1, reg2);				break;
		case 1:		sprintf(buffer, "ADDC    R%02d,R%02d", reg1, reg2);				break;
		case 2:		sprintf(buffer, "ADDQ    $%X,R%02d", convert_zero[reg1], reg2);	break;
		case 3:		sprintf(buffer, "ADDQT   $%X,R%02d", convert_zero[reg1], reg2);	break;
		case 4:		sprintf(buffer, "SUB     R%02d,R%02d", reg1, reg2);				break;
		case 5:		sprintf(buffer, "SUBC    R%02d,R%02d", reg1, reg2);				break;
		case 6:		sprintf(buffer, "SUBQ    $%X,R%02d", convert_zero[reg1], reg2);	break;
		case 7:		sprintf(buffer, "SUBQT   $%X,R%02d", convert_zero[reg1], reg2);	break;
		case 8:		sprintf(buffer, "NEG     R%02d", reg2);							break;
		case 9:		sprintf(buffer, "AND     R%02d,R%02d", reg1, reg2);				break;
		case 10:	sprintf(buffer, "OR      R%02d,R%02d", reg1, reg2);				break;
		case 11:	sprintf(buffer, "XOR     R%02d,R%02d", reg1, reg2);				break;
		case 12:	sprintf(buffer, "NOT     R%02d", reg2);							break;
		case 13:	sprintf(buffer, "BTST    $%X,R%02d", reg1, reg2);				break;
		case 14:	sprintf(buffer, "BSET    $%X,R%02d", reg1, reg2);				break;
		case 15:	sprintf(buffer, "BCLR    $%X,R%02d", reg1, reg2);				break;
		case 16:	sprintf(buffer, "MULT    R%02d,R%02d", reg1, reg2);				break;
		case 17:	sprintf(buffer, "IMULT   R%02d,R%02d", reg1, reg2);				break;
		case 18:	sprintf(buffer, "IMULTN  R%02d,R%02d", reg1, reg2);				break;
		case 19:	sprintf(buffer, "RESMAC  R%02d", reg2);							break;
		case 20:	sprintf(buffer, "IMACN   R%02d,R%02d", reg1, reg2);				break;
		case 21:	sprintf(buffer, "DIV     R%02d,R%02d", reg1, reg2);				break;
		case 22:	sprintf(buffer, "ABS     R%02d", reg2);							break;
		case 23:	sprintf(buffer, "SH      R%02d,R%02d", reg1, reg2);				break;
		case 24:	sprintf(buffer, "SHLQ    $%X,R%02d", 32 - reg1, reg2);	break;
		case 25:	sprintf(buffer, "SHRQ    $%X,R%02d", convert_zero[reg1], reg2);	break;
		case 26:	sprintf(buffer, "SHA     R%02d,R%02d", reg1, reg2);				break;
		case 27:	sprintf(buffer, "SHARQ   $%X,R%02d", convert_zero[reg1], reg2);	break;
		case 28:	sprintf(buffer, "ROR     R%02d,R%02d", reg1, reg2);				break;
		case 29:	sprintf(buffer, "RORQ    $%X,R%02d", convert_zero[reg1], reg2);	break;
		case 30:	sprintf(buffer, "CMP     R%02d,R%02d", reg1, reg2);				break;
		case 31:	sprintf(buffer, "CMPQ    %s,R%02d", signed_16bit((int16_t)(reg1 << 11) >> 11), reg2);break;
		case 32:	if (dsp_type == JAGUAR_GPU)
						sprintf(buffer, "SAT8    R%02d", reg2);
					else
						sprintf(buffer, "SUBQMOD $%X,R%02d", convert_zero[reg1], reg2);
					break;
		case 33:	if (dsp_type == JAGUAR_GPU)
						sprintf(buffer, "SAT16   R%02d", reg2);
					else
						sprintf(buffer, "SAT16S  R%02d", reg2);
					break;
		case 34:	sprintf(buffer, "MOVE    R%02d,R%02d", reg1, reg2);				break;
		case 35:	sprintf(buffer, "MOVEQ   %d,R%02d", reg1, reg2);				break;
		case 36:	sprintf(buffer, "MOVETA  R%02d,R%02d", reg1, reg2);				break;
		case 37:	sprintf(buffer, "MOVEFA  R%02d,R%02d", reg1, reg2);				break;
		case 38:	sprintf(buffer, "MOVEI   #$%X,R%02d", ROPCODE(pc) | (ROPCODE(pc+2)<<16), reg2); size = 6; break;
		case 39:	sprintf(buffer, "LOADB   (R%02d),R%02d", reg1, reg2);			break;
		case 40:	sprintf(buffer, "LOADW   (R%02d),R%02d", reg1, reg2);			break;
		case 41:	sprintf(buffer, "LOAD    (R%02d),R%02d", reg1, reg2);			break;
		case 42:	if (dsp_type == JAGUAR_GPU)
						sprintf(buffer, "LOADP   (R%02d),R%02d", reg1, reg2);
					else
						sprintf(buffer, "SAT32S  R%02d", reg2);
					break;
		case 43:	sprintf(buffer, "LOAD    (R14+$%X),R%02d", convert_zero[reg1]*4, reg2);break;
		case 44:	sprintf(buffer, "LOAD    (R15+$%X),R%02d", convert_zero[reg1]*4, reg2);break;
		case 45:	sprintf(buffer, "STOREB  R%02d,(R%02d)", reg2, reg1);			break;
		case 46:	sprintf(buffer, "STOREW  R%02d,(R%02d)", reg2, reg1);			break;
		case 47:	sprintf(buffer, "STORE   R%02d,(R%02d)", reg2, reg1);			break;
		case 48:	if (dsp_type == JAGUAR_GPU)
						sprintf(buffer, "STOREP  R%02d,(R%02d)", reg2, reg1);
					else
						sprintf(buffer, "MIRROR  R%02d", reg2);
					break;
		case 49:	sprintf(buffer, "STORE   R%02d,(R14+$%X)", reg2, convert_zero[reg1]*4);break;
		case 50:	sprintf(buffer, "STORE   R%02d,(R15+$%X)", reg2, convert_zero[reg1]*4);break;
		case 51:	sprintf(buffer, "MOVE    PC,R%02d", reg2);						break;
		case 52:	sprintf(buffer, "JUMP    %s(R%02d)", condition[reg2], reg1);	break;
		case 53:	sprintf(buffer, "JR      %s$%X", condition[reg2], pc + ((int8_t)(reg1 << 3) >> 2)); break;
		case 54:	sprintf(buffer, "MMULT   R%02d,R%02d", reg1, reg2);				break;
		case 55:	sprintf(buffer, "MTOI    R%02d,R%02d", reg1, reg2);				break;
		case 56:	sprintf(buffer, "NORMI   R%02d,R%02d", reg1, reg2);				break;
		case 57:	sprintf(buffer, "NOP");											break;
		case 58:	sprintf(buffer, "LOAD    (R14+R%02d),R%02d", reg1, reg2);		break;
		case 59:	sprintf(buffer, "LOAD    (R15+R%02d),R%02d", reg1, reg2);		break;
		case 60:	sprintf(buffer, "STORE   R%02d,(R14+R%02d)", reg2, reg1);		break;
		case 61:	sprintf(buffer, "STORE   R%02d,(R15+R%02d)", reg2, reg1);		break;
		case 62:	if (dsp_type == JAGUAR_GPU)
						sprintf(buffer, "SAT24   R%02d", reg2);
					else
						sprintf(buffer, "illegal [%d,%d]", reg1, reg2);
					break;
		case 63:	if (dsp_type == JAGUAR_GPU)
						sprintf(buffer, (reg1 ? "UNPACK  R%02d" : "PACK    R%02d"), reg2);
					else
						sprintf(buffer, "ADDQMOD $%X,R%02d", convert_zero[reg1], reg2);
					break;
	}

#if 0
	sprintf(bufferOut,"%-24s (%04X)", buffer, op);
#else
	if (size == 2)
		sprintf(bufferOut, "%04X            %-24s", op, buffer);
	else
	{
		uint16_t word1 = ROPCODE(pc), word2 = ROPCODE(pc + 2);
		sprintf(bufferOut, "%04X %04X %04X  %-24s", op, word1, word2, buffer);
	}
#endif

	return size;
}
