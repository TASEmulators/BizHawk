/******************************************************************************/
/* Mednafen Sega Saturn Emulation Module                                      */
/******************************************************************************/
/* vdp2.h:
**  Copyright (C) 2015-2016 Mednafen Team
**
** This program is free software; you can redistribute it and/or
** modify it under the terms of the GNU General Public License
** as published by the Free Software Foundation; either version 2
** of the License, or (at your option) any later version.
**
** This program is distributed in the hope that it will be useful,
** but WITHOUT ANY WARRANTY; without even the implied warranty of
** MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
** GNU General Public License for more details.
**
** You should have received a copy of the GNU General Public License
** along with this program; if not, write to the Free Software Foundation, Inc.,
** 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
*/

#ifndef __MDFN_SS_VDP2_H
#define __MDFN_SS_VDP2_H

namespace MDFN_IEN_SS
{
namespace VDP2
{

uint32 Write8_DB(uint32 A, uint16 DB) MDFN_HOT;
uint32 Write16_DB(uint32 A, uint16 DB) MDFN_HOT;
uint16 Read16_DB(uint32 A) MDFN_HOT;

void Init(const bool IsPAL) MDFN_COLD;

void Reset(bool powering_up) MDFN_COLD;
void SetLayerEnableMask(uint64 mask) MDFN_COLD;

sscpu_timestamp_t Update(sscpu_timestamp_t timestamp);
void AdjustTS(const int32 delta);

void StartFrame(EmulateSpecStruct* espec, const bool clock28m);

INLINE bool GetVBOut(void) { extern bool VBOut; return VBOut; }
INLINE bool GetHBOut(void) { extern bool HBOut; return HBOut; }

//
//
enum
{
 GSREG_LINE = 0,
 GSREG_DON,
 GSREG_BM,
 GSREG_IM,
 GSREG_VRES,
 GSREG_HRES
};

uint32 GetRegister(const unsigned id, char* const special, const uint32 special_len);
void SetRegister(const unsigned id, const uint32 value);
uint8 PeekVRAM(const uint32 addr);
void PokeVRAM(const uint32 addr, const uint8 val);
void MakeDump(const std::string& path) MDFN_COLD;

INLINE uint32 PeekLine(void) { extern int32 VCounter; return VCounter; }
INLINE uint32 PeekHPos(void) { extern int32 HCounter; return HCounter; }
}
}
#endif
