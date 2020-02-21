//	Altirra - Atari 800/800XL/5200 emulator
//	I/O library - Windows .WAV file/structure support
//	Copyright (C) 2009-2016 Avery Lee
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

#ifndef f_AT_ATIO_WAV_H
#define f_AT_ATIO_WAV_H

#include <vd2/system/vdtypes.h>

namespace nsVDWinFormats {
	struct Guid {
		uint32	mData1;
		uint16	mData2;
		uint16	mData3;
		uint8	mData4[8];

		bool operator==(const Guid&) const;
	};

	/// Mirror of WAVEFORMATEX.
	struct WaveFormatEx {
		uint16	mFormatTag;
		uint16	mChannels;
		uint16	mSamplesPerSecLo;
		uint16	mSamplesPerSecHi;
		uint16	mAvgBytesPerSecLo;
		uint16	mAvgBytesPerSecHi;
		uint16	mBlockAlign;
		uint16	mBitsPerSample;
		uint16	mSize;

		uint32 GetSamplesPerSec() const {
			return (uint32)mSamplesPerSecLo + ((uint32)mSamplesPerSecHi << 16);
		}

		void SetSamplesPerSec(uint32 v) {
			mSamplesPerSecLo = (uint16)v;
			mSamplesPerSecHi = (uint16)(v >> 16);
		}

		uint32 GetAvgBytesPerSec() const {
			return (uint32)mAvgBytesPerSecLo + ((uint32)mAvgBytesPerSecHi << 16);
		}

		void SetAvgBytesPerSec(uint32 v) {
			mAvgBytesPerSecLo = (uint16)v;
			mAvgBytesPerSecHi = (uint16)(v >> 16);
		}
	};

	/// Mirror of WAVEFORMATEXTENSIBLE
	struct WaveFormatExtensible {
		WaveFormatEx mFormat;
		union {
			uint16 mBitDepth;
			uint16 mSamplesPerBlock;		// may be zero, according to MSDN
		};
		uint32		mChannelMask;
		Guid		mGuid;
	};

	enum {
		kWAVE_FORMAT_PCM = 1,
		kWAVE_FORMAT_EXTENSIBLE = 0xfffe
	};

	extern const Guid kKSDATAFORMAT_SUBTYPE_PCM;

	// Helper class.
	struct WaveFormatExPCM : public WaveFormatEx{
		WaveFormatExPCM(uint32 samplingRate, uint32 channels, uint32 precision) {
			mFormatTag = kWAVE_FORMAT_PCM;
			mChannels = (uint16)channels;
			SetSamplesPerSec(samplingRate);
			mBitsPerSample = (uint16)precision;
			mBlockAlign = (uint16)(mChannels * ((precision + 7) >> 3));
			SetAvgBytesPerSec(samplingRate * mBlockAlign);
			mSize = 0;
		}
	};
}

#endif
