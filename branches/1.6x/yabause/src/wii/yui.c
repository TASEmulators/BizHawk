/*  Copyright 2008 Theo Berkau

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

#include <stdio.h>
#include <stdlib.h>
#include <stdarg.h>
#include <string.h>
#include <malloc.h>
#include <wiiuse/wpad.h>
#include <ogcsys.h>
#include <gccore.h>
#include <fat.h>
#include "../cs0.h"
#include "../m68kcore.h"
#include "../peripheral.h"
#include "../vidsoft.h"
#include "../vdp2.h"
#include "../yui.h"
#include "perwii.h"
#include "sndwii.h"
#include "menu.h"

static u32 *xfb[2] = { NULL, NULL };
int fbsel = 0;
static GXRModeObj *rmode = NULL;
volatile int done=0;
volatile int resetemu=0;
int running=1;
static void *_console_buffer = NULL;
void __console_init_ex(void *conbuffer,int tgt_xstart,int tgt_ystart,int tgt_stride,int con_xres,int con_yres,int con_stride);

SH2Interface_struct *SH2CoreList[] = {
&SH2Interpreter,
&SH2DebugInterpreter,
NULL
};

PerInterface_struct *PERCoreList[] = {
&PERDummy,
&PERWiiKeyboard,
&PERWiiClassic,
NULL
};

CDInterface *CDCoreList[] = {
&DummyCD,
&ISOCD,
NULL
};

SoundInterface_struct *SNDCoreList[] = {
&SNDDummy,
&SNDWII,
NULL
};

VideoInterface_struct *VIDCoreList[] = {
&VIDDummy,
&VIDSoft,
NULL
};

M68K_struct *M68KCoreList[] = {
&M68KDummy,
&M68KC68K,
#ifdef HAVE_Q68
&M68KQ68,
#endif
NULL
};

static char bupfilename[512]="bkram.bin";
static char biosfilename[512]="bios.bin";
static char isofilename[512]="game.cue";

extern int vdp2width, vdp2height;

void gotoxy(int x, int y);
void OnScreenDebugMessage(char *string, ...);
void DoMenu();

void reset()
{
   resetemu=1;
}

void powerdown()
{
   done = 1;
}

int DVDStopMotor()
{
   static char dvdstr[] ATTRIBUTE_ALIGN(32) = "/dev/di";
   s32 fd;
   u8 buf[0x20];
   u8 outbuf[0x20];

   if ((fd = IOS_Open(dvdstr,0)) < 0)
      return 0;
	
   ((u32 *)buf)[0x00] = 0xE3000000;
   ((u32 *)buf)[0x01] = 0;
   ((u32 *)buf)[0x02] = 0;

   IOS_Ioctl(fd, buf[0], buf, 0x20, outbuf, 0x20);

   IOS_Close(fd);

   return 1;
}

int main(int argc, char **argv)
{
   WPAD_Init();
   SYS_SetResetCallback(reset);
   SYS_SetPowerCallback(powerdown);
   DVDStopMotor();
	
   fatInitDefault();

   DoMenu();

   exit(0);
   return 0;
}

int YuiExec()
{
   yabauseinit_struct yinit;
   int ret;

   VIDEO_Init();

   switch(VIDEO_GetCurrentTvMode()) 
   {
      case VI_NTSC:
         rmode = &TVNtsc240Ds;
	 break;
      case VI_PAL:
         rmode = &TVPal264Ds;
 	 break;
      case VI_MPAL:
	 rmode = &TVMpal480IntDf;
	 break;
      default:
         rmode = &TVNtsc240Ds;
	 break;
   }

   // Allocate two buffers(may not be necessary)
   xfb[0] = MEM_K0_TO_K1(SYS_AllocateFramebuffer(rmode));
   xfb[1] = MEM_K0_TO_K1(SYS_AllocateFramebuffer(rmode));
	
   VIDEO_Configure(rmode);
   VIDEO_ClearFrameBuffer (rmode, xfb[0], COLOR_BLACK);
   VIDEO_ClearFrameBuffer (rmode, xfb[1], COLOR_BLACK);
   VIDEO_SetNextFramebuffer(xfb[0]);
   VIDEO_SetBlack(FALSE);
   VIDEO_Flush();
   VIDEO_WaitVSync();
   if(rmode->viTVMode&VI_NON_INTERLACE) 
      VIDEO_WaitVSync();

   free(_console_buffer);
   _console_buffer = malloc(10*10*VI_DISPLAY_PIX_SZ);
   __console_init_ex(_console_buffer,10,10,rmode->fbWidth*VI_DISPLAY_PIX_SZ,10,10,10*VI_DISPLAY_PIX_SZ);

   memset(&yinit, 0, sizeof(yabauseinit_struct));
   yinit.percoretype = PERCORE_WIICLASSIC;
   yinit.sh2coretype = SH2CORE_INTERPRETER;
   yinit.vidcoretype = VIDCORE_SOFT;
   yinit.sndcoretype = SNDCORE_WII;
   yinit.cdcoretype = CDCORE_ISO;
   yinit.m68kcoretype = M68KCORE_C68K;
   yinit.carttype = CART_NONE;
   yinit.regionid = REGION_AUTODETECT;
   if (strcmp(biosfilename, "") == 0)
      yinit.biospath = NULL;
   else
      yinit.biospath = biosfilename;
   yinit.cdpath = isofilename;
//   yinit.buppath = bupfilename;
   yinit.buppath = NULL;
   yinit.mpegpath = NULL;
   yinit.cartpath = NULL;
   yinit.netlinksetting = NULL;
   yinit.videoformattype = VIDEOFORMATTYPE_NTSC;

   // Hijack the fps display
   VIDSoft.OnScreenDebugMessage = OnScreenDebugMessage;

   if ((ret = YabauseInit(&yinit)) == 0)
   {
      EnableAutoFrameSkip();
      VIDEO_ClearFrameBuffer(rmode, xfb[fbsel], COLOR_BLACK);

      while(!done)
      {
         if (PERCore->HandleEvents() != 0)
            return -1;
         if (resetemu)
         {
            YabauseReset();
            resetemu = 0;
            SYS_SetResetCallback(reset);
         }
      }
      YabauseDeInit();
   }
   else
   {
      while(!done)
         VIDEO_WaitVSync();
   }

   return 0;
}

void YuiErrorMsg(const char *string)
{
   if (strncmp(string, "Master SH2 invalid opcode", 25) == 0)
   {
      if (!running)
         return;
      running = 0;
      printf("%s\n", string);
   }
}

void YuiSwapBuffers()
{
   int i, j;
   u32 *curfb;
   u32 *buf;

   fbsel ^= 1;
   curfb = xfb[fbsel];
   buf = dispbuffer;

   for (j = 0; j < vdp2height; j++)
   {
      for (i = 0; i < vdp2width; i++)
      {
         // This isn't pretty
         int y1, cb1, cr1;
         int cb, cr;
         u8 r, g, b;
      
         r = buf[0] >> 24;
         g = buf[0] >> 16;
         b = buf[0] >> 8;
         buf++;
        
         y1 = (299 * r + 587 * g + 114 * b) / 1000;
         cb1 = (-16874 * r - 33126 * g + 50000 * b + 12800000) / 100000;
         cr1 = (50000 * r - 41869 * g - 8131 * b + 12800000) / 100000;
         cb = (cb1 + cb1) >> 1;
         cr = (cr1 + cr1) >> 1;

         curfb[0] = (y1 << 24) | (cb << 16) | (y1 << 8) | cr;
         curfb++;
      }
   }

   VIDEO_SetNextFramebuffer (xfb[fbsel]);
   VIDEO_Flush ();
}

void gotoxy(int x, int y)
{
   printf("\033[%d;%dH", y, x);
}

void OnScreenDebugMessage(char *string, ...)
{
   va_list arglist;

   va_start(arglist, string);
   gotoxy(0, 1);
   vprintf(string, arglist);
   printf("\n");
   gotoxy(0, 1);
   va_end(arglist);
}

int ClearMenu(const unsigned long *bmp)
{
   fbsel ^= 1;
   memcpy (xfb[fbsel], bmp, MENU_SIZE * 2);
   VIDEO_SetNextFramebuffer (xfb[fbsel]);
   VIDEO_Flush ();
   return 0;   
}

typedef struct
{
   char *name;
   int (*func)();
} menuitem_struct;

int MenuNone()
{
   return 0;
}

menuitem_struct menuitem[] = {
{ "Start emulation", YuiExec },
{ "Load ISO/CUE", MenuNone },
{ "Settings", MenuNone },
{ "About", MenuNone },
{ "Exit", NULL },
{ NULL, NULL }
};

void InitMenu()
{
   VIDEO_Init();
   switch(VIDEO_GetCurrentTvMode()) 
   {
      case VI_NTSC:
         rmode = &TVNtsc480IntDf;
	 break;
      case VI_PAL:
         rmode = &TVPal528IntDf;
 	 break;
      case VI_MPAL:
	 rmode = &TVMpal480IntDf;
	 break;
      default:
         rmode = &TVNtsc480IntDf;
	 break;
   }

   xfb[0] = MEM_K0_TO_K1(SYS_AllocateFramebuffer(rmode));
   xfb[1] = MEM_K0_TO_K1(SYS_AllocateFramebuffer(rmode));
   VIDEO_Configure(rmode);
   VIDEO_ClearFrameBuffer (rmode, xfb[0], COLOR_BLACK);
   VIDEO_ClearFrameBuffer (rmode, xfb[1], COLOR_BLACK);
   VIDEO_SetNextFramebuffer(xfb[0]);
   VIDEO_SetBlack(FALSE);
   VIDEO_Flush();
   VIDEO_WaitVSync();
   if(rmode->viTVMode&VI_NON_INTERLACE) 
      VIDEO_WaitVSync();
   free(_console_buffer);
   _console_buffer = malloc(280*100*VI_DISPLAY_PIX_SZ);
   __console_init_ex(_console_buffer,180,200,rmode->fbWidth*VI_DISPLAY_PIX_SZ,280,100,280*VI_DISPLAY_PIX_SZ);
}

void DoMenu()
{
   int menuselect=0;
   int i;
   int nummenu=0;

   InitMenu();

   for (i = 0; menuitem[i].name != NULL; i++)
      nummenu++;

   for (;;)
   {
      VIDEO_WaitVSync();
      WPAD_ScanPads();

      // Get Wii Remote/Keyboard/etc. presses
      if (WPAD_ButtonsDown(0) & WPAD_CLASSIC_BUTTON_UP ||
          WPAD_ButtonsDown(0) & WPAD_BUTTON_RIGHT)
      {
         menuselect--;
         if (menuselect < 0)
            menuselect = nummenu-1;
      }
      else if (WPAD_ButtonsDown(0) & WPAD_CLASSIC_BUTTON_DOWN ||
               WPAD_ButtonsDown(0) & WPAD_BUTTON_LEFT)
      {          
         menuselect++;
         if (menuselect == nummenu)
            menuselect = 0; 
      }

      if (WPAD_ButtonsDown(0) & WPAD_CLASSIC_BUTTON_A ||
          WPAD_ButtonsDown(0) & WPAD_BUTTON_A ||
          WPAD_ButtonsDown(0) & WPAD_BUTTON_2)
      {
          if (menuitem[menuselect].func)
          {
             if (menuitem[menuselect].func())
                return;
             InitMenu();
          }
          else
             return;
      }

      // Draw menu
      ClearMenu(menu_bmp);

      // Draw menu items
      gotoxy(0, 0);
      for (i = 0; i < nummenu; i++)
      {
         if (menuselect == i)
            printf("\033[%d;%dm", 30+9, 0);
         else
            printf("\033[%d;%dm", 30+7, 0);
         printf("%s\n", menuitem[i].name);
      }     
   }
}
