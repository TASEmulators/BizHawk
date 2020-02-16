//
//   Copyright (C) 2010 by sinamas <sinamas at users.sourceforge.net>
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

#include "interruptrequester.h"
#include "savestate.h"

namespace gambatte {

InterruptRequester::InterruptRequester()
: eventTimes_(disabled_time)
, minIntTime_(0)
, ifreg_(0)
, iereg_(0)
{
}

void InterruptRequester::loadState(SaveState const &state) {
	minIntTime_ = state.mem.minIntTime;
	ifreg_ = state.mem.ioamhram.get()[0x10F];
	iereg_ = state.mem.ioamhram.get()[0x1FF] & 0x1F;
	intFlags_.set(state.mem.IME, state.mem.halted);

	eventTimes_.setValue<intevent_interrupts>(intFlags_.imeOrHalted() && pendingIrqs()
		? minIntTime_
		: static_cast<unsigned long>(disabled_time));
}

void InterruptRequester::resetCc(unsigned long oldCc, unsigned long newCc) {
	minIntTime_ = minIntTime_ < oldCc ? 0 : minIntTime_ - (oldCc - newCc);

	if (eventTimes_.value(intevent_interrupts) != disabled_time)
		eventTimes_.setValue<intevent_interrupts>(minIntTime_);
}

void InterruptRequester::ei(unsigned long cc) {
	intFlags_.setIme();
	minIntTime_ = cc + 1;

	if (pendingIrqs())
		eventTimes_.setValue<intevent_interrupts>(minIntTime_);
}

void InterruptRequester::di() {
	intFlags_.unsetIme();

	if (!intFlags_.imeOrHalted())
		eventTimes_.setValue<intevent_interrupts>(disabled_time);
}

void InterruptRequester::halt() {
	intFlags_.setHalted();

	if (pendingIrqs())
		eventTimes_.setValue<intevent_interrupts>(minIntTime_);
}

void InterruptRequester::unhalt() {
	intFlags_.unsetHalted();

	if (!intFlags_.imeOrHalted())
		eventTimes_.setValue<intevent_interrupts>(disabled_time);
}

void InterruptRequester::flagIrq(unsigned bit) {
	ifreg_ |= bit;

	if (intFlags_.imeOrHalted() && pendingIrqs())
		eventTimes_.setValue<intevent_interrupts>(minIntTime_);
}

void InterruptRequester::flagIrq(unsigned bit, unsigned long cc) {
	unsigned const prevPending = pendingIrqs();
	ifreg_ |= bit;

	if (!prevPending && pendingIrqs() && intFlags_.imeOrHalted()) {
		minIntTime_ = std::max(minIntTime_, cc);
		eventTimes_.setValue<intevent_interrupts>(minIntTime_);
	}
}

void InterruptRequester::setIereg(unsigned iereg) {
	iereg_ = iereg & 0x1F;

	if (intFlags_.imeOrHalted()) {
		eventTimes_.setValue<intevent_interrupts>(pendingIrqs()
			? minIntTime_
			: static_cast<unsigned long>(disabled_time));
	}
}

void InterruptRequester::setIfreg(unsigned ifreg) {
	ifreg_ = ifreg;

	if (intFlags_.imeOrHalted()) {
		eventTimes_.setValue<intevent_interrupts>(pendingIrqs()
			? minIntTime_
			: static_cast<unsigned long>(disabled_time));
	}
}

void InterruptRequester::setMinIntTime(unsigned long cc) {
	minIntTime_ = cc;

	if (eventTimes_.value(intevent_interrupts) < minIntTime_)
		eventTimes_.setValue<intevent_interrupts>(minIntTime_);
}

SYNCFUNC(InterruptRequester)
{
	SSS(eventTimes_);
	NSS(minIntTime_);
	NSS(ifreg_);
	NSS(iereg_);
	NSS(intFlags_.flags_);
}

}
