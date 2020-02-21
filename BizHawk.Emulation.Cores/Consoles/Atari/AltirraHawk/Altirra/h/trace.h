//	Altirra - Atari 800/800XL/5200 emulator
//	Execution trace data structures
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

#ifndef f_TRACE_H
#define f_TRACE_H

#include <atomic>
#include <vd2/system/linearalloc.h>
#include <vd2/system/refcount.h>

class VDStringW;
class ATTraceChannelSimple;
class ATTraceChannelFormatted;

///////////////////////////////////////////////////////////////////////////

enum ATTraceGroupType {
	kATTraceGroupType_Normal,
	kATTraceGroupType_Frames,
	kATTraceGroupType_Video,
	kATTraceGroupType_CPUHistory,
	kATTraceGroupType_Tape
};

static constexpr uint32 kATTraceColor_Default			= 0xA0FFC0;
static constexpr uint32 kATTraceColor_CPUThread_Idle	= 0xB0B0B0;
static constexpr uint32 kATTraceColor_CPUThread_Main	= 0xA0FFC0;
static constexpr uint32 kATTraceColor_CPUThread_CIO		= 0xA0FFC0;
static constexpr uint32 kATTraceColor_CPUThread_SIO		= 0xA0FFC0;
static constexpr uint32 kATTraceColor_CPUThread_IRQ		= 0x4080FF;
static constexpr uint32 kATTraceColor_CPUThread_VBI		= 0xF9BC52;
static constexpr uint32 kATTraceColor_CPUThread_VBIDeferred = 0xC2EF47;
static constexpr uint32 kATTraceColor_CPUThread_DLI		= 0xFF9050;
static constexpr uint32 kATTraceColor_IO_Default		= kATTraceColor_Default;
static constexpr uint32 kATTraceColor_IO_Read			= 0xA0FFC0;
static constexpr uint32 kATTraceColor_IO_Write			= 0xFF9050;
static constexpr uint32 kATTraceColor_Tape_Play			= 0xA0FFC0;
static constexpr uint32 kATTraceColor_Tape_Record		= 0xFF9050;

static constexpr double kATTraceTime_Infinity = 1e+10;

///////////////////////////////////////////////////////////////////////////

struct ATTraceEvent {
	double mEventStart;
	double mEventStop;
	uint32 mFgColor;
	uint32 mBgColor;
	VDStringW mNameBuffer;
	const wchar_t *mpName;
};

class IATTraceChannel : public IVDRefUnknown {
public:
	virtual const wchar_t *GetName() const = 0;
	virtual double GetDuration() const = 0;
	virtual bool IsEmpty() const = 0;
	virtual void StartIteration(double startTime, double endTime, double eventThreshold) = 0;
	virtual bool GetNextEvent(ATTraceEvent& ev) = 0;
};

class ATTraceGroup final : public vdrefcount {
	ATTraceGroup(const ATTraceGroup&) = delete;
	ATTraceGroup& operator=(const ATTraceGroup&) = delete;

public:
	ATTraceGroup();
	~ATTraceGroup();

	const wchar_t *GetName() const;
	void SetName(const wchar_t *name);

	ATTraceGroupType GetType() const { return mType; }
	void SetType(ATTraceGroupType type) { mType = type; }

	void AddChannel(IATTraceChannel *ch);
	ATTraceChannelSimple *AddSimpleChannel(uint64 tickOffset, double tickScale, const wchar_t *name);
	ATTraceChannelFormatted *AddFormattedChannel(uint64 tickOffset, double tickScale, const wchar_t *name);

	size_t GetChannelCount() const;
	IATTraceChannel *GetChannel(size_t index) const;

	double GetDuration() const;

private:
	vdfastvector<IATTraceChannel *> mChannels;
	VDStringW mName;
	ATTraceGroupType mType = kATTraceGroupType_Normal;
};

///////////////////////////////////////////////////////////////////////////

class ATTraceCollection : public vdrefcount {
	ATTraceCollection(const ATTraceCollection&) = delete;
	ATTraceCollection& operator=(const ATTraceCollection&) = delete;
public:
	ATTraceCollection();
	~ATTraceCollection();

	ATTraceGroup *AddGroup(const wchar_t *name, ATTraceGroupType type = kATTraceGroupType_Normal);

	size_t GetGroupCount() const;
	ATTraceGroup *GetGroup(size_t index) const;

private:
	vdfastvector<ATTraceGroup *> mGroups;
};

///////////////////////////////////////////////////////////////////////////

class ATTraceMemoryTracker {
public:
	void AddSize(uint64 delta) { mSize += delta; }
	uint64 GetSize() const { return mSize; }

private:
	std::atomic<uint64> mSize { 0 };
};

struct ATTraceContext {
	uint64 mBaseTime;
	double mBaseTickScale;
	vdrefptr<ATTraceCollection> mpCollection;
	ATTraceMemoryTracker mMemTracker;
};

struct ATTraceSettings {
	bool mbTraceVideo;
	uint32 mTraceVideoDivisor;
	bool mbTraceCpuInsns;
	bool mbTraceBasic;
	bool mbAutoLimitTraceMemory;
};

///////////////////////////////////////////////////////////////////////////

class ATTraceChannelTickBased : public vdrefcounted<IATTraceChannel> {
public:
	ATTraceChannelTickBased(uint64 tickOffset, double tickScale, const wchar_t *name);

	void TruncateLastEvent(uint64 tick);

	void *AsInterface(uint32 iid) override;

	const wchar_t *GetName() const override final;
	double GetDuration() const override final;
	bool IsEmpty() const override final;
	void StartIteration(double startTime, double endTime, double eventThreshold) override final;
	bool GetNextEvent(ATTraceEvent& ev) override final;

protected:
	virtual void DecodeName(ATTraceEvent& ev, const void *data) const = 0;

	void AddRawTickEvent(uint64 tickStart, uint64 tickEnd, const void *data, uint32 bgColor);
	void AddOpenRawTickEvent(uint64 tickStart, const void *data, uint32 bgColor);

private:
	struct SimpleEvent {
		double mStartTime;
		double mEndTime;
		const void *mpData;
		uint32 mBgColor;
		uint32 mFgColor;
	};

	vdfastdeque<SimpleEvent> mEvents;
	vdfastdeque<SimpleEvent>::const_iterator mIt;
	double mIterEndTime;
	double mIterThreshold;
	double mTickScale;
	uint64 mTickOffset;
	VDStringW mName;
};

class ATTraceChannelSimple final : public ATTraceChannelTickBased {
public:
	using ATTraceChannelTickBased::ATTraceChannelTickBased;

	void AddTickEvent(uint64 tickStart, uint64 tickEnd, const wchar_t *s, uint32 color) {
		AddRawTickEvent(tickStart, tickEnd, s, color);
	}

	void AddOpenTickEvent(uint64 tickStart, const wchar_t *s, uint32 color) {
		AddOpenRawTickEvent(tickStart, s, color);
	}

	void DecodeName(ATTraceEvent& ev, const void *data) const override;
};

class ATTraceChannelFormatted final : public ATTraceChannelTickBased {
	typedef void (*RawFormatter)(VDStringW& ev, const void *data);
	typedef void (*RawDeleter)(void *data);

	struct FormatterInfo {
		RawFormatter mpFormatter;
		RawDeleter mpDeleter;
		void *mpData;
	};

public:
	using ATTraceChannelTickBased::ATTraceChannelTickBased;

	template<typename Formatter>
	void AddTickEvent(uint64 tickStart, uint64 tickEnd, Formatter f, uint32 color) {
		FormatterInfo& fi = mDeleters.push_back();
		fi.mpFormatter = [](VDStringW& ev, const void *data) { (*(const Formatter *)data)(ev); };

		void *p = AddRawFormattedTickEvent(tickStart, tickEnd, color, sizeof(f), alignof(Formatter), &fi);

		fi.mpData = p;

		new((Formatter *)p) Formatter(std::move(f));

		fi.mpDeleter = std::is_trivially_destructible<Formatter>::value ? nullptr : (RawDeleter)[](void *p) { ((Formatter *)p)->~Formatter(); };
	}

	template<typename Formatter>
	void AddOpenTickEvent(uint64 tickStart, Formatter f, uint32 color) {
		FormatterInfo& fi = mDeleters.push_back();
		fi.mpFormatter = [](VDStringW& ev, const void *data) { (*(const Formatter *)data)(ev); };

		void *p = AddOpenRawFormattedTickEvent(tickStart, color, sizeof(f), alignof(Formatter), &fi);

		fi.mpData = p;

		new((Formatter *)p) Formatter(std::move(f));

		fi.mpDeleter = std::is_trivially_destructible<Formatter>::value ? nullptr : (RawDeleter)[](void *p) { ((Formatter *)p)->~Formatter(); };
	}

	template<typename... Args>
	void AddTickEventF(uint64 tickStart, uint64 tickEnd, uint32 color, const wchar_t *format, Args&&... args) {
		AddTickEvent(tickStart, tickEnd,
			[=](VDStringW& s) {
				s.sprintf(format, args...);
			},
			color
		);
	}

	template<typename... Args>
	void AddOpenTickEventF(uint64 tickStart, uint32 color, const wchar_t *format, Args&&... args) {
		AddOpenTickEvent(tickStart,
			[=](VDStringW& s) {
				s.sprintf(format, args...);
			},
			color
		);
	}

	void DecodeName(ATTraceEvent& ev, const void *data) const override;

private:
	void *AddRawFormattedTickEvent(uint64 tickStart, uint64 tickEnd, uint32 color, size_t size, size_t align, FormatterInfo *fi);
	void *AddOpenRawFormattedTickEvent(uint64 tickStart, uint32 color, size_t size, size_t align, FormatterInfo *fi);

	vdfastdeque<FormatterInfo> mDeleters;
	VDLinearAllocator mLinearAlloc;
};

#endif
