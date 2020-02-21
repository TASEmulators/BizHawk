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

#include <stdafx.h>
#include <vector>
#include <list>
#include <map>
#include <vd2/system/VDString.h>
#include <vd2/system/binary.h>
#include <vd2/system/error.h>
#include <vd2/system/vdalloc.h>
#include <vd2/system/fileasync.h>
#include <vd2/system/math.h>
#include "aviwriter.h"
#include "oshelper.h"

///////////////////////////////////////////

enum {
	kAVIF_HASINDEX			= 0x00000010,
	kAVIF_MUSTUSEINDEX		= 0x00000020,
	kAVIF_ISINTERLEAVED		= 0x00000100,
	kAVIF_WASCAPTUREFILE	= 0x00010000,
	kAVIF_COPYRIGHTED		= 0x00020000,
	kAVIIF_KEYFRAME			= 0x0010
};

// The following comes from the OpenDML 1.0 spec for extended AVI files

// bIndexType codes
//
#define AVI_INDEX_OF_INDEXES 0x00	// when each entry in aIndex
									// array points to an index chunk

#define AVI_INDEX_OF_CHUNKS 0x01	// when each entry in aIndex
									// array points to a chunk in the file

#define AVI_INDEX_IS_DATA	0x80	// when each entry is aIndex is
									// really the data

// bIndexSubtype codes for INDEX_OF_CHUNKS

#define AVI_INDEX_2FIELD	0x01	// when fields within frames

#pragma pack(push, 2)
									// are also indexed
struct _avisuperindex_entry {
	uint64 qwOffset;		// absolute file offset, offset 0 is
							// unused entry??
	uint32 dwSize;			// size of index chunk at this offset
	uint32 dwDuration;		// time span in stream ticks
};
struct _avistdindex_entry {
	uint32 dwOffset;			// qwBaseOffset + this is absolute file offset
	uint32 dwSize;			// bit 31 is set if this is NOT a keyframe
};

typedef struct _avisuperindex_chunk {
	uint32	fcc;					// ix##
	uint32	cb;					// size of this structure
	uint16	wLongsPerEntry;		// must be 4 (size of each entry in aIndex array)
	uint8	bIndexSubType;			// must be 0 or AVI_INDEX_2FIELD
	uint8	bIndexType;			// must be AVI_INDEX_OF_INDEXES
	uint32	nEntriesInUse;		// number of entries in aIndex array that
								// are used
	uint32	dwChunkId;			// ##dc or ##db or ##wb etc
	uint32	dwReserved[3];		// must be 0
//	struct _avisuperindex_entry aIndex[];
} AVISUPERINDEX, * PAVISUPERINDEX;

typedef struct _avistdindex_chunk {
	uint32	fcc;					// ix##
	uint32	cb;
	uint16	wLongsPerEntry;		// must be sizeof(aIndex[0])/sizeof(uint32)
	uint8	bIndexSubType;			// must be 0
	uint8	bIndexType;			// must be AVI_INDEX_OF_CHUNKS
	uint32	nEntriesInUse;		//
	uint32	dwChunkId;			// ##dc or ##db or ##wb etc..
	uint64	qwBaseOffset;		// all dwOffsets in aIndex array are
								// relative to this
	uint32	dwReserved3;			// must be 0
//	struct _avistdindex_entry aIndex[];
} AVISTDINDEX, * PAVISTDINDEX;	

#pragma pack(pop)

///////////////////////////////////////////////////////////////////////////

namespace {
	const uint32	kChunkID_RIFF		= 'FFIR';		// RIFF: file format chunk
	const uint32	kChunkID_LIST		= 'TSIL';		// LIST: encapsulation chunk
	const uint32	kChunkID_AVI		= ' IVA';		// AVI : AVI format tag
	const uint32	kChunkID_AVIX		= 'XIVA';		// AVI : AVI format tag
	const uint32	kChunkID_movi		= 'ivom';		// movi: main data chunk
	const uint32	kChunkID_hdrl		= 'lrdh';		// hdrl: header LIST
	const uint32	kChunkID_odml		= 'lmdo';		// odml: OpenDML header LIST
	const uint32	kChunkID_dmlh		= 'hlmd';		// dmlh: OpenDML header
	const uint32	kChunkID_JUNK		= 'KNUJ';		// JUNK: padding
	const uint32	kChunkID_avih		= 'hiva';		// avih: AVI header
	const uint32	kChunkID_strl		= 'lrts';		// strl: stream LIST
	const uint32	kChunkID_strh		= 'hrts';		// strh: stream header
	const uint32	kChunkID_strf		= 'frts';		// strf: stream format
	const uint32	kChunkID_idx1		= '1xdi';		// idx1: AVI1 index chunk
	const uint32	kChunkID_indx		= 'xdni';		// indx: AVI2 index chunk
	const uint32	kChunkID_segm		= 'mges';		// segm: VirtualDub segment chain chunk
}

///////////////////////////////////////////////////////////////////////////

class AVIOutputFileStream;

class AVIOutputFile : public IVDMediaOutputAVIFile {
private:
	enum {
		kDefaultSuperIndexEntries		= 256,			// Maximum number of OpenDML first-level entries -- the AVI file can never grow past 4GB x this value.
		kDefaultSubIndexEntries			= 8192			// Maximum number of OpenDML second-level entries -- the AVI file can never contain more than the product of these values in sample chunks.
	};

	vdautoptr<IVDFileAsync>	mpFileAsync;
	sint64		mFilePosition;
	uint32		mAVIXLevel;

	struct AVIBlock {
		sint64		riff_pos;		// position of 'RIFF' chunk
		sint64		movi_pos;		// position of 'movi' chunk
		uint32		riff_len;		// length of 'RIFF' chunk
		uint32		movi_len;		// length of 'movi' chunk
	};

	std::vector<AVIBlock>	mBlocks;
	int			mBlock;

	uint32		mHeaderPosition;
	uint32		mAVIHeaderPos;
	uint32		mAVI2HeaderPos;
	uint32		mSegmentHintPos;

	uint32		mSuperIndexLimit;
	uint32		mSubIndexLimit;

	struct IndexEntry {
		uint64	offset;
		uint32	id;
		uint32	length_and_flags;
	};

	struct IndexEntryBlock {
		enum { kEntries = 1024 };

		IndexEntry entries[kEntries];
	};

	typedef std::vector<IndexEntryBlock *> tIndex;
	tIndex		mIndex;
	uint32		mIndexEntries;

	vdfastvector<char>	mHeaderBlock;
	vdfastvector<char>	mHiddenTag;

	struct Header {
		uint32		dwMicroSecPerFrame;
		uint32		dwMaxBytesPerSec;
		uint32		dwPaddingGranularity;
		uint32		dwFlags;
		uint32		dwTotalFrames;
		uint32		dwInitialFrames;
		uint32		dwStreams;
		uint32		dwSuggestedBufferSize;
		uint32		dwWidth;
		uint32		dwHeight;
		uint32		dwReserved[4];
	};

	Header		mAVIHeader;

	sint32		mBufferSize;
	sint32		mChunkSize;
	IVDFileAsync::Mode mAsyncMode;
	uint32		mAVILimit;				// approx max bytes for initial 'AVI ' block
	uint32		mAVIXLimit;				// approx max bytes per 'AVIX' extension block

	bool		mbCaching;
	bool		mbExtendedAVI;
	bool		mbCaptureMode;
	bool		mbInitialized;
	bool		mbInterleaved;

	vdfastvector<char>	mSegmentHint;

	sint64		mFarthestWritePoint;

	struct StreamInfo {
		AVIOutputFileStream	*mpStream;
		sint64	mLargestPosDelta;
		sint64	mFirstChunkPos;
		sint64	mLastChunkPos;
		uint32	mChunkCount;
		uint32	mChunkCountBlock0;
		uint32	mChunkID;
		sint32	mHeaderPos;				// position of stream header
		sint32	mFormatPos;				// position of format
		sint32	mSuperIndexPos;			// position of superindex
		uint32	mAlignment;				///< Required alignment in bytes (power of two), or zero if no special alignment is required.

		bool	mbIsVideo;

		StreamInfo();
		~StreamInfo();
	};

	typedef std::list<StreamInfo> tStreams;
	tStreams mStreams;

	IVDMediaOutputStream	*mpFirstVideoStream;
	IVDMediaOutputStream	*mpFirstAudioStream;

	long		mIndexSize;
	bool		mbLimitTo4GB;
	bool		mbPreemptiveExtendFailed;

	int			mTextInfoListSize;
	int			mTextInfoCodePage;
	int			mTextInfoCountryCode;
	int			mTextInfoLanguage;
	int			mTextInfoDialect;

	typedef std::map<uint32, VDStringA> tTextInfo;
	tTextInfo	mTextInfo;

	void		HeaderWrite(const void *data, long len);
	uint32		HeaderWriteChunk(uint32 ckid, const void *data, long len);
	uint32		HeaderBeginList(uint32 ckid);
	void		HeaderEndList(uint32 pos);
	void		HeaderFlush();
	void		HeaderSeek(uint32 pos)	{ mHeaderPosition = pos; }
	uint32		HeaderTell() { return mHeaderPosition; }

	void		FastWrite(const void *data, int len);
	void		FileOpen(const wchar_t *pwszFile, bool bCaching, uint32 nBufferSize, uint32 nChunkSize, IVDFileAsync::Mode mode);
	void		FileWrite(sint64 pos, const void *data, uint32 size);
	sint64		FileSize();
	void		FileEndFastPath();
	void		FileClose();
	void		FileSafeTruncateAndClose(sint64 pos);

	void		BlockClose();
	void		BlockOpen();

	void		WriteIndexAVI1();
	void		WriteIndexAVI2(AVISUPERINDEX *asi, _avisuperindex_entry *asie, int nStream);
	void		WriteSubIndexAVI2(struct _avisuperindex_entry *asie, const IndexEntry *avie2, int size, uint32 dwChunkId, uint32 dwSampleSize);
public:
	AVIOutputFile();
	virtual ~AVIOutputFile();

	void *AsInterface(uint32 id);

	IVDMediaOutput *AsIMediaOutput() { return this; }

	void disable_os_caching();
	void disable_extended_avi();
	void set_1Gb_limit();
	void set_capture_mode(bool b);
	void setAlignment(int stream, uint32 align);
	void setSegmentHintBlock(bool fIsFinal, const char *pszNextPath, int cbBlock);
	void setHiddenTag(const char *pTag);
	void setInterleaved(bool bInterleaved) { mbInterleaved = bInterleaved; }
	void setBuffering(sint32 nBufferSize, sint32 nChunkSize, IVDFileAsync::Mode asyncMode);
	void setIndexingLimits(sint32 nMaxSuperIndexEntries, sint32 nMaxSubIndexEntries) {
		mSuperIndexLimit = nMaxSuperIndexEntries;
		mSubIndexLimit = nMaxSubIndexEntries;
	}
	void setTextInfoEncoding(int codePage, int countryCode, int language, int dialect);
	void setTextInfo(uint32 ckid, const char *text);

	IVDMediaOutputStream *createAudioStream();
	IVDMediaOutputStream *createVideoStream();
	IVDMediaOutputStream *getAudioOutput() { return mpFirstAudioStream; }
	IVDMediaOutputStream *getVideoOutput() { return mpFirstVideoStream; }

	bool init(const wchar_t *szFile);

	void finalize();

	void writeIndexedChunk(int nStream, uint32 flags, const void *pBuffer, uint32 cbBuffer);
	void partialWriteIndexedChunkBegin(int nStream, uint32 flags, uint32 cbBuffer);
	void partialWriteIndexedChunk(int nStream, const void *pBuffer, uint32 cbBuffer);
	void partialWriteIndexedChunkEnd(int nStream);

	uint32 bufferStatus(uint32 *lplBufferSize);

	sint64 GetCurrentSize() const { return mFarthestWritePoint; }
};

///////////////////////////////////////////////////////////////////////////

class AVIOutputStream : public IVDMediaOutputStream {
public:
	virtual void *	getFormat() { return mFormat.data(); }
	virtual int		getFormatLen() { return (int)mFormat.size(); }
	virtual void	setFormat(const void *pFormat, int len) { mFormat.assign((const char *)pFormat, (const char *)pFormat + len); }

	virtual const AVIStreamHeader_fixed& getStreamInfo() { return streamInfo; }
	virtual void	setStreamInfo(const AVIStreamHeader_fixed& hdr) { streamInfo = hdr; }
	virtual void	updateStreamInfo(const AVIStreamHeader_fixed& hdr) { streamInfo = hdr; }

	virtual void	flush() {}
	virtual void	finish() {}

protected:
	vdfastvector<char> mFormat;
	AVIStreamHeader_fixed streamInfo;
};

class AVIOutputFileStream : public AVIOutputStream {
public:
	AVIOutputFileStream(AVIOutputFile *pParent, int nStream);

	void *AsInterface(uint32 iid) {
		if (iid == IVDMediaOutputStream::kTypeID)
			return static_cast<IVDMediaOutputStream *>(this);

		return NULL;
	}

	void write(uint32 flags, const void *pBuffer, uint32 cbBuffer, uint32 samples);
	void partialWriteBegin(uint32 flags, uint32 bytes, uint32 samples);
	void partialWrite(const void *pBuffer, uint32 cbBuffer);
	void partialWriteEnd();

protected:
	AVIOutputFile *const mpParent;
	const int mStream;
};

AVIOutputFileStream::AVIOutputFileStream(AVIOutputFile *pParent, int nStream)
	: mpParent(pParent)
	, mStream(nStream)
{
}

void AVIOutputFileStream::write(uint32 flags, const void *pBuffer, uint32 cbBuffer, uint32 samples) {
	partialWriteBegin(flags, cbBuffer, samples);
	partialWrite(pBuffer, cbBuffer);
	partialWriteEnd();
}

void AVIOutputFileStream::partialWriteBegin(uint32 flags, uint32 bytes, uint32 samples) {
	// ActiveMovie/WMP requires a non-zero dwSuggestedBufferSize for
	// hierarchial indexing.  So we continually bump it up to the
	// largest chunk size.

	if (streamInfo.dwSuggestedBufferSize < bytes)
		streamInfo.dwSuggestedBufferSize = bytes;

	if (~streamInfo.dwLength < samples)
		streamInfo.dwLength = 0xFFFFFFFF;
	else
		streamInfo.dwLength += samples;

	mpParent->partialWriteIndexedChunkBegin(mStream, flags, bytes);
}

void AVIOutputFileStream::partialWrite(const void *pBuffer, uint32 cbBuffer) {
	mpParent->partialWriteIndexedChunk(mStream, pBuffer, cbBuffer);
}

void AVIOutputFileStream::partialWriteEnd() {
	mpParent->partialWriteIndexedChunkEnd(mStream);
}

///////////////////////////////////////////////////////////////////////////

IVDMediaOutputAVIFile *VDCreateMediaOutputAVIFile() {
	return new AVIOutputFile;
}

IVDMediaOutputAVIFile *VDGetIMediaOutputFile(IVDMediaOutput *pOutput) {
	return static_cast<AVIOutputFile *>(pOutput);
}

///////////////////////////////////////////////////////////////////////////

AVIOutputFile::AVIOutputFile()
	: mpFirstAudioStream(NULL)
	, mpFirstVideoStream(NULL)
	, mTextInfoCodePage(0)
	, mTextInfoCountryCode(0)
	, mTextInfoLanguage(0)
	, mTextInfoDialect(0)
{
	mbCaching			= true;
	mAVIXLevel			= 0;
	mBlock				= 0;
	mbExtendedAVI		= true;
	mAVILimit			= 0x7F000000L;
	mAVIXLimit			= 0x7F000000L;
	mbCaptureMode		= false;
	mbInitialized		= false;
	mbInterleaved		= true;
	mBufferSize			= 1048576;				// reasonable default: 1MB buffer, 256K chunks
	mChunkSize			= mBufferSize >> 2;
	mAsyncMode			= IVDFileAsync::kModeAsynchronous;
	mSuperIndexLimit	= kDefaultSuperIndexEntries;
	mSubIndexLimit		= kDefaultSubIndexEntries;

	mHeaderBlock.reserve(16384);
	mFarthestWritePoint	= 0;
	mIndexSize			= 0;
	mbPreemptiveExtendFailed = false;
}

AVIOutputFile::~AVIOutputFile() {
	while(!mIndex.empty()) {
		delete mIndex.back();
		mIndex.pop_back();
	}

	FileSafeTruncateAndClose(mFarthestWritePoint);
}

////////////////////////////////////

AVIOutputFile::StreamInfo::StreamInfo()
	: mpStream(NULL)
	, mLargestPosDelta(0)
	, mFirstChunkPos(0)
	, mLastChunkPos(0)
	, mChunkCount(0)
	, mAlignment(0)
{
}

AVIOutputFile::StreamInfo::~StreamInfo() {
	delete mpStream;
}

//////////////////////////////////

void *AVIOutputFile::AsInterface(uint32 id) {
	if (id == IVDMediaOutput::kTypeID)
		return static_cast<IVDMediaOutput *>(this);

	return NULL;
}

IVDMediaOutputStream *AVIOutputFile::createVideoStream() {
	mStreams.resize(mStreams.size() + 1);
	StreamInfo& stream = mStreams.back();

	if (!(stream.mpStream = new_nothrow AVIOutputFileStream(this, (int)mStreams.size() - 1)))
		throw MyMemoryError();

	stream.mbIsVideo = true;

	if (!mpFirstVideoStream)
		mpFirstVideoStream = stream.mpStream;

	return stream.mpStream;
}

IVDMediaOutputStream *AVIOutputFile::createAudioStream() {
	mStreams.resize(mStreams.size() + 1);
	StreamInfo& stream = mStreams.back();

	if (!(stream.mpStream = new_nothrow AVIOutputFileStream(this, (int)mStreams.size() - 1)))
		throw MyMemoryError();

	stream.mbIsVideo = false;

	if (!mpFirstAudioStream)
		mpFirstAudioStream = stream.mpStream;

	return stream.mpStream;
}

void AVIOutputFile::disable_os_caching() {
	mbCaching = false;
}

void AVIOutputFile::disable_extended_avi() {
	mbExtendedAVI = false;
}

void AVIOutputFile::set_1Gb_limit() {
	mAVILimit = 0x3F000000L;
}

void AVIOutputFile::set_capture_mode(bool b) {
	mbCaptureMode = b;
}

void AVIOutputFile::setAlignment(int streamIdx, uint32 alignment) {
	if (VDINLINEASSERTFALSE((uint32)streamIdx >= mStreams.size()))
		return;

	tStreams::iterator itStream(mStreams.begin());
	std::advance(itStream, streamIdx);
	StreamInfo& stream = *itStream;

	stream.mAlignment = alignment;
}

void AVIOutputFile::setSegmentHintBlock(bool fIsFinal, const char *pszNextPath, int cbBlock) {
	mSegmentHint.resize(cbBlock, 0);

	mSegmentHint[0] = !fIsFinal;
	if (pszNextPath)
		strcpy(&mSegmentHint[1], pszNextPath);
}

void AVIOutputFile::setHiddenTag(const char *pTag) {
	const size_t s = strlen(pTag);

	mHiddenTag.clear();
	mHiddenTag.insert(mHiddenTag.end(), pTag, pTag + s);
}

void AVIOutputFile::setBuffering(sint32 nBufferSize, sint32 nChunkSize, IVDFileAsync::Mode asyncMode) {
	VDASSERT(!(mBufferSize & (mBufferSize-1)));
	VDASSERT(!(mChunkSize & (mChunkSize-1)));
	VDASSERT(mChunkSize >= 4096);
	VDASSERT(mBufferSize >= mChunkSize);

	mBufferSize = nBufferSize;
	mChunkSize = nChunkSize;
	mAsyncMode = asyncMode;
}

void AVIOutputFile::setTextInfoEncoding(int codePage, int countryCode, int language, int dialect) {
	mTextInfoCodePage		= codePage;
	mTextInfoCountryCode	= countryCode;
	mTextInfoLanguage		= language;
	mTextInfoDialect		= dialect;
}

void AVIOutputFile::setTextInfo(uint32 ckid, const char *text) {
	mTextInfo[ckid] = text;
}

// I don't like to bitch about other programs (well, okay, so I do), but
// Windows Media Player deserves special attention here.  The ActiveMovie
// implementation of OpenDML hierarchial indexing >2Gb *SUCKS*.  It can't
// cope with a JUNK chunk at the end of the hdrl chunk (even though
// the Microsoft documentation in AVIRIFF.H shows one), requires that
// all standard indexes be the same size except for the last one, and
// requires buffer size information for streams.  NONE of this is required
// by ActiveMovie when an extended index is absent (easily verified by
// changing the 'indx' chunks to JUNK chunks).  While diagnosing these
// problems I got an interesting array of error messages from WMP,
// including:
//
//	o Downloading codec from activex.microsoft.com
//		(Because of an extended index!?)
//	o "Cannot allocate memory because no size has been set"
//		???
//	o "The file format is invalid."
//		Detail: "The file format is invalid. (Error=8004022F)"
//		Gee, that clears everything up.
//	o My personal favorite: recursion of the above until the screen
//		has 100+ dialogs and WMP crashes with a stack fault.
//
// Basically, supporting WMP (or as I like to call it, WiMP) was an
// absolute 100% pain in the ass.

bool AVIOutputFile::init(const wchar_t *szFile) {
	//mbLimitTo4GB = IsFilenameOnFATVolume(szFile);
	mbLimitTo4GB = false;

	VDASSERT(!mStreams.empty());

	mIndex.reserve(16);
	mIndexEntries = 0;

	// Initialize main AVI header (avih)
	memset(&mAVIHeader, 0, sizeof mAVIHeader);
	mAVIHeader.dwMicroSecPerFrame		= 0;
	mAVIHeader.dwMaxBytesPerSec			= 0;
	mAVIHeader.dwPaddingGranularity		= 0;
	mAVIHeader.dwFlags					= kAVIF_HASINDEX | (mbInterleaved ? kAVIF_ISINTERLEAVED : 0) | (mbCaptureMode ? kAVIF_WASCAPTUREFILE : 0);
	mAVIHeader.dwTotalFrames			= 0;
	mAVIHeader.dwInitialFrames			= 0;
	mAVIHeader.dwStreams				= (uint32)mStreams.size();
	mAVIHeader.dwSuggestedBufferSize	= 0;
	mAVIHeader.dwWidth					= 0;
	mAVIHeader.dwHeight					= 0;

	if (mpFirstVideoStream) {
		const AVIStreamHeader_fixed& vhdr = mpFirstVideoStream->getStreamInfo();
		const VDAVIBitmapInfoHeader *pVF = (const VDAVIBitmapInfoHeader *)mpFirstVideoStream->getFormat();
		mAVIHeader.dwMicroSecPerFrame		= (uint32)((vhdr.dwScale * (uint64)1000000U) / vhdr.dwRate);
		mAVIHeader.dwWidth					= pVF->biWidth;
		mAVIHeader.dwHeight					= pVF->biHeight;
	}

	// Initialize file

	FileOpen(szFile, mbCaching, mBufferSize, mChunkSize, mAsyncMode);
	mHeaderPosition = 0;

	////////// Initialize the first 'AVI ' chunk //////////

	uint32 dw[64];

	// start RIFF chunk

	dw[0]	= kChunkID_RIFF;
	dw[1]	= 0;
	dw[2]	= kChunkID_AVI;

	HeaderWrite(dw, 12);

	// start header chunk
	uint32 hdrl_pos = HeaderBeginList(kChunkID_hdrl);

	// write out main AVI header
	mAVIHeaderPos = HeaderWriteChunk(kChunkID_avih, &mAVIHeader, sizeof mAVIHeader);

	// write out stream headers
	tStreams::iterator it(mStreams.begin()), itEnd(mStreams.end());
	int i=0;

	for(; it != itEnd; ++it, ++i) {
		StreamInfo& stream = *it;

		// set chunk ID for stream
		char buf[4];
		sprintf(buf, "%02x", i);
		stream.mChunkID = buf[0] + (buf[1]<<8);

		if (stream.mbIsVideo) {
			const VDAVIBitmapInfoHeader& bih = *(const VDAVIBitmapInfoHeader *)stream.mpStream->getFormat();

			if (bih.biCompression == VDAVIBitmapInfoHeader::kCompressionRGB)
				stream.mChunkID += 'bd'<<16;
			else
				stream.mChunkID += 'cd'<<16;
		} else {
			stream.mChunkID += 'bw'<<16;
		}

		// open stream header list
		uint32 strl_pos = HeaderBeginList(kChunkID_strl);

		// write out stream header and format
		stream.mHeaderPos	= HeaderWriteChunk(kChunkID_strh, &stream.mpStream->getStreamInfo(), sizeof(AVIStreamHeader_fixed));
		stream.mFormatPos	= HeaderWriteChunk(kChunkID_strf, stream.mpStream->getFormat(), stream.mpStream->getFormatLen());

		// write out space for superindex, but make it a padding chunk for now
		if (mbExtendedAVI) {
			AVISUPERINDEX asi={0};
			struct _avisuperindex_entry asie_dummy = {0};

			stream.mSuperIndexPos = (uint32)HeaderTell();
			asi.fcc = kChunkID_JUNK;
			asi.cb = (sizeof asi)-8 + mSuperIndexLimit*sizeof(_avisuperindex_entry);
			HeaderWrite(&asi, sizeof asi);

			for(uint32 i=0; i<mSuperIndexLimit; ++i)
				HeaderWrite(&asie_dummy, sizeof(_avisuperindex_entry));
		}

		// finish header
		HeaderEndList(strl_pos);
	}

	// write out dmlh header (indicates real # of frames)

	if (mbExtendedAVI) {
		uint32 odml_pos = HeaderBeginList(kChunkID_odml);

		memset(dw, 0, sizeof dw);
		mAVI2HeaderPos = HeaderWriteChunk(kChunkID_dmlh, dw, 62*4);

		HeaderEndList(odml_pos);
	}

	// write out segment hint block

	if (!mSegmentHint.empty())
		mSegmentHintPos = HeaderWriteChunk(kChunkID_segm, &mSegmentHint[0], (long)mSegmentHint.size());

	HeaderEndList(hdrl_pos);

	// pad out to a multiple of 2048 bytes
	//
	// WARNING: ActiveMovie/WMP can't handle a trailing JUNK chunk in hdrl
	//			if an extended index is in use.  It says the file format
	//			is invalid!
	//
	// WARNING: WMP8 (XP) thinks a file is an MP3 file if it contains two
	//			MP3 frames within its first 8K.  We force the LIST/movi
	//			chunk to start beyond 8K to solve this problem.

	{
		sint32	curpos = (sint32)HeaderTell();

		if (curpos < 8192 || (curpos & 2047)) {
			sint32	padpos = std::max<sint32>(8192, (curpos + 8 + 2047) & ~2047);
			sint32	pad = padpos - curpos - 8;

			vdfastvector<char> s(pad, 0);

			memcpy(s.data(), mHiddenTag.data(), std::min<size_t>(s.size(), mHiddenTag.size()));

			HeaderWriteChunk(kChunkID_JUNK, s.data(), pad);
		}
	}

	mpFileAsync->FastWrite(&mHeaderBlock.front(), (uint32)mHeaderBlock.size());
	mFilePosition = mHeaderBlock.size();

	// If we're using the fast path, we're aligned to a sector boundary.
	// Write out the 12 header bytes.

	BlockOpen();

	// Compute how many bytes are necessary for text information structures.
	uint32 textInfoSize = 0;
	if (!mTextInfo.empty()) {
		textInfoSize = 8;		// LIST+size

		mTextInfoListSize = 4;	// 'INFO'

		if (mTextInfoCodePage || mTextInfoCountryCode || mTextInfoLanguage || mTextInfoDialect)
			mTextInfoListSize += 16;

		tTextInfo::const_iterator it(mTextInfo.begin()), itEnd(mTextInfo.end());
		for(; it!=itEnd; ++it) {
			const VDStringA& text = (*it).second;
			mTextInfoListSize += (text.size() + 9 + 1) & ~1;
		}

		textInfoSize += mTextInfoListSize;
	}

	mAVIXLevel += textInfoSize;

	mbInitialized = true;

	return true;
}

void AVIOutputFile::finalize() {
	VDDEBUG("AVIOutputFile: Beginning finalize.\n");

	uint32 dw;
	int i;

	if (!mbInitialized)
		return;

	// update OpenDML indices (must be done here as the indices are written via fast write)
	if (mbExtendedAVI) {
		if (mpFirstVideoStream) {
			HeaderSeek(mAVI2HeaderPos+8);
			dw = mpFirstVideoStream->getStreamInfo().dwLength;
			HeaderWrite(&dw, 4);
		}

		if (mBlock >= 1) {		// The current block is still open, so mBlock==1 means two blocks are present.
			vdfastvector<_avisuperindex_entry> asie(mSuperIndexLimit);

			tStreams::iterator it(mStreams.begin()), itEnd(mStreams.end());

			int i=0;
			for(; it!=itEnd; ++it, ++i) {
				StreamInfo& stream = *it;
				AVISUPERINDEX asi;

				WriteIndexAVI2(&asi, asie.data(), i);
				HeaderSeek(stream.mSuperIndexPos);
				HeaderWrite(&asi, sizeof asi);
				HeaderWrite(asie.data(), sizeof(_avisuperindex_entry)*mSuperIndexLimit);
			}
		}
	}

	// finish last Xblock
	BlockClose();

	// fast path: clean it up and resync slow path.
	FileEndFastPath();

	// Set dwMaxBytesPerSec in the AVI header according to the various streams. We need this to
	// bypass some interesting logic in Windows Vista Explorer; setting this similarly to
	// DirectShow makes the bitrate display somewhat reasonable. And yes, DirectShow does simply
	// use the entire file length, although it truncates duration to seconds (which we don't do).
	// Explorer uses dwMaxBytesPerSec as if it were dwAvgBytesPerSec, so we'd best not try to
	// actually compute a max here.
	{
		tStreams::iterator it(mStreams.begin()), itEnd(mStreams.end());
		for(; it!=itEnd; ++it) {
			const StreamInfo& stream = *it;
			const AVIStreamHeader_fixed& hdr = stream.mpStream->getStreamInfo();

			if (hdr.fccType == VDMAKEFOURCC('v', 'i', 'd', 's')) {
				if (stream.mChunkCount && hdr.dwScale && stream.mChunkCount)
					mAVIHeader.dwMaxBytesPerSec = VDClampToUint32(VDRoundToInt64((double)mFilePosition / (double)stream.mChunkCount * hdr.dwRate / hdr.dwScale));

				break;
			}
		}
	}

	HeaderSeek(mAVIHeaderPos+8);
	HeaderWrite(&mAVIHeader, sizeof mAVIHeader);

	// update stream headers
	tStreams::iterator it(mStreams.begin()), itEnd(mStreams.end());
	for(; it!=itEnd; ++it) {
		StreamInfo& stream = *it;

		HeaderSeek(stream.mHeaderPos+8);
		HeaderWrite(&stream.mpStream->getStreamInfo(), sizeof(AVIStreamHeader_fixed));

		if (!stream.mbIsVideo) {
			// we have to rewrite the audio format, in case someone
			// fixed fields in the format afterward (MPEG-1/L3)
			HeaderSeek(stream.mFormatPos+8);
			HeaderWrite(mpFirstAudioStream->getFormat(), mpFirstAudioStream->getFormatLen());
		}
	}

	if (!mSegmentHint.empty()) {
		HeaderSeek(mSegmentHintPos+8);
		HeaderWrite(mSegmentHint.data(), (long)mSegmentHint.size());
	}

	HeaderFlush();

	for(i=0; i<mBlock; i++) {
		AVIBlock& blockinfo = mBlocks[i];

		FileWrite(blockinfo.riff_pos+4, &blockinfo.riff_len, 4);
		FileWrite(blockinfo.movi_pos+4, &blockinfo.movi_len, 4);
	}

	FileClose();

	VDDEBUG("AVIOutputFile: Finalize was successful.\n");
}

uint32 AVIOutputFile::bufferStatus(uint32 *lplBufferSize) {
	return 0;
}

////////////////////////////

void AVIOutputFile::HeaderWrite(const void *data, long len) {
	uint32 cursize = (uint32)mHeaderBlock.size();

	if (mHeaderPosition < cursize)
		memcpy(&mHeaderBlock[mHeaderPosition], data, std::min<int>(cursize-mHeaderPosition, len));

	if (mHeaderPosition + len > cursize)
		mHeaderBlock.insert(mHeaderBlock.end(), (char *)data + (cursize-mHeaderPosition), (char *)data + len);

	mHeaderPosition += len;
}

uint32 AVIOutputFile::HeaderBeginList(uint32 ckid) {
	uint32 dw[3];

	dw[0] = kChunkID_LIST;
	dw[1] = 0;
	dw[2] = ckid;

	HeaderWrite(dw, 12);

	return mHeaderPosition;
}

void AVIOutputFile::HeaderEndList(uint32 pos) {
	uint32 size;
	uint32 currentPos = mHeaderPosition;
	
	size = (uint32)(currentPos - (pos-4));

	HeaderSeek(pos-8);
	HeaderWrite(&size, 4);
	HeaderSeek(currentPos);
}

uint32 AVIOutputFile::HeaderWriteChunk(uint32 ckid, const void *data, long len) {
	uint32 dw[2];

	dw[0] = ckid;
	dw[1] = len;

	uint32 pos = mHeaderPosition;
	
	HeaderWrite(dw, 8);
	HeaderWrite(data, len);

	if (len & 1) {
		dw[0] = 0;
		HeaderWrite(dw, 1);
	}

	return pos;
}

void AVIOutputFile::HeaderFlush() {
	FileWrite(0, mHeaderBlock.data(), (uint32)mHeaderBlock.size());
}

void AVIOutputFile::FastWrite(const void *data, int len) {
	mFilePosition += len;
	mpFileAsync->FastWrite(data, len);

	if (mFarthestWritePoint < mFilePosition)
		mFarthestWritePoint = mFilePosition;
}

void AVIOutputFile::FileOpen(const wchar_t *pwszFile, bool bCaching, uint32 nBufferSize, uint32 nChunkSize, IVDFileAsync::Mode mode) {
	mpFileAsync = VDCreateFileAsync(mode);
	mpFileAsync->SetPreemptiveExtend(true);
	mpFileAsync->Open(pwszFile, mBufferSize / mChunkSize, mChunkSize);
	mFilePosition = 0;
}

void AVIOutputFile::FileWrite(sint64 pos, const void *data, uint32 size) {
	mpFileAsync->Write(pos, data, size);
	mFilePosition = pos + size;
	if (mFarthestWritePoint < mFilePosition)
		mFarthestWritePoint = mFilePosition;
}

sint64 AVIOutputFile::FileSize() {
	return mpFileAsync->GetSize();
}

void AVIOutputFile::FileEndFastPath() {
	mpFileAsync->FastWriteEnd();
	mpFileAsync->Truncate(mFilePosition);
}

void AVIOutputFile::FileClose() {
	mpFileAsync->Close();
}

void AVIOutputFile::FileSafeTruncateAndClose(sint64 pos) {
	if (mpFileAsync)
		mpFileAsync->SafeTruncateAndClose(pos);
}

void AVIOutputFile::writeIndexedChunk(int nStream, uint32 flags, const void *pBuffer, uint32 cbBuffer) {
	partialWriteIndexedChunkBegin(nStream, flags, cbBuffer);
	partialWriteIndexedChunk(nStream, pBuffer, cbBuffer);
	partialWriteIndexedChunkEnd(nStream);
}

void AVIOutputFile::partialWriteIndexedChunkBegin(int nStream, uint32 flags, uint32 cbBuffer) {
	VDASSERT(mbInitialized);
	
	tStreams::iterator itStream(mStreams.begin());
	std::advance(itStream, nStream);
	StreamInfo& stream = *itStream;

	// Determine if we need to open another RIFF block (xblock).

	uint32 siz = cbBuffer + (cbBuffer&1) + 16;

	// The original AVI format can't accommodate RIFF chunks >4Gb due to the
	// use of 32-bit size fields.  Most RIFF parsers don't handle >2Gb because
	// of inappropriate use of signed variables.  And to top it all off,
	// stupid mistakes in the MCI RIFF parser prevent processing beyond the
	// 1Gb mark.
	//
	// To be safe, we keep the first RIFF AVI chunk below 1Gb, and subsequent
	// RIFF AVIX chunks below 2Gb.  We have to leave ourselves a little safety
	// margin (16Mb in this case) for index blocks.

	bool fOpenNewBlock = false;
	if (mbExtendedAVI)
		if (mAVIXLevel + siz > (mBlock ? mAVIXLimit : mAVILimit))
			fOpenNewBlock = true;

	// Check available disk space.
	//
	// Take the largest separation between data blocks,

	sint64 chunkloc;

	chunkloc = mFilePosition;
	if (fOpenNewBlock)
		chunkloc += 24;

	if (!stream.mFirstChunkPos)
		stream.mFirstChunkPos = chunkloc;

	if (chunkloc - stream.mLastChunkPos > stream.mLargestPosDelta)
		stream.mLargestPosDelta = chunkloc - stream.mLastChunkPos;

	// compute how much total space we need to close the file

	mIndexSize = 8;

	for(tStreams::const_iterator it(mStreams.begin()), itEnd(mStreams.end()); it!=itEnd; ++it) {
		const StreamInfo& s = *it;
		uint32 chunkCount = stream.mChunkCount;

		if (&s == &stream)
			++chunkCount;

		if (mbExtendedAVI && s.mLargestPosDelta) {
			const uint32 idxblocksize = std::min<uint32>(mSubIndexLimit, (uint32)(0xFFFFFFFF / s.mLargestPosDelta) + 1);
			uint32 idxblocks = (s.mChunkCount + idxblocksize - 1) / idxblocksize;

			mIndexSize += idxblocks * (sizeof(AVISTDINDEX) + 8*idxblocksize);
			mIndexSize += 8*stream.mChunkCount;
		}

		mIndexSize += 16*stream.mChunkCount;
	}
	
	// Give ourselves ~4K of headroom...

	sint64	maxpoint = (chunkloc + cbBuffer + 1 + 8 + 14 + 2047 + mIndexSize + 4096) & -2048ll;

	if (mbLimitTo4GB && maxpoint >= (sint64)0xFFFFFFFF) {		// maximum size on 98 or FAT32: 2^32-1 bytes (NOT 2^32).
		VDDEBUG("AVIOutputFile: overflow detected!  maxpoint=%I64d\n", maxpoint);
		VDDEBUG("AVIOutputFile: mIndexSize = %08lx\n", mIndexSize);

		throw MyError("Out of file space: Files cannot exceed 4 gigabytes on a FAT32 partition.");
	}

	if (!mpFileAsync->IsPreemptiveExtendActive() && !mpFileAsync->Extend(maxpoint))
		throw MyError("Not enough space to write additional data.");

	stream.mLastChunkPos = chunkloc;

	// If we need to open a new Xblock, do so.

	if (fOpenNewBlock) {
		BlockClose();
		BlockOpen();
	}

	// Align the chunk, if an alignment was specified for this stream.
	//
	// NOTE: We have to skip this alignment for the very first chunk we write, in order to
	//       avoid screwing up the legacy index.

	uint32 buf[2];

	if (stream.mAlignment && mIndexEntries) {
		int offset = (int)(mFilePosition+8) & (stream.mAlignment - 1);

		if (offset) {
			buf[0] = kChunkID_JUNK;
			buf[1] = (-offset-8) & (stream.mAlignment - 1);
			FastWrite(buf, 8);
			FastWrite(NULL, buf[1]);
		}
	}

	// Write index entry for the chunk.
	const int idxoffset = mIndexEntries & (IndexEntryBlock::kEntries - 1);

	if (!idxoffset)
		mIndex.push_back(new IndexEntryBlock);

	IndexEntry& ent = mIndex.back()->entries[idxoffset];
	ent.offset				= mFilePosition;
	ent.id					= stream.mChunkID;
	ent.length_and_flags	= cbBuffer;
	if (!(flags & IVDMediaOutputStream::kFlagKeyFrame))
		ent.length_and_flags |= 0x80000000L;

	++stream.mChunkCount;			// Important: This must only be done iff the entry is added or a crash will occur during index write.
	++mIndexEntries;

	// Write the chunk.
	buf[0] = stream.mChunkID;
	buf[1] = cbBuffer;
	FastWrite(buf, 8);

	mAVIXLevel += siz;
}

void AVIOutputFile::partialWriteIndexedChunk(int nStream, const void *pBuffer, uint32 cbBuffer) {
	FastWrite(pBuffer, cbBuffer);
}

void AVIOutputFile::partialWriteIndexedChunkEnd(int nStream) {
	// AVI chunks must be aligned to even boundaries.
	uint32 lastSizeAndFlags = mIndex.back()->entries[(mIndexEntries - 1) & (IndexEntryBlock::kEntries - 1)].length_and_flags;
	if (lastSizeAndFlags & 1)
		FastWrite(NULL, 1);
}

void AVIOutputFile::BlockClose() {
	AVIBlock& blockinfo = mBlocks[mBlock];

	blockinfo.movi_len = (uint32)(mFilePosition - (blockinfo.movi_pos+8));

	if (!mBlock) {
		if (mpFirstVideoStream)
			mAVIHeader.dwTotalFrames = mpFirstVideoStream->getStreamInfo().dwLength;

		WriteIndexAVI1();

		for(tStreams::iterator it(mStreams.begin()), itEnd(mStreams.end()); it!=itEnd; ++it) {
			StreamInfo& stream = *it;

			stream.mChunkCountBlock0 = stream.mChunkCount;
		}

		if (!mTextInfo.empty()) {
			struct {
				uint32 ckid;
				uint32 size;
				uint32 listid;
			} infoList = { (uint32)'TSIL', (uint32)mTextInfoListSize, (uint32)'OFNI' };

			FastWrite(&infoList, 12);

			if (mTextInfoCodePage || mTextInfoCountryCode || mTextInfoLanguage || mTextInfoDialect) {
				struct {
					uint32 ckid;
					uint32 size;
					uint16	wCodePage;
					uint16	wCountryCode;
					uint16	wLanguageCode;
					uint16	wDialect;
				} csetData = {
					(uint32)'TESC',
					8,
					(uint16)mTextInfoCodePage,
					(uint16)mTextInfoCountryCode,
					(uint16)mTextInfoLanguage,
					(uint16)mTextInfoDialect
				};

				FastWrite(&csetData, sizeof csetData);
			}

			tTextInfo::iterator itT(mTextInfo.begin()), itTEnd(mTextInfo.end());

			for(; itT != itTEnd; ++itT) {
				const uint32 ckid = (*itT).first;
				const VDStringA& text = (*itT).second;

				uint32 hdr[2]={ckid, text.size()+1 };

				FastWrite(hdr, 8);
				FastWrite(text.data(), text.size());

				uint32 zero = 0;
				FastWrite(&zero, hdr[1] & 1 ? 2 : 1);
			}
		}
	}

	blockinfo.riff_len = (uint32)(mFilePosition - (blockinfo.riff_pos+8));

	++mBlock;

	mAVIXLevel = 0;
}

void AVIOutputFile::BlockOpen() {
	uint32 dw[8];

	// If we're in capture mode, keep this stuff aligned to 8-byte boundaries!

	mBlocks.push_back(AVIBlock());

	VDASSERT(mBlocks.size() == size_t(mBlock+1));

	AVIBlock& blockinfo = mBlocks.back();

	if (mBlock) {
		blockinfo.riff_pos = mFilePosition;

		dw[0] = kChunkID_RIFF;
		dw[1] = 0x7F000000;
		dw[2] = kChunkID_AVIX;
		dw[3] = kChunkID_LIST;
		dw[4] = 0x7F000000;
		dw[5] = kChunkID_movi;
		FastWrite(dw,24);

		blockinfo.movi_pos = mFilePosition - 12;
	} else {
		blockinfo.riff_pos = 0;

		blockinfo.movi_pos = mFilePosition;

		dw[0] = kChunkID_LIST;
		dw[1] = 0x7FFFFFFF;
		dw[2] = kChunkID_movi;

		FastWrite(dw, 12);
	}

	// WARNING: For AVIFile to parse the index correctly, it assumes that the
	// first chunk in an index starts right after the movi chunk!

//	dw[0] = ckidAVIPADDING;
//	dw[1] = 4;
//	dw[2] = 0;
//	_write(dw, 12);
}

void AVIOutputFile::WriteIndexAVI1() {
	uint32 dw[4 * IndexEntryBlock::kEntries];

	dw[0] = kChunkID_idx1;
	dw[1] = 16 * mIndexEntries;
	FastWrite(dw, 8);

	tIndex::const_iterator it(mIndex.begin());
	const uint32 base = (uint32)(mBlocks.front().movi_pos + 8);

	uint32 count = mIndexEntries;
	while(count) {
		const IndexEntryBlock& block = **it;
		++it;

		uint32 n = IndexEntryBlock::kEntries;
		if (n > count)
			n = count;
		count -= n;

		uint32 *p = dw;
		for(uint32 i=0; i<n; ++i) {
			const IndexEntry& e = block.entries[i];

			p[0] = e.id;
			p[1] = e.length_and_flags & 0x80000000 ? 0 : kAVIIF_KEYFRAME;
			p[2] = (uint32)(e.offset - base);
			p[3] = e.length_and_flags & 0x7fffffff;
			p += 4;
		}

		FastWrite(dw, 16*n);
	}
}

void AVIOutputFile::WriteIndexAVI2(AVISUPERINDEX *asi, _avisuperindex_entry *asie, int nStream) {
	tStreams::const_iterator itStream(mStreams.begin());
	std::advance(itStream, nStream);
	const StreamInfo& stream = *itStream;
	
	uint32 size = stream.mChunkCount;

	if (!size)
		return;

	// Linearize index.
	vdfastvector<IndexEntry> streamIndex(stream.mChunkCount);
	IndexEntry *asie2 = streamIndex.data();
	IndexEntry *asie2_limit = asie2 + stream.mChunkCount;

	{
		uint32 left = mIndexEntries;
		tIndex::const_iterator it(mIndex.begin());
		while(left) {
			const IndexEntryBlock& block = **it;
			++it;

			uint32 n = IndexEntryBlock::kEntries;
			if (n > left)
				n = left;
			left -= n;

			for(uint32 i=0; i<n; ++i) {
				const IndexEntry& e = block.entries[i];

				if (e.id == stream.mChunkID) {
					*asie2++ = e;
					if (asie2 == asie2_limit)
						goto index_complete;
				}
			}
		}
		VDASSERT(false);

index_complete:
		;
	}

	asie2 = streamIndex.data();

	// Now we run into a bit of a problem.  DirectShow's AVI2 parser requires
	// that all index blocks have the same # of entries (except the last),
	// which is a problem since we also have to guarantee that each block
	// has offsets <4Gb.

	// For now, use a O(n^2) algorithm to find the optimal size.

	int blocksize = mSubIndexLimit;

	while(blocksize > 1) {
		int nextblock = 0;
		sint64 offset;
		uint32 i;

		for(i=0; i<size; i++) {
			if (i == nextblock) {
				nextblock += blocksize;
				offset = asie2[i].offset;
			}

			if (asie2[i].offset >= (uint64)offset + 0x100000000ull)
				break;
		}

		if (i >= size)
			break;

		--blocksize;
	}
	
	uint32 blockcount = (size + blocksize - 1) / blocksize;

	if (blockcount > mSuperIndexLimit)
		throw MyError("AVIOutput: Not enough superindex entries to index AVI file.  (%d slots required, %d slots preallocated)",
			blockcount, mSuperIndexLimit);
	
	// Write out the actual index blocks.
	const uint32 chunkID = stream.mChunkID;
	const uint32 dwSampleSize = stream.mpStream->getStreamInfo().dwSampleSize;

	memset(asie, 0, sizeof(_avisuperindex_entry)*mSuperIndexLimit);

	int indexnum=0;
	while(size > 0) {
		int tc = std::min<int>(size, blocksize);

		WriteSubIndexAVI2(&asie[indexnum++], asie2, tc, chunkID, dwSampleSize);

		asie2 += tc;
		size -= tc;
	}

	memset(asi, 0, sizeof(AVISUPERINDEX));
	asi->fcc			= kChunkID_indx;
	asi->cb				= sizeof(AVISUPERINDEX)-8 + sizeof(_avisuperindex_entry)*mSuperIndexLimit;
	asi->wLongsPerEntry	= 4;
	asi->bIndexSubType	= 0;
	asi->bIndexType		= AVI_INDEX_OF_INDEXES;
	asi->nEntriesInUse	= indexnum;
	asi->dwChunkId		= chunkID;
}

void AVIOutputFile::WriteSubIndexAVI2(struct _avisuperindex_entry *asie, const IndexEntry *avie2, int size, uint32 dwChunkId, uint32 dwSampleSize) {
	AVISTDINDEX asi;
	_avistdindex_entry asie3[64];
	sint64 offset = avie2->offset;

	VDASSERT(size>0);
	VDASSERT(avie2[size-1].offset - avie2[0].offset < VD64(0x100000000));

	// Check to see if we need to open a new AVIX block
	if (mAVIXLevel + sizeof(AVISTDINDEX) + size*sizeof(_avistdindex_entry) > (mBlock ? mAVIXLimit : mAVILimit)) {
		BlockClose();
		BlockOpen();
	}

	// setup superindex entry

	asie->qwOffset	= mFilePosition;
	asie->dwSize	= sizeof(AVISTDINDEX) + size*sizeof(_avistdindex_entry);

	if (dwSampleSize) {
		sint64 total_bytes = 0;

		for(int i=0; i<size; i++)
			total_bytes += avie2[i].length_and_flags & 0x7FFFFFFF;

		asie->dwDuration = (uint32)(total_bytes / dwSampleSize);
	} else
		asie->dwDuration = size;

	asi.fcc				= ((dwChunkId & 0xFFFF)<<16) + 'xi';
	asi.cb				= asie->dwSize - 8;
	asi.wLongsPerEntry	= 2;
	asi.bIndexSubType	= 0;
	asi.bIndexType		= AVI_INDEX_OF_CHUNKS;
	asi.nEntriesInUse	= size;
	asi.dwChunkId		= dwChunkId;
	asi.qwBaseOffset	= offset + 8;
	asi.dwReserved3		= 0;

	FastWrite(&asi, sizeof asi);

	while(size > 0) {
		int tc = size;
		if (tc>64) tc=64;

		for(int i=0; i<tc; i++) {
			asie3[i].dwOffset	= (uint32)(avie2->offset - offset);
			asie3[i].dwSize		= avie2->length_and_flags;
			++avie2;
		}

		FastWrite(asie3, tc*sizeof(_avistdindex_entry));

		size -= tc;
	}
}
