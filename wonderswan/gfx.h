#ifndef __WSWAN_GFX_H
#define __WSWAN_GFX_H

#include "system.h"

namespace MDFN_IEN_WSWAN
{

class GFX
{
public:
	GFX();

	// TCACHE ====================================
	void InvalidByAddr(uint32);
	void SetVideo(int, bool);
	void MakeTiles();
	void GetTile(uint32 number,uint32 line,int flipv,int fliph,int bank);
	// TCACHE/====================================
	void Scanline(uint32 *target);
	void SetPixelFormat();

	void Init(bool color);
	void Reset();
	void Write(uint32 A, uint8 V);
	uint8 Read(uint32 A);
	void PaletteRAMWrite(uint32 ws_offset, uint8 data);

	bool ExecuteLine(uint32 *surface, bool skip);

	void SetLayerEnableMask(uint32 mask);
	void SetBWPalette(const uint32 *colors);
	void SetColorPalette(const uint32 *colors);

private:
	// TCACHE ====================================
	uint8	tiles[256][256][2][8];
	uint8	wsTCache[512*64];			
	uint8	wsTCache2[512*64];			
	uint8	wsTCacheFlipped[512*64];
	uint8	wsTCacheFlipped2[512*64];
	uint8	wsTCacheUpdate[512];		
	uint8	wsTCacheUpdate2[512];		  
	uint8	wsTileRow[8];
	// TCACHE/====================================
	int		wsVMode;

	uint32 wsMonoPal[16][4];
	uint32 wsColors[8];
	uint32 wsCols[16][16];

	uint32 ColorMapG[16];
	uint32 ColorMap[16*16*16];
	uint32 LayerEnabled;

	uint8 wsLine;                 /*current scanline*/

	uint8 SpriteTable[0x80][4];
	uint32 SpriteCountCache;
	uint8 DispControl;
	uint8 BGColor;
	uint8 LineCompare;
	uint8 SPRBase;
	uint8 SpriteStart, SpriteCount;
	uint8 FGBGLoc;
	uint8 FGx0, FGy0, FGx1, FGy1;
	uint8 SPRx0, SPRy0, SPRx1, SPRy1;

	uint8 BGXScroll, BGYScroll;
	uint8 FGXScroll, FGYScroll;
	uint8 LCDControl, LCDIcons;

	uint8 BTimerControl;
	uint16 HBTimerPeriod;
	uint16 VBTimerPeriod;

	uint16 HBCounter, VBCounter;
	uint8 VideoMode;

	bool wsc; // mono / color

public:
	System *sys;
	template<bool isReader>void SyncState(NewState *ns);
};

}

#endif
