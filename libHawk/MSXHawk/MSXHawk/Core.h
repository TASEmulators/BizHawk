#pragma once

#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>

#include "Z80A.h"
#include "PSG.h"
#include "VDP.h"

namespace MSXHawk
{
	class MSXCore
	{
	public:
		MSXCore() 
		{

		};
				
		VDP vdp;
		Z80A cpu;
		SN76489sms psg;
	};
}

