//	VirtualDub - Video processing and capture application
//	Copyright (C) 1998-2001 Avery Lee
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

#ifndef f_VD2_RIZA_AUDIOOUT_H
#define f_VD2_RIZA_AUDIOOUT_H

#include <vd2/system/thread.h>
#include <vd2/system/vdstl.h>

struct HWAVEOUT__;
struct tWAVEFORMATEX;

class IVDAudioOutput {
public:
	virtual ~IVDAudioOutput() = default;

	virtual uint32	GetPreferredSamplingRate(const wchar_t *preferredDevice) const = 0;

	virtual bool	Init(uint32 bufsize, uint32 bufcount, const tWAVEFORMATEX *wf, const wchar_t *preferredDevice) = 0;
	virtual void	Shutdown() = 0;
	virtual void	GoSilent() = 0;

	virtual bool	IsSilent() = 0;
	virtual bool	IsFrozen() = 0;
	virtual uint32	GetAvailSpace() = 0;
	virtual uint32	GetBufferLevel() = 0;
	virtual uint32	EstimateHWBufferLevel(bool *underflowDetected) = 0;
	virtual sint32	GetPosition() = 0;
	virtual sint32	GetPositionBytes() = 0;
	virtual double	GetPositionTime() = 0;

	// Returns the mixing rate in Hz. This is the rate at which audio must be produced.
	// This will differ for WASAPI; WaveOut, DirectSound, and XAudio2 will always return
	// the requested rate.
	virtual uint32	GetMixingRate() const = 0;

	virtual bool	Start() = 0;
	virtual bool	Stop() = 0;
	virtual bool	Flush() = 0;

	virtual bool	Write(const void *data, uint32 len) = 0;
	virtual bool	Finalize(uint32 timeout = (uint32)-1) = 0;
};

IVDAudioOutput *VDCreateAudioOutputWaveOutW32();
IVDAudioOutput *VDCreateAudioOutputDirectSoundW32();
IVDAudioOutput *VDCreateAudioOutputXAudio2W32();
IVDAudioOutput *VDCreateAudioOutputWASAPIW32();

#endif
