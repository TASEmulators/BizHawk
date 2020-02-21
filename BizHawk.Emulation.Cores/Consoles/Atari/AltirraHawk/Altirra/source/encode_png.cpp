#include <stdafx.h>
#include <stdio.h>
#include <algorithm>
#include <numeric>
#include <vd2/system/zip.h>
#include <vd2/system/error.h>
#include <vd2/system/binary.h>
#include <vd2/Kasumi/pixmap.h>
#include <vd2/Kasumi/pixmapops.h>
#include <vd2/Kasumi/pixmaputils.h>
#include "encode_png.h"
#include "common_png.h"

#ifndef VDFORCEINLINE
	#ifdef _MSC_VER
		#define VDFORCEINLINE __forceinline
	#else
		#define VDFORCEINLINE
	#endif
#endif

namespace {
	const unsigned len_tbl[32]={
		3,4,5,6,7,8,9,10,
		11,13,15,17,19,23,27,31,
		35,43,51,59,67,83,99,115,
		131,163,195,227,258,259
	};

	const unsigned char len_bits_tbl[32]={
		0,0,0,0,0,0,0,0,1,1,1,1,2,2,2,2,3,3,3,3,4,4,4,4,5,5,5,5,0
	};

	const unsigned char dist_bits_tbl[]={
		0,0,0,0,1,1,2,2,3,3,4,4,5,5,6,6,7,7,8,8,9,9,10,10,11,11,12,12,13,13
	};

	const unsigned dist_tbl[]={
		1,2,3,4,5,7,9,13,
		17,25,33,49,65,97,129,193,
		257,385,513,769,1025,1537,2049,3073,
		4097,6145,8193,12289,16385,24577,
		32769
	};

	const unsigned char hclen_tbl[]={
		16,17,18,0,8,7,9,6,10,5,11,4,12,3,13,2,14,1,15
	};

	void PNGPredictEncodeNone(uint8 *dst, const uint8 *row, const uint8 *prevrow, uint32 rowbytes, uint32 bpp) {
		memcpy(dst, row, rowbytes);
	}

	void PNGPredictEncodeSub(uint8 *dst, const uint8 *row, const uint8 *prevrow, uint32 rowbytes, uint32 bpp) {
		for(uint32 i=0; i<bpp; ++i)
			dst[i] = row[i];

		for(uint32 i=bpp; i<rowbytes; ++i)
			dst[i] = row[i] - row[i-bpp];
	}

	void PNGPredictEncodeUp(uint8 *dst, const uint8 *row, const uint8 *prevrow, uint32 rowbytes, uint32 bpp) {
		if (prevrow) {
			for(uint32 i=0; i<rowbytes; ++i)
				dst[i] = row[i] - prevrow[i];
		} else {
			memcpy(dst, row, rowbytes);
		}
	}

	void PNGPredictEncodeAverage(uint8 *dst, const uint8 *row, const uint8 *prevrow, uint32 rowbytes, uint32 bpp) {
		if (prevrow) {
			for(uint32 i=0; i<bpp; ++i)
				dst[i] = row[i] - (prevrow[i]>>1);

			for(uint32 j=bpp; j<rowbytes; ++j)
				dst[j] = row[j] - ((prevrow[j] + row[j-bpp])>>1);
		} else {
			for(uint32 i=0; i<bpp; ++i)
				dst[i] = row[i];

			for(uint32 j=bpp; j<rowbytes; ++j)
				dst[j] = row[j] - (row[j-bpp]>>1);
		}
	}

	void PNGPredictEncodePaeth(uint8 *dst, const uint8 *row, const uint8 *prevrow, uint32 rowbytes, uint32 bpp) {
		using namespace nsVDPNG;

		if (prevrow) {
			for(uint32 i=0; i<bpp; ++i)
				dst[i] = row[i] - PNGPaethPredictor(0, prevrow[i], 0);
			for(uint32 j=bpp; j<rowbytes; ++j)
				dst[j] = row[j] - PNGPaethPredictor(row[j-bpp], prevrow[j], prevrow[j-bpp]);
		} else {
			for(uint32 i=0; i<bpp; ++i)
				dst[i] = row[i];
			for(uint32 j=bpp; j<rowbytes; ++j)
				dst[j] = row[j] - PNGPaethPredictor(row[j-bpp], 0, 0);
		}
	}

	uint32 ComputeSumAbsoluteSignedBytes(const sint8 *src, uint32 len) {
		uint32 sum = 0;
		do {
			sint8 c = *src++;
			sint8 mask = c>>7;

			sum += (c + mask) ^ mask;
		} while(--len);

		return sum;
	}
}

struct VDHuffmanHistoSorterData {
	VDHuffmanHistoSorterData(const int pHisto[288]) {
		for(int i=0; i<288; ++i) {
			mHisto[i] = (pHisto[i] << 9) + 287 - i;
		}
	}

	int mHisto[288];
};

struct VDHuffmanHistoSorter {
	VDHuffmanHistoSorter(const VDHuffmanHistoSorterData& data) : mpHisto(data.mHisto) {}

	// We want to sort by descending probability first, then by ascending code point.
	bool operator()(int f1, int f2) const {
		return mpHisto[f1] > mpHisto[f2];
	}

	const int *mpHisto;
};

class VDPNGHuffmanTable {
public:
	VDPNGHuffmanTable();

	void Init();

	inline void Tally(int c) {
		++mHistogram[c];
	}

	inline void Tally(int c, int count) {
		mHistogram[c] += count;
	}

	void BuildCode(int depth_limit = 15);
	void BuildEncodingTable(uint16 *p, int *l, int limit);
	void BuildStaticLengthEncodingTable(uint16 *p, int *l);
	void BuildStaticDistanceEncodingTable(uint16 *p, int *l);

	uint32 GetCodeCount(int limit) const;
	uint32 GetOutputSize() const;
	uint32 GetStaticOutputSize() const;

	const uint16 *GetDHTSegment() { return mDHT; }
	int GetDHTSegmentLen() const { return mDHTLength; }

private:
	int mHistogram[288];
	int mHistogram2[288];
	uint16 mDHT[288+16];
	int mDHTLength;
};

VDPNGHuffmanTable::VDPNGHuffmanTable() {
	Init();
}

void VDPNGHuffmanTable::Init() {
	std::fill(mHistogram, mHistogram+288, 0);
}

void VDPNGHuffmanTable::BuildCode(int depth_limit) {
	int i;
	int nonzero_codes = 0;

	for(i=0; i<288; ++i) {
		mDHT[i+16] = i;
		if (mHistogram[i])
			++nonzero_codes;
		mHistogram2[i] = mHistogram[i];
	}

	// Codes are stored in the second half of the DHT segment in decreasing
	// order of frequency.

	std::sort(&mDHT[16], &mDHT[16+288], VDHuffmanHistoSorter(VDHuffmanHistoSorterData(mHistogram)));
	mDHTLength = 16 + nonzero_codes;

	// Sort histogram in increasing order.

	std::sort(mHistogram, mHistogram+288);

	int *A = mHistogram+288 - nonzero_codes;

	// Begin merging process (from "In-place calculation of minimum redundancy codes" by A. Moffat and J. Katajainen)
	//
	// There are three merging possibilities:
	//
	// 1) Leaf node with leaf node.
	// 2) Leaf node with internal node.
	// 3) Internal node with internal node.

	int leaf = 2;					// Next, smallest unattached leaf node.
	int internal = 0;				// Next, smallest unattached internal node.

	// Merging always creates one internal node and eliminates one node from
	// the total, so we will always be doing N-1 merges.

	A[0] += A[1];		// First merge is always two leaf nodes.
	for(int next=1; next<nonzero_codes-1; ++next) {		// 'next' is the value that receives the next unattached internal node.
		int a, b;

		// Pick first node.
		if (leaf < nonzero_codes && A[leaf] <= A[internal]) {
			A[next] = a=A[leaf++];			// begin new internal node with P of smallest leaf node
		} else {
			A[next] = a=A[internal];		// begin new internal node with P of smallest internal node
			A[internal++] = next;					// hook smallest internal node as child of new node
		}

		// Pick second node.
		if (internal >= next || (leaf < nonzero_codes && A[leaf] <= A[internal])) {
			A[next] += b=A[leaf++];			// complete new internal node with P of smallest leaf node
		} else {
			A[next] += b=A[internal];		// complete new internal node with P of smallest internal node
			A[internal++] = next;					// hook smallest internal node as child of new node
		}
	}

	// At this point, we have a binary tree composed entirely of pointers to
	// parents, partially sorted such that children are always before their
	// parents in the array.  Traverse the array backwards, replacing each
	// node with its depth in the tree.

	A[nonzero_codes-2] = 0;		// root has height 0 (0 bits)
	for(i = nonzero_codes-3; i>=0; --i)
		A[i] = A[A[i]]+1;		// child height is 1+height(parent).

	// Compute canonical tree bit depths for first part of DHT segment.
	// For each internal node at depth N, add two counts at depth N+1
	// and subtract one count at depth N.  Essentially, we are splitting
	// as we go.  We traverse backwards to ensure that no counts will drop
	// below zero at any time.

	std::fill(mDHT, mDHT+16, 0);

	int overallocation = 0;

	mDHT[0] = 2;		// 2 codes at depth 1 (1 bit)
	for(i = nonzero_codes-3; i>=0; --i) {
		int depth = A[i];

		// The optimal Huffman tree for N nodes can have a depth of N-1,
		// but we have to constrain ourselves at depth 15.  We simply
		// pile up counts at depth 15.  This causes us to overallocate the
		// codespace, but we will compensate for that later.

		if (depth >= depth_limit) {
			++mDHT[depth_limit-1];
		} else {
			--mDHT[depth-1];
			++mDHT[depth];
			++mDHT[depth];
		}
	}

	// Remove the extra code point.
	for(i=15; i>=0; --i) {
		if (mDHT[i])
			overallocation += mDHT[i] * (0x8000 >> i);
	}
	overallocation -= 0x10000;

	// We may have overallocated the codespace if we were forced to shorten
	// some codewords.

	if (overallocation > 0) {
		// Codespace is overallocated.  Begin lengthening codes from bit depth
		// 15 down until we are under the limit.

		i = depth_limit-2;
		while(overallocation > 0) {
			if (mDHT[i]) {
				--mDHT[i];
				++mDHT[i+1];
				overallocation -= 0x4000 >> i;
				if (i < depth_limit-2)
					++i;
			} else
				--i;
		}

		// We may be undercommitted at this point.  Raise codes from bit depth
		// 1 up until we are at the desired limit.

		int underallocation = -overallocation;

		i = 1;
		while(underallocation > 0) {
			if (mDHT[i] && (0x8000>>i) <= underallocation) {
				underallocation -= (0x8000>>i);
				--mDHT[i];
				--i;
				++mDHT[i];
			} else {
				++i;
			}
		}
	}
}

uint32 VDPNGHuffmanTable::GetOutputSize() const {
	const uint16 *pCodes = mDHT+16;

	uint32 size = 0;

	for(int len=0; len<16; ++len) {
		int count = mDHT[len];

		uint32 points = 0;
		while(count--) {
			int code = *pCodes++;

			points += mHistogram2[code];
		}

		size += points * (len + 1);
	}

	return size;
}

uint32 VDPNGHuffmanTable::GetCodeCount(int limit) const {
	return std::accumulate(mHistogram2, mHistogram2+limit, 0);
}

uint32 VDPNGHuffmanTable::GetStaticOutputSize() const {
	uint32 sum7 = 0;
	uint32 sum8 = 0;
	uint32 sum9 = 0;
	sum8 = std::accumulate(mHistogram2+  0, mHistogram2+144, sum8);
	sum9 = std::accumulate(mHistogram2+144, mHistogram2+256, sum9);
	sum7 = std::accumulate(mHistogram2+256, mHistogram2+280, sum7);
	sum8 = std::accumulate(mHistogram2+280, mHistogram2+288, sum8);

	return 7*sum7 + 8*sum8 + 9*sum9;
}

static unsigned revword15(unsigned x) {
	unsigned y = 0;
	for(int i=0; i<15; ++i) {
		y = y + y + (x&1);
		x >>= 1;
	}
	return y;
}

void VDPNGHuffmanTable::BuildEncodingTable(uint16 *p, int *l, int limit) {
	const uint16 *pCodes = mDHT+16;

	uint16 total = 0;
	uint16 inc = 0x4000;

	for(int len=0; len<16; ++len) {
		int count = mDHT[len];

		while(count--) {
			int code = *pCodes++;

			l[code] = len+1;
		}

		for(int k=0; k<limit; ++k) {
			if (l[k] == len+1) {
				p[k] = revword15(total) << (16 - (len+1));
				total += inc;
			}
		}
		inc >>= 1;
	}
}

void VDPNGHuffmanTable::BuildStaticLengthEncodingTable(uint16 *p, int *l) {
	memset(mDHT, 0, sizeof(mDHT[0])*16);
	mDHT[6] = 24;
	mDHT[7] = 152;
	mDHT[8] = 112;

	uint16 *dst = mDHT + 16;
	for(int i=256; i<280; ++i)
		*dst++ = i;
	for(int i=0; i<144; ++i)
		*dst++ = i;
	for(int i=280; i<288; ++i)
		*dst++ = i;
	for(int i=144; i<256; ++i)
		*dst++ = i;

	BuildEncodingTable(p, l, 288);
}

void VDPNGHuffmanTable::BuildStaticDistanceEncodingTable(uint16 *p, int *l) {
	memset(mDHT, 0, sizeof(mDHT[0])*16);
	mDHT[4] = 32;

	for(int i=0; i<32; ++i)
		mDHT[i+16] = i;

	BuildEncodingTable(p, l, 32);
}

class VDPNGDeflateEncoder {
public:
	VDPNGDeflateEncoder();
	VDPNGDeflateEncoder(const VDPNGDeflateEncoder&);
	~VDPNGDeflateEncoder();

	VDPNGDeflateEncoder& operator=(const VDPNGDeflateEncoder&);

	void Init(bool quick);
	void Write(const void *src, size_t len);
	void ForceNewBlock();
	void Finish();

	uint32 EstimateOutputSize();

	vdfastvector<uint8>& GetOutput() { return mOutput; }

protected:
	void EndBlock(bool term);
	void Compress(bool flush);
	void VDFORCEINLINE PutBits(uint32 encoding, int enclen);
	void FlushBits();
	uint32 Flush(int n, int ndists, bool term, bool test);

	uint32	mAccum;
	int		mAccBits;
	uint32	mHistoryPos;
	uint32	mHistoryTail;
	uint32	mHistoryBase;
	uint32	mHistoryBlockStart;
	uint32	mLenExtraBits;
	uint32	mPendingLen;
	uint8	*mpLen;
	uint16	*mpCode;
	uint16	*mpDist;

	uint32	mWindowLimit;

	vdfastvector<uint8> mOutput;
	VDAdler32Checker mAdler32;

	// Block coding tables
	uint16	mCodeEnc[288];
	int		mCodeLen[288];

	uint16	mDistEnc[32];
	int		mDistLen[32];

	uint8	mHistoryBuffer[65536+6];
	sint32	mHashNext[32768];
	sint32	mHashTable[65536];
	uint8	mLenBuf[32769];
	uint16	mCodeBuf[32769];
	uint16	mDistBuf[32769];
};

VDPNGDeflateEncoder::VDPNGDeflateEncoder()
{
}

VDPNGDeflateEncoder::VDPNGDeflateEncoder(const VDPNGDeflateEncoder& src) {
	*this = src;
}

VDPNGDeflateEncoder::~VDPNGDeflateEncoder() {
}

VDPNGDeflateEncoder& VDPNGDeflateEncoder::operator=(const VDPNGDeflateEncoder& src) {
	if (this != &src) {
		mAccum			= src.mAccum;
		mAccBits		= src.mAccBits;
		mHistoryPos		= src.mHistoryPos;
		mHistoryTail	= src.mHistoryTail;
		mHistoryBase	= src.mHistoryBase;
		mHistoryBlockStart = src.mHistoryBlockStart;
		mLenExtraBits = src.mLenExtraBits;
		mPendingLen		= src.mPendingLen;
		mpLen			= mLenBuf + (src.mpLen - src.mLenBuf);
		mpCode			= mCodeBuf + (src.mpCode - src.mCodeBuf);
		mpDist			= mDistBuf + (src.mpDist - src.mDistBuf);
		mWindowLimit	= src.mWindowLimit;
		mOutput			= src.mOutput;
		mAdler32		= src.mAdler32;

		memcpy(mHistoryBuffer, src.mHistoryBuffer, mHistoryTail);
		memcpy(mHashNext, src.mHashNext, sizeof mHashNext);
		memcpy(mHashTable, src.mHashTable, sizeof mHashTable);
		memcpy(mLenBuf, src.mLenBuf, sizeof(mLenBuf[0]) * (src.mpLen - src.mLenBuf));
		memcpy(mCodeBuf, src.mCodeBuf, sizeof(mCodeBuf[0]) * (src.mpCode - src.mCodeBuf));
		memcpy(mDistBuf, src.mDistBuf, sizeof(mDistBuf[0]) * (src.mpDist - src.mDistBuf));
	}
	return *this;
}

void VDPNGDeflateEncoder::Init(bool quick) {
	std::fill(mHashNext, mHashNext+32768, -0x20000);
	std::fill(mHashTable, mHashTable+65536, -0x20000);

	mWindowLimit = quick ? 1024 : 32768;

	mpLen = mLenBuf;
	mpCode = mCodeBuf;
	mpDist = mDistBuf;
	mHistoryPos = 0;
	mHistoryTail = 0;
	mHistoryBase = 0;
	mHistoryBlockStart = 0;
	mLenExtraBits = 0;
	mPendingLen = 0;
	mAccum = 0;
	mAccBits = 0;

	mOutput.push_back(0x78);	// 32K window, Deflate
	mOutput.push_back(0xDA);	// maximum compression, no dictionary, check offset = 0x1A
}

void VDPNGDeflateEncoder::Write(const void *src, size_t len) {
	while(len > 0) {
		uint32 tc = sizeof mHistoryBuffer - mHistoryTail;

		if (!tc) {
			Compress(false);
			continue;
		}

		if ((size_t)tc > len)
			tc = (uint32)len;

		mAdler32.Process(src, tc);
		memcpy(mHistoryBuffer + mHistoryTail, src, tc);

		mHistoryTail += tc;
		src = (const char *)src + tc;
		len -= tc;
	}
}

void VDPNGDeflateEncoder::ForceNewBlock() {
	Compress(false);
	EndBlock(false);
}

#define HASH(pos) (((uint32)hist[(pos)] ^ ((uint32)hist[(pos)+1] << 2) ^ ((uint32)hist[(pos)+2] << 4) ^ ((uint32)hist[(pos)+3] << 6) ^ ((uint32)hist[(pos)+4] << 7) ^ ((uint32)hist[(pos)+5] << 8)) & 0xffff)

void VDPNGDeflateEncoder::EndBlock(bool term) {
	if (mpCode > mCodeBuf) {
		if (mPendingLen) {
			const uint8 *hist = mHistoryBuffer - mHistoryBase;
			int bestlen = mPendingLen - 1;
			mPendingLen = 0;

			while(bestlen-- > 0) {
				int hval = HASH(mHistoryPos);
				mHashNext[mHistoryPos & 0x7fff] = mHashTable[hval];
				mHashTable[hval] = mHistoryPos;
				++mHistoryPos;
			}
		}

		*mpCode++ = 256;
		Flush((int)(mpCode - mCodeBuf), (int)(mpDist - mDistBuf), term, false);
		mpCode = mCodeBuf;
		mpDist = mDistBuf;
		mpLen = mLenBuf;
		mHistoryBlockStart = mHistoryPos;
		mLenExtraBits = 0;
	}
}

void VDPNGDeflateEncoder::Compress(bool flush) {
	uint8	*lenptr = mpLen;
	uint16	*codeptr = mpCode;
	uint16	*distptr = mpDist;

	const uint8 *hist = mHistoryBuffer - mHistoryBase;

	uint32 pos = mHistoryPos;
	uint32 len = mHistoryBase + mHistoryTail;
	uint32 maxpos = flush ? len : len > 258+6 ? len - (258+6) : 0;		// +6 is for the 6-byte hash.
	while(pos < maxpos) {
		if (codeptr >= mCodeBuf + 32768) {
			mpCode = codeptr;
			mpDist = distptr;
			mpLen = lenptr;
			mHistoryPos = pos;
			EndBlock(false);
			pos = mHistoryPos;
			codeptr = mpCode;
			distptr = mpDist;
			lenptr = mpLen;

			// Note that it's possible for the EndBlock() to have flushed out a pending
			// run and pushed us all the way to maxpos.
			VDASSERT(pos <= mHistoryBase + mHistoryTail);
			continue;
		}

		uint8 c = hist[pos];
		uint32 hcode = HASH(pos);

		sint32 hpos = mHashTable[hcode];
		uint32 limit = 258;
		if (limit > len-pos)
			limit = len-pos;

		sint32 hlimit = pos - mWindowLimit;		// note that our initial hash table values are low enough to avoid colliding with this.
		if (hlimit < 0)
			hlimit = 0;

		uint32 bestlen = 5;
		uint32 bestoffset = 0;

		if (hpos >= hlimit && limit >= 6) {
			sint32 hstart = hpos;
			const unsigned char *s2 = hist + pos;
			uint32 matchWord1 = *(const uint32 *)s2;
			uint16 matchWord2 = *(const uint16 *)(s2 + 4);
			do {
				const unsigned char *s1 = hist + hpos - bestlen + 5;
				uint32 mlen = 0;

				if (s1[bestlen] == s2[bestlen] && *(const uint32 *)s1 == matchWord1 && *(const uint16 *)(s1 + 4) == matchWord2) {
					mlen = 6;
					while(mlen < limit && s1[mlen] == s2[mlen])
						++mlen;

					if (mlen > bestlen) {
						bestoffset = pos - hpos + bestlen - 5;
						// hop hash chains!
						hpos += mlen - bestlen;
						if (hpos == pos)
							hpos = hstart;
						else
							hpos = mHashNext[(hpos + mlen - bestlen) & 0x7fff];
						hlimit += (mlen - bestlen);

						bestlen = mlen;
						continue;
					}
				}

				hpos = mHashNext[hpos & 0x7fff];
			} while(hpos >= hlimit);
		}

		// Normally, we'd accept any match of longer 3 or greater. However, the savings for this aren't
		// enough to match the decrease in the effectiveness of the Huffman encoding, so it's usually
		// better to keep only longer matches. We follow the lead of zlib's Z_FILTERED and only accept
		// matches of length 6 or longer. It turns out that we can greatly speed up compression when
		// this is the case since we can use a longer hash -- the PNG filtering often means a very
		// skewed distribution which hinders the effectiveness of a 3-byte hash.
		if (bestlen >= 6) {
			// check for an illegal match
			VDASSERT((uint32)(bestoffset-1) < 32768U);
			VDASSERT(bestlen < 259);
			VDASSERT(!memcmp(hist+pos, hist+pos-bestoffset, bestlen));
			VDASSERT(pos >= bestoffset);
			VDASSERT(pos+bestlen <= len);
			VDASSERT(pos-bestoffset >= mHistoryBase);

			unsigned lcode = 0;
			while(bestlen >= len_tbl[lcode+1])
				++lcode;
			*codeptr++ = lcode + 257;
			*distptr++ = bestoffset;
			*lenptr++ = bestlen - 3;
			mLenExtraBits += len_bits_tbl[lcode];
		} else {
			*codeptr++ = c;
			bestlen = 1;
		}

		// Lazy matching.
		//
		//	prev	current		compare		action
		//	======================================
		//	lit		lit						append
		//	lit		match					stash
		//	match	lit						retire
		//	match	match		shorter		retire
		//	match	match		longer		obsolete
		VDASSERT(pos+bestlen <= mHistoryBase + mHistoryTail);

		if (!mPendingLen) {
			if (bestlen > 1) {
				mPendingLen = bestlen;
				bestlen = 1;
			}
		} else {
			if (bestlen > mPendingLen) {
				codeptr[-2] = hist[pos - 1];
				distptr[-2] = distptr[-1];
				--distptr;
				lenptr[-2] = lenptr[-1];
				--lenptr;
				mPendingLen = bestlen;
				bestlen = 1;
			} else {
				--codeptr;
				if (bestlen > 1) {
					--distptr;
					--lenptr;
				}

				bestlen = mPendingLen - 1;
				mPendingLen = 0;
			}
		}

		VDASSERT(pos+bestlen <= mHistoryBase + mHistoryTail);

		if (bestlen > 0) {
			mHashNext[pos & 0x7fff] = mHashTable[hcode];
			mHashTable[hcode] = pos;
			++pos;

			while(--bestlen) {
				uint32 hcode = HASH(pos);
				mHashNext[pos & 0x7fff] = mHashTable[hcode];
				mHashTable[hcode] = pos;
				++pos;
			}
		}
	}

	// shift down by 32K
	if (pos - mHistoryBase >= 49152) {
		uint32 delta = (pos - 32768) - mHistoryBase;
		memmove(mHistoryBuffer, mHistoryBuffer + delta, mHistoryTail - delta);
		mHistoryBase += delta;
		mHistoryTail -= delta;
	}

	mHistoryPos = pos;
	mpLen = lenptr;
	mpCode = codeptr;
	mpDist = distptr;
}

void VDPNGDeflateEncoder::Finish() {
	while(mHistoryPos != mHistoryBase + mHistoryTail)
		Compress(true);

	VDASSERT(mpCode != mCodeBuf);
	EndBlock(true);

	FlushBits();

	// write Adler32 checksum
	uint8 crc[4];
	VDWriteUnalignedBEU32(crc, mAdler32.Adler32());

	mOutput.insert(mOutput.end(), crc, crc+4);
}

uint32 VDPNGDeflateEncoder::EstimateOutputSize() {
	Compress(false);

	return (uint32)mOutput.size() * 8 + mAccBits + Flush((int)(mpCode - mCodeBuf), (int)(mpDist - mDistBuf), false, true);
}

void VDFORCEINLINE VDPNGDeflateEncoder::PutBits(uint32 encoding, int enclen) {
	mAccum >>= enclen;
	mAccum += encoding;
	mAccBits += enclen;

	if (mAccBits >= 16) {
		mAccBits -= 16;
//		uint8 c[2] = { mAccum >> (16-mAccBits), mAccum >> (24-mAccBits) };

//		mOutput.insert(mOutput.end(), c, c+2);
		mOutput.push_back(mAccum >> (16-mAccBits));
		mOutput.push_back(mAccum >> (24-mAccBits));
	}		
}

void VDPNGDeflateEncoder::FlushBits() {
	while(mAccBits > 0) {
		mOutput.push_back(0xff & (mAccum >> (32-mAccBits)));
		mAccBits -= 8;
	}
}

uint32 VDPNGDeflateEncoder::Flush(int n, int ndists, bool term, bool test) {
	const uint16 *codes = mCodeBuf;
	const uint8 *lens = mLenBuf;
	const uint16 *dists = mDistBuf;

	VDPNGHuffmanTable htcodes, htdists, htlens;
	int i;

	memset(mCodeLen, 0, sizeof mCodeLen);
	memset(mDistLen, 0, sizeof mDistLen);

	for(i=0; i<n; ++i)
		htcodes.Tally(codes[i]);

	htcodes.BuildCode(15);

	for(i=0; i<ndists; ++i) {
		int c=0;
		while(dists[i] >= dist_tbl[c+1])
			++c;

		htdists.Tally(c);
	}

	htdists.BuildCode(15);

	int totalcodes = 286;
	int totaldists = 30;
	int totallens = totalcodes + totaldists;

	htcodes.BuildEncodingTable(mCodeEnc, mCodeLen, 288);
	htdists.BuildEncodingTable(mDistEnc, mDistLen, 32);

	// RLE the length table
	uint8 lenbuf[286+30+1];
	uint8 *lendst = lenbuf;
	uint8 rlebuf[286+30+1];
	uint8 *rledst = rlebuf;

	for(i=0; i<totalcodes; ++i)
		*lendst++ = mCodeLen[i];

	for(i=0; i<totaldists; ++i)
		*lendst++ = mDistLen[i];

	*lendst = 255;		// avoid match

	int last = -1;
	uint32 treeExtraBits = 0;
	i=0;
	while(i<totallens) {
		if (!lenbuf[i] && !lenbuf[i+1] && !lenbuf[i+2]) {
			int j;
			for(j=3; j<138 && !lenbuf[i+j]; ++j)
				;
			if (j < 11) {
				*rledst++ = 17;
				*rledst++ = j-3;
				treeExtraBits += 3;
			} else {
				*rledst++ = 18;
				*rledst++ = j-11;
				treeExtraBits += 7;
			}
			htlens.Tally(rledst[-2]);
			i += j;
			last = 0;
		} else if (lenbuf[i] == last && lenbuf[i+1] == last && lenbuf[i+2] == last) {
			int j;
			for(j=3; j<6 && lenbuf[i+j] == last; ++j)
				;
			*rledst++ = 16;
			htlens.Tally(16);
			*rledst++ = j-3;
			treeExtraBits += 2;
			i += j;
		} else {
			htlens.Tally(*rledst++ = lenbuf[i++]);
			last = lenbuf[i-1];
		}
	}

	htlens.BuildCode(7);

	// compute bits for dynamic encoding
	uint32 blockSize = mHistoryPos - mHistoryBlockStart;
	uint32 alignBits = -(mAccBits+3) & 7;
	uint32 dynamicBlockBits = htcodes.GetOutputSize() + htdists.GetOutputSize() + mLenExtraBits + htlens.GetOutputSize() + 14 + 19*3 + treeExtraBits;
	uint32 staticBlockBits = htcodes.GetStaticOutputSize() + htdists.GetCodeCount(32)*5 + mLenExtraBits;
	uint32 storeBlockBits = blockSize*8 + 32 + alignBits;

	if (storeBlockBits < dynamicBlockBits && storeBlockBits < staticBlockBits) {
		if (test)
			return storeBlockBits;

		PutBits((term ? 0x20000000 : 0) + (0 << 30), 3);

		// align to byte boundary
		PutBits(0, alignBits);

		// write block size
		PutBits((blockSize << 16) & 0xffff0000, 16);
		PutBits((~blockSize << 16) & 0xffff0000, 16);

		// write the block.
		FlushBits();

		const uint8 *base = &mHistoryBuffer[mHistoryBlockStart - mHistoryBase];
		mOutput.insert(mOutput.end(), base, base+blockSize);
	} else {
		if (dynamicBlockBits < staticBlockBits) {
			if (test)
				return dynamicBlockBits;

			PutBits((term ? 0x20000000 : 0) + (2 << 30), 3);

			PutBits((totalcodes - 257) << 27, 5);	// code count - 257
			PutBits((totaldists - 1) << 27, 5);	// dist count - 1
			PutBits(0xf0000000, 4);	// ltbl count - 4

			uint16 hlenc[19];
			int hllen[19]={0};
			htlens.BuildEncodingTable(hlenc, hllen, 19);

			for(i=0; i<19; ++i) {
				int k = hclen_tbl[i];

				PutBits(hllen[k] << 29, 3);
			}

			uint8 *rlesrc = rlebuf;
			while(rlesrc < rledst) {
				uint8 c = *rlesrc++;
				PutBits((uint32)hlenc[c] << 16, hllen[c]);

				if (c == 16)
					PutBits((uint32)*rlesrc++ << 30, 2);
				else if (c == 17)
					PutBits((uint32)*rlesrc++ << 29, 3);
				else if (c == 18)
					PutBits((uint32)*rlesrc++ << 25, 7);
			}
		} else {
			if (test)
				return staticBlockBits;

			PutBits((term ? 0x20000000 : 0) + (1 << 30), 3);

			memset(mCodeLen, 0, sizeof(mCodeLen));
			memset(mDistLen, 0, sizeof(mDistLen));
			htcodes.BuildStaticLengthEncodingTable(mCodeEnc, mCodeLen);
			htdists.BuildStaticDistanceEncodingTable(mDistEnc, mDistLen);
		}

		for(i=0; i<n; ++i) {
			unsigned code = *codes++;
			unsigned clen = mCodeLen[code];

			PutBits((uint32)mCodeEnc[code] << 16, clen);

			if (code >= 257) {
				unsigned extralenbits = len_bits_tbl[code-257];
				unsigned len = *lens++ + 3;

				VDASSERT(len >= len_tbl[code-257]);
				VDASSERT(len < len_tbl[code-256]);

				if (extralenbits)
					PutBits((len - len_tbl[code-257]) << (32 - extralenbits), extralenbits);

				unsigned dist = *dists++;
				int dcode=0;
				while(dist >= dist_tbl[dcode+1])
					++dcode;

				PutBits((uint32)mDistEnc[dcode] << 16, mDistLen[dcode]);

				unsigned extradistbits = dist_bits_tbl[dcode];

				if (extradistbits)
					PutBits((dist - dist_tbl[dcode]) << (32 - extradistbits), extradistbits);
			}
		}
	}

	return 0;
}

class VDImageEncoderPNG : public IVDImageEncoderPNG {
public:
	VDImageEncoderPNG();
	~VDImageEncoderPNG();

	void Encode(const VDPixmap& px, const void *&p, uint32& len, bool quick);

protected:
	vdfastvector<uint8>	mOutput;
};

VDImageEncoderPNG::VDImageEncoderPNG() {
}

VDImageEncoderPNG::~VDImageEncoderPNG() {
}

void VDImageEncoderPNG::Encode(const VDPixmap& px, const void *&p, uint32& len, bool quick) {
	using namespace nsVDPNG;

	mOutput.assign(kPNGSignature, kPNGSignature + 8);

	struct IHDR {
		uint32	mChunkLength;
		uint32	mChunkType;
		uint32	mWidth;
		uint32	mHeight;
		uint8	mDepth;
		uint8	mColorType;
		uint8	mCompression;
		uint8	mFilterMethod;
		uint8	mInterlaceMethod;
	} ihdr;

	ihdr.mChunkLength		= VDToBE32(13);
	ihdr.mChunkType			= VDMAKEFOURCC('I', 'H', 'D', 'R');
	ihdr.mWidth				= VDToBE32(px.w);
	ihdr.mHeight			= VDToBE32(px.h);
	ihdr.mDepth				= 8;
	ihdr.mColorType			= 2;		// truecolor
	ihdr.mCompression		= 0;		// Deflate
	ihdr.mFilterMethod		= 0;		// basic adaptive filtering
	ihdr.mInterlaceMethod	= 0;		// no interlacing

	const VDCRCTable& crcTable = VDCRCTable::CRC32;
	uint32 ihdr_crc = VDToBE32(crcTable.CRC(&ihdr.mChunkType, 17));

	mOutput.insert(mOutput.end(), (const uint8 *)&ihdr, (const uint8 *)&ihdr + 21);
	mOutput.insert(mOutput.end(), (const uint8 *)&ihdr_crc, (const uint8 *)&ihdr_crc + 4);

	VDPixmapBuffer pxtmp(px.w, px.h, nsVDPixmap::kPixFormat_RGB888);
	VDPixmapBlt(pxtmp, px);

	vdautoptr<VDPNGDeflateEncoder> enc(new VDPNGDeflateEncoder);	// way too big for stack

	const uint32 w = pxtmp.w;
	const uint32 rowbytes = w*3;
	vdfastvector<uint8> temprowbuf(rowbytes*5);
	uint8 *tempmem = temprowbuf.data();
	const uint8 *prevrow = NULL;

	enc->Init(quick);

	for(uint32 y=0; y<(uint32)pxtmp.h; ++y) {
		// swap red and blue for this row
		uint8 *dst = (uint8 *)pxtmp.data + pxtmp.pitch * y;
		const uint8 *src = dst;
		for(uint32 x=w; x; --x) {
			uint8 b = dst[0];
			uint8 r = dst[2];
			dst[0] = r;
			dst[2] = b;
			dst += 3;
		}

		// try all predictors
		static void (*const predictors[])(uint8 *dst, const uint8 *row, const uint8 *prevrow, uint32 rowbytes, uint32 bpp) = {
			PNGPredictEncodeNone,
			PNGPredictEncodeSub,
			PNGPredictEncodeUp,
			PNGPredictEncodeAverage,
			PNGPredictEncodePaeth,
		};

		uint32 best = 0;
		uint32 bestscore = 0xFFFFFFFF;

		for(int i=0; i<5; ++i) {
			uint8 *dst = tempmem + rowbytes * i;
			predictors[i](dst, src, prevrow, rowbytes, 3);

			uint32 score = ComputeSumAbsoluteSignedBytes((const sint8*)dst, rowbytes);
			if (score < bestscore) {
				best = i;
				bestscore = score;
			}
		}

		const uint8 comp = best;
		enc->Write(&comp, 1);
		enc->Write(tempmem + rowbytes * best, rowbytes);

		prevrow = src;
	}
	enc->Finish();
	vdfastvector<uint8>& encoutput = enc->GetOutput();

	struct IDAT {
		uint32	mChunkLength;
		uint32	mChunkType;
	} idat;
	idat.mChunkLength		= VDToBE32((uint32)encoutput.size());
	idat.mChunkType			= VDMAKEFOURCC('I', 'D', 'A', 'T');

	mOutput.insert(mOutput.end(), (const uint8 *)&idat, (const uint8 *)&idat + 8);
	mOutput.insert(mOutput.end(), encoutput.begin(), encoutput.end());

	VDCRCChecker crcChecker(crcTable);
	crcChecker.Process(&idat.mChunkType, 4);
	crcChecker.Process(encoutput.data(), (sint32)encoutput.size());
	uint32 idat_crc = VDToBE32(crcChecker.CRC());
	mOutput.insert(mOutput.end(), (const uint8 *)&idat_crc, (const uint8 *)&idat_crc + 4);

	uint8 footer[]={
		0, 0, 0, 0, 'I', 'E', 'N', 'D', 0, 0, 0, 0
	};

	VDWriteUnalignedBEU32(footer+8, crcTable.CRC(footer + 4, 4));

	mOutput.insert(mOutput.end(), footer, footer+12);

	p = mOutput.data();
	len = (uint32)mOutput.size();
}

IVDImageEncoderPNG *VDCreateImageEncoderPNG() {
	return new VDImageEncoderPNG;
}
