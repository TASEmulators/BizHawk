#ifndef f_AT_UIDRAWINGUTILS_H
#define f_AT_UIDRAWINGUTILS_H

#include <vd2/system/vectors.h>
#include <vd2/VDDisplay/renderer.h>

void ATUIDrawBevel(IVDDisplayRenderer& rdr, const vdrect32& r, uint32 tlColor, uint32 brColor);
void ATUIDrawThin3DRect(IVDDisplayRenderer& rdr, const vdrect32& r, bool depressed);
void ATUIDraw3DRect(IVDDisplayRenderer& rdr, const vdrect32& r, bool depressed);

#endif
