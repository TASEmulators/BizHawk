/*
this disassembler is courtesy of mednafen
*/

#include "psx.h"
#include "types.h"
#include <string>
#include <string.h>

//TODO - add break opcode to disassembly

namespace MDFN_IEN_PSX
{

struct OpEntry
{
 u32 mask;
 u32 value;
 const char *mnemonic;
 const char *format;
};

#define MASK_OP (0x3F << 26)
#define MASK_FUNC (0x3F)
#define MASK_RS (0x1F << 21)
#define MASK_RT (0x1F << 16)
#define MASK_RD (0x1F << 11)
#define MASK_SA (0x1F << 6)

#define MK_OP(mnemonic, format, op, func, extra_mask)	{ MASK_OP | (op ? 0 : MASK_FUNC) | extra_mask, (op << 26) | func, mnemonic, format }

#define MK_OP_REGIMM(mnemonic, regop)	{ MASK_OP | MASK_RT, (0x01 << 26) | (regop << 16), mnemonic, "s, p" }

static OpEntry ops[] =
{
	MK_OP("nop",	"",	0, 0, MASK_RT | MASK_RD | MASK_SA),
 MK_OP("sll",	"d, t, a", 0, 0, 0),
 MK_OP("srl",   "d, t, a", 0, 2, 0),
 MK_OP("sra",   "d, t, a", 0, 3, 0),

 MK_OP("sllv",   "d, t, s", 0, 4, 0),
 MK_OP("srlv",   "d, t, s", 0, 6, 0),
 MK_OP("srav",   "d, t, s", 0, 7, 0),

 MK_OP("jr",   	 "s", 0, 8, 0),
 MK_OP("jalr",   "d, s", 0, 9, 0),

 MK_OP("syscall", "", 0, 12, 0),	// TODO
 MK_OP("break",   "", 0, 13, 0),	// TODO

 MK_OP("mfhi",  "d", 0, 16, 0),
 MK_OP("mthi",  "s", 0, 17, 0),
 MK_OP("mflo",  "d", 0, 18, 0),
 MK_OP("mtlo",  "s", 0, 19, 0),

 MK_OP("mult",  "s, t", 0, 24, 0),
 MK_OP("multu", "s, t", 0, 25, 0),
 MK_OP("div",   "s, t", 0, 26, 0),
 MK_OP("divu",  "s, t", 0, 27, 0),

 MK_OP("add",   "d, s, t", 0, 32, 0),
 MK_OP("addu",  "d, s, t", 0, 33, 0),
 MK_OP("sub",	"d, s, t", 0, 34, 0),
 MK_OP("subu",	"d, s, t", 0, 35, 0),
 MK_OP("and",   "d, s, t", 0, 36, 0),
 MK_OP("or",    "d, s, t", 0, 37, 0),
 MK_OP("xor",   "d, s, t", 0, 38, 0),
 MK_OP("nor",   "d, s, t", 0, 39, 0),
 MK_OP("slt",   "d, s, t", 0, 42, 0),
 MK_OP("sltu",  "d, s, t", 0, 43, 0),

 MK_OP_REGIMM("bgez",	0x01),
 MK_OP_REGIMM("bgezal", 0x11),
 MK_OP_REGIMM("bltz",	0x00),
 MK_OP_REGIMM("bltzal",	0x10),

 
 MK_OP("j",	"P", 2, 0, 0),
 MK_OP("jal",	"P", 3, 0, 0),

 MK_OP("beq",	"s, t, p", 4, 0, 0),
 MK_OP("bne",   "s, t, p", 5, 0, 0),
 MK_OP("blez",  "s, p", 6, 0, 0),
 MK_OP("bgtz",  "s, p", 7, 0, 0),

 MK_OP("addi",  "t, s, i", 8, 0, 0),
 MK_OP("addiu", "t, s, i", 9, 0, 0),
 MK_OP("slti",  "t, s, i", 10, 0, 0),
 MK_OP("sltiu", "t, s, i", 11, 0, 0),

 MK_OP("andi",  "t, s, z", 12, 0, 0),

 MK_OP("ori",  	"t, s, z", 13, 0, 0),
 MK_OP("xori",  "t, s, z", 14, 0, 0),
 MK_OP("lui",	"t, z", 15, 0, 0),

 // COP0 stuff here
 //#define MK_OP(mnemonic, format, op, func, extra_mask)	{ MASK_OP | (op ? 0 : MASK_FUNC) | extra_mask, (op << 26) | func, mnemonic, format }
#define COPMF(num) ((num)<<21)
 MK_OP("mfc0",  "t, D", 16, 0, 0x03E00000),
 MK_OP("mfc1",  "t, D", 17, 0, 0x03E00000),
 MK_OP("mfc2",  "t, D", 18, 0, 0x03E00000),
 MK_OP("mfc3",  "t, D", 19, 0, 0x03E00000),
 MK_OP("mtc0",  "t, D", 16, COPMF(4), 0x03E00000),
 MK_OP("mtc1",  "t, D", 17, COPMF(4), 0x03E00000),
 MK_OP("mtc2",  "t, D", 18, COPMF(4), 0x03E00000),
 MK_OP("mtc3",  "t, D", 19, COPMF(4), 0x03E00000),
 //MK_OP("rfe",   "",     19, COPMF(4), 0x03E00000), //TODO

 MK_OP("lb",    "t, i(s)", 32, 0, 0),
 MK_OP("lh",    "t, i(s)", 33, 0, 0),
 MK_OP("lwl",   "t, i(s)", 34, 0, 0),
 MK_OP("lw",    "t, i(s)", 35, 0, 0),
 MK_OP("lbu",   "t, i(s)", 36, 0, 0),
 MK_OP("lhu",   "t, i(s)", 37, 0, 0),
 MK_OP("lwr",   "t, i(s)", 38, 0, 0),

 MK_OP("sb",    "t, i(s)", 40, 0, 0),
 MK_OP("sh",    "t, i(s)", 41, 0, 0),
 MK_OP("swl",   "t, i(s)", 42, 0, 0),
 MK_OP("sw",	"t, i(s)", 43, 0, 0),
 MK_OP("swr",   "t, i(s)", 46, 0, 0),

 { 0, 0, NULL, NULL }
};

std::string DisassembleMIPS(u32 PC, u32 instr)
{
 std::string ret = "UNKNOWN";
 unsigned int rs = (instr >> 21) & 0x1F;
 unsigned int rt = (instr >> 16) & 0x1F;
 unsigned int rd = (instr >> 11) & 0x1F;
 unsigned int shamt = (instr >> 6) & 0x1F;
 unsigned int immediate = (s32)(s16)(instr & 0xFFFF);
 unsigned int immediate_ze = (instr & 0xFFFF);
 unsigned int jt = instr & ((1 << 26) - 1);

 static const char *gpr_names[32] =
 {
  "r0", "at", "v0", "v1", "a0", "a1", "a2", "a3", "t0", "t1", "t2", "t3", "t4", "t5", "t6", "t7",
    "s0", "s1", "s2", "s3", "s4", "s5", "s6", "s7", "t8", "t9", "k0", "k1", "gp", "sp", "fp", "ra"
 };

 OpEntry *op = ops;

 while(op->mnemonic)
 {
  if((instr & op->mask) == op->value)
  {
   // a = shift amount
   // s = rs
   // t = rt
   // d = rd
   // i = immediate
   // z = immediate, zero-extended
   // p = PC + 4 + immediate
   // P = ((PC + 4) & 0xF0000000) | (26bitval << 2)
   char s_a[16];
   char s_i[16];
   char s_z[16];
   char s_p[16];
   char s_P[16];
	 char s_D[16];

	_snprintf(s_D, sizeof(s_D), "%d", rd);

   _snprintf(s_a, sizeof(s_a), "%d", shamt);

   if(immediate < 0)
    _snprintf(s_i, sizeof(s_i), "%d", immediate);
   else
    _snprintf(s_i, sizeof(s_i), "0x%04x", (u32)immediate);

   _snprintf(s_z, sizeof(s_z), "0x%04x", immediate_ze);

   _snprintf(s_p, sizeof(s_p), "0x%08x", PC + 4 + (immediate << 2));

   _snprintf(s_P, sizeof(s_P), "0x%08x", ((PC + 4) & 0xF0000000) | (jt << 2));

   ret = std::string(op->mnemonic);
   ret.append(10 - ret.size(), ' ');

   for(unsigned int i = 0; i < strlen(op->format); i++)
   {
    switch(op->format[i])
    {
     case 'a':
	ret.append(s_a);
	break;

     case 'i':
	ret.append(s_i);
	break;

     case 'z':
	ret.append(s_z);
	break;

     case 'p':
	ret.append(s_p);
	break;

     case 'P':
	ret.append(s_P);
	break;

     case 's':
	ret.append(gpr_names[rs]);
	break;

     case 't':
	ret.append(gpr_names[rt]);
	break;

    case 'd':
	ret.append(gpr_names[rd]);
	break;

		case 'D':
			ret.append(s_D);
			break;

     default:
	ret.append(1, op->format[i]);
	break;
    }
   }
   break;
  }
  op++;
 }

 return(ret);
}

}

