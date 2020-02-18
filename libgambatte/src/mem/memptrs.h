//
//   Copyright (C) 2007-2010 by sinamas <sinamas at users.sourceforge.net>
//
//   This program is free software; you can redistribute it and/or modify
//   it under the terms of the GNU General Public License version 2 as
//   published by the Free Software Foundation.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//   GNU General Public License version 2 for more details.
//
//   You should have received a copy of the GNU General Public License
//   version 2 along with this program; if not, write to the
//   Free Software Foundation, Inc.,
//   51 Franklin St, Fifth Floor, Boston, MA  02110-1301, USA.
//

#ifndef MEMPTRS_H
#define MEMPTRS_H

#include "newstate.h"
#include "array.h"

namespace gambatte {

enum OamDmaSrc {
	oam_dma_src_rom,
	oam_dma_src_sram,
	oam_dma_src_vram,
	oam_dma_src_wram,
	oam_dma_src_invalid,
	oam_dma_src_off };

enum {
	mm_rom_begin = 0x0000,
	mm_rom1_begin = 0x4000,
	mm_vram_begin = 0x8000,
	mm_sram_begin = 0xA000,
	mm_wram_begin = 0xC000,
	mm_wram1_begin = 0xD000,
	mm_wram_mirror_begin = 0xE000,
	mm_oam_begin = 0xFE00,
	mm_io_begin = 0xFF00,
	mm_hram_begin = 0xFF80 };

enum { max_num_vrambanks = 2 };
inline std::size_t rambank_size() { return 0x2000; }
inline std::size_t rombank_size() { return 0x4000; }
inline std::size_t vrambank_size() { return 0x2000; }
inline std::size_t wrambank_size() { return 0x1000; }

class MemPtrs {
public:
	enum RamFlag { read_en = 1, write_en = 2, rtc_en = 4 };

	MemPtrs();
	void reset(unsigned rombanks, unsigned rambanks, unsigned wrambanks);

	unsigned char const* rmem(unsigned area) const { return rmem_[area]; }
	unsigned char* wmem(unsigned area) const { return wmem_[area]; }
	unsigned char* romdata() const { return memchunk_ + pre_rom_pad_size(); }
	unsigned char* romdata(unsigned area) const { return romdata_[area]; }
	unsigned char* romdataend() const { return rambankdata_ - max_num_vrambanks * vrambank_size(); }
	unsigned char* vramdata() const { return romdataend(); }
	unsigned char* vramdataend() const { return rambankdata_; }
	unsigned char* rambankdata() const { return rambankdata_; }
	unsigned char* rambankdataend() const { return wramdata_[0]; }
	unsigned char* wramdata(unsigned area) const { return wramdata_[area]; }
	unsigned char* wramdataend() const { return wramdataend_; }
	unsigned char const* rdisabledRam() const { return rdisabledRamw(); }
	unsigned char const* rsrambankptr() const { return rsrambankptr_; }
	unsigned char* wsrambankptr() const { return wsrambankptr_; }
	unsigned char* vrambankptr() const { return vrambankptr_; }
	OamDmaSrc oamDmaSrc() const { return oamDmaSrc_; }
	bool isInOamDmaConflictArea(unsigned p) const;

	void setRombank0(unsigned bank);
	void setRombank(unsigned bank);
	void setRambank(unsigned ramFlags, unsigned rambank);
	void setVrambank(unsigned bank) { vrambankptr_ = vramdata() + bank * vrambank_size() - mm_vram_begin; }
	void setWrambank(unsigned bank);
	void setOamDmaSrc(OamDmaSrc oamDmaSrc);

private:
	unsigned char const *rmem_[0x10];
	unsigned char       *wmem_[0x10];
	unsigned char *romdata_[2];
	unsigned char *wramdata_[2];
	unsigned char *vrambankptr_;
	unsigned char *rsrambankptr_;
	unsigned char *wsrambankptr_;
	SimpleArray<unsigned char> memchunk_;
	unsigned char *rambankdata_;
	unsigned char *wramdataend_;
	OamDmaSrc oamDmaSrc_;

	unsigned curRomBank_;

	int memchunk_len;
	int memchunk_saveoffs;
	int memchunk_savelen;

	static std::size_t pre_rom_pad_size() { return mm_rom1_begin; }
	void disconnectOamDmaAreas();
	unsigned char * rdisabledRamw() const { return wramdataend_; }
	unsigned char * wdisabledRam()  const { return wramdataend_ + rambank_size(); }

public:
	template<bool isReader>void SyncState(NewState *ns);
};

inline bool isCgb(MemPtrs const& memptrs) {
	int const num_cgb_wrambanks = 8;
	std::size_t const wramsize = memptrs.wramdataend() - memptrs.wramdata(0);
	return wramsize == num_cgb_wrambanks * wrambank_size();
}

}

#endif
