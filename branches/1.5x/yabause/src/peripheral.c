/*  Copyright 2005 Guillaume Duhamel
    Copyright 2005-2006 Theo Berkau

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

#include "debug.h"
#include "peripheral.h"

const char * PerPadNames[] =
{
"Up",
"Right",
"Down",
"Left",
"R",
"L",
"Start",
"A",
"B",
"C",
"X",
"Y",
"Z",
NULL
};

const char * PerMouseNames[] =
{
"A",
"B",
"C",
"Start",
NULL
};

PortData_struct PORTDATA1;
PortData_struct PORTDATA2;

PerInterface_struct * PERCore = NULL;
extern PerInterface_struct * PERCoreList[];

typedef struct {
	u8 name;
	void (*Press)(void *);
	void (*Release)(void *);
} PerBaseConfig_struct;

typedef struct {
	u32 key;
	PerBaseConfig_struct * base;
	void * controller;
} PerConfig_struct;

#define PERCALLBACK(func) ((void (*) (void *)) func)

PerBaseConfig_struct perkeybaseconfig[] = {
	{ PERPAD_UP, PERCALLBACK(PerPadUpPressed), PERCALLBACK(PerPadUpReleased) },
	{ PERPAD_RIGHT, PERCALLBACK(PerPadRightPressed), PERCALLBACK(PerPadRightReleased) },
	{ PERPAD_DOWN, PERCALLBACK(PerPadDownPressed), PERCALLBACK(PerPadDownReleased) },
	{ PERPAD_LEFT, PERCALLBACK(PerPadLeftPressed), PERCALLBACK(PerPadLeftReleased) },
	{ PERPAD_RIGHT_TRIGGER, PERCALLBACK(PerPadRTriggerPressed), PERCALLBACK(PerPadRTriggerReleased) },
	{ PERPAD_LEFT_TRIGGER, PERCALLBACK(PerPadLTriggerPressed), PERCALLBACK(PerPadLTriggerReleased) },
	{ PERPAD_START, PERCALLBACK(PerPadStartPressed), PERCALLBACK(PerPadStartReleased) },
	{ PERPAD_A, PERCALLBACK(PerPadAPressed), PERCALLBACK(PerPadAReleased) },
	{ PERPAD_B, PERCALLBACK(PerPadBPressed), PERCALLBACK(PerPadBReleased) },
        { PERPAD_C, PERCALLBACK(PerPadCPressed), PERCALLBACK(PerPadCReleased) },
	{ PERPAD_X, PERCALLBACK(PerPadXPressed), PERCALLBACK(PerPadXReleased) },
	{ PERPAD_Y, PERCALLBACK(PerPadYPressed), PERCALLBACK(PerPadYReleased) },
	{ PERPAD_Z, PERCALLBACK(PerPadZPressed), PERCALLBACK(PerPadZReleased) },
};

PerBaseConfig_struct permousebaseconfig[] = {
	{ PERMOUSE_LEFT, PERCALLBACK(PerMouseLeftPressed), PERCALLBACK(PerMouseLeftReleased) },
	{ PERMOUSE_MIDDLE, PERCALLBACK(PerMouseMiddlePressed), PERCALLBACK(PerMouseMiddleReleased) },
	{ PERMOUSE_RIGHT, PERCALLBACK(PerMouseRightPressed), PERCALLBACK(PerMouseRightReleased) },
	{ PERMOUSE_START, PERCALLBACK(PerMouseStartPressed), PERCALLBACK(PerMouseStartReleased) },
};

static u32 perkeyconfigsize = 0;
static PerConfig_struct * perkeyconfig = NULL;

static void PerUpdateConfig(PerBaseConfig_struct * baseconfig, int nelems, void * controller);

//////////////////////////////////////////////////////////////////////////////

int PerInit(int coreid) {
   int i;

   // So which core do we want?
   if (coreid == PERCORE_DEFAULT)
      coreid = 0; // Assume we want the first one

   // Go through core list and find the id
   for (i = 0; PERCoreList[i] != NULL; i++)
   {
      if (PERCoreList[i]->id == coreid)
      {
         // Set to current core
         PERCore = PERCoreList[i];
         break;
      }
   }

   if (PERCore == NULL)
      return -1;

   if (PERCore->Init() != 0)
      return -1;

   return 0;
}

//////////////////////////////////////////////////////////////////////////////

void PerDeInit(void) {
   if (PERCore)
      PERCore->DeInit();
   PERCore = NULL;
}

//////////////////////////////////////////////////////////////////////////////

void PerPadUpPressed(PerPad_struct * pad) {
   *pad->padbits &= 0xEF;
   SMPCLOG("Up\n");
}

//////////////////////////////////////////////////////////////////////////////

void PerPadUpReleased(PerPad_struct * pad) {
   *pad->padbits |= ~0xEF;
}

//////////////////////////////////////////////////////////////////////////////

void PerPadDownPressed(PerPad_struct * pad) {
   *pad->padbits &= 0xDF;
   SMPCLOG("Down\n");
}

//////////////////////////////////////////////////////////////////////////////

void PerPadDownReleased(PerPad_struct * pad) {
   *pad->padbits |= ~0xDF;
}

//////////////////////////////////////////////////////////////////////////////

void PerPadRightPressed(PerPad_struct * pad) {
   *pad->padbits &= 0x7F;
   SMPCLOG("Right\n");
}

//////////////////////////////////////////////////////////////////////////////

void PerPadRightReleased(PerPad_struct * pad) {
   *pad->padbits |= ~0x7F;
}

//////////////////////////////////////////////////////////////////////////////

void PerPadLeftPressed(PerPad_struct * pad) {
   *pad->padbits &= 0xBF;
   SMPCLOG("Left\n");
}

//////////////////////////////////////////////////////////////////////////////

void PerPadLeftReleased(PerPad_struct * pad) {
   *pad->padbits |= ~0xBF;
}

//////////////////////////////////////////////////////////////////////////////

void PerPadStartPressed(PerPad_struct * pad) {
   *pad->padbits &= 0xF7;
   SMPCLOG("Start\n");
}

//////////////////////////////////////////////////////////////////////////////

void PerPadStartReleased(PerPad_struct * pad) {
   *pad->padbits |= ~0xF7;
}

//////////////////////////////////////////////////////////////////////////////

void PerPadAPressed(PerPad_struct * pad) {
   *pad->padbits &= 0xFB;
   SMPCLOG("A\n");
}

//////////////////////////////////////////////////////////////////////////////

void PerPadAReleased(PerPad_struct * pad) {
   *pad->padbits |= ~0xFB;
}

//////////////////////////////////////////////////////////////////////////////

void PerPadBPressed(PerPad_struct * pad) {
   *pad->padbits &= 0xFE;
   SMPCLOG("B\n");
}

//////////////////////////////////////////////////////////////////////////////

void PerPadBReleased(PerPad_struct * pad) {
   *pad->padbits |= ~0xFE;
}

//////////////////////////////////////////////////////////////////////////////

void PerPadCPressed(PerPad_struct * pad) {
   *pad->padbits &= 0xFD;
   SMPCLOG("C\n");
}

//////////////////////////////////////////////////////////////////////////////

void PerPadCReleased(PerPad_struct * pad) {
   *pad->padbits |= ~0xFD;
}

//////////////////////////////////////////////////////////////////////////////

void PerPadXPressed(PerPad_struct * pad) {
   *(pad->padbits + 1) &= 0xBF;
   SMPCLOG("X\n");
}

//////////////////////////////////////////////////////////////////////////////

void PerPadXReleased(PerPad_struct * pad) {
   *(pad->padbits + 1) |= ~0xBF;
}

//////////////////////////////////////////////////////////////////////////////

void PerPadYPressed(PerPad_struct * pad) {
   *(pad->padbits + 1) &= 0xDF;
   SMPCLOG("Y\n");
}

//////////////////////////////////////////////////////////////////////////////

void PerPadYReleased(PerPad_struct * pad) {
   *(pad->padbits + 1) |= ~0xDF;
}

//////////////////////////////////////////////////////////////////////////////

void PerPadZPressed(PerPad_struct * pad) {
   *(pad->padbits + 1) &= 0xEF;
   SMPCLOG("Z\n");
}

//////////////////////////////////////////////////////////////////////////////

void PerPadZReleased(PerPad_struct * pad) {
   *(pad->padbits + 1) |= ~0xEF;
}

//////////////////////////////////////////////////////////////////////////////

void PerPadRTriggerPressed(PerPad_struct * pad) {
   *(pad->padbits + 1) &= 0x7F;
   SMPCLOG("Right Trigger\n");
}

//////////////////////////////////////////////////////////////////////////////

void PerPadRTriggerReleased(PerPad_struct * pad) {
   *(pad->padbits + 1) |= ~0x7F;
}

//////////////////////////////////////////////////////////////////////////////

void PerPadLTriggerPressed(PerPad_struct * pad) {
   *(pad->padbits + 1) &= 0xF7;
   SMPCLOG("Left Trigger\n");
}

//////////////////////////////////////////////////////////////////////////////

void PerPadLTriggerReleased(PerPad_struct * pad) {
   *(pad->padbits + 1) |= ~0xF7;
}

//////////////////////////////////////////////////////////////////////////////

void PerMouseLeftPressed(PerMouse_struct * mouse) {
   *(mouse->mousebits) |= 1;
}

//////////////////////////////////////////////////////////////////////////////

void PerMouseLeftReleased(PerMouse_struct * mouse) {
   *(mouse->mousebits) &= 0xFFFE;
}

//////////////////////////////////////////////////////////////////////////////

void PerMouseMiddlePressed(PerMouse_struct * mouse) {
   *(mouse->mousebits) |= 4;
}

//////////////////////////////////////////////////////////////////////////////

void PerMouseMiddleReleased(PerMouse_struct * mouse) {
   *(mouse->mousebits) &= 0xFFFB;
}

//////////////////////////////////////////////////////////////////////////////

void PerMouseRightPressed(PerMouse_struct * mouse) {
   *(mouse->mousebits) |= 2;
}

//////////////////////////////////////////////////////////////////////////////

void PerMouseRightReleased(PerMouse_struct * mouse) {
   *(mouse->mousebits) &= 0xFFFD;
}

//////////////////////////////////////////////////////////////////////////////

void PerMouseStartPressed(PerMouse_struct * mouse) {
   *(mouse->mousebits) |= 8;
}

//////////////////////////////////////////////////////////////////////////////

void PerMouseStartReleased(PerMouse_struct * mouse) {
   *(mouse->mousebits) &= 0xFFF7;
}

//////////////////////////////////////////////////////////////////////////////

void PerMouseMove(PerMouse_struct * mouse, s32 dispx, s32 dispy)
{
   int negx, negy, overflowx, overflowy;
   u8 diffx, diffy;

   negx = ((mouse->mousebits[0] >> 4) & 1);
   negy = ((mouse->mousebits[0] >> 5) & 1);
   overflowx = ((mouse->mousebits[0] >> 6) & 1);
   overflowy = ((mouse->mousebits[0] >> 7) & 1);

   if (negx) diffx = ~(mouse->mousebits[1]) & 0xFF;
   else diffx = mouse->mousebits[1];
   if (negy) diffy = ~(mouse->mousebits[2]) & 0xFF;
   else diffy = mouse->mousebits[2];

   if (dispx > 0)
   {
      if (negx)
      {
         if (dispx - diffx > 0)
         {
            diffx = dispx - diffx;
            negx = 0;
         }
         else diffx -= -dispx;
      }
      else diffx += dispx;
   }
   else
   {
      if (negx) diffx += -dispx;
      else
      {
         if (diffx + dispx > 0) diffx += dispx;
         else
         {
            diffx = -dispx - diffx;
            negx = 1;
         }
      }
   }

   if (dispy > 0)
   {
      if (negy)
      {
         if (dispy - diffy > 0)
         {
            diffy = dispy - diffy;
            negy = 0;
         }
         else diffy -= -dispy;
      }
      else diffy += dispy;
   }
   else
   {
      if (negy) diffy += -dispy;
      else
      {
         if (diffy + dispy > 0) diffy += dispy;
         else
         {
            diffy = -dispy - diffy;
            negy = 1;
         }
      }
   }

   mouse->mousebits[0] = (overflowy << 7) | (overflowx << 6) | (negy << 5) | (negx << 4) | (mouse->mousebits[0] & 0x0F);
   if (negx) mouse->mousebits[1] = ~(diffx);
   else mouse->mousebits[1] = diffx;
   if (negy) mouse->mousebits[2] = ~(diffy);
   else mouse->mousebits[2] = diffy;
}

//////////////////////////////////////////////////////////////////////////////

void * PerAddPeripheral(PortData_struct *port, int perid)
{
   int pernum = port->data[0] & 0xF;
   int i;
   int peroffset=1;
   u8 size;
   int current = 1;
   void * controller;

   if (pernum == 0xF)
     return NULL;

   // if only one peripheral is connected use 0xF0, otherwise use 0x00 or 0x10
   if (pernum == 0)
   {
      pernum = 1;
      port->data[0] = 0xF1;
   }
   else
   {
      if (pernum == 1)
      {
         u8 tmp = peroffset;
         tmp += (port->data[peroffset] & 0xF) + 1;

         for(i = 0;i < 5;i++)
            port->data[tmp + i] = 0xFF;
      }
      pernum = 6;
      port->data[0] = 0x16;

      // figure out where we're at, then add peripheral id + 1
      current = 0;
      size = port->data[peroffset] & 0xF;
      while ((current < pernum) && (size != 0xF))
      {
         peroffset += size + 1;
         current++;
         size = port->data[peroffset] & 0xF;
      }

      if (current == pernum)
      {
         return NULL;
      }
      current++;
   }

   port->data[peroffset] = perid;
   peroffset++;

   // set peripheral data for peripheral to default values and adjust size
   // of port data
   switch (perid)
   {
      case PERPAD:
         port->data[peroffset] = 0xFF;
         port->data[peroffset+1] = 0xFF;
         port->size = peroffset+2;
         break;
      case PERMOUSE:
         port->data[peroffset] = 0;
         port->data[peroffset + 1] = 0;
         port->data[peroffset + 2] = 0;
         port->size = peroffset + 3;
         break;
      default: break;
   }

   {
      u8 tmp = peroffset;
      tmp += (perid & 0xF);
      for(i = 0;i < (pernum - current);i++)
      {
         port->data[tmp + i] = 0xFF;
         port->size++;
      }
   }

   controller = (port->data + (peroffset - 1));
   switch (perid)
   {
      case PERPAD:
         PerUpdateConfig(perkeybaseconfig, 13, controller);
         break;
      case PERMOUSE:
         PerUpdateConfig(permousebaseconfig, 4, controller);
         break;
   }
   return controller;
}

//////////////////////////////////////////////////////////////////////////////

int PerGetId(void * peripheral)
{
   u8 * id = peripheral;
   return *id;
}

//////////////////////////////////////////////////////////////////////////////

void PerRemovePeripheral(UNUSED PortData_struct *port, UNUSED int removeoffset)
{
   // stub
}

//////////////////////////////////////////////////////////////////////////////

void PerFlush(PortData_struct * port)
{
   /* FIXME this function only flush data if there's a mouse connected as
    * first peripheral */
  u8 perid = port->data[1];
  if (perid == 0xE3)
  {
     PerMouse_struct * mouse = (PerMouse_struct *) (port->data + 1);

     mouse->mousebits[0] &= 0x0F;
     mouse->mousebits[1] = 0;
     mouse->mousebits[2] = 0;
  }
}

//////////////////////////////////////////////////////////////////////////////

void PerKeyDown(u32 key)
{
	unsigned int i = 0;

	while(i < perkeyconfigsize)
	{
		if (key == perkeyconfig[i].key)
		{
			perkeyconfig[i].base->Press(perkeyconfig[i].controller);
		}
		i++;
	}
}

//////////////////////////////////////////////////////////////////////////////

void PerKeyUp(u32 key)
{
	unsigned int i = 0;

	while(i < perkeyconfigsize)
	{
		if (key == perkeyconfig[i].key)
		{
			perkeyconfig[i].base->Release(perkeyconfig[i].controller);
		}
		i++;
	}
}

//////////////////////////////////////////////////////////////////////////////

void PerSetKey(u32 key, u8 name, void * controller)
{
	unsigned int i = 0;

	while(i < perkeyconfigsize)
	{
		if ((name == perkeyconfig[i].base->name) && (controller == perkeyconfig[i].controller))
		{
			perkeyconfig[i].key = key;
		}
		i++;
	}
}

//////////////////////////////////////////////////////////////////////////////

void PerPortReset(void)
{
        PORTDATA1.data[0] = 0xF0;
        PORTDATA1.size = 1;
        PORTDATA2.data[0] = 0xF0;
        PORTDATA2.size = 1;

	perkeyconfigsize = 0;
        if (perkeyconfig)
           free(perkeyconfig);
	perkeyconfig = NULL;
}

//////////////////////////////////////////////////////////////////////////////

void PerUpdateConfig(PerBaseConfig_struct * baseconfig, int nelems, void * controller)
{
   u32 oldsize = perkeyconfigsize;
   u32 i, j;

   perkeyconfigsize += nelems;
   perkeyconfig = realloc(perkeyconfig, perkeyconfigsize * sizeof(PerConfig_struct));
   j = 0;
   for(i = oldsize;i < perkeyconfigsize;i++)
   {
      perkeyconfig[i].base = baseconfig + j;
      perkeyconfig[i].controller = controller;
      j++;
   }
}

//////////////////////////////////////////////////////////////////////////////

PerPad_struct * PerPadAdd(PortData_struct * port)
{
   return PerAddPeripheral(port, PERPAD);
}

//////////////////////////////////////////////////////////////////////////////

PerMouse_struct * PerMouseAdd(PortData_struct * port)
{
   return PerAddPeripheral(port, PERMOUSE);
}

//////////////////////////////////////////////////////////////////////////////
// Dummy Interface
//////////////////////////////////////////////////////////////////////////////

int PERDummyInit(void);
void PERDummyDeInit(void);
int PERDummyHandleEvents(void);
void PERDummyNothing(void);

//static PortData_struct port1;
//static PortData_struct port2;

u32 PERDummyScan(void);
void PERDummyFlush(void);
void PERDummyKeyName(u32 key, char * name, int size);

PerInterface_struct PERDummy = {
PERCORE_DUMMY,
"Dummy Input Interface",
PERDummyInit,
PERDummyDeInit,
PERDummyHandleEvents,
PERDummyNothing,
PERDummyScan,
0,
PERDummyFlush
#ifdef PERKEYNAME
,PERDummyKeyName
#endif
};

//////////////////////////////////////////////////////////////////////////////

int PERDummyInit(void) {

   return 0;
}

//////////////////////////////////////////////////////////////////////////////

void PERDummyDeInit(void) {
}

//////////////////////////////////////////////////////////////////////////////

void PERDummyNothing(void) {
}

//////////////////////////////////////////////////////////////////////////////

int PERDummyHandleEvents(void) {
   if (YabauseExec() != 0)
      return -1;

   return 0;
}

//////////////////////////////////////////////////////////////////////////////

u32 PERDummyScan(void) {
   return 0;
}

//////////////////////////////////////////////////////////////////////////////

void PERDummyFlush(void) {
}

//////////////////////////////////////////////////////////////////////////////

void PERDummyKeyName(UNUSED u32 key, char * name, UNUSED int size) {
	*name = 0;
}
