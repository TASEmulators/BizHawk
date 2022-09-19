//
// OS agnostic CDROM interface functions
//
// by James Hammons
// (C) 2010 Underground Software
//
// JLH = James Hammons <jlhamm@acm.org>
//
// Who  When        What
// ---  ----------  ------------------------------------------------------------
// JLH  01/16/2010  Created this log ;-)
//

#include "cdintf.h"

bool CDIntfInit(void)
{
	return false;
}

void CDIntfDone(void)
{
}

bool CDIntfReadBlock(uint32_t sector, uint8_t * buffer)
{
	return false;
}

uint8_t CDIntfGetSessionInfo(uint32_t session, uint32_t offset)
{
	return 0xFF;
}

uint8_t CDIntfGetTrackInfo(uint32_t track, uint32_t offset)
{
	return 0xFF;
}
