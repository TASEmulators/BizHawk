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

#include "cartridge.h"
#include "../savestate.h"
#include <algorithm>
#include <cstdio>
#include <cstring>
#include <fstream>

using namespace gambatte;

namespace {

unsigned toMulti64Rombank(unsigned rombank) {
	return (rombank >> 1 & 0x30) | (rombank & 0xF);
}

class DefaultMbc : public Mbc {
public:
	virtual bool isAddressWithinAreaRombankCanBeMappedTo(unsigned addr, unsigned bank) const {
		return (addr < rombank_size()) == (bank == 0);
	}

	virtual void SyncState(NewState *ns, bool isReader)
	{
	}
};

class Mbc0 : public DefaultMbc {
public:
	explicit Mbc0(MemPtrs &memptrs)
	: memptrs_(memptrs)
	, enableRam_(false)
	{
	}

	virtual unsigned char curRomBank() const {
		return 1;
	}

	virtual void romWrite(unsigned const p, unsigned const data, unsigned long const /*cc*/) {
		if (p < rambank_size()) {
			enableRam_ = (data & 0xF) == 0xA;
			memptrs_.setRambank(enableRam_ ? MemPtrs::read_en | MemPtrs::write_en : 0, 0);
		}
	}

	virtual void loadState(SaveState::Mem const &ss) {
		enableRam_ = ss.enableRam;
		memptrs_.setRambank(enableRam_ ? MemPtrs::read_en | MemPtrs::write_en : 0, 0);
	}

private:
	MemPtrs &memptrs_;
	bool enableRam_;

public:
	virtual void SyncState(NewState *ns, bool isReader)
	{
		NSS(enableRam_);
	}
};

inline unsigned rambanks(MemPtrs const &memptrs) {
	return (memptrs.rambankdataend() - memptrs.rambankdata()) / rambank_size();
}

inline unsigned rombanks(MemPtrs const &memptrs) {
	return (memptrs.romdataend() - memptrs.romdata()) / rombank_size();
}

class Mbc1 : public DefaultMbc {
public:
	explicit Mbc1(MemPtrs &memptrs)
	: memptrs_(memptrs)
	, rombank_(1)
	, rambank_(0)
	, enableRam_(false)
	, rambankMode_(false)
	{
	}

	virtual unsigned char curRomBank() const {
		return rombank_;
	}

	virtual void romWrite(unsigned const p, unsigned const data, unsigned long const /*cc*/) {
		switch (p >> 13 & 3) {
		case 0:
			enableRam_ = (data & 0xF) == 0xA;
			setRambank();
			break;
		case 1:
			rombank_ = rambankMode_ ? data & 0x1F : (rombank_ & 0x60) | (data & 0x1F);
			setRombank();
			break;
		case 2:
			if (rambankMode_) {
				rambank_ = data & 3;
				setRambank();
			} else {
				rombank_ = (data << 5 & 0x60) | (rombank_ & 0x1F);
				setRombank();
			}

			break;
		case 3:
			// Should this take effect immediately rather?
			rambankMode_ = data & 1;
			break;
		}
	}

	virtual void loadState(SaveState::Mem const &ss) {
		rombank_ = ss.rombank;
		rambank_ = ss.rambank;
		enableRam_ = ss.enableRam;
		rambankMode_ = ss.rambankMode;
		setRambank();
		setRombank();
	}

private:
	MemPtrs &memptrs_;
	unsigned char rombank_;
	unsigned char rambank_;
	bool enableRam_;
	bool rambankMode_;

	static unsigned adjustedRombank(unsigned bank) { return bank & 0x1F ? bank : bank | 1; }

	void setRambank() const {
		memptrs_.setRambank(enableRam_ ? MemPtrs::read_en | MemPtrs::write_en : 0,
		                    rambank_ & (rambanks(memptrs_) - 1));
	}

	void setRombank() const { memptrs_.setRombank(adjustedRombank(rombank_) & (rombanks(memptrs_) - 1)); }


public:
	virtual void SyncState(NewState *ns, bool isReader)
	{
		NSS(rombank_);
		NSS(rambank_);
		NSS(enableRam_);
		NSS(rambankMode_);
	}
};

class Mbc1Multi64 : public Mbc {
public:
	explicit Mbc1Multi64(MemPtrs &memptrs)
	: memptrs_(memptrs)
	, rombank_(1)
	, enableRam_(false)
	, rombank0Mode_(false)
	{
	}

	virtual unsigned char curRomBank() const {
		return rombank_;
	}

	virtual void romWrite(unsigned const p, unsigned const data, unsigned long const /*cc*/) {
		switch (p >> 13 & 3) {
		case 0:
			enableRam_ = (data & 0xF) == 0xA;
			memptrs_.setRambank(enableRam_ ? MemPtrs::read_en | MemPtrs::write_en : 0, 0);
			break;
		case 1:
			rombank_ = (rombank_   & 0x60) | (data    & 0x1F);
			memptrs_.setRombank(rombank0Mode_
				? adjustedRombank(toMulti64Rombank(rombank_))
				: adjustedRombank(rombank_) & (rombanks(memptrs_) - 1));
			break;
		case 2:
			rombank_ = (data << 5 & 0x60) | (rombank_ & 0x1F);
			setRombank();
			break;
		case 3:
			rombank0Mode_ = data & 1;
			setRombank();
			break;
		}
	}

	virtual void loadState(SaveState::Mem const &ss) {
		rombank_ = ss.rombank;
		enableRam_ = ss.enableRam;
		rombank0Mode_ = ss.rambankMode;
		memptrs_.setRambank(enableRam_ ? MemPtrs::read_en | MemPtrs::write_en : 0, 0);
		setRombank();
	}

	virtual bool isAddressWithinAreaRombankCanBeMappedTo(unsigned addr, unsigned bank) const {
		return (addr < 0x4000) == ((bank & 0xF) == 0);
	}

private:
	MemPtrs &memptrs_;
	unsigned char rombank_;
	bool enableRam_;
	bool rombank0Mode_;

	static unsigned adjustedRombank(unsigned bank) { return bank & 0x1F ? bank : bank | 1; }

	void setRombank() const {
		if (rombank0Mode_) {
			unsigned const rb = toMulti64Rombank(rombank_);
			memptrs_.setRombank0(rb & 0x30);
			memptrs_.setRombank(adjustedRombank(rb));
		} else {
			memptrs_.setRombank0(0);
			memptrs_.setRombank(adjustedRombank(rombank_) & (rombanks(memptrs_) - 1));
		}
	}

public:
	virtual void SyncState(NewState *ns, bool isReader)
	{
		NSS(rombank_);
		NSS(enableRam_);
		NSS(rombank0Mode_);
	}
};

class Mbc2 : public DefaultMbc {
public:
	explicit Mbc2(MemPtrs &memptrs)
	: memptrs_(memptrs)
	, rombank_(1)
	, enableRam_(false)
	{
	}

	virtual unsigned char curRomBank() const {
		return rombank_;
	}

	virtual void romWrite(unsigned const p, unsigned const data, unsigned long const /*cc*/) {
		switch (p & 0x6100) {
		case 0x0000:
			enableRam_ = (data & 0xF) == 0xA;
			memptrs_.setRambank(enableRam_ ? MemPtrs::read_en | MemPtrs::write_en : 0, 0);
			break;
		case 0x2100:
			rombank_ = data & 0xF;
			memptrs_.setRombank(rombank_ & (rombanks(memptrs_) - 1));
			break;
		}
	}

	virtual void loadState(SaveState::Mem const &ss) {
		rombank_ = ss.rombank;
		enableRam_ = ss.enableRam;
		memptrs_.setRambank(enableRam_ ? MemPtrs::read_en | MemPtrs::write_en : 0, 0);
		memptrs_.setRombank(rombank_ & (rombanks(memptrs_) - 1));
	}

private:
	MemPtrs &memptrs_;
	unsigned char rombank_;
	bool enableRam_;

public:
	virtual void SyncState(NewState *ns, bool isReader)
	{
		NSS(rombank_);
		NSS(enableRam_);
	}
};

class Mbc3 : public DefaultMbc {
public:
	Mbc3(MemPtrs &memptrs, Rtc *const rtc)
	: memptrs_(memptrs)
	, rtc_(rtc)
	, rombank_(1)
	, rambank_(0)
	, enableRam_(false)
	{
	}

	virtual unsigned char curRomBank() const {
		return rombank_;
	}

	virtual void romWrite(unsigned const p, unsigned const data, unsigned long const cc) {
		switch (p >> 13 & 3) {
		case 0:
			enableRam_ = (data & 0xF) == 0xA;
			setRambank();
			break;
		case 1:
			rombank_ = data & 0x7F;
			setRombank();
			break;
		case 2:
			rambank_ = data;
			setRambank();
			break;
		case 3:
			if (rtc_)
				rtc_->latch(data, cc);

			break;
		}
	}

	virtual void loadState(SaveState::Mem const &ss) {
		rombank_ = ss.rombank;
		rambank_ = ss.rambank;
		enableRam_ = ss.enableRam;
		setRambank();
		setRombank();
	}

private:
	MemPtrs &memptrs_;
	Rtc *const rtc_;
	unsigned char rombank_;
	unsigned char rambank_;
	bool enableRam_;

	void setRambank() const {
		unsigned flags = enableRam_ ? MemPtrs::read_en | MemPtrs::write_en : 0;

		if (rtc_) {
			rtc_->set(enableRam_, rambank_);

			if (rtc_->activeData())
				flags |= MemPtrs::rtc_en;
		}

		memptrs_.setRambank(flags, rambank_ & (rambanks(memptrs_) - 1));
	}

	void setRombank() const {
		memptrs_.setRombank(std::max(rombank_ & (rombanks(memptrs_) - 1), 1u));
	}

public:
	virtual void SyncState(NewState *ns, bool isReader)
	{
		NSS(rombank_);
		NSS(rambank_);
		NSS(enableRam_);
	}
};

class HuC1 : public DefaultMbc {
public:
	explicit HuC1(MemPtrs &memptrs)
	: memptrs_(memptrs)
	, rombank_(1)
	, rambank_(0)
	, enableRam_(false)
	, rambankMode_(false)
	{
	}

	virtual unsigned char curRomBank() const {
		return rombank_;
	}

	virtual void romWrite(unsigned const p, unsigned const data, unsigned long const /*cc*/) {
		switch (p >> 13 & 3) {
		case 0:
			enableRam_ = (data & 0xF) == 0xA;
			setRambank();
			break;
		case 1:
			rombank_ = data & 0x3F;
			setRombank();
			break;
		case 2:
			rambank_ = data & 3;
			rambankMode_ ? setRambank() : setRombank();
			break;
		case 3:
			rambankMode_ = data & 1;
			setRambank();
			setRombank();
			break;
		}
	}

	virtual void loadState(SaveState::Mem const &ss) {
		rombank_ = ss.rombank;
		rambank_ = ss.rambank;
		enableRam_ = ss.enableRam;
		rambankMode_ = ss.rambankMode;
		setRambank();
		setRombank();
	}

private:
	MemPtrs &memptrs_;
	unsigned char rombank_;
	unsigned char rambank_;
	bool enableRam_;
	bool rambankMode_;

	void setRambank() const {
		memptrs_.setRambank(enableRam_ ? MemPtrs::read_en | MemPtrs::write_en : MemPtrs::read_en,
		                    rambankMode_ ? rambank_ & (rambanks(memptrs_) - 1) : 0);
	}

	void setRombank() const {
		memptrs_.setRombank((rambankMode_ ? rombank_ : rambank_ << 6 | rombank_)
		                  & (rombanks(memptrs_) - 1));
	}

public:
	virtual void SyncState(NewState *ns, bool isReader)
	{
		NSS(rombank_);
		NSS(rambank_);
		NSS(enableRam_);
		NSS(rambankMode_);
	}
};

class HuC3 : public DefaultMbc {
public:
	HuC3(MemPtrs& memptrs, HuC3Chip* const huc3)
		: memptrs_(memptrs)
		, huc3_(huc3)
		, rombank_(1)
		, rambank_(0)
		, ramflag_(0)
	{
	}

	virtual unsigned char curRomBank() const {
		return rombank_;
	}

	virtual bool disabledRam() const {
		return false;
	}

	virtual void romWrite(unsigned const p, unsigned const data, unsigned long const /*cc*/) {
		switch (p >> 13 & 3) {
		case 0:
			ramflag_ = data;
			//printf("[HuC3] set ramflag to %02X\n", data);
			setRambank();
			break;
		case 1:
			//printf("[HuC3] set rombank to %02X\n", data);
			rombank_ = data;
			setRombank();
			break;
		case 2:
			//printf("[HuC3] set rambank to %02X\n", data);
			rambank_ = data;
			setRambank();
			break;
		case 3:
			// GEST: "programs will write 1 here"
			break;
		}
	}

	virtual void SyncState(NewState *ns, bool isReader)
	{
		NSS(rombank_);
		NSS(rambank_);
		NSS(ramflag_);
	}

	virtual void loadState(SaveState::Mem const& ss) {
		rombank_ = ss.rombank;
		rambank_ = ss.rambank;
		ramflag_ = ss.HuC3RAMflag;
		setRambank();
		setRombank();
	}

private:
	MemPtrs& memptrs_;
	HuC3Chip* const huc3_;
	unsigned char rombank_;
	unsigned char rambank_;
	unsigned char ramflag_;

	void setRambank() const {
		huc3_->setRamflag(ramflag_);

		unsigned flags;
		if (ramflag_ >= 0x0B && ramflag_ < 0x0F) {
			// System registers mode
			flags = MemPtrs::read_en | MemPtrs::write_en | MemPtrs::rtc_en;
		}
		else if (ramflag_ == 0x0A || ramflag_ > 0x0D) {
			// Read/write mode
			flags = MemPtrs::read_en | MemPtrs::write_en;
		}
		else {
			// Read-only mode ??
			flags = MemPtrs::read_en;
		}

		memptrs_.setRambank(flags, rambank_ & (rambanks(memptrs_) - 1));
	}

	void setRombank() const {
		memptrs_.setRombank(std::max(rombank_ & (rombanks(memptrs_) - 1), 1u));
	}
};

class Mbc5 : public DefaultMbc {
public:
	explicit Mbc5(MemPtrs &memptrs)
	: memptrs_(memptrs)
	, rombank_(1)
	, rambank_(0)
	, enableRam_(false)
	{
	}

	virtual unsigned char curRomBank() const {
		return rombank_;
	}

	virtual void romWrite(unsigned const p, unsigned const data, unsigned long const /*cc*/) {
		switch (p >> 13 & 3) {
		case 0:
			enableRam_ = (data & 0xF) == 0xA;
			setRambank();
			break;
		case 1:
			rombank_ = p < 0x3000
			         ? (rombank_  & 0x100) |  data
			         : (data << 8 & 0x100) | (rombank_ & 0xFF);
			setRombank();
			break;
		case 2:
			rambank_ = data & 0xF;
			setRambank();
			break;
		case 3:
			break;
		}
	}

	virtual void loadState(SaveState::Mem const &ss) {
		rombank_ = ss.rombank;
		rambank_ = ss.rambank;
		enableRam_ = ss.enableRam;
		setRambank();
		setRombank();
	}

private:
	MemPtrs &memptrs_;
	unsigned short rombank_;
	unsigned char rambank_;
	bool enableRam_;

	void setRambank() const {
		memptrs_.setRambank(enableRam_ ? MemPtrs::read_en | MemPtrs::write_en : 0,
		                    rambank_ & (rambanks(memptrs_) - 1));
	}

	void setRombank() const { memptrs_.setRombank(rombank_ & (rombanks(memptrs_) - 1)); }

public:
	virtual void SyncState(NewState *ns, bool isReader)
	{
		NSS(rombank_);
		NSS(rambank_);
		NSS(enableRam_);
	}
};

std::string stripExtension(std::string const& str) {
	std::string::size_type const lastDot = str.find_last_of('.');
	std::string::size_type const lastSlash = str.find_last_of('/');

	if (lastDot != std::string::npos && (lastSlash == std::string::npos || lastSlash < lastDot))
		return str.substr(0, lastDot);

	return str;
}

std::string stripDir(std::string const& str) {
	std::string::size_type const lastSlash = str.find_last_of('/');
	if (lastSlash != std::string::npos)
		return str.substr(lastSlash + 1);

	return str;
}

void enforce8bit(unsigned char* data, std::size_t size) {
	if (static_cast<unsigned char>(0x100))
		while (size--)
			*data++ &= 0xFF;
}

unsigned pow2ceil(unsigned n) {
	--n;
	n |= n >> 1;
	n |= n >> 2;
	n |= n >> 4;
	n |= n >> 8;
	++n;

	return n;
}

bool presumedMulti64Mbc1(unsigned char const header[], unsigned rombanks) {
	return header[0x147] == 1 && header[0x149] == 0 && rombanks == 64;
}

bool hasBattery(unsigned char headerByte0x147) {
	switch (headerByte0x147) {
	case 0x03:
	case 0x06:
	case 0x09:
	case 0x0F:
	case 0x10:
	case 0x13:
	case 0x1B:
	case 0x1E:
	case 0xFE: // huc3
	case 0xFF:
		return true;
	}

	return false;
}

bool hasRtc(unsigned headerByte0x147) {
	switch (headerByte0x147) {
	case 0x0F:
	case 0x10:
	case 0xFE: // huc3
		return true;
	}

	return false;
}

int asHex(char c) {
	return c >= 'A' ? c - 'A' + 0xA : c - '0';
}

}

Cartridge::Cartridge()
: rtc_(time_)
, huc3_(time_)
{
}

void Cartridge::setStatePtrs(SaveState &state) {
	state.mem.vram.set(memptrs_.vramdata(), memptrs_.vramdataend() - memptrs_.vramdata());
	state.mem.sram.set(memptrs_.rambankdata(), memptrs_.rambankdataend() - memptrs_.rambankdata());
	state.mem.wram.set(memptrs_.wramdata(0), memptrs_.wramdataend() - memptrs_.wramdata(0));
}

void Cartridge::loadState(SaveState const &state) {
	huc3_.loadState(state);
	rtc_.loadState(state);
	time_.loadState(state);
	mbc_->loadState(state.mem);
}

static bool isMbc2(unsigned char h147) { return h147 == 5 || h147 == 6; }

static unsigned numRambanksFromH14x(unsigned char h147, unsigned char h149) {
	switch (h149) {
	case 0x00: return isMbc2(h147) ? 1 : 0;
	case 0x01:
	case 0x02: return 1;
	case 0x03: return 4;
	case 0x04: return 16;
	case 0x05: return 8;
	}

	return 4;
}

LoadRes Cartridge::loadROM(char const *romfiledata, unsigned romfilelength, bool const forceDmg, bool const multicartCompat) {
	enum Cartridgetype { type_plain,
	                     type_mbc1,
	                     type_mbc2,
	                     type_mbc3,
	                     type_mbc5,
	                     type_huc1,
						 type_huc3 };
	Cartridgetype type = type_plain;
	unsigned rambanks = 1;
	unsigned rombanks = 2;
	bool cgb = false;

	{
		unsigned char header[0x150];
		if (romfilelength >= sizeof header)
			std::memcpy(header, romfiledata, sizeof header);
		else
			return LOADRES_IO_ERROR;

		switch (header[0x0147]) {
		case 0x00: type = type_plain; break;
		case 0x01:
		case 0x02:
		case 0x03: type = type_mbc1; break;
		case 0x05:
		case 0x06: type = type_mbc2; break;
		case 0x08:
		case 0x09: type = type_plain; break;
		case 0x0B:
		case 0x0C:
		case 0x0D: return LOADRES_UNSUPPORTED_MBC_MMM01;
		case 0x0F:
		case 0x10:
		case 0x11:
		case 0x12:
		case 0x13: type = type_mbc3; break;
		case 0x15:
		case 0x16:
		case 0x17: return LOADRES_UNSUPPORTED_MBC_MBC4;
		case 0x19:
		case 0x1A:
		case 0x1B:
		case 0x1C:
		case 0x1D:
		case 0x1E: type = type_mbc5; break;
		case 0x20: return LOADRES_UNSUPPORTED_MBC_MBC6;
		case 0x22: return LOADRES_UNSUPPORTED_MBC_MBC7;
		case 0xFC: return LOADRES_UNSUPPORTED_MBC_POCKET_CAMERA;
		case 0xFD: return LOADRES_UNSUPPORTED_MBC_TAMA5;
		case 0xFE: type = type_huc3; break;
		case 0xFF: type = type_huc1; break;
		default:   return LOADRES_BAD_FILE_OR_UNKNOWN_MBC;
		}

		/*switch (header[0x0148]) {
		case 0x00: rombanks = 2; break;
		case 0x01: rombanks = 4; break;
		case 0x02: rombanks = 8; break;
		case 0x03: rombanks = 16; break;
		case 0x04: rombanks = 32; break;
		case 0x05: rombanks = 64; break;
		case 0x06: rombanks = 128; break;
		case 0x07: rombanks = 256; break;
		case 0x08: rombanks = 512; break;
		case 0x52: rombanks = 72; break;
		case 0x53: rombanks = 80; break;
		case 0x54: rombanks = 96; break;
		default: return -1;
		}*/

		rambanks = numRambanksFromH14x(header[0x147], header[0x149]);
		cgb = !forceDmg;
	}
	std::size_t const filesize = romfilelength;
	rombanks = std::max(pow2ceil(filesize / rombank_size()), 2u);

	mbc_.reset();
	memptrs_.reset(rombanks, rambanks, cgb ? 8 : 2);
	rtc_.set(false, 0);
	huc3_.set(false);

	std::memcpy(memptrs_.romdata(), romfiledata, (filesize / rombank_size() * rombank_size()));
	std::memset(memptrs_.romdata() + filesize / rombank_size() * rombank_size(),
	            0xFF,
	            (rombanks - filesize / rombank_size()) * rombank_size());
	enforce8bit(memptrs_.romdata(), rombanks * rombank_size());

	switch (type) {
	case type_plain: mbc_.reset(new Mbc0(memptrs_)); break;
	case type_mbc1:
		if (multicartCompat && presumedMulti64Mbc1(memptrs_.romdata(), rombanks)) {
			mbc_.reset(new Mbc1Multi64(memptrs_));
		} else
			mbc_.reset(new Mbc1(memptrs_));

		break;
	case type_mbc2: mbc_.reset(new Mbc2(memptrs_)); break;
	case type_mbc3:
		mbc_.reset(new Mbc3(memptrs_, hasRtc(memptrs_.romdata()[0x147]) ? &rtc_ : 0));
		break;
	case type_mbc5: mbc_.reset(new Mbc5(memptrs_)); break;
	case type_huc1: mbc_.reset(new HuC1(memptrs_)); break;
	case type_huc3:
		huc3_.set(true);
		mbc_.reset(new HuC3(memptrs_, &huc3_));
		break;
	}

	return LOADRES_OK;
}

void Cartridge::loadSavedata(char const *data, unsigned long const cc) {
	if (hasBattery(memptrs_.romdata()[0x147])) {
		int length = memptrs_.rambankdataend() - memptrs_.rambankdata();
		std::memcpy(memptrs_.rambankdata(), data, length);
		data += length;
		enforce8bit(memptrs_.rambankdata(), length);
	}

	if (hasRtc(memptrs_.romdata()[0x147])) {
		timeval basetime;
		basetime.tv_sec = (*data++);
		basetime.tv_sec = basetime.tv_sec << 8 | (*data++);
		basetime.tv_sec = basetime.tv_sec << 8 | (*data++);
		basetime.tv_sec = basetime.tv_sec << 8 | (*data++);
		basetime.tv_usec = (*data++);
		basetime.tv_usec = basetime.tv_usec << 8 | (*data++);
		basetime.tv_usec = basetime.tv_usec << 8 | (*data++);
		basetime.tv_usec = basetime.tv_usec << 8 | (*data++);

		time_.setBaseTime(basetime, cc);
	}
}

int Cartridge::saveSavedataLength() {
	int ret = 0;
	if (hasBattery(memptrs_.romdata()[0x147])) {
		ret = memptrs_.rambankdataend() - memptrs_.rambankdata();
	}
	if (hasRtc(memptrs_.romdata()[0x147])) {
		ret += 8;
	}
	return ret;
}

void Cartridge::saveSavedata(char *dest, unsigned long const cc) {
	if (hasBattery(memptrs_.romdata()[0x147])) {
		int length = memptrs_.rambankdataend() - memptrs_.rambankdata();
		std::memcpy(dest, memptrs_.rambankdata(), length);
		dest += length;
	}

	if (hasRtc(memptrs_.romdata()[0x147])) {
		timeval basetime = time_.baseTime(cc);
		*dest++ = (basetime.tv_sec  >> 24 & 0xFF);
		*dest++ = (basetime.tv_sec  >> 16 & 0xFF);
		*dest++ = (basetime.tv_sec  >>  8 & 0xFF);
		*dest++ = (basetime.tv_sec        & 0xFF);
		*dest++ = (basetime.tv_usec >> 24 & 0xFF);
		*dest++ = (basetime.tv_usec >> 16 & 0xFF);
		*dest++ = (basetime.tv_usec >>  8 & 0xFF);
		*dest++ = (basetime.tv_usec       & 0xFF);
	}
}

bool Cartridge::getMemoryArea(int which, unsigned char **data, int *length) const {
	if (!data || !length)
		return false;

	switch (which)
	{
	case 0:
		*data = memptrs_.vramdata();
		*length = memptrs_.vramdataend() - memptrs_.vramdata();
		return true;
	case 1:
		*data = memptrs_.romdata();
		*length = memptrs_.romdataend() - memptrs_.romdata();
		return true;
	case 2:
		*data = memptrs_.wramdata(0);
		*length = memptrs_.wramdataend() - memptrs_.wramdata(0);
		return true;
	case 3:
		*data = memptrs_.rambankdata();
		*length = memptrs_.rambankdataend() - memptrs_.rambankdata();
		return true;

	default:
		return false;
	}
	return false;
}

SYNCFUNC(Cartridge)
{
	SSS(huc3_);
	SSS(memptrs_);
	SSS(time_);
	SSS(rtc_);
	TSS(mbc_);
}
