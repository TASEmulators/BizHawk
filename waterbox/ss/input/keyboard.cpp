/******************************************************************************/
/* Mednafen Sega Saturn Emulation Module                                      */
/******************************************************************************/
/* keyboard.cpp:
**  Copyright (C) 2017 Mednafen Team
**
** This program is free software; you can redistribute it and/or
** modify it under the terms of the GNU General Public License
** as published by the Free Software Foundation; either version 2
** of the License, or (at your option) any later version.
**
** This program is distributed in the hope that it will be useful,
** but WITHOUT ANY WARRANTY; without even the implied warranty of
** MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
** GNU General Public License for more details.
**
** You should have received a copy of the GNU General Public License
** along with this program; if not, write to the Free Software Foundation, Inc.,
** 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
*/

// TODO: Debouncing?

//
// PS/2 keyboard adapter seems to do PS/2 processing near/at the end of a Saturn-side read sequence, which creates about 1 frame of extra latency
// in practice.  We handle things a bit differently here, to avoid the latency.
//
// Also, the PS/2 adapter seems to set the typematic delay to around 250ms, but we emulate it here as 400ms, as 250ms is
// a tad bit too short.  It can be changed to 250ms by adjusting a single #if statement, though.
//
// During testing, a couple of early-1990s PS/2 keyboards malfunctioned and failed to work with the PS/2 adapter.
// Not sure why, maybe a power draw issue?
//
// The keyboard emulated doesn't have special Windows-keyboard keys, as they don't appear to work correctly with the PS/2 adapter
// (scancode field is updated, but no make nor break bits are set to 1), and it's good to have some non-shared keys for input grabbing toggling purposes...
//
//

// make and break bits should not both be set to 1 at the same time.
// pause is special
// new key press halts repeat of held key, and it doesn't restart even if new key is released.
//

#include "common.h"
#include "keyboard.h"

namespace MDFN_IEN_SS
{

IODevice_Keyboard::IODevice_Keyboard() : phys{0,0,0,0}
{

}

IODevice_Keyboard::~IODevice_Keyboard()
{

}

void IODevice_Keyboard::Power(void)
{
 phase = -1;
 tl = true;
 data_out = 0x01;

 simbutt = simbutt_pend = 0;
 lock = lock_pend = 0;

 mkbrk_pend = 0;
 memset(buffer, 0, sizeof(buffer));

 //memcpy(processed, phys, sizeof(processed));
 memset(processed, 0, sizeof(processed));
 memset(fifo, 0, sizeof(fifo));
 fifo_rdp = 0;
 fifo_wrp = 0;
 fifo_cnt = 0;

 rep_sc = -1;
 rep_dcnt = 0;
}

void IODevice_Keyboard::UpdateInput(const uint8* data, const int32 time_elapsed)
{
 phys[0] = MDFN_de64lsb(&data[0x00]);
 phys[1] = MDFN_de64lsb(&data[0x08]);
 phys[2] = MDFN_de16lsb(&data[0x10]);
 phys[3] = 0;
 //
 if(rep_dcnt > 0)
  rep_dcnt -= time_elapsed;

 for(unsigned i = 0; i < 4; i++)
 {
  uint64 tmp = phys[i] ^ processed[i];
  unsigned bp;

  while((bp = (63 ^ MDFN_lzcount64(tmp))) < 64)
  {
   const uint64 mask = ((uint64)1 << bp);
   const int sc = ((i << 6) + bp);

   if(fifo_cnt >= (fifo_size - (sc == 0x82)))
    goto fifo_oflow_abort;

   if(phys[i] & mask)
   {
    rep_sc = sc;
#if 1
    rep_dcnt = 400000;
#else
    rep_dcnt = 250000;
#endif
    fifo[fifo_wrp] = 0x800 | sc;
    fifo_wrp = (fifo_wrp + 1) % fifo_size;
    fifo_cnt++;
   }

   if(!(phys[i] & mask) == (sc != 0x82))
   {
    if(rep_sc == sc)
     rep_sc = -1;

    fifo[fifo_wrp] = 0x100 | sc;
    fifo_wrp = (fifo_wrp + 1) % fifo_size;
    fifo_cnt++;
   }

   processed[i] = (processed[i] & ~mask) | (phys[i] & mask);
   tmp &= ~mask;
  }
 }

 if(rep_sc >= 0)
 {
  while(rep_dcnt <= 0)
  {
   if(fifo_cnt >= fifo_size)
    goto fifo_oflow_abort;

   fifo[fifo_wrp] = 0x800 | rep_sc;
   fifo_wrp = (fifo_wrp + 1) % fifo_size;
   fifo_cnt++;

   rep_dcnt += 33333;
  }
 }

 fifo_oflow_abort:;
}

void IODevice_Keyboard::UpdateOutput(uint8* data)
{
 data[0x12] = lock;
}

uint8 IODevice_Keyboard::UpdateBus(const uint8 smpc_out, const uint8 smpc_out_asserted)
{
 if(smpc_out & 0x40)
 {
  phase = -1;
  tl = true;
  data_out = 0x01;
 }
 else
 {
  if((bool)(smpc_out & 0x20) != tl)
  {
   tl = !tl;
   phase += (phase < 11);

   if(!phase)
   {
    if(mkbrk_pend == (uint8)mkbrk_pend && fifo_cnt)
    {
     mkbrk_pend = fifo[fifo_rdp];
     fifo_rdp = (fifo_rdp + 1) % fifo_size;
     fifo_cnt--;

     bool p = mkbrk_pend & 0x800;

     switch(mkbrk_pend & 0xFF)
     {
      case 0x89: /*  Up */ simbutt_pend = simbutt & ~(1 <<  0); simbutt_pend &= ~(p <<  1); simbutt_pend |= (p <<  0); break;
      case 0x8A: /*Down */ simbutt_pend = simbutt & ~(1 <<  1); simbutt_pend &= ~(p <<  0); simbutt_pend |= (p <<  1); break;
      case 0x86: /*Left */ simbutt_pend = simbutt & ~(1 <<  2); simbutt_pend &= ~(p <<  3); simbutt_pend |= (p <<  2); break;
      case 0x8D: /*Right*/ simbutt_pend = simbutt & ~(1 <<  3); simbutt_pend &= ~(p <<  2); simbutt_pend |= (p <<  3); break;
      case 0x22: /*   X */ simbutt_pend = simbutt & ~(1 <<  4); simbutt_pend |= (p <<  4); break;
      case 0x21: /*   C */ simbutt_pend = simbutt & ~(1 <<  5); simbutt_pend |= (p <<  5); break;
      case 0x1A: /*   Z */ simbutt_pend = simbutt & ~(1 <<  6); simbutt_pend |= (p <<  6); break;
      case 0x76: /* Esc */ simbutt_pend = simbutt & ~(1 <<  7); simbutt_pend |= (p <<  7); break;
      case 0x23: /*   D */ simbutt_pend = simbutt & ~(1 <<  8); simbutt_pend |= (p <<  8); break;
      case 0x1B: /*   S */ simbutt_pend = simbutt & ~(1 <<  9); simbutt_pend |= (p <<  9); break;
      case 0x1C: /*   A */ simbutt_pend = simbutt & ~(1 << 10); simbutt_pend |= (p << 10); break;
      case 0x24: /*   E */ simbutt_pend = simbutt & ~(1 << 11); simbutt_pend |= (p << 11); break;
      case 0x15: /*   Q */ simbutt_pend = simbutt & ~(1 << 15); simbutt_pend |= (p << 15); break;

      case 0x7E: /* Scrl */ lock_pend = lock ^ (p ? LOCK_SCROLL : 0); break;
      case 0x77: /* Num  */ lock_pend = lock ^ (p ? LOCK_NUM : 0);    break;
      case 0x58: /* Caps */ lock_pend = lock ^ (p ? LOCK_CAPS : 0);   break;
     }
    }
    buffer[ 0] = 0x3;
    buffer[ 1] = 0x4;
    buffer[ 2] = (((simbutt_pend >>  0) ^ 0xF) & 0xF);
    buffer[ 3] = (((simbutt_pend >>  4) ^ 0xF) & 0xF);
    buffer[ 4] = (((simbutt_pend >>  8) ^ 0xF) & 0xF);
    buffer[ 5] = (((simbutt_pend >> 12) ^ 0xF) & 0x8) | 0x0;
    buffer[ 6] = lock_pend;
    buffer[ 7] = ((mkbrk_pend >> 8) & 0xF) | 0x6;
    buffer[ 8] =  (mkbrk_pend >> 4) & 0xF;
    buffer[ 9] =  (mkbrk_pend >> 0) & 0xF;
    buffer[10] = 0x0;
    buffer[11] = 0x1;
   }

   if(phase == 9)
   {
    mkbrk_pend = (uint8)mkbrk_pend;
    lock = lock_pend;
    simbutt = simbutt_pend;
   }

   data_out = buffer[phase];
  }
 }

 return (smpc_out & (smpc_out_asserted | 0xE0)) | (((tl << 4) | data_out) &~ smpc_out_asserted);
}

static const IDIIS_StatusState Lock_SS[] =
{
 { "off", gettext_noop("Off") },
 { "on", gettext_noop("On") },
};

const IDIISG IODevice_Keyboard_US101_IDII =
{
 /* 0x00 */ { NULL, "empty", -1, IDIT_BUTTON },
 /* 0x01 */ { "f9", "F9", -1, IDIT_BUTTON },
 /* 0x02 */ { NULL, "empty", -1, IDIT_BUTTON },
 /* 0x03 */ { "f5", "F5", -1, IDIT_BUTTON },
 /* 0x04 */ { "f3", "F3", -1, IDIT_BUTTON },
 /* 0x05 */ { "f1", "F1", -1, IDIT_BUTTON },
 /* 0x06 */ { "f2", "F2", -1, IDIT_BUTTON },
 /* 0x07 */ { "f12", "F12", -1, IDIT_BUTTON },
 /* 0x08 */ { NULL, "empty", -1, IDIT_BUTTON },
 /* 0x09 */ { "f10", "F10", -1, IDIT_BUTTON },
 /* 0x0A */ { "f8", "F8", -1, IDIT_BUTTON },
 /* 0x0B */ { "f6", "F6", -1, IDIT_BUTTON },
 /* 0x0C */ { "f4", "F4", -1, IDIT_BUTTON },
 /* 0x0D */ { "tab", "Tab", -1, IDIT_BUTTON },
 /* 0x0E */ { "grave", "Grave `", -1, IDIT_BUTTON },
 /* 0x0F */ { NULL, "empty", -1, IDIT_BUTTON },

 /* 0x10 */ { NULL, "empty", -1, IDIT_BUTTON },
 /* 0x11 */ { "lalt", "Left Alt", -1, IDIT_BUTTON },
 /* 0x12 */ { "lshift", "Left Shift", -1, IDIT_BUTTON },
 /* 0x13 */ { NULL, "empty", -1, IDIT_BUTTON },
 /* 0x14 */ { "lctrl", "Left Ctrl", -1, IDIT_BUTTON },
 /* 0x15 */ { "q", "Q", -1, IDIT_BUTTON },
 /* 0x16 */ { "1", "1(One)", -1, IDIT_BUTTON },
 /* 0x17 */ { "ralt", "Right Alt", -1, IDIT_BUTTON },
 /* 0x18 */ { "rctrl", "Right Ctrl", -1, IDIT_BUTTON },
 /* 0x19 */ { "kp_enter", "Keypad Enter", -1, IDIT_BUTTON },
 /* 0x1A */ { "z", "Z", -1, IDIT_BUTTON },
 /* 0x1B */ { "s", "S", -1, IDIT_BUTTON },
 /* 0x1C */ { "a", "A", -1, IDIT_BUTTON },
 /* 0x1D */ { "w", "W", -1, IDIT_BUTTON },
 /* 0x1E */ { "2", "2", -1, IDIT_BUTTON },
 /* 0x1F */ { NULL, "empty", -1, IDIT_BUTTON },

 /* 0x20 */ { NULL, "empty", -1, IDIT_BUTTON },
 /* 0x21 */ { "c", "C", -1, IDIT_BUTTON },
 /* 0x22 */ { "x", "X", -1, IDIT_BUTTON },
 /* 0x23 */ { "d", "D", -1, IDIT_BUTTON },
 /* 0x24 */ { "e", "E", -1, IDIT_BUTTON },
 /* 0x25 */ { "4", "4", -1, IDIT_BUTTON },
 /* 0x26 */ { "3", "3", -1, IDIT_BUTTON },
 /* 0x27 */ { NULL, "empty", -1, IDIT_BUTTON },
 /* 0x28 */ { NULL, "empty", -1, IDIT_BUTTON },
 /* 0x29 */ { "space", "Space", -1, IDIT_BUTTON },
 /* 0x2A */ { "v", "V", -1, IDIT_BUTTON },
 /* 0x2B */ { "f", "F", -1, IDIT_BUTTON },
 /* 0x2C */ { "t", "T", -1, IDIT_BUTTON },
 /* 0x2D */ { "r", "R", -1, IDIT_BUTTON },
 /* 0x2E */ { "5", "5", -1, IDIT_BUTTON },
 /* 0x2F */ { NULL, "empty", -1, IDIT_BUTTON },

 /* 0x30 */ { NULL, "empty", -1, IDIT_BUTTON },
 /* 0x31 */ { "n", "N", -1, IDIT_BUTTON },
 /* 0x32 */ { "b", "B", -1, IDIT_BUTTON },
 /* 0x33 */ { "h", "H", -1, IDIT_BUTTON },
 /* 0x34 */ { "g", "G", -1, IDIT_BUTTON },
 /* 0x35 */ { "y", "Y", -1, IDIT_BUTTON },
 /* 0x36 */ { "6", "6", -1, IDIT_BUTTON },
 /* 0x37 */ { NULL, "empty", -1, IDIT_BUTTON },
 /* 0x38 */ { NULL, "empty", -1, IDIT_BUTTON },
 /* 0x39 */ { NULL, "empty", -1, IDIT_BUTTON },
 /* 0x3A */ { "m", "M", -1, IDIT_BUTTON },
 /* 0x3B */ { "j", "J", -1, IDIT_BUTTON },
 /* 0x3C */ { "u", "U", -1, IDIT_BUTTON },
 /* 0x3D */ { "7", "7", -1, IDIT_BUTTON },
 /* 0x3E */ { "8", "8", -1, IDIT_BUTTON },
 /* 0x3F */ { NULL, "empty", -1, IDIT_BUTTON },

 /* 0x40 */ { NULL, "empty", -1, IDIT_BUTTON },
 /* 0x41 */ { "comma", "Comma ,", -1, IDIT_BUTTON },
 /* 0x42 */ { "k", "K", -1, IDIT_BUTTON },
 /* 0x43 */ { "i", "I", -1, IDIT_BUTTON },
 /* 0x44 */ { "o", "O", -1, IDIT_BUTTON },
 /* 0x45 */ { "0", "0(Zero)", -1, IDIT_BUTTON },
 /* 0x46 */ { "9", "9", -1, IDIT_BUTTON },
 /* 0x47 */ { NULL, "empty", -1, IDIT_BUTTON },
 /* 0x48 */ { NULL, "empty", -1, IDIT_BUTTON },
 /* 0x49 */ { "period", "Period .", -1, IDIT_BUTTON },
 /* 0x4A */ { "slash", "Slash /", -1, IDIT_BUTTON },
 /* 0x4B */ { "l", "L", -1, IDIT_BUTTON },
 /* 0x4C */ { "semicolon", "Semicolon ;", -1, IDIT_BUTTON },
 /* 0x4D */ { "p", "P", -1, IDIT_BUTTON },
 /* 0x4E */ { "Minus", "Minus -", -1, IDIT_BUTTON },
 /* 0x4F */ { NULL, "empty", -1, IDIT_BUTTON },

 /* 0x50 */ { NULL, "empty", -1, IDIT_BUTTON },
 /* 0x51 */ { NULL, "empty", -1, IDIT_BUTTON },
 /* 0x52 */ { "quote", "Quote '", -1, IDIT_BUTTON },
 /* 0x53 */ { NULL, "empty", -1, IDIT_BUTTON },
 /* 0x54 */ { "leftbracket", "Left Bracket [", -1, IDIT_BUTTON },
 /* 0x55 */ { "equals", "Equals =", -1, IDIT_BUTTON },
 /* 0x56 */ { NULL, "empty", -1, IDIT_BUTTON },
 /* 0x57 */ { NULL, "empty", -1, IDIT_BUTTON },
 /* 0x58 */ { "capslock", "Caps Lock", -1, IDIT_BUTTON },
 /* 0x59 */ { "rshift", "Right Shift", -1, IDIT_BUTTON },
 /* 0x5A */ { "enter", "Enter", -1, IDIT_BUTTON },
 /* 0x5B */ { "rightbracket", "Right Bracket ]", -1, IDIT_BUTTON },
 /* 0x5C */ { NULL, "empty", -1, IDIT_BUTTON },
 /* 0x5D */ { "backslash", "Backslash \\", -1, IDIT_BUTTON },
 /* 0x5E */ { NULL, "empty", -1, IDIT_BUTTON },
 /* 0x5F */ { NULL, "empty", -1, IDIT_BUTTON },

 /* 0x60 */ { NULL, "empty", -1, IDIT_BUTTON },
 /* 0x61 */ { NULL, "empty", -1, IDIT_BUTTON },
 /* 0x62 */ { NULL, "empty", -1, IDIT_BUTTON },
 /* 0x63 */ { NULL, "empty", -1, IDIT_BUTTON },
 /* 0x64 */ { NULL, "empty", -1, IDIT_BUTTON },
 /* 0x65 */ { NULL, "empty", -1, IDIT_BUTTON },
 /* 0x66 */ { "backspace", "Backspace", -1, IDIT_BUTTON },
 /* 0x67 */ { NULL, "empty", -1, IDIT_BUTTON },
 /* 0x68 */ { NULL, "empty", -1, IDIT_BUTTON },
 /* 0x69 */ { "kp_end", "Keypad End/1", -1, IDIT_BUTTON },
 /* 0x6A */ { NULL, "empty", -1, IDIT_BUTTON },
 /* 0x6B */ { "kp_left", "Keypad Left/4", -1, IDIT_BUTTON },
 /* 0x6C */ { "kp_home", "Keypad Home/7", -1, IDIT_BUTTON },
 /* 0x6D */ { NULL, "empty", -1, IDIT_BUTTON },
 /* 0x6E */ { NULL, "empty", -1, IDIT_BUTTON },
 /* 0x6F */ { NULL, "empty", -1, IDIT_BUTTON },

 /* 0x70 */ { "kp_insert", "Keypad Insert/0", -1, IDIT_BUTTON },
 /* 0x71 */ { "kp_delete", "Keypad Delete", -1, IDIT_BUTTON },
 /* 0x72 */ { "kp_down", "Keypad Down/2", -1, IDIT_BUTTON },
 /* 0x73 */ { "kp_center", "Keypad Center/5", -1, IDIT_BUTTON },
 /* 0x74 */ { "kp_right", "Keypad Right/6", -1, IDIT_BUTTON },
 /* 0x75 */ { "kp_up", "Keypad Up/8", -1, IDIT_BUTTON },
 /* 0x76 */ { "esc", "Escape", -1, IDIT_BUTTON },
 /* 0x77 */ { "numlock", "Num Lock", -1, IDIT_BUTTON },
 /* 0x78 */ { "f11", "F11", -1, IDIT_BUTTON },
 /* 0x79 */ { "kp_plus", "Keypad Plus", -1, IDIT_BUTTON },
 /* 0x7A */ { "kp_pagedown", "Keypad Pagedown/3", -1, IDIT_BUTTON },
 /* 0x7B */ { "kp_minus", "Keypad Minus", -1, IDIT_BUTTON },
 /* 0x7C */ { "kp_asterisk", "Keypad Asterisk(Multiply)", -1, IDIT_BUTTON },
 /* 0x7D */ { "kp_pageup", "Keypad Pageup/9", -1, IDIT_BUTTON },
 /* 0x7E */ { "scrolllock", "Scroll Lock", -1, IDIT_BUTTON },
 /* 0x7F */ { NULL, "empty", -1, IDIT_BUTTON },

 /* 0x80 */ { "kp_slash", "Keypad Slash(Divide)", -1, IDIT_BUTTON },
 /* 0x81 */ { "insert", "Insert", -1, IDIT_BUTTON },
 /* 0x82 */ { "pause", "Pause", -1, IDIT_BUTTON },
 /* 0x83 */ { "f7", "F7", -1, IDIT_BUTTON },
 /* 0x84 */ { "printscreen", "Print Screen", -1, IDIT_BUTTON },
 /* 0x85 */ { "delete", "Delete", -1, IDIT_BUTTON },
 /* 0x86 */ { "left", "Cursor Left", -1, IDIT_BUTTON },
 /* 0x87 */ { "home", "Home", -1, IDIT_BUTTON },
 /* 0x88 */ { "end", "End", -1, IDIT_BUTTON },
 /* 0x89 */ { "up", "Up", -1, IDIT_BUTTON },
 /* 0x8A */ { "down", "Down", -1, IDIT_BUTTON },
 /* 0x8B */ { "pageup", "Page Up", -1, IDIT_BUTTON },
 /* 0x8C */ { "pagedown", "Page Down", -1, IDIT_BUTTON },
 /* 0x8D */ { "right", "Right", -1, IDIT_BUTTON },
 /* 0x8E */ { NULL, "empty", -1, IDIT_BUTTON },
 /* 0x8F */ { NULL, "empty", -1, IDIT_BUTTON },

 IDIIS_Status("scrolllock_status", "Scroll Lock", Lock_SS, sizeof(Lock_SS) / sizeof(Lock_SS[0])),
 IDIIS_Status("numlock_status", "Num Lock", Lock_SS, sizeof(Lock_SS) / sizeof(Lock_SS[0])),
 IDIIS_Status("capslock_status", "Caps Lock", Lock_SS, sizeof(Lock_SS) / sizeof(Lock_SS[0]))
};

}
