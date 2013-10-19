using System;
using System.Collections.Generic;

namespace BizHawk.Emulation.CPUs.M68000
{
    partial class MC68000
    {
        void BuildOpcodeTable()
        {
            // NOTE: Do not change the order of these assigns without testing. There is
            // some overwriting of less-specific opcodes with more-specific opcodes.
            // TODO: should really come up with means of only assigning to applicable addressing modes,
            // instead of this lame overwriting business.

            Assign("move",  MOVE,  "00", "Size2_0", "XnAm", "AmXn");
            Assign("movea", MOVEA, "00", "Size2_0", "Xn", "001", "AmXn");
            Assign("moveq", MOVEQ, "0111", "Xn", "0", "Data8");
            Assign("movem", MOVEM0,"010010001", "Size1", "AmXn");
            Assign("movem", MOVEM1,"010011001", "Size1", "AmXn");
            Assign("lea",   LEA,   "0100", "Xn", "111", "AmXn");
            Assign("clr",   CLR,   "01000010", "Size2_1", "AmXn");
            Assign("ext",   EXT,   "010010001", "Size1", "000", "Xn");
            Assign("pea",   PEA,   "0100100001", "AmXn");

            Assign("andi",  ANDI,  "00000010", "Size2_1", "AmXn");
            Assign("eori",  EORI,  "00001010", "Size2_1", "AmXn");
            Assign("ori",   ORI,   "00000000", "Size2_1", "AmXn");
            Assign("asl",   ASLd,  "1110", "Data3", "1", "Size2_1", "Data1", "00", "Xn");
            Assign("asr",   ASRd,  "1110", "Data3", "0", "Size2_1", "Data1", "00", "Xn");
            Assign("lsl",   LSLd,  "1110", "Data3", "1", "Size2_1", "Data1", "01", "Xn");
            Assign("lsr",   LSRd,  "1110", "Data3", "0", "Size2_1", "Data1", "01", "Xn");
            Assign("roxl",  ROXLd, "1110", "Data3", "1", "Size2_1", "Data1", "10", "Xn");
            Assign("roxr",  ROXRd, "1110", "Data3", "0", "Size2_1", "Data1", "10", "Xn");
            Assign("rol",   ROLd,  "1110", "Data3", "1", "Size2_1", "Data1", "11", "Xn");
            Assign("ror",   RORd,  "1110", "Data3", "0", "Size2_1", "Data1", "11", "Xn");
            Assign("swap",  SWAP,  "0100100001000","Xn");
            Assign("and",   AND0,  "1100", "Xn", "0", "Size2_1", "AmXn");
            Assign("and",   AND1,  "1100", "Xn", "1", "Size2_1", "AmXn");
            Assign("eor",   EOR,   "1011", "Xn", "1", "Size2_1", "AmXn");
            Assign("or",    OR0,   "1000", "Xn", "0", "Size2_1", "AmXn");
            Assign("or",    OR1,   "1000", "Xn", "1", "Size2_1", "AmXn");
            Assign("not",   NOT,   "01000110", "Size2_1", "AmXn");
            Assign("neg",   NEG,   "01000100", "Size2_1", "AmXn");

            Assign("jmp",   JMP,   "0100111011", "AmXn");
            Assign("jsr",   JSR,   "0100111010", "AmXn");
            Assign("bcc",   Bcc,   "0110", "CondMain", "Data8");
            Assign("bra",   BRA,   "01100000", "Data8");
            Assign("bsr",   BSR,   "01100001", "Data8");
            Assign("scc",   Scc,   "0101", "CondAll", "11","AmXn");
            Assign("dbcc",  DBcc,  "0101", "CondAll", "11001", "Xn");
            Assign("rte",   RTE,   "0100111001110011");
            Assign("rts",   RTS,   "0100111001110101");
            Assign("rtr",   RTR,   "0100111001110111");
            Assign("tst",   TST,   "01001010", "Size2_1", "AmXn");
            Assign("btst",  BTSTi, "0000100000", "AmXn");
            Assign("btst",  BTSTr, "0000", "Xn", "100", "AmXn");
            Assign("bchg",  BCHGi, "0000100001", "AmXn");
            Assign("bchg",  BCHGr, "0000", "Xn", "101", "AmXn");
            Assign("bclr",  BCLRi, "0000100010", "AmXn");
            Assign("bclr",  BCLRr, "0000", "Xn", "110", "AmXn");
            Assign("bset",  BSETi, "0000100011", "AmXn");
            Assign("bset",  BSETr, "0000", "Xn", "111", "AmXn");
            Assign("link",  LINK,  "0100111001010", "Xn");
            Assign("unlk",  UNLK,  "0100111001011", "Xn");
            Assign("nop",   NOP,   "0100111001110001");

            Assign("add",   ADD0,  "1101", "Xn", "0", "Size2_1", "AmXn");
            Assign("add",   ADD1,  "1101", "Xn", "1", "Size2_1", "AmXn");
            Assign("adda",  ADDA,  "1101", "Xn", "Size1", "11", "AmXn");
            Assign("addi",  ADDI,  "00000110", "Size2_1", "AmXn");
            Assign("addq",  ADDQ,  "0101", "Data3", "0", "Size2_1", "AmXn");
            Assign("sub",   SUB0,  "1001", "Xn", "0", "Size2_1", "AmXn");
            Assign("sub",   SUB1,  "1001", "Xn", "1", "Size2_1", "AmXn");
            Assign("suba",  SUBA,  "1001", "Xn", "Size1", "11", "AmXn");
            Assign("subi",  SUBI,  "00000100", "Size2_1", "AmXn");
            Assign("subq",  SUBQ,  "0101", "Data3", "1", "Size2_1", "AmXn");
            Assign("cmp",   CMP,   "1011", "Xn", "0", "Size2_1", "AmXn");
            Assign("cmpm",  CMPM,  "1011", "Xn", "1", "Size2_1", "001", "Xn");
            Assign("cmpa",  CMPA,  "1011", "Xn", "Size1", "11", "AmXn");
            Assign("cmpi",  CMPI,  "00001100", "Size2_1", "AmXn");
            Assign("mulu",  MULU,  "1100", "Xn", "011", "AmXn");  // TODO accurate timing
            Assign("muls",  MULS,  "1100", "Xn", "111", "AmXn");  // TODO accurate timing
            Assign("divu",  DIVU,  "1000", "Xn", "011", "AmXn");  // TODO accurate timing
            Assign("divs",  DIVS,  "1000", "Xn", "111", "AmXn");  // TODO accurate timing

            Assign("move2sr", MOVEtSR, "0100011011", "AmXn");
            Assign("movefsr", MOVEfSR, "0100000011", "AmXn");
            Assign("moveusp", MOVEUSP, "010011100110", "Data1", "Xn");
            Assign("andi2sr", ANDI_SR, "0000001001111100");
            Assign("eori2sr", EORI_SR, "0000101001111100");
            Assign("ori2sr",  ORI_SR,  "0000000001111100");
            Assign("moveccr", MOVECCR, "0100010011", "AmXn");
            Assign("trap",    TRAP,    "010011100100", "Data4");
        }

        void Assign(string instr, Action exec, string root, params string[] bitfield)
        {
            List<string> opList = new List<string>();
            opList.Add(root);

            foreach (var component in bitfield)
            {
                     if (component.IsBinary())             AppendConstant(opList, component);
                else if (component == "Size1")    opList = AppendPermutations(opList, Size1);
                else if (component == "Size2_0")  opList = AppendPermutations(opList, Size2_0);
                else if (component == "Size2_1")  opList = AppendPermutations(opList, Size2_1);
                else if (component == "XnAm")     opList = AppendPermutations(opList, Xn3Am3);
                else if (component == "AmXn")     opList = AppendPermutations(opList, Am3Xn3);
                else if (component == "Xn")       opList = AppendPermutations(opList, Xn3);
                else if (component == "CondMain") opList = AppendPermutations(opList, ConditionMain);
                else if (component == "CondAll")  opList = AppendPermutations(opList, ConditionAll);
                else if (component == "Data1")    opList = AppendData(opList, 1);
                else if (component == "Data4")    opList = AppendData(opList, 4);
                else if (component == "Data3")    opList = AppendData(opList, 3);
                else if (component == "Data8")    opList = AppendData(opList, 8);
            }

            foreach (var opcode in opList)
            {
                int opc = Convert.ToInt32(opcode, 2);
                if (Opcodes[opc] != null && instr.NotIn("movea","andi2sr","eori2sr","ori2sr","ext","dbcc","swap","cmpm"))
                    Console.WriteLine("Setting opcode for {0}, a handler is already set. overwriting. {1:X4}", instr, opc);
                Opcodes[opc] = exec;
            }
        }

        void AppendConstant(List<string> ops, string constant)
        {
            for (int i=0; i<ops.Count; i++)
                ops[i] = ops[i] + constant;
        }

        List<string> AppendPermutations(List<string> ops, string[] permutations)
        {
            List<string> output = new List<string>();

            foreach (var input in ops)
                foreach (var perm in permutations)
                    output.Add(input + perm);

            return output;
        }

        List<string> AppendData(List<string> ops, int bits)
        {
            List<string> output = new List<string>();
            
            foreach (var input in ops)
                for (int i = 0; i < BinaryExp(bits); i++)
                    output.Add(input+Convert.ToString(i, 2).PadLeft(bits, '0'));

            return output;
        }

        int BinaryExp(int bits)
        {
            int res = 1;
            for (int i = 0; i < bits; i++)
                res *= 2;
            return res;
        }

        #region Tables

        static readonly string[] Size2_0 = {"01", "11", "10"};
        static readonly string[] Size2_1 = {"00", "01", "10"};
        static readonly string[] Size1   = {"0", "1" };
        static readonly string[] Xn3     = {"000","001","010","011","100","101","110","111"};

        static readonly string[] Xn3Am3 = {
            "000000", // Dn   Data register
            "001000",
            "010000",
            "011000",
            "100000",
            "101000",
            "110000",
            "111000",

            "000001", // An    Address register
            "001001",
            "010001",
            "011001",
            "100001",
            "101001",
            "110001",
            "111001",

            "000010", // (An) Address
            "001010",
            "010010",
            "011010",
            "100010",
            "101010",
            "110010",
            "111010",

            "000011", // (An)+ Address with Postincrement
            "001011",
            "010011",
            "011011",
            "100011",
            "101011",
            "110011",
            "111011",

            "000100", // -(An) Address with Predecrement
            "001100",
            "010100",
            "011100",
            "100100",
            "101100",
            "110100",
            "111100",

            "000101", // (d16, An) Address with Displacement
            "001101",
            "010101",
            "011101",
            "100101",
            "101101",
            "110101",
            "111101",

            "000110", // (d8, An, Xn) Address with Index
            "001110",
            "010110",
            "011110",
            "100110",
            "101110",
            "110110",
            "111110",

            "010111", // (d16, PC)     PC with Displacement
            "011111", // (d8, PC, Xn)  PC with Index
            "000111", // (xxx).W       Absolute Short
            "001111", // (xxx).L       Absolute Long
            "100111", // #imm          Immediate            
        };        

        static readonly string[] Am3Xn3 = {
            "000000", // Dn   Data register
            "000001",
            "000010",
            "000011",
            "000100",
            "000101",
            "000110",
            "000111",

            "001000", // An    Address register
            "001001",
            "001010",
            "001011",
            "001100",
            "001101",
            "001110",
            "001111",

            "010000", // (An) Address
            "010001",
            "010010",
            "010011",
            "010100",
            "010101",
            "010110",
            "010111",

            "011000", // (An)+ Address with Postincrement
            "011001",
            "011010",
            "011011",
            "011100",
            "011101",
            "011110",
            "011111",

            "100000", // -(An) Address with Predecrement
            "100001",
            "100010",
            "100011",
            "100100",
            "100101",
            "100110",
            "100111",

            "101000", // (d16, An) Address with Displacement
            "101001",
            "101010",
            "101011",
            "101100",
            "101101",
            "101110",
            "101111",

            "110000", // (d8, An, Xn) Address with Index
            "110001",
            "110010",
            "110011",
            "110100",
            "110101",
            "110110",
            "110111",

            "111010", // (d16, PC)     PC with Displacement
            "111011", // (d8, PC, Xn)  PC with Index
            "111000", // (xxx).W       Absolute Short
            "111001", // (xxx).L       Absolute Long
            "111100", // #imm          Immediate            
        };

        static readonly string[] ConditionMain = {
            "0010", // HI  Higher (unsigned)
            "0011", // LS  Lower or Same (unsigned)
            "0100", // CC  Carry Clear (aka Higher or Same, unsigned)
            "0101", // CS  Carry Set (aka Lower, unsigned)
            "0110", // NE  Not Equal
            "0111", // EQ  Equal
            "1000", // VC  Overflow Clear
            "1001", // VS  Overflow Set
            "1010", // PL  Plus
            "1011", // MI  Minus
            "1100", // GE  Greater or Equal (signed)
            "1101", // LT  Less Than (signed)
            "1110", // GT  Greater Than (signed)
            "1111"  // LE  Less or Equal (signed)
        };

        static readonly string[] ConditionAll = {
            "0000", // T   True 
            "0001", // F   False            
            "0010", // HI  Higher (unsigned)
            "0011", // LS  Lower or Same (unsigned)
            "0100", // CC  Carry Clear (aka Higher or Same, unsigned)
            "0101", // CS  Carry Set (aka Lower, unsigned)
            "0110", // NE  Not Equal
            "0111", // EQ  Equal
            "1000", // VC  Overflow Clear
            "1001", // VS  Overflow Set
            "1010", // PL  Plus
            "1011", // MI  Minus
            "1100", // GE  Greater or Equal (signed)
            "1101", // LT  Less Than (signed)
            "1110", // GT  Greater Than (signed)
            "1111"  // LE  Less or Equal (signed)
        };

        #endregion
    }
}