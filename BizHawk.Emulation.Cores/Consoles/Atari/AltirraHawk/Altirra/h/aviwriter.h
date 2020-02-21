//	VirtualDub - Video processing and capture application
//	Copyright (C) 1998-2004 Avery Lee
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

#ifndef f_AVIOUTPUTFILE_H
#define f_AVIOUTPUTFILE_H

#include <vd2/system/unknown.h>
#include <vd2/system/fileasync.h>
#include <vd2/Riza/bitmap.h>

struct AVIStreamHeader_fixed {
    uint32		fccType;
    uint32		fccHandler;
    uint32		dwFlags;
    uint16		wPriority;
    uint16		wLanguage;
    uint32		dwInitialFrames;
    uint32		dwScale;	
    uint32		dwRate;
    uint32		dwStart;
    uint32		dwLength;
    uint32		dwSuggestedBufferSize;
    uint32		dwQuality;
    uint32		dwSampleSize;
	struct {
		sint16	left;
		sint16	top;
		sint16	right;
		sint16	bottom;
	} rcFrame;
};

class IVDMediaOutputStream : public IVDUnknown {
public:
	enum { kTypeID = 'mots' };

	virtual ~IVDMediaOutputStream() {}		// shouldn't be here but need to get rid of common delete in root destructor

	virtual void *	getFormat() = 0;
	virtual int		getFormatLen() = 0;
	virtual void	setFormat(const void *pFormat, int len) = 0;

	virtual const AVIStreamHeader_fixed& getStreamInfo() = 0;
	virtual void	setStreamInfo(const AVIStreamHeader_fixed& hdr) = 0;
	virtual void	updateStreamInfo(const AVIStreamHeader_fixed& hdr) = 0;

	enum {
		kFlagKeyFrame = 0x10		// clone of AVIIF_KEYFRAME
	};

	virtual void	write(uint32 flags, const void *pBuffer, uint32 cbBuffer, uint32 samples) = 0;

	virtual void	partialWriteBegin(uint32 flags, uint32 bytes, uint32 samples) = 0;
	virtual void	partialWrite(const void *pBuffer, uint32 cbBuffer) = 0;
	virtual void	partialWriteEnd() = 0;

	virtual void	flush() = 0;
	virtual void	finish() = 0;
};

class VDINTERFACE IVDMediaOutput : public IVDUnknown {
public:
	enum { kTypeID = 'mout' };

	virtual ~IVDMediaOutput() {}

	virtual bool init(const wchar_t *szFile)=0;
	virtual void finalize()=0;

	virtual IVDMediaOutputStream *createAudioStream() = 0;
	virtual IVDMediaOutputStream *createVideoStream() = 0;
	virtual IVDMediaOutputStream *getAudioOutput() = 0;		// DEPRECATED
	virtual IVDMediaOutputStream *getVideoOutput() = 0;		// DEPRECATED
};

class IVDMediaOutputAVIFile : public IVDMediaOutput {
public:
	virtual void disable_os_caching() = 0;
	virtual void disable_extended_avi() = 0;
	virtual void set_1Gb_limit() = 0;
	virtual void set_capture_mode(bool b) = 0;
	virtual void setAlignment(int stream, uint32 align) = 0;
	virtual void setInterleaved(bool bInterleaved) = 0;
	virtual void setBuffering(sint32 nBufferSize, sint32 nChunkSize, IVDFileAsync::Mode asyncMode) = 0;
	virtual void setSegmentHintBlock(bool fIsFinal, const char *pszNextPath, int cbBlock) = 0;
	virtual void setHiddenTag(const char *pTag) = 0;
	virtual void setIndexingLimits(sint32 nMaxSuperIndexEntries, sint32 nMaxSubIndexEntries) = 0;

	virtual void setTextInfoEncoding(int codePage, int countryCode, int language, int dialect) = 0;
	virtual void setTextInfo(uint32 ckid, const char *text) = 0;

	virtual uint32 bufferStatus(uint32 *lplBufferSize) = 0;
	virtual sint64 GetCurrentSize() const = 0;
};

IVDMediaOutputAVIFile *VDCreateMediaOutputAVIFile();

#endif
