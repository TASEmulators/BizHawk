#ifndef f_VD2_TESSA_FORMAT_H
#define f_VD2_TESSA_FORMAT_H

#include <vd2/Tessa/Types.h>

uint32 VDTGetBytesPerBlockRow(VDTFormat format, uint32 w);
uint32 VDTGetNumBlockRows(VDTFormat format, uint32 h);

#endif	// f_VD2_TESSA_FORMAT_H
