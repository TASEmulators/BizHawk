/* Mednafen - Multi-system Emulator
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 */

#include <assert.h>

#include "psx.h"
#include "frontio.h"

#include "input/gamepad.h"
#include "input/dualanalog.h"
#include "input/dualshock.h"
#include "input/mouse.h"
#include "input/negcon.h"
#include "input/guncon.h"
#include "input/justifier.h"

#include "input/memcard.h"

#include "input/multitap.h"

#define PSX_FIODBGINFO(format, ...) { /* printf(format " -- timestamp=%d -- PAD temp\n", ## __VA_ARGS__, timestamp); */  }

namespace MDFN_IEN_PSX
{

InputDevice::InputDevice() : chair_r(0), chair_g(0), chair_b(0), chair_x(-1000), chair_y(-1000)
{
}

InputDevice::~InputDevice()
{
}

void InputDevice::Power(void)
{
}

void InputDevice::Update(const pscpu_timestamp_t timestamp)
{

}

void InputDevice::ResetTS(void)
{

}


bool InputDevice::RequireNoFrameskip(void)
{
 return(false);
}

pscpu_timestamp_t InputDevice::GPULineHook(const pscpu_timestamp_t timestamp, bool vsync, uint32 *pixels, const MDFN_PixelFormat* const format, const unsigned width, const unsigned pix_clock_offset, const unsigned pix_clock, const unsigned pix_clock_divider)
{
 return(PSX_EVENT_MAXTS);
}


void InputDevice::UpdateInput(const void *data)
{
}


void InputDevice::SetDTR(bool new_dtr)
{

}

bool InputDevice::GetDSR(void)
{
 return(0);
}

bool InputDevice::Clock(bool TxD, int32 &dsr_pulse_delay)
{
 dsr_pulse_delay = 0;

 return(1);
}

uint32 InputDevice::GetNVSize(void) const
{
 return(0);
}

const uint8* InputDevice::ReadNV(void) const
{
 return NULL;
}

void InputDevice::WriteNV(const uint8 *buffer, uint32 offset, uint32 count)
{

}

uint64 InputDevice::GetNVDirtyCount(void) const
{
 return(0);
}

void InputDevice::ResetNVDirtyCount(void)
{

}

//an old snippet tha showshow to set up a multitap device
  //if(emulate_multitap[mp])
  // DevicesTap[mp]->SetSubDevice(EP_to_SP(emulate_multitap, i), Devices[i], emulate_memcards[i] ? DevicesMC[i] : DummyDevice);
  //else
  // DevicesTap[mp]->SetSubDevice(EP_to_SP(emulate_multitap, i), DummyDevice, DummyDevice);


FrontIO::FrontIO()
{
	//a dummy device used for memcards (please rename me)
	DummyDevice = new InputDevice();

	for(int i=0;i<2;i++)
	{
		Ports[i] = new InputDevice();
		PortData[i] = NULL;
		MCPorts[i] = new InputDevice();
	}
} 


FrontIO::~FrontIO()
{
	for(int i=0;i<2;i++)
	{
		delete Ports[i];
		delete MCPorts[i];
	}
	delete DummyDevice;
}

pscpu_timestamp_t FrontIO::CalcNextEventTS(pscpu_timestamp_t timestamp, int32 next_event)
{
 pscpu_timestamp_t ret;

 if(ClockDivider > 0 && ClockDivider < next_event)
  next_event = ClockDivider;

 for(int i = 0; i < 4; i++)
  if(dsr_pulse_delay[i] > 0 && next_event > dsr_pulse_delay[i])
   next_event = dsr_pulse_delay[i];

 ret = timestamp + next_event;

 if(irq10_pulse_ts[0] < ret)
  ret = irq10_pulse_ts[0];

 if(irq10_pulse_ts[1] < ret)
  ret = irq10_pulse_ts[1];

 return(ret);
}

static const uint8 ScaleShift[4] = { 0, 0, 4, 6 };

void FrontIO::CheckStartStopPending(pscpu_timestamp_t timestamp, bool skip_event_set)
{
 //const bool prior_ReceiveInProgress = ReceiveInProgress;
 //const bool prior_TransmitInProgress = TransmitInProgress;
 bool trigger_condition = false;

 trigger_condition = (ReceivePending && (Control & 0x4)) || (TransmitPending && (Control & 0x1));

 if(trigger_condition)
 {
  if(ReceivePending)
  {
   ReceivePending = false;
   ReceiveInProgress = true;
   ReceiveBufferAvail = false;
   ReceiveBuffer = 0;
   ReceiveBitCounter = 0;
  }

  if(TransmitPending)
  {
   TransmitPending = false;
   TransmitInProgress = true;
   TransmitBitCounter = 0;
  }

  ClockDivider = std::max<uint32>(0x20, (Baudrate << ScaleShift[Mode & 0x3]) & ~1); // Minimum of 0x20 is an emulation sanity check to prevent severe performance degradation.
  //printf("CD: 0x%02x\n", ClockDivider);
 }

 if(!(Control & 0x5))
 {
  ReceiveInProgress = false;
  TransmitInProgress = false;
 }

 if(!ReceiveInProgress && !TransmitInProgress)
  ClockDivider = 0;

 if(!(skip_event_set))
  PSX_SetEventNT(PSX_EVENT_FIO, CalcNextEventTS(timestamp, 0x10000000));
}

// DSR IRQ bit setting appears(from indirect tests on real PS1) to be level-sensitive, not edge-sensitive
INLINE void FrontIO::DoDSRIRQ(void)
{
 if(Control & 0x1000)
 {
  PSX_FIODBGINFO("[DSR] IRQ");
  istatus = true;
  IRQ_Assert(IRQ_SIO, true);
 }
}



void FrontIO::Write(pscpu_timestamp_t timestamp, uint32 A, uint32 V)
{
 PSX_FIODBGINFO("[FIO] Write: %08x %08x", A, V);

 V <<= (A & 1) * 8;

 Update(timestamp);

 switch(A & 0xE)
 {
  case 0x0:
  case 0x2:
        V <<= (A & 2) * 8;
	TransmitBuffer = V;
	TransmitPending = true;
	TransmitInProgress = false;
	break;

  case 0x8:
	Mode = V & 0x013F;
	break;

  case 0xa:
	if(ClockDivider > 0 && ((V & 0x2000) != (Control & 0x2000)) && ((Control & 0x2) == (V & 0x2))  )
	 PSX_DBG(PSX_DBG_WARNING, "FIO device selection changed during comm %04x->%04x\n", Control, V);

	//printf("Control: %d, %04x\n", timestamp, V);
	Control = V & 0x3F2F;

	if(V & 0x10)
        {
	 istatus = false;
	 IRQ_Assert(IRQ_SIO, false);
	}

	if(V & 0x40)	// Reset
	{
	 istatus = false;
	 IRQ_Assert(IRQ_SIO, false);

	 ClockDivider = 0;
	 ReceivePending = false;
	 TransmitPending = false;

	 ReceiveInProgress = false;
	 TransmitInProgress = false;

	 ReceiveBufferAvail = false;

	 TransmitBuffer = 0;
	 ReceiveBuffer = 0;

	 ReceiveBitCounter = 0;
	 TransmitBitCounter = 0;

	 Mode = 0;
	 Control = 0;
	 Baudrate = 0;
	}

	Ports[0]->SetDTR((Control & 0x2) && !(Control & 0x2000));
        MCPorts[0]->SetDTR((Control & 0x2) && !(Control & 0x2000));
	Ports[1]->SetDTR((Control & 0x2) && (Control & 0x2000));
        MCPorts[1]->SetDTR((Control & 0x2) && (Control & 0x2000));

#if 1
if(!((Control & 0x2) && !(Control & 0x2000)))
{
 dsr_pulse_delay[0] = 0;
 dsr_pulse_delay[2] = 0;
 dsr_active_until_ts[0] = -1;
 dsr_active_until_ts[2] = -1;
}

if(!((Control & 0x2) && (Control & 0x2000)))
{
 dsr_pulse_delay[1] = 0;
 dsr_pulse_delay[3] = 0;
 dsr_active_until_ts[1] = -1;
 dsr_active_until_ts[3] = -1;
}

#endif
	// TODO: Uncomment out in the future once our CPU emulation is a bit more accurate with timing, to prevent causing problems with games
	// that may clear the IRQ in an unsafe pattern that only works because its execution was slow enough to allow DSR to go inactive.  (Whether or not
	// such games even exist though is unknown!)
	//if(timestamp < dsr_active_until_ts[0] || timestamp < dsr_active_until_ts[1] || timestamp < dsr_active_until_ts[2] || timestamp < dsr_active_until_ts[3])
	// DoDSRIRQ();

	break;

  case 0xe:
	Baudrate = V;
	//printf("%02x\n", V);
	//MDFN_DispMessage("%02x\n", V);
	break;
 }

 CheckStartStopPending(timestamp, false);
}

uint32 FrontIO::Read(pscpu_timestamp_t timestamp, uint32 A)
{
 uint32 ret = 0;

 Update(timestamp);

 switch(A & 0xE)
 {
  case 0x0:
  case 0x2:
	//printf("FIO Read: 0x%02x\n", ReceiveBuffer);
	ret = ReceiveBuffer | (ReceiveBuffer << 8) | (ReceiveBuffer << 16) | (ReceiveBuffer << 24);
	ReceiveBufferAvail = false;
	ReceivePending = true;
	ReceiveInProgress = false;
	CheckStartStopPending(timestamp, false);
	ret >>= (A & 2) * 8;
	break;

  case 0x4:
	ret = 0;

	if(!TransmitPending && !TransmitInProgress)
	 ret |= 0x1;

	if(ReceiveBufferAvail)
	 ret |= 0x2;

	if(timestamp < dsr_active_until_ts[0] || timestamp < dsr_active_until_ts[1] || timestamp < dsr_active_until_ts[2] || timestamp < dsr_active_until_ts[3])
	 ret |= 0x80;

	if(istatus)
	 ret |= 0x200;

	break;

  case 0x8:
	ret = Mode;
	break;

  case 0xa:
	ret = Control;
	break;

  case 0xe:
	ret = Baudrate;
	break;
 }

 ret >>= (A & 1) * 8;

 if((A & 0xF) != 0x4)
  PSX_FIODBGINFO("[FIO] Read: %08x %08x", A, ret);

 return(ret);
}

pscpu_timestamp_t FrontIO::Update(pscpu_timestamp_t timestamp)
{
 int32 clocks = timestamp - lastts;
 bool need_start_stop_check = false;

 for(int i = 0; i < 4; i++)
  if(dsr_pulse_delay[i] > 0)
  {
   dsr_pulse_delay[i] -= clocks;
   if(dsr_pulse_delay[i] <= 0)
   {
    dsr_active_until_ts[i] = timestamp + 32 + dsr_pulse_delay[i];
    DoDSRIRQ();
   }
  }

 for(int i = 0; i < 2; i++)
 {
  if(timestamp >= irq10_pulse_ts[i])
  {
   //printf("Yay: %d %u\n", i, timestamp);
   irq10_pulse_ts[i] = PSX_EVENT_MAXTS;
   IRQ_Assert(IRQ_PIO, true);
   IRQ_Assert(IRQ_PIO, false);
  }
 }

 if(ClockDivider > 0)
 {
  ClockDivider -= clocks;

  while(ClockDivider <= 0)
  {
   if(ReceiveInProgress || TransmitInProgress)
   {
    bool rxd = 0, txd = 0;
    const uint32 BCMask = 0x07;

    if(TransmitInProgress)
    {
     txd = (TransmitBuffer >> TransmitBitCounter) & 1;
     TransmitBitCounter = (TransmitBitCounter + 1) & BCMask;
     if(!TransmitBitCounter)
     {
      need_start_stop_check = true;
      PSX_FIODBGINFO("[FIO] Data transmitted: %08x", TransmitBuffer);
      TransmitInProgress = false;

      if(Control & 0x400)
      {
       istatus = true;
       IRQ_Assert(IRQ_SIO, true);
      }
     }
    }

    rxd = Ports[0]->Clock(txd, dsr_pulse_delay[0]) & Ports[1]->Clock(txd, dsr_pulse_delay[1]) &
	  MCPorts[0]->Clock(txd, dsr_pulse_delay[2]) & MCPorts[1]->Clock(txd, dsr_pulse_delay[3]);

    if(ReceiveInProgress)
    {
     ReceiveBuffer &= ~(1 << ReceiveBitCounter);
     ReceiveBuffer |= rxd << ReceiveBitCounter;

     ReceiveBitCounter = (ReceiveBitCounter + 1) & BCMask;

     if(!ReceiveBitCounter)
     {
      need_start_stop_check = true;
      PSX_FIODBGINFO("[FIO] Data received: %08x", ReceiveBuffer);

      ReceiveInProgress = false;
      ReceiveBufferAvail = true;

      if(Control & 0x800)
      {
       istatus = true;
       IRQ_Assert(IRQ_SIO, true);
      }
     }
    }
    ClockDivider += std::max<uint32>(0x20, (Baudrate << ScaleShift[Mode & 0x3]) & ~1); // Minimum of 0x20 is an emulation sanity check to prevent severe performance degradation.
   }
   else
    break;
  }
 }


 lastts = timestamp;


 if(need_start_stop_check)
 {
  CheckStartStopPending(timestamp, true);
 }

 return(CalcNextEventTS(timestamp, 0x10000000));
}

void FrontIO::ResetTS(void)
{
	for(int i=0;i<2;i++)
	{
		if(Ports[i] != NULL)
		{
			Ports[i]->Update(lastts); 	// Maybe eventually call Update() from FrontIO::Update() and remove this(but would hurt speed)?
			Ports[i]->ResetTS();
		}

		if(MCPorts[i] != NULL)
		{
			MCPorts[i]->Update(lastts); 	// Maybe eventually call Update() from FrontIO::Update() and remove this(but would hurt speed)?
			MCPorts[i]->ResetTS();
		}
	}

 for(int i = 0; i < 2; i++)
 {
  if(irq10_pulse_ts[i] != PSX_EVENT_MAXTS)
   irq10_pulse_ts[i] -= lastts;
 }

 for(int i = 0; i < 4; i++)
 {
  if(dsr_active_until_ts[i] >= 0)
  {
   dsr_active_until_ts[i] -= lastts;
   //printf("SPOOONY: %d %d\n", i, dsr_active_until_ts[i]);
  }
 }
 lastts = 0;
}


void FrontIO::Reset(bool powering_up)
{
 for(int i = 0; i < 4; i++)
 {
  dsr_pulse_delay[i] = 0;
  dsr_active_until_ts[i] = -1;
 }

 for(int i = 0; i < 2; i++)
 {
  irq10_pulse_ts[i] = PSX_EVENT_MAXTS;
 }

 lastts = 0;

 //
 //

 ClockDivider = 0;

 ReceivePending = false;
 TransmitPending = false;

 ReceiveInProgress = false;
 TransmitInProgress = false;

 ReceiveBufferAvail = false;

 TransmitBuffer = 0;
 ReceiveBuffer = 0;

 ReceiveBitCounter = 0;
 TransmitBitCounter = 0;

 Mode = 0;
 Control = 0;
 Baudrate = 0;

 //power on all plugged devices (are we doing this when attaching them?)
 if(powering_up)
 {
	 for(int i=0;i<2;i++)
	 {
		 if(Ports[i] != NULL) Ports[i]->Power();
		 if(MCPorts[i] != NULL) MCPorts[i]->Power();
	 }
 }

 istatus = false;
}

void FrontIO::UpdateInput(void)
{
	for(int i=0;i<2;i++)
	{
		if(Ports[i] != NULL) Ports[i]->UpdateInput(PortData[i]);
	}
}

// Take care to call ->Power() only if the device actually changed.
void FrontIO::SetInput(unsigned int port, const char *type, void *ptr)
{
	//clean up the old device
	delete Ports[port];
	Ports[port] = NULL;

 if(!strcmp(type, "gamepad") || !strcmp(type, "dancepad"))
  Ports[port] = Device_Gamepad_Create();
 else if(!strcmp(type, "dualanalog"))
  Ports[port] = Device_DualAnalog_Create(false);
 else if(!strcmp(type, "analogjoy"))
  Ports[port] = Device_DualAnalog_Create(true);
 else if(!strcmp(type, "dualshock"))
  Ports[port] = Device_DualShock_Create();
 else if(!strcmp(type, "mouse"))
  Ports[port] = Device_Mouse_Create();
 else if(!strcmp(type, "negcon"))
  Ports[port] = Device_neGcon_Create();
 else if(!strcmp(type, "guncon"))
  Ports[port] = Device_GunCon_Create();
 else if(!strcmp(type, "justifier"))
  Ports[port] = Device_Justifier_Create();
 else
  Ports[port] = new InputDevice();

 // " Take care to call ->Power() only if the device actually changed. " - TO THINK ABOUT. maybe irrelevant in octoshock
 //if(Devices[port] != nd)

 //OCTOSHOCK TODO - not sure I understand this
 if(port < 2)
  irq10_pulse_ts[port] = PSX_EVENT_MAXTS;

 Ports[port]->Power();
 PortData[port] = ptr;
}

uint64 FrontIO::GetMemcardDirtyCount(unsigned int which)
{
 assert(which < 2);
 
 return(MCPorts[which]->GetNVDirtyCount());
}

//TODO - ok, savestating varying input devices. this is tricky.
//its like... what happens when the hardware unfreezes with different input attached? 
//thats some kind of instantaneous change event which shouldnt/cant be properly emulated or likely even implemented
//so in that respect it's very much like (if not identical to) CDs.
//heres a discussion question. what are we doing here? savestating the CONSOLE or savestating the ENTIRE SYSTEM?
//well, what's being emulated?
//I dont know. lets save it for later.
//You know, this is one reason mednafen had a distinction between ports and devices.
//But I had to get rid of it, I just had to. At least they need to be organized into a pool differently somehow.

//Anyway, think about this: We cant just savestate the entire system. The game will be depending on the input devices being in a certain state.
//If theyre in any other state, literally, any other state, then the game will fail.
//Therefore the entire system needs saving together and mismatches MUST NOT BE PERMITTED.

SYNCFUNC(FrontIO)
{
	NSS(ClockDivider);

	NSS(ReceivePending);
	NSS(TransmitPending);

	NSS(ReceiveInProgress);
	NSS(TransmitInProgress);

	NSS(ReceiveBufferAvail);

	NSS(ReceiveBuffer);
	NSS(TransmitBuffer);

	NSS(ReceiveBitCounter);
	NSS(TransmitBitCounter);

	NSS(Mode);
	NSS(Control);
	NSS(Baudrate);

	NSS(istatus);

	// FIXME: Step mode save states.
	NSS(irq10_pulse_ts);
	NSS(dsr_pulse_delay);
	NSS(dsr_active_until_ts);

	//state actions for ports and such
	for(int i=0;i<2;i++)
	{
		ns->EnterSection("PORT%d",i);
		Ports[i]->SyncState(isReader,ns);
		ns->ExitSection("PORT%d",i);
		ns->EnterSection("MCPORT%d",i);
		MCPorts[i]->SyncState(isReader,ns);
		ns->ExitSection("MCPORT%d",i);
	}

	//more of this crap....
	if(isReader)
 {
  IRQ_Assert(IRQ_SIO, istatus);
 }

}

bool FrontIO::RequireNoFrameskip(void)
{
	//this whole function is nonsense. frontend should know what it has attached
 return(false);
}


void FrontIO::GPULineHook(const pscpu_timestamp_t timestamp, const pscpu_timestamp_t line_timestamp, bool vsync, uint32 *pixels, const MDFN_PixelFormat* const format, const unsigned width, const unsigned pix_clock_offset, const unsigned pix_clock, const unsigned pix_clock_divider)
{
 Update(timestamp);

 for(unsigned i = 0; i < 2; i++)
 {
  pscpu_timestamp_t plts = Ports[i]->GPULineHook(line_timestamp, vsync, pixels, format, width, pix_clock_offset, pix_clock, pix_clock_divider);

  irq10_pulse_ts[i] = plts;

  if(irq10_pulse_ts[i] <= timestamp)
  {
   irq10_pulse_ts[i] = PSX_EVENT_MAXTS;
   IRQ_Assert(IRQ_PIO, true);
   IRQ_Assert(IRQ_PIO, false);
  }
 }


 PSX_SetEventNT(PSX_EVENT_FIO, CalcNextEventTS(timestamp, 0x10000000));
}




}
