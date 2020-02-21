//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2008-2017 Avery Lee
//	CPU emulation library - 6809 state definitions
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

#ifndef f_AT_ATCPU_STATES6809_H
#define f_AT_ATCPU_STATES6809_H

namespace ATCPUStates6809 {
	enum ATCPUState6809 {
		k6809StateNop,
		k6809StateWait_1,
		k6809StateReadOpcode,
		k6809StateReadOpcodeNoBreak,
		k6809StateReadOpcode10_1,
		k6809StateReadOpcode11_1,

		k6809StateReadAddrDirect_1,
		k6809StateReadAddrExtHi,
		k6809StateReadAddrExtLo,

		k6809StateReadImm_1,
		k6809StateReadImmHi_1,
		k6809StateReadImmLo_1,

		k6809StateReadByte_1,
		k6809StateReadByteHi_1,
		k6809StateReadByteLo_1,
		k6809StateWriteByte_1,
		k6809StateWriteByteHi_1,
		k6809StateWriteByteLo_1,

		k6809StateIndexed_2p,
		k6809StateIndexedPostInc,
		k6809StateIndexedPreDec,
		k6809StateIndexedA,
		k6809StateIndexedB,
		k6809StateIndexedD,
		k6809StateIndexed5BitOffset,
		k6809StateIndexed8BitOffset,
		k6809StateIndexed16BitOffsetHi,
		k6809StateIndexed16BitOffsetLo_1,
		k6809StateIndexed8BitOffsetPCR_1,
		k6809StateIndexed16BitOffsetPCRHi_1,
		k6809StateIndexedIndirectLo,
		k6809StateIndexedRts,

		k6809StateClrData_NZVC_1,
		k6809StateAToData,
		k6809StateBToData,
		k6809StateDToData,
		k6809StateXToData,
		k6809StateYToData,
		k6809StateSToData,
		k6809StateUToData,
		k6809StatePCToData,
		k6809StateDataToA,
		k6809StateDataToA_NZV,
		k6809StateDataToB,
		k6809StateDataToB_NZV,
		k6809StateDataToD,
		k6809StateDataToD_NZV,
		k6809StateDataToX_NZV,
		k6809StateDataToY_NZV,
		k6809StateDataToS_NZV,
		k6809StateDataToU_NZV,
		k6809StateDataToCC,
		k6809StateDataToPC,

		k6809StateAddrToX,
		k6809StateAddrToX_Z,
		k6809StateAddrToY,
		k6809StateAddrToY_Z,
		k6809StateAddrToS,
		k6809StateAddrToU,
		k6809StateAddrToPC,

		k6809StatePushHiS_1,
		k6809StatePushLoS_1,
		k6809StatePopHiS_1,
		k6809StatePopLoS_1,
		k6809StatePopS_1,
		k6809StatePushRegMaskS_1,
		k6809StatePullRegMaskS_1,
		k6809StatePushRegMaskU_1,
		k6809StatePullRegMaskU_1,

		k6809StateSwapAddrPC_1,
		k6809StateRti_TestE,
		k6809StateCwai_SetE,
		k6809StateCwai_Wait,
		k6809StateSync,
		k6809StateIntEntryE0,
		k6809StateIntEntryE1,
		k6809StateBeginIrq,
		k6809StateBeginFirq,
		k6809StateBeginNmi,
		k6809StateBeginSwi,
		k6809StateBeginSwi2,
		k6809StateBeginSwi3,

		k6809StateExg,
		k6809StateTfr,

		k6809StateTest_NZV_1,
		k6809StateAndA_NZV,
		k6809StateAndB_NZV,
		k6809StateAndCC_1,
		k6809StateOrCC_1,
		k6809StateOrToA_NZV,
		k6809StateOrToB_NZV,
		k6809StateXorToA_NZV,
		k6809StateXorToB_NZV,
		k6809StateAddA_HNZVC,
		k6809StateAddB_HNZVC,
		k6809StateAddD_NZVC_1,
		k6809StateAdcA_HNZVC,
		k6809StateAdcB_HNZVC,
		k6809StateSubA_HNZVC,
		k6809StateSubB_HNZVC,
		k6809StateSubD_NZVC_1,
		k6809StateSbcA_HNZVC,
		k6809StateSbcB_HNZVC,
		k6809StateCmpX_NZVC_1,
		k6809StateCmpY_NZVC_1,
		k6809StateCmpU_NZVC_1,
		k6809StateCmpS_NZVC_1,
		k6809StateNeg_HNZVC_1,
		k6809StateDec_NZV_1,
		k6809StateInc_NZV_1,
		k6809StateAsl_HNZVC_1,
		k6809StateAsr_HNZC_1,
		k6809StateLsr_NZC_1,
		k6809StateRol_NZVC_1,
		k6809StateRor_NZC_1,
		k6809StateCom_NZVC_1,
		k6809StateMul_ZC_1,
		k6809StateDaa_NZVC_1,
		k6809StateSex_NZV_1,
		k6809StateAbx_1,

		k6809StateBra_1,		// branch always
		k6809StateBhi_1,		// branch on (C or Z)=0
		k6809StateBls_1,		// branch on (C or Z)=1
		k6809StateBcc_1,		// branch on C=0
		k6809StateBcs_1,		// branch on C=1
		k6809StateBne_1,		// branch on Z=0
		k6809StateBeq_1,		// branch on Z=1
		k6809StateBvc_1,		// branch on V=0
		k6809StateBvs_1,		// branch on V=1
		k6809StateBpl_1,		// branch on N=0
		k6809StateBmi_1,		// branch on N=1
		k6809StateBge_1,		// branch on N^V=0
		k6809StateBlt_1,		// branch on N^V=1
		k6809StateBgt_1,		// branch on (Z or N^V) = 0
		k6809StateBle_1,		// branch on (Z or N^V) = 1

		k6809StateRegenerateDecodeTables,
		k6809StateAddToHistory,
		k6809StateBreakOnUnsupportedOpcode,

		k6809StateCount
	};
}

#endif
