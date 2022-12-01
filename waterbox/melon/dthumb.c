/*

Target CPU: ARM946E-S (Nintendo DS main CPU) and ARM7TDMI (Nintendo DS secondary CPU, Nintendo GBA main CPU)
Target Architecture: ARMv5TE and ARMv4T
ARM version: 5, 4
THUMB version: 2, 1
Documentation: https://www.intel.com/content/dam/support/us/en/programmable/support-resources/bulk-container/pdfs/literature/third-party/ddi0100e-arm-arm.pdf

*/

/* Instructions marked with an asterisk * are not available in ARMv4T */

/* Alphabetical list of ARM instructions (number of variants) */
/*
ADC         Add with Carry
ADD         Add
AND         Logical AND
B           Branch
BL          Branch and Link
BIC         Bit Clear
BKPT        Breakpoint*
BLX (2)     Branch with Link and Exchange*
BX          Branch and Exchange
CDP         Coprocessor Data Processing
CLZ         Count Leading Zeros*
CMN         Compare Negative
CMP         Compare
EOR         Logical Exclusive OR
LDC         Load Coprocessor
LDC2        Load Coprocessor 2*
LDM (3)     Load Multiple
LDR         Load Register
LDRB        Load Register Byte
LDRBT       Load Register Byte with Translation
LDRH        Load Register Halfword
LDRSB       Load Register Signed Byte
LDRSH       Load Register Signed Halfword
LDRT        Load Register with Translation
MCR         Move to Coprocessor from ARM Register
MCR2        Move to Coprocessor from ARM Register 2*
MLA         Multiply Accumulate
MOV         Move
MRC         Move to ARM Register from Coprocessor
MRC2        Move to ARM Register from Coprocessor 2*
MRS         Move PSR to General-purpose Register
MSR         Move to Status Register from ARM Register
MUL         Multiply
MVN         Move Negative
ORR         Logical OR
RSB         Reverse Substract
RSC         Reverse Substract with Carry
SBC         Substract with Carry
SMLAL       Signed Multply Accumulate Long
SMULL       Signed Multply Long
STC         Store Coprocessor
STC2        Store Coprocessor 2*
STM (2)     Store Multiple
STR         Store Register
STRB        Store Register Byte
STRBT       Store Register Byte with Translation
STRH        Store Register Halfword
STRT        Store Register with Translation
SUB         Substract
SWI         Software Interrupt
SWP         Swap
SWPB        Swap Byte
TEQ         Test Equivalence
TST         Test
UMLAL       Unsigned Multply Accumulate Long
UMULL       Unsigned Multply Long

//DSP enhanced (ARMv5TE exclusive)

LDRD        Load Register Dual
MCRR        Move to Coprocessor from Registers
MRRC        Move to Registers from Coprocessor
PLD         Preload Data
QADD        Saturating signed Add
QDADD       ;Performs a saturated integer doubling of one operand followed by a saturated integer addition with the other operand
QDSUB       ;Performs a saturated integer doubling of one operand followed by a saturated integer substraction from the other operand
QSUB        Saturating signed Subtraction
SMLA        Signed Multiply Accumulate
SMLAL       Signed Multiply Accumulate Long
SMLAW       Signed Multiply Accumulate Word
SMUL        Signed Multiply
SMULW       Signed Multiply Word
STRD        Store Register Dual
*/

/* Alphabetical list of THUMB instructions (number of variants) */
/*
ADC         Add with Carry
ADD (7)     Add
AND         Logical AND
ASR (2)     Arithmetic Shift Right
B (2)       Branch
BIC         Bit Clear
BKPT        Breakpoint*
BL          Branch with Link
BLX (2)     Branch with Link and Exchange*
BX          Branch and Exchange
CMN         Compare Negative
CMP (3)     Compare
EOR         Logical Exclusive OR
LDMIA       Load Multiple Increment After
LDR (4)     Load Register
LDRB (2)    Load Register Byte
LDRH (2)    Load Register Halfword
LDRSB       Load Register Signed Byte
LDRSH       Load Register Signed Halfword
LSL (2)     Logical Shift Left
LSR (2)     Logical Shift Right
MOV (3)     Move
MUL         Multiply
MVN         Move NOT
NEG         Negate
ORR         Logical OR
POP         Pop Multiple Registers
PUSH        Push Multiple Registers
ROR         Rotate Right Register
SBC         Substract with Carry
STMIA       Store Multiple Increment After
STR (3)     Store Register
STRB (2)    Store Register Byte
STRH (2)    Store Register Halfword
SUB (4)     Substract
SWI         Software Interrupt
TST         Test
*/

/* INCLUDES */

#include <stdio.h>
#include <string.h>
#include <stdint.h>
#include <stdlib.h>
#include <time.h>

/* MACROS */

//#define _CRT_SECURE_NO_WARNINGS 1
#define STRING_LENGTH (80) //can fail at 64 in SubstituteSubString
#define CONDITIONS_MAX (16)

#define BITS(x, b, n) ((x >> b) & ((1 << n) - 1)) //retrieves n bits from x starting at bit b
#define SIGNEX32_BITS(x, b, n) ((BITS(x,b,n) ^ (1<<(n-1))) - (1<<(n-1))) //convert n-bit value to signed 32 bits
#define SIGNEX32_VAL(x, n) ((x ^ (1<<(n-1))) - (1<<(n-1))) //convert n-bit value to signed 32 bits
#define ROR(x, n) ((x>>n)|(x<<(32-n))) //rotate right 32-bit value x by n bits

/* TYPEDEFS */

typedef uint32_t u32;
typedef uint16_t u16;
typedef uint8_t u8;

typedef enum {
    SIZE_16,
    SIZE_32
}THUMBSIZE;

typedef enum {
    ARMv4T, //ARM v4, THUMB v1
    ARMv5TE, //ARM v5, THUMB v2
    ARMv6 //ARM v6, THUMB v3
}ARMARCH; //only 32-bit legacy architectures with THUMB support

typedef enum {
    EQ, //equal, Z set
    NE, //not equal, Z clear
    CS, //carry set, C set (or HS, unsigned higher or same)
    CC, //carry clear, C clear (or LO, unsigned lower)
    MI, //minus/negative, N set
    PL, //plus/positive/zero, N clear
    VS, //overflow, V set
    VC, //no overflow, V clear
    HI, //unsigned higher, C set and Z clear
    LS, //unsigned lower or same, C clear or Z set
    GE, //signed greater than or equal, N==V
    LT, //signed less than, N!=V
    GT, //signed greater than, Z==0 and N==V
    LE, //signed less than or equal, Z==1 or N!=V
    AL, //unconditional, only with IT instructions
    NV  //unconditional, usually undefined
}CONDITION;

/* GLOBALS */

//todo: maybe put "2" instead of "nv" (or nothing) in the last one
const char Conditions[CONDITIONS_MAX][3] = { "eq", "ne", "cs", "cc", "mi", "pl", "vs", "vc", "hi", "ls", "ge", "lt", "gt", "le", "", "" }; //last two are "al" and "nv", but never displayed

const char AddressingModes[4][3] = {
    "da", //Decrement after
    "ia", //Increment after
    "db", //Decrement before
    "ib"  //Increment before
};

const char DataProcessing_thumb[16][4] = {
    "and",
    "eor",
    "lsl",
    "lsr",
    "asr",
    "adc",
    "sbc",
    "ror",
    "tst",
    "neg",
    "cmp",
    "cmn",
    "orr",
    "mul",
    "bic",
    "mvn"
};

const char DataProcessing_arm[16][4] = {
    "and",
    "eor",
    "sub",
    "rsb",
    "add",
    "adc",
    "sbc",
    "rsc",
    "tst", //no s
    "teq", //no s
    "cmp", //no s
    "cmn", //no s
    "orr",
    "mov", //only 1 source operand
    "bic",
    "mvn"  //only 1 source operand
};

const char MSR_cxsf[16][5] = {
    "",
    "c",
    "x",
    "xc",
    "s",
    "sc",
    "sx",
    "sxc",
    "f",
    "fc",
    "fx",
    "fxc",
    "fs",
    "fsc",
    "fsx",
    "fsxc"
};

const char LoadStoreRegister[8][6] = {
    "str", //STR (2)
    "strh", //STRH (2)
    "strb", //STRB (2)
    "ldrsb", //LDRSB
    "ldr", //LDR (2)
    "ldrh", //LDRH (2)
    "ldrb", //LDRB (2)
    "ldrsh" //LDRSH
};

const char DSP_AddSub[4][6] = { "qadd", "qsub", "qdadd", "qdsub" };
const char DSP_Multiplies[4][6] = { "smla", "", "smlal", "smul" }; //slot 1 empty, decided elsehow
const char MultiplyLong[4][6] = { "umull", "umlal", "smull", "smlal" };
const char AddCmpMovHighRegisters[3][4] = { "add", "cmp", "mov" };
const char MovCmpAddSubImmediate[4][4] = { "mov", "cmp", "add", "sub" };
const char Shifters[4][4] = { "lsl", "lsr", "asr", "ror" }; //+rrx
const char ShiftImmediate[4][4] = { "lsl", "lsr", "asr", "" }; //thumb

u32 debug_na_count = 0;

/* LIBRARY FUNCTIONS */

static void SubstituteSubString(char dst[STRING_LENGTH], u32 index, const char* sub, u32 size) {
    /* Insert sub string of length size (< STRING_LENGTH) at dst[index] */
    char tmp[STRING_LENGTH] = { 0 }; //zinit
    memcpy(&tmp, sub, size); //init
    memcpy(&tmp[size], &dst[index + size + 1], STRING_LENGTH - index - size - 1); //save
    memcpy(&dst[index], tmp, STRING_LENGTH - index - 1); //restore
}

static void CheckSpecialRegister(char* str, int size) {
    /* Replace special registers by their common names */
    //(r12->ip), r13->sp, r14->lr, r15->pc
    //todo: check for size != -1
    for (int i = 0; i < size; i++) //when using a variable string size (return value of sprintf), 10% faster than constant size of STRING_LENGTH 64
    {
        if (str[i] == 'r')
        {
            switch (*(u16*)&str[i + 1])
            {
            case 0x3331: //"13"
            {
                SubstituteSubString(str, i, "sp", sizeof("sp") - 1);
                break;
            }
            case 0x3431: //"14"
            {
                SubstituteSubString(str, i, "lr", sizeof("lr") - 1);
                break;
            }
            case 0x3531: //"15"
            {
                SubstituteSubString(str, i, "pc", sizeof("pc") - 1);
                break;
            }
            }
        }
    }
}

static u32 FormatStringRegisterList_thumb(char str[STRING_LENGTH], u16 reg, const char* pclr, u8 pclr_size) {
    /* Format str according to the reg bifield, group consecutive registers together */
    /* Support for a special 9th register name */
    u32 bits = 0; //return value, number of 1 bits in reg
    u32 streak = 0; //current streak, used to group registers together
    u32 pos = 0; //position in the str array
    char tmp[4] = { 0 }; //temporary buffer for register write
    for (u32 i = 0; i < 9; i++) //reg is a 9-bit value
    {
        if (BITS(reg, i, 1))
        {
            bits++;
            switch (streak)
            {
            case 0: //streak ended, print current register
            {
                if (i == 8)
                {
                    sprintf(tmp, "%s", pclr); //pc or lr override
                    memcpy(&str[pos], tmp, sizeof(tmp)); //write
                    pos += pclr_size; //sizeof(pclr)
                }
                else
                {
                    sprintf(tmp, "r%u,", i); //r0-r7
                    memcpy(&str[pos], tmp, sizeof(tmp)); //write
                    pos += 3; //sizeof("r0")
                }
                break;
            }
            case 1: break; //used to catch default cases later
            case 2: //hyphenation if at least 3 in a row
            {
                str[pos - 1] = '-'; //replaces the comma
                break;
            }
            default:
            {
                //todo: a bit repetitive, tidy up
                if (i == 8) //if on last bit, close the current streak by writing previous and last register 
                {
                    sprintf(tmp, "r%u,", i - 1); //previous register (can't be LR/PC)
                    memcpy(&str[pos], tmp, sizeof(tmp)); //write
                    pos += 3; //depends on size of tmp
                    sprintf(tmp, "%s", pclr); //pc or lr override
                    memcpy(&str[pos], tmp, sizeof(tmp)); //write
                    pos += pclr_size; //sizeof(pclr)
                }
            }
            }
            streak = (i < 8) ? streak + 1 : 0; //avoids grouping LR/PC
        }
        else
        {
            if (streak > 1) //if broke with at least 3 in a row, close
            {
                sprintf(tmp, "r%u,", i - 1); //previous register (can't be LR/PC)
                memcpy(&str[pos], tmp, sizeof(tmp)); //write
                pos += 3; //depends on size of tmp
            }
            streak = 0; //reset
        }
    }
    if (pos) str[pos - 1] = 0; //removes the coma on the last register   
    return bits; //number of 1 bits
}

static u32 FormatStringRegisterList_arm(char str[STRING_LENGTH], u16 reg) {
    /* Format str according to the reg bifield, group consecutive registers together */
    u32 bits = 0; //return value, number of 1 bits in reg
    u32 streak = 0; //current streak, used to group registers together
    u32 pos = 0; //position in the str array
    char tmp[5] = { 0 }; //temporary buffer for register write
    for (u32 i = 0; i < 16; i++) //all registers
    {
        if (BITS(reg, i, 1))
        {
            bits++;
            switch (streak)
            {
            case 0: //streak started, print current register
            {
                sprintf(tmp, "r%u,", i); //r0-r15
                memcpy(&str[pos], tmp, sizeof(tmp)); //write
                pos = (i < 10) ? pos + sizeof("r0") : pos + sizeof("r10");
                break;
            }
            case 1: break;
            case 2: //hyphenation if at least 3 in a row
            {
                str[pos - 1] = '-'; //replaces the comma
                break;
            }
            default:
            {
                if (i == 15) //if on last bit, close the current streak by writing last register 
                {
                    str[pos++] = 'p';
                    str[pos++] = 'c';
                    str[pos++] = ','; //comma will get deleted later
                }
            }
            }
            streak++;
        }
        else
        {
            if (streak > 1) //if broke with at least 3 in a row, close
            {
                sprintf(tmp, "r%u,", i - 1); //previous register
                memcpy(&str[pos], tmp, sizeof(tmp)); //write
                pos = (i - 1 < 10) ? pos + sizeof("r0") : pos + sizeof("r10");
            }
            streak = 0; //reset
        }
    }
    if (pos) str[pos - 1] = 0; //removes the coma on the last register   
    return bits; //number of 1 bits
}

static int FormatExtraLoadStore(u32 c, char* str, u8 cond, const char* op) {
    /*  */
    u8 rd = BITS(c, 12, 4);
    u8 rn = BITS(c, 16, 4);
    u8 w = BITS(c, 21, 1);
    u8 p = BITS(c, 24, 1);
    if (BITS(c, 22, 1)) //immediate
    {
        u8 ofs = (BITS(c, 8, 4) << 4) | BITS(c, 0, 4);
        const char* sign = BITS(c, 23, 1) ? "+" : "-";
        if (p) //offset, pre-indexed
        {
            const char* pre = w ? "!" : "";
            return sprintf(str, "%s%s r%u, [r%u, #%s0x%X]%s", op, Conditions[cond], rd, rn, sign, ofs, pre);
        }
        else //post-indexed
        {
            if (w) return 0; //w must be 0, else UNPREDICTABLE //todo: check if really invalid
            return sprintf(str, "%s%s r%u, [r%u], #%s0x%X", op, Conditions[cond], rd, rn, sign, ofs);
        }
    }
    else //register
    {
        u8 rm = BITS(c, 0, 4);
        const char* sign = BITS(c, 23, 1) ? "" : "-"; //implicit "+" in front of registers
        if (p) //offset, pre-indexed
        {
            const char* pre = w ? "!" : "";
            return sprintf(str, "%s%s r%u, [r%u, %sr%u]%s", op, Conditions[cond], rd, rn, sign, rm, pre);
        }
        else //post-indexed
        {
            if (w) return 0; //w must be 0, else UNPREDICTABLE //todo: check if really invalid
            return sprintf(str, "%s%s r%u, [r%u], %sr%u", op, Conditions[cond], rd, rn, sign, rm);
        }
    }
}

u32 Disassemble_thumb(u32 code, char str[STRING_LENGTH], ARMARCH tv) {
    /* Convert a code into a string, return size of the processed code (SIZE_16 or SIZE_32) */

    THUMBSIZE thumb_size = SIZE_16; //return value
    u16 c = code & 0xffff; //low 16 bits
    int size = 0; //return value of sprintf to be passed to CheckSpecialRegister

    switch (c >> 13)
    {
    case 0: //0x0000 //LSL, LSR, ASR, ADD, SUB
    {
        u8 rd = BITS(c, 0, 3);
        u8 rn = BITS(c, 3, 3);
        u8 index = BITS(c, 11, 2);
        if (index == 3) //ADD, SUB, MOV
        {
            if (BITS(c, 6, 5) == 16) //MOV (2) (technically ADD (1) with imm==0)
            {
                size = sprintf(str, "mov r%u, r%u", rd, rn);
            }
            else
            {
                u8 rm_imm = BITS(c, 6, 3);
                const char* op = (BITS(c, 9, 1)) ? "sub" : "add";
                if (BITS(c, 10, 1)) //ADD (1), SUB (1) -immediate
                {
                    size = sprintf(str, "%s r%u, r%u, #0x%X", op, rd, rn, rm_imm);
                }
                else //ADD (3), SUB (3) -register
                {
                    size = sprintf(str, "%s r%u, r%u, r%u", op, rd, rn, rm_imm);
                }
            }
        }
        else //Shift by immediate: LSL (1), LSR (1), ASR (1) 
        {
            size = sprintf(str, "%s r%u, r%u, #0x%X", ShiftImmediate[index], rd, rn, BITS(c, 6, 5));
        }
        break;
    }

    case 1: //0x2000 //MOV (1), CMP (1), ADD (2), SUB (2)
    {
        size = sprintf(str, "%s r%u, #0x%X", MovCmpAddSubImmediate[BITS(c, 11, 2)], BITS(c, 8, 3), BITS(c, 0, 8));
        break;
    }

    case 2: //0x4000 //lots...
    {
        switch (BITS(c, 10, 3))
        {
        case 0: //Data-processing registers
        {
            size = sprintf(str, "%s r%u, r%u", DataProcessing_thumb[BITS(c, 6, 4)], BITS(c, 0, 3), BITS(c, 3, 3));
            break;
        }
        case 1: //Special data processing
        {
            u8 rd = (BITS(c, 7, 1) << 3) | (BITS(c, 0, 3));
            u8 rm = BITS(c, 3, 4);
            u8 op = BITS(c, 8, 2);
            if (op == 3) //Branch/exchange instruction set
            {
                if (BITS(c, 0, 3)) break; //Should-Be-Zero
                if (BITS(c, 7, 1)) //BLX (2)
                {
                    if (tv < ARMv5TE) break; //UNPREDICTABLE prior to ARM version 5
                    size = sprintf(str, "blx r%u", rm);
                }
                else //BX
                {
                    size = sprintf(str, "bx r%u", rm);
                }
            }
            else //ADD (4), CMP (3), MOV (3)
            {
                if (!BITS(c, 6, 2)) break; //UNPREDICTABLE
                size = sprintf(str, "%s r%u, r%u", AddCmpMovHighRegisters[op], rd, rm);
            }
            break;
        }
        default: //Load from literal pool, Load/Store register offset
        {
            if (BITS(c, 12, 1)) //Load/store register offset
            {
                size = sprintf(str, "%s r%u, [r%u, r%u]", LoadStoreRegister[BITS(c, 9, 3)], BITS(c, 0, 3), BITS(c, 3, 3), BITS(c, 6, 3));
            }
            else //LDR (3)
            {
                if (!BITS(c, 11, 1)) break; //Should-Be-One
                size = sprintf(str, "ldr r%u, [pc, #0x%X]", BITS(c, 8, 3), 4 * BITS(c, 0, 8));
            }
        }
        }
        break;
    }

    case 3: //0x6000 //STR, LDR, STRB, LDRB
    {
        switch (BITS(c, 11, 2))
        {
        case 0: //STR (1)
        {
            size = sprintf(str, "str r%u, [r%u, #0x%X]", BITS(c, 0, 3), BITS(c, 3, 3), 4 * BITS(c, 6, 5));
            break;
        }
        case 1: //LDR (1)
        {
            size = sprintf(str, "ldr r%u, [r%u, #0x%X]", BITS(c, 0, 3), BITS(c, 3, 3), 4 * BITS(c, 6, 5));
            break;
        }
        case 2: //STRB (1)
        {
            size = sprintf(str, "strb r%u, [r%u, #0x%X]", BITS(c, 0, 3), BITS(c, 3, 3), BITS(c, 6, 5));
            break;
        }
        case 3: //LDRB (1)
        {
            size = sprintf(str, "ldrb r%u, [r%u, #0x%X]", BITS(c, 0, 3), BITS(c, 3, 3), BITS(c, 6, 5));
            break;
        }
        }
        break;
    }

    case 4: //0x8000 //STR, LDR, STRH, LDRH
    {
        if (BITS(c, 12, 1)) //LDR (4), STR (3)
        {
            const char* op = (BITS(c, 11, 1)) ? "ldr" : "str";
            size = sprintf(str, "%s r%u, [sp, #0x%X]", op, BITS(c, 8, 3), 4 * BITS(c, 0, 8));
        }
        else //LDRH (1), STRH (1)
        {
            const char* op = (BITS(c, 11, 1)) ? "ldrh" : "strh";
            size = sprintf(str, "%s r%u, [r%u, #0x%X]", op, BITS(c, 0, 3), BITS(c, 3, 3), 2 * BITS(c, 6, 5));
        }
        break;
    }

    case 5: //0xA000 //Misc and ADD to sp or pc
    {
        if (BITS(c, 12, 1)) //Misc, fig 6-2
        {
            switch (BITS(c, 8, 4))
            {
            case 0: //ADD (4), SUB (7) to/from SP
            {
                const char* op = (BITS(c, 7, 1)) ? "sub" : "add";
                size = sprintf(str, "%s sp, #0x%X", op, 4 * BITS(c, 0, 7));
                break;
            }
            //PUSH/POP
            case 4:
            case 5:
            case 12:
            case 13:
            {
                char reglist[STRING_LENGTH] = { 0 };
                u16 registers = BITS(c, 0, 9);
                if (BITS(c, 11, 1)) //POP
                {
                    if (FormatStringRegisterList_thumb(reglist, registers, "pc", sizeof("pc"))) //if BitCount(registers) < 1 then UNPREDICTABLE
                    {
                        size = sprintf(str, "pop {%s}", reglist);
                    }
                }
                else //PUSH
                {
                    if (FormatStringRegisterList_thumb(reglist, registers, "lr", sizeof("lr"))) //if BitCount(registers) < 1 then UNPREDICTABLE
                    {
                        size = sprintf(str, "push {%s}", reglist);
                    }
                }
                break;
            }
            case 14: //BKPT
            {
                if (tv >= ARMv5TE) //undefined prior to ARM version 5
                {
                    size = sprintf(str, "bkpt #0x%X", BITS(c, 0, 8));
                }
                break;
            }
            }
        }
        else //ADD (5), ADD (6) to SP or PC
        {
            const char* sppc = (BITS(c, 11, 2)) ? "sp" : "pc";
            size = sprintf(str, "add r%u, %s, #0x%X", BITS(c, 8, 3), sppc, 4 * BITS(c, 0, 8));
        }
        break;
    }

    case 6: //0xC000 //B, SWI, LDMIA, STMIA
    {
        if (BITS(c, 12, 1)) //Conditional branch, Undefined, System call
        {
            switch (BITS(c, 8, 4))
            {
            case 14: //UDF "Permanently undefined space", OS dependant
            {
                //size = sprintf(str, "udf #0x%X", BITS(c, 0, 8)); //note: not an instruction in ARMv5TE, just UNDEFINED
                break;
            }
            case 15: //SWI
            {
                size = sprintf(str, "swi #0x%X", BITS(c, 0, 8));
                break;
            }
            default: //B (1) conditional
            {
                size = sprintf(str, "b%s #0x%X", Conditions[BITS(c, 8, 4)], 4 + 2 * SIGNEX32_BITS(c, 0, 8));
            }
            }
        }
        else //LDMIA/STMIA
        {
            char reglist[STRING_LENGTH] = { 0 };
            const char* op = (BITS(c, 11, 1)) ? "ldmia" : "stmia";
            if (FormatStringRegisterList_thumb(reglist, BITS(c, 0, 8), "", 0))
            {
                size = sprintf(str, "%s r%u!, {%s}", op, BITS(c, 8, 3), reglist);
            }
        }
        break;
    }

    case 7: //0xE000 //B, then 32-bit instructions
    {
        switch (BITS(c, 11, 2))
        {
        case 0:
        {
            size = sprintf(str, "b #0x%X", 4 + 2 * SIGNEX32_BITS(c, 0, 11)); //11 bits to signed 32 bits
            break;
        }
        //case 1: break; //undefined on first pass
        case 2: //BL/BLX prefix
        {
            c = code >> 16; //get high 16 bits
            if (c >> 13 == 7)
            {
                switch (BITS(c, 11, 2))
                {
                case 1: //BLX (1)
                {
                    if (tv < ARMv5TE) break;
                    if (!BITS(c, 0, 1))
                    {
                        thumb_size = SIZE_32;

                        int ofs = (BITS((code & 0xffff), 0, 11)) << 12;
                        ofs = SIGNEX32_VAL(ofs, 23);
                        ofs += 4;
                        ofs += 2 * BITS(c, 0, 11);
                        //note: bit 1 should be cleared (word aligned target address)
                        size = sprintf(str, "blx #0x%X", ofs);
                    }
                    break;
                }
                //case 2: break; //BL/BLX prefix
                case 3: //BL
                {
                    thumb_size = SIZE_32;

                    int ofs = (BITS((code & 0xffff), 0, 11)) << 12;
                    ofs = SIGNEX32_VAL(ofs, 23);
                    ofs += 4;
                    ofs += 2 * BITS(c, 0, 11);

                    size = sprintf(str, "bl #0x%X", ofs);
                    break;
                }
                }
            }
            break;
        }
        }
        break;
    }
    }
    if (!str[0]) //in case nothing was written to it
    {
        debug_na_count++;
        sprintf(str, "n/a");
    }
    else
    {
        CheckSpecialRegister(str, size); //formatting
    }
    return thumb_size;
}

void Disassemble_arm(u32 code, char str[STRING_LENGTH], ARMARCH av) {
    /**/
    //Reference: page 68 of 811 from the ARM Architecture reference manual june 2000 edition
    //todo: proper support for ARMv4T 
    //todo: extra caution for UNPREDICTABLE instructions, need to remove them? or decode regardless?

    int size = 0; //return value of sprintf to be passed to CheckSpecialRegister
    u32 c = code; //alias
    u8 cond = BITS(c, 28, 4); //condition bits

    //todo: check unconditional instructions first to avoid putting a check later at each stage

    switch (BITS(c, 25, 3))
    {
    case 0: //Data processing, DSP instructions, ...
    {
        if (cond == NV) break; //undefined
        if (BITS(c, 4, 1))
        {
            if (BITS(c, 7, 1)) //Multiplies, extra load/stores: see fig 3-2
            {
                u8 oplo = BITS(c, 5, 2);
                if (!oplo)
                {
                    if (!BITS(c, 22, 3)) //Multiply (accumulate)
                    {
                        u8 rm = BITS(c, 0, 4);
                        u8 rs = BITS(c, 8, 4);
                        u8 rn = BITS(c, 12, 4);
                        u8 rd = BITS(c, 16, 4);
                        const char* s = BITS(c, 20, 1) ? "s" : "";
                        if (BITS(c, 21, 1)) //MLA
                        {
                            size = sprintf(str, "mla%s%s r%u, r%u, r%u, r%u", s, Conditions[cond], rd, rm, rs, rn);
                        }
                        else //MUL
                        {
                            if (rn) break; //Should-Be-Zero
                            size = sprintf(str, "mul%s%s r%u, r%u, r%u", s, Conditions[cond], rd, rm, rs);
                        }
                    }
                    else if (BITS(c, 23, 1)) //Multiply (accumulate) long
                    {
                        const char* s = BITS(c, 20, 1) ? "s" : "";
                        size = sprintf(str, "%s%s%s r%u, r%u, r%u, r%u", MultiplyLong[BITS(c, 21, 2)], s, Conditions[cond], BITS(c, 12, 4), BITS(c, 16, 4), BITS(c, 0, 4), BITS(c, 8, 4));
                    }
                    else //Swap/swap byte (SWP, SWPB)
                    {
                        if (BITS(c, 8, 4)) break; //Should-Be-Zero
                        const char* b = BITS(c, 22, 1) ? "b" : ""; //byte or no
                        size = sprintf(str, "swp%s%s r%u, r%u, [r%u]", b, Conditions[cond], BITS(c, 12, 4), BITS(c, 0, 4), BITS(c, 16, 4));
                    }
                }
                else
                {
                    if (!BITS(c, 22, 1) && BITS(c, 8, 4)) break; //Should-Be-Zero if register offset
                    if (oplo == 1) //Load/store halfword
                    {
                        const char* l = BITS(c, 20, 1) ? "ldrh" : "strh"; //load or store
                        size = FormatExtraLoadStore(c, str, cond, l);
                    }
                    else
                    {
                        if (BITS(c, 20, 1)) //Load signed halfword/byte
                        {
                            const char* h = BITS(c, 5, 1) ? "ldrsh" : "ldrsb"; //halfword/byte
                            size = FormatExtraLoadStore(c, str, cond, h);
                        }
                        else //Load/store two words
                        {
                            if (BITS(c, 12, 1)) break; //undefined if Rd is odd
                            const char* s = BITS(c, 5, 1) ? "strd" : "ldrd"; //store or load
                            size = FormatExtraLoadStore(c, str, cond, s);
                        }
                    }
                }
            }
            else
            {
                if (BITS(c, 23, 2) == 2 && !BITS(c, 20, 1)) //Miscellanous instructions, see fig 3-3
                {
                    u8 oplo = BITS(c, 5, 2);
                    u8 ophi = BITS(c, 21, 2);
                    switch (oplo)
                    {
                    case 0:
                    {
                        if (ophi == 3) //CLZ
                        {
                            //note: if PC is in either register, UNPREDICTABLE
                            if (av < ARMv5TE) break;
                            if (!BITS(c, 16, 4) || !BITS(c, 8, 4)) break; //Should-Be-One
                            size = sprintf(str, "clz%s r%u, r%u", Conditions[cond], BITS(c, 12, 4), BITS(c, 0, 4));
                        }
                        else if (ophi == 1)//Branch/exchange instruction set (BX)
                        {
                            if (BITS(c, 8, 12) != 0xfff) break; //Should-Be-One
                            size = sprintf(str, "bx%s r%u", Conditions[cond], BITS(c, 0, 4));
                        }
                        break;
                    }
                    case 1: //BLX (2)
                    {
                        if (av < ARMv5TE) break;
                        if (ophi != 1) break;
                        if (BITS(c, 8, 12) != 0xfff) break; //Should-Be-One
                        size = sprintf(str, "blx%s r%u", Conditions[cond], BITS(c, 0, 4));
                        break;
                    }
                    case 2: //Enhanced DSP add/sub (QADD, QDADD, QSUB, QDSUB)
                    {
                        //note: if PC is in either register, UNPREDICTABLE
                        if (av < ARMv5TE) break;
                        if (BITS(c, 8, 4)) break; //Should-Be-Zero
                        size = sprintf(str, "%s%s r%u, r%u, r%u", DSP_AddSub[ophi], Conditions[cond], BITS(c, 12, 4), BITS(c, 0, 4), BITS(c, 16, 4));
                        break;
                    }
                    case 3: //Software breakpoint (BKPT)
                    {
                        if (av < ARMv5TE) break;
                        if (ophi != 1) break;
                        size = sprintf(str, "bkpt #0x%X", (BITS(c, 8, 12) << 4) | BITS(c, 0, 4));
                        break;
                    }
                    }
                }
                else //Data processing register shift
                {
                    u8 rm = BITS(c, 0, 4);
                    u8 shift = BITS(c, 5, 2);
                    u8 rs = BITS(c, 8, 4);
                    u8 rd = BITS(c, 12, 4);
                    u8 rn = BITS(c, 16, 4);
                    u8 op = BITS(c, 21, 4);
                    const char* s = BITS(c, 20, 1) ? "s" : "";
                    switch (op)
                    {
                    case 8: //TST
                    case 9: //TEQ
                    case 10: //CMP
                    case 11: //CMN
                    {
                        size = sprintf(str, "%s%s r%u, r%u, %s r%u", DataProcessing_arm[op], Conditions[cond], rn, rm, Shifters[shift], rs);
                        break;
                    }
                    case 13: //MOV
                    case 15: //MVN
                    {
                        size = sprintf(str, "%s%s%s r%u, r%u, %s r%u", DataProcessing_arm[op], s, Conditions[cond], rd, rm, Shifters[shift], rs);
                        break;
                    }
                    default:
                    {
                        size = sprintf(str, "%s%s%s r%u, r%u, r%u, %s r%u", DataProcessing_arm[op], s, Conditions[cond], rd, rn, rm, Shifters[shift], rs);
                    }
                    }
                }
            }
        }
        else //bit 4 == 0
        {
            if (BITS(c, 23, 2) == 2 && !BITS(c, 20, 1)) //Miscellanous instructions, see fig 3-3
            {
                if (BITS(c, 7, 1)) //Enhanced DSP multiplies
                {
                    if (av < ARMv5TE) break;
                    //note: PC for any register is UNPREDICTABLE
                    u8 rm = BITS(c, 0, 4);
                    const char* x = BITS(c, 5, 1) ? "t" : "b";
                    const char* y = BITS(c, 6, 1) ? "t" : "b";
                    u8 rs = BITS(c, 8, 4);
                    u8 rn_rdlo = BITS(c, 12, 4);
                    u8 rd_rdhi = BITS(c, 16, 4);
                    u8 op = BITS(c, 21, 2);
                    switch (op)
                    {
                    case 0: //SMLA
                    {
                        size = sprintf(str, "%s%s%s%s r%u, r%u, r%u, r%u", DSP_Multiplies[op], x, y, Conditions[cond], rd_rdhi, rm, rs, rn_rdlo);
                        break;
                    }
                    case 1: //SMLAW, SMULW
                    {
                        if (BITS(c, 5, 1)) //SMULW
                        {
                            if (rn_rdlo) break; //Should-Be-Zero 
                            size = sprintf(str, "smulw%s%s r%u, r%u, r%u", y, Conditions[cond], rd_rdhi, rm, rs);
                        }
                        else //SMLAW
                        {
                            size = sprintf(str, "smlaw%s%s r%u, r%u, r%u, r%u", y, Conditions[cond], rd_rdhi, rm, rs, rn_rdlo);
                        }
                        break;
                    }
                    case 2: //SMLAL
                    {
                        size = sprintf(str, "%s%s%s%s r%u, r%u, r%u, r%u", DSP_Multiplies[op], x, y, Conditions[cond], rn_rdlo, rd_rdhi, rm, rs);
                        break;
                    }
                    case 3: //SMUL
                    {
                        if (rn_rdlo) break; //Should-Be-Zero
                        size = sprintf(str, "%s%s%s%s r%u, r%u, r%u", DSP_Multiplies[op], x, y, Conditions[cond], rd_rdhi, rm, rs);
                        break;
                    }
                    }
                }
                else
                {
                    if (!BITS(c, 0, 12) && BITS(c, 16, 4) == 15) //Move status reg to reg (MRS)
                    {
                        //note: if Rd == PC, UNPREDICTABLE
                        char sreg = BITS(c, 22, 1) ? 's' : 'c'; //SPSR (1) or CPSR (0)
                        size = sprintf(str, "mrs%s r%u, %cpsr", Conditions[cond], BITS(c, 12, 4), sreg);
                    }
                    else if (BITS(c, 12, 4) == 15 && !BITS(c, 4, 8) && BITS(c, 21, 1)) //Move reg to status reg (MSR register)
                    {
                        char sreg = BITS(c, 22, 1) ? 's' : 'c'; //SPSR (1) or CPSR (0)
                        size = sprintf(str, "msr%s %cpsr_%s, r%u", Conditions[cond], sreg, MSR_cxsf[BITS(c, 16, 4)], BITS(c, 0, 4));
                    }
                }
            }
            else //Data processing immediate shift
            {
                //todo: write a function for all 3 Data Processing switch-cases
                u8 op = BITS(c, 21, 4);
                u8 rm = BITS(c, 0, 4);
                u8 shift = BITS(c, 5, 2);
                u8 shift_imm = BITS(c, 7, 5);
                char sstr[STRING_LENGTH] = "rrx"; //default, overwrite if incorrect
                if ((shift == 1 || shift == 2) && !shift_imm) shift_imm = 32; //0~31 for LSL, 1~32 for LSR, ASR and ROR, always 0 for RRX
                if (!(shift == 3 && !shift_imm)) sprintf(sstr, "%s #%u", Shifters[shift], shift_imm);
                u8 rd = BITS(c, 12, 4);
                u8 rn = BITS(c, 16, 4);
                const char* s = BITS(c, 20, 1) ? "s" : "";
                switch (op)
                {
                case 8: //TST
                case 9: //TEQ
                case 10: //CMP
                case 11: //CMN
                {
                    size = sprintf(str, "%s%s r%u, r%u, %s", DataProcessing_arm[op], Conditions[cond], rn, rm, sstr);
                    break;
                }
                case 13: //MOV
                case 15: //MVN
                {
                    size = sprintf(str, "%s%s%s r%u, r%u, %s", DataProcessing_arm[op], s, Conditions[cond], rd, rm, sstr);
                    break;
                }
                default:
                {
                    size = sprintf(str, "%s%s%s r%u, r%u, r%u, %s", DataProcessing_arm[op], s, Conditions[cond], rd, rn, rm, sstr);
                }
                }
            }
        }
        break;
    }
    case 1: //Data processing and MSR immediate
    {
        if (cond == NV) break; //undefined
        u32 imm = ROR(BITS(c, 0, 8), 2 * BITS(c, 8, 4));

        if (BITS(c, 12, 4) == 15 && !BITS(c, 20, 1)) //MSR immediate
        {
            char sreg = BITS(c, 22, 1) ? 's' : 'c'; //SPSR (1) or CPSR (0)
            size = sprintf(str, "msr%s %cpsr_%s, #0x%X", Conditions[cond], sreg, MSR_cxsf[BITS(c, 16, 4)], imm);
        }
        else //Data processing immediate
        {
            u8 op = BITS(c, 21, 4);
            u8 rd = BITS(c, 12, 4);
            u8 rn = BITS(c, 16, 4);
            const char* s = BITS(c, 20, 1) ? "s" : "";
            switch (op)
            {
            case 8: //TST
            case 9: //TEQ
            case 10: //CMP
            case 11: //CMN
                //always update the condition codes: <op>{<cond>} <Rn>, <shift>
            {
                size = sprintf(str, "%s%s r%u, #0x%X", DataProcessing_arm[op], Conditions[cond], rn, imm);
                break;
            }
            case 13: //MOV
            case 15: //MVN
                //only one source operand: <op>{<cond>}{S} <Rd>, <shift>
            {
                size = sprintf(str, "%s%s%s r%u, #0x%X", DataProcessing_arm[op], s, Conditions[cond], rd, imm);
                break;
            }

            default: //others: <op>{<cond>}{S} <Rd>, <Rn>, <shift>
            {
                size = sprintf(str, "%s%s%s r%u, r%u, #0x%X", DataProcessing_arm[op], s, Conditions[cond], rd, rn, imm);
            }
            }
        }
        break;
    }
    case 2: //Load/store immediate offset
    {
        if (cond == NV) break; //undefined
        //bits 25, 24, 23 and 21 decide the addressing mode
        u8 rd = BITS(c, 12, 4);
        u8 rn = BITS(c, 16, 4);
        u16 imm = BITS(c, 0, 12); //12 bits for LDR and LDRB (8 bits for LDRH and LDRSB)
        const char* sign = BITS(c, 23, 1) ? "+" : "-"; //sign of the immediate offset
        const char* ls = BITS(c, 20, 1) ? "ldr" : "str"; //load or store
        const char* b = BITS(c, 22, 1) ? "b" : ""; //byte or word
        if (BITS(c, 24, 1)) //offset or pre-indexed
        {
            const char* w = BITS(c, 21, 1) ? "!" : ""; //pre-indexed if 1
            size = sprintf(str, "%s%s%s r%u, [r%u, #%s0x%X]%s", ls, b, Conditions[cond], rd, rn, sign, imm, w);
        }
        else //post-indexed
        {
            const char* w = BITS(c, 21, 1) ? "t" : ""; //user mode
            size = sprintf(str, "%s%s%s%s r%u, [r%u], #%s0x%X", ls, b, w, Conditions[cond], rd, rn, sign, imm);
        }
        break;
    }
    case 3: //Load/store register offset
    {
        if (cond == NV) break; //undefined
        if (BITS(c, 4, 1)) break; //undefined
        u8 rm = BITS(c, 0, 4);
        u8 shift = BITS(c, 5, 2);
        u16 shift_imm = BITS(c, 7, 5);
        u8 rd = BITS(c, 12, 4);
        u8 rn = BITS(c, 16, 4);
        const char* sign = BITS(c, 23, 1) ? "" : "-"; //sign of the immediate offset, + implicit
        const char* ls = BITS(c, 20, 1) ? "ldr" : "str"; //load or store
        const char* b = BITS(c, 22, 1) ? "b" : ""; //byte or word
        char sstr[STRING_LENGTH] = "rrx"; //default, overwrite if incorrect
        if ((shift == 1 || shift == 2) && !shift_imm) shift_imm = 32; //0~31 for LSL, 1~32 for LSR, ASR and ROR, always 0 for RRX
        if (!(shift == 3 && !shift_imm)) sprintf(sstr, "%s #%u", Shifters[shift], shift_imm);
        //note: if rm==r15 or rn==r15 then UNPREDICTABLE
        //note: if rn==rm then UNPREDICTABLE
        if (BITS(c, 24, 1)) //offset or pre-indexed
        {
            const char* w = BITS(c, 21, 1) ? "!" : ""; //pre-indexed if 1
            size = sprintf(str, "%s%s%s r%u, [r%u, %sr%u, %s]%s", ls, b, Conditions[cond], rd, rn, sign, rm, sstr, w);
        }
        else //post-indexed
        {
            const char* w = BITS(c, 21, 1) ? "t" : ""; //user mode
            size = sprintf(str, "%s%s%s%s r%u, [r%u], %sr%u, %s", ls, b, w, Conditions[cond], rd, rn, sign, rm, sstr);
        }
        break;
    }
    case 4: //Load/store multiple
    {
        if (cond == NV) break; //undefined
        char reglist[STRING_LENGTH] = { 0 };
        FormatStringRegisterList_arm(reglist, BITS(c, 0, 16));
        u8 rn = BITS(c, 16, 4);
        const char* w = BITS(c, 21, 1) ? "!" : ""; //W bit
        const char* s = BITS(c, 22, 1) ? "^" : ""; //S bit
        u8 am = BITS(c, 23, 2); //PU bits
        const char* op = (BITS(c, 20, 1)) ? "ldm" : "stm"; //LDM or STM
        size = sprintf(str, "%s%s%s r%u%s, {%s}%s", op, AddressingModes[am], Conditions[cond], rn, w, reglist, s);
        break;
    }
    case 5: //Branch instructions
    {
        if (cond == NV && av >= ARMv5TE) //BLX (1)
        {
            size = sprintf(str, "blx #0x%X", 8 + 4 * SIGNEX32_BITS(c, 0, 24) + 2 * BITS(c, 24, 1));
        }
        else //B, BL
        {
            const char* l = (BITS(c, 24, 1)) ? "l" : "";
            size = sprintf(str, "b%s%s #0x%X", l, Conditions[cond], 8 + 4 * SIGNEX32_BITS(c, 0, 24));
        }
        break;
    }
    case 6: //Coprocessor load/store, Double register transfers 
    {
        if (av < ARMv5TE) break;
        //if (cond == NV) break; //only unpredictable prior to ARMv5
        if (BITS(c, 21, 4) == 2) //MCRR, MRRC
        {
            //note: if PC is specified for Rn or Rd, UNPREDICTABLE
            const char* op = BITS(c, 20, 1) ? "mrrc" : "mcrr";
            size = sprintf(str, "%s%s p%u, #0x%X, r%u, r%u, c%u", op, Conditions[cond], BITS(c, 8, 4), BITS(c, 4, 4), BITS(c, 12, 4), BITS(c, 16, 4), BITS(c, 0, 4));
        }
        else //LDC, STC
        {
            //todo: reformat the cond checks into only 1
            if (cond == NV && av < ARMv5TE) break;

            const char* op = BITS(c, 20, 1) ? "ldc" : "stc";
            const char* str_cond = (cond == NV) ? "2" : Conditions[cond]; //LDC2, STC2
            const char* l = BITS(c, 22, 1) ? "l" : ""; //long
            char str_cond_long[8] = { 0 };
            if (cond == NV) sprintf(str_cond_long, "%s%s", str_cond, l);
            else sprintf(str_cond_long, "%s%s", l, str_cond);
            u8 ofs_opt = BITS(c, 0, 8);
            u8 cp_num = BITS(c, 8, 4);
            u8 crd = BITS(c, 12, 4);
            u8 rn = BITS(c, 16, 4);
            const char* sign = BITS(c, 23, 1) ? "+" : "-";
            switch ((2 * BITS(c, 24, 1)) | BITS(c, 21, 1)) //(p*2) | w
            {
            case 0: //p==0, w==0 //unindexed: [<Rn>], <option>
            {
                size = sprintf(str, "%s%s p%u, c%u, [r%u], {0x%X}", op, str_cond_long, cp_num, crd, rn, ofs_opt);
                break;
            }
            case 1: //p==0, w==1 //post indexed: [<Rn>], #+/-<offset_8>*4
            {
                size = sprintf(str, "%s%s p%u, c%u, [r%u], #%s0x%X", op, str_cond_long, cp_num, crd, rn, sign, 4 * ofs_opt);
                break;
            }
            case 2: //p==1, w==0 //immediate offset: [<Rn>, #+/-<offset_8>*4]
            {
                size = sprintf(str, "%s%s p%u, c%u, [r%u, #%s0x%X]", op, str_cond_long, cp_num, crd, rn, sign, 4 * ofs_opt);
                break;
            }
            case 3: //p==1, w==1 //pre indexed: [<Rn>, #+/-<offset_8>*4]!
            {
                size = sprintf(str, "%s%s p%u, c%u, [r%u, #%s0x%X]!", op, str_cond_long, cp_num, crd, rn, sign, 4 * ofs_opt);
                break;
            }
            }
        }
        break;
    }
    case 7: //Software Interrupt, Coprocessor register transfer, Coprocessor data processing
    {
        if (BITS(c, 24, 1)) //SWI
        {
            if (cond == NV) break;
            size = sprintf(str, "swi%s #0x%X", Conditions[cond], BITS(c, 0, 24));
        }
        else
        {
            if (cond == NV && av < ARMv5TE) break;

            u8 crn = BITS(c, 16, 4);
            u8 p = BITS(c, 8, 4);
            u8 rd_crd = BITS(c, 12, 4);
            u8 crm = BITS(c, 0, 4);
            u8 op2 = BITS(c, 5, 3);
            const char* str_cond = (cond == NV) ? "2" : Conditions[cond]; //suffix
            if (BITS(c, 4, 1)) //MCR, MRC
            {
                const char* str_op = BITS(c, 20, 1) ? "mrc" : "mcr";
                size = sprintf(str, "%s%s p%u, #0x%X, r%u, c%u, c%u, #0x%X", str_op, str_cond, p, BITS(c, 21, 3), rd_crd, crn, crm, op2);
            }
            else //CDP
            {
                size = sprintf(str, "cdp%s p%u, #0x%X, c%u, c%u, c%u, #0x%X", str_cond, p, BITS(c, 20, 4), rd_crd, crn, crm, op2); //no condition suffix for cdp2
            }
        }
        break;
    }
    }

    //also: unconditionnal instructions if bits 28 to 31 are 1111
    if (!str[0] && (c & 0xFD70F000) == 0xF550F000) //Cache preload (PLD)
    {
        //todo: placing it here is wasteful, find a workaround
        //bit-pattern: 1111 01x1 x101 xxxx 1111 xxxx xxxx xxxx
        //data mask  : 1111 1101 0111 0000 1111 0000 0000 0000
        //inst. mask : 1111 0101 0101 0000 1111 0000 0000 0000
        //note: only offset addressing modes
        // [<Rn>, #+/-<offset_12>] //immediate
        // [<Rn>, +/-<Rm>] //register
        // [<Rn>, +/-<Rm>, <shift> #<shift_imm>] //scaled register
        u8 rn = BITS(c, 16, 4);
        if (BITS(c, 25, 1)) //(scaled) register
        {
            u8 rm = BITS(c, 0, 4);
            u8 shift = BITS(c, 5, 2);
            u16 shift_imm = BITS(c, 7, 5);
            const char* sign = BITS(c, 23, 1) ? "" : "-";
            char sstr[STRING_LENGTH] = "rrx"; //default, overwrite if incorrect
            if ((shift == 1 || shift == 2) && !shift_imm) shift_imm = 32; //0~31 for LSL, 1~32 for LSR, ASR and ROR, always 0 for RRX
            if (!(shift == 3 && !shift_imm)) sprintf(sstr, "%s #%u", Shifters[shift], shift_imm); //intermediate sprintf
            size = sprintf(str, "pld [r%u, %sr%u, %s]", rn, sign, rm, sstr);
        }
        else //immediate
        {
            const char* sign = BITS(c, 23, 1) ? "+" : "-";
            size = sprintf(str, "pld [r%u, #%s0x%X]", rn, sign, BITS(c, 0, 12));
        }
    }

    if (!str[0]) //in case nothing was written to it
    {
        debug_na_count++;
        sprintf(str, "n/a"); //don't need to get size, no special register formatting
    }
    else
    {
        CheckSpecialRegister(str, size); //formatting of SP, LR, PC
    }
}
