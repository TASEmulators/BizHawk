//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2014 Avery Lee
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

#ifndef f_AT_ATCORE_DEVICEVIDEO_H
#define f_AT_ATCORE_DEVICEVIDEO_H

#include <vd2/system/unknown.h>
#include <vd2/system/vectors.h>

struct VDPixmap;

struct ATDeviceVideoInfo {
	// True if the device is producing a valid signal. This is used to signal
	// if the device is programmed with valid scanning parameters or junk.
	bool mbSignalValid;

	// Horizontal and vertical scan rates, in Hz.
	float mHorizScanRate;
	float mVertScanRate;

	// Counter that increments every time the memory address or layout of
	// the frame buffer changes.
	uint32 mFrameBufferLayoutChangeCount;

	// Counter that increments every time the contents of the frame buffer
	// change.
	uint32 mFrameBufferChangeCount;

	// Size of the text screen in characters.
	int mTextRows;
	int mTextColumns;

	// Physical aspect ratio of a pixel, as width/height.
	double mPixelAspectRatio;

	// Portion of the frame buffer intended for display. This can be smaller
	// than the pixmap supplied.
	vdrect32 mDisplayArea;

	// #RRGGBB border color.
	uint32 mBorderColor;
};

class IATDeviceVideoOutput {
public:
	enum { kTypeID = 'advo' };

	// Advance by the given number of 300Hz ticks. 300Hz allows for rough
	// matching between PAL and NTSC rates.
	virtual void Tick(uint32 hz300ticks) = 0;

	// Update framebuffer with any pending changes.
	virtual void UpdateFrame() = 0;

	// Retrieve the current frame buffer. This is valid until the frame buffer
	// layout change count changes (which must be checked before this is used
	// each time).
	virtual const VDPixmap& GetFrameBuffer() = 0;

	virtual const ATDeviceVideoInfo& GetVideoInfo() = 0;

	// Convert a pixel position in the frame buffer to a caret position. Caret
	// positions are immediately to the left of each character cell. The result
	// is clamped to [0,W] horizontally and [0,H-1] vertically for a WxH text
	// screen in reading order; points above the screen return (0,0) and points
	// below it return (W,H-1).
	virtual vdpoint32 PixelToCaretPos(const vdpoint32& pixelPos) = 0;

	// Convert a character rect to a pixel rectangle in the frame buffer.
	// (0,0) is the top left.
	virtual vdrect32 CharToPixelRect(const vdrect32& r) = 0;

	// Read ATASCII characters from the text screen, starting at (x,y) and
	// extending up to n characters, within the same line. Returns the number
	// of characters read.
	virtual int ReadRawText(uint8 *dst, int x, int y, int n) = 0;

	// Returns a count that is incremented whenever any activity is seen
	// on the attached device, even if no frame buffer changes occur. This
	// is used to determine when the associated screen is "active."
	virtual uint32 GetActivityCounter() = 0;
};

#endif
