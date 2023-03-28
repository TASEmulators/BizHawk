//
// Object Processor
//
// Original source by David Raingeard (Cal2)
// GCC/SDL port by Niels Wagenaar (Linux/WIN32) and Caz (BeOS)
// Extensive cleanups/fixes/rewrites by James Hammons
// (C) 2010 Underground Software
//
// JLH = James Hammons <jlhamm@acm.org>
//
// Who  When        What
// ---  ----------  -----------------------------------------------------------
// JLH  01/16/2010  Created this log ;-)
//

#include "op.h"

#include <stdlib.h>
#include <string.h>
#include "gpu.h"
#include "jaguar.h"
#include "m68000/m68kinterface.h"
#include "memory.h"
#include "tom.h"

#define BLEND_Y(dst, src)	op_blend_y[(((uint16_t)dst<<8)) | ((uint16_t)(src))]
#define BLEND_CR(dst, src)	op_blend_cr[(((uint16_t)dst)<<8) | ((uint16_t)(src))]

#define OBJECT_TYPE_BITMAP	0					// 000
#define OBJECT_TYPE_SCALE	1					// 001
#define OBJECT_TYPE_GPU		2					// 010
#define OBJECT_TYPE_BRANCH	3					// 011
#define OBJECT_TYPE_STOP	4					// 100

#define CONDITION_EQUAL				0			// VC == YPOS
#define CONDITION_LESS_THAN			1			// VC < YPOS
#define CONDITION_GREATER_THAN		2			// VC > YPOS
#define CONDITION_OP_FLAG_SET		3
#define CONDITION_SECOND_HALF_LINE	4

static void OPProcessFixedBitmap(uint64_t p0, uint64_t p1);
static void OPProcessScaledBitmap(uint64_t p0, uint64_t p1, uint64_t p2);

// Local global variables

// Blend tables (64K each)
static uint8_t op_blend_y[0x10000];
static uint8_t op_blend_cr[0x10000];

static uint32_t op_pointer;

static const int32_t phraseWidthToPixels[8] = { 64, 32, 16, 8, 4, 2, 0, 0 };

//
// Object Processor initialization
//
void OPInit(void)
{
	for(int i=0; i<256*256; i++)
	{
		int y = (i >> 8) & 0xFF;
		int dy = (int8_t)i;
		int c1 = (i >> 8) & 0x0F;
		int dc1 = (int8_t)(i << 4) >> 4;
		int c2 = (i >> 12) & 0x0F;
		int dc2 = (int8_t)(i & 0xF0) >> 4;

		y += dy;

		if (y < 0)
			y = 0;
		else if (y > 0xFF)
			y = 0xFF;

		op_blend_y[i] = y;

		c1 += dc1;

		if (c1 < 0)
			c1 = 0;
		else if (c1 > 0x0F)
			c1 = 0x0F;

		c2 += dc2;

		if (c2 < 0)
			c2 = 0;
		else if (c2 > 0x0F)
			c2 = 0x0F;

		op_blend_cr[i] = (c2 << 4) | c1;
	}

	OPReset();
}

//
// Object Processor reset
//
void OPReset(void)
{
}

static uint32_t OPGetListPointer(void)
{
	return GET16(tomRam8, 0x20) | (GET16(tomRam8, 0x22) << 16);
}

static uint32_t OPGetStatusRegister(void)
{
	return GET16(tomRam8, 0x26);
}

static void OPSetCurrentObject(uint64_t object)
{
	tomRam8[0x17] = object & 0xFF; object >>= 8;
	tomRam8[0x16] = object & 0xFF; object >>= 8;
	tomRam8[0x15] = object & 0xFF; object >>= 8;
	tomRam8[0x14] = object & 0xFF; object >>= 8;

	tomRam8[0x13] = object & 0xFF; object >>= 8;
	tomRam8[0x12] = object & 0xFF; object >>= 8;
	tomRam8[0x11] = object & 0xFF; object >>= 8;
	tomRam8[0x10] = object & 0xFF;
}

static uint64_t OPLoadPhrase(uint32_t offset)
{
	offset &= ~0x07;
	return ((uint64_t)JaguarReadLong(offset, OP) << 32) | (uint64_t)JaguarReadLong(offset+4, OP);
}

static void OPStorePhrase(uint32_t offset, uint64_t p)
{
	offset &= ~0x07;
	JaguarWriteLong(offset, p >> 32, OP);
	JaguarWriteLong(offset + 4, p & 0xFFFFFFFF, OP);
}

//
// Object Processor main routine
//
void OPProcessList(int halfline)
{
	halfline &= 0x7FF;

	op_pointer = OPGetListPointer();

	uint32_t opCyclesToRun = 30000;

	while (op_pointer)
	{
		uint64_t p0 = OPLoadPhrase(op_pointer);
		op_pointer += 8;

		switch ((uint8_t)p0 & 0x07)
		{
			case OBJECT_TYPE_BITMAP:
			{
				uint16_t ypos = (p0 >> 3) & 0x7FF;
				uint32_t height = (p0 & 0xFFC000) >> 14;
				uint32_t oldOPP = op_pointer - 8;

				if (halfline >= ypos && height > 0)
				{
					uint64_t p1 = OPLoadPhrase(oldOPP | 0x08);
					OPProcessFixedBitmap(p0, p1);

					height--;

					uint64_t data = (p0 & 0xFFFFF80000000000LL) >> 40;
					uint64_t dwidth = (p1 & 0xFFC0000) >> 15;
					data += dwidth;

					p0 &= ~0xFFFFF80000FFC000LL;
					p0 |= (uint64_t)height << 14;
					p0 |= data << 40;
					OPStorePhrase(oldOPP, p0);
				}

				op_pointer = (p0 & 0x000007FFFF000000LL) >> 21;

				if (op_pointer > 0x1FFFFF && op_pointer < 0x800000)
					op_pointer &= 0xFF1FFFFF;

				break;
			}
			case OBJECT_TYPE_SCALE:
			{
				uint16_t ypos = (p0 >> 3) & 0x7FF;
				uint32_t height = (p0 & 0xFFC000) >> 14;
				uint32_t oldOPP = op_pointer - 8;

				if (halfline >= ypos && height > 0)
				{
					uint64_t p1 = OPLoadPhrase(oldOPP | 0x08);
					uint64_t p2 = OPLoadPhrase(oldOPP | 0x10);
					OPProcessScaledBitmap(p0, p1, p2);

					uint16_t remainder = (p2 >> 16) & 0xFF;
					uint8_t vscale = p2 >> 8;

					if (vscale == 0)
						vscale = 0x20;

					if (remainder < 0x20)
					{
						uint64_t data = (p0 & 0xFFFFF80000000000LL) >> 40;
						uint64_t dwidth = (p1 & 0xFFC0000) >> 15;

						while (remainder < 0x20)
						{
							remainder += vscale;

							if (height)
								height--;

							data += dwidth;
						}

						p0 &= ~0xFFFFF80000FFC000LL;
						p0 |= (uint64_t)height << 14;
						p0 |= data << 40;
						OPStorePhrase(oldOPP, p0);
					}

					remainder -= 0x20;

					p2 &= ~0x0000000000FF0000LL;
					p2 |= (uint64_t)remainder << 16;
					OPStorePhrase(oldOPP + 16, p2);
				}

				op_pointer = (p0 & 0x000007FFFF000000LL) >> 21;

				if (op_pointer > 0x1FFFFF && op_pointer < 0x800000)
					op_pointer &= 0xFF1FFFFF;

				break;
			}
			case OBJECT_TYPE_GPU:
			{
				OPSetCurrentObject(p0);
				GPUSetIRQLine(3, ASSERT_LINE);
				break;
			}
			case OBJECT_TYPE_BRANCH:
			{
				uint16_t ypos = (p0 >> 3) & 0x7FF;
				uint8_t  cc   = (p0 >> 14) & 0x07;
				uint32_t link = (p0 >> 21) & 0x3FFFF8;

				switch (cc)
				{
					case CONDITION_EQUAL:
						if (halfline == ypos || ypos == 0x7FF)
							op_pointer = link;
						break;
					case CONDITION_LESS_THAN:
						if (halfline < ypos)
							op_pointer = link;
						break;
					case CONDITION_GREATER_THAN:
						if (halfline > ypos)
							op_pointer = link;
						break;
					case CONDITION_OP_FLAG_SET:
						if (OPGetStatusRegister() & 0x01)
							op_pointer = link;
						break;
					case CONDITION_SECOND_HALF_LINE:
						if (TOMGetHC() & 0x0400)
							op_pointer = link;
						break;
				}
				break;
			}
			case OBJECT_TYPE_STOP:
			{
				OPSetCurrentObject(p0);

				if ((p0 & 0x08) && TOMIRQEnabled(IRQ_OPFLAG))
				{
					TOMSetPendingObjectInt();
					m68k_set_irq(2);
				}

				return;
			}
		}

		opCyclesToRun--;

		if (!opCyclesToRun)
			return;
	}
}

//
// Store fixed size bitmap in line buffer
//
static void OPProcessFixedBitmap(uint64_t p0, uint64_t p1)
{
	uint8_t depth = (p1 >> 12) & 0x07;
	int32_t xpos = ((int16_t)((p1 << 4) & 0xFFFF)) >> 4;
	uint32_t iwidth = (p1 >> 28) & 0x3FF;
	uint32_t data = (p0 >> 40) & 0xFFFFF8;
	uint32_t firstPix = (p1 >> 49) & 0x3F;
	firstPix &= 0x3E;

	uint8_t flags = (p1 >> 45) & 0x07;
	bool flagREFLECT = (flags & OPFLAG_REFLECT ? true : false),
		flagRMW = (flags & OPFLAG_RMW ? true : false),
		flagTRANS = (flags & OPFLAG_TRANS ? true : false);

	uint8_t index = (p1 >> 37) & 0xFE;
	uint32_t pitch = (p1 >> 15) & 0x07;
	pitch <<= 3;

	uint8_t * paletteRAM = &tomRam8[0x400];
	uint16_t * paletteRAM16 = (uint16_t *)paletteRAM;

	if (iwidth == 0)
		iwidth = 1;

	if (iwidth == 0)
		return;

	int32_t startPos = xpos, endPos = xpos +
		(!flagREFLECT ? (phraseWidthToPixels[depth] * iwidth) - 1
		: -((phraseWidthToPixels[depth] * iwidth) + 1));
	uint32_t clippedWidth = 0, phraseClippedWidth = 0, dataClippedWidth = 0;
	bool in24BPPMode = (((GET16(tomRam8, 0x0028) >> 1) & 0x03) == 1 ? true : false);

	int32_t limit = 720;
	int32_t lbufWidth = 719;

	if ((!flagREFLECT && (endPos < 0 || startPos > lbufWidth))
		|| (flagREFLECT && (startPos < 0 || endPos > lbufWidth)))
		return;

	if (startPos < 0)
		clippedWidth = 0 - startPos,
		dataClippedWidth = phraseClippedWidth = clippedWidth / phraseWidthToPixels[depth],
		startPos = 0 - (clippedWidth % phraseWidthToPixels[depth]);

	if (endPos < 0)
		clippedWidth = 0 - endPos,
		phraseClippedWidth = clippedWidth / phraseWidthToPixels[depth];

	if (endPos > lbufWidth)
		clippedWidth = endPos - lbufWidth,
		phraseClippedWidth = clippedWidth / phraseWidthToPixels[depth];

	if (startPos > lbufWidth)
		clippedWidth = startPos - lbufWidth,
		dataClippedWidth = phraseClippedWidth = clippedWidth / phraseWidthToPixels[depth],
		startPos = lbufWidth + (clippedWidth % phraseWidthToPixels[depth]);

	iwidth -= phraseClippedWidth;

	data += dataClippedWidth * pitch;

	uint32_t lbufAddress = 0x1800 + (startPos * 2);
	uint8_t * currentLineBuffer = &tomRam8[lbufAddress];

	if (depth == 0)
	{
		int32_t lbufDelta = ((int8_t)((flags << 7) & 0xFF) >> 5) | 0x02;

		uint64_t pixels = ((uint64_t)JaguarReadLong(data, OP) << 32) | JaguarReadLong(data + 4, OP);
		pixels <<= firstPix;
		int i = firstPix;

		while (iwidth--)
		{
			while (i++ < 64)
			{
				uint8_t bit = pixels >> 63;
				if (flagTRANS && bit == 0)
					;
				else
				{
					if (!flagRMW)
						*(uint16_t *)currentLineBuffer = paletteRAM16[index | bit];
					else
						*currentLineBuffer =
							BLEND_CR(*currentLineBuffer, paletteRAM[(index | bit) << 1]),
						*(currentLineBuffer + 1) =
							BLEND_Y(*(currentLineBuffer + 1), paletteRAM[((index | bit) << 1) + 1]);
				}

				currentLineBuffer += lbufDelta;
				pixels <<= 1;
			}

			i = 0;
			data += pitch;
			pixels = ((uint64_t)JaguarReadLong(data, OP) << 32) | JaguarReadLong(data + 4, OP);
		}
	}
	else if (depth == 1)
	{
		index &= 0xFC;
		int32_t lbufDelta = ((int8_t)((flags << 7) & 0xFF) >> 5) | 0x02;

		while (iwidth--)
		{
			uint64_t pixels = ((uint64_t)JaguarReadLong(data, OP) << 32) | JaguarReadLong(data + 4, OP);
			data += pitch;

			for(int i=0; i<32; i++)
			{
				uint8_t bits = pixels >> 62;
				if (flagTRANS && bits == 0)
					;
				else
				{
					if (!flagRMW)
						*(uint16_t *)currentLineBuffer = paletteRAM16[index | bits];
					else
						*currentLineBuffer =
							BLEND_CR(*currentLineBuffer, paletteRAM[(index | bits) << 1]),
						*(currentLineBuffer + 1) =
							BLEND_Y(*(currentLineBuffer + 1), paletteRAM[((index | bits) << 1) + 1]);
				}

				currentLineBuffer += lbufDelta;
				pixels <<= 2;
			}
		}
	}
	else if (depth == 2)
	{
		index &= 0xF0;
		int32_t lbufDelta = ((int8_t)((flags << 7) & 0xFF) >> 5) | 0x02;

		while (iwidth--)
		{
			uint64_t pixels = ((uint64_t)JaguarReadLong(data, OP) << 32) | JaguarReadLong(data + 4, OP);
			data += pitch;

			for(int i=0; i<16; i++)
			{
				uint8_t bits = pixels >> 60;
				if (flagTRANS && bits == 0)
					;
				else
				{
					if (!flagRMW)
						*(uint16_t *)currentLineBuffer = paletteRAM16[index | bits];
					else
						*currentLineBuffer =
							BLEND_CR(*currentLineBuffer, paletteRAM[(index | bits) << 1]),
						*(currentLineBuffer + 1) =
							BLEND_Y(*(currentLineBuffer + 1), paletteRAM[((index | bits) << 1) + 1]);
				}

				currentLineBuffer += lbufDelta;
				pixels <<= 4;
			}
		}
	}
	else if (depth == 3)
	{
		int32_t lbufDelta = ((int8_t)((flags << 7) & 0xFF) >> 5) | 0x02;

		uint64_t pixels = ((uint64_t)JaguarReadLong(data, OP) << 32) | JaguarReadLong(data + 4, OP);
		firstPix &= 0x30;
		pixels <<= firstPix;
		int i = firstPix >> 3;

		while (iwidth--)
		{
			while (i++ < 8)
			{
				uint8_t bits = pixels >> 56;
				if (flagTRANS && bits == 0)
					;
				else
				{
					if (!flagRMW)
						*(uint16_t *)currentLineBuffer = paletteRAM16[bits];
					else
						*currentLineBuffer =
							BLEND_CR(*currentLineBuffer, paletteRAM[bits << 1]),
						*(currentLineBuffer + 1) =
							BLEND_Y(*(currentLineBuffer + 1), paletteRAM[(bits << 1) + 1]);
				}

				currentLineBuffer += lbufDelta;
				pixels <<= 8;
			}
			i = 0;

			data += pitch;
			pixels = ((uint64_t)JaguarReadLong(data, OP) << 32) | JaguarReadLong(data + 4, OP);
		}
	}
	else if (depth == 4)
	{
		int32_t lbufDelta = ((int8_t)((flags << 7) & 0xFF) >> 5) | 0x02;

		while (iwidth--)
		{
			uint64_t pixels = ((uint64_t)JaguarReadLong(data, OP) << 32) | JaguarReadLong(data + 4, OP);
			data += pitch;

			for(int i=0; i<4; i++)
			{
				uint8_t bitsHi = pixels >> 56, bitsLo = pixels >> 48;

				if (flagTRANS && ((bitsLo | bitsHi) == 0))
					;
				else
				{
					if (!flagRMW)
						*currentLineBuffer = bitsHi,
						*(currentLineBuffer + 1) = bitsLo;
					else
						*currentLineBuffer =
							BLEND_CR(*currentLineBuffer, bitsHi),
						*(currentLineBuffer + 1) =
							BLEND_Y(*(currentLineBuffer + 1), bitsLo);
				}

				currentLineBuffer += lbufDelta;
				pixels <<= 16;
			}
		}
	}
	else if (depth == 5)
	{
		int32_t lbufDelta = ((int8_t)((flags << 7) & 0xFF) >> 4) | 0x04;

		while (iwidth--)
		{
			uint64_t pixels = ((uint64_t)JaguarReadLong(data, OP) << 32) | JaguarReadLong(data + 4, OP);
			data += pitch;

			for(int i=0; i<2; i++)
			{
				uint8_t bits3 = pixels >> 56, bits2 = pixels >> 48,
					bits1 = pixels >> 40, bits0 = pixels >> 32;

				if (flagTRANS && (bits3 | bits2 | bits1 | bits0) == 0)
					;
				else
					*currentLineBuffer = bits3,
					*(currentLineBuffer + 1) = bits2,
					*(currentLineBuffer + 2) = bits1,
					*(currentLineBuffer + 3) = bits0;

				currentLineBuffer += lbufDelta;
				pixels <<= 32;
			}
		}
	}
}

//
// Store scaled bitmap in line buffer
//
static void OPProcessScaledBitmap(uint64_t p0, uint64_t p1, uint64_t p2)
{
	uint8_t depth = (p1 >> 12) & 0x07;
	int32_t xpos = ((int16_t)((p1 << 4) & 0xFFFF)) >> 4;
	uint32_t iwidth = (p1 >> 28) & 0x3FF;
	uint32_t data = (p0 >> 40) & 0xFFFFF8;

	uint32_t firstPix = (p1 >> 49) & 0x3F;

	uint8_t flags = (p1 >> 45) & 0x07;
	bool flagREFLECT = (flags & OPFLAG_REFLECT ? true : false),
		flagRMW = (flags & OPFLAG_RMW ? true : false),
		flagTRANS = (flags & OPFLAG_TRANS ? true : false);
	uint8_t index = (p1 >> 37) & 0xFE;
	uint32_t pitch = (p1 >> 15) & 0x07;

	uint8_t * paletteRAM = &tomRam8[0x400];
	uint16_t * paletteRAM16 = (uint16_t *)paletteRAM;

	uint16_t hscale = p2 & 0xFF;
	uint16_t horizontalRemainder = hscale;
	int32_t scaledWidthInPixels = (iwidth * phraseWidthToPixels[depth] * hscale) >> 5;
	uint32_t scaledPhrasePixels = (phraseWidthToPixels[depth] * hscale) >> 5;

	if (iwidth == 0 || hscale == 0)
		return;

	int32_t startPos = xpos, endPos = xpos +
		(!flagREFLECT ? scaledWidthInPixels - 1 : -(scaledWidthInPixels + 1));
	uint32_t clippedWidth = 0, phraseClippedWidth = 0, dataClippedWidth = 0;
	bool in24BPPMode = (((GET16(tomRam8, 0x0028) >> 1) & 0x03) == 1 ? true : false);

	int32_t limit = 720;
	int32_t lbufWidth = 719;

	if ((!flagREFLECT && (endPos < 0 || startPos > lbufWidth))
		|| (flagREFLECT && (startPos < 0 || endPos > lbufWidth)))
		return;

	uint32_t scaledPhrasePixelsUS = phraseWidthToPixels[depth] * hscale;

	if (startPos < 0)
		clippedWidth = (0 - startPos) << 5,
		dataClippedWidth = phraseClippedWidth = (clippedWidth / scaledPhrasePixelsUS) >> 5,
		startPos += (dataClippedWidth * scaledPhrasePixelsUS) >> 5;

	if (endPos < 0)
		clippedWidth = 0 - endPos,
		phraseClippedWidth = clippedWidth / scaledPhrasePixels;

	if (endPos > lbufWidth)
		clippedWidth = endPos - lbufWidth,
		phraseClippedWidth = clippedWidth / scaledPhrasePixels;

	if (startPos > lbufWidth)
		clippedWidth = startPos - lbufWidth,
		dataClippedWidth = phraseClippedWidth = clippedWidth / scaledPhrasePixels,
		startPos = lbufWidth + (clippedWidth % scaledPhrasePixels);

	iwidth -= phraseClippedWidth;

	data += dataClippedWidth * (pitch << 3);

	uint32_t lbufAddress = 0x1800 + startPos * 2;
	uint8_t * currentLineBuffer = &tomRam8[lbufAddress];

	if (depth == 0)
	{
		int32_t lbufDelta = ((int8_t)((flags << 7) & 0xFF) >> 5) | 0x02;

		int pixCount = 0;
		uint64_t pixels = ((uint64_t)JaguarReadLong(data, OP) << 32) | JaguarReadLong(data + 4, OP);

		while ((int32_t)iwidth > 0)
		{
			uint8_t bits = pixels >> 63;

			if (flagTRANS && bits == 0)
				;
			else
			{
				if (!flagRMW)
					*(uint16_t *)currentLineBuffer = paletteRAM16[index | bits];
				else
					*currentLineBuffer =
						BLEND_CR(*currentLineBuffer, paletteRAM[(index | bits) << 1]),
					*(currentLineBuffer + 1) =
						BLEND_Y(*(currentLineBuffer + 1), paletteRAM[((index | bits) << 1) + 1]);
			}

			currentLineBuffer += lbufDelta;

			while (horizontalRemainder < 0x20)
			{
				horizontalRemainder += hscale;
				pixCount++;
				pixels <<= 1;
			}
			horizontalRemainder -= 0x20;

			if (pixCount > 63)
			{
				int phrasesToSkip = pixCount / 64, pixelShift = pixCount % 64;

				data += (pitch << 3) * phrasesToSkip;
				pixels = ((uint64_t)JaguarReadLong(data, OP) << 32) | JaguarReadLong(data + 4, OP);
				pixels <<= 1 * pixelShift;
				iwidth -= phrasesToSkip;
				pixCount = pixelShift;
			}
		}
	}
	else if (depth == 1)
	{
		index &= 0xFC;
		int32_t lbufDelta = ((int8_t)((flags << 7) & 0xFF) >> 5) | 0x02;

		int pixCount = 0;
		uint64_t pixels = ((uint64_t)JaguarReadLong(data, OP) << 32) | JaguarReadLong(data + 4, OP);

		while ((int32_t)iwidth > 0)
		{
			uint8_t bits = pixels >> 62;

			if (flagTRANS && bits == 0)
				;
			else
			{
				if (!flagRMW)
					*(uint16_t *)currentLineBuffer = paletteRAM16[index | bits];
				else
					*currentLineBuffer =
						BLEND_CR(*currentLineBuffer, paletteRAM[(index | bits) << 1]),
					*(currentLineBuffer + 1) =
						BLEND_Y(*(currentLineBuffer + 1), paletteRAM[((index | bits) << 1) + 1]);
			}

			currentLineBuffer += lbufDelta;

			while (horizontalRemainder < 0x20)
			{
				horizontalRemainder += hscale;
				pixCount++;
				pixels <<= 2;
			}
			horizontalRemainder -= 0x20;

			if (pixCount > 31)
			{
				int phrasesToSkip = pixCount / 32, pixelShift = pixCount % 32;

				data += (pitch << 3) * phrasesToSkip;
				pixels = ((uint64_t)JaguarReadLong(data, OP) << 32) | JaguarReadLong(data + 4, OP);
				pixels <<= 2 * pixelShift;
				iwidth -= phrasesToSkip;
				pixCount = pixelShift;
			}
		}
	}
	else if (depth == 2)
	{
		index &= 0xF0;
		int32_t lbufDelta = ((int8_t)((flags << 7) & 0xFF) >> 5) | 0x02;

		int pixCount = 0;
		uint64_t pixels = ((uint64_t)JaguarReadLong(data, OP) << 32) | JaguarReadLong(data + 4, OP);

		while ((int32_t)iwidth > 0)
		{
			uint8_t bits = pixels >> 60;

			if (flagTRANS && bits == 0)
				;
			else
			{
				if (!flagRMW)
					*(uint16_t *)currentLineBuffer = paletteRAM16[index | bits];
				else
					*currentLineBuffer =
						BLEND_CR(*currentLineBuffer, paletteRAM[(index | bits) << 1]),
					*(currentLineBuffer + 1) =
						BLEND_Y(*(currentLineBuffer + 1), paletteRAM[((index | bits) << 1) + 1]);
			}

			currentLineBuffer += lbufDelta;

			while (horizontalRemainder < 0x20)
			{
				horizontalRemainder += hscale;
				pixCount++;
				pixels <<= 4;
			}
			horizontalRemainder -= 0x20;

			if (pixCount > 15)
			{
				int phrasesToSkip = pixCount / 16, pixelShift = pixCount % 16;

				data += (pitch << 3) * phrasesToSkip;
				pixels = ((uint64_t)JaguarReadLong(data, OP) << 32) | JaguarReadLong(data + 4, OP);
				pixels <<= 4 * pixelShift;
				iwidth -= phrasesToSkip;
				pixCount = pixelShift;
			}
		}
	}
	else if (depth == 3)
	{
		int32_t lbufDelta = ((int8_t)((flags << 7) & 0xFF) >> 5) | 0x02;

		int pixCount = 0;
		uint64_t pixels = ((uint64_t)JaguarReadLong(data, OP) << 32) | JaguarReadLong(data + 4, OP);

		while ((int32_t)iwidth > 0)
		{
			uint8_t bits = pixels >> 56;

			if (flagTRANS && bits == 0)
				;
			else
			{
				if (!flagRMW)
					*(uint16_t *)currentLineBuffer = paletteRAM16[bits];
				else
					*currentLineBuffer =
						BLEND_CR(*currentLineBuffer, paletteRAM[bits << 1]),
					*(currentLineBuffer + 1) =
						BLEND_Y(*(currentLineBuffer + 1), paletteRAM[(bits << 1) + 1]);
			}

			currentLineBuffer += lbufDelta;

			while (horizontalRemainder < 0x20)
			{
				horizontalRemainder += hscale;
				pixCount++;
				pixels <<= 8;
			}
			horizontalRemainder -= 0x20;

			if (pixCount > 7)
			{
				int phrasesToSkip = pixCount / 8, pixelShift = pixCount % 8;

				data += (pitch << 3) * phrasesToSkip;
				pixels = ((uint64_t)JaguarReadLong(data, OP) << 32) | JaguarReadLong(data + 4, OP);
				pixels <<= 8 * pixelShift;
				iwidth -= phrasesToSkip;
				pixCount = pixelShift;
			}
		}
	}
	else if (depth == 4)
	{
		int32_t lbufDelta = ((int8_t)((flags << 7) & 0xFF) >> 5) | 0x02;

		int pixCount = 0;
		uint64_t pixels = ((uint64_t)JaguarReadLong(data, OP) << 32) | JaguarReadLong(data + 4, OP);

		while ((int32_t)iwidth > 0)
		{
			uint8_t bitsHi = pixels >> 56, bitsLo = pixels >> 48;

			if (flagTRANS && ((bitsLo | bitsHi) == 0))
				;
			else
			{
				if (!flagRMW)
					*currentLineBuffer = bitsHi,
					*(currentLineBuffer + 1) = bitsLo;
				else
					*currentLineBuffer =
						BLEND_CR(*currentLineBuffer, bitsHi),
					*(currentLineBuffer + 1) =
						BLEND_Y(*(currentLineBuffer + 1), bitsLo);
			}

			currentLineBuffer += lbufDelta;

			while (horizontalRemainder < 0x20)
			{
				horizontalRemainder += hscale;
				pixCount++;
				pixels <<= 16;
			}
			horizontalRemainder -= 0x20;

			if (pixCount > 3)
			{
				int phrasesToSkip = pixCount / 4, pixelShift = pixCount % 4;

				data += (pitch << 3) * phrasesToSkip;
				pixels = ((uint64_t)JaguarReadLong(data, OP) << 32) | JaguarReadLong(data + 4, OP);
				pixels <<= 16 * pixelShift;

				iwidth -= phrasesToSkip;

				pixCount = pixelShift;
			}
		}
	}
	else if (depth == 5)
	{
		int32_t lbufDelta = ((int8_t)((flags << 7) & 0xFF) >> 4) | 0x04;

		while (iwidth--)
		{
			uint64_t pixels = ((uint64_t)JaguarReadLong(data, OP) << 32) | JaguarReadLong(data + 4, OP);
			data += pitch << 3;

			for(int i=0; i<2; i++)
			{
				uint8_t bits3 = pixels >> 56, bits2 = pixels >> 48,
					bits1 = pixels >> 40, bits0 = pixels >> 32;

				if (flagTRANS && (bits3 | bits2 | bits1 | bits0) == 0)
					;
				else
					*currentLineBuffer = bits3,
					*(currentLineBuffer + 1) = bits2,
					*(currentLineBuffer + 2) = bits1,
					*(currentLineBuffer + 3) = bits0;

				currentLineBuffer += lbufDelta;
				pixels <<= 32;
			}
		}
	}
}
