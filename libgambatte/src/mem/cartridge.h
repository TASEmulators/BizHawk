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

#ifndef CARTRIDGE_H
#define CARTRIDGE_H

#include "loadres.h"
#include "memptrs.h"
#include "rtc.h"
#include "savestate.h"
#include <memory>
#include <string>
#include <vector>
#include "newstate.h"

namespace gambatte {

class Mbc {
public:
	virtual ~Mbc() {}
	virtual void romWrite(unsigned P, unsigned data) = 0;
	virtual void loadState(SaveState::Mem const &ss) = 0;
	virtual bool isAddressWithinAreaRombankCanBeMappedTo(unsigned address, unsigned rombank) const = 0;

	template<bool isReader>void SyncState(NewState *ns)
	{
		// can't have virtual templates, so..
		SyncState(ns, isReader);
	}
	virtual void SyncState(NewState *ns, bool isReader) = 0;
};

class Cartridge {
public:
	void setStatePtrs(SaveState &);
	void loadState(SaveState const &);
	bool loaded() const { return mbc_.get(); }
	unsigned char const * rmem(unsigned area) const { return memptrs_.rmem(area); }
	unsigned char * wmem(unsigned area) const { return memptrs_.wmem(area); }
	unsigned char * vramdata() const { return memptrs_.vramdata(); }
	unsigned char * romdata(unsigned area) const { return memptrs_.romdata(area); }
	unsigned char * wramdata(unsigned area) const { return memptrs_.wramdata(area); }
	unsigned char const * rdisabledRam() const { return memptrs_.rdisabledRam(); }
	unsigned char const * rsrambankptr() const { return memptrs_.rsrambankptr(); }
	unsigned char * wsrambankptr() const { return memptrs_.wsrambankptr(); }
	unsigned char * vrambankptr() const { return memptrs_.vrambankptr(); }
	OamDmaSrc oamDmaSrc() const { return memptrs_.oamDmaSrc(); }
	void setVrambank(unsigned bank) { memptrs_.setVrambank(bank); }
	void setWrambank(unsigned bank) { memptrs_.setWrambank(bank); }
	void setOamDmaSrc(OamDmaSrc oamDmaSrc) { memptrs_.setOamDmaSrc(oamDmaSrc); }
	unsigned curRomBank() const { return memptrs_.curRomBank(); }
	void mbcWrite(unsigned addr, unsigned data) { mbc_->romWrite(addr, data); }
	bool isCgb() const { return gambatte::isCgb(memptrs_); }
	void rtcWrite(unsigned data) { rtc_.write(data); }
	unsigned char rtcRead() const { return *rtc_.activeData(); }
	void loadSavedata(char const *data);
	int saveSavedataLength();
	void saveSavedata(char *dest);
	bool getMemoryArea(int which, unsigned char **data, int *length) const;
	LoadRes loadROM(char const *romfiledata, unsigned romfilelength, bool forceDmg, bool multicartCompat);
	char const * romTitle() const { return reinterpret_cast<char const *>(memptrs_.romdata() + 0x134); }

	void setRTCCallback(std::uint32_t (*callback)()) {
		rtc_.setRTCCallback(callback);
	}

private:
	MemPtrs memptrs_;
	Rtc rtc_;
	std::unique_ptr<Mbc> mbc_;

public:
	template<bool isReader>void SyncState(NewState *ns);
};

}

#endif
