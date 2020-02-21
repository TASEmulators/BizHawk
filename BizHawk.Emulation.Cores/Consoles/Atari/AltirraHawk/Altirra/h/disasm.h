//	Altirra - Atari 800/800XL emulator
//	Copyright (C) 2008 Avery Lee
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

#ifndef AT_DISASM_H
#define AT_DISASM_H

class VDStringA;

class IATDebugTarget;

enum ATCPUMode : uint8;
enum ATCPUSubMode : uint8;
enum ATDebugDisasmMode : uint8;

void ATDisassembleCaptureRegisterContext(ATCPUHistoryEntry& hent);
void ATDisassembleCaptureRegisterContext(IATDebugTarget *target, ATCPUHistoryEntry& hent);
void ATDisassembleCaptureRegisterContext(ATCPUHistoryEntry& hent, const ATCPUExecState& execState, ATDebugDisasmMode execMode);
void ATDisassembleCaptureInsnContext(uint16 addr, uint8 bank, ATCPUHistoryEntry& hent);
void ATDisassembleCaptureInsnContext(uint32 globalAddr, ATCPUHistoryEntry& hent);
void ATDisassembleCaptureInsnContext(IATDebugTarget *target, uint16 addr, uint8 bank, ATCPUHistoryEntry& hent);
void ATDisassembleCaptureInsnContext(IATDebugTarget *target, uint32 globalAddr, ATCPUHistoryEntry& hent);
uint16 ATDisassembleInsn(uint16 addr, uint8 bank = 0);
uint16 ATDisassembleInsn(char *buf, uint16 addr, bool decodeReferences);
uint16 ATDisassembleInsn(VDStringA& buf, uint16 addr, bool decodeReferences);

uint16 ATDisassembleInsn(VDStringA& buf,
	IATDebugTarget *target,
	ATDebugDisasmMode disasmMode,
	const ATCPUHistoryEntry& hent,
	bool decodeReferences,
	bool decodeRefsHistory,
	bool showPCAddress,
	bool showCodeBytes,
	bool showLabels,
	bool lowercaseOps = false,
	bool wideOpcode = false,
	bool showLabelNamespaces = true,
	bool showSymbols = true,
	bool showGlobalPC = false);

uint16 ATDisassembleGetFirstAnchor(IATDebugTarget *target, uint16 addr, uint16 targetAddr, uint32 addrBank);
void ATDisassemblePredictContext(ATCPUHistoryEntry& hent, ATDebugDisasmMode execMode);

int ATGetOpcodeLength(uint8 opcode);
int ATGetOpcodeLength(uint8 opcode, uint8 p, bool emuMode);
int ATGetOpcodeLength(uint8 opcode, uint8 p, bool emuMode, ATDebugDisasmMode disasmMode);
bool ATIsValidOpcode(uint8 opcode);

uint32 ATGetOpcodeLengthZ80(uint8 opcode);
uint32 ATGetOpcodeLengthZ80ED(uint8 opcode);
uint32 ATGetOpcodeLengthZ80CB(uint8 opcode);
uint32 ATGetOpcodeLengthZ80DDFD(uint8 opcode);
uint32 ATGetOpcodeLengthZ80DDFDCB(uint8 opcode);

#endif
