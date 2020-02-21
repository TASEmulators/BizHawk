//	Altirra - Atari 800/800XL/5200 emulator
//	I/O library - cassette FSK and direct decoders
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

#ifndef f_AT_ATIO_CASSETTEDECODER_H
#define f_AT_ATIO_CASSETTEDECODER_H

class VDBufferedWriteStream;

class ATCassetteDecoderFSK {
public:
	ATCassetteDecoderFSK();

	void Reset();

	template<bool T_DoAnalysis>
	void Process(const sint16 *samples, uint32 n, uint32 *bitfield, uint32 bitoffset, float *adest);

protected:
	sint32 mAcc0R;
	sint32 mAcc0I;
	sint32 mAcc1R;
	sint32 mAcc1I;
	uint32 mIndex;
	sint16 mHistory[24];
};

///////////////////////////////////////////////////////////////////////////////

class ATCassetteDecoderDirect {
public:
	ATCassetteDecoderDirect();

	void Reset();

	template<bool T_DoAnalysis>
	void Process(const sint16 *samples, uint32 n, uint32 *bitfield, uint32 bitoffset, float *adest);

protected:
	bool mbCurrentState;
	float mPrevLevel;
	float mAGC;
};

#endif
