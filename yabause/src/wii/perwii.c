/*  Copyright 2008 Theo Berkau
    Copyright 2008 Romulo

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
#include <ogcsys.h>
#include <wiiuse/wpad.h>
#include <ogc/ipc.h>
#include "../peripheral.h"
#include "../vdp1.h"
#include "../vdp2.h"
#include "perwii.h"
#include "keys.h"

s32 kbdfd = -1;
volatile BOOL kbdconnected = FALSE;
extern u16 buttonbits;
PerPad_struct *pad[12];
volatile int keystate;
extern volatile int done;

struct
{
   u32 msg;
   u32 unknown;
   u8 modifier;
   u8 unknown2;
   u8 keydata[6];
} kbdevent ATTRIBUTE_ALIGN(32);

s32 KeyboardConnectCallback(s32 result,void *usrdata);

s32 KeyboardPoll(s32 result, void *usrdata)
{
   int i;

   if (kbdconnected)
   {
      switch(kbdevent.msg)
      {
         case MSG_DISCONNECT:
            kbdconnected = FALSE;
            IOS_IoctlAsync(kbdfd, 0, NULL, 0, (void *)&kbdevent, 0x10, KeyboardConnectCallback, NULL);
            break;
         case MSG_EVENT:
            // Hackish, horray!
            pad[0]->padbits[0] = 0xFF;
            pad[0]->padbits[1] = 0xFF;

            for (i = 0; i < 6; i++)
            {
               if (kbdevent.keydata[i] == 0)
                  break;
               PerKeyDown(kbdevent.keydata[i]);
            }
            IOS_IoctlAsync(kbdfd, 0, NULL, 0, (void *)&kbdevent, 0x10, KeyboardPoll, NULL);
            break;
         default: break;
      }
   }

   return 0;
}

s32 KeyboardMenuCallback(s32 result, void *usrdata)
{
   int i;
   int oldkeystate;
   int newkeystate;

   if (kbdconnected)
   {
      switch(kbdevent.msg)
      {
         case MSG_DISCONNECT:
            kbdconnected = FALSE;
            IOS_IoctlAsync(kbdfd, 0, NULL, 0, (void *)&kbdevent, 0x10, KeyboardConnectCallback, NULL);
            break;
         case MSG_EVENT:
            oldkeystate = keystate;
            newkeystate = 0;

            for (i = 0; i < 6; i++)
            {
               if (kbdevent.keydata[i] == 0)
                  break;
               switch(kbdevent.keydata[i])
               {
                  case KEY_UP:
                     if (!(oldkeystate & PAD_BUTTON_UP))
                        newkeystate |= PAD_BUTTON_UP;
                     break;
                  case KEY_DOWN:
                     if (!(oldkeystate & PAD_BUTTON_DOWN))
                        newkeystate |= PAD_BUTTON_DOWN;
                     break;
                  case KEY_ENTER:
                     if (!(oldkeystate & PAD_BUTTON_A))
                        newkeystate |= PAD_BUTTON_A;
                     break;
                  default: break;
               }
            }
            keystate = (oldkeystate ^ newkeystate) & newkeystate;
            IOS_IoctlAsync(kbdfd, 0, NULL, 0, (void *)&kbdevent, 0x10, KeyboardMenuCallback, NULL);
            break;
         default: break;
      }
   }

   return 0;
}

s32 KeyboardConnectCallback(s32 result,void *usrdata)
{
   // Should be the connect msg
   if (kbdevent.msg == MSG_CONNECT)
   {
      IOS_IoctlAsync(kbdfd, 0, NULL, 0, (void *)&kbdevent, 0x10, usrdata, NULL);
      kbdconnected = TRUE;
   }
   return 0;
}

int KBDInit(s32 (*initcallback)(s32, void *), s32 (*runcallback)(s32, void *))
{
   static char kbdstr[] ATTRIBUTE_ALIGN(32) = "/dev/usb/kbd";

   if ((kbdfd = IOS_Open(kbdstr, IPC_OPEN_NONE)) < 0)
      return -1;

   IOS_IoctlAsync(kbdfd, 0, NULL, 0, (void *)&kbdevent, 0x10, initcallback, runcallback);
   return 0;
}

int PERKeyboardInit()
{
   PerPortReset();
   pad[0] = PerPadAdd(&PORTDATA1);

//   SetupKeyPush(keypush, KEY_F1, ToggleFPS);
//   SetupKeyPush(keypush, KEY_1, ToggleNBG0);
//   SetupKeyPush(keypush, KEY_2, ToggleNBG1);
//   SetupKeyPush(keypush, KEY_3, ToggleNBG2);
//   SetupKeyPush(keypush, KEY_4, ToggleNBG3);
//   SetupKeyPush(keypush, KEY_4, ToggleRBG0);
//   SetupKeyPush(keypush, KEY_5, ToggleVDP1);

   PerSetKey(KEY_UP, PERPAD_UP, pad[0]);
   PerSetKey(KEY_DOWN, PERPAD_DOWN, pad[0]);
   PerSetKey(KEY_LEFT, PERPAD_LEFT, pad[0]);
   PerSetKey(KEY_RIGHT, PERPAD_RIGHT, pad[0]);
   PerSetKey(KEY_K, PERPAD_A, pad[0]);
   PerSetKey(KEY_L, PERPAD_B, pad[0]);
   PerSetKey(KEY_M, PERPAD_C, pad[0]);
   PerSetKey(KEY_U, PERPAD_X, pad[0]);
   PerSetKey(KEY_I, PERPAD_Y, pad[0]);
   PerSetKey(KEY_O, PERPAD_Z, pad[0]);
   PerSetKey(KEY_X, PERPAD_LEFT_TRIGGER, pad[0]);
   PerSetKey(KEY_Z, PERPAD_RIGHT_TRIGGER, pad[0]);
   PerSetKey(KEY_J, PERPAD_START, pad[0]);

   return KBDInit(KeyboardConnectCallback, KeyboardPoll);
}

void PERKeyboardDeInit()
{
   if (kbdfd > -1)
   {
      IOS_Close(kbdfd);
      kbdfd = -1;
   }
}

int PERKeyboardHandleEvents(void)
{
   return YabauseExec();
}

PerInterface_struct PERWiiKeyboard = {
PERCORE_WIIKBD,
"USB Keyboard Interface",
PERKeyboardInit,
PERKeyboardDeInit,
PERKeyboardHandleEvents
};

//////////////////////////////////////////////////////////////////////////////
// Wii Remote/ Classic Controller
//////////////////////////////////////////////////////////////////////////////

int PERClassicInit(void)	
{
   PerPortReset();
   pad[0] = PerPadAdd(&PORTDATA1);
   pad[1] = PerPadAdd(&PORTDATA2);

   return 0;
}

void PERClassicDeInit(void)	
{
}

int PERClassicHandleEvents(void)	
{
   u32 buttonsDown;
   int i;

   WPAD_ScanPads();

   for (i = 0; i < 2; i++)
   {
      buttonsDown = WPAD_ButtonsHeld(i);

      if (buttonsDown & WPAD_CLASSIC_BUTTON_UP ||
          buttonsDown & WPAD_BUTTON_RIGHT)
         PerPadUpPressed(pad[i]);
      else
         PerPadUpReleased(pad[i]);

      if (buttonsDown & WPAD_CLASSIC_BUTTON_DOWN ||
          buttonsDown & WPAD_BUTTON_LEFT)
         PerPadDownPressed(pad[i]);
      else
         PerPadDownReleased(pad[i]);

      if (buttonsDown & WPAD_CLASSIC_BUTTON_LEFT ||
          buttonsDown & WPAD_BUTTON_UP)
         PerPadLeftPressed(pad[i]);
      else
         PerPadLeftReleased(pad[i]);

      if (buttonsDown & WPAD_CLASSIC_BUTTON_RIGHT ||
          buttonsDown & WPAD_BUTTON_DOWN)
         PerPadRightPressed(pad[i]);
      else
         PerPadRightReleased(pad[i]);

      if (buttonsDown & WPAD_CLASSIC_BUTTON_PLUS ||
          buttonsDown & WPAD_BUTTON_PLUS)
         PerPadStartPressed(pad[i]);
      else
         PerPadStartReleased(pad[i]);

      if (buttonsDown & WPAD_CLASSIC_BUTTON_Y ||
          buttonsDown & WPAD_BUTTON_A)
         PerPadAPressed(pad[i]);
      else
         PerPadAReleased(pad[i]);

      if (buttonsDown & WPAD_CLASSIC_BUTTON_B ||
          buttonsDown & WPAD_BUTTON_1)
         PerPadBPressed(pad[i]);
      else
         PerPadBReleased(pad[i]);

      if (buttonsDown & WPAD_CLASSIC_BUTTON_A ||
          buttonsDown & WPAD_BUTTON_2)
         PerPadCPressed(pad[i]);
      else
         PerPadCReleased(pad[i]);

      if (buttonsDown & WPAD_CLASSIC_BUTTON_X ||
          buttonsDown & WPAD_BUTTON_MINUS)
         PerPadXPressed(pad[i]);
      else
         PerPadXReleased(pad[i]);

      if (buttonsDown & WPAD_CLASSIC_BUTTON_ZL ||
          buttonsDown & WPAD_BUTTON_B)
         PerPadYPressed(pad[i]);
      else
         PerPadYReleased(pad[i]);

      if (buttonsDown & WPAD_CLASSIC_BUTTON_ZR)
         PerPadZPressed(pad[i]);
      else
         PerPadZReleased(pad[i]);

      if (buttonsDown & WPAD_CLASSIC_BUTTON_FULL_L)
         PerPadLTriggerPressed(pad[i]);
      else
         PerPadLTriggerReleased(pad[i]);

      if (buttonsDown & WPAD_CLASSIC_BUTTON_FULL_R)
         PerPadRTriggerPressed(pad[i]);
      else
         PerPadRTriggerReleased(pad[i]);

      if (buttonsDown & WPAD_CLASSIC_BUTTON_HOME ||
          buttonsDown & WPAD_BUTTON_HOME)
      {
         done = 1;
         return 0;
      }
   }

   return YabauseExec();
}

PerInterface_struct PERWiiClassic = 
{
PERCORE_WIICLASSIC,
"Wii Remote/Classic Controller",
PERClassicInit,
PERClassicDeInit,
PERClassicHandleEvents
};

