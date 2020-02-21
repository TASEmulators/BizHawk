//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2008-2015 Avery Lee
//	CPU emulation library - state definitions
//
//	This program is free software; you can redistribute it and/or modify
//	it under the terms of the GNU General Public License as published by
//	the Free Software Foundation; either version 2 of the License, or
//	(at your option) any later version.
//
//	This program is distributed in the hope that it will be useful,
//	but WITHOUT ANY WARRANTY; without even the implied warranty of
//	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//	GNU General Public License for more details.
//
//	You should have received a copy of the GNU General Public License
//	along with this program; if not, write to the Free Software
//	Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.

#ifndef f_AT_ATCPU_STATESZ80_H
#define f_AT_ATCPU_STATESZ80_H

namespace ATCPUStatesZ80 {
	enum ATCPUStateZ80 {
		kZ80StateNop,
		kZ80StateReadOpcode,
		kZ80StateReadOpcodeNoBreak,
		kZ80StateReadOpcodeCB,
		kZ80StateReadOpcodeED,
		kZ80StateReadOpcodeDD,
		kZ80StateReadOpcodeFD,
		kZ80StateReadOpcodeDDFDCB,

		kZ80StateRegenerateDecodeTables,
		kZ80StateAddToHistory,
		kZ80StateBreakOnUnsupportedOpcode,

		kZ80StateWait_1T,
		kZ80StateWait_2T,
		kZ80StateWait_3T,
		kZ80StateWait_4T,
		kZ80StateWait_5T,
		kZ80StateWait_7T,
		kZ80StateWait_8T,
		kZ80StateWait_11T,

		kZ80StateReadImm,
		kZ80StateReadImmAddr,
		kZ80StateRead,
		kZ80StateReadL,
		kZ80StateReadH,
		kZ80StateReadIXdToAddr,

		kZ80StateWrite,
		kZ80StateWriteL,
		kZ80StateWriteH,

		kZ80StateReadPort,
		kZ80StateReadPortC,
		kZ80StateWritePort,
		kZ80StateWritePortC,
		
		kZ80StatePush,
		kZ80StatePop,

		kZ80State0ToData,
		kZ80StateAToData,
		kZ80StateBToData,
		kZ80StateCToData,
		kZ80StateDToData,
		kZ80StateEToData,
		kZ80StateHToData,
		kZ80StateLToData,
		kZ80StateIXHToData,
		kZ80StateIXLToData,
		kZ80StateDataToA,
		kZ80StateDataToB,
		kZ80StateDataToC,
		kZ80StateDataToD,
		kZ80StateDataToE,
		kZ80StateDataToH,
		kZ80StateDataToL,
		kZ80StateDataToIXH,
		kZ80StateDataToIXL,

		kZ80StateAFToData,
		kZ80StateBCToData,
		kZ80StateDEToData,
		kZ80StateHLToData,
		kZ80StateIXToData,
		kZ80StateSPToData,
		kZ80StatePCToData,
		kZ80StatePCp2ToData,

		kZ80StateBCToAddr,
		kZ80StateDEToAddr,
		kZ80StateHLToAddr,
		kZ80StateSPToAddr,

		kZ80StateDataToAF,
		kZ80StateDataToBC,
		kZ80StateDataToDE,
		kZ80StateDataToHL,
		kZ80StateDataToIX,
		kZ80StateDataToSP,
		kZ80StateDataToPC,

		kZ80StateAddrToPC,

		kZ80StateIToA_1T,
		kZ80StateRToA_1T,
		kZ80StateAToI_1T,
		kZ80StateAToR_1T,

		kZ80StateExaf,
		kZ80StateExx,
		kZ80StateExDEHL,
		kZ80StateExHLData,
		kZ80StateExIXData,

		kZ80StateAddToA,
		kZ80StateAdcToA,
		kZ80StateSubToA,
		kZ80StateSbcToA,
		kZ80StateCpToA,
		kZ80StateDec,
		kZ80StateInc,
		kZ80StateAndToA,
		kZ80StateOrToA,
		kZ80StateXorToA,

		kZ80StateDec16,
		kZ80StateInc16,

		kZ80StateRlca,
		kZ80StateRla,
		kZ80StateRrca,
		kZ80StateRra,

		kZ80StateRld,
		kZ80StateRrd,

		kZ80StateRlc,
		kZ80StateRl,
		kZ80StateRrc,
		kZ80StateRr,
		kZ80StateSla,
		kZ80StateSrl,
		kZ80StateSra,

		kZ80StateCplToA,
		kZ80StateNegA,
		kZ80StateDaa,

		kZ80StateBit0,
		kZ80StateBit1,
		kZ80StateBit2,
		kZ80StateBit3,
		kZ80StateBit4,
		kZ80StateBit5,
		kZ80StateBit6,
		kZ80StateBit7,

		kZ80StateSet0,
		kZ80StateSet1,
		kZ80StateSet2,
		kZ80StateSet3,
		kZ80StateSet4,
		kZ80StateSet5,
		kZ80StateSet6,
		kZ80StateSet7,

		kZ80StateRes0,
		kZ80StateRes1,
		kZ80StateRes2,
		kZ80StateRes3,
		kZ80StateRes4,
		kZ80StateRes5,
		kZ80StateRes6,
		kZ80StateRes7,

		kZ80StateCCF,
		kZ80StateSCF,

		kZ80StateAddToHL,
		kZ80StateAdcToHL,
		kZ80StateSbcToHL,

		kZ80StateAddToIX,

		kZ80StateDI,
		kZ80StateEI,
		kZ80StateRetn,
		kZ80StateReti,
		kZ80StateHaltEnter,
		kZ80StateHalt,
		kZ80StateIM0_4T,
		kZ80StateIM1_4T,
		kZ80StateIM2_4T,

		kZ80StateRst00,
		kZ80StateRst08,
		kZ80StateRst10,
		kZ80StateRst18,
		kZ80StateRst20,
		kZ80StateRst28,
		kZ80StateRst30,
		kZ80StateRst38,
		kZ80StateRst66,
		kZ80StateRstIntVec,

		kZ80StateJP,
		kZ80StateJR,
		kZ80StateStep1I,
		kZ80StateStep1D,
		kZ80StateStep1I_IO,
		kZ80StateStep1D_IO,
		kZ80StateStep2I,
		kZ80StateStep2D,
		kZ80StateRep,
		kZ80StateRep_IO,
		kZ80StateRepNZ,
		kZ80StateSkipUnlessNZ,
		kZ80StateSkipUnlessZ,
		kZ80StateSkipUnlessNC,
		kZ80StateSkipUnlessC,
		kZ80StateSkipUnlessPO,
		kZ80StateSkipUnlessPE,
		kZ80StateSkipUnlessP,
		kZ80StateSkipUnlessM,
		kZ80StateDjnz,

		kZ80StateCount
	};
}

#endif
