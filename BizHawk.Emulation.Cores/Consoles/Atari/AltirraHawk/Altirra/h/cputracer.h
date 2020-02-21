//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2008-2017 Avery Lee
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

#ifndef f_AT_CPUTRACER_H
#define f_AT_CPUTRACER_H

#include <vd2/system/linearalloc.h>
#include <at/atcore/scheduler.h>

struct ATTraceContext;
class ATTraceChannelSimple;
class ATTraceChannelCPUHistory;
class ATTraceChannelFormatted;

class ATCPUTracer final : public IATSchedulerCallback {
	ATCPUTracer(const ATCPUTracer&) = delete;
	ATCPUTracer& operator=(const ATCPUTracer&) = delete;
public:
	ATCPUTracer();
	~ATCPUTracer();

	void Init(ATCPUEmulator *cpu, ATScheduler *scheduler, ATScheduler *slowScheduler, IATCPUTimestampDecoderProvider *tsdprovider, ATTraceContext *traceContext, bool traceInsns, bool traceBasic);
	void Shutdown();

private:
	void OnScheduledEvent(uint32 id) override;

private:
	void Reschedule();
	void Update();

	ATCPUEmulator *mpCPU = nullptr;
	IATCPUTimestampDecoderProvider *mpTSDProvider = nullptr;
	ATScheduler *mpScheduler = nullptr;
	ATScheduler *mpSlowScheduler = nullptr;
	ATEvent *mpUpdateEvent = nullptr;

	uint32	mLastHistoryCounter = 0;
	bool	mbAdjustStackNext = false;
	uint8	mLastS = 0;

	enum ThreadContext : sint8 {
		kThreadContext_Idle,
		kThreadContext_Main,
		kThreadContext_CIOIdle,
		kThreadContext_CIO,
		kThreadContext_SIOIdle,
		kThreadContext_SIO,
		kThreadContext_IRQ,
		kThreadContext_VBI,
		kThreadContext_VBIDeferred,
		kThreadContext_DLI,
	};

	int		mThreadContext = -1;
	uint32	mIdleCounter = 0;
	uint64	mIdleStartTime = 0;
	uint64	mThreadContextStartTime = 0;

	bool	mbTraceBasic = false;
	sint32	mBasicLineNo = -1;
	sint32	mBasicLineAddr = -1;
	uint64	mBasicLineStartTime = 0;

	ATTraceChannelSimple *mpTraceChannels[7] = {};
	ATTraceChannelCPUHistory *mpTraceChannelHistory = nullptr;
	ATTraceChannelFormatted *mpTraceChannelBasic = nullptr;

	struct StackEntry {
		sint8 mContext;
		uint8 mIdleCounter;
	};

	StackEntry	mStackTable[256] = {};
};

#endif	// f_AT_PROFILER_H
