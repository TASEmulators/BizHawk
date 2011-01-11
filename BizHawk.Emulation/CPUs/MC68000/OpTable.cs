using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace MC68000
{
	internal delegate void Operation();

	public partial class MC68K
	{
		private Operation[] m_OpTable = new Operation[0x10000];

		private void BuildOpTable()
		{
			// First, define regular expressions for each operation
			Dictionary<Operation, Regex> opIndex = new Dictionary<Operation, Regex>();

			// Data Movement
			opIndex.Add(new Operation(EXG), new Regex("1100" + "[0-1]{3}" + "1" + "(01000|01001|10001)" + "[0-1]{3}"));
			opIndex.Add(new Operation(LEA), new Regex("0100" + "[0-1]{3}" + "111" + "(((010|101|110)[0-1]{3})|(111(000|001|010|011)))"));
			opIndex.Add(new Operation(LINK), new Regex("0100111001010" + "[0-1]{3}"));
			opIndex.Add(new Operation(MOVE), new Regex("00" + "(01|11|10)" + "(([0-1]{3}(000|010|011|100|101|110))|((000|001)111))" + "(((000|001|010|011|100|101|110)[0-1]{3})|(111(000|001|100|010|011)))"));
			opIndex.Add(new Operation(MOVEA), new Regex("00" + "(11|10)" + "[0-1]{3}" + "001" + "(((000|001|010|011|100|101|110)[0-1]{3})|(111(000|001|100|010|011)))"));
			opIndex.Add(new Operation(MOVEM_Mem2Reg), new Regex("01001" + "1" + "001" + "[0-1]" + "((010|011|101|110)[0-1]{3}|(111(000|001|010|011)))"));
			opIndex.Add(new Operation(MOVEM_Reg2Mem), new Regex("01001" + "0" + "001" + "[0-1]" + "((010|100|101|110)[0-1]{3}|(111(000|001)))"));
			opIndex.Add(new Operation(MOVEP), new Regex("0000" + "[0-1]{3}" + "(100|101|110|111)" + "001" + "[0-1]{3}"));
			opIndex.Add(new Operation(MOVEQ), new Regex("0111" + "[0-1]{3}" + "0" + "[0-1]{8}"));
			opIndex.Add(new Operation(PEA), new Regex("0100100001" + "(((010|101|110)[0-1]{3})|(111(000|001|010|011)))"));
			opIndex.Add(new Operation(UNLK), new Regex("0100111001011" + "[0-1]{3}"));

			// Integer Arithmetic
			opIndex.Add(new Operation(ADD_Dest), new Regex("1101" + "[0-1]{3}" + "1" + "(00|01|10)" + "(((010|011|100|101|110)[0-1]{3})|(111(000|001)))"));
			opIndex.Add(new Operation(ADD_Source), new Regex("1101" + "[0-1]{3}" + "0" + "(00|01|10)" + "(((000|001|010|011|100|101|110)[0-1]{3})|(111(000|001|100|010|011)))"));
			opIndex.Add(new Operation(ADDA), new Regex("1101" + "[0-1]{3}" + "(011|111)" + "(((000|001|010|011|100|101|110)[0-1]{3})|(111(000|001|100|010|011)))"));
			opIndex.Add(new Operation(ADDI), new Regex("00000110" + "(00|01|10)" + "(((000|010|011|100|101|110)[0-1]{3})|(111(000|001)))"));
			opIndex.Add(new Operation(ADDQ), new Regex("0101" + "[0-1]{3}" + "0" + "(00|01|10)" + "(((000|001|010|011|100|101|110)[0-1]{3})|(111(000|001)))"));
			opIndex.Add(new Operation(ADDX), new Regex("1101" + "[0-1]{3}" + "1" + "(00|01|10)" + "00" + "[0-1]" + "[0-1]{3}"));
			opIndex.Add(new Operation(CLR), new Regex("01000010" + "(00|01|10)" + "(((000|010|011|100|101|110)[0-1]{3})|(111(000|001)))"));
			opIndex.Add(new Operation(CMP), new Regex("1011" + "[0-1]{3}" + "(000|001|010)" + "(((000|001|010|011|100|101|110)[0-1]{3})|(111(000|001|100|010|011)))"));
			opIndex.Add(new Operation(CMPA), new Regex("1011" + "[0-1]{3}" + "(011|111)" + "(((000|001|010|011|100|101|110)[0-1]{3})|(111(000|001|100|010|011)))"));
			opIndex.Add(new Operation(CMPI), new Regex("00001100" + "(00|01|10)" + "(((000|010|011|100|101|110)[0-1]{3})|(111(000|001|010|011)))"));
			opIndex.Add(new Operation(CMPM), new Regex("1011" + "[0-1]{3}" + "1" + "(00|01|10)" + "001" + "[0-1]{3}"));
			opIndex.Add(new Operation(DIVS), new Regex("1000" + "[0-1]{3}" + "111" + "(((000|010|011|100|101|110)[0-1]{3})|(111(000|001|100|010|011)))"));
			opIndex.Add(new Operation(DIVU), new Regex("1000" + "[0-1]{3}" + "011" + "(((000|010|011|100|101|110)[0-1]{3})|(111(000|001|100|010|011)))"));
			opIndex.Add(new Operation(EXT), new Regex("0100100" + "(010|011|111)" + "000" + "[0-1]{3}"));
			opIndex.Add(new Operation(MULS), new Regex("1100" + "[0-1]{3}" + "111" + "(((000|010|011|100|101|110)[0-1]{3})|(111(000|001|100|010|011)))"));
			opIndex.Add(new Operation(MULU), new Regex("1100" + "[0-1]{3}" + "011" + "(((000|010|011|100|101|110)[0-1]{3})|(111(000|001|100|010|011)))"));
			opIndex.Add(new Operation(NEG), new Regex("01000100" + "(00|01|10)" + "(((000|010|011|100|101|110)[0-1]{3})|(111(000|001)))"));
			opIndex.Add(new Operation(NEGX), new Regex("01000000" + "(00|01|10)" + "(((000|010|011|100|101|110)[0-1]{3})|(111(000|001)))"));
			opIndex.Add(new Operation(SUB_Dest), new Regex("1001" + "[0-1]{3}" + "1" + "(00|01|10)" + "(((010|011|100|101|110)[0-1]{3})|(111(000|001)))"));
			opIndex.Add(new Operation(SUB_Source), new Regex("1001" + "[0-1]{3}" + "0" + "(00|01|10)" + "(((000|001|010|011|100|101|110)[0-1]{3})|(111(000|001|100|010|011)))"));
			opIndex.Add(new Operation(SUBA), new Regex("1001" + "[0-1]{3}" + "(011|111)" + "(((000|001|010|011|100|101|110)[0-1]{3})|(111(000|001|100|010|011)))"));
			opIndex.Add(new Operation(SUBI), new Regex("00000100" + "(00|01|10)" + "(((000|010|011|100|101|110)[0-1]{3})|(111(000|001)))"));
			opIndex.Add(new Operation(SUBQ), new Regex("0101" + "[0-1]{3}" + "1" + "(00|01|10)" + "(((000|001|010|011|100|101|110)[0-1]{3})|(111(000|001)))"));
			opIndex.Add(new Operation(SUBX), new Regex("1001" + "[0-1]{3}" + "1" + "(00|01|10)" + "00" + "[0-1]" + "[0-1]{3}"));

			// Logical
			opIndex.Add(new Operation(AND_Dest), new Regex("1100" + "[0-1]{3}" + "1" + "(00|01|10)" + "(((010|011|100|101|110)[0-1]{3})|(111(000|001)))"));
			opIndex.Add(new Operation(AND_Source), new Regex("1100" + "[0-1]{3}" + "0" + "(00|01|10)" + "(((000|010|011|100|101|110)[0-1]{3})|(111(000|001|100|010|011)))"));
			opIndex.Add(new Operation(ANDI), new Regex("00000010" + "(00|01|10)" + "(((000|010|011|100|101|110)[0-1]{3})|(111(000|001)))"));
			opIndex.Add(new Operation(EOR), new Regex("1011" + "[0-1]{3}" + "(100|101|110)" + "(((000|010|011|100|101|110)[0-1]{3})|(111(000|001)))"));
			opIndex.Add(new Operation(EORI), new Regex("00001010" + "(00|01|10)" + "(((000|010|011|100|101|110)[0-1]{3})|(111(000|001)))"));
			opIndex.Add(new Operation(OR_Dest), new Regex("1000" + "[0-1]{3}" + "1" + "(00|01|10)" + "(((010|011|100|101|110)[0-1]{3})|(111(000|001)))"));
			opIndex.Add(new Operation(OR_Source), new Regex("1000" + "[0-1]{3}" + "0" + "(00|01|10)" + "(((000|010|011|100|101|110)[0-1]{3})|(111(000|001|100|010|011)))"));
			opIndex.Add(new Operation(ORI), new Regex("00000000" + "(00|01|10)" + "(((000|010|011|100|101|110)[0-1]{3})|(111(000|001)))"));
			opIndex.Add(new Operation(NOT), new Regex("01000110" + "(00|01|10)" + "(((000|010|011|100|101|110)[0-1]{3})|(111(000|001)))"));

			// Shift and Rotate
			opIndex.Add(new Operation(ASL), new Regex("1110" + "[0-1]{3}" + "1" + "(00|01|10)" + "[0-1]" + "00" + "[0-1]{3}"));
			opIndex.Add(new Operation(ASR), new Regex("1110" + "[0-1]{3}" + "0" + "(00|01|10)" + "[0-1]" + "00" + "[0-1]{3}"));
			opIndex.Add(new Operation(ASL_ASR_Memory), new Regex("1110000" + "[0-1]" + "11" + "(((010|011|100|101|110)[0-1]{3})|(111(000|001)))"));
			opIndex.Add(new Operation(LSL), new Regex("1110" + "[0-1]{3}" + "1" + "(00|01|10)" + "[0-1]" + "01" + "[0-1]{3}"));
			opIndex.Add(new Operation(LSR), new Regex("1110" + "[0-1]{3}" + "0" + "(00|01|10)" + "[0-1]" + "01" + "[0-1]{3}"));
			opIndex.Add(new Operation(LSL_LSR_Memory), new Regex("1110001" + "[0-1]" + "11" + "(((010|011|100|101|110)[0-1]{3})|(111(000|001)))"));
			opIndex.Add(new Operation(ROL), new Regex("1110" + "[0-1]{3}" + "1" + "(00|01|10)" + "[0-1]" + "11" + "[0-1]{3}"));
			opIndex.Add(new Operation(ROR), new Regex("1110" + "[0-1]{3}" + "0" + "(00|01|10)" + "[0-1]" + "11" + "[0-1]{3}"));
			opIndex.Add(new Operation(ROL_ROR_Memory), new Regex("1110011" + "[0-1]" + "11" + "(((010|011|100|101|110)[0-1]{3})|(111(000|001)))"));
			opIndex.Add(new Operation(ROXL), new Regex("1110" + "[0-1]{3}" + "1" + "(00|01|10)" + "[0-1]" + "10" + "[0-1]{3}"));
			opIndex.Add(new Operation(ROXR), new Regex("1110" + "[0-1]{3}" + "0" + "(00|01|10)" + "[0-1]" + "10" + "[0-1]{3}"));
			opIndex.Add(new Operation(ROXL_ROXR_Memory), new Regex("1110010" + "[0-1]" + "11" + "(((010|011|100|101|110)[0-1]{3})|(111(000|001)))"));
			opIndex.Add(new Operation(SWAP), new Regex("0100100001000" + "[0-1]{3}"));

			// Bit Manipulation
			opIndex.Add(new Operation(BTST_Dynamic), new Regex("0000" + "[0-1]{3}" + "100" + "(((000|010|011|100|101|110)[0-1]{3})|(111(000|001|100|010|011)))"));
			opIndex.Add(new Operation(BTST_Static), new Regex("0000100000" + "(((000|010|011|100|101|110)[0-1]{3})|(111(000|001|010|011)))"));

			opIndex.Add(new Operation(BSET_Dynamic), new Regex("0000" + "[0-1]{3}" + "111" + "(((000|010|011|100|101|110)[0-1]{3})|(111(000|001)))"));
			opIndex.Add(new Operation(BSET_Static), new Regex("0000100011" + "(((000|010|011|100|101|110)[0-1]{3})|(111(000|001)))"));

			opIndex.Add(new Operation(BCLR_Dynamic), new Regex("0000" + "[0-1]{3}" + "110" + "(((000|010|011|100|101|110)[0-1]{3})|(111(000|001)))"));
			opIndex.Add(new Operation(BCLR_Static), new Regex("0000100010" + "(((000|010|011|100|101|110)[0-1]{3})|(111(000|001)))"));

			opIndex.Add(new Operation(BCHG_Dynamic), new Regex("0000" + "[0-1]{3}" + "101" + "(((000|010|011|100|101|110)[0-1]{3})|(111(000|001)))"));
			opIndex.Add(new Operation(BCHG_Static), new Regex("0000100001" + "(((000|010|011|100|101|110)[0-1]{3})|(111(000|001)))"));

			// Program Control
			opIndex.Add(new Operation(Bcc), new Regex("0110" + "(001|010|011|100|101|110|111)[0-1]" + "[0-1]{8}"));
			opIndex.Add(new Operation(DBcc), new Regex("0101" + "[0-1]{4}" + "11001" + "[0-1]{3}"));
			opIndex.Add(new Operation(Scc), new Regex("0101" + "[0-1]{4}" + "11" + "(((000|010|011|100|101|110)[0-1]{3})|(111(000|001)))"));

			opIndex.Add(new Operation(BRA), new Regex("01100000" + "[0-1]{8}"));
			opIndex.Add(new Operation(BSR), new Regex("01100001" + "[0-1]{8}"));
			opIndex.Add(new Operation(JMP), new Regex("0100111011" + "(((010|101|110)[0-1]{3})|(111(000|001|010|011)))"));
			opIndex.Add(new Operation(JSR), new Regex("0100111010" + "(((010|101|110)[0-1]{3})|(111(000|001|010|011)))"));
			opIndex.Add(new Operation(NOP), new Regex("0100111001110001"));

			opIndex.Add(new Operation(RTS), new Regex("0100111001110101"));

			opIndex.Add(new Operation(TST), new Regex("01001010" + "(00|01|10)" + "(((000|010|011|100|101|110)[0-1]{3})|(111(000|001)))"));
		
			// System Control
			opIndex.Add(new Operation(ANDI_to_CCR), new Regex("0000001000111100"));
			opIndex.Add(new Operation(ANDI_to_SR), new Regex("0000001001111100"));
			opIndex.Add(new Operation(CHK), new Regex("0100" + "[0-1]{3}" + "(11|10)" + "0" + "(((000|010|011|100|101|110)[0-1]{3})|(111(000|001|100|010|011)))"));
			opIndex.Add(new Operation(EORI_to_CCR), new Regex("0000101000111100"));
			opIndex.Add(new Operation(EORI_to_SR), new Regex("0000101001111100"));
			opIndex.Add(new Operation(ILLEGAL), new Regex("0100101011111100"));
			opIndex.Add(new Operation(MOVE_from_SR), new Regex("0100000011" + "(((000|010|011|100|101|110)[0-1]{3})|(111(000|001)))"));
			opIndex.Add(new Operation(MOVE_to_CCR), new Regex("0100010011" + "(((000|010|011|100|101|110)[0-1]{3})|(111(000|001|100|010|011)))"));
			opIndex.Add(new Operation(MOVE_to_SR), new Regex("0100011011" + "(((000|010|011|100|101|110)[0-1]{3})|(111(000|001|100|010|011)))"));
			opIndex.Add(new Operation(MOVE_USP), new Regex("010011100110" + "[0-1]" + "[0-1]{3}"));
			opIndex.Add(new Operation(ORI_to_CCR), new Regex("0000000000111100"));
			opIndex.Add(new Operation(ORI_to_SR), new Regex("0000000001111100"));
			opIndex.Add(new Operation(RESET), new Regex("0100111001110000"));
			opIndex.Add(new Operation(RTE), new Regex("0100111001110011"));
			opIndex.Add(new Operation(RTR), new Regex("0100111001110111"));
			opIndex.Add(new Operation(STOP), new Regex("0100111001110010"));
			opIndex.Add(new Operation(TRAP), new Regex("010011100100" + "[0-1]{4}"));
			opIndex.Add(new Operation(TRAPV), new Regex("0100111001110110"));

			// Now, run through every possible 16-bit binary number,
			// find the matching expression, and add that code to the table
			for (int i = 0; i < 0x10000; i++)
			{
				string binaryString = Convert.ToString(i, 2);
				binaryString = binaryString.PadLeft(16, '0');
				Dictionary<Operation, Regex>.Enumerator enumerator = opIndex.GetEnumerator();
				while (enumerator.MoveNext())
				{
					if (enumerator.Current.Value.IsMatch(binaryString))
					{
						if (this.m_OpTable[i] != null)
						{
							throw new Exception("Two operations with clashing codes!");
						}
						this.m_OpTable[i] = enumerator.Current.Key;
					}
				}
			}
		}
	}
}
