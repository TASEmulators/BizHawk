#include "../psx.h"
#include "../frontio.h"
#include "gamepad.h"

namespace MDFN_IEN_PSX
{

class InputDevice_Gamepad : public InputDevice
{
 public:

 InputDevice_Gamepad();
 virtual ~InputDevice_Gamepad();

 virtual void Power(void);
 virtual void UpdateInput(const void *data);
 virtual void SyncState(bool isReader, EW::NewState *ns);

 //
 //
 //
 virtual void SetDTR(bool new_dtr);
 virtual bool GetDSR(void);
 virtual bool Clock(bool TxD, int32 &dsr_pulse_delay);

 private:

//non-serialized state
 IO_Gamepad* io;

 bool dtr;

 uint8 buttons[2];

 int32 command_phase;
 uint32 bitpos;
 uint8 receive_buffer;

 uint8 command;

 uint8 transmit_buffer[3];
 uint32 transmit_pos;
 uint32 transmit_count;
};

InputDevice_Gamepad::InputDevice_Gamepad()
{
 Power();
}

InputDevice_Gamepad::~InputDevice_Gamepad()
{

}

void InputDevice_Gamepad::Power(void)
{
 dtr = 0;

 buttons[0] = buttons[1] = 0;

 command_phase = 0;

 bitpos = 0;

 receive_buffer = 0;

 command = 0;

 memset(transmit_buffer, 0, sizeof(transmit_buffer));

 transmit_pos = 0;
 transmit_count = 0;
}

void InputDevice_Gamepad::SyncState(bool isReader, EW::NewState *ns)
{
	NSS(dtr);

	NSS(buttons);

	NSS(command_phase);
	NSS(bitpos);
	NSS(receive_buffer);

	NSS(command);

	NSS(transmit_buffer);
	NSS(transmit_pos);
	NSS(transmit_count);
}

void InputDevice_Gamepad::UpdateInput(const void *data)
{
	io = (IO_Gamepad*)data;

 buttons[0] = io->buttons[0];
 buttons[1] = io->buttons[1];
}


void InputDevice_Gamepad::SetDTR(bool new_dtr)
{
 if(!dtr && new_dtr)
 {
  command_phase = 0;
  bitpos = 0;
  transmit_pos = 0;
  transmit_count = 0;
 }
 else if(dtr && !new_dtr)
 {
  //if(bitpos || transmit_count)
  // printf("[PAD] Abort communication!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!\n");
 }

 dtr = new_dtr;
}

bool InputDevice_Gamepad::GetDSR(void)
{
 if(!dtr)
  return(0);

 if(!bitpos && transmit_count)
  return(1);

 return(0);
}

bool InputDevice_Gamepad::Clock(bool TxD, int32 &dsr_pulse_delay)
{
 bool ret = 1;

 dsr_pulse_delay = 0;

 if(!dtr)
  return(1);

 if(transmit_count)
  ret = (transmit_buffer[transmit_pos] >> bitpos) & 1;

 receive_buffer &= ~(1 << bitpos);
 receive_buffer |= TxD << bitpos;
 bitpos = (bitpos + 1) & 0x7;

 if(!bitpos)
 {
  //printf("[PAD] Receive: %02x -- command_phase=%d\n", receive_buffer, command_phase);

  if(transmit_count)
  {
   transmit_pos++;
   transmit_count--;
  }


  switch(command_phase)
  {
   case 0:
 	  if(receive_buffer != 0x01)
	    command_phase = -1;
	  else
	  {
	   transmit_buffer[0] = 0x41;
	   transmit_pos = 0;
	   transmit_count = 1;
	   command_phase++;
	  }
	  break;

   case 1:
	command = receive_buffer;
	command_phase++;

	transmit_buffer[0] = 0x5A;

	//if(command != 0x42)
	// fprintf(stderr, "Gamepad unhandled command: 0x%02x\n", command);
	//assert(command == 0x42);
	if(command == 0x42)
	{
	 //printf("PAD COmmand 0x42, sl=%u\n", GPU->GetScanlineNum());

	 transmit_buffer[1] = 0xFF ^ buttons[0];
	 transmit_buffer[2] = 0xFF ^ buttons[1];
	 transmit_pos = 0;
	 transmit_count = 3;
		io->active = true;
	}
	else
	{
		command_phase = -1;
		transmit_buffer[1] = 0;
		transmit_buffer[2] = 0;
		transmit_pos = 0;
		transmit_count = 0;
	}
	break;

  }
 }

 if(!bitpos && transmit_count)
  dsr_pulse_delay = 0x40; //0x100;

 return(ret);
}

InputDevice *Device_Gamepad_Create(void)
{
 return new InputDevice_Gamepad();
}



}
