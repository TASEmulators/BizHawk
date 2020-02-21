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

#ifndef AT_DECMATH_H
#define AT_DECMATH_H

class ATCPUEmulator;
class ATCPUEmulatorMemory;

double ATDebugReadDecFloatAsBinary(ATCPUEmulatorMemory& mem, uint16 addr);
double ATReadDecFloatAsBinary(ATCPUEmulatorMemory& mem, uint16 addr);
double ATReadDecFloatAsBinary(const uint8 bytes[6]);

void ATAccelAFP(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem);
void ATAccelFASC(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem);
void ATAccelIPF(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem);
void ATAccelFPI(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem);
void ATAccelFADD(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem);
void ATAccelFSUB(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem);
void ATAccelFMUL(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem);
void ATAccelFDIV(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem);
void ATAccelLOG(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem);
void ATAccelLOG10(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem);
void ATAccelEXP(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem);
void ATAccelEXP10(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem);
void ATAccelSKPSPC(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem);
void ATAccelISDIGT(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem);
void ATAccelNORMALIZE(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem);
void ATAccelPLYEVL(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem);
void ATAccelZFR0(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem);
void ATAccelZF1(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem);
void ATAccelZFL(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem);
void ATAccelLDBUFA(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem);
void ATAccelFLD0R(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem);
void ATAccelFLD0P(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem);
void ATAccelFLD1R(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem);
void ATAccelFLD1P(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem);
void ATAccelFST0R(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem);
void ATAccelFST0P(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem);
void ATAccelFMOVE(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem);
void ATAccelREDRNG(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem);

#endif
