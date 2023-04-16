//
// OBJECTP.H: Object Processor header file
//

#ifndef __OBJECTP_H__
#define __OBJECTP_H__

#include <stdint.h>

void OPInit(void);
void OPReset(void);
void OPDone(void);

void OPProcessList(int scanline);

#define OPFLAG_RELEASE		8					// Bus release bit
#define OPFLAG_TRANS		4					// Transparency bit
#define OPFLAG_RMW			2					// Read-Modify-Write bit
#define OPFLAG_REFLECT		1					// Horizontal mirror bit

#endif	// __OBJECTP_H__
