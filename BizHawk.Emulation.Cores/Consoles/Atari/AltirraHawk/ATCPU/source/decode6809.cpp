//	Altirra - Atari 800/800XL emulator
//	Copyright (C) 2008-2016 Avery Lee
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

#include <stdafx.h>
#include <at/atcpu/decode6809.h>
#include <at/atcpu/states6809.h>

void ATCPUDecoderGenerator6809::RebuildTables(ATCPUDecoderTables6809& dst, bool stopOnBRK, bool historyTracing, bool enableBreakpoints) {
	using namespace ATCPUStates6809;

	mbStopOnBRK = stopOnBRK;
	mpDstState = dst.mDecodeHeap;

	dst.mIrqSequence = 0;
	*mpDstState++ = k6809StateIntEntryE1;
	*mpDstState++ = k6809StatePushRegMaskS_1;
	dst.mCwaiIrqSequence = (uint16)(mpDstState - dst.mDecodeHeap);
	*mpDstState++ = k6809StateBeginIrq;
	*mpDstState++ = k6809StateReadByteHi_1;
	*mpDstState++ = k6809StateReadByteLo_1;
	*mpDstState++ = k6809StateDataToPC;
	*mpDstState++ = enableBreakpoints ? k6809StateReadOpcode : k6809StateReadOpcodeNoBreak;

	dst.mFirqSequence = (uint16)(mpDstState - dst.mDecodeHeap);
	*mpDstState++ = k6809StateIntEntryE0;
	*mpDstState++ = k6809StatePushRegMaskS_1;
	dst.mCwaiFirqSequence = (uint16)(mpDstState - dst.mDecodeHeap);
	*mpDstState++ = k6809StateBeginFirq;
	*mpDstState++ = k6809StateReadByteHi_1;
	*mpDstState++ = k6809StateReadByteLo_1;
	*mpDstState++ = k6809StateDataToPC;
	*mpDstState++ = enableBreakpoints ? k6809StateReadOpcode : k6809StateReadOpcodeNoBreak;

	dst.mNmiSequence = (uint16)(mpDstState - dst.mDecodeHeap);
	*mpDstState++ = k6809StateIntEntryE1;
	*mpDstState++ = k6809StatePushRegMaskS_1;
	dst.mCwaiNmiSequence = (uint16)(mpDstState - dst.mDecodeHeap);
	*mpDstState++ = k6809StateBeginNmi;
	*mpDstState++ = k6809StateReadByteHi_1;
	*mpDstState++ = k6809StateReadByteLo_1;
	*mpDstState++ = k6809StateDataToPC;
	*mpDstState++ = enableBreakpoints ? k6809StateReadOpcode : k6809StateReadOpcodeNoBreak;

	DecodeInsns(dst, dst.mInsns, &ATCPUDecoderGenerator6809::DecodeInsn, historyTracing, enableBreakpoints);
	DecodeInsns(dst, dst.mInsns10, &ATCPUDecoderGenerator6809::DecodeInsn10, false, enableBreakpoints);
	DecodeInsns(dst, dst.mInsns11, &ATCPUDecoderGenerator6809::DecodeInsn11, false, enableBreakpoints);

	// 00000: ,R+ (3 cycles)
	dst.mIndexedModes[0] = (uint16)(mpDstState - dst.mDecodeHeap);
	*mpDstState++ = k6809StateIndexedPostInc;
	*mpDstState++ = k6809StateWait_1;
	*mpDstState++ = k6809StateIndexedRts;

	// 00001: ,R++ (4 cycles)
	dst.mIndexedModes[1] = (uint16)(mpDstState - dst.mDecodeHeap);
	*mpDstState++ = k6809StateIndexedPostInc;
	*mpDstState++ = k6809StateIndexedPostInc;
	*mpDstState++ = k6809StateWait_1;
	*mpDstState++ = k6809StateIndexedRts;

	// 00010: ,-R (3 cycles)
	dst.mIndexedModes[2] = (uint16)(mpDstState - dst.mDecodeHeap);
	*mpDstState++ = k6809StateIndexedPreDec;
	*mpDstState++ = k6809StateWait_1;
	*mpDstState++ = k6809StateIndexedRts;

	// 00011: ,--R (4 cycles)
	dst.mIndexedModes[3] = (uint16)(mpDstState - dst.mDecodeHeap);
	*mpDstState++ = k6809StateIndexedPreDec;
	*mpDstState++ = k6809StateIndexedPreDec;
	*mpDstState++ = k6809StateWait_1;
	*mpDstState++ = k6809StateIndexedRts;

	// 00100: ,R (1 cycle)
	dst.mIndexedModes[4] = (uint16)(mpDstState - dst.mDecodeHeap);
	*mpDstState++ = k6809StateIndexedRts;

	// 00101: B,R (2 cycles)
	dst.mIndexedModes[5] = (uint16)(mpDstState - dst.mDecodeHeap);
	*mpDstState++ = k6809StateIndexedB;
	*mpDstState++ = k6809StateIndexedRts;

	// 00110: A,R (2 cycles)
	dst.mIndexedModes[6] = (uint16)(mpDstState - dst.mDecodeHeap);
	*mpDstState++ = k6809StateIndexedA;
	*mpDstState++ = k6809StateIndexedRts;

	// 01000: d8,R (2 cycles)
	dst.mIndexedModes[8] = (uint16)(mpDstState - dst.mDecodeHeap);
	*mpDstState++ = k6809StateIndexed8BitOffset;
	*mpDstState++ = k6809StateIndexedRts;

	// 01001: d16,R (5 cycles)
	dst.mIndexedModes[9] = (uint16)(mpDstState - dst.mDecodeHeap);
	*mpDstState++ = k6809StateIndexed16BitOffsetHi;
	*mpDstState++ = k6809StateIndexed16BitOffsetLo_1;
	*mpDstState++ = k6809StateWait_1;
	*mpDstState++ = k6809StateWait_1;
	*mpDstState++ = k6809StateIndexedRts;

	// 01011: D,R (5 cycles)
	dst.mIndexedModes[11] = (uint16)(mpDstState - dst.mDecodeHeap);
	*mpDstState++ = k6809StateIndexedD;
	*mpDstState++ = k6809StateWait_1;
	*mpDstState++ = k6809StateWait_1;
	*mpDstState++ = k6809StateWait_1;
	*mpDstState++ = k6809StateIndexedRts;

	// 01100: d8,PCR (2 cycles)
	dst.mIndexedModes[12] = (uint16)(mpDstState - dst.mDecodeHeap);
	*mpDstState++ = k6809StateIndexed8BitOffsetPCR_1;
	*mpDstState++ = k6809StateIndexedRts;

	// 01101: d16,PCR (6 cycles)
	dst.mIndexedModes[13] = (uint16)(mpDstState - dst.mDecodeHeap);
	*mpDstState++ = k6809StateIndexed16BitOffsetPCRHi_1;
	*mpDstState++ = k6809StateIndexed16BitOffsetLo_1;
	*mpDstState++ = k6809StateWait_1;
	*mpDstState++ = k6809StateWait_1;
	*mpDstState++ = k6809StateWait_1;
	*mpDstState++ = k6809StateIndexedRts;

	// 10001: [,R++] (7 cycles)
	dst.mIndexedModes[17] = (uint16)(mpDstState - dst.mDecodeHeap);
	*mpDstState++ = k6809StateIndexedPostInc;
	*mpDstState++ = k6809StateIndexedPostInc;
	*mpDstState++ = k6809StateWait_1;
	*mpDstState++ = k6809StateWait_1;
	*mpDstState++ = k6809StateReadByteHi_1;
	*mpDstState++ = k6809StateIndexedIndirectLo;
	*mpDstState++ = k6809StateIndexedRts;

	// 10011: [,--R] (7 cycles)
	dst.mIndexedModes[19] = (uint16)(mpDstState - dst.mDecodeHeap);
	*mpDstState++ = k6809StateIndexedPreDec;
	*mpDstState++ = k6809StateIndexedPreDec;
	*mpDstState++ = k6809StateWait_1;
	*mpDstState++ = k6809StateWait_1;
	*mpDstState++ = k6809StateReadByteHi_1;
	*mpDstState++ = k6809StateIndexedIndirectLo;
	*mpDstState++ = k6809StateIndexedRts;

	// 10100: [,R] (4 cycles)
	dst.mIndexedModes[20] = (uint16)(mpDstState - dst.mDecodeHeap);
	*mpDstState++ = k6809StateWait_1;
	*mpDstState++ = k6809StateReadByteHi_1;
	*mpDstState++ = k6809StateIndexedIndirectLo;
	*mpDstState++ = k6809StateIndexedRts;

	// 10101: [B,R] (5 cycles)
	dst.mIndexedModes[21] = (uint16)(mpDstState - dst.mDecodeHeap);
	*mpDstState++ = k6809StateIndexedB;
	*mpDstState++ = k6809StateWait_1;
	*mpDstState++ = k6809StateReadByteHi_1;
	*mpDstState++ = k6809StateIndexedIndirectLo;
	*mpDstState++ = k6809StateIndexedRts;

	// 10110: [A,R] (5 cycles)
	dst.mIndexedModes[22] = (uint16)(mpDstState - dst.mDecodeHeap);
	*mpDstState++ = k6809StateIndexedA;
	*mpDstState++ = k6809StateWait_1;
	*mpDstState++ = k6809StateReadByteHi_1;
	*mpDstState++ = k6809StateIndexedIndirectLo;
	*mpDstState++ = k6809StateIndexedRts;

	// 11000: [d8,R] (5 cycles)
	dst.mIndexedModes[24] = (uint16)(mpDstState - dst.mDecodeHeap);
	*mpDstState++ = k6809StateIndexed8BitOffset;
	*mpDstState++ = k6809StateWait_1;
	*mpDstState++ = k6809StateReadByteHi_1;
	*mpDstState++ = k6809StateIndexedIndirectLo;
	*mpDstState++ = k6809StateIndexedRts;

	// 11001: [d16,R] (8 cycles)
	dst.mIndexedModes[25] = (uint16)(mpDstState - dst.mDecodeHeap);
	*mpDstState++ = k6809StateIndexed16BitOffsetHi;
	*mpDstState++ = k6809StateIndexed16BitOffsetLo_1;
	*mpDstState++ = k6809StateWait_1;
	*mpDstState++ = k6809StateWait_1;
	*mpDstState++ = k6809StateWait_1;
	*mpDstState++ = k6809StateReadByteHi_1;
	*mpDstState++ = k6809StateIndexedIndirectLo;
	*mpDstState++ = k6809StateIndexedRts;

	// 11011: [D,R] (8 cycles)
	dst.mIndexedModes[27] = (uint16)(mpDstState - dst.mDecodeHeap);
	*mpDstState++ = k6809StateIndexedD;
	*mpDstState++ = k6809StateWait_1;
	*mpDstState++ = k6809StateWait_1;
	*mpDstState++ = k6809StateWait_1;
	*mpDstState++ = k6809StateWait_1;
	*mpDstState++ = k6809StateReadByteHi_1;
	*mpDstState++ = k6809StateIndexedIndirectLo;
	*mpDstState++ = k6809StateIndexedRts;

	// 11100: [d8,PCR] (5 cycles)
	dst.mIndexedModes[28] = (uint16)(mpDstState - dst.mDecodeHeap);
	*mpDstState++ = k6809StateIndexed8BitOffsetPCR_1;
	*mpDstState++ = k6809StateWait_1;
	*mpDstState++ = k6809StateReadByteHi_1;
	*mpDstState++ = k6809StateIndexedIndirectLo;
	*mpDstState++ = k6809StateIndexedRts;

	// 11101: [d16,PCR] (9 cycles)
	dst.mIndexedModes[29] = (uint16)(mpDstState - dst.mDecodeHeap);
	*mpDstState++ = k6809StateIndexed16BitOffsetPCRHi_1;
	*mpDstState++ = k6809StateIndexed8BitOffset;
	*mpDstState++ = k6809StateWait_1;
	*mpDstState++ = k6809StateWait_1;
	*mpDstState++ = k6809StateWait_1;
	*mpDstState++ = k6809StateReadByteHi_1;
	*mpDstState++ = k6809StateIndexedIndirectLo;
	*mpDstState++ = k6809StateIndexedRts;
	
	// 11111: [nnnn] (6 cycles)
	dst.mIndexedModes[31] = (uint16)(mpDstState - dst.mDecodeHeap);
	*mpDstState++ = k6809StateReadAddrExtHi;
	*mpDstState++ = k6809StateReadAddrExtLo;
	*mpDstState++ = k6809StateWait_1;
	*mpDstState++ = k6809StateReadByteHi_1;
	*mpDstState++ = k6809StateIndexedIndirectLo;
	*mpDstState++ = k6809StateIndexedRts;

	// invalid codes (map to ,R or [,R])
	dst.mIndexedModes[7] = dst.mIndexedModes[4];
	dst.mIndexedModes[10] = dst.mIndexedModes[4];
	dst.mIndexedModes[14] = dst.mIndexedModes[4];
	dst.mIndexedModes[15] = dst.mIndexedModes[4];
	dst.mIndexedModes[18] = dst.mIndexedModes[20];
	dst.mIndexedModes[16] = dst.mIndexedModes[20];
	dst.mIndexedModes[23] = dst.mIndexedModes[20];
	dst.mIndexedModes[26] = dst.mIndexedModes[20];
	dst.mIndexedModes[30] = dst.mIndexedModes[20];

	// 5-bit offset (2 cycles)
	dst.mIndexedModes[32] = (uint16)(mpDstState - dst.mDecodeHeap);
	*mpDstState++ = k6809StateIndexed5BitOffset;
	*mpDstState++ = k6809StateIndexedRts;
}

void ATCPUDecoderGenerator6809::DecodeInsns(ATCPUDecoderTables6809& dst, uint16 *p, bool (ATCPUDecoderGenerator6809::*pfn)(uint8), bool historyTracing, bool enableBreakpoints) {
	using namespace ATCPUStates6809;

	const auto stateReadOpcode = enableBreakpoints ? k6809StateReadOpcode : k6809StateReadOpcodeNoBreak;

	for(int i=0; i<256; ++i) {
		*p++ = (uint16)(mpDstState - dst.mDecodeHeap);

		if (historyTracing)
			*mpDstState++ = k6809StateAddToHistory;

		if (!(this->*pfn)((uint8)i))
			*mpDstState++ = k6809StateBreakOnUnsupportedOpcode;

		*mpDstState++ = stateReadOpcode;
	}
}

bool ATCPUDecoderGenerator6809::DecodeInsn(uint8 opcode) {
	using namespace ATCPUStates6809;

	switch(opcode) {
		case 0x00:		// NEG direct
			DecodeAddrDirect_2();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateNeg_HNZVC_1;
			*mpDstState++ = k6809StateWriteByte_1;
			return true;

		case 0x03:		// COM direct
			DecodeAddrDirect_2();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateCom_NZVC_1;
			*mpDstState++ = k6809StateWriteByte_1;
			return true;

		case 0x04:		// LSR direct (6)
			DecodeAddrDirect_2();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateLsr_NZC_1;
			*mpDstState++ = k6809StateWriteByte_1;
			return true;

		case 0x06:		// ROR direct (6)
			DecodeAddrDirect_2();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateRor_NZC_1;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateWriteByte_1;
			return true;

		case 0x07:		// ASR direct
			DecodeAddrDirect_2();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateAsr_HNZC_1;
			*mpDstState++ = k6809StateWriteByte_1;
			return true;

		case 0x08:		// ASL direct
			DecodeAddrDirect_2();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateAsl_HNZVC_1;
			*mpDstState++ = k6809StateWriteByte_1;
			return true;

		case 0x09:		// ROL direct
			DecodeAddrDirect_2();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateRol_NZVC_1;
			*mpDstState++ = k6809StateWriteByte_1;
			return true;

		case 0x0A:		// DEC direct
			DecodeAddrDirect_2();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateDec_NZV_1;
			*mpDstState++ = k6809StateWriteByte_1;
			return true;

		case 0x0F:		// CLR direct (6)
			DecodeAddrDirect_2();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateClrData_NZVC_1;
			*mpDstState++ = k6809StateWriteByte_1;
			return true;

		case 0x10:		// $10xx opcodes
			*mpDstState++ = k6809StateReadOpcode10_1;
			return true;

		case 0x11:		// $11xx opcodes
			*mpDstState++ = k6809StateReadOpcode11_1;
			return true;

		case 0x12:		// NOP (2)
			*mpDstState++ = k6809StateWait_1;
			return true;

		case 0x13:		// SYNC
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateSync;
			*mpDstState++ = k6809StateWait_1;
			return true;

		case 0x16:		// LBRA (5)
			*mpDstState++ = k6809StateIndexed16BitOffsetPCRHi_1;
			*mpDstState++ = k6809StateIndexed16BitOffsetLo_1;
			*mpDstState++ = k6809StateBra_1;
			*mpDstState++ = k6809StateWait_1;
			return true;

		case 0x17:		// LBSR (9)
			*mpDstState++ = k6809StateIndexed16BitOffsetPCRHi_1;
			*mpDstState++ = k6809StateIndexed16BitOffsetLo_1;
			*mpDstState++ = k6809StateSwapAddrPC_1;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StatePushLoS_1;
			*mpDstState++ = k6809StatePushHiS_1;
			return true;

		case 0x19:		// DAA (2)
			*mpDstState++ = k6809StateDaa_NZVC_1;
			return true;

		case 0x1A:		// ORCC imm (3)
			*mpDstState++ = k6809StateReadImm_1;
			*mpDstState++ = k6809StateOrCC_1;
			return true;

		case 0x1C:		// ANDCC imm (3)
			*mpDstState++ = k6809StateReadImm_1;
			*mpDstState++ = k6809StateAndCC_1;
			return true;

		case 0x1D:		// SEX (2)
			*mpDstState++ = k6809StateSex_NZV_1;
			return true;

		case 0x1E:		// EXG
			*mpDstState++ = k6809StateReadImm_1;
			*mpDstState++ = k6809StateExg;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateWait_1;
			return true;

		case 0x1F:		// TFR
			*mpDstState++ = k6809StateReadImm_1;
			*mpDstState++ = k6809StateTfr;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateWait_1;
			return true;

		case 0x20:		// BRA
			*mpDstState++ = k6809StateIndexed8BitOffsetPCR_1;
			*mpDstState++ = k6809StateBra_1;
			return true;

		case 0x21:		// BRN
			*mpDstState++ = k6809StateIndexed8BitOffsetPCR_1;
			*mpDstState++ = k6809StateWait_1;
			return true;

		case 0x22:		// BHI
			*mpDstState++ = k6809StateIndexed8BitOffsetPCR_1;
			*mpDstState++ = k6809StateBhi_1;
			*mpDstState++ = k6809StateNop;
			return true;

		case 0x23:		// BLS
			*mpDstState++ = k6809StateIndexed8BitOffsetPCR_1;
			*mpDstState++ = k6809StateBls_1;
			*mpDstState++ = k6809StateNop;
			return true;

		case 0x24:		// BCC
			*mpDstState++ = k6809StateIndexed8BitOffsetPCR_1;
			*mpDstState++ = k6809StateBcc_1;
			*mpDstState++ = k6809StateNop;
			return true;

		case 0x25:		// BCS
			*mpDstState++ = k6809StateIndexed8BitOffsetPCR_1;
			*mpDstState++ = k6809StateBcs_1;
			*mpDstState++ = k6809StateNop;
			return true;

		case 0x26:		// BNE
			*mpDstState++ = k6809StateIndexed8BitOffsetPCR_1;
			*mpDstState++ = k6809StateBne_1;
			*mpDstState++ = k6809StateNop;
			return true;

		case 0x27:		// BEQ
			*mpDstState++ = k6809StateIndexed8BitOffsetPCR_1;
			*mpDstState++ = k6809StateBeq_1;
			*mpDstState++ = k6809StateNop;
			return true;

		case 0x28:		// BVC
			*mpDstState++ = k6809StateIndexed8BitOffsetPCR_1;
			*mpDstState++ = k6809StateBvc_1;
			*mpDstState++ = k6809StateNop;
			return true;

		case 0x29:		// BVS
			*mpDstState++ = k6809StateIndexed8BitOffsetPCR_1;
			*mpDstState++ = k6809StateBvs_1;
			*mpDstState++ = k6809StateNop;
			return true;

		case 0x2A:		// BPL
			*mpDstState++ = k6809StateIndexed8BitOffsetPCR_1;
			*mpDstState++ = k6809StateBpl_1;
			*mpDstState++ = k6809StateNop;
			return true;

		case 0x2B:		// BMI
			*mpDstState++ = k6809StateIndexed8BitOffsetPCR_1;
			*mpDstState++ = k6809StateBmi_1;
			*mpDstState++ = k6809StateNop;
			return true;

		case 0x2C:		// BGE
			*mpDstState++ = k6809StateIndexed8BitOffsetPCR_1;
			*mpDstState++ = k6809StateBge_1;
			*mpDstState++ = k6809StateNop;
			return true;

		case 0x2D:		// BLT
			*mpDstState++ = k6809StateIndexed8BitOffsetPCR_1;
			*mpDstState++ = k6809StateBlt_1;
			*mpDstState++ = k6809StateNop;
			return true;

		case 0x2E:		// BGT
			*mpDstState++ = k6809StateIndexed8BitOffsetPCR_1;
			*mpDstState++ = k6809StateBgt_1;
			*mpDstState++ = k6809StateNop;
			return true;

		case 0x2F:		// BLE
			*mpDstState++ = k6809StateIndexed8BitOffsetPCR_1;
			*mpDstState++ = k6809StateBle_1;
			*mpDstState++ = k6809StateNop;
			return true;

		case 0x30:		// LEAX
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateAddrToX_Z;
			return true;

		case 0x31:		// LEAY
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateAddrToY_Z;
			return true;

		case 0x32:		// LEAS
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateAddrToS;
			return true;

		case 0x33:		// LEAU
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateAddrToU;
			return true;

		case 0x34:		// PSHS
			*mpDstState++ = k6809StateReadImm_1;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StatePushRegMaskS_1;
			return true;

		case 0x35:		// PULS
			*mpDstState++ = k6809StateReadImm_1;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StatePullRegMaskS_1;
			*mpDstState++ = k6809StateWait_1;
			return true;

		case 0x36:		// PSHU
			*mpDstState++ = k6809StateReadImm_1;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StatePushRegMaskU_1;
			return true;

		case 0x37:		// PULU
			*mpDstState++ = k6809StateReadImm_1;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StatePullRegMaskU_1;
			*mpDstState++ = k6809StateWait_1;
			return true;

		case 0x39:		// RTS
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StatePopHiS_1;
			*mpDstState++ = k6809StatePopLoS_1;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateDataToPC;
			return true;

		case 0x3A:		// ABX (3)
			*mpDstState++ = k6809StateAbx_1;
			return true;

		case 0x3B:		// RTI (6/15)
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StatePopS_1;
			*mpDstState++ = k6809StateDataToCC;
			*mpDstState++ = k6809StateRti_TestE;
			*mpDstState++ = k6809StatePullRegMaskS_1;
			*mpDstState++ = k6809StateWait_1;
			return true;

		case 0x3C:		// CWAI
			*mpDstState++ = k6809StateReadImm_1;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateCwai_SetE;
			*mpDstState++ = k6809StatePushRegMaskS_1;
			*mpDstState++ = k6809StateCwai_Wait;
			return true;

		case 0x3D:		// MUL (11)
			*mpDstState++ = k6809StateMul_ZC_1;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateWait_1;
			return true;

		case 0x3F:		// SWI (19)
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateIntEntryE1;
			*mpDstState++ = k6809StatePushRegMaskS_1;	//12
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateBeginSwi;
			*mpDstState++ = k6809StateReadByteHi_1;
			*mpDstState++ = k6809StateReadByteLo_1;
			*mpDstState++ = k6809StateDataToPC;
			*mpDstState++ = k6809StateWait_1;
			return true;

		case 0x40:		// NEGA (2)
			*mpDstState++ = k6809StateAToData;
			*mpDstState++ = k6809StateNeg_HNZVC_1;
			*mpDstState++ = k6809StateDataToA;
			return true;

		case 0x43:		// COMA (2)
			*mpDstState++ = k6809StateAToData;
			*mpDstState++ = k6809StateCom_NZVC_1;
			*mpDstState++ = k6809StateDataToA;
			return true;

		case 0x44:		// LSRA (2)
			*mpDstState++ = k6809StateAToData;
			*mpDstState++ = k6809StateLsr_NZC_1;
			*mpDstState++ = k6809StateDataToA;
			return true;

		case 0x46:		// RORA (2)
			*mpDstState++ = k6809StateAToData;
			*mpDstState++ = k6809StateRor_NZC_1;
			*mpDstState++ = k6809StateDataToA;
			return true;

		case 0x47:		// ASRA (2)
			*mpDstState++ = k6809StateAToData;
			*mpDstState++ = k6809StateAsr_HNZC_1;
			*mpDstState++ = k6809StateDataToA;
			return true;

		case 0x48:		// ASLA (2)
			*mpDstState++ = k6809StateAToData;
			*mpDstState++ = k6809StateAsl_HNZVC_1;
			*mpDstState++ = k6809StateDataToA;
			return true;

		case 0x49:		// ROLA (2)
			*mpDstState++ = k6809StateAToData;
			*mpDstState++ = k6809StateRol_NZVC_1;
			*mpDstState++ = k6809StateDataToA;
			return true;

		case 0x4A:		// DECA (2)
			*mpDstState++ = k6809StateAToData;
			*mpDstState++ = k6809StateDec_NZV_1;
			*mpDstState++ = k6809StateDataToA;
			return true;

		case 0x4C:		// INCA (2)
			*mpDstState++ = k6809StateAToData;
			*mpDstState++ = k6809StateInc_NZV_1;
			*mpDstState++ = k6809StateDataToA;
			return true;

		case 0x4D:		// TSTA (2)
			*mpDstState++ = k6809StateAToData;
			*mpDstState++ = k6809StateTest_NZV_1;
			return true;

		case 0x4F:		// CLRA (2)
			*mpDstState++ = k6809StateClrData_NZVC_1;
			*mpDstState++ = k6809StateDataToA_NZV;
			return true;

		case 0x50:		// NEGB (2)
			*mpDstState++ = k6809StateBToData;
			*mpDstState++ = k6809StateNeg_HNZVC_1;
			*mpDstState++ = k6809StateDataToB;
			return true;

		case 0x53:		// COMB (2)
			*mpDstState++ = k6809StateBToData;
			*mpDstState++ = k6809StateCom_NZVC_1;
			*mpDstState++ = k6809StateDataToB;
			return true;

		case 0x54:		// LSRB (2)
			*mpDstState++ = k6809StateBToData;
			*mpDstState++ = k6809StateLsr_NZC_1;
			*mpDstState++ = k6809StateDataToB;
			return true;

		case 0x56:		// RORB (2)
			*mpDstState++ = k6809StateBToData;
			*mpDstState++ = k6809StateRor_NZC_1;
			*mpDstState++ = k6809StateDataToB;
			return true;

		case 0x57:		// ASRB (2)
			*mpDstState++ = k6809StateBToData;
			*mpDstState++ = k6809StateAsr_HNZC_1;
			*mpDstState++ = k6809StateDataToB;
			return true;

		case 0x58:		// ASLB (2)
			*mpDstState++ = k6809StateBToData;
			*mpDstState++ = k6809StateAsl_HNZVC_1;
			*mpDstState++ = k6809StateDataToB;
			return true;

		case 0x59:		// ROLB (2)
			*mpDstState++ = k6809StateBToData;
			*mpDstState++ = k6809StateRol_NZVC_1;
			*mpDstState++ = k6809StateDataToB;
			return true;

		case 0x5A:		// DECB (2)
			*mpDstState++ = k6809StateBToData;
			*mpDstState++ = k6809StateDec_NZV_1;
			*mpDstState++ = k6809StateDataToB;
			return true;

		case 0x5C:		// INCB (2)
			*mpDstState++ = k6809StateBToData;
			*mpDstState++ = k6809StateInc_NZV_1;
			*mpDstState++ = k6809StateDataToB;
			return true;

		case 0x5D:		// TSTB (2)
			*mpDstState++ = k6809StateBToData;
			*mpDstState++ = k6809StateTest_NZV_1;
			*mpDstState++ = k6809StateWait_1;
			return true;

		case 0x5F:		// CLRB (2)
			*mpDstState++ = k6809StateClrData_NZVC_1;
			*mpDstState++ = k6809StateDataToB_NZV;
			return true;

		case 0x60:		// NEG indexed (6+)
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateNeg_HNZVC_1;
			*mpDstState++ = k6809StateWriteByte_1;
			return true;

		case 0x63:		// COM indexed (6+)
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateCom_NZVC_1;
			*mpDstState++ = k6809StateWriteByte_1;
			return true;

		case 0x64:		// LSR indexed (6+)
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateLsr_NZC_1;
			*mpDstState++ = k6809StateWriteByte_1;
			return true;

		case 0x66:		// ROR indexed (6+)
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateRor_NZC_1;
			*mpDstState++ = k6809StateWriteByte_1;
			return true;

		case 0x67:		// ASR indexed (6+)
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateAsr_HNZC_1;
			*mpDstState++ = k6809StateWriteByte_1;
			return true;

		case 0x68:		// ASL indexed (6+)
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateAsl_HNZVC_1;
			*mpDstState++ = k6809StateWriteByte_1;
			return true;

		case 0x69:		// ROL indexed (6+)
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateRol_NZVC_1;
			*mpDstState++ = k6809StateWriteByte_1;
			return true;

		case 0x6A:		// DEC indexed (6+)
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateDec_NZV_1;
			*mpDstState++ = k6809StateWriteByte_1;
			return true;

		case 0x6C:		// INC indexed (6+)
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateInc_NZV_1;
			*mpDstState++ = k6809StateWriteByte_1;
			return true;

		case 0x6D:		// TST indexed (6+)
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateTest_NZV_1;
			return true;

		case 0x6E:		// JMP indexed (3+)
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateAddrToPC;
			return true;

		case 0x6F:		// CLR indexed (6+)
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateClrData_NZVC_1;
			*mpDstState++ = k6809StateWriteByte_1;
			return true;

		case 0x70:		// NEG extended (7)
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateNeg_HNZVC_1;
			*mpDstState++ = k6809StateWriteByte_1;
			return true;

		case 0x73:		// COM extended (7)
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateCom_NZVC_1;
			*mpDstState++ = k6809StateWriteByte_1;
			return true;

		case 0x74:		// LSR extended (7)
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateLsr_NZC_1;
			*mpDstState++ = k6809StateWriteByte_1;
			return true;

		case 0x76:		// ROR extended (7)
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateRor_NZC_1;
			*mpDstState++ = k6809StateWriteByte_1;
			return true;

		case 0x77:		// ASR extended (7)
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateAsr_HNZC_1;
			*mpDstState++ = k6809StateWriteByte_1;
			return true;

		case 0x78:		// ASL extended (7)
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateAsl_HNZVC_1;
			*mpDstState++ = k6809StateWriteByte_1;
			return true;

		case 0x79:		// ROL extended (7)
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateRol_NZVC_1;
			*mpDstState++ = k6809StateWriteByte_1;
			return true;

		case 0x7A:		// DEC extended (7)
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateDec_NZV_1;
			*mpDstState++ = k6809StateWriteByte_1;
			return true;

		case 0x7C:		// INC extended (7)
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateInc_NZV_1;
			*mpDstState++ = k6809StateWriteByte_1;
			return true;

		case 0x7D:		// TST extended (7)
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateTest_NZV_1;
			*mpDstState++ = k6809StateWait_1;
			return true;

		case 0x7E:		// JMP extended (4)
			*mpDstState++ = k6809StateAddrToPC;
			return true;

		case 0x7F:		// CLR extended (7)
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateClrData_NZVC_1;
			*mpDstState++ = k6809StateWriteByte_1;
			return true;

		case 0x80:		// SUBA immediate (2)
			*mpDstState++ = k6809StateReadImm_1;
			*mpDstState++ = k6809StateSubA_HNZVC;
			*mpDstState++ = k6809StateDataToA;
			return true;

		case 0x81:		// CMPA immediate (2)
			*mpDstState++ = k6809StateReadImm_1;
			*mpDstState++ = k6809StateSubA_HNZVC;
			return true;

		case 0x82:		// SBCA imm (2)
			*mpDstState++ = k6809StateReadImm_1;
			*mpDstState++ = k6809StateSbcA_HNZVC;
			*mpDstState++ = k6809StateDataToA;
			return true;

		case 0x83:		// SUBD immediate (4)
			*mpDstState++ = k6809StateReadImmHi_1;
			*mpDstState++ = k6809StateReadImmLo_1;
			*mpDstState++ = k6809StateSubD_NZVC_1;
			*mpDstState++ = k6809StateDataToD;
			return true;

		case 0x84:		// ANDA imm (2)
			*mpDstState++ = k6809StateReadImm_1;
			*mpDstState++ = k6809StateAndA_NZV;
			*mpDstState++ = k6809StateDataToA;
			return true;

		case 0x85:		// BITA imm (2)
			*mpDstState++ = k6809StateReadImm_1;
			*mpDstState++ = k6809StateAndA_NZV;
			return true;

		case 0x86:		// LDA imm (2)
			*mpDstState++ = k6809StateReadImm_1;
			*mpDstState++ = k6809StateDataToA_NZV;
			return true;

		case 0x88:		// EORA imm (2)
			*mpDstState++ = k6809StateReadImm_1;
			*mpDstState++ = k6809StateXorToA_NZV;
			return true;
		
		case 0x89:		// ADCA imm (2)
			*mpDstState++ = k6809StateReadImm_1;
			*mpDstState++ = k6809StateAdcA_HNZVC;
			*mpDstState++ = k6809StateDataToA;
			return true;

		case 0x8A:		// ORA imm (2)
			*mpDstState++ = k6809StateReadImm_1;
			*mpDstState++ = k6809StateOrToA_NZV;
			return true;

		case 0x8B:		// ADDA imm (2)
			*mpDstState++ = k6809StateReadImm_1;
			*mpDstState++ = k6809StateAddA_HNZVC;
			*mpDstState++ = k6809StateDataToA;
			return true;

		case 0x8C:		// CMPX imm16 (4)
			*mpDstState++ = k6809StateReadImmHi_1;
			*mpDstState++ = k6809StateReadImmLo_1;
			*mpDstState++ = k6809StateCmpX_NZVC_1;
			return true;

		case 0x8D:		// BSR rel (7)
			*mpDstState++ = k6809StateIndexed8BitOffsetPCR_1;
			*mpDstState++ = k6809StateSwapAddrPC_1;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StatePushLoS_1;
			*mpDstState++ = k6809StatePushHiS_1;
			return true;

		case 0x8E:		// LDX imm16
			*mpDstState++ = k6809StateReadImmHi_1;
			*mpDstState++ = k6809StateReadImmLo_1;
			*mpDstState++ = k6809StateDataToX_NZV;
			return true;

		case 0x90:		// SUBA direct (4)
			DecodeAddrDirect_2();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateSubA_HNZVC;
			*mpDstState++ = k6809StateDataToA;
			return true;

		case 0x91:		// CMPA direct (4)
			DecodeAddrDirect_2();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateSubA_HNZVC;
			return true;

		case 0x92:		// SBCA direct (4)
			DecodeAddrDirect_2();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateSbcA_HNZVC;
			*mpDstState++ = k6809StateDataToA;
			return true;

		case 0x93:		// SUBD direct (6)
			DecodeAddrDirect_2();
			*mpDstState++ = k6809StateReadByteHi_1;
			*mpDstState++ = k6809StateReadByteLo_1;
			*mpDstState++ = k6809StateSubD_NZVC_1;
			*mpDstState++ = k6809StateDataToD;
			return true;

		case 0x94:		// ANDA direct (4)
			DecodeAddrDirect_2();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateAndA_NZV;
			*mpDstState++ = k6809StateDataToA;
			return true;

		case 0x95:		// BITA direct (4)
			DecodeAddrDirect_2();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateAndA_NZV;
			return true;

		case 0x96:		// LDA direct (4)
			DecodeAddrDirect_2();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateDataToA_NZV;
			return true;

		case 0x97:		// STA direct (4)
			DecodeAddrDirect_2();
			*mpDstState++ = k6809StateAToData;
			*mpDstState++ = k6809StateWriteByte_1;
			return true;

		case 0x98:		// EORA direct (4)
			DecodeAddrDirect_2();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateXorToA_NZV;
			return true;

		case 0x99:		// ADCA direct (4)
			DecodeAddrDirect_2();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateAdcA_HNZVC;
			*mpDstState++ = k6809StateDataToA;
			return true;

		case 0x9A:		// ORA direct (4)
			DecodeAddrDirect_2();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateOrToA_NZV;
			return true;

		case 0x9B:		// ADDA direct (4)
			DecodeAddrDirect_2();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateAddA_HNZVC;
			*mpDstState++ = k6809StateDataToA;
			return true;

		case 0x9C:		// CMPX direct (6)
			DecodeAddrDirect_2();
			*mpDstState++ = k6809StateReadByteHi_1;
			*mpDstState++ = k6809StateReadByteLo_1;
			*mpDstState++ = k6809StateCmpX_NZVC_1;
			return true;

		case 0x9D:		// JSR direct (7)
			DecodeAddrDirect_2();
			*mpDstState++ = k6809StatePCToData;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StatePushLoS_1;
			*mpDstState++ = k6809StatePushHiS_1;
			*mpDstState++ = k6809StateAddrToPC;
			return true;

		case 0x9E:		// LDX direct (5)
			DecodeAddrDirect_2();
			*mpDstState++ = k6809StateReadByteHi_1;
			*mpDstState++ = k6809StateReadByteLo_1;
			*mpDstState++ = k6809StateDataToX_NZV;
			return true;

		case 0x9F:		// STX direct (5)
			DecodeAddrDirect_2();
			*mpDstState++ = k6809StateXToData;
			*mpDstState++ = k6809StateWriteByteHi_1;
			*mpDstState++ = k6809StateWriteByteLo_1;
			return true;

		case 0xA0:		// SUBA indexed (4+)
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateSubA_HNZVC;
			*mpDstState++ = k6809StateDataToA;
			return true;

		case 0xA1:		// CMPA indexed (4+)
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateSubA_HNZVC;
			return true;

		case 0xA3:		// SUBD indexed (6+)
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateReadByteHi_1;
			*mpDstState++ = k6809StateReadByteLo_1;
			*mpDstState++ = k6809StateSubD_NZVC_1;
			*mpDstState++ = k6809StateDataToD;
			return true;

		case 0xA4:		// ANDA indexed (4+)
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateAndA_NZV;
			*mpDstState++ = k6809StateDataToA;
			return true;

		case 0xA5:		// BITA indexed (4+)
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateAndA_NZV;
			return true;

		case 0xA6:		// LDA indexed (4+)
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateDataToA_NZV;
			return true;

		case 0xA7:		// STA indexed (4+)
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateAToData;
			*mpDstState++ = k6809StateWriteByte_1;
			return true;

		case 0xA8:		// EORA indexed (4+)
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateXorToA_NZV;
			return true;

		case 0xA9:		// ADCA indexed (4+)
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateAdcA_HNZVC;
			*mpDstState++ = k6809StateDataToA;
			return true;

		case 0xAA:		// ORA indexed (4+)
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateOrToA_NZV;
			return true;

		case 0xAB:		// ADDA indexed (4+)
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateAddA_HNZVC;
			*mpDstState++ = k6809StateDataToA;
			return true;

		case 0xAC:		// CMPX indexed (6+)
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateReadByteHi_1;
			*mpDstState++ = k6809StateReadByteLo_1;
			*mpDstState++ = k6809StateCmpX_NZVC_1;
			return true;

		case 0xAD:		// JSR indexed (7+)
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StatePCToData;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StatePushLoS_1;
			*mpDstState++ = k6809StatePushHiS_1;
			*mpDstState++ = k6809StateAddrToPC;
			return true;

		case 0xAE:		// LDX indexed (5+)
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateReadByteHi_1;
			*mpDstState++ = k6809StateReadByteLo_1;
			*mpDstState++ = k6809StateDataToX_NZV;
			return true;

		case 0xAF:		// STX indexed (5+)
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateXToData;
			*mpDstState++ = k6809StateWriteByteHi_1;
			*mpDstState++ = k6809StateWriteByteLo_1;
			return true;

		case 0xB0:		// SUBA extended (5)
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateSubA_HNZVC;
			*mpDstState++ = k6809StateDataToA;
			return true;

		case 0xB1:		// CMPA extended (5)
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateSubA_HNZVC;
			return true;

		case 0xB2:		// SBCA extended (5)
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateSbcA_HNZVC;
			*mpDstState++ = k6809StateDataToA;
			return true;

		case 0xB3:		// SUBD extended (7)
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StateReadByteHi_1;
			*mpDstState++ = k6809StateReadByteLo_1;
			*mpDstState++ = k6809StateSubD_NZVC_1;
			*mpDstState++ = k6809StateDataToD;
			return true;

		case 0xB4:		// ANDA extended (5)
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateAndA_NZV;
			*mpDstState++ = k6809StateDataToA;
			return true;

		case 0xB5:		// BITA extended (5)
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateAndA_NZV;
			return true;

		case 0xB6:		// LDA extended (5)
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateDataToA_NZV;
			return true;

		case 0xB7:		// STA extended (5)
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StateAToData;
			*mpDstState++ = k6809StateWriteByte_1;
			return true;

		case 0xB8:		// EORA extended (5)
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateXorToA_NZV;
			return true;

		case 0xB9:		// ADCA extended (5)
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateAdcA_HNZVC;
			return true;

		case 0xBA:		// ORA extended (5)
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateOrToA_NZV;
			return true;

		case 0xBB:		// ADDA extended (5)
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateAddA_HNZVC;
			*mpDstState++ = k6809StateDataToA;
			return true;

		case 0xBC:		// CMPX extended (7)
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateCmpX_NZVC_1;
			return true;

		case 0xBD:		// JSR extended (8)
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StatePCToData;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StatePushLoS_1;
			*mpDstState++ = k6809StatePushHiS_1;
			*mpDstState++ = k6809StateAddrToPC;
			return true;

		case 0xBE:		// LDX extended (6)
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StateReadByteHi_1;
			*mpDstState++ = k6809StateReadByteLo_1;
			*mpDstState++ = k6809StateDataToX_NZV;
			return true;

		case 0xBF:		// STX extended (6)
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StateXToData;
			*mpDstState++ = k6809StateWriteByteHi_1;
			*mpDstState++ = k6809StateWriteByteLo_1;
			return true;

		case 0xC0:		// SUBB imm (2)
			*mpDstState++ = k6809StateReadImm_1;
			*mpDstState++ = k6809StateSubB_HNZVC;
			*mpDstState++ = k6809StateDataToB;
			return true;

		case 0xC1:		// CMPB imm (2)
			*mpDstState++ = k6809StateReadImm_1;
			*mpDstState++ = k6809StateSubB_HNZVC;
			return true;

		case 0xC2:		// SBCB imm (2)
			*mpDstState++ = k6809StateReadImm_1;
			*mpDstState++ = k6809StateSbcB_HNZVC;
			*mpDstState++ = k6809StateDataToB;
			return true;

		case 0xC3:		// ADDD imm (4)
			*mpDstState++ = k6809StateReadImmHi_1;
			*mpDstState++ = k6809StateReadImmLo_1;
			*mpDstState++ = k6809StateAddD_NZVC_1;
			*mpDstState++ = k6809StateDataToD;
			return true;

		case 0xC4:		// ANDB imm (2)
			*mpDstState++ = k6809StateReadImm_1;
			*mpDstState++ = k6809StateAndB_NZV;
			*mpDstState++ = k6809StateDataToB;
			return true;

		case 0xC5:		// BITB imm (2)
			*mpDstState++ = k6809StateReadImm_1;
			*mpDstState++ = k6809StateAndB_NZV;
			return true;

		case 0xC6:		// LDB imm (2)
			*mpDstState++ = k6809StateReadImm_1;
			*mpDstState++ = k6809StateDataToB_NZV;
			return true;

		case 0xC8:		// EORB imm (2)
			*mpDstState++ = k6809StateReadImm_1;
			*mpDstState++ = k6809StateXorToB_NZV;
			return true;

		case 0xC9:		// ADCB imm (2)
			*mpDstState++ = k6809StateReadImm_1;
			*mpDstState++ = k6809StateAdcB_HNZVC;
			return true;

		case 0xCA:		// ORB imm (2)
			*mpDstState++ = k6809StateReadImm_1;
			*mpDstState++ = k6809StateOrToB_NZV;
			return true;

		case 0xCB:		// ADDB imm (2)
			*mpDstState++ = k6809StateReadImm_1;
			*mpDstState++ = k6809StateAddB_HNZVC;
			*mpDstState++ = k6809StateDataToB;
			return true;

		case 0xCC:		// LDD imm16 (3)
			*mpDstState++ = k6809StateReadImmHi_1;
			*mpDstState++ = k6809StateReadImmLo_1;
			*mpDstState++ = k6809StateDataToD_NZV;
			return true;

		case 0xCE:		// LDU imm16 (3)
			*mpDstState++ = k6809StateReadImmHi_1;
			*mpDstState++ = k6809StateReadImmLo_1;
			*mpDstState++ = k6809StateDataToU_NZV;
			return true;

		case 0xD0:		// SUBB direct (4)
			DecodeAddrDirect_2();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateSubB_HNZVC;
			*mpDstState++ = k6809StateDataToB;
			return true;

		case 0xD1:		// CMPB direct (4)
			DecodeAddrDirect_2();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateSubB_HNZVC;
			return true;

		case 0xD2:		// SBCB direct (4)
			DecodeAddrDirect_2();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateSbcB_HNZVC;
			*mpDstState++ = k6809StateDataToB;
			return true;

		case 0xD3:		// ADDD direct (6)
			DecodeAddrDirect_2();
			*mpDstState++ = k6809StateReadByteHi_1;
			*mpDstState++ = k6809StateReadByteLo_1;
			*mpDstState++ = k6809StateAddD_NZVC_1;
			*mpDstState++ = k6809StateDataToB;
			return true;

		case 0xD4:		// ANDB direct (4)
			DecodeAddrDirect_2();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateAndB_NZV;
			*mpDstState++ = k6809StateDataToB;
			return true;

		case 0xD5:		// BITB direct (4)
			DecodeAddrDirect_2();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateAndB_NZV;
			return true;

		case 0xD6:		// LDB direct (4)
			DecodeAddrDirect_2();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateDataToB_NZV;
			return true;

		case 0xD7:		// STB direct (4)
			DecodeAddrDirect_2();
			*mpDstState++ = k6809StateBToData;
			*mpDstState++ = k6809StateWriteByte_1;
			return true;

		case 0xD8:		// EORB direct (4)
			DecodeAddrDirect_2();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateXorToB_NZV;
			return true;

		case 0xD9:		// ADCB direct (4)
			DecodeAddrDirect_2();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateAdcB_HNZVC;
			*mpDstState++ = k6809StateDataToB;
			return true;

		case 0xDA:		// ORB direct (4)
			DecodeAddrDirect_2();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateOrToB_NZV;
			return true;

		case 0xDB:		// ADDB direct (4)
			DecodeAddrDirect_2();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateAddB_HNZVC;
			*mpDstState++ = k6809StateDataToB;
			return true;

		case 0xDC:		// LDD direct (5)
			DecodeAddrDirect_2();
			*mpDstState++ = k6809StateReadByteHi_1;
			*mpDstState++ = k6809StateReadByteLo_1;
			*mpDstState++ = k6809StateDataToD_NZV;
			return true;

		case 0xDD:		// STD direct (5)
			DecodeAddrDirect_2();
			*mpDstState++ = k6809StateDToData;
			*mpDstState++ = k6809StateWriteByteHi_1;
			*mpDstState++ = k6809StateWriteByteLo_1;
			return true;

		case 0xDE:		// LDU direct (5)
			DecodeAddrDirect_2();
			*mpDstState++ = k6809StateReadByteHi_1;
			*mpDstState++ = k6809StateReadByteLo_1;
			*mpDstState++ = k6809StateDataToU_NZV;
			return true;

		case 0xDF:		// STU direct (5)
			DecodeAddrDirect_2();
			*mpDstState++ = k6809StateUToData;
			*mpDstState++ = k6809StateWriteByteHi_1;
			*mpDstState++ = k6809StateWriteByteLo_1;
			return true;

		case 0xE0:		// SUBB indexed (4+)
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateSubB_HNZVC;
			*mpDstState++ = k6809StateDataToB;
			return true;

		case 0xE1:		// CMPB indexed (4+)
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateSubB_HNZVC;
			return true;

		case 0xE2:		// SBCB indexed (4+)
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateSbcB_HNZVC;
			*mpDstState++ = k6809StateDataToB;
			return true;

		case 0xE3:		// ADDD indexed (6+)
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateReadByteHi_1;
			*mpDstState++ = k6809StateReadByteLo_1;
			*mpDstState++ = k6809StateAddD_NZVC_1;
			*mpDstState++ = k6809StateDataToD;
			return true;

		case 0xE4:		// ANDB indexed (4+)
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateAndB_NZV;
			*mpDstState++ = k6809StateDataToB;
			return true;

		case 0xE5:		// BITB indexed (4+)
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateAndB_NZV;
			return true;

		case 0xE6:		// LDB indexed (4+)
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateDataToB_NZV;
			return true;

		case 0xE7:		// STB indexed (4+)
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateBToData;
			*mpDstState++ = k6809StateWriteByte_1;
			return true;

		case 0xE8:		// EORB indexed (4+)
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateXorToB_NZV;
			return true;

		case 0xE9:		// ADCB indexed (4+)
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateAdcB_HNZVC;
			*mpDstState++ = k6809StateDataToB;
			return true;

		case 0xEA:		// ORB indexed (4+)
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateOrToB_NZV;
			return true;

		case 0xEB:		// ADDB indexed (4+)
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateAddB_HNZVC;
			*mpDstState++ = k6809StateDataToB;
			return true;

		case 0xEC:		// LDD indexed (5+)
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateReadByteHi_1;
			*mpDstState++ = k6809StateReadByteLo_1;
			*mpDstState++ = k6809StateDataToD_NZV;
			return true;

		case 0xED:		// STD indexed (5+)
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateDToData;
			*mpDstState++ = k6809StateWriteByteHi_1;
			*mpDstState++ = k6809StateWriteByteLo_1;
			return true;

		case 0xEE:		// LDU indexed (5+)
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateReadImmHi_1;
			*mpDstState++ = k6809StateReadImmLo_1;
			*mpDstState++ = k6809StateDataToU_NZV;
			return true;

		case 0xEF:		// STU indexed (5+)
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateUToData;
			*mpDstState++ = k6809StateWriteByteHi_1;
			*mpDstState++ = k6809StateWriteByteLo_1;
			return true;

		case 0xF0:		// SUBB extended (5)
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateSubB_HNZVC;
			*mpDstState++ = k6809StateDataToB;
			return true;

		case 0xF1:		// CMPB extended (5)
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateSubB_HNZVC;
			*mpDstState++ = k6809StateDataToB;
			return true;

		case 0xF2:		// SBCB extended (5)
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateSbcB_HNZVC;
			*mpDstState++ = k6809StateDataToB;
			return true;

		case 0xF3:		// ADDD extended (7)
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StateReadByteHi_1;
			*mpDstState++ = k6809StateReadByteLo_1;
			*mpDstState++ = k6809StateAddD_NZVC_1;
			*mpDstState++ = k6809StateDataToB;
			return true;

		case 0xF4:		// ANDB extended (5)
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateAndB_NZV;
			*mpDstState++ = k6809StateDataToB;
			return true;

		case 0xF5:		// BITB extended (5)
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateAndB_NZV;
			return true;

		case 0xF6:		// LDB extended (5)
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateDataToB_NZV;
			return true;

		case 0xF7:		// STB extended (5)
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StateBToData;
			*mpDstState++ = k6809StateWriteByte_1;
			return true;

		case 0xF8:		// EORB extended (5)
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateXorToB_NZV;
			return true;

		case 0xF9:		// ADCB extended (5)
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateAdcB_HNZVC;
			*mpDstState++ = k6809StateDataToB;
			return true;

		case 0xFA:		// ORB extended (5)
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateOrToB_NZV;
			return true;

		case 0xFB:		// ADDB extended (5)
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StateReadByte_1;
			*mpDstState++ = k6809StateAddB_HNZVC;
			*mpDstState++ = k6809StateDataToB;
			return true;

		case 0xFC:		// LDD extended
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StateReadByteHi_1;
			*mpDstState++ = k6809StateReadByteLo_1;
			*mpDstState++ = k6809StateDataToD_NZV;
			return true;

		case 0xFD:		// STD extended
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StateDToData;
			*mpDstState++ = k6809StateWriteByteHi_1;
			*mpDstState++ = k6809StateWriteByteLo_1;
			return true;

		case 0xFE:		// LDU extended
			*mpDstState++ = k6809StateReadImmHi_1;
			*mpDstState++ = k6809StateReadImmLo_1;
			*mpDstState++ = k6809StateDataToU_NZV;
			return true;

		case 0xFF:		// STU extended
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StateUToData;
			*mpDstState++ = k6809StateWriteByteHi_1;
			*mpDstState++ = k6809StateWriteByteLo_1;
			return true;
	}

	return false;
}

bool ATCPUDecoderGenerator6809::DecodeInsn10(uint8 opcode) {
	using namespace ATCPUStates6809;

	switch(opcode) {
		case 0x20:		// LBRA
			*mpDstState++ = k6809StateIndexed16BitOffsetPCRHi_1;
			*mpDstState++ = k6809StateIndexed16BitOffsetLo_1;
			*mpDstState++ = k6809StateBra_1;
			*mpDstState++ = k6809StateWait_1;
			return true;

		case 0x21:		// LBRN (5)
			*mpDstState++ = k6809StateIndexed16BitOffsetPCRHi_1;
			*mpDstState++ = k6809StateIndexed16BitOffsetLo_1;
			*mpDstState++ = k6809StateWait_1;
			return true;

		case 0x22:		// LBHI (5/6)
			*mpDstState++ = k6809StateIndexed16BitOffsetPCRHi_1;
			*mpDstState++ = k6809StateIndexed16BitOffsetLo_1;
			*mpDstState++ = k6809StateBhi_1;
			*mpDstState++ = k6809StateWait_1;
			return true;

		case 0x23:		// LBLS
			*mpDstState++ = k6809StateIndexed16BitOffsetPCRHi_1;
			*mpDstState++ = k6809StateIndexed16BitOffsetLo_1;
			*mpDstState++ = k6809StateBls_1;
			*mpDstState++ = k6809StateWait_1;
			return true;

		case 0x24:		// LBCC
			*mpDstState++ = k6809StateIndexed16BitOffsetPCRHi_1;
			*mpDstState++ = k6809StateIndexed16BitOffsetLo_1;
			*mpDstState++ = k6809StateBcc_1;
			*mpDstState++ = k6809StateWait_1;
			return true;

		case 0x25:		// LBCS
			*mpDstState++ = k6809StateIndexed16BitOffsetPCRHi_1;
			*mpDstState++ = k6809StateIndexed16BitOffsetLo_1;
			*mpDstState++ = k6809StateBcs_1;
			*mpDstState++ = k6809StateWait_1;
			return true;

		case 0x26:		// LBNE
			*mpDstState++ = k6809StateIndexed16BitOffsetPCRHi_1;
			*mpDstState++ = k6809StateIndexed16BitOffsetLo_1;
			*mpDstState++ = k6809StateBne_1;
			*mpDstState++ = k6809StateWait_1;
			return true;

		case 0x27:		// LBEQ
			*mpDstState++ = k6809StateIndexed16BitOffsetPCRHi_1;
			*mpDstState++ = k6809StateIndexed16BitOffsetLo_1;
			*mpDstState++ = k6809StateBeq_1;
			*mpDstState++ = k6809StateWait_1;
			return true;

		case 0x28:		// LBVC
			*mpDstState++ = k6809StateIndexed16BitOffsetPCRHi_1;
			*mpDstState++ = k6809StateIndexed16BitOffsetLo_1;
			*mpDstState++ = k6809StateBvc_1;
			*mpDstState++ = k6809StateWait_1;
			return true;

		case 0x29:		// LBVS
			*mpDstState++ = k6809StateIndexed16BitOffsetPCRHi_1;
			*mpDstState++ = k6809StateIndexed16BitOffsetLo_1;
			*mpDstState++ = k6809StateBvs_1;
			*mpDstState++ = k6809StateWait_1;
			return true;

		case 0x2A:		// LBPL
			*mpDstState++ = k6809StateIndexed16BitOffsetPCRHi_1;
			*mpDstState++ = k6809StateIndexed16BitOffsetLo_1;
			*mpDstState++ = k6809StateBpl_1;
			*mpDstState++ = k6809StateWait_1;
			return true;

		case 0x2B:		// LBMI
			*mpDstState++ = k6809StateIndexed16BitOffsetPCRHi_1;
			*mpDstState++ = k6809StateIndexed16BitOffsetLo_1;
			*mpDstState++ = k6809StateBmi_1;
			*mpDstState++ = k6809StateWait_1;
			return true;

		case 0x2C:		// LBGE
			*mpDstState++ = k6809StateIndexed16BitOffsetPCRHi_1;
			*mpDstState++ = k6809StateIndexed16BitOffsetLo_1;
			*mpDstState++ = k6809StateBge_1;
			*mpDstState++ = k6809StateWait_1;
			return true;

		case 0x2D:		// LBLT
			*mpDstState++ = k6809StateIndexed16BitOffsetPCRHi_1;
			*mpDstState++ = k6809StateIndexed16BitOffsetLo_1;
			*mpDstState++ = k6809StateBlt_1;
			*mpDstState++ = k6809StateWait_1;
			return true;

		case 0x2E:		// LBGT
			*mpDstState++ = k6809StateIndexed16BitOffsetPCRHi_1;
			*mpDstState++ = k6809StateIndexed16BitOffsetLo_1;
			*mpDstState++ = k6809StateBgt_1;
			*mpDstState++ = k6809StateWait_1;
			return true;

		case 0x2F:		// LBLE
			*mpDstState++ = k6809StateIndexed16BitOffsetPCRHi_1;
			*mpDstState++ = k6809StateIndexed16BitOffsetLo_1;
			*mpDstState++ = k6809StateBle_1;
			*mpDstState++ = k6809StateWait_1;
			return true;

		case 0x3F:		// SWI2 (20)
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateIntEntryE1;
			*mpDstState++ = k6809StatePushRegMaskS_1;	//12
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateBeginSwi2;
			*mpDstState++ = k6809StateReadByteHi_1;
			*mpDstState++ = k6809StateReadByteLo_1;
			*mpDstState++ = k6809StateDataToPC;
			*mpDstState++ = k6809StateWait_1;
			return true;

		case 0x83:		// CMPD imm16 (5)
			*mpDstState++ = k6809StateReadImmHi_1;
			*mpDstState++ = k6809StateReadImmLo_1;
			*mpDstState++ = k6809StateSubD_NZVC_1;
			return true;

		case 0x8C:		// CMPY imm16 (5)
			*mpDstState++ = k6809StateReadImmHi_1;
			*mpDstState++ = k6809StateReadImmLo_1;
			*mpDstState++ = k6809StateCmpY_NZVC_1;
			return true;

		case 0x8E:		// LDY imm16 (4)
			*mpDstState++ = k6809StateReadImmHi_1;
			*mpDstState++ = k6809StateReadImmLo_1;
			*mpDstState++ = k6809StateDataToY_NZV;
			return true;

		case 0x93:		// CMPD direct (7)
			DecodeAddrDirect_2();
			*mpDstState++ = k6809StateReadByteHi_1;
			*mpDstState++ = k6809StateReadByteLo_1;
			*mpDstState++ = k6809StateSubD_NZVC_1;
			return true;

		case 0x9C:		// CMPY direct (7)
			DecodeAddrDirect_2();
			*mpDstState++ = k6809StateReadByteHi_1;
			*mpDstState++ = k6809StateReadByteLo_1;
			*mpDstState++ = k6809StateCmpY_NZVC_1;
			return true;

		case 0x9E:		// LDY direct (6)
			DecodeAddrDirect_2();
			*mpDstState++ = k6809StateYToData;
			*mpDstState++ = k6809StateWriteByteHi_1;
			*mpDstState++ = k6809StateWriteByteLo_1;
			return true;

		case 0x9F:		// STY direct (6)
			DecodeAddrDirect_2();
			*mpDstState++ = k6809StateYToData;
			*mpDstState++ = k6809StateWriteByteHi_1;
			*mpDstState++ = k6809StateWriteByteLo_1;
			return true;

		case 0xA3:		// CMPD indexed (7+)
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateReadByteHi_1;
			*mpDstState++ = k6809StateReadByteLo_1;
			*mpDstState++ = k6809StateSubD_NZVC_1;
			return true;

		case 0xAC:		// CMPY indexed (7+)
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateReadByteHi_1;
			*mpDstState++ = k6809StateReadByteLo_1;
			*mpDstState++ = k6809StateCmpY_NZVC_1;
			return true;

		case 0xAE:		// LDY indexed (6+)
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateReadByteHi_1;
			*mpDstState++ = k6809StateReadByteLo_1;
			*mpDstState++ = k6809StateDataToY_NZV;
			return true;

		case 0xAF:		// STY indexed (6+)
			DecodeAddrIndexed_2p();
			*mpDstState++ = k6809StateYToData;
			*mpDstState++ = k6809StateWriteByteHi_1;
			*mpDstState++ = k6809StateWriteByteLo_1;
			return true;

		case 0xB3:		// CMPD extended (8)
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StateReadByteHi_1;
			*mpDstState++ = k6809StateReadByteLo_1;
			*mpDstState++ = k6809StateSubD_NZVC_1;
			return true;

		case 0xBC:		// CMPY extended (8)
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StateReadByteHi_1;
			*mpDstState++ = k6809StateReadByteLo_1;
			*mpDstState++ = k6809StateCmpY_NZVC_1;
			return true;

		case 0xBE:		// LDY extended (7)
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StateReadByteHi_1;
			*mpDstState++ = k6809StateReadByteLo_1;
			*mpDstState++ = k6809StateDataToY_NZV;
			return true;

		case 0xBF:		// STY extended (7)
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StateYToData;
			*mpDstState++ = k6809StateWriteByteHi_1;
			*mpDstState++ = k6809StateWriteByteLo_1;
			return true;

		case 0xCE:		// LDS imm16 (4)
			*mpDstState++ = k6809StateReadImmHi_1;
			*mpDstState++ = k6809StateReadImmLo_1;
			*mpDstState++ = k6809StateDataToS_NZV;
			return true;

		case 0xDE:		// LDS direct (6)
			DecodeAddrDirect_2();
			*mpDstState++ = k6809StateReadByteHi_1;
			*mpDstState++ = k6809StateReadByteLo_1;
			*mpDstState++ = k6809StateDataToS_NZV;
			return true;

		case 0xDF:		// STS direct (6)
			DecodeAddrDirect_2();
			*mpDstState++ = k6809StateSToData;
			*mpDstState++ = k6809StateWriteByteHi_1;
			*mpDstState++ = k6809StateWriteByteLo_1;
			return true;

		case 0xEE:		// LDS indexed (6+)
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateReadByteHi_1;
			*mpDstState++ = k6809StateReadByteLo_1;
			*mpDstState++ = k6809StateDataToS_NZV;
			return true;

		case 0xEF:		// STS indexed (6+)
			*mpDstState++ = k6809StateIndexed_2p;
			*mpDstState++ = k6809StateSToData;
			*mpDstState++ = k6809StateWriteByteHi_1;
			*mpDstState++ = k6809StateWriteByteLo_1;
			return true;

		case 0xFE:		// LDS extended (7)
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StateReadByteHi_1;
			*mpDstState++ = k6809StateReadByteLo_1;
			*mpDstState++ = k6809StateDataToS_NZV;
			return true;

		case 0xFF:		// STS extended (7)
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StateSToData;
			*mpDstState++ = k6809StateWriteByteHi_1;
			*mpDstState++ = k6809StateWriteByteLo_1;
			return true;
	}

	return false;
}

bool ATCPUDecoderGenerator6809::DecodeInsn11(uint8 opcode) {
	using namespace ATCPUStates6809;

	switch(opcode) {
		case 0x3F:		// SWI3 (20)
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateIntEntryE1;
			*mpDstState++ = k6809StatePushRegMaskS_1;	//12
			*mpDstState++ = k6809StateWait_1;
			*mpDstState++ = k6809StateBeginSwi3;
			*mpDstState++ = k6809StateReadByteHi_1;
			*mpDstState++ = k6809StateReadByteLo_1;
			*mpDstState++ = k6809StateDataToPC;
			*mpDstState++ = k6809StateWait_1;
			return true;

		case 0x83:		// CMPU imm16 (5)
			*mpDstState++ = k6809StateReadImmHi_1;
			*mpDstState++ = k6809StateReadImmLo_1;
			*mpDstState++ = k6809StateCmpU_NZVC_1;
			return true;

		case 0x8C:		// CMPS imm16 (5)
			*mpDstState++ = k6809StateReadImmHi_1;
			*mpDstState++ = k6809StateReadImmLo_1;
			*mpDstState++ = k6809StateCmpS_NZVC_1;
			return true;

		case 0x93:		// CMPU direct (7)
			DecodeAddrDirect_2();
			*mpDstState++ = k6809StateReadByteHi_1;
			*mpDstState++ = k6809StateReadByteLo_1;
			*mpDstState++ = k6809StateCmpU_NZVC_1;
			return true;

		case 0x9C:		// CMPS direct (7)
			DecodeAddrDirect_2();
			*mpDstState++ = k6809StateReadByteHi_1;
			*mpDstState++ = k6809StateReadByteLo_1;
			*mpDstState++ = k6809StateCmpS_NZVC_1;
			return true;

		case 0xA3:		// CMPU indexed (7+)
			DecodeAddrIndexed_2p();
			*mpDstState++ = k6809StateReadByteHi_1;
			*mpDstState++ = k6809StateReadByteLo_1;
			*mpDstState++ = k6809StateCmpU_NZVC_1;
			return true;

		case 0xAC:		// CMPS indexed (7+)
			DecodeAddrIndexed_2p();
			*mpDstState++ = k6809StateReadByteHi_1;
			*mpDstState++ = k6809StateReadByteLo_1;
			*mpDstState++ = k6809StateCmpS_NZVC_1;
			return true;

		case 0xB3:		// CMPU extended (8)
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StateReadByteHi_1;
			*mpDstState++ = k6809StateReadByteLo_1;
			*mpDstState++ = k6809StateCmpU_NZVC_1;
			return true;

		case 0xBC:		// CMPS extended (8)
			DecodeAddrExtended_3();
			*mpDstState++ = k6809StateReadByteHi_1;
			*mpDstState++ = k6809StateReadByteLo_1;
			*mpDstState++ = k6809StateCmpS_NZVC_1;
			return true;
	}

	return false;
}

void ATCPUDecoderGenerator6809::DecodeAddrDirect_2() {
	*mpDstState++ = ATCPUStates6809::k6809StateReadAddrDirect_1;
	*mpDstState++ = ATCPUStates6809::k6809StateWait_1;
}

void ATCPUDecoderGenerator6809::DecodeAddrIndexed_2p() {
	*mpDstState++ = ATCPUStates6809::k6809StateIndexed_2p;
}

void ATCPUDecoderGenerator6809::DecodeAddrExtended_3() {
	*mpDstState++ = ATCPUStates6809::k6809StateReadAddrExtHi;
	*mpDstState++ = ATCPUStates6809::k6809StateReadAddrExtLo;
	*mpDstState++ = ATCPUStates6809::k6809StateWait_1;
}
