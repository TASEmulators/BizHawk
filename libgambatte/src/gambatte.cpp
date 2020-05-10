//
//   Copyright (C) 2007 by sinamas <sinamas at users.sourceforge.net>
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

#include "gambatte.h"
#include "cpu.h"
#include "initstate.h"
#include "savestate.h"
#include <cstring>
#include <sstream>

namespace gambatte {

struct GB::Priv {
	CPU cpu;
	unsigned loadflags;
	unsigned layersMask;

	uint_least32_t vbuff[160*144];

	Priv() : loadflags(0), layersMask(layer_mask_bg | layer_mask_window | layer_mask_obj)
	{
	}
};

GB::GB() : p_(new Priv) {}

GB::~GB() {
	delete p_;
}

std::ptrdiff_t GB::runFor(gambatte::uint_least32_t *const soundBuf, std::size_t &samples) {
	if (!p_->cpu.loaded()) {
		samples = 0;
		return -1;
	}

	p_->cpu.setVideoBuffer(p_->vbuff, 160);
	p_->cpu.setSoundBuffer(soundBuf);

	long const cyclesSinceBlit = p_->cpu.runFor(samples * 2);
	samples = p_->cpu.fillSoundBuffer();
	return cyclesSinceBlit >= 0
	     ? static_cast<std::ptrdiff_t>(samples) - (cyclesSinceBlit >> 1)
	     : cyclesSinceBlit;
}

void GB::setLayers(unsigned mask) {
	p_->cpu.setLayers(mask);
}

void GB::blitTo(gambatte::uint_least32_t *videoBuf, std::ptrdiff_t pitch) {
	gambatte::uint_least32_t *src = p_->vbuff;
	gambatte::uint_least32_t *dst = videoBuf;

	for (int i = 0; i < 144; i++)
	{
		std::memcpy(dst, src, sizeof gambatte::uint_least32_t * 160);
		src += 160;
		dst += pitch;
	}
}

void GB::reset() {
	if (p_->cpu.loaded()) {

		int length = p_->cpu.saveSavedataLength();
		char *s;
		if (length > 0)
		{
			s = (char *) std::malloc(length);
			p_->cpu.saveSavedata(s);
		}

		SaveState state;
		p_->cpu.setStatePtrs(state);
		setInitState(state, !(p_->loadflags & FORCE_DMG));
		p_->cpu.loadState(state);
		if (length > 0)
		{
			p_->cpu.loadSavedata(s);
			std::free(s);
		}
	}
}

void GB::setInputGetter(unsigned (*getInput)()) {
	p_->cpu.setInputGetter(getInput);
}

void GB::setReadCallback(MemoryCallback callback) {
	p_->cpu.setReadCallback(callback);
}

void GB::setWriteCallback(MemoryCallback callback) {
	p_->cpu.setWriteCallback(callback);
}

void GB::setExecCallback(MemoryCallback callback) {
	p_->cpu.setExecCallback(callback);
}

void GB::setCDCallback(CDCallback cdc) {
	p_->cpu.setCDCallback(cdc);
}

void GB::setTraceCallback(void (*callback)(void *)) {
	p_->cpu.setTraceCallback(callback);
}

void GB::setScanlineCallback(void (*callback)(), int sl) {
	p_->cpu.setScanlineCallback(callback, sl);
}

void GB::setLinkCallback(void(*callback)()) {
	p_->cpu.setLinkCallback(callback);
}

void GB::setTimeMode(bool useCycles) {
	p_->cpu.setTimeMode(useCycles);
}

void GB::setRtcDivisorOffset(long const rtcDivisorOffset) {
	p_->cpu.setRtcDivisorOffset(rtcDivisorOffset);
}

LoadRes GB::load(char const *romfiledata, unsigned romfilelength, unsigned const flags) {
	LoadRes const loadres = p_->cpu.load(romfiledata, romfilelength, flags);

	if (loadres == LOADRES_OK) {
		SaveState state;
		p_->cpu.setStatePtrs(state);
		p_->loadflags = flags;
		setInitState(state, !(flags & FORCE_DMG));
		p_->cpu.loadState(state);
	}

	return loadres;
}

int GB::loadBios(char const* biosfiledata, std::size_t size) {
	p_->cpu.setBios(biosfiledata, size);
	return 0;
}

bool GB::isCgb() const {
	return p_->cpu.isCgb();
}

bool GB::isLoaded() const {
	return p_->cpu.loaded();
}

void GB::saveSavedata(char *dest) {
	if (p_->cpu.loaded())
		p_->cpu.saveSavedata(dest);
}
void GB::loadSavedata(char const *data) {
	if (p_->cpu.loaded())
		p_->cpu.loadSavedata(data);
}
int GB::saveSavedataLength() {
	if (p_->cpu.loaded())
		return p_->cpu.saveSavedataLength();
	else
		return -1;
}

bool GB::getMemoryArea(int which, unsigned char **data, int *length) {
	if (p_->cpu.loaded())
		return p_->cpu.getMemoryArea(which, data, length);
	else
		return false;
}

unsigned char GB::externalRead(unsigned short addr) {
	if (p_->cpu.loaded())
		return p_->cpu.externalRead(addr);
	else
		return 0;
}

void GB::externalWrite(unsigned short addr, unsigned char val) {
	if (p_->cpu.loaded())
		p_->cpu.externalWrite(addr, val);
}


void GB::setDmgPaletteColor(int palNum, int colorNum, unsigned long rgb32) {
	p_->cpu.setDmgPaletteColor(palNum, colorNum, rgb32);
}

void GB::setCgbPalette(unsigned *lut) {
	p_->cpu.setCgbPalette(lut);
}

std::string const GB::romTitle() const {
	if (p_->cpu.loaded()) {
		char title[0x11];
		std::memcpy(title, p_->cpu.romTitle(), 0x10);
		title[title[0xF] & 0x80 ? 0xF : 0x10] = '\0';
		return std::string(title);
	}

	return std::string();
}

int GB::linkStatus(int which) {
	return p_->cpu.linkStatus(which);
}

void GB::getRegs(int *dest) {
	p_->cpu.getRegs(dest);
}

void GB::setInterruptAddresses(int *addrs, int numAddrs) {
	p_->cpu.setInterruptAddresses(addrs, numAddrs);
}

int GB::getHitInterruptAddress() {
	return p_->cpu.getHitInterruptAddress();
}

SYNCFUNC(GB)
{
	SSS(p_->cpu);
	NSS(p_->loadflags);
	NSS(p_->vbuff);
}

}
