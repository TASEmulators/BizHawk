#ifndef f_VD2_DITA_W32ACCEL_H
#define f_VD2_DITA_W32ACCEL_H

#include <vd2/system/win32/miniwindows.h>
#include <vd2/Dita/accel.h>

void VDUIExtractAcceleratorTableW32(VDAccelTableDefinition& dst, HACCEL haccel, const VDAccelToCommandEntry *pCommands, uint32 nCommands);

HACCEL VDUIBuildAcceleratorTableW32(const VDAccelTableDefinition& def);
void VDUIUpdateMenuAcceleratorsW32(HMENU hmenu, const VDAccelTableDefinition& def);

#endif
