/*
*   Glide64 - Glide video plugin for Nintendo 64 emulators.
*   Copyright (c) 2002  Dave2001
*
*   This program is free software; you can redistribute it and/or modify
*   it under the terms of the GNU General Public License as published by
*   the Free Software Foundation; either version 2 of the License, or
*   any later version.
*
*   This program is distributed in the hope that it will be useful,
*   but WITHOUT ANY WARRANTY; without even the implied warranty of
*   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
*   GNU General Public License for more details.
*
*   You should have received a copy of the GNU General Public
*   Licence along with this program; if not, write to the Free
*   Software Foundation, Inc., 51 Franklin Street, Fifth Floor, 
*   Boston, MA  02110-1301, USA
*/

//****************************************************************
//
// Glide64 - Glide Plugin for Nintendo 64 emulators (tested mostly with Project64)
// Project started on December 29th, 2001
//
// To modify Glide64:
// * Write your name and (optional)email, commented by your work, so I know who did it, and so that you can find which parts you modified when it comes time to send it to me.
// * Do NOT send me the whole project or file that you modified.  Take out your modified code sections, and tell me where to put them.  If people sent the whole thing, I would have many different versions, but no idea how to combine them all.
//
// Official Glide64 development channel: #Glide64 on EFnet
//
// Original author: Dave2001 (Dave2999@hotmail.com)
// Other authors: Gonetz, Gugaman
//
//****************************************************************

// ** NOTE: this file has been modified from it's original version, which can be
//    downloaded along with Project64 **

/**********************************************************************************
Common gfx plugin spec, version #1.3 maintained by zilmar (zilmar@emulation64.com)

All questions or suggestions should go through the mailing list.
http://www.egroups.com/group/Plugin64-Dev
***********************************************************************************

Notes:
------

Setting the approprate bits in the MI_INTR_REG and calling CheckInterrupts which 
are both passed to the DLL in InitiateGFX will generate an Interrupt from with in 
the plugin.

The Setting of the RSP flags and generating an SP interrupt  should not be done in
the plugin

**********************************************************************************/

// THIS FILE IS A PRECOMPILED HEADER TO DECREASE BUILD TIME.  INCLUDE ALL STANDARD
//  .H FILES HERE

#ifndef _GFX_H_INCLUDED__
#define _GFX_H_INCLUDED__
#include "m64p.h"

#if defined(WIN32) && defined(GCC)
// ZIGGY ARRRRGG what a pain in the #$@$
//# include "Gfx #1.3-mangling.h"
#ifdef GCC
#include <stdio.h>
//#define printf(...)
#endif
#endif

#define _WIN32_WINNT 0x0400

#ifdef _WIN32
#include <windows.h>
#else // _WIN32
#include "winlnxdefs.h"
#endif // _WIN32
#include <stdio.h>

#include <ostream>
#include <fstream>

#include <cstddef>      // offsetof
#ifndef _WIN32
#include <cmath>
#endif
#ifdef _WIN32
#include <io.h>
#include <direct.h>
#include <mmsystem.h>
#include <commctrl.h>
#endif // _WIN32
#include <time.h>
#include <glide.h>
#include <g3ext.h>
#include "rdp.h"

#if defined(__cplusplus)
extern "C" {
#endif

// tooltips have an error on debug mode, this can be overcome by clicking ignore,
//  but I'd rather just disable tooltips altogether since nobody else should
//  have the debug version anyway.
#ifndef _DEBUG
//#ifndef __INTEL_COMPILER  // tooltips don't work with intel compiler
#define USE_TOOLTIPS
//#endif
#endif

#ifndef VPDEBUG
# define _FINAL_RELEASE_
#endif
// #ifdef WIN32
// # define _FINAL_RELEASE_
// #else
//   //# define _FINAL_RELEASE_
// #endif

//********
// Logging

// ********************************
// ** TAKE OUT BEFORE RELEASE!!! **
//#define LOGGING           // log of spec functions called
//#define LOG_KEY           // says "Key!!!" in the log when space bar is pressed

//#define LOG_UCODE

//#define ALTTAB_FIX
    
//#define EXTREME_LOGGING       // lots of logging
                            //  note that some of these things are inserted/removed
                            //  from within the code & may not be changed by this define.

//#define TLUT_LOGGING      // log every entry of the TLUT?
// ********************************

#define FPS                 // fps counter able? (not enabled necessarily)

#define LOGNOTKEY           // Log if not pressing:
#ifdef WIN32
#define LOGKEY      VK_LCONTROL     // this key
#else
#include <SDL.h>
#define LOGKEY KMOD_LCTRL
inline int GetAsyncKeyState(int key) {
  return (SDL_GetModState() & key) != 0;
}
#endif

#define LOG_COMMANDS        // log the whole 64-bit command as (0x........, 0x........)

#define CATCH_EXCEPTIONS    // catch exceptions so it doesn't freeze and will report
                            // "The gfx plugin has caused an exception" instead.

#define FLUSH               // flush the file buffer. slower logging, but makes sure
                            //  the command is logged before continuing (in case of
                            //  crash or exception, the log will not be cut short)
#ifndef _FINAL_RELEASE_
#define RDP_LOGGING         // Allow logging (will not log unless checked, but allows the option)
                            //  Logging functions will not be compiled if this is not present.
//#define RDP_ERROR_LOG
#endif

#define FPS_FRAMES  10      // Number of frames in which to make an FPS count

//#define SHOW_FULL_TEXVIEWER   // shows the entire contents of the texture in the cache viewer,
                                // usually used to debug clamping issues.


// Usually enabled
#define LARGE_TEXTURE_HANDLING  // allow large-textured objects to be split?

#ifdef ALTTAB_FIX
extern HHOOK hhkLowLevelKybd;
extern LRESULT CALLBACK LowLevelKeyboardProc(int nCode, 
   WPARAM wParam, LPARAM lParam);
#endif

// Simulations
//#define SIMULATE_VOODOO1
//#define SIMULATE_BANSHEE
//********

#ifdef EXT_LOGGING
extern std::ofstream extlog;
#define EXT(x) extlog.open("ext.txt",ios::app); extlog << x; extlog.close();
#else
#define EXT(x)
#endif

#ifndef _FINAL_RELEASE_
#define UNIMP_LOG           // Keep enabled, option in dialog
#define BRIGHT_RED          // Keep enabled, option in dialog
#endif

#define COLORED_DEBUGGER    // ;) pretty colors

#ifdef FPS
extern LARGE_INTEGER perf_freq;
extern LARGE_INTEGER fps_last;
extern LARGE_INTEGER fps_next;
extern float            fps;
extern DWORD            fps_count;
#endif

// rdram mask at 0x400000 bytes (bah, not right for majora's mask)
//#define BMASK 0x7FFFFF
extern unsigned long BMASK;
#define WMASK   0x3FFFFF
#define DMASK   0x1FFFFF

extern int num_tmu;
extern int max_tex_size;
extern long sup_mirroring;
extern BOOL sup_32bit_tex;
extern DWORD update_screen_count;

extern DWORD resolutions[0x18][2];

//#define PERFORMANCE
#ifdef PERFORMANCE
extern __int64 perf_cur;
extern __int64 perf_next;
#endif

#ifdef LOGGING
extern std::ofstream loga;
#define LOG(x) loga.open("log.txt",ios::app); loga << x; loga.flush(); loga.close();
#else
#define LOG(x) WriteLog(M64MSG_VERBOSE, "%s", x);
#endif


#ifdef RDP_LOGGING
extern int dumping;
extern BOOL log_open;
extern std::ofstream rdp_log;
//#define rdp_log std::cerr;
#define OPEN_RDP_LOG() EXT("OPEN_RDP_LOG ()\n"); if (settings.logging && !log_open) { /*rdp_log.open ("rdp.txt");*/ log_open=TRUE; }
#define CLOSE_RDP_LOG() EXT("CLOSE_RDP_LOG ()\n"); if (settings.logging && log_open) { /*rdp_log.close ();*/ log_open=FALSE; }

#ifdef LOGNOTKEY
#define RDP(x) EXT("RDP (...)\n"); if (dumping && settings.logging && log_open) { if (!(GetAsyncKeyState(LOGKEY)&0x8000)) { fprintf(stderr, x);/*rdp_log << x; rdp_log.flush();*/ } }
#else
#define RDP(x) EXT("RDP (...)\n"); if (dumping && settings.logging && log_open) { fprintf(stderr, x);/*rdp_log << x; rdp_log.flush();*/ }
#endif

#else
#define OPEN_RDP_LOG()
#define CLOSE_RDP_LOG()
#define RDP(x)
#endif


#ifdef RDP_ERROR_LOG
extern BOOL elog_open;
extern std::ofstream rdp_err;
//#define rdp_err std::cerr
#define OPEN_RDP_E_LOG() EXT("OPEN_RDP_E_LOG ()\n"); if (settings.elogging && !elog_open) { /*rdp_err.open ("rdp_e.txt");*/ elog_open=TRUE; }
#define CLOSE_RDP_E_LOG() EXT("CLOSE_RDP_LOG ()\n"); if (settings.elogging && elog_open) { /*rdp_err.close (); */elog_open=FALSE; }
#define RDP_E(x) if (dumping && settings.elogging) { FRDP_E (x); }
#else
#define OPEN_RDP_E_LOG()
#define CLOSE_RDP_E_LOG()
#define RDP_E(x)
#endif


#ifdef RDP_LOGGING
__inline void FRDP (const char *fmt, ...)
{
#ifdef RDP_LOGGING
    if (!dumping || !settings.logging || !log_open) return;

#ifdef LOGNOTKEY
    if (GetAsyncKeyState(LOGKEY)&0x8000) return;
#endif

    va_list ap;
    va_start(ap, fmt);
    vsprintf(out_buf, fmt, ap);
    RDP (out_buf);
    va_end(ap);
#endif
}
__inline void FRDP_E (const char *fmt, ...)
{
#ifdef RDP_ERROR_LOG
    if (!dumping || !settings.elogging || !elog_open) return;

#ifdef LOGNOTKEY
    if (GetAsyncKeyState(LOGKEY)&0x8000) return;
#endif

    sprintf (out_buf, "%08x: (%08x, %08x) ", rdp.pc[rdp.pc_i]-8, rdp.cmd0, rdp.cmd1);
    rdp_err << out_buf;

    va_list ap2;
    va_start(ap2, fmt);
    vsprintf(out_buf, fmt, ap2);
//  rdp_err << out_buf;
//  rdp_err.flush();
  fprintf(stderr, out_buf);
    va_end(ap2);
#endif
}

#else
#ifndef GCC
#define FRDP(...)
#define FRDP_E(...)
#else // _WIN32
inline void FRDP (const char *fmt, ...) {}
inline void FRDP_E (const char *fmt, ...) {}
#endif // _WIN32
#endif


extern BOOL fullscreen;
extern BOOL romopen;
extern BOOL to_fullscreen;
extern BOOL debugging;
extern HINSTANCE hInstance;

extern BOOL evoodoo;
extern BOOL ev_fullscreen;

extern BOOL exception;

extern PROPSHEETHEADER  m_PropSheet;
extern PROPSHEETPAGE m_psp[3];


BOOL InitGfx (BOOL);
void ReleaseGfx ();
void DrawFrameBuffer ();

// The highest 8 bits are the segment # (1-16), and the lower 24 bits are the offset to
// add to it.
__inline DWORD segoffset (DWORD so)
{
    return (rdp.segment[(so>>24)&0x0f] + (so&BMASK))&BMASK;
}

/* Plugin types */
#define PLUGIN_TYPE_GFX             2

//TODO: remove
//#define EXPORT                      __declspec(dllexport)
//#define CALL                        _cdecl

/***** Structures *****/
typedef struct {
    WORD Version;        /* Set to 0x0103 */
    WORD Type;           /* Set to PLUGIN_TYPE_GFX */
    char Name[100];      /* Name of the DLL */

    /* If DLL supports memory these memory options then set them to TRUE or FALSE
       if it does not support it */
    BOOL NormalMemory;    /* a normal BYTE array */ 
    BOOL MemoryBswaped;  /* a normal BYTE array where the memory has been pre
                              bswap on a dword (32 bits) boundry */
} PLUGIN_INFO;

#if 0
//TODO: remove

typedef struct {
    HWND hWnd;          /* Render window */
    HWND hStatusBar;    /* if render window does not have a status bar then this is NULL */

    BOOL MemoryBswaped;    // If this is set to TRUE, then the memory has been pre
                           //   bswap on a dword (32 bits) boundry 
                           //   eg. the first 8 bytes are stored like this:
                           //        4 3 2 1   8 7 6 5

    BYTE * HEADER;  // This is the rom header (first 40h bytes of the rom
                    // This will be in the same memory format as the rest of the memory.
    BYTE * RDRAM;
    BYTE * DMEM;
    BYTE * IMEM;

    DWORD * MI_INTR_REG;

    DWORD * DPC_START_REG;
    DWORD * DPC_END_REG;
    DWORD * DPC_CURRENT_REG;
    DWORD * DPC_STATUS_REG;
    DWORD * DPC_CLOCK_REG;
    DWORD * DPC_BUFBUSY_REG;
    DWORD * DPC_PIPEBUSY_REG;
    DWORD * DPC_TMEM_REG;

    DWORD * VI_STATUS_REG;
    DWORD * VI_ORIGIN_REG;
    DWORD * VI_WIDTH_REG;
    DWORD * VI_INTR_REG;
    DWORD * VI_V_CURRENT_LINE_REG;
    DWORD * VI_TIMING_REG;
    DWORD * VI_V_SYNC_REG;
    DWORD * VI_H_SYNC_REG;
    DWORD * VI_LEAP_REG;
    DWORD * VI_H_START_REG;
    DWORD * VI_V_START_REG;
    DWORD * VI_V_BURST_REG;
    DWORD * VI_X_SCALE_REG;
    DWORD * VI_Y_SCALE_REG;

    void (*CheckInterrupts)( void );
} GFX_INFO;
#endif

extern GFX_INFO gfx;
extern BOOL no_dlist;

typedef GrContext_t (FX_CALL *GRWINOPENEXT)( FxU32                   hWnd,
                                             GrScreenResolution_t    resolution, 
                                             GrScreenRefresh_t       refresh, 
                                             GrColorFormat_t         format, 
                                             GrOriginLocation_t      origin, 
                                             GrPixelFormat_t         pixelformat,
                                             int                     nColBuffers,
                                             int                     nAuxBuffers) ; 

typedef void (FX_CALL *GRTEXBUFFEREXT)( GrChipID_t          tmu, 
                                        FxU32               startAddress, 
                                        GrLOD_t             lodmin, 
                                        GrLOD_t             lodmax, 
                                        GrAspectRatio_t     aspect, 
                                        GrTextureFormat_t   fmt, 
                                        FxU32               evenOdd) ; 

typedef void (FX_CALL *GRAUXBUFFEREXT)( GrBuffer_t buffer ) ;

typedef void (FX_CALL *GRCOLORCOMBINEEXT) (GrCCUColor_t     a,
                                        GrCombineMode_t  a_mode,
                                        GrCCUColor_t     b,
                                        GrCombineMode_t  b_mode,
                                        GrCCUColor_t     c,
                                        FxBool           c_invert,
                                        GrCCUColor_t     d,
                                        FxBool           d_invert,
                                        FxU32            shift,
                                        FxBool           invert) ;

typedef void (FX_CALL *GRTEXCOLORCOMBINEEXT) (GrChipID_t       tmu,
                                           GrTCCUColor_t    a,
                                           GrCombineMode_t  a_mode,
                                           GrTCCUColor_t    b,
                                           GrCombineMode_t  b_mode,
                                           GrTCCUColor_t    c,
                                           FxBool           c_invert,
                                           GrTCCUColor_t    d,
                                           FxBool           d_invert,
                                           FxU32            shift,
                                           FxBool           invert);

typedef void (FX_CALL *GRCONSTANTCOLORVALUEEXT)
          (GrChipID_t       tmu,
           GrColor_t        value);

typedef void (FX_CALL *GRSTIPPLE)( FxI32 mode) ; 

typedef void (FX_CALL *GRCONFIGWRAPPEREXT)(HINSTANCE instance, HWND hwnd);

typedef GrScreenResolution_t (FX_CALL *GRWRAPPERFULLSCREENRESOLUTIONEXT)();

// ZIGGY framebuffer copy extension
// allow to copy the depth or color buffer from back/front to front/back
// (GL has separate back and front depth buffer, unlike glide, so this extension
//  makes sense only in a wrapper)
#define GR_FBCOPY_MODE_DEPTH 0
#define GR_FBCOPY_MODE_COLOR 1
#define GR_FBCOPY_BUFFER_BACK 0
#define GR_FBCOPY_BUFFER_FRONT 1
typedef void (FX_CALL *GRFRAMEBUFFERCOPYEXT)(int x, int y, int w, int h,
                                             int buffer_from, int buffer_to, int mode);

extern GRFRAMEBUFFERCOPYEXT grFramebufferCopyExt;

extern GRTEXBUFFEREXT       grTextureBufferExt;
extern GRTEXBUFFEREXT       grTextureAuxBufferExt;
extern GRAUXBUFFEREXT       grAuxBufferExt;
extern GRSTIPPLE            grStippleModeExt;
extern GRSTIPPLE            grStipplePatternExt;

#ifndef GR_STIPPLE_DISABLE
#define GR_STIPPLE_DISABLE  0x0
#define GR_STIPPLE_PATTERN  0x1
#define GR_STIPPLE_ROTATE   0x2
#endif

void ReadSettings ();
void ReadSpecialSettings (const char name[21]);
void WriteSettings ();

#if 0
//TODO: remove
/******************************************************************
  Function: CaptureScreen
  Purpose:  This function dumps the current frame to a file
  input:    pointer to the directory to save the file to
  output:   none
*******************************************************************/ 
EXPORT void CALL CaptureScreen ( char * Directory );

/******************************************************************
  Function: ChangeWindow
  Purpose:  to change the window between fullscreen and window 
            mode. If the window was in fullscreen this should 
            change the screen to window mode and vice vesa.
  input:    none
  output:   none
*******************************************************************/ 
EXPORT void CALL ChangeWindow (void);

/******************************************************************
  Function: CloseDLL
  Purpose:  This function is called when the emulator is closing
            down allowing the dll to de-initialise.
  input:    none
  output:   none
*******************************************************************/ 
EXPORT void CALL CloseDLL (void);

/******************************************************************
  Function: DllAbout
  Purpose:  This function is optional function that is provided
            to give further information about the DLL.
  input:    a handle to the window that calls this function
  output:   none
*******************************************************************/ 
EXPORT void CALL DllAbout ( HWND hParent );

/******************************************************************
  Function: DllConfig
  Purpose:  This function is optional function that is provided
            to allow the user to configure the dll
  input:    a handle to the window that calls this function
  output:   none
*******************************************************************/ 
EXPORT void CALL DllConfig ( HWND hParent );

/******************************************************************
  Function: DllTest
  Purpose:  This function is optional function that is provided
            to allow the user to test the dll
  input:    a handle to the window that calls this function
  output:   none
*******************************************************************/ 
EXPORT void CALL DllTest ( HWND hParent );


EXPORT void CALL ReadScreen(void **dest, int *width, int *height);

/******************************************************************
  Function: DrawScreen
  Purpose:  This function is called when the emulator receives a
            WM_PAINT message. This allows the gfx to fit in when
            it is being used in the desktop.
  input:    none
  output:   none
*******************************************************************/ 
EXPORT void CALL DrawScreen (void);

/******************************************************************
  Function: GetDllInfo
  Purpose:  This function allows the emulator to gather information
            about the dll by filling in the PluginInfo structure.
  input:    a pointer to a PLUGIN_INFO stucture that needs to be
            filled by the function. (see def above)
  output:   none
*******************************************************************/ 
EXPORT void CALL GetDllInfo ( PLUGIN_INFO * PluginInfo );

/******************************************************************
  Function: InitiateGFX
  Purpose:  This function is called when the DLL is started to give
            information from the emulator that the n64 graphics
            uses. This is not called from the emulation thread.
  Input:    Gfx_Info is passed to this function which is defined
            above.
  Output:   TRUE on success
            FALSE on failure to initialise
             
  ** note on interrupts **:
  To generate an interrupt set the appropriate bit in MI_INTR_REG
  and then call the function CheckInterrupts to tell the emulator
  that there is a waiting interrupt.
*******************************************************************/ 
EXPORT BOOL CALL InitiateGFX (GFX_INFO Gfx_Info);

/******************************************************************
  Function: MoveScreen
  Purpose:  This function is called in response to the emulator
            receiving a WM_MOVE passing the xpos and ypos passed
            from that message.
  input:    xpos - the x-coordinate of the upper-left corner of the
            client area of the window.
            ypos - y-coordinate of the upper-left corner of the
            client area of the window. 
  output:   none
*******************************************************************/ 
EXPORT void CALL MoveScreen (int xpos, int ypos);

/******************************************************************
  Function: ProcessDList
  Purpose:  This function is called when there is a Dlist to be
            processed. (High level GFX list)
  input:    none
  output:   none
*******************************************************************/ 
EXPORT void CALL ProcessDList(void);

/******************************************************************
  Function: ProcessRDPList
  Purpose:  This function is called when there is a Dlist to be
            processed. (Low level GFX list)
  input:    none
  output:   none
*******************************************************************/ 
EXPORT void CALL ProcessRDPList(void);

/******************************************************************
  Function: RomClosed
  Purpose:  This function is called when a rom is closed.
  input:    none
  output:   none
*******************************************************************/ 
EXPORT void CALL RomClosed (void);

/******************************************************************
  Function: RomOpen
  Purpose:  This function is called when a rom is open. (from the 
            emulation thread)
  input:    none
  output:   none
*******************************************************************/ 
EXPORT void CALL RomOpen (void);

/******************************************************************
  Function: ShowCFB
  Purpose:  Useally once Dlists are started being displayed, cfb is
            ignored. This function tells the dll to start displaying
            them again.
  input:    none
  output:   none
*******************************************************************/ 
EXPORT void CALL ShowCFB (void);

/******************************************************************
  Function: UpdateScreen
  Purpose:  This function is called in response to a vsync of the
            screen were the VI bit in MI_INTR_REG has already been
            set
  input:    none
  output:   none
*******************************************************************/ 
EXPORT void CALL UpdateScreen (void);

/******************************************************************
  Function: ViStatusChanged
  Purpose:  This function is called to notify the dll that the
            ViStatus registers value has been changed.
  input:    none
  output:   none
*******************************************************************/ 
EXPORT void CALL ViStatusChanged (void);

/******************************************************************
  Function: ViWidthChanged
  Purpose:  This function is called to notify the dll that the
            ViWidth registers value has been changed.
  input:    none
  output:   none
*******************************************************************/ 
EXPORT void CALL ViWidthChanged (void);


/******************************************************************
  Function: FrameBufferWrite
  Purpose:  This function is called to notify the dll that the
            frame buffer has been modified by CPU at the given address.
  input:    addr        rdram address
            val         val
            size        1 = BYTE, 2 = WORD, 4 = DWORD
  output:   none
*******************************************************************/ 
EXPORT void CALL FBWrite(DWORD, DWORD);

typedef struct
{
    DWORD addr;
    DWORD val;
    DWORD size;             // 1 = BYTE, 2 = WORD, 4=DWORD
} FrameBufferModifyEntry;

/******************************************************************
  Function: FrameBufferWriteList
  Purpose:  This function is called to notify the dll that the
            frame buffer has been modified by CPU at the given address.
  input:    FrameBufferModifyEntry *plist
            size = size of the plist, max = 1024
  output:   none
*******************************************************************/ 
EXPORT void CALL FBWList(FrameBufferModifyEntry *plist, DWORD size);

/******************************************************************
  Function: FrameBufferRead
  Purpose:  This function is called to notify the dll that the
            frame buffer memory is beening read at the given address.
            DLL should copy content from its render buffer to the frame buffer
            in N64 RDRAM
            DLL is responsible to maintain its own frame buffer memory addr list
            DLL should copy 4KB block content back to RDRAM frame buffer.
            Emulator should not call this function again if other memory
            is read within the same 4KB range
  input:    addr        rdram address
            val         val
            size        1 = BYTE, 2 = WORD, 4 = DWORD
  output:   none
*******************************************************************/ 
EXPORT void CALL FBRead(DWORD addr);

/************************************************************************
Function: FBGetFrameBufferInfo
Purpose:  This function is called by the emulator core to retrieve depth
buffer information from the video plugin in order to be able
to notify the video plugin about CPU depth buffer read/write
operations

size:
= 1     byte
= 2     word (16 bit) <-- this is N64 default depth buffer format
= 4     dword (32 bit)

when depth buffer information is not available yet, set all values
in the FrameBufferInfo structure to 0

input:    FrameBufferInfo *pinfo
pinfo is pointed to a FrameBufferInfo structure which to be
filled in by this function
output:   Values are return in the FrameBufferInfo structure
************************************************************************/
EXPORT void CALL FBGetFrameBufferInfo(void *pinfo);

/******************************************************************
   NOTE: THIS HAS BEEN ADDED FOR MUPEN64PLUS AND IS NOT PART OF THE
         ORIGINAL SPEC
  Function: SetConfigDir
  Purpose:  To pass the location where config files should be read/
            written to.
  input:    path to config directory
  output:   none
*******************************************************************/
EXPORT void CALL SetConfigDir( char *configDir );

/******************************************************************
   NOTE: THIS HAS BEEN ADDED FOR MUPEN64PLUS AND IS NOT PART OF THE
         ORIGINAL SPEC
  Function: SetRenderingCallback
  Purpose:  Allows emulator to register a callback function that will
            be called by the graphics plugin just before the the
            frame buffers are swapped.
            This was added as a way for the emulator to draw emulator-
            specific things to the screen, e.g. On-screen display.
  input:    pointer to callback function. The function expects
            to receive the current window width and height.
  output:   none
*******************************************************************/
EXPORT void CALL SetRenderingCallback(void (*callback)());
#endif
#if defined(__cplusplus)
}
#endif
#endif //_GFX_H_INCLUDED__

