/***************************************************************************
 *   Copyright (C) 2007 by Sindre Aamås                                    *
 *   aamas@stud.ntnu.no                                                    *
 *                                                                         *
 *   This program is free software; you can redistribute it and/or modify  *
 *   it under the terms of the GNU General Public License version 2 as     *
 *   published by the Free Software Foundation.                            *
 *                                                                         *
 *   This program is distributed in the hope that it will be useful,       *
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         *
 *   GNU General Public License version 2 for more details.                *
 *                                                                         *
 *   You should have received a copy of the GNU General Public License     *
 *   version 2 along with this program; if not, write to the               *
 *   Free Software Foundation, Inc.,                                       *
 *   59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.             *
 ***************************************************************************/
#include "gambatte.h"
#include "cpu.h"
#include "savestate.h"
#include "initstate.h"
#include <sstream>
#include <cstring>

namespace gambatte {
struct GB::Priv {
	CPU cpu;
	bool gbaCgbMode;
	unsigned layersMask;

	uint_least32_t vbuff[160*144];
	
	Priv() : gbaCgbMode(false), layersMask(LAYER_MASK_BG | LAYER_MASK_OBJ)
	{
	}

	~Priv()
	{
	}
};
	
GB::GB() : p_(new Priv) {}

GB::~GB() {
	//if (p_->cpu.loaded())
	//	p_->cpu.saveSavedata();
	
	delete p_;
}

long GB::runFor(gambatte::uint_least32_t *const soundBuf, unsigned &samples) {
	if (!p_->cpu.loaded()) {
		samples = 0;
		return -1;
	}
	
	p_->cpu.setVideoBuffer(p_->vbuff, 160);
	p_->cpu.setSoundBuffer(soundBuf);
	const long cyclesSinceBlit = p_->cpu.runFor(samples * 2);
	samples = p_->cpu.fillSoundBuffer();
	
	return cyclesSinceBlit < 0 ? cyclesSinceBlit : static_cast<long>(samples) - (cyclesSinceBlit >> 1);
}

void GB::setLayers(unsigned mask)
{
	p_->cpu.setLayers(mask);
}

void GB::blitTo(gambatte::uint_least32_t *videoBuf, int pitch)
{
	gambatte::uint_least32_t *src = p_->vbuff;
	gambatte::uint_least32_t *dst = videoBuf;

	for (int i = 0; i < 144; i++)
	{
		std::memcpy(dst, src, sizeof(gambatte::uint_least32_t) * 160);
		src += 160;
		dst += pitch;
	}
}

void GB::reset(const std::uint32_t now) {
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
		if (use_bios) 
		{
			p_->cpu.reset_bios(0);
		}

		setInitState(state, p_->cpu.isCgb(), p_->gbaCgbMode, now, use_bios);
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

void GB::setReadCallback(void (*callback)(unsigned)) {
	p_->cpu.setReadCallback(callback);
}

void GB::setWriteCallback(void (*callback)(unsigned)) {
	p_->cpu.setWriteCallback(callback);
}

void GB::setExecCallback(void (*callback)(unsigned)) {
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

void GB::setRTCCallback(std::uint32_t (*callback)()) {
	p_->cpu.setRTCCallback(callback);
}

int GB::load(const char *romfiledata, unsigned romfilelength, const char *biosfiledata, unsigned biosfilelength, const std::uint32_t now, const unsigned flags) {
	//if (p_->cpu.loaded())
	//	p_->cpu.saveSavedata();
	
	const int failed = p_->cpu.load(romfiledata, romfilelength, biosfiledata, biosfilelength, flags & FORCE_DMG, flags & MULTICART_COMPAT);
	use_bios = biosfilelength > 0 ? true : false;
	
	if (!failed) {
		SaveState state;
		p_->cpu.setStatePtrs(state);
		setInitState(state, p_->cpu.isCgb(), p_->gbaCgbMode = flags & GBA_CGB, now, use_bios);
		p_->cpu.loadState(state);
		//p_->cpu.loadSavedata();
	}
	
	return failed;
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
void GB::loadSavedata(const char *data) {
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

unsigned char GB::ExternalRead(unsigned short addr) {
	if (p_->cpu.loaded())
		return p_->cpu.ExternalRead(addr);
	else
		return 0;
}

void GB::ExternalWrite(unsigned short addr, unsigned char val) {
	if (p_->cpu.loaded())
		p_->cpu.ExternalWrite(addr, val);
}


void GB::setDmgPaletteColor(unsigned palNum, unsigned colorNum, unsigned rgb32) {
	p_->cpu.setDmgPaletteColor(palNum, colorNum, rgb32);
}

void GB::setCgbPalette(unsigned *lut) {
	p_->cpu.setCgbPalette(lut);
}

const std::string GB::romTitle() const {
	if (p_->cpu.loaded()) {
		char title[0x11];
		std::memcpy(title, p_->cpu.romTitle(), 0x10);
		title[(title[0xF] & 0x80) ? 0xF : 0x10] = '\0';
		return std::string(title);
	}
	
	return std::string();
}

int GB::LinkStatus(int which) {
	return p_->cpu.LinkStatus(which);
}

void GB::GetRegs(int *dest) {
	p_->cpu.GetRegs(dest);
}

SYNCFUNC(GB)
{
	SSS(p_->cpu);
	NSS(p_->gbaCgbMode);
	NSS(p_->vbuff);
	NSS(use_bios);
}

}
