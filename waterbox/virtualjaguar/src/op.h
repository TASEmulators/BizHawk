//
// OBJECTP.H: Object Processor header file
//

#ifndef __OBJECTP_H__
#define __OBJECTP_H__

#include <stdint.h>

void OPInit(void);
void OPReset(void);
void OPDone(void);

uint64_t OPLoadPhrase(uint32_t offset);

void OPProcessList(int scanline, bool render);
uint32_t OPGetListPointer(void);
void OPSetStatusRegister(uint32_t data);
uint32_t OPGetStatusRegister(void);
void OPSetCurrentObject(uint64_t object);

#define OPFLAG_RELEASE		8					// Bus release bit
#define OPFLAG_TRANS		4					// Transparency bit
#define OPFLAG_RMW			2					// Read-Modify-Write bit
#define OPFLAG_REFLECT		1					// Horizontal mirror bit

// Exported variables

extern uint8_t objectp_running;

#endif	// __OBJECTP_H__
