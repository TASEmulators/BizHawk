#ifndef f_AT_XEP80_H
#define f_AT_XEP80_H

#include <vd2/system/vectors.h>
#include <vd2/Kasumi/pixmap.h>
#include <vd2/Kasumi/pixmaputils.h>
#include <at/atcore/scheduler.h>

class ATPIAEmulator;

struct ATXEP80TextDisplayInfo {
	int mColumns;
	int mRows;
};

class ATXEP80Emulator : public IATSchedulerCallback {
	ATXEP80Emulator(const ATXEP80Emulator&);
	ATXEP80Emulator& operator=(const ATXEP80Emulator&);
public:
	ATXEP80Emulator();
	~ATXEP80Emulator();

	void Init(ATScheduler *sched, IATDevicePortManager *pia);
	void Shutdown();

	void ColdReset();

	void SoftReset();

	void InitFonts();

	bool IsVideoSignalValid() const { return mbValidSignal; }
	float GetVideoHorzRate() const { return mHorzRate; }
	float GetVideoVertRate() const { return mVertRate; }
	const VDPixmap& GetFrameBuffer() const { return mFrame; }

	void Tick(uint32 ticks300Hz);
	void UpdateFrame();

	uint32 GetFrameLayoutChangeCount();
	uint32 GetFrameChangeCount() const;
	const vdrect32 GetDisplayArea() const;
	double GetPixelAspectRatio() const;

	uint32 GetDataReceivedCount();

	const ATXEP80TextDisplayInfo GetTextDisplayInfo() const;
	const vdpoint32 PixelToCaretPos(const vdpoint32& pixelPos) const;
	const vdrect32 CharToPixelRect(const vdrect32& r) const;
	int ReadRawText(uint8 *dst, int x, int y, int n) const;

	static void StaticOnPIAOutputChanged(void *data, uint32 outputState);
	void OnPIAOutputChanged(uint32 outputState);

public:
	virtual void OnScheduledEvent(uint32 id);

private:
	struct CommandInfo;
	
	static const CommandInfo *LookupCommand(uint8 ch);

	void OnReceiveByte(uint32 ch);
	void SendCursor(uint8 offset);
	void BeginWrite(uint8 len);

	void OnChar(uint8);
	void OnCmdSetCursorHPos(uint8);
	void OnCmdSetCursorHPosHi(uint8);
	void OnCmdSetLeftMarginLo(uint8);
	void OnCmdSetLeftMarginHi(uint8);
	void OnCmdSetCursorVPos(uint8);
	void OnCmdSetGraphics(uint8);
	void OnCmdModifyGraphics50Hz(uint8);
	void OnCmdSetRightMarginLo(uint8);
	void OnCmdSetRightMarginHi(uint8);
	void OnCmdReadCharAndAdvance(uint8);
	void OnCmdRequestCursorHPos(uint8);
	void OnCmdMasterReset(uint8);
	void OnCmdPrinterPortStatus(uint8);
	void OnCmdFillPrevChar(uint8);
	void OnCmdFillSpace(uint8);
	void OnCmdFillEOL(uint8);
	void OnCmdReadChar(uint8);
	void OnCmdReadTimerCounter(uint8);
	void OnCmdClearListFlag(uint8);
	void OnCmdSetListFlag(uint8);
	void OnCmdSetNormalMode(uint8);
	void OnCmdSetBurstMode(uint8);
	void OnCmdSetCharSet(uint8);
	void OnCmdSetText50Hz(uint8);
	void OnCmdCursorOff(uint8);
	void OnCmdCursorOn(uint8);
	void OnCmdCursorOnBlink(uint8);
	void OnCmdMoveToLogicalStart(uint8);
	void OnCmdSetScrollX(uint8);
	void OnCmdSetPrinterOutput(uint8);
	void OnCmdSetReverseVideo(uint8);
	void OnCmdSetExtraByte(uint8);
	void OnCmdWriteCursor(uint8);
	void OnCmdSetCursorAddr(uint8);
	void OnCmdWriteByte(uint8);
	void OnCmdWriteInternalByte(uint8);
	void OnCmdSetHomeAddr(uint8);
	void OnCmdWriteVCR(uint8);
	void OnCmdSetTCP(uint8);
	void OnCmdWriteTCP(uint8);
	void OnCmdSetBeginAddr(uint8);
	void OnCmdSetEndAddr(uint8);
	void OnCmdSetStatusAddr(uint8);
	void OnCmdSetAttrLatch(uint8 ch);
	void OnCmdSetBaudRate(uint8);
	void OnCmdSetUMX(uint8);

	void Clear();
	void ClearLine(int y);
	void InsertChar();
	void DeleteChar();
	void InsertLine();
	void DeleteLine();
	void Advance(bool extendLine);
	void Scroll();
	void UpdateCursorAddr();
	void InvalidateCursor();
	void InvalidateFrame();

	void RebuildBlockGraphics();
	void RebuildActiveFont();
	void RecomputeBaudRate();
	void RecomputeVideoTiming();

	uint8 *GetRowPtr(int y) { return &mVRAM[(mRowPtrs[y] & 0x1F) << 8]; }

	enum CommandState {
		kState_WaitCommand,
		kState_ReturningData
	};

	CommandState mCommandState;
	int mReadBitState;
	int mWriteBitState;
	uint32 mCurrentData;
	uint32 mCurrentWriteData;
	uint16 mWriteBuffer[3];
	uint8 mWriteIndex;
	uint8 mWriteLength;
	uint8 mScrollX;
	uint8 mX;
	uint8 mY;
	uint16 mBeginAddr;			// BEGD register
	uint16 mEndAddr;			// ENDD register
	uint16 mHomeAddr;			// HOME register
	uint16 mCursorAddr;			// CURS register
	uint16 mStatusAddr;			// SROW register
	uint8 mLastX;
	uint8 mLastY;
	uint8 mLastChar;
	uint8 mLeftMargin;
	uint8 mRightMargin;
	uint8 mAttrA;
	uint8 mAttrB;
	uint8 mExtraByte;
	bool mbEscape;
	bool mbDisplayControl;
	bool mbBurstMode;
	bool mbGraphicsMode;
	bool mbInternalCharset;
	bool mbPrinterMode;
	bool mbCursorEnabled;
	bool mbCursorBlinkEnabled;
	bool mbCursorBlinkState;
	bool mbCursorReverseVideo;
	bool mbCharBlinkState;
	bool mbReverseVideo;
	bool mbReverseVideoBlinkField;
	bool mbPAL;
	uint8 mTickAccum;
	uint16 mBlinkAccum;
	uint8 mBlinkRate;
	uint8 mBlinkDutyCycle;
	uint8 mUnderlineStart;
	uint8 mUnderlineEnd;

	uint8 mUARTPrescale;
	uint8 mUARTBaud;
	uint8 mUARTMultiplex;
	uint32 mCyclesPerBitXmit;
	uint32 mCyclesPerBitRecv;

	uint8 mCharWidth;
	uint8 mCharHeight;
	uint8 mGfxColumns;
	uint8 mGfxRowMid;
	uint8 mGfxRowBot;
	uint8 mTCP;

	uint8 mHorzCount;
	uint8 mHorzBlankStart;
	uint8 mHorzSyncStart;
	uint8 mHorzSyncEnd;
	uint8 mVertCount;
	uint8 mVertBlankStart;
	uint8 mVertSyncBegin;
	uint8 mVertSyncEnd;
	uint8 mVertStatusRow;		// Timing chain register 8. Note that this is the last row before the status row.
	uint8 mVertExtraScans;
	float mHorzRate;
	float mVertRate;
	bool mbValidSignal;

	bool mbInvalidBlockGraphics;
	bool mbInvalidActiveFont;
	
	IATDevicePortManager *mpPIA;
	int mPIAInput;
	int mPIAOutput;

	ATScheduler *mpScheduler;
	ATEvent *mpReadBitEvent;
	ATEvent *mpWriteBitEvent;

	uint32 mFrameLayoutChangeCount;
	uint32 mFrameChangeCount;
	uint32 mDataReceivedCount;

	VDPixmapBuffer mFrame;

	uint8 mRowPtrs[25];

	uint8 mVRAM[8192];

	// Fonts by character, for three different modes:
	//
	//  A14=0, A13=0: Normal character set
	//  A14=0, A13=1: International character set
	//  A14=1:        Internal character set
	//
	// We pregen all of these at once because it is possible to
	// mix them by writing directly to row addresses.
	//
	// Within each character set, there are three subarrays, one for normal
	// mode, and another two for left and right halves of double width.
	//
	// The subarrays are in turn indexed by VRAM byte and then line (0-15).
	// Each 128 character half is generated according to the current attribute
	// latches.
	//
	uint16 mActiveFonts[3][3*256*16];

	// Source fonts: normal, int'l, internal, and block. The block graphics font
	// is dynamically updated based on graphics settings in the timing chain.
	uint16 mFonts[4][256*16];
	uint32 mPalette[256];

	static const CommandInfo kCommands[];
};

#endif
