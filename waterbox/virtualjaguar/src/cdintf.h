//
// CDINTF.H: OS agnostic CDROM access funcions
//
// by James Hammons
//

#ifndef __CDINTF_H__
#define __CDINTF_H__

#include <stdint.h>

bool CDIntfInit(void);
void CDIntfDone(void);
bool CDIntfReadBlock(uint32_t, uint8_t *);
uint32_t CDIntfGetNumSessions(void);
void CDIntfSelectDrive(uint32_t);
uint32_t CDIntfGetCurrentDrive(void);
const uint8_t * CDIntfGetDriveName(uint32_t);
uint8_t CDIntfGetSessionInfo(uint32_t, uint32_t);
uint8_t CDIntfGetTrackInfo(uint32_t, uint32_t);

#endif	// __CDINTF_H__
