#include "../psx.h"
#include "../frontio.h"
#include "dualanalog.h"

namespace MDFN_IEN_PSX
{

class InputDevice_DualAnalog : public InputDevice
{
 public:

 InputDevice_DualAnalog(bool joystick_mode_);
 virtual ~InputDevice_DualAnalog();

 virtual void Power(void);
 virtual void UpdateInput(const void *data);

 //
 //
 //
 virtual void SetDTR(bool new_dtr);
 virtual bool GetDSR(void);
 virtual bool Clock(bool TxD, int32 &dsr_pulse_delay);

 private:

//non-serialized state
 IO_DualAnalog* io;

 bool joystick_mode;
 bool dtr;

 uint8 buttons[2];
 uint8 axes[2][2];

 int32 command_phase;
 uint32 bitpos;
 uint8 receive_buffer;

 uint8 command;

 uint8 transmit_buffer[8];
 uint32 transmit_pos;
 uint32 transmit_count;
};

InputDevice_DualAnalog::InputDevice_DualAnalog(bool joystick_mode_) : joystick_mode(joystick_mode_)
{
 Power();
}

InputDevice_DualAnalog::~InputDevice_DualAnalog()
{

}

void InputDevice_DualAnalog::Power(void)
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


void InputDevice_DualAnalog::UpdateInput(const void *data)
{
	io = (IO_DualAnalog*)data;

 buttons[0] = io->buttons[0];
 buttons[1] = io->buttons[1];

  //OCTOSHOCK EDIT - so we can set values directly
 //for(int stick = 0; stick < 2; stick++)
 //{
 // for(int axis = 0; axis < 2; axis++)
 // {
 //  const uint8* aba = &d8[2] + stick * 8 + axis * 4;
 //  int32 tmp;

 //  tmp = 32768 + MDFN_de16lsb(&aba[0]) - ((int32)MDFN_de16lsb(&aba[2]) * 32768 / 32767);
 //  tmp >>= 8;

 //  axes[stick][axis] = tmp;
 // }
 //}

	axes[0][0] = io->right_x;
	axes[0][1] = io->right_y;
	axes[1][0] = io->left_x;
	axes[1][1] = io->left_y;

 //printf("%d %d %d %d\n", axes[0][0], axes[0][1], axes[1][0], axes[1][1]);
}


void InputDevice_DualAnalog::SetDTR(bool new_dtr)
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

bool InputDevice_DualAnalog::GetDSR(void)
{
 if(!dtr)
  return(0);

 if(!bitpos && transmit_count)
  return(1);

 return(0);
}

bool InputDevice_DualAnalog::Clock(bool TxD, int32 &dsr_pulse_delay)
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
	   transmit_buffer[0] = joystick_mode ? 0x53 : 0x73;
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

	if(command == 0x42)
	{
	 transmit_buffer[1] = 0xFF ^ buttons[0];
	 transmit_buffer[2] = 0xFF ^ buttons[1];
	 transmit_buffer[3] = axes[0][0];
	 transmit_buffer[4] = axes[0][1];
	 transmit_buffer[5] = axes[1][0];
	 transmit_buffer[6] = axes[1][1];
	 transmit_pos = 0;
	 transmit_count = 7;
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
   case 2:
	//if(receive_buffer)
	// printf("%d: %02x\n", 7 - transmit_count, receive_buffer);
	break;
  }
 }

 if(!bitpos && transmit_count)
  dsr_pulse_delay = 0x40; //0x100;

 return(ret);
}

InputDevice *Device_DualAnalog_Create(bool joystick_mode)
{
 return new InputDevice_DualAnalog(joystick_mode);
}



}
