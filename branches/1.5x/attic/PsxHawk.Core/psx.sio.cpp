/*
this file contains the sio subsystem emulation.
*/

#include "psx.h"
#include <assert.h>
#include <stdio.h>
#include <stddef.h>


static u8 getPrescaler(const u32 prescaler_type)
{
	static const u8 prescalers[] = {0,1,16,64}; 
	return prescalers[prescaler_type];
}
		

//information in this file is derived from observatoins in mednafen and mame

void PSX::SioController::Reset()
{
	status.TX_EMPTY = 1;
	status.TX_RDY = 1;
	status.RX_RDY = 0;
	status.OVERRUN = 0;
	status.IRQ = 0;
}

float PSX::SioController::CalculateBaud()
{
	return (float)PSX_CLOCK / this->baud_reg / getPrescaler(mode.prescaler_type);
}

void PSX::sio_wr(const int size, const u32 addr, const u32 val)
{
	assert(size==2);
	assert((addr&1)==0);
	DEBUG_SIO("sio write size %d addr %08X = %08X\n",size,addr,val);
	const u32 portnum = (addr>>4)&1;
	SioController &port = sio[portnum];
	
	switch(addr&0xF)
	{
	case 0x00:
		break;
	case 0x08:
		port.mode.value = val;
		printf("baud set to approx %f\n",port.CalculateBaud());
		break;
	case 0x0A:
		{
			SioController::ControlReg reg;
			reg.value = val;
			if(reg.RESET)
			{
				DEBUG_SIO("reset port %d\n",portnum);
				port.Reset();
			}
			if(reg.IACK)
			{
				port.status.IRQ = 0;
				port.control.IACK = 0;
				//todo - irq sync
			}

			//check for rising edge of DTR signal and alert the appropriate port
			if(port.control.DTR == 0 && reg.DTR == 1)
			{
				sio_dtr(port.control.PORT_SEL);
			}

			//replicate stored bits
			port.control.TX_IENA = reg.RX_IENA;
			port.control.RX_IENA = reg.RX_IENA;
			port.control.DSR_IENA = reg.DSR_IENA;
			port.control.DTR = reg.DTR;
			port.control.PORT_SEL = reg.PORT_SEL;
		}
		break;
	case 0x0E:
		port.baud_reg = val;
		printf("baud set to approx %f\n",port.CalculateBaud());
		break;
	default:
		DEBUG_SIO("UNHANDLED\n");
	}
}

void PSX::sio_dtr(const u32 port)
{
}

u32 PSX::sio_rd(const int size, const u32 addr)
{
	assert(size==2);
	assert((addr&1)==0);
	u32 ret = 0;
	DEBUG_SIO("sio read size %d addr %08X = %08X\n",size,addr,ret);
	const u32 port = (addr>>4)&1;

	return 0;
}
