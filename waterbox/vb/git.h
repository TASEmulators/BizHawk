#pragma once

struct MDFN_Surface
{
	uint32 *pixels;
	int pitch32;
};

struct MDFN_Rect
{
	int x, y, w, h;
};

struct EmulateSpecStruct
{
	// Pitch(32-bit) must be equal to width and >= the "fb_width" specified in the MDFNGI struct for the emulated system.
	// Height must be >= to the "fb_height" specified in the MDFNGI struct for the emulated system.
	// The framebuffer pointed to by surface->pixels is written to by the system emulation code.
	uint32 *pixels;

	// Pointer to sound buffer, set by the driver code, that the emulation code should render sound to.
	// Guaranteed to be at least 500ms in length, but emulation code really shouldn't exceed 40ms or so.  Additionally, if emulation code
	// generates >= 100ms,
	// DEPRECATED: Emulation code may set this pointer to a sound buffer internal to the emulation module.
	int16 *SoundBuf;

	// Number of cycles that this frame consumed, using MDFNGI::MasterClock as a time base.
	// Set by emulation code.
	int64 MasterCycles;

	// Set by the system emulation code every frame, to denote the horizontal and vertical offsets of the image, and the size
	// of the image.  If the emulated system sets the elements of LineWidths, then the width(w) of this structure
	// is ignored while drawing the image.
	MDFN_Rect DisplayRect;

	// Maximum size of the sound buffer, in frames.  Set by the driver code.
	int32 SoundBufMaxSize;

	// Number of frames currently in internal sound buffer.  Set by the system emulation code, to be read by the driver code.
	int32 SoundBufSize;

	// 0 UDLR SelectStartBA UDLR(right dpad) LtrigRtrig 13
	int32 Buttons;

	// set by core, true if lagged
	int32 Lagged;
};

/*typedef struct
{

 void (*Emulate)(EmulateSpecStruct *espec);
 void (*TransformInput)(void);	// Called before Emulate, and within MDFN_MidSync(), to implement stuff like setting-controlled PC Engine SEL+RUN button exclusion in a way
				// that won't cause desyncs with movies and netplay.

 void (*SetInput)(unsigned port, const char *type, uint8* data);
 bool (*SetMedia)(uint32 drive_idx, uint32 state_idx, uint32 media_idx, uint32 orientation_idx);


 // Called when netplay starts, or the controllers controlled by local players changes during
 // an existing netplay session.  Called with ~(uint64)0 when netplay ends.
 // (For future use in implementing portable console netplay)
 void (*NPControlNotif)(uint64 c);

 const MDFNSetting *Settings;

 // Time base for EmulateSpecStruct::MasterCycles
 // MasterClock must be >= MDFN_MASTERCLOCK_FIXED(1.0)
 // All or part of the fractional component may be ignored in some timekeeping operations in the emulator to prevent integer overflow,
 // so it is unwise to have a fractional component when the integral component is very small(less than say, 10000).
 #define MDFN_MASTERCLOCK_FIXED(n)	((int64)((double)(n) * (1LL << 32)))
 int64 MasterClock;

 // Nominal frames per second * 65536 * 256, truncated.
 // May be deprecated in the future due to many systems having slight frame rate programmability.
 uint32 fps;

 // multires is a hint that, if set, indicates that the system has fairly programmable video modes(particularly, the ability
 // to display multiple horizontal resolutions, such as the PCE, PC-FX, or Genesis).  In practice, it will cause the driver
 // code to set the linear interpolation on by default.
 //
 // lcm_width and lcm_height are the least common multiples of all possible
 // resolutions in the frame buffer as specified by DisplayRect/LineWidths(Ex for PCE: widths of 256, 341.333333, 512,
 // lcm = 1024)
 //
 // nominal_width and nominal_height specify the resolution that Mednafen should display
 // the framebuffer image in at 1x scaling, scaled from the dimensions of DisplayRect, and optionally the LineWidths array
 // passed through espec to the Emulate() function.
 //
 bool multires;

 int lcm_width;
 int lcm_height;


 int nominal_width;
 int nominal_height;

 int fb_width;		// Width of the framebuffer(not necessarily width of the image).  MDFN_Surface width should be >= this.
 int fb_height;		// Height of the framebuffer passed to the Emulate() function(not necessarily height of the image)

 int soundchan; 	// Number of output sound channels.  Only values of 1 and 2 are currently supported.


 int rotated;

 std::string name;    // Game name, UTF-8 encoding 
 uint8 MD5[16];
 uint8 GameSetMD5[16];	// A unique ID for the game set this CD belongs to, only used in PC-FX emulation. 
 bool GameSetMD5Valid; // True if GameSetMD5 is valid. 

 VideoSystems VideoSystem;
 GameMediumTypes GameType;	// Deprecated.

 RMD_Layout* RMD;

 const char *cspecial;  // Special cart expansion: DIP switches, barcode reader, etc. 

 std::vector<const char *>DesiredInput; // Desired input device for the input ports, NULL for don't care

 // For mouse relative motion.
 double mouse_sensitivity;


 //
 // For absolute coordinates(IDIT_X_AXIS and IDIT_Y_AXIS), usually mapped to a mouse(hence the naming).
 //
 float mouse_scale_x, mouse_scale_y;
 float mouse_offs_x, mouse_offs_y; 
} MDFNGI;*/
