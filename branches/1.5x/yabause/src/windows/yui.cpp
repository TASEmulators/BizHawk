/*  Copyright 2004 Guillaume Duhamel
    Copyright 2004-2008 Theo Berkau
    Copyright 2005 Joost Peters

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
#include "hotkey.h"
#include <windows.h>
#include <commctrl.h>
#include <GL/gl.h>
#undef FASTCALL
extern "C" {
#include "../cs2.h"
#include "../vdp2.h"
#include "../yui.h"
#include "../vidogl.h"
#include "../vidsoft.h"
#include "../peripheral.h"
#include "../cs0.h"
#include "../debug.h"
#include "../m68kcore.h"
#include "../movie.h"
#include "../snddx.h"
#include "cheats.h"
#include "../perdx.h"
#include "cd.h"
#include "aviout.h"
#include "cpudebug/yuidebug.h"
#include "disasm.h"
#include "hexedit.h"
#include "settings/settings.h"
}

#include "resource.h"
#include "ram_search.h"

#ifdef NOC68K
#include "../m68kc68k.h"
#endif
//#include "../m68khle.h"

#define DONT_PROFILE
extern "C" {
#include "../profile.h"
#include "yuiwin.h"
}
#include "ramwatch.h"

//Prototypes
void ResetGame();
void HardResetGame();
void YuiPlayMovie(HWND hWnd);
void YuiRecordMovie(HWND hWnd);
void YuiScreenshot(HWND hWnd);
void YuiRecordAvi(HWND hWnd);
void YuiStopAvi();
void WriteToINI();

HANDLE emuthread=INVALID_HANDLE_VALUE;
int KillEmuThread=0;
int stop=1;
int stopped=1;
int paused=0;
int yabwinw;
int yabwinh;
int screenwidth;
int screenheight;
int AlreadyStarted;

HINSTANCE y_hInstance;
HWND YabWin=NULL;
HMENU YabMenu=NULL;
HDC YabHDC=NULL;
HGLRC YabHRC=NULL;
BOOL isfullscreenset=FALSE;
int yabwinx = 0;
int yabwiny = 0;
psp_struct settingspsp;
extern HWND RamSearchHWnd;

int oldbpp = 0;
static int redsize = 0;
static int greensize = 0;
static int bluesize = 0;
static int depthsize = 0;

int AVIRecording = 0;

TCHAR yssfilename[MAX_PATH] = TEXT("\0");
char ysspath[MAX_PATH] = "\0";

TCHAR avifilename[MAX_PATH] = TEXT("\0");
char avipath[MAX_PATH] = "\0";

TCHAR ymvfilename[MAX_PATH] = TEXT("\0");
char ymvpath[MAX_PATH] = "\0";

char netlinksetting[80];
TCHAR bmpfilename[MAX_PATH] = TEXT("\0");

LRESULT CALLBACK WindowProc(HWND hWnd,UINT uMsg,WPARAM wParam,LPARAM lParam);
void YuiReleaseVideo(void);

extern "C" SH2Interface_struct *SH2CoreList[] = {
&SH2Interpreter,
&SH2DebugInterpreter,
NULL
};

extern "C" PerInterface_struct *PERCoreList[] = {
&PERDummy,
&PERDIRECTX,
NULL
};

extern "C" CDInterface *CDCoreList[] = {
&DummyCD,
&ISOCD,
&ArchCD,
NULL
};

extern "C" SoundInterface_struct *SNDCoreList[] = {
&SNDDummy,
&SNDDIRECTX,
NULL
};

extern "C" VideoInterface_struct *VIDCoreList[] = {
&VIDDummy,
&VIDOGL,
&VIDSoft,
NULL
};

extern "C" M68K_struct *M68KCoreList[] = {
&M68KDummy,
#ifndef NOC68K
&M68KC68K,
#endif
#ifdef HAVE_Q68
&M68KQ68,
#endif
//&M68KHLE,
NULL
};

//////////////////////////////////////////////////////////////////////////////

extern "C" 
{
   HWND DXGetWindow()
   {
      return YabWin;
   }
};

//////////////////////////////////////////////////////////////////////////////

void YuiSetVideoAttribute(int type, int val)
{
   switch (type)
   {
      case RED_SIZE:
      {
         redsize = val;
         break;
      }
      case GREEN_SIZE:
      {
         greensize = val;
         break;
      }
      case BLUE_SIZE:
      {
         bluesize = val;
         break;
      }
      case DEPTH_SIZE:
      {
         depthsize = val;
         break;
      }
      default: break;
   }
}

//////////////////////////////////////////////////////////////////////////////

int YuiSetVideoMode(int width, int height, int bpp, int fullscreen)
{
   PIXELFORMATDESCRIPTOR pfd;
   DWORD style=0;
   DWORD exstyle=0;
   RECT rect;

   if (!isfullscreenset && fullscreen)
   {
      GetWindowRect(YabWin, &rect);
      yabwinx = rect.left;
      yabwiny = rect.top;
   }

   // Make sure any previously setup variables are released
   if (oldbpp != bpp)
      YuiReleaseVideo();

   if (fullscreen)
   {
       DEVMODE dmSettings;
       LONG ret;

       memset(&dmSettings, 0, sizeof(dmSettings));
       dmSettings.dmSize = sizeof(dmSettings);
       dmSettings.dmPelsWidth = width;
       dmSettings.dmPelsHeight = height;
       dmSettings.dmBitsPerPel = bpp;
       dmSettings.dmFields = DM_PELSWIDTH | DM_PELSHEIGHT | DM_BITSPERPEL;

       if ((ret = ChangeDisplaySettings(&dmSettings,CDS_FULLSCREEN)) != DISP_CHANGE_SUCCESSFUL)
       {
          // revert back to windowed mode
          ChangeDisplaySettings(NULL,0);
          ShowCursor(TRUE);
          fullscreen = FALSE;
          style = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX;
          exstyle = WS_EX_APPWINDOW | WS_EX_WINDOWEDGE;
          SetMenu(YabWin, YabMenu);
          width=windowwidth;
          height=windowheight;
       }
       else
       {
          // Adjust window styles
          style = WS_POPUP;
          exstyle = WS_EX_APPWINDOW;
          SetMenu(YabWin, NULL);
          ShowCursor(FALSE);
       }
   }
   else
   {
       // Adjust window styles
       style = WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU | WS_THICKFRAME | WS_MINIMIZEBOX;
       exstyle = WS_EX_APPWINDOW | WS_EX_WINDOWEDGE;
       SetMenu(YabWin, YabMenu);
   }

   SetWindowLong(YabWin, GWL_STYLE, style);
   SetWindowLong(YabWin, GWL_EXSTYLE, exstyle);

   rect.left = 0;
   rect.right = width;
   rect.top = 0;
   rect.bottom = height;
   AdjustWindowRectEx(&rect, style, FALSE, exstyle);

   if (!fullscreen)
   {
      rect.right = rect.left + width + GetSystemMetrics(SM_CXSIZEFRAME) * 2;
      rect.bottom = rect.top + height + (GetSystemMetrics(SM_CYSIZEFRAME) * 2) + GetSystemMetrics(SM_CYMENU) + GetSystemMetrics(SM_CYCAPTION);  
   }

   // Get the Device Context for our window
   if (oldbpp != bpp)
   {
      if ((YabHDC = GetDC(YabWin)) == NULL)
      {
         YuiReleaseVideo();
         return -1;
      }

      // Let's setup the Pixel format for the context

      memset(&pfd, 0, sizeof(PIXELFORMATDESCRIPTOR));
      pfd.nSize = sizeof(PIXELFORMATDESCRIPTOR);
      pfd.nVersion = 1;
      pfd.dwFlags = PFD_DRAW_TO_WINDOW | PFD_SUPPORT_OPENGL | PFD_DOUBLEBUFFER;
      pfd.iPixelType = PFD_TYPE_RGBA;
      pfd.cColorBits = bpp;
      pfd.cRedBits = redsize;
      pfd.cGreenBits = greensize;
      pfd.cBlueBits = bluesize;
      pfd.cAlphaBits = 0;
      pfd.cAccumRedBits = 0;
      pfd.cAccumGreenBits = 0;
      pfd.cAccumBlueBits = 0;
      pfd.cAccumAlphaBits = 0;
      pfd.cAccumBits = pfd.cAccumRedBits + pfd.cAccumGreenBits +
         pfd.cAccumBlueBits + pfd.cAccumAlphaBits;
      pfd.cDepthBits = depthsize;
      pfd.cStencilBits = 8;

      SetPixelFormat(YabHDC, ChoosePixelFormat(YabHDC, &pfd), &pfd);

      if ((YabHRC = wglCreateContext(YabHDC)) == NULL)
      {
         YuiReleaseVideo();
         return -1;
      }

      if(wglMakeCurrent(YabHDC,YabHRC) == FALSE)
      {
         YuiReleaseVideo();
         return -1;
      }
   }

   ShowWindow(YabWin,SW_SHOW);
   SetForegroundWindow(YabWin);
   SetFocus(YabWin);

   if (fullscreen)
      SetWindowPos(YabWin, HWND_TOP, 0, 0, rect.right-rect.left, rect.bottom-rect.top, SWP_NOCOPYBITS | SWP_SHOWWINDOW);
   else
      SetWindowPos(YabWin, HWND_TOP, yabwinx, yabwiny, rect.right-rect.left, rect.bottom-rect.top, SWP_NOCOPYBITS | SWP_SHOWWINDOW);

   isfullscreenset = fullscreen;

   screenwidth = width;
   screenheight = height;
   oldbpp = bpp;

   return 0;
}

//////////////////////////////////////////////////////////////////////////////

void YuiReleaseVideo(void)
{
   if (isfullscreenset)
   {
      ChangeDisplaySettings(NULL,0);
      ShowCursor(TRUE);
   }

   if (YabHRC)
   {
       wglMakeCurrent(NULL,NULL);
       wglDeleteContext(YabHRC);
       YabHRC = NULL;
   }

   if (YabHDC)
   {
      ReleaseDC(YabWin,YabHDC);
      YabHDC = NULL;
   }
}

//////////////////////////////////////////////////////////////////////////////

void YuiSwapBuffers()
{
   SwapBuffers(YabHDC);
}

//////////////////////////////////////////////////////////////////////////////

int YuiCaptureScreen(const char *filename)
{
   u8 *buf;
   FILE *fp;
   BITMAPFILEHEADER bmf;
   BITMAPINFOHEADER bmi;
   int totalsize=screenwidth * screenheight * sizeof(u32);
   int i;

   if ((fp = fopen(filename, "wb")) == NULL)
   {
      // error
      return -1;
   }

   if ((buf = (u8 *)malloc(totalsize)) == NULL)
   {
      // error
      fclose(fp);
      return -2;
   }

   SwapBuffers(YabHDC);
   glReadBuffer(GL_BACK);
   glReadPixels(0, 0, screenwidth, screenheight, GL_RGBA, GL_UNSIGNED_BYTE, buf);
   SwapBuffers(YabHDC);

   for (i = 0; i < (screenwidth * screenheight); i++)
   {
      u8 temp;

      temp = buf[i * 4];
      buf[i * 4] = buf[(i * 4) + 2];
      buf[(i * 4) + 2] = temp;
   }

   // Setup BMP header
   ZeroMemory(&bmf, sizeof(bmf));
   bmf.bfType = 'B' | ('M' << 8);
   bmf.bfSize = sizeof(bmf);
   bmf.bfOffBits = sizeof(bmf) + sizeof(bmi);

   ZeroMemory(&bmi, sizeof(bmi));

   bmi.biSize = sizeof(bmi);
   bmi.biWidth = screenwidth;
   bmi.biHeight = screenheight;
   bmi.biPlanes = 1;
   bmi.biBitCount = 32;
   bmi.biCompression = 0; // None
   bmi.biSizeImage = bmi.biWidth * bmi.biHeight * sizeof(u32);

   fwrite((void *)&bmf, 1, sizeof(bmf), fp);
   fwrite((void *)&bmi, 1, sizeof(bmi), fp);
   fwrite((void *)buf, 1, totalsize, fp);
   fclose(fp);
   free(buf);

   return 0;
}

//////////////////////////////////////////////////////////////////////////////

static void AviEnd()
{
	DRV_AviEnd();
}
	  	
//////////////////////////////////////////////////////////////////////////////

extern "C" void YuiCaptureVideo(void)
{
	u8 *buf;
	int totalsize=screenwidth * screenheight * sizeof(u32);

	if(AVIRecording == 0)
		return;

	if ((buf = (u8 *)malloc(totalsize)) == NULL)
	{
		return;
	}

	SwapBuffers(YabHDC);
	glReadBuffer(GL_BACK);
	glReadPixels(0, 0, screenwidth, screenheight, GL_RGBA, GL_UNSIGNED_BYTE, buf);
	SwapBuffers(YabHDC);

	DRV_AviVideoUpdate((const u16*)buf, YabWin);
}

//////////////////////////////////////////////////////////////////////////////

void YuiPause()
{
#ifdef USETHREADS
   stop = 1;
   while (!stopped) { Sleep(0); }
#endif
   ScspMuteAudio(SCSP_MUTE_SYSTEM);
   paused = 1;
}

//////////////////////////////////////////////////////////////////////////////

void YuiUnPause()
{
   if (paused)
   {
      ScspUnMuteAudio(SCSP_MUTE_SYSTEM);
#ifdef USETHREADS
      stop = 0;
#endif
      paused = 0;
   }
}

//////////////////////////////////////////////////////////////////////////////

void YuiTempPause()
{
#ifdef USETHREADS
   if (!paused)
   {
      stop = 1;
      while (!stopped) { Sleep(0); }
      ScspMuteAudio(SCSP_MUTE_SYSTEM);
   }
#else
   ScspMuteAudio(SCSP_MUTE_SYSTEM);
#endif
}

//////////////////////////////////////////////////////////////////////////////

void YuiTempUnPause()
{
#ifdef USETHREADS
   if (!paused)
   {
      ScspUnMuteAudio(SCSP_MUTE_SYSTEM);
      stop = 0;
   }
#else
   ScspUnMuteAudio(SCSP_MUTE_SYSTEM);
#endif
}


//////////////////////////////////////////////////////////////////////////////

//adelikat: TODO: This should be the only pause function called for menu & hotkey items
void TogglePause()
{
	PauseOrUnpause();
}
//////////////////////////////////////////////////////////////////////////////

#ifdef USETHREADS
DWORD WINAPI YabauseEmulate(LPVOID arg)
{
   yabauseinit_struct yinit;
   int ret;

YabauseSetup:
   memset(&yinit, 0, sizeof(yabauseinit_struct));
   yinit.percoretype = percoretype;
   yinit.sh2coretype = sh2coretype;
   yinit.vidcoretype = vidcoretype;
   yinit.sndcoretype = sndcoretype;
   if (IsPathCdrom(cdrompath))
      yinit.cdcoretype = CDCORE_SPTI;
   else
      yinit.cdcoretype = CDCORE_ISO;
   //yinit.m68kcoretype = M68KCORE_HLE;
#ifdef NOC68K
   yinit.m68kcoretype = M68KCORE_DEFAULT;
#else
   yinit.m68kcoretype = M68KCORE_C68K;
#endif
   yinit.carttype = carttype;
   yinit.regionid = regionid;
   if (strcmp(biosfilename, "") == 0)
      yinit.biospath = NULL;
   else
      yinit.biospath = biosfilename;
   yinit.cdpath = cdrompath;
   yinit.buppath = backupramfilename;
   yinit.mpegpath = mpegromfilename;
   yinit.cartpath = cartfilename;
   yinit.netlinksetting = netlinksetting;
   yinit.videoformattype = VIDEOFORMATTYPE_NTSC;

   if ((ret = YabauseInit(&yinit)) < 0)
   {
      if (ret == -2)
      {
         nocorechange = 1;

         // Startup Settings Configuration
         ret = (int)SettingsCreatePropertySheets(NULL, TRUE, &settingspsp);

         free(settingspsp.psp);
         memset(&settingspsp, 0, sizeof(settingspsp));

         if (ret == TRUE)
         {
            YabauseDeInit();
            goto YabauseSetup;
         }
      }
      PostMessage(YabWin, WM_CLOSE, 0, 0);
      return -1;
   }

   if (usefullscreenonstartup)
      VIDCore->Resize(fullscreenwidth, fullscreenheight, 1);
   else if (usecustomwindowsize)
      VIDCore->Resize(windowwidth, windowheight, 0);

   PERDXLoadDevices(inifilename);

   stop = 0;   

   ScspSetVolume(sndvolume);

   if (enableautofskip)
      EnableAutoFrameSkip();
   else
      DisableAutoFrameSkip();

//   nocorechange = 1;

   while(!KillEmuThread)
   {
      while (!stop)
      {
         stopped = 0;

         if (PERCore->HandleEvents() != 0)
         {
            YuiReleaseVideo();
            if (YabMenu)
               DestroyMenu(YabMenu);
            return -1;
         }
         Update_RAM_Search();
         Update_RAM_Watch();
         YuiCaptureVideo();

         Sleep(0);
      }

      if (changecore)
      {
         // Update cores
         if (changecore & 0x1)
         {
            if (IsPathCdrom(cdrompath))
               Cs2ChangeCDCore(CDCORE_SPTI, cdrompath);
            else
               Cs2ChangeCDCore(CDCORE_ISO, cdrompath);
         }
         else if (changecore & 0x2)
         {
            int coretype=vidcoretype;
            corechanged=1;
            VideoChangeCore(vidcoretype);
            Sleep(0);
            if (VIDCore && !VIDCore->IsFullscreen())
            {
               if (usecustomwindowsize)
                  VIDCore->Resize(windowwidth, windowheight, 0);
               else
                  VIDCore->Resize(320, 224, 0);
            }
         }
         else if (changecore & 0x4)
            ScspChangeSoundCore(sndcoretype);
         

         changecore = 0;
         corechanged = 1;
      }

      stopped = 1;

      Sleep(300);
   }
   return 0;
}
#endif

//////////////////////////////////////////////////////////////////////////////

void YuiPrintUsage()
{
   MessageBox (NULL, (LPCWSTR)_16("Usage: yabause [OPTIONS]...\n"
                     "-h\t\t--help\t\t\tPrint help and exit\n"
                     "-b STRING\t--bios=STRING\t\tbios file\n"
                     "-i STRING\t\t--iso=STRING\t\tiso/cue file\n"
                     "-c STRING\t--cdrom=STRING\t\tcdrom path\n"
                     "-ns\t\t--nosound\t\tturn sound off\n"
                     "-f\t\t--fullscreen\t\tstart in fullscreen mode\n"
                     "\t\t--binary=STRING:ADDRESS\tLoad binary file to address"),
                     (LPCWSTR)_16("Command line usage"),  MB_OK | MB_ICONINFORMATION);
}

//////////////////////////////////////////////////////////////////////////////

int ParseStringEmbeddedSpaces(char *out)
{
   char *argv;
   
   if ((argv = strtok(NULL, " ")) != NULL)
   {      
      if (argv[0] == '\"')
      {
         strcpy(out, argv+1);

         if ((argv = strtok(NULL, "\"")) != NULL)
         {
            strcat(out, " ");
            strcat(out, argv);
         }
      }
      else if (out != NULL)
         strcpy(out, argv);
      return TRUE;
   }
   return FALSE;
}

//////////////////////////////////////////////////////////////////////////////

int ParseStringEmbeddedSpaces2(char *out, char *in)
{
   char *argv;
   char tempstr[MAX_PATH];

   if (sscanf(strchr(in, '=')+1, "%[^\n]", tempstr) == 0)
      return FALSE;

   if (tempstr[0] == '\"')
   {
      strcpy(out, tempstr+1);

      if ((argv = strtok(NULL, "\"")) != NULL)
      {
         strcat(out, " ");
         strcat(out, argv);
      }
   }
   else if (out != NULL)
      strcpy(out, tempstr);
   return TRUE;
}

//////////////////////////////////////////////////////////////////////////////

yabauseinit_struct yinit;

int YuiInit(LPSTR lpCmdLine)
{
   MSG                         msg;
   DWORD inifilenamesize=0;
   char *pinifilename;
   char tempstr[MAX_PATH];
   HACCEL hAccel;
   static char szAppName[128];
   WNDCLASS MyWndClass;
   int ret;
   int ip[4];
   INITCOMMONCONTROLSEX iccs;
   char *argv=NULL;
   int forcecdpath=0;
   char filename[MAX_PATH];
   u32 addr=0;
   int loadexec=0;
   char *cmddup;

   memset(&iccs, 0, sizeof(INITCOMMONCONTROLSEX));
   iccs.dwSize = sizeof(INITCOMMONCONTROLSEX);
   iccs.dwICC = ICC_INTERNET_CLASSES | ICC_TAB_CLASSES | ICC_PROGRESS_CLASS;
   InitCommonControlsEx(&iccs);

   InitDisasm();
   InitHexEdit();

   y_hInstance = GetModuleHandle(NULL);

   // get program pathname
   inifilenamesize = GetModuleFileNameA(y_hInstance, inifilename, MAX_PATH);

   // set pointer to start of extension

   if ((pinifilename = strrchr(inifilename, '.')))
      // replace .exe with .ini
      sprintf(pinifilename, ".ini");

#ifndef NO_CLI
   // Just handle the basic args
   cmddup = strdup(lpCmdLine);
   argv = strtok(cmddup, " ");

   while (argv != NULL)
   {
      if (strcmp(argv, "-h") == 0 ||
          strcmp(argv, "-?") == 0 ||
          strcmp(argv, "--help") == 0)
      {
         // Show usage
         YuiPrintUsage();
         free(cmddup);
         return 0;
      }
      else if (strcmp(argv, "-i") == 0 ||
               strcmp(argv, "-c") == 0)
      {
         if ((argv = strtok(NULL, " ")) != NULL)
         {
            if (argv[0] == '\"')
            {
               if ((argv = strtok(NULL, "\"")) == NULL)
                  break;
            }
            forcecdpath = 0;
            break;
         }
         break;
      }
      else if (strstr(argv, "--iso=") ||
               strstr(argv, "--cdrom="))
      {
         forcecdpath = 0;
         break;
      }

      argv = strtok(NULL, " ");
   }

   free(cmddup);
#endif

#ifdef USEHOTKEY
   LoadHotkeyConfig();
#endif

   if (GetPrivateProfileStringA("General", "CDROMDrive", "", cdrompath, MAX_PATH, inifilename) == 0)
   {
      if (forcecdpath)
      {
         nocorechange = 1;

         ret = (int)SettingsCreatePropertySheets(NULL, TRUE, &settingspsp);
         free(settingspsp.psp);
         memset(&settingspsp, 0, sizeof(settingspsp));

         // Startup Settings Configuration
         if (ret != TRUE)
         {
            // exit program with error
            MessageBox (NULL, (LPCWSTR)_16("yabause.ini must be properly setup before program can be used."), (LPCWSTR)_16("Error"),  MB_OK | MB_ICONINFORMATION);
            return -1;
         }
      }
   }

   GetPrivateProfileStringA("General", "BiosPath", "", biosfilename, MAX_PATH, inifilename);
   GetPrivateProfileStringA("General", "BackupRamPath", "bkram.bin", backupramfilename, MAX_PATH, inifilename);
   GetPrivateProfileStringA("General", "MpegRomPath", "", mpegromfilename, MAX_PATH, inifilename);
   GetPrivateProfileStringA("General", "StatePath", "", ysspath, MAX_PATH, inifilename);
   if (strcmp(ysspath, "") == 0)
      GetCurrentDirectoryA(MAX_PATH, ysspath);

   GetPrivateProfileStringA("General", "CartType", "", tempstr, MAX_PATH, inifilename);

   // figure out cart type here, grab cartfilename if necessary
   carttype = atoi(tempstr);
   
   switch (carttype)
   {
      case CART_PAR:
      case CART_BACKUPRAM4MBIT:
      case CART_BACKUPRAM8MBIT:
      case CART_BACKUPRAM16MBIT:
      case CART_BACKUPRAM32MBIT:
      case CART_ROM16MBIT:
         GetPrivateProfileStringA("General", "CartPath", "", cartfilename, MAX_PATH, inifilename);
         break;
      default: break;
   }

   // Grab Bios Language Settings
//   GetPrivateProfileStringA("General", "BiosLanguage", "", tempstr, MAX_PATH, inifilename);

   // Grab SH2 Core Settings
   GetPrivateProfileStringA("General", "SH2Core", "", tempstr, MAX_PATH, inifilename);
   sh2coretype = atoi(tempstr);

   // Grab Region Settings
   GetPrivateProfileStringA("General", "Region", "", tempstr, MAX_PATH, inifilename);

   if (strlen(tempstr) == 1)
   {
      switch (tempstr[0])
      {
         case 'J':
            regionid = 1;
            break;
         case 'T':
            regionid = 2;
            break;
         case 'U':
            regionid = 4;
            break;
         case 'B':
            regionid = 5;
            break;
         case 'K':
            regionid = 6;
            break;
         case 'A':
            regionid = 0xA;
            break;
         case 'E':
            regionid = 0xC;
            break;
         case 'L':
            regionid = 0xD;
            break;
         default: break;
      }
   }
   else if (stricmp(tempstr, "AUTO") == 0)
      regionid = 0;

   // Grab Video Core Settings
   GetPrivateProfileStringA("Video", "VideoCore", "-1", tempstr, MAX_PATH, inifilename);
   vidcoretype = atoi(tempstr);
   if (vidcoretype == -1)
      vidcoretype = VIDCORE_OGL;

   // Grab Auto Frameskip Settings
   GetPrivateProfileStringA("Video", "AutoFrameSkip", "0", tempstr, MAX_PATH, inifilename);
   enableautofskip = atoi(tempstr);

   // Grab Full Screen Settings
   GetPrivateProfileStringA("Video", "UseFullScreenOnStartup", "0", tempstr, MAX_PATH, inifilename);
   usefullscreenonstartup = atoi(tempstr);

   GetPrivateProfileStringA("Video", "FullScreenWidth", "640", tempstr, MAX_PATH, inifilename);
   fullscreenwidth = atoi(tempstr);

   GetPrivateProfileStringA("Video", "FullScreenHeight", "480", tempstr, MAX_PATH, inifilename);
   fullscreenheight = atoi(tempstr);

   // Grab Window Settings
   GetPrivateProfileStringA("Video", "UseCustomWindowSize", "0", tempstr, MAX_PATH, inifilename);
   usecustomwindowsize = atoi(tempstr);

   GetPrivateProfileStringA("Video", "WindowWidth", "320", tempstr, MAX_PATH, inifilename);
   windowwidth = atoi(tempstr);

   GetPrivateProfileStringA("Video", "WindowHeight", "224", tempstr, MAX_PATH, inifilename);
   windowheight = atoi(tempstr);

   // Grab Sound Core Settings
   GetPrivateProfileStringA("Sound", "SoundCore", "-1", tempstr, MAX_PATH, inifilename);
   sndcoretype = atoi(tempstr);

   if (sndcoretype == -1)
      sndcoretype = SNDCORE_DIRECTX;

   // Grab Volume Settings
   GetPrivateProfileStringA("Sound", "Volume", "100", tempstr, MAX_PATH, inifilename);
   sndvolume = atoi(tempstr);

   GetPrivateProfileStringA("General", "CartType", "", tempstr, MAX_PATH, inifilename);

   // Grab Netlink Settings
   GetPrivateProfileStringA("Netlink", "LocalRemoteIP", "127.0.0.1", tempstr, MAX_PATH, inifilename);
   sscanf(tempstr, "%d.%d.%d.%d", ip, ip+1, ip+2, ip+3);
   netlinklocalremoteip = (DWORD)MAKEIPADDRESS(ip[0], ip[1], ip[2], ip[3]);

   GetPrivateProfileStringA("Netlink", "Port", "7845", tempstr, MAX_PATH, inifilename);
   netlinkport = atoi(tempstr);

   sprintf(netlinksetting, "%d.%d.%d.%d\n%d", (int)FIRST_IPADDRESS(netlinklocalremoteip), (int)SECOND_IPADDRESS(netlinklocalremoteip), (int)THIRD_IPADDRESS(netlinklocalremoteip), (int)FOURTH_IPADDRESS(netlinklocalremoteip), netlinkport);

	// Grab RamWatch Settings
	GetPrivateProfileStringA("RamWatch", "AutoLoad", "0", tempstr, MAX_PATH, inifilename);
	AutoRWLoad = atoi(tempstr);
	GetPrivateProfileStringA("RamWatch", "SaveWindowPos", "0", tempstr, MAX_PATH, inifilename);
	RWSaveWindowPos = atoi(tempstr);
	GetPrivateProfileStringA("RamWatch", "Ram_x", "0", tempstr, MAX_PATH, inifilename);
	ramw_x = atoi(tempstr);
	GetPrivateProfileStringA("RamWatch", "Ram_y", "0", tempstr, MAX_PATH, inifilename);
	ramw_y = atoi(tempstr);
	for(int i = 0; i < MAX_RECENT_WATCHES; i++)
		{
			char str[256];
			sprintf(str, "Recent Watch %d", i+1);
			GetPrivateProfileStringA("Watches", str, "", &rw_recent_files[i][0], 1024, inifilename);
		}
	
	// Grab OSD Toggle setting
	GetPrivateProfileStringA("Video", "OSD Display", "0", tempstr, MAX_PATH, inifilename);
	int x = atoi(tempstr);
	SetOSDToggle(x);

#if DEBUG
   // Grab Logging settings
   GetPrivateProfileStringA("Log", "Enable", "0", tempstr, MAX_PATH, inifilename);
   uselog = atoi(tempstr);

   GetPrivateProfileStringA("Log", "Type", "0", tempstr, MAX_PATH, inifilename);
   logtype = atoi(tempstr);

   GetPrivateProfileStringA("Log", "Filename", "", logfilename, MAX_PATH, inifilename);

   if (uselog)
   {
      switch (logtype)
      {
         case 0: // Log to file
            MainLog = DebugInit("main", DEBUG_STREAM, logfilename);
            break;
         case 1: // Log to Window
         {
            RECT rect;

            if ((logbuffer = (char *)malloc(logsize)) == NULL)
               break;
            LogWin = CreateDialog(y_hInstance,
                                  MAKEINTRESOURCE(IDD_LOG),
                                  NULL,
                                  (DLGPROC)LogDlgProc);
            GetWindowRect(LogWin, &rect);
            GetPrivateProfileStringA("Log", "WindowX", "0", tempstr, MAX_PATH, inifilename);
            ret = atoi(tempstr);
            GetPrivateProfileStringA("Log", "WindowY", "0", tempstr, MAX_PATH, inifilename);
            SetWindowPos(LogWin, HWND_TOP, ret, atoi(tempstr), rect.right-rect.left, rect.bottom-rect.top, SWP_NOCOPYBITS | SWP_SHOWWINDOW);
            MainLog = DebugInit("main", DEBUG_CALLBACK, (char *)&UpdateLogCallback);
            break;
         }
         default: break;
      }
   }
#endif

   // Get Window Position(if saved)
   GetPrivateProfileStringA("General", "WindowX", "0", tempstr, MAX_PATH, inifilename);
   yabwinx = atoi(tempstr);
   GetPrivateProfileStringA("General", "WindowY", "0", tempstr, MAX_PATH, inifilename);
   yabwiny = atoi(tempstr);

#ifndef NO_CLI
   cmddup = strdup(lpCmdLine);
   // Now that all the ini stuff is done, continue grabbing args
   argv = strtok(cmddup, " ");

   while (argv != NULL)
   {
      if (strcmp(argv, "-b") == 0 && (argv = strtok(NULL, " ")))
         ParseStringEmbeddedSpaces(biosfilename);
      else if (strstr(argv, "--bios="))
         ParseStringEmbeddedSpaces2(biosfilename, argv);
      else if (strcmp(argv, "-i") == 0 || 
               strcmp(argv, "-c") == 0)
         ParseStringEmbeddedSpaces(cdrompath);
      else if (strstr(argv, "--iso=") || 
               strstr(argv, "--cdrom="))
         ParseStringEmbeddedSpaces2(cdrompath, argv);
      else if (strcmp(argv, "-ns") == 0 ||
               strcmp(argv, "--nosound") == 0)
         sndcoretype = SNDCORE_DUMMY;
      else if (strcmp(argv, "-f") == 0 ||
               strcmp(argv, "--fullscreen") == 0)
         usefullscreenonstartup = TRUE;
      else if (strstr(argv, "--binary="))
      {
         char *p;
        
         if (!ParseStringEmbeddedSpaces2(filename, argv))
            continue;

         p = strrchr(filename, ':')+1;

         if (sscanf(p, "%lx", &addr))
         {
            p = strrchr(filename, ':');
            p[0] = '\0';
         }
         else
            addr = 0x06004000;

         loadexec = 1;
      }
      else
      {
         printf("Invalid argument %s\n", argv);
         return 0;
      }
      argv = strtok(NULL, " ");
   }

   free(cmddup);
#endif

   // Figure out how much of the screen is useable
   if (usecustomwindowsize)
   {
      // Since we can't retrieve it, use default values
      yabwinw = windowwidth + GetSystemMetrics(SM_CXSIZEFRAME) * 2;
      yabwinh = windowheight + (GetSystemMetrics(SM_CYSIZEFRAME) * 2) + GetSystemMetrics(SM_CYMENU) + GetSystemMetrics(SM_CYCAPTION);
   }
   else
   {
      yabwinw = 320 + GetSystemMetrics(SM_CXSIZEFRAME) * 2;
      yabwinh = 224 + (GetSystemMetrics(SM_CYSIZEFRAME) * 2) + GetSystemMetrics(SM_CYMENU) + GetSystemMetrics(SM_CYCAPTION);
   }

   hAccel = LoadAccelerators(y_hInstance, MAKEINTRESOURCE(IDR_MAIN_ACCEL));

   // Set up and register window class
   MyWndClass.style = CS_HREDRAW | CS_VREDRAW | CS_OWNDC;
   MyWndClass.lpfnWndProc = (WNDPROC) WindowProc;
   MyWndClass.cbClsExtra = 0;
   MyWndClass.cbWndExtra = sizeof(DWORD);
   MyWndClass.hInstance = y_hInstance;
   MyWndClass.hIcon = LoadIcon(y_hInstance, MAKEINTRESOURCE(IDI_ICON));
   MyWndClass.hCursor = LoadCursor(NULL, IDC_ARROW);
   MyWndClass.hbrBackground = (HBRUSH) GetStockObject(BLACK_BRUSH);
   MyWndClass.lpszClassName = (LPCWSTR)_16("Yabause");
   MyWndClass.lpszMenuName = NULL;

   YabMenu = LoadMenu(y_hInstance, MAKEINTRESOURCE(IDR_MENU));

   if (!RegisterClass(&MyWndClass))
      return -1;

   sprintf(szAppName, "Yabause %s", VERSION);

   // Create new window
   YabWin = CreateWindow((LPCWSTR)_16("Yabause"),       // class
                         (LPCWSTR)_16(szAppName),       // caption
                         WS_OVERLAPPED | WS_CAPTION | WS_SYSMENU |                                        
                         WS_THICKFRAME | WS_MINIMIZEBOX |   // style
                         WS_CLIPCHILDREN,
                         yabwinx,              // x pos
                         yabwiny,              // y pos
                         yabwinw,              // width
                         yabwinh,              // height
                         HWND_DESKTOP,         // parent window
                         NULL,                 // menu
                         y_hInstance,          // instance
                         NULL);                // parms


#ifndef USETHREADS
YabauseSetup:
   memset(&yinit, 0, sizeof(yabauseinit_struct));
   yinit.percoretype = percoretype;
   yinit.sh2coretype = sh2coretype;
   yinit.vidcoretype = vidcoretype;
   yinit.sndcoretype = sndcoretype;
   if (IsPathCdrom(cdrompath))
      yinit.cdcoretype = CDCORE_SPTI;
   else
      yinit.cdcoretype = CDCORE_ISO;
   //yinit.m68kcoretype = M68KCORE_HLE;
#ifdef NOC68K
   yinit.m68kcoretype = M68KCORE_DEFAULT;
#else
   yinit.m68kcoretype = M68KCORE_C68K;
#endif
   yinit.carttype = carttype;
   yinit.regionid = regionid;
   if (strcmp(biosfilename, "") == 0)
      yinit.biospath = NULL;
   else
      yinit.biospath = biosfilename;
   yinit.cdpath = cdrompath;
   yinit.buppath = backupramfilename;
   yinit.mpegpath = mpegromfilename;
   yinit.cartpath = cartfilename;
   yinit.netlinksetting = netlinksetting;
   yinit.videoformattype = VIDEOFORMATTYPE_NTSC;

   if (GetPrivateProfileStringA("General", "CDROMDrive", "", cdrompath, MAX_PATH, inifilename) != 0) {

   if ((ret = YabauseInit(&yinit)) < 0)
   {
      if (ret == -2)
      {
         nocorechange = 1;

         ret = (int)SettingsCreatePropertySheets(NULL, TRUE, &settingspsp);
         free(settingspsp.psp);
         memset(&settingspsp, 0, sizeof(settingspsp));

         if (ret != TRUE)
         {
            // exit program with error
            MessageBox (NULL, (LPCWSTR)_16("yabause.ini must be properly setup before program can be used."), (LPCWSTR)_16("Error"),  MB_OK | MB_ICONINFORMATION);
            return -1;
         }

         YuiReleaseVideo();
         YabauseDeInit();

         goto YabauseSetup;
      }
      return -1;
   }

   if (usefullscreenonstartup)
      VIDCore->Resize(fullscreenwidth, fullscreenheight, 1);
   else if (usecustomwindowsize)
      VIDCore->Resize(windowwidth, windowheight, 0);

   AlreadyStarted=true;
   }
   else {

      SetMenu(YabWin, YabMenu);

      ShowWindow(YabWin,SW_SHOW);
      SetForegroundWindow(YabWin);
      SetFocus(YabWin);
   }

   PERDXLoadDevices(inifilename);

   stop = 0;

   ScspSetVolume(sndvolume);

   if (enableautofskip)
      EnableAutoFrameSkip();
   else
      DisableAutoFrameSkip();

   if (vidcoretype == VIDCORE_DUMMY)
   {
      SetMenu(YabWin, YabMenu);

      ShowWindow(YabWin,SW_SHOW);
      SetForegroundWindow(YabWin);
      SetFocus(YabWin);
   }

   if (AutoRWLoad)
   {
	  //Open Ram Watch if its auto-load setting is checked
	  OpenRWRecentFile(0);	
	  RamWatchHWnd = CreateDialog(y_hInstance, MAKEINTRESOURCE(IDD_RAMWATCH), YabWin, (DLGPROC) RamWatchProc);	
   }

   if (loadexec)
      MappedMemoryLoadExec(filename, addr);

   if ((strcmp (cdrompath,"") == 0)) {
   paused=true;
   }

   while (!stop)
   {
      if (PeekMessage(&msg,NULL,0,0,PM_REMOVE))
      {
         if (RamWatchHWnd && IsDialogMessage(RamWatchHWnd, &msg))
		 {
			if(msg.message == WM_KEYDOWN) // send keydown messages to the dialog (for accelerators, and also needed for the Alt key to work)
				SendMessage(RamWatchHWnd, msg.message, msg.wParam, msg.lParam);
			continue;
		 }

		 if (TranslateAccelerator(YabWin, hAccel, &msg) == 0)
         {
            TranslateMessage(&msg);
            DispatchMessage(&msg);
         }
      }

      if (!paused && PERCore->HandleEvents() != 0)
      {
         YuiReleaseVideo();
         if (YabMenu)
            DestroyMenu(YabMenu);
         return -1;
      }
   }

#else

   KillEmuThread=0;
   emuthread = CreateThread(NULL, 0, YabauseEmulate, &KillEmuThread, 0, NULL);

   while (GetMessage(&msg,NULL,0,0))
   {
      if (TranslateAccelerator(YabWin, hAccel, &msg) == 0)
      {
         TranslateMessage(&msg);
         DispatchMessage(&msg);
      }
   }

   if (emuthread != INVALID_HANDLE_VALUE)
   {
      // If the playback thread is going
      KillEmuThread=1;     // Set the flag telling it to stop
      if (WaitForSingleObject(emuthread,INFINITE) == WAIT_TIMEOUT)
      {
         // Couldn't close thread cleanly
         TerminateThread(emuthread,0);
      }
      CloseHandle(emuthread);
      emuthread = INVALID_HANDLE_VALUE;
   }
#endif

   YuiReleaseVideo();
   if (YabMenu)
      DestroyMenu(YabMenu);

   sprintf(tempstr, "%d", yabwinx);
   WritePrivateProfileStringA("General", "WindowX", tempstr, inifilename);
   sprintf(tempstr, "%d", yabwiny);
   WritePrivateProfileStringA("General", "WindowY", tempstr, inifilename);

   if (argv)
      LocalFree(argv);

   return 0;
}

extern "C" void StartGame(){

	if(!AlreadyStarted) {

	YabauseInit(&yinit);
	paused=false;
	VideoChangeCore(vidcoretype);

	if (VIDCore && !VIDCore->IsFullscreen() && usecustomwindowsize)
		VIDCore->Resize(windowwidth, windowheight, 0);
	AlreadyStarted=1;
	}
	
}

//////////////////////////////////////////////////////////////////////////////

void ClearMenuChecks(HMENU hmenu, int startid, int endid)
{
   int i;

   for (i = startid; i <= endid; i++)
      CheckMenuItem(hmenu, i, MF_UNCHECKED);
}

//////////////////////////////////////////////////////////////////////////////

void ChangeLanguage(int id)
{
#ifdef HAVE_LIBMINI18N
   static char *langfiles[] = {
      "yabause_de.yts",
      "yabause_en.yts",
      "yabause_fr.yts",
      "yabause_it.yts",
      "yabause_pt.yts",
      "yabause_pt_BR.yts",
      "yabause_es.yts",
      "yabause_sv.yts"
   };

   if (langfiles[id-IDM_GERMAN])
   {
      if (mini18n_set_locale(langfiles[id-IDM_GERMAN]) == -1)
         return;
   }

   ClearMenuChecks(YabMenu, IDM_GERMAN, IDM_SPANISH);
   CheckMenuItem(YabMenu, id, MF_CHECKED);
#endif
}

//////////////////////////////////////////////////////////////////////////////
LRESULT CALLBACK WindowProc(HWND hWnd,UINT uMsg,WPARAM wParam,LPARAM lParam)
{
   DIDEVCAPS didc;
   int i;
   char text[MAX_PATH];

   switch (uMsg)
   {
      case WM_COMMAND:
      {
         switch (LOWORD(wParam))
         {
            case IDM_MEMTRANSFER:
            {
               YuiTempPause();
               DialogBox(y_hInstance, MAKEINTRESOURCE(IDD_MEMTRANSFER), hWnd, (DLGPROC)MemTransferDlgProc);
               YuiTempUnPause();
               break;
            }
          /*  case IDM_RUN:
            {
               YuiUnPause();
               EnableMenuItem(YabMenu, IDM_RUN, MF_GRAYED);
               EnableMenuItem(YabMenu, IDM_PAUSE, MF_ENABLED);
               break;
            }	
            */
			case IDM_PAUSE:
            {
               TogglePause();
               break;
            }
            case IDM_RESET:
            {
               ResetGame();
               break;
            }
            case IDM_HARDRESET:
            {
               HardResetGame();
               break;
            }
            case IDM_CHEATSEARCH:
            {
               YuiTempPause();
               DialogBox(y_hInstance, MAKEINTRESOURCE(IDD_CHEATSEARCH), hWnd, (DLGPROC)CheatSearchDlgProc);
               YuiTempUnPause();
               break;
            }
            case IDM_CHEATLIST:
            {
               YuiTempPause();
               DialogBox(y_hInstance, MAKEINTRESOURCE(IDD_CHEATLIST), hWnd, (DLGPROC)CheatListDlgProc);
               YuiTempUnPause();
               break;
            }
            case IDM_SETTINGS:
            {
               YuiTempPause();
               SettingsCreatePropertySheets(hWnd, TRUE, &settingspsp);
               free(settingspsp.psp);
               memset(&settingspsp, 0, sizeof(settingspsp));
               YuiTempUnPause();
               break;
			}
			case ID_RAM_SEARCH:
				if(!RamSearchHWnd)
				{
					InitRamSearch();
					RamSearchHWnd = CreateDialog(y_hInstance, MAKEINTRESOURCE(IDD_RAMSEARCH), hWnd, (DLGPROC) RamSearchProc);
				}
				else
					SetForegroundWindow(RamSearchHWnd);
				break;

			case IDM_HOTKEY_CONFIG:
				{
					YuiTempPause();
#ifdef USEHOTKEY
					DialogBox(y_hInstance, MAKEINTRESOURCE(IDD_KEYCUSTOM), hWnd, DlgHotkeyConfig);
#endif
					YuiTempUnPause();
				}
				break;
			case IDM_BACKUPRAMMANAGER:
				{
					YuiTempPause();
					DialogBox(y_hInstance, MAKEINTRESOURCE(IDD_BACKUPRAM), hWnd, (DLGPROC)BackupRamDlgProc);
               YuiTempUnPause();
               break;
            }
			case IDM_OPENCUEISO:
				{
					YuiTempPause();

					WCHAR tempwstr[MAX_PATH];
					WCHAR filter[1024];
					OPENFILENAME ofn;

					// setup ofn structure
					ZeroMemory(&ofn, sizeof(OPENFILENAME));
					ofn.lStructSize = sizeof(OPENFILENAME);
					ofn.hwndOwner = hWnd;

					CreateFilter(filter, 1024,
						"Supported image files (*.cue, *.iso)", "*.cue;*.iso",
						"Cue files (*.cue)", "*.cue",
						"Iso files (*.iso)", "*.iso",
						"All files (*.*)", "*.*", NULL);

					ofn.lpstrFilter = filter;
					GetDlgItemText(hWnd, IDC_IMAGEEDIT, tempwstr, MAX_PATH);
					ofn.lpstrFile = tempwstr;
					ofn.nMaxFile = sizeof(tempwstr);
					ofn.Flags = OFN_FILEMUSTEXIST;

					if (GetOpenFileName(&ofn))
					{
						char tempstr[512];

						WideCharToMultiByte(CP_ACP, 0, tempwstr, -1, tempstr, MAX_PATH, NULL, NULL);

						if (strcmp(tempstr, cdrompath) != 0)
						{
							strcpy(cdrompath, tempstr);
					//		cdromchanged = TRUE;
						}
						StartGame();

					WritePrivateProfileStringA("General", "CDROMDrive", cdrompath, inifilename);
#ifndef USETHREADS
					Cs2ChangeCDCore(CDCORE_ISO, cdrompath);
#else
					corechanged = 0;
					changecore |= 1;
					while (corechanged == 0) { Sleep(0); }
#endif
					YabauseReset();
					}

					YuiTempUnPause();
				}
				break;
            case IDM_GERMAN:
            case IDM_ENGLISH:
            case IDM_FRENCH:
            case IDM_ITALIAN:
            case IDM_PORTUGUESE:
            case IDM_PORTUGUESEBRAZIL:
            case IDM_SPANISH:
            case IDM_SWEDISH:
               ChangeLanguage(LOWORD(wParam));
               break;
            case IDM_MSH2DEBUG:
            {
               YuiTempPause();
               debugsh = MSH2;
               DialogBox(y_hInstance, MAKEINTRESOURCE(IDD_SH2DEBUG), hWnd, (DLGPROC)SH2DebugDlgProc);
               YuiTempUnPause();
               break;
            }
            case IDM_SSH2DEBUG:
            {
               YuiTempPause();
               debugsh = SSH2;
               DialogBox(y_hInstance, MAKEINTRESOURCE(IDD_SH2DEBUG), hWnd, (DLGPROC)SH2DebugDlgProc);
               YuiTempUnPause();
               break;
            }
            case IDM_VDP1DEBUG:
            {
               YuiTempPause();
               DialogBox(y_hInstance, MAKEINTRESOURCE(IDD_VDP1DEBUG), hWnd, (DLGPROC)VDP1DebugDlgProc);
               YuiTempUnPause();
               break;
            }
            case IDM_VDP2DEBUG:
            {
               YuiTempPause();
               DialogBox(y_hInstance, MAKEINTRESOURCE(IDD_VDP2DEBUG), hWnd, (DLGPROC)VDP2DebugDlgProc);
               YuiTempUnPause();
               break;
            }
            case IDM_M68KDEBUG:
            {
               YuiTempPause();
               DialogBox(y_hInstance, MAKEINTRESOURCE(IDD_M68KDEBUG), hWnd, (DLGPROC)M68KDebugDlgProc);
               YuiTempUnPause();
               break;
            }
            case IDM_SCUDSPDEBUG:
            {
               YuiTempPause();
               DialogBox(y_hInstance, MAKEINTRESOURCE(IDD_SCUDSPDEBUG), hWnd, (DLGPROC)SCUDSPDebugDlgProc);
               YuiTempUnPause();
               break;
            }
		case ID_RAM_WATCH:
			if(!RamWatchHWnd)
			{
				RamWatchHWnd = CreateDialog(y_hInstance, MAKEINTRESOURCE(IDD_RAMWATCH), hWnd, (DLGPROC) RamWatchProc);
			}
			else
				SetForegroundWindow(RamWatchHWnd);
			break;
            case IDM_SCSPDEBUG:
            {
               YuiTempPause();
               DialogBox(y_hInstance, MAKEINTRESOURCE(IDD_SCSPDEBUG), hWnd, (DLGPROC)SCSPDebugDlgProc);
               YuiTempUnPause();
               break;
            }
            case IDM_SMPCDEBUG:
            {
               YuiTempPause();
               DialogBox(y_hInstance, MAKEINTRESOURCE(IDD_SMPCDEBUG), hWnd, (DLGPROC)SMPCDebugDlgProc);
               YuiTempUnPause();
               break;
            }
            case IDM_MEMORYEDITOR:
            {
               YuiTempPause();
               DialogBox(y_hInstance, MAKEINTRESOURCE(IDD_MEMORYEDITOR), hWnd, (DLGPROC)MemoryEditorDlgProc);
               YuiTempUnPause();
               break;
            }
            case IDM_TOGGLEFULLSCREEN:
            {
               // Normally I should be using the function provided in vdp2.c,
               // but it doesn't support odd custom resolutions.
               if (isfullscreenset)
                  VIDCore->Resize(windowwidth, windowheight, 0);
               else
                  VIDCore->Resize(fullscreenwidth, fullscreenheight, 1);

               break;
            }
            case IDM_TOGGLENBG0:
            {
               ToggleNBG0();
               break;
            }
            case IDM_TOGGLENBG1:
            {
               ToggleNBG1();
               break;
            }
            case IDM_TOGGLENBG2:
            {
               ToggleNBG2();
               break;
            }
            case IDM_TOGGLENBG3:
            {
               ToggleNBG3();
               break;
            }
            case IDM_TOGGLERBG0:
            {
               ToggleRBG0();
               break;
            }
            case IDM_TOGGLEVDP1:
            {
               ToggleVDP1();
               break;
            }
            case IDM_TOGGLEFPS:
            {
               ToggleFPS();
               break;
            }
            case IDM_SAVESTATEAS:
            {
               WCHAR filter[1024];
               OPENFILENAME ofn;

               YuiTempPause();

               CreateFilter(filter, 1024,
                  "Yabause Save State files", "*.YSS",
                  "All files (*.*)", "*.*", NULL);

               SetupOFN(&ofn, OFN_DEFAULTSAVE, hWnd, filter,
                        yssfilename, sizeof(yssfilename)/sizeof(TCHAR));
               ofn.lpstrDefExt = (LPCWSTR)_16("YSS");

               if (GetSaveFileName(&ofn))
               {
                  WideCharToMultiByte(CP_ACP, 0, yssfilename, -1, text, sizeof(text), NULL, NULL);
                  if (YabSaveState(text) != 0)
                     MessageBox (hWnd, (LPCWSTR)_16("Couldn't save state file"), (LPCWSTR)_16("Error"),  MB_OK | MB_ICONINFORMATION);
               }
               YuiTempUnPause();
               break;
            }
            case IDM_LOADSTATEAS:
            {
               WCHAR filter[1024];
               OPENFILENAME ofn;

               YuiTempPause();

               CreateFilter(filter, 1024,
                  "Yabause Save State files", "*.YSS",
                  "All files (*.*)", "*.*", NULL);

               SetupOFN(&ofn, OFN_DEFAULTLOAD, hWnd, filter,
                        yssfilename, sizeof(yssfilename)/sizeof(TCHAR));

               if (GetOpenFileName(&ofn))
               {
                  WideCharToMultiByte(CP_ACP, 0, yssfilename, -1, text, sizeof(text), NULL, NULL);
                  if (YabLoadState(text) != 0)
                     MessageBox (hWnd, (LPCWSTR)_16("Couldn't load state file"), (LPCWSTR)_16("Error"),  MB_OK | MB_ICONINFORMATION);
               }
               YuiTempUnPause();

               break;
            }
			case IDM_FILE_RECORDAVI:
				YuiRecordAvi(hWnd);
				break;
			case IDM_FILE_STOPAVI:
				YuiStopAvi();
				break;
			case MENU_RECORD_MOVIE:
				YuiRecordMovie(hWnd);
				break;
			case MENU_PLAY_MOVIE:
               YuiPlayMovie(hWnd);
			   break;
			case MENU_STOP_MOVIE:
				StopMovie();
				break;
			case IDM_TOGGLEREADONLY:
				MovieToggleReadOnly();
				break;
			case IDM_FRAMEADVANCEPAUSE:
				PauseOrUnpause();
				break;
            case IDM_SAVESTATE_F2:
            case IDM_SAVESTATE_F3:
            case IDM_SAVESTATE_F4:
            case IDM_SAVESTATE_F5:
            case IDM_SAVESTATE_F6:
            case IDM_SAVESTATE_F7:
            case IDM_SAVESTATE_F8:
            case IDM_SAVESTATE_F9:
            case IDM_SAVESTATE_F10:
               YuiTempPause();
               if (YabSaveStateSlot(ysspath, LOWORD(wParam)-IDM_SAVESTATE_F2) != 0)
                  MessageBox (hWnd, (LPCWSTR)_16("Couldn't save state file"), (LPCWSTR)_16("Error"),  MB_OK | MB_ICONINFORMATION);
               YuiTempUnPause();
               break;
            case IDM_LOADSTATE_F2:
            case IDM_LOADSTATE_F3:
            case IDM_LOADSTATE_F4:
            case IDM_LOADSTATE_F5:
            case IDM_LOADSTATE_F6:
            case IDM_LOADSTATE_F7:
            case IDM_LOADSTATE_F8:
            case IDM_LOADSTATE_F9:
            case IDM_LOADSTATE_F10:
               YuiTempPause();
               if (YabLoadStateSlot(ysspath, LOWORD(wParam)-IDM_LOADSTATE_F2) != 0)
                  MessageBox (hWnd, (LPCWSTR)_16("Couldn't load state file"), (LPCWSTR)_16("Error"),  MB_OK | MB_ICONINFORMATION);
               YuiTempUnPause();
               break;
            case IDM_CAPTURESCREEN:
				YuiScreenshot(hWnd);
				break;
            case IDM_EXIT:
            {
               ScspMuteAudio(SCSP_MUTE_SYSTEM);
               PostMessage(hWnd, WM_CLOSE, 0, 0);
               break;
            }
			case IDM_WEBSITE:
            {
               ShellExecuteA(NULL, "open", "http://yabause.sourceforge.net", NULL, NULL, SW_SHOWNORMAL);
               break;
            }
            case IDM_FORUM:
            {
               ShellExecuteA(NULL, "open", "http://yabause.sourceforge.net/forums/", NULL, NULL, SW_SHOWNORMAL);
               break;
            }
            case IDM_SUBMITBUGREPORT:
            {
               ShellExecuteA(NULL, "open", "http://sourceforge.net/tracker/?func=add&group_id=89991&atid=592126", NULL, NULL, SW_SHOWNORMAL);
               break;
            }
            case IDM_DONATE:
            {
               ShellExecuteA(NULL, "open", "https://sourceforge.net/donate/index.php?group_id=89991", NULL, NULL, SW_SHOWNORMAL);
               break;
            }
            case IDM_COMPATLIST:
            {
               ShellExecuteA(NULL, "open", "http://www.emu-compatibility.com/yabause/index.php?lang=uk", NULL, NULL, SW_SHOWNORMAL);
               break;
            }			
            case IDM_ABOUT:
            {
               YuiTempPause();
               DialogBox(y_hInstance, MAKEINTRESOURCE(IDD_ABOUT), hWnd, (DLGPROC)AboutDlgProc);
               YuiTempUnPause();
               break;
            }
         }

         return 0L;
      }

	case WM_KEYDOWN:
		//	if(wParam != VK_PAUSE)
		//		break;
	case WM_SYSKEYDOWN:
#ifdef USEHOTKEY
	case WM_CUSTKEYDOWN:
		{
			int modifiers = GetModifiers(wParam);
			if(!HandleKeyMessage(wParam,lParam, modifiers))
				return 0;
			break;
		}
	case WM_KEYUP:
		//	if(wParam != VK_PAUSE)
		//		break;
	case WM_SYSKEYUP:
	case WM_CUSTKEYUP:
		{
			int modifiers = GetModifiers(wParam);
			HandleKeyUp(wParam, lParam, modifiers);
		}
		break;
#endif
      case WM_ENTERMENULOOP:
      {
#ifndef USETHREADS
         ScspMuteAudio(SCSP_MUTE_SYSTEM);
#endif
         for (i = 0; i < 12; i++)
         {
            if (paddevice[i].lpDIDevice)
            {
               didc.dwSize = sizeof(DIDEVCAPS);

               if (IDirectInputDevice8_GetCapabilities(paddevice[i].lpDIDevice, &didc) != DI_OK)
                  continue;

               if (GET_DIDEVICE_TYPE(didc.dwDevType) == DI8DEVTYPE_MOUSE)
                  IDirectInputDevice8_Unacquire(paddevice[i].lpDIDevice);
            }
         }

		EnableMenuItem(YabMenu, IDM_FILE_RECORDAVI, MF_BYCOMMAND | (!DRV_AviIsRecording())? MF_ENABLED : MF_GRAYED);
		EnableMenuItem(YabMenu, IDM_FILE_STOPAVI,   MF_BYCOMMAND | (DRV_AviIsRecording()) ? MF_ENABLED : MF_GRAYED);
		EnableMenuItem(YabMenu, MENU_RECORD_MOVIE,  MF_BYCOMMAND | (!IsMovieLoaded())     ? MF_ENABLED : MF_GRAYED);
		EnableMenuItem(YabMenu, MENU_PLAY_MOVIE,    MF_BYCOMMAND | (!IsMovieLoaded())     ? MF_ENABLED : MF_GRAYED);
		EnableMenuItem(YabMenu, MENU_STOP_MOVIE,    MF_BYCOMMAND | (IsMovieLoaded())      ? MF_ENABLED : MF_GRAYED);
		EnableMenuItem(YabMenu, IDM_RESET,    MF_BYCOMMAND | (AlreadyStarted)      ? MF_ENABLED : MF_GRAYED);
		EnableMenuItem(YabMenu, IDM_HARDRESET,    MF_BYCOMMAND | (AlreadyStarted)      ? MF_ENABLED : MF_GRAYED);
		EnableMenuItem(YabMenu, IDM_MSH2DEBUG,    MF_BYCOMMAND | (AlreadyStarted)      ? MF_ENABLED : MF_GRAYED);
		EnableMenuItem(YabMenu, IDM_SSH2DEBUG,    MF_BYCOMMAND | (AlreadyStarted)      ? MF_ENABLED : MF_GRAYED);
		EnableMenuItem(YabMenu, IDM_VDP1DEBUG,    MF_BYCOMMAND | (AlreadyStarted)      ? MF_ENABLED : MF_GRAYED);
		EnableMenuItem(YabMenu, IDM_VDP2DEBUG,    MF_BYCOMMAND | (AlreadyStarted)      ? MF_ENABLED : MF_GRAYED);
		EnableMenuItem(YabMenu, IDM_M68KDEBUG,    MF_BYCOMMAND | (AlreadyStarted)      ? MF_ENABLED : MF_GRAYED);
		EnableMenuItem(YabMenu, IDM_SCUDSPDEBUG,    MF_BYCOMMAND | (AlreadyStarted)      ? MF_ENABLED : MF_GRAYED);
		EnableMenuItem(YabMenu, IDM_SCSPDEBUG,    MF_BYCOMMAND | (AlreadyStarted)      ? MF_ENABLED : MF_GRAYED);
		EnableMenuItem(YabMenu, IDM_SMPCDEBUG,    MF_BYCOMMAND | (AlreadyStarted)      ? MF_ENABLED : MF_GRAYED);
		EnableMenuItem(YabMenu, IDM_MEMORYEDITOR,    MF_BYCOMMAND | (AlreadyStarted)      ? MF_ENABLED : MF_GRAYED);
		EnableMenuItem(YabMenu, IDM_BACKUPRAMMANAGER,    MF_BYCOMMAND | (AlreadyStarted)      ? MF_ENABLED : MF_GRAYED);
		EnableMenuItem(YabMenu, IDM_SAVESTATE_F2,    MF_BYCOMMAND | (AlreadyStarted)      ? MF_ENABLED : MF_GRAYED);
		EnableMenuItem(YabMenu, IDM_SAVESTATE_F3,    MF_BYCOMMAND | (AlreadyStarted)      ? MF_ENABLED : MF_GRAYED);
		EnableMenuItem(YabMenu, IDM_SAVESTATE_F4,    MF_BYCOMMAND | (AlreadyStarted)      ? MF_ENABLED : MF_GRAYED);
		EnableMenuItem(YabMenu, IDM_SAVESTATE_F5,    MF_BYCOMMAND | (AlreadyStarted)      ? MF_ENABLED : MF_GRAYED);
		EnableMenuItem(YabMenu, IDM_SAVESTATE_F6,    MF_BYCOMMAND | (AlreadyStarted)      ? MF_ENABLED : MF_GRAYED);
		EnableMenuItem(YabMenu, IDM_SAVESTATE_F7,    MF_BYCOMMAND | (AlreadyStarted)      ? MF_ENABLED : MF_GRAYED);
		EnableMenuItem(YabMenu, IDM_SAVESTATE_F8,    MF_BYCOMMAND | (AlreadyStarted)      ? MF_ENABLED : MF_GRAYED);
		EnableMenuItem(YabMenu, IDM_SAVESTATE_F9,    MF_BYCOMMAND | (AlreadyStarted)      ? MF_ENABLED : MF_GRAYED);
		EnableMenuItem(YabMenu, IDM_SAVESTATE_F10,    MF_BYCOMMAND | (AlreadyStarted)      ? MF_ENABLED : MF_GRAYED);
		EnableMenuItem(YabMenu, IDM_LOADSTATE_F2,    MF_BYCOMMAND | (AlreadyStarted)      ? MF_ENABLED : MF_GRAYED);
		EnableMenuItem(YabMenu, IDM_LOADSTATE_F3,    MF_BYCOMMAND | (AlreadyStarted)      ? MF_ENABLED : MF_GRAYED);
		EnableMenuItem(YabMenu, IDM_LOADSTATE_F4,    MF_BYCOMMAND | (AlreadyStarted)      ? MF_ENABLED : MF_GRAYED);
		EnableMenuItem(YabMenu, IDM_LOADSTATE_F5,    MF_BYCOMMAND | (AlreadyStarted)      ? MF_ENABLED : MF_GRAYED);
		EnableMenuItem(YabMenu, IDM_LOADSTATE_F6,    MF_BYCOMMAND | (AlreadyStarted)      ? MF_ENABLED : MF_GRAYED);
		EnableMenuItem(YabMenu, IDM_LOADSTATE_F7,    MF_BYCOMMAND | (AlreadyStarted)      ? MF_ENABLED : MF_GRAYED);
		EnableMenuItem(YabMenu, IDM_LOADSTATE_F8,    MF_BYCOMMAND | (AlreadyStarted)      ? MF_ENABLED : MF_GRAYED);
		EnableMenuItem(YabMenu, IDM_LOADSTATE_F9,    MF_BYCOMMAND | (AlreadyStarted)      ? MF_ENABLED : MF_GRAYED);
		EnableMenuItem(YabMenu, IDM_LOADSTATE_F10,    MF_BYCOMMAND | (AlreadyStarted)      ? MF_ENABLED : MF_GRAYED);
		EnableMenuItem(YabMenu, IDM_SAVESTATEAS,    MF_BYCOMMAND | (AlreadyStarted)      ? MF_ENABLED : MF_GRAYED);
		EnableMenuItem(YabMenu, IDM_LOADSTATEAS,    MF_BYCOMMAND | (AlreadyStarted)      ? MF_ENABLED : MF_GRAYED);
		EnableMenuItem(YabMenu, MENU_RECORD_MOVIE,    MF_BYCOMMAND | (AlreadyStarted)      ? MF_ENABLED : MF_GRAYED);
		EnableMenuItem(YabMenu, MENU_PLAY_MOVIE,    MF_BYCOMMAND | (AlreadyStarted)      ? MF_ENABLED : MF_GRAYED);
		EnableMenuItem(YabMenu, IDM_FILE_RECORDAVI,    MF_BYCOMMAND | (AlreadyStarted)      ? MF_ENABLED : MF_GRAYED);
		EnableMenuItem(YabMenu, ID_RAM_WATCH,    MF_BYCOMMAND | (AlreadyStarted)      ? MF_ENABLED : MF_GRAYED);
		EnableMenuItem(YabMenu, ID_RAM_SEARCH,    MF_BYCOMMAND | (AlreadyStarted)      ? MF_ENABLED : MF_GRAYED);
		EnableMenuItem(YabMenu, IDM_MEMTRANSFER,    MF_BYCOMMAND | (AlreadyStarted)      ? MF_ENABLED : MF_GRAYED);
		
		CheckMenuItem(YabMenu, IDM_PAUSE, FrameAdvanceVariable ? MF_CHECKED:MF_UNCHECKED);
		
		return 0L;
      }
      case WM_EXITMENULOOP:
      {
#ifndef USETHREADS
         ScspUnMuteAudio(SCSP_MUTE_SYSTEM);
#endif
         for (i = 0; i < 12; i++)
         {
            if (paddevice[i].lpDIDevice)
            {
               didc.dwSize = sizeof(DIDEVCAPS);

               if (IDirectInputDevice8_GetCapabilities(paddevice[i].lpDIDevice, &didc) != DI_OK)
                  continue;

               if (GET_DIDEVICE_TYPE(didc.dwDevType) == DI8DEVTYPE_MOUSE)
                  IDirectInputDevice8_Acquire(paddevice[i].lpDIDevice);
            }
         }
         return 0L;
      }
      case WM_MOVE:
      {
         RECT rect;
         WINDOWPLACEMENT info;

         GetWindowPlacement(hWnd, &info);
         if (info.showCmd != SW_SHOWMINIMIZED)
         {
            GetWindowRect(hWnd, &rect);
            yabwinx = rect.left;
            yabwiny = rect.top;
         }
         return 0L;
      }
      case WM_CLOSE:
      {
         if (AskSave())
		 {
			 stop = 1;
			 PostQuitMessage(0);
			 WriteToINI();
			 return 0L;
		 }
      }
      case WM_SIZE:
      {
         return 0L;
      }
      case WM_PAINT:
      {
         PAINTSTRUCT ps;

         BeginPaint(hWnd, &ps);
         EndPaint(hWnd, &ps);
         return 0L;
      }
      case WM_DESTROY:
         if (AskSave())
		 {
			stop = 1;
			PostQuitMessage(0);
			WriteToINI();
			return 0L;
		 }
    }

    return DefWindowProc(hWnd, uMsg, wParam, lParam);
}

//////////////////////////////////////////////////////////////////////////////

LRESULT CALLBACK AboutDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam,
                              LPARAM lParam)
{
   char tempstr[256];

   switch (uMsg)
   {
      case WM_INITDIALOG:
         sprintf(tempstr, "Yabause v%s", VERSION);
         SetDlgItemText(hDlg, IDC_VERSIONTEXT, (LPCWSTR)_16(tempstr));
         return TRUE;
      case WM_COMMAND:
      {
         switch (LOWORD(wParam))
         {
            case IDOK:
            {
               EndDialog(hDlg, TRUE);

               return TRUE;
            }
            default: break;
         }
         break;
      }
      case WM_CLOSE:
      {
         EndDialog(hDlg, TRUE);

         return TRUE;
      }
      default: break;
   }

   return FALSE;
}

//////////////////////////////////////////////////////////////////////////////

int PASCAL WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance,
                   LPSTR lpCmdLine, int nCmdShow)
{
#ifdef HAVE_LIBMINI18N
   mini18n_set_domain("trans");
#endif

#ifdef USEHOTKEY
   InitCustomControls();
   InitCustomKeys(&CustomKeys);
#endif

   if (YuiInit(lpCmdLine) != 0)
      fprintf(stderr, "Error running Yabause\n");

   YabauseDeInit();
   PROFILE_PRINT();
#if DEBUG
   LogStop();
   if (LogWin)
   {
      RECT rect;
      char text[10];

      // Remember log window position
      GetWindowRect(LogWin, &rect);
      sprintf(text, "%ld", rect.left);
      WritePrivateProfileStringA("Log", "WindowX", text, inifilename);
      sprintf(text, "%ld", rect.top);
      WritePrivateProfileStringA("Log", "WindowY", text, inifilename);

      DestroyWindow(LogWin);
   }
#endif
   return 0;
}

//adelikat
//All WritePrivateProfile calls should go here and be called only on Yabause close (or a possible save config menu option)
void WriteToINI()
{
	char text[10];
	
	//RamWatch
	WritePrivateProfileStringA("RamWatch", "AutoLoad", AutoRWLoad ? "1" : "0", inifilename);
	WritePrivateProfileStringA("RamWatch", "SaveWindowPos", RWSaveWindowPos ? "1" : "0", inifilename);
	sprintf(text, "%ld", ramw_x);
	WritePrivateProfileStringA("RamWatch", "Ram_x", text, inifilename);
	sprintf(text, "%ld", ramw_y);
	WritePrivateProfileStringA("RamWatch", "Ram_y", text, inifilename);
	for(int i = 0; i < MAX_RECENT_WATCHES; i++)
	{
		char str[256];
		sprintf(str, "Recent Watch %d", i+1);
		WritePrivateProfileStringA("Watches", str, &rw_recent_files[i][0], inifilename);	
	}

	//OSD Display
	//extern int fpstoggle;
	sprintf(text, "%1d", GetOSDToggle());
	WritePrivateProfileStringA("Video", "OSD Display", text, inifilename);
}

//////////////////////////////////////////////////////////////////////////////

void ResetGame()
{
	YuiTempPause();
    YabauseResetButton();
    YuiTempUnPause();
}

void HardResetGame()
{
   YuiTempPause();

   yinit.percoretype = percoretype;
   yinit.sh2coretype = sh2coretype;
   yinit.vidcoretype = vidcoretype;
   yinit.sndcoretype = sndcoretype;
   yinit.carttype = carttype;
   yinit.regionid = regionid;
   yinit.biospath = biosfilename;
   yinit.cdpath = cdrompath;
   yinit.buppath = backupramfilename;
   yinit.mpegpath = mpegromfilename;
   yinit.cartpath = cartfilename;
   yinit.netlinksetting = netlinksetting;

   YabauseInit(&yinit);
   VideoChangeCore(vidcoretype);

   if (VIDCore && !VIDCore->IsFullscreen() && usecustomwindowsize)
      VIDCore->Resize(windowwidth, windowheight, 0);
   YabauseReset();
   YuiTempUnPause();
}

void SaveState(int num) {

	YuiTempPause();
	if (YabSaveStateSlot(ysspath, num) != 0)
		MessageBox (YabWin, (LPCWSTR)_16("Couldn't save state file"), (LPCWSTR)_16("Error"),  MB_OK | MB_ICONINFORMATION);
	YuiTempUnPause();
}

void LoadState(int num) {

	YuiTempPause();
	if (YabLoadStateSlot(ysspath, num) != 0)
		MessageBox (YabWin, (LPCWSTR)_16("Couldn't load state file"), (LPCWSTR)_16("Error"),  MB_OK | MB_ICONINFORMATION);
	Update_RAM_Search();
	Update_RAM_Watch();
	YuiTempUnPause();
}

void YuiPlayMovie(HWND hWnd) 
{
	char text[MAX_PATH];
	WCHAR filter[1024];
	OPENFILENAME ofn;

	YuiTempPause();
	CreateFilter(filter, 1024,
		"Yabause Movie files", "*.YMV",
		"All files (*.*)", "*.*", NULL);
	SetupOFN(&ofn, OFN_DEFAULTLOAD, hWnd, filter,
	ymvfilename, sizeof(ymvfilename)/sizeof(TCHAR));

	if (GetOpenFileName(&ofn))
	{
		WideCharToMultiByte(CP_ACP, 0, ymvfilename, -1, text, sizeof(text), NULL, NULL);
		PlayMovie(text);
	}
	YuiTempUnPause();
}

void YuiRecordMovie(HWND hWnd)
{
	char text[MAX_PATH];
	WCHAR filter[1024];
	OPENFILENAME ofn;

	YuiTempPause();
	CreateFilter(filter, 1024,
		"Yabause Movie file", "*.YMV",
		"All files (*.*)", "*.*", NULL);

	SetupOFN(&ofn, OFN_DEFAULTSAVE, hWnd, filter,
	ymvfilename, sizeof(ymvfilename)/sizeof(TCHAR));
	ofn.lpstrDefExt = (LPCWSTR)_16("YMV");

	if (GetSaveFileName(&ofn))
	{
		WideCharToMultiByte(CP_ACP, 0, ymvfilename, -1, text, sizeof(text), NULL, NULL);
		SaveMovie(text);
	}
}

void ToggleFullScreenHK() {

	// Normally I should be using the function provided in vdp2.c,
	// but it doesn't support odd custom resolutions.
	if (isfullscreenset)
		VIDCore->Resize(windowwidth, windowheight, 0);
	else
		VIDCore->Resize(fullscreenwidth, fullscreenheight, 1);
}
					
void YuiScreenshot(HWND hWnd)
{
	OPENFILENAME ofn;
    char text[MAX_PATH];        
	WCHAR filter[1024];
	YuiTempPause();

	CreateFilter(filter, 1024,
	  "Bitmap Files", "*.BMP",
	  "All files (*.*)", "*.*", NULL);

	SetupOFN(&ofn, OFN_DEFAULTSAVE, hWnd, filter,
		   bmpfilename, sizeof(bmpfilename)/sizeof(TCHAR));
	ofn.lpstrDefExt = (LPCWSTR)_16("BMP");

	if (GetSaveFileName(&ofn))
	{
	  WideCharToMultiByte(CP_ACP, 0, bmpfilename, -1, text, sizeof(text), NULL, NULL);
	  if (YuiCaptureScreen(text))
		 MessageBox (hWnd, (LPCWSTR)_16("Couldn't save capture file"), (LPCWSTR)_16("Error"),  MB_OK | MB_ICONINFORMATION);
	}
	YuiTempUnPause();
}

void YuiRecordAvi(HWND hWnd)
{
	WCHAR filter[1024];
	char text[MAX_PATH];
	OPENFILENAME ofn;

	YuiTempPause();

	CreateFilter(filter, 1024,
		"AVI Files *.avi)", "*.avi",
		"All files (*.*)", "*.*", NULL);

	SetupOFN(&ofn, OFN_DEFAULTSAVE, hWnd, filter,
		avifilename, sizeof(avifilename)/sizeof(TCHAR));
	ofn.lpstrDefExt = (LPCWSTR)_16("AVI");

	if (GetSaveFileName(&ofn))
	{
		WideCharToMultiByte(CP_ACP, 0, avifilename, -1, text, sizeof(text), NULL, NULL);

		DRV_AviBegin(text, hWnd);
		AVIRecording=1;
	}
	YuiTempUnPause();
}

void YuiStopAvi()
{
	DRV_AviEnd();
	AVIRecording=0;
}
