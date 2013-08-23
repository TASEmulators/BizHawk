/*  Copyright 2006 Guillaume Duhamel
    Copyright 2006 Anders Montonen
    Copyright 2010 Alex Marshall

    This file is part of Yabause.

    Yabause is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    Yabause is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Yabause; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301  USA
*/

#include <unistd.h>
#include <Carbon/Carbon.h>
#include <AGL/agl.h>

#include "../core.h"
#include "../memory.h"
#include "settings.h"
#include "cpustatus.h"
#include "../yabause.h"

#define YUI_MENU_EMULATION		1
#define YUI_MENU_DEBUG			2

#define YUI_COMMAND_RESET		1
#define YUI_COMMAND_PAUSE		2
#define YUI_COMMAND_RESUME		3
#define YUI_COMMAND_SHOW_CPU		4
#define YUI_COMMAND_HIDE_CPU		5
#define YUI_COMMAND_TOGGLE_NBG0		6
#define YUI_COMMAND_TOGGLE_NBG1		7
#define YUI_COMMAND_TOGGLE_NBG2		8
#define YUI_COMMAND_TOGGLE_NBG3		9
#define YUI_COMMAND_TOGGLE_RBG0		10
#define YUI_COMMAND_TOGGLE_VDP1		11
#define YUI_COMMAND_TOGGLE_FULLSCREEN	12
#define YUI_COMMAND_LOAD_BINARY		13
#define YUI_COMMAND_LOAD_AND_EXECUTE	14
#define YUI_COMMAND_SAVE_BINARY		15

AGLContext  myAGLContext = NULL;
WindowRef   myWindow = NULL;
yabauseinit_struct yinit;

M68K_struct * M68KCoreList[] = {
&M68KDummy,
#ifdef HAVE_C68K
&M68KC68K,
#endif
#ifdef HAVE_Q68
&M68KQ68,
#endif
NULL
};

SH2Interface_struct *SH2CoreList[] = {
&SH2Interpreter,
&SH2DebugInterpreter,
NULL
};

PerInterface_struct *PERCoreList[] = {
&PERDummy,
NULL
};

CDInterface *CDCoreList[] = {
&DummyCD,
&ISOCD,
&ArchCD,
NULL
};

SoundInterface_struct *SNDCoreList[] = {
&SNDDummy,
#ifdef HAVE_LIBSDL
&SNDSDL,
#endif
NULL
};

VideoInterface_struct *VIDCoreList[] = {
&VIDDummy,
&VIDOGL,
&VIDSoft,
NULL
};

static EventLoopTimerRef EventTimer;
int load_file_core(char* file, char* addr, int type);

void YuiIdle(EventLoopTimerRef a, void * b)
{
    PERCore->HandleEvents();
}

void read_settings(void) {
	PerPad_struct * pad;
	int i;
	CFStringRef s;
	yinit.percoretype = PERCORE_DUMMY;
	yinit.sh2coretype = SH2CORE_INTERPRETER;
	yinit.vidcoretype = VIDCORE_OGL;
	yinit.m68kcoretype = M68KCORE_C68K;
	s = CFPreferencesCopyAppValue(CFSTR("VideoCore"),
		kCFPreferencesCurrentApplication);
	if (s)
		yinit.vidcoretype = CFStringGetIntValue(s) - 1;
	yinit.sndcoretype = SNDCORE_DUMMY;
	s = CFPreferencesCopyAppValue(CFSTR("SoundCore"),
		kCFPreferencesCurrentApplication);
	if (s)
		yinit.sndcoretype = CFStringGetIntValue(s) - 1;
	yinit.cdcoretype = CDCORE_ARCH;
	s = CFPreferencesCopyAppValue(CFSTR("CDROMCore"),
		kCFPreferencesCurrentApplication);
	if (s)
		yinit.cdcoretype = CFStringGetIntValue(s) - 1;
	yinit.carttype = CART_NONE;
    s = CFPreferencesCopyAppValue(CFSTR("CartType"),
        kCFPreferencesCurrentApplication);
    if (s)
        yinit.carttype = CFStringGetIntValue(s) - 1;
	yinit.regionid = 0;
    s = CFPreferencesCopyAppValue(CFSTR("Region"),
        kCFPreferencesCurrentApplication);
    if (s)
        yinit.regionid = CFStringGetIntValue(s) - 1;

	yinit.biospath = 0;
	s = CFPreferencesCopyAppValue(CFSTR("BiosPath"),
		kCFPreferencesCurrentApplication);
	if (s)
		yinit.biospath = strdup(CFStringGetCStringPtr(s, 0));
	yinit.cdpath = 0;
	s = CFPreferencesCopyAppValue(CFSTR("CDROMDrive"),
		kCFPreferencesCurrentApplication);
	if (s)
		yinit.cdpath = strdup(CFStringGetCStringPtr(s, 0));
	yinit.buppath = 0;
    s = CFPreferencesCopyAppValue(CFSTR("BackupRamPath"),
        kCFPreferencesCurrentApplication);
    if (s)
        yinit.buppath = strdup(CFStringGetCStringPtr(s, 0));
	yinit.mpegpath = 0;
    s = CFPreferencesCopyAppValue(CFSTR("MpegRomPath"),
        kCFPreferencesCurrentApplication);
    if (s)
        yinit.mpegpath = strdup(CFStringGetCStringPtr(s, 0));
	yinit.cartpath = 0;
    s = CFPreferencesCopyAppValue(CFSTR("CartPath"),
        kCFPreferencesCurrentApplication);
    if (s)
        yinit.cartpath = strdup(CFStringGetCStringPtr(s, 0));
    
	yinit.videoformattype = VIDEOFORMATTYPE_NTSC;
	
	s = CFPreferencesCopyAppValue(CFSTR("AutoFrameSkip"),
		kCFPreferencesCurrentApplication);
	if (s)
		yinit.frameskip = CFStringGetIntValue(s);

	PerPortReset();
	pad = PerPadAdd(&PORTDATA1);

	i = 0;
	while(PerPadNames[i]) {
		s = CFPreferencesCopyAppValue(
			CFStringCreateWithCString(0, PerPadNames[i], 0),
			kCFPreferencesCurrentApplication);
		if (s)
			PerSetKey(CFStringGetIntValue(s), i, pad);
		i++;
	}
}

static void YuiPause(const int Pause)
{
    EventTimerInterval Interval;

    if(Pause)
    {
        Interval = kEventDurationForever;
        ScspMuteAudio(SCSP_MUTE_SYSTEM);
    }
    else
    {
        Interval = 16*kEventDurationMillisecond;
        ScspUnMuteAudio(SCSP_MUTE_SYSTEM);
    }

    SetEventLoopTimerNextFireTime(EventTimer, Interval);
}

void YuiRun(void) {
	static int FirstRun = 1;
	EventLoopTimerUPP myFrameUPP;

	if(FirstRun)
	{
		myFrameUPP = NewEventLoopTimerUPP(YuiIdle);
		InstallEventLoopTimer(GetCurrentEventLoop(), kEventDurationNoWait,
			16*kEventDurationMillisecond, myFrameUPP, NULL, &EventTimer);
		FirstRun = 0;
	}
	else
	{
        YuiPause(0);
        YabauseDeInit();
	}

	read_settings();
	YabauseInit(&yinit);
}

static void TogglePairedMenuItems(MenuRef menu, MenuItemIndex BaseItemIndex)
{
	MenuItemAttributes ItemAttributes;

	GetMenuItemAttributes(menu, BaseItemIndex, &ItemAttributes);

	if(ItemAttributes & kMenuItemAttrHidden)
	{
		ChangeMenuItemAttributes(menu, BaseItemIndex, 0, kMenuItemAttrHidden);
		ChangeMenuItemAttributes(menu, BaseItemIndex+1, kMenuItemAttrHidden, 0);
	}
	else
	{
		ChangeMenuItemAttributes(menu, BaseItemIndex, kMenuItemAttrHidden, 0);
		ChangeMenuItemAttributes(menu, BaseItemIndex+1, 0, kMenuItemAttrHidden);
	}
}

OSStatus MyWindowEventHandler (EventHandlerCallRef myHandler, EventRef theEvent, void* userData)
{
  OSStatus ret = noErr;
  MenuRef menu;

  switch(GetEventClass(theEvent)) {
    case kEventClassWindow:
      switch (GetEventKind (theEvent)) {
        case kEventWindowClose:

          YabauseDeInit();
          QuitApplicationEventLoop();
          break;
 
        case kEventWindowBoundsChanged:
          aglUpdateContext(myAGLContext);
          {
            Rect bounds;
            GetEventParameter(theEvent, kEventParamCurrentBounds,
	      typeQDRectangle, NULL, sizeof(Rect), NULL, &bounds);
            glViewport(0, 0, bounds.right - bounds.left,
	      bounds.bottom - bounds.top);
          }
          break;
      }
      break;
    case kEventClassCommand:
      {
        HICommand command;
        GetEventParameter(theEvent, kEventParamDirectObject,
	  typeHICommand, NULL, sizeof(HICommand), NULL, &command);
        switch(command.commandID) {
          case kHICommandPreferences:
	    CreateSettingsWindow();
            break;
          case kHICommandQuit:
            YabauseDeInit();
            QuitApplicationEventLoop();
            break;
          case YUI_COMMAND_RESET:
              YabauseReset();
              break;
          case YUI_COMMAND_PAUSE:
              YuiPause(1);
              menu = GetMenuRef(YUI_MENU_EMULATION);
              TogglePairedMenuItems(menu, 2);
              UpdateCPUStatusWindow();
              break;
        case YUI_COMMAND_RESUME:
            YuiPause(0);
            menu = GetMenuRef(YUI_MENU_EMULATION);
            TogglePairedMenuItems(menu, 2);
            break;
        case YUI_COMMAND_SHOW_CPU:
            ShowCPUStatusWindow();
            menu = GetMenuRef(YUI_MENU_DEBUG);
            TogglePairedMenuItems(menu, 1);
            break;
        case YUI_COMMAND_HIDE_CPU:
            HideCPUStatusWindow();
            menu = GetMenuRef(YUI_MENU_DEBUG);
            TogglePairedMenuItems(menu, 1);
            break;
        case YUI_COMMAND_TOGGLE_NBG0:
            if(VIDCore)
            {
                menu = GetMenuRef(YUI_MENU_DEBUG);
                TogglePairedMenuItems(menu, 4);
                ToggleNBG0();
            }
            break;
        case YUI_COMMAND_TOGGLE_NBG1:
            if(VIDCore)
            {
                menu = GetMenuRef(YUI_MENU_DEBUG);
                TogglePairedMenuItems(menu, 6);
                ToggleNBG1();
            }
            break;
        case YUI_COMMAND_TOGGLE_NBG2:
            if(VIDCore)
            {
                menu = GetMenuRef(YUI_MENU_DEBUG);
                TogglePairedMenuItems(menu, 8);
                ToggleNBG2();
            }
            break;
        case YUI_COMMAND_TOGGLE_NBG3:
            if(VIDCore)
            {
                menu = GetMenuRef(YUI_MENU_DEBUG);
                TogglePairedMenuItems(menu, 10);
                ToggleNBG3();
            }
            break;
        case YUI_COMMAND_TOGGLE_RBG0:
            if(VIDCore)
            {
                menu = GetMenuRef(YUI_MENU_DEBUG);
                TogglePairedMenuItems(menu, 12);
                ToggleRBG0();
            }
            break;
        case YUI_COMMAND_TOGGLE_VDP1:
            if(VIDCore)
            {
                menu = GetMenuRef(YUI_MENU_DEBUG);
                TogglePairedMenuItems(menu, 14);
                ToggleVDP1();
            }
            break;
        case YUI_COMMAND_TOGGLE_FULLSCREEN:
            if(VIDCore)
            {
                menu = GetMenuRef(YUI_MENU_EMULATION);
                TogglePairedMenuItems(menu, 5);
                ToggleFullScreen();
            }
            break;
        case YUI_COMMAND_LOAD_BINARY:
            CreateLoadWindow(0);
            break;
        case YUI_COMMAND_LOAD_AND_EXECUTE:
            CreateLoadWindow(1);
            break;
        case YUI_COMMAND_SAVE_BINARY:
//            MappedMemorySave(file, address, size);
            break;
        default:
            ret = eventNotHandledErr;
            printf("unhandled command\n");
            break;
        }
      }
      break;

    case kEventClassKeyboard:
      switch(GetEventKind(theEvent)) {
        int i;
        UInt32 key;
        case kEventRawKeyDown:
          GetEventParameter(theEvent, kEventParamKeyCode,
            typeUInt32, NULL, sizeof(UInt32), NULL, &key);
          PerKeyDown(key);
          break;
        case kEventRawKeyUp:
          GetEventParameter(theEvent, kEventParamKeyCode,
            typeUInt32, NULL, sizeof(UInt32), NULL, &key);
          PerKeyUp(key);
          break;
      }
      break;
    }
 
  return ret;
}

static WindowRef CreateMyWindow() {

  WindowRef myWindow;
  Rect contentBounds;

  CFStringRef windowTitle = CFSTR("Yabause");
  WindowClass windowClass = kDocumentWindowClass;
  WindowAttributes attributes =
    kWindowStandardDocumentAttributes |
    kWindowStandardHandlerAttribute |
    kWindowLiveResizeAttribute;

  EventTypeSpec eventList[] = {
    { kEventClassWindow, kEventWindowClose },
    { kEventClassWindow, kEventWindowBoundsChanged },
    { kEventClassCommand, kEventCommandProcess },
    { kEventClassKeyboard, kEventRawKeyDown },
    { kEventClassKeyboard, kEventRawKeyUp }
  };
 
  SetRect(&contentBounds, 200, 200, 520, 424);

  CreateNewWindow (windowClass,
			 attributes,
			 &contentBounds,
			 &myWindow);

  SetWindowTitleWithCFString (myWindow, windowTitle);
  CFRelease(windowTitle);
  ShowWindow(myWindow);

  InstallWindowEventHandler(myWindow,
			    NewEventHandlerUPP (MyWindowEventHandler),
			    GetEventTypeCount(eventList),
			    eventList, myWindow, NULL);
  return myWindow;
}

static OSStatus MyAGLReportError (void) {
    GLenum err = aglGetError();

    if (err == AGL_NO_ERROR)
        return noErr;
    else
        return (OSStatus) err;
}

static OSStatus MySetWindowAsDrawableObject  (WindowRef window)
{
    OSStatus err = noErr;

    GLint attributes[] =  { AGL_RGBA,
                        AGL_DOUBLEBUFFER, 
                        AGL_DEPTH_SIZE, 24, 
                        AGL_NONE };

    AGLPixelFormat myAGLPixelFormat;

    myAGLPixelFormat = aglChoosePixelFormat (NULL, 0, attributes);

    err = MyAGLReportError ();

    if (myAGLPixelFormat) {
        myAGLContext = aglCreateContext (myAGLPixelFormat, NULL);

        err = MyAGLReportError ();
        aglDestroyPixelFormat(myAGLPixelFormat);
    }

    if (! aglSetDrawable (myAGLContext, GetWindowPort (window)))
            err = MyAGLReportError ();

    if (!aglSetCurrentContext (myAGLContext))
            err = MyAGLReportError ();

    return err;

}

int main(int argc, char* argv[]) {
  MenuRef menu;
  EventLoopTimerRef nextFrameTimer;
  IBNibRef menuNib;

  myWindow = CreateMyWindow();
  MySetWindowAsDrawableObject(myWindow);

  CreateNibReference(CFSTR("menu"), &menuNib);
  SetMenuBarFromNib(menuNib, CFSTR("MenuBar"));

  EnableMenuCommand(NULL, kHICommandPreferences);

  read_settings();

  YuiRun();
  if(argc >= 2)
    load_file_core(argv[1], (argc >= 3) ? argv[2] : NULL, 1);

  RunApplicationEventLoop();

  return 0;
}

void YuiErrorMsg(const char * string) {
	printf("%s\n", string);
}

void YuiSetVideoAttribute(int type, int val) {
}

int YuiSetVideoMode(int width, int height, int bpp, int fullscreen)
{
    static          CFDictionaryRef oldDisplayMode = 0;
    static          int             oldDisplayModeValid = 0;
    
    AGLPixelFormat  myAGLPixelFormat;
    AGLDrawable     myDrawable = aglGetDrawable(myAGLContext);
    OSStatus        err = noErr;
    GLint attributesFullscreen[] =  { AGL_RGBA,
                                      AGL_FULLSCREEN,
                                      AGL_DOUBLEBUFFER, 
                                      AGL_DEPTH_SIZE, 24, 
                                      AGL_NONE };
    CGDirectDisplayID   displayId = kCGDirectMainDisplay;
    
    if(myDrawable)
    {
        if(fullscreen)
        {
            Rect            bounds;
            CGPoint         point;
            CGDisplayCount  displayCount;

            GetWindowBounds(myWindow, kWindowGlobalPortRgn, &bounds);
            point.x = (float)bounds.left;
            point.y = (float)bounds.top;
            
            CGGetDisplaysWithPoint(point, 1, &displayId, &displayCount);
            
            CFDictionaryRef refDisplayMode = CGDisplayBestModeForParameters(displayId,
                                                                            bpp, width, height, NULL);
            if(refDisplayMode)
            {
                GDHandle gdhDisplay; 
                oldDisplayMode = CGDisplayCurrentMode(displayId);
                oldDisplayModeValid = 1;

                aglSetDrawable(myAGLContext, NULL);
                aglSetCurrentContext(NULL);
                aglDestroyContext(myAGLContext);
                myAGLContext = NULL;

                CGCaptureAllDisplays();
                CGDisplaySwitchToMode(displayId, refDisplayMode);
                CGDisplayHideCursor(displayId);

                DMGetGDeviceByDisplayID((DisplayIDType)displayId, &gdhDisplay, 0);

                myAGLPixelFormat = aglChoosePixelFormat(&gdhDisplay, 1, attributesFullscreen);
                if(myAGLPixelFormat)
                {
                    myAGLContext = aglCreateContext(myAGLPixelFormat, NULL);
                    if(myAGLContext)
                    {
                        aglSetCurrentContext(myAGLContext);
                        aglSetFullScreen(myAGLContext, width, height, 0, 0);
                    }
                    
                    err = MyAGLReportError();
                    aglDestroyPixelFormat(myAGLPixelFormat);
                }
                else
                {
                    err = MyAGLReportError();
                    CGReleaseAllDisplays();
                    CGDisplayShowCursor(displayId);
                }
            }
            else
            {
                err = MyAGLReportError();
            }
        }
        else
        {
            if(oldDisplayModeValid)
            {
                oldDisplayModeValid = 0;
                
                aglSetDrawable(myAGLContext, NULL);
                aglSetCurrentContext(NULL);
                aglDestroyContext(myAGLContext);
                myAGLContext = NULL;

                CGDisplayShowCursor(displayId);
                CGDisplaySwitchToMode(displayId, oldDisplayMode);
                CGReleaseAllDisplays();

                MySetWindowAsDrawableObject(myWindow);
            }
        }
    }
    
    return !(err == noErr);
}

void YuiSwapBuffers(void) {
  aglSwapBuffers(myAGLContext);
}
