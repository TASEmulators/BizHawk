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

InputDevice::InputDevice() : chair_r(0), chair_g(0), chair_b(0), draw_chair(0), chair_x(-1000), chair_y(-1000)
{
}

InputDevice::~InputDevice()
{
}

void InputDevice::Power(void)
{
}

int InputDevice::StateAction(StateMem* sm, int load, int data_only, const char* section_name)
{
 return(1);
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

uint32 InputDevice::GetNVSize(void)
{
 return(0);
}

void InputDevice::ReadNV(uint8 *buffer, uint32 offset, uint32 count)
{

}

void InputDevice::WriteNV(const uint8 *buffer, uint32 offset, uint32 count)
{

}

uint64 InputDevice::GetNVDirtyCount(void)
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

	//always add one memory device for now
	MCPorts[0] = Device_Memcard_Create();
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
 assert(!(A & 0x1));

 PSX_FIODBGINFO("[FIO] Write: %08x %08x", A, V);

 Update(timestamp);

 switch(A & 0xF)
 {
  case 0x0:
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

 assert(!(A & 0x1));

 Update(timestamp);

 switch(A & 0xF)
 {
  case 0x0:
	//printf("FIO Read: 0x%02x\n", ReceiveBuffer);
	ret = ReceiveBuffer | (ReceiveBuffer << 8) | (ReceiveBuffer << 16) | (ReceiveBuffer << 24);
	ReceiveBufferAvail = false;
	ReceivePending = true;
	ReceiveInProgress = false;
	CheckStartStopPending(timestamp, false);
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


void FrontIO::Power(void)
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
 for(int i=0;i<2;i++)
 {
	 if(Ports[i] != NULL) Ports[i]->Power();
	 if(MCPorts[i] != NULL) MCPorts[i]->Power();
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

void FrontIO::SetInput(unsigned int port, const char *type, void *ptr)
{
	//clean up the old device
	delete Ports[port];
	Ports[port] = NULL;

 //OCTOSHOCK TODO - not sure I understand this
 if(port < 2)
  irq10_pulse_ts[port] = PSX_EVENT_MAXTS;

 if(!strcmp(type, "gamepad") || !strcmp(type, "dancepad"))
  Ports[port] = Device_Gamepad_Create();
 else if(!strcmp(type, "dualanalog"))
  Ports[port] = Device_DualAnalog_Create(false);
 else if(!strcmp(type, "analogjoy"))
  Ports[port] = Device_DualAnalog_Create(true);
 else if(!strcmp(type, "dualshock"))
 {
  char name[256];
  snprintf(name, 256, "DualShock on port %u", port + 1);
  Ports[port] = Device_DualShock_Create(std::string(name));
 }
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

 //Devices[port]->SetCrosshairsColor(chair_colors[port]);
 PortData[port] = ptr;
}

uint64 FrontIO::GetMemcardDirtyCount(unsigned int which)
{
 assert(which < 2);
 
 return(MCPorts[which]->GetNVDirtyCount());
}

int FrontIO::StateAction(StateMem* sm, int load, int data_only)
{
 SFORMAT StateRegs[] =
 {
  SFVAR(ClockDivider),

  SFVAR(ReceivePending),
  SFVAR(TransmitPending),

  SFVAR(ReceiveInProgress),
  SFVAR(TransmitInProgress),

  SFVAR(ReceiveBufferAvail),

  SFVAR(ReceiveBuffer),
  SFVAR(TransmitBuffer),

  SFVAR(ReceiveBitCounter),
  SFVAR(TransmitBitCounter),

  SFVAR(Mode),
  SFVAR(Control),
  SFVAR(Baudrate),

  SFVAR(istatus),

  // FIXME: Step mode save states.
  SFARRAY32(irq10_pulse_ts, sizeof(irq10_pulse_ts) / sizeof(irq10_pulse_ts[0])),
  SFARRAY32(dsr_pulse_delay, sizeof(dsr_pulse_delay) / sizeof(dsr_pulse_delay[0])),
  SFARRAY32(dsr_active_until_ts, sizeof(dsr_active_until_ts) / sizeof(dsr_active_until_ts[0])),

  SFEND
 };

 int ret = MDFNSS_StateAction(sm, load, data_only, StateRegs, "FIO");

 //TODO - SAVESTATES

 //for(unsigned i = 0; i < 8; i++)
 //{
	//static const char* labels[] = {
	//	"FIODEV0","FIODEV1","FIODEV2","FIODEV3","FIODEV4","FIODEV5","FIODEV6","FIODEV7"
	//};

 // ret &= Devices[i]->StateAction(sm, load, data_only, labels[i]);
 //}

 //for(unsigned i = 0; i < 8; i++)
 //{
	//static const char* labels[] = {
	//	"FIOMC0","FIOMC1","FIOMC2","FIOMC3","FIOMC4","FIOMC5","FIOMC6","FIOMC7"
	//};


 // ret &= DevicesMC[i]->StateAction(sm, load, data_only, labels[i]);
 //}

 //for(unsigned i = 0; i < 2; i++)
 //{
	//static const char* labels[] = {
	//	"FIOTAP0","FIOTAP1",
	//};

 // ret &= DevicesTap[i]->StateAction(sm, load, data_only, labels[i]);
 //}

 if(load)
 {
  IRQ_Assert(IRQ_SIO, istatus);
 }

 return(ret);
}

bool FrontIO::RequireNoFrameskip(void)
{
	//this whole function is nonsense. frontend should know what it has attached
 return(false);
}

void FrontIO::GPULineHook(const pscpu_timestamp_t timestamp, const pscpu_timestamp_t line_timestamp, bool vsync, uint32 *pixels, const MDFN_PixelFormat* const format, const unsigned width, const unsigned pix_clock_offset, const unsigned pix_clock, const unsigned pix_clock_divider)
{
 Update(timestamp);

 for(int i = 0; i < 2; i++)
 {
	 //octoshock edits.. not sure how safe it is
	 if(Ports[i] == NULL)
		 continue;

	pscpu_timestamp_t plts = Ports[i]->GPULineHook(line_timestamp, vsync, pixels, format, width, pix_clock_offset, pix_clock, pix_clock_divider);

  if(i < 2)
  {
   irq10_pulse_ts[i] = plts;

   if(irq10_pulse_ts[i] <= timestamp)
   {
    irq10_pulse_ts[i] = PSX_EVENT_MAXTS;
    IRQ_Assert(IRQ_PIO, true);
    IRQ_Assert(IRQ_PIO, false);
   }
  }
 }

 PSX_SetEventNT(PSX_EVENT_FIO, CalcNextEventTS(timestamp, 0x10000000));
}

static InputDeviceInfoStruct InputDeviceInfoPSXPort[] =
{
 // None
 {
  "none",
  "none",
  NULL,
  NULL,
  0,
  NULL 
 },

 // Gamepad(SCPH-1080)
 {
  "gamepad",
  "Digital Gamepad",
  "PlayStation digital gamepad; SCPH-1080.",
  NULL,
  sizeof(Device_Gamepad_IDII) / sizeof(InputDeviceInputInfoStruct),
  Device_Gamepad_IDII,
 },

 // Dual Shock Gamepad(SCPH-1200)
 {
  "dualshock",
  "DualShock",
  "DualShock gamepad; SCPH-1200.  Emulation in Mednafen includes the analog mode toggle button.  Rumble is emulated, but currently only supported on Linux, and MS Windows via the XInput API and XInput-compatible gamepads/joysticks.  If you're having trouble getting rumble to work on Linux, see if Mednafen is printing out error messages during startup regarding /dev/input/event*, and resolve the issue(s) as necessary.",
  NULL,
  sizeof(Device_DualShock_IDII) / sizeof(InputDeviceInputInfoStruct),
  Device_DualShock_IDII,
 },

 // Dual Analog Gamepad(SCPH-1180), forced to analog mode.
 {
  "dualanalog",
  "Dual Analog",
  "Dual Analog gamepad; SCPH-1180.  It is the predecessor/prototype to the more advanced DualShock.  Emulated in Mednafen as forced to analog mode, and without rumble.",
  NULL,
  sizeof(Device_DualAnalog_IDII) / sizeof(InputDeviceInputInfoStruct),
  Device_DualAnalog_IDII,
 },


 // Analog joystick(SCPH-1110), forced to analog mode - emulated through a tweak to dual analog gamepad emulation.
 {
  "analogjoy",
  "Analog Joystick",
  "Flight-game-oriented dual-joystick controller; SCPH-1110.   Emulated in Mednafen as forced to analog mode.",
  NULL,
  sizeof(Device_AnalogJoy_IDII) / sizeof(InputDeviceInputInfoStruct),
  Device_AnalogJoy_IDII,
 },

 {
  "mouse",
  "Mouse",
  NULL,
  NULL,
  sizeof(Device_Mouse_IDII) / sizeof(InputDeviceInputInfoStruct),
  Device_Mouse_IDII,
 },

 {
  "negcon",
  "neGcon",
  "Namco's unconventional twisty racing-game-oriented gamepad; NPC-101.",
  NULL,
  sizeof(Device_neGcon_IDII) / sizeof(InputDeviceInputInfoStruct),
  Device_neGcon_IDII,
 },

 {
  "guncon",
  "GunCon",
  "Namco's light gun; NPC-103.",
  NULL,
  sizeof(Device_GunCon_IDII) / sizeof(InputDeviceInputInfoStruct),
  Device_GunCon_IDII,
 },

 {
  "justifier",
  "Konami Justifier",
  "Konami's light gun; SLUH-00017.  Rumored to be wrought of the coagulated rage of all who tried to shoot The Dog.  If the game you want to play supports the \"GunCon\", you should use that instead. NOTE: Currently does not work properly when on any of ports 1B-1D and 2B-2D.",
  NULL,
  sizeof(Device_Justifier_IDII) / sizeof(InputDeviceInputInfoStruct),
  Device_Justifier_IDII,
 },

 {
  "dancepad",
  "Dance Pad",
  "Dingo Dingo Rodeo!",
  NULL,
  sizeof(Device_Dancepad_IDII) / sizeof(InputDeviceInputInfoStruct),
  Device_Dancepad_IDII,
 },

};

static const InputPortInfoStruct PortInfo[] =
{
 { "port1", "Virtual Port 1", sizeof(InputDeviceInfoPSXPort) / sizeof(InputDeviceInfoStruct), InputDeviceInfoPSXPort, "gamepad" },
 { "port2", "Virtual Port 2", sizeof(InputDeviceInfoPSXPort) / sizeof(InputDeviceInfoStruct), InputDeviceInfoPSXPort, "gamepad" },
 { "port3", "Virtual Port 3", sizeof(InputDeviceInfoPSXPort) / sizeof(InputDeviceInfoStruct), InputDeviceInfoPSXPort, "gamepad" },
 { "port4", "Virtual Port 4", sizeof(InputDeviceInfoPSXPort) / sizeof(InputDeviceInfoStruct), InputDeviceInfoPSXPort, "gamepad" },
 { "port5", "Virtual Port 5", sizeof(InputDeviceInfoPSXPort) / sizeof(InputDeviceInfoStruct), InputDeviceInfoPSXPort, "gamepad" },
 { "port6", "Virtual Port 6", sizeof(InputDeviceInfoPSXPort) / sizeof(InputDeviceInfoStruct), InputDeviceInfoPSXPort, "gamepad" },
 { "port7", "Virtual Port 7", sizeof(InputDeviceInfoPSXPort) / sizeof(InputDeviceInfoStruct), InputDeviceInfoPSXPort, "gamepad" },
 { "port8", "Virtual Port 8", sizeof(InputDeviceInfoPSXPort) / sizeof(InputDeviceInfoStruct), InputDeviceInfoPSXPort, "gamepad" },
};

InputInfoStruct FIO_InputInfo =
{
 sizeof(PortInfo) / sizeof(InputPortInfoStruct),
 PortInfo
};


}
