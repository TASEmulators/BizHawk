//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2017 Avery Lee
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

#include <stdafx.h>
#include "trace.h"

ATTraceGroup::ATTraceGroup() {
}

ATTraceGroup::~ATTraceGroup() {
	while(!mChannels.empty()) {
		auto *p = mChannels.back();
		mChannels.pop_back();

		p->Release();
	}
}

void ATTraceGroup::SetName(const wchar_t *name) {
	mName = name;
}

const wchar_t *ATTraceGroup::GetName() const {
	return mName.c_str();
}

void ATTraceGroup::AddChannel(IATTraceChannel *ch) {
	mChannels.push_back(ch);
	ch->AddRef();
}

ATTraceChannelSimple *ATTraceGroup::AddSimpleChannel(uint64 tickOffset, double tickScale, const wchar_t *name) {
	vdrefptr<ATTraceChannelSimple> ch { new ATTraceChannelSimple(tickOffset, tickScale, name) };

	mChannels.push_back(ch);

	return ch.release();
}

ATTraceChannelFormatted *ATTraceGroup::AddFormattedChannel(uint64 tickOffset, double tickScale, const wchar_t *name) {
	vdrefptr<ATTraceChannelFormatted> ch { new ATTraceChannelFormatted(tickOffset, tickScale, name) };

	mChannels.push_back(ch);

	return ch.release();
}

size_t ATTraceGroup::GetChannelCount() const {
	return mChannels.size();
}

IATTraceChannel *ATTraceGroup::GetChannel(size_t index) const {
	return mChannels[index];
}

double ATTraceGroup::GetDuration() const {
	double duration = 0;

	for(IATTraceChannel *ch : mChannels)
		duration = std::max(duration, ch->GetDuration());

	return duration;
}

///////////////////////////////////////////////////////////////////////////

ATTraceCollection::ATTraceCollection() {
}

ATTraceCollection::~ATTraceCollection() {
	while(!mGroups.empty()) {
		mGroups.back()->Release();
		mGroups.pop_back();
	}
}

ATTraceGroup *ATTraceCollection::AddGroup(const wchar_t *name, ATTraceGroupType type) {
	vdrefptr<ATTraceGroup> group { new ATTraceGroup };

	group->SetName(name);
	group->SetType(type);

	mGroups.push_back(group);

	return group.release();
}

size_t ATTraceCollection::GetGroupCount() const {
	return mGroups.size();
}

ATTraceGroup *ATTraceCollection::GetGroup(size_t index) const {
	return mGroups[index];
}

///////////////////////////////////////////////////////////////////////////

ATTraceChannelTickBased::ATTraceChannelTickBased(uint64 tickOffset, double tickScale, const wchar_t *name)
	: mTickOffset(tickOffset)
	, mTickScale(tickScale)
	, mName(name)
{
}

void ATTraceChannelTickBased::TruncateLastEvent(uint64 tick) {
	if (!mEvents.empty()) {
		const double t = (double)(tick - mTickOffset) * mTickScale;
		SimpleEvent& ev = mEvents.back();

		if (ev.mEndTime > t)
			ev.mEndTime = t;
	}
}

void *ATTraceChannelTickBased::AsInterface(uint32 iid) {
	return nullptr;
}

const wchar_t *ATTraceChannelTickBased::GetName() const {
	return mName.c_str();
}

double ATTraceChannelTickBased::GetDuration() const {
	return mEvents.empty() ? 0 : mEvents.back().mEndTime;
}

void ATTraceChannelTickBased::AddRawTickEvent(uint64 start, uint64 end, const void *data, uint32 bgColor) {
	const uint32 bgLuma = (bgColor & 0xFF00FF) * (uint32)(54 + (19 << 16)) + (bgColor & 0xFF00) * (uint32)(183 << 8);
	const uint32 fgColor = (bgLuma >= 0x80000000) ? 0 : 0xFFFFFF;

	mEvents.push_back(
		SimpleEvent {
			(double)(start - mTickOffset) * mTickScale,
			(double)(end - mTickOffset) * mTickScale,
			data,
			bgColor,
			fgColor
		}
	);
}

void ATTraceChannelTickBased::AddOpenRawTickEvent(uint64 start, const void *data, uint32 bgColor) {
	const uint32 bgLuma = (bgColor & 0xFF00FF) * (uint32)(54 + (19 << 16)) + (bgColor & 0xFF00) * (uint32)(183 << 8);
	const uint32 fgColor = (bgLuma >= 0x80000000) ? 0 : 0xFFFFFF;

	mEvents.push_back(
		SimpleEvent {
			(double)(start - mTickOffset) * mTickScale,
			kATTraceTime_Infinity,
			data,
			bgColor,
			fgColor
		}
	);
}

bool ATTraceChannelTickBased::IsEmpty() const {
	return mEvents.empty();
}

void ATTraceChannelTickBased::StartIteration(double startTime, double endTime, double eventThreshold) {
	auto it = std::lower_bound(mEvents.cbegin(), mEvents.cend(), startTime,
		[](const SimpleEvent& ev, double t) { return ev.mStartTime < t; });

	if (it != mEvents.begin() && std::prev(it)->mEndTime > startTime - mIterThreshold)
		--it;

	mIt = it;
	mIterEndTime = endTime;
	mIterThreshold = eventThreshold;
}

bool ATTraceChannelTickBased::GetNextEvent(ATTraceEvent& ev) {
	if (mIt == mEvents.end())
		return false;

	const SimpleEvent& sev = *mIt;
	++mIt;

	if (sev.mStartTime >= mIterEndTime) {
		mIt = mEvents.end();
		return false;
	}

	ev.mEventStart = sev.mStartTime;
	ev.mEventStop = sev.mEndTime;
	ev.mFgColor = sev.mFgColor;
	ev.mBgColor = sev.mBgColor;

	if (sev.mEndTime - sev.mStartTime < mIterThreshold) {
		double t = sev.mEndTime;
		ev.mpName = nullptr;

		while(mIt != mEvents.end()) {
			const SimpleEvent& sev2 = *mIt;

			if (sev2.mEndTime - t > mIterThreshold || sev2.mEndTime - sev2.mStartTime >= mIterThreshold)
				break;

			t = sev2.mEndTime;
			++mIt;

			if (t >= mIterEndTime)
				break;
		}

		ev.mEventStop = t;
	} else {
		DecodeName(ev, sev.mpData);
	}

	return true;
}

///////////////////////////////////////////////////////////////////////////

void ATTraceChannelSimple::DecodeName(ATTraceEvent& ev, const void *data) const {
	ev.mpName = (const wchar_t *)data;
}

///////////////////////////////////////////////////////////////////////////

void ATTraceChannelFormatted::DecodeName(ATTraceEvent& ev, const void *data) const {
	const FormatterInfo *fi = (const FormatterInfo *)data;

	fi->mpFormatter(ev.mNameBuffer, fi->mpData);
	ev.mpName = ev.mNameBuffer.c_str();
}

void *ATTraceChannelFormatted::AddRawFormattedTickEvent(uint64 tickStart, uint64 tickEnd, uint32 color, size_t size, size_t align, FormatterInfo *fi) {
	fi->mpDeleter = nullptr;

	AddRawTickEvent(tickStart, tickEnd, fi, color);

	return mLinearAlloc.Allocate(size, align);
}

void *ATTraceChannelFormatted::AddOpenRawFormattedTickEvent(uint64 tickStart, uint32 color, size_t size, size_t align, FormatterInfo *fi) {
	fi->mpDeleter = nullptr;

	AddOpenRawTickEvent(tickStart, fi, color);

	return mLinearAlloc.Allocate(size, align);
}

