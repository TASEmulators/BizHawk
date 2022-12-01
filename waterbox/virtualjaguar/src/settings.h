//
// settings.h: Header file
//

#ifndef __SETTINGS_H__
#define __SETTINGS_H__

#include <stdint.h>

// Settings struct

struct VJSettings
{
	bool hardwareTypeNTSC;
	bool useJaguarBIOS;
	bool hardwareTypeAlpine;
	bool useFastBlitter;
};

// Exported variables

extern VJSettings vjs;

#endif	// __SETTINGS_H__
