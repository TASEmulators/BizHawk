#include <stdafx.h>
#include <at/atui/uidrawingutils.h>

void ATUIDrawBevel(IVDDisplayRenderer& rdr, const vdrect32& r, uint32 tlColor, uint32 brColor) {
	vdpoint32 pts[5] = {
		vdpoint32(r.right-1, r.top),
		vdpoint32(r.left, r.top),
		vdpoint32(r.left, r.bottom-1),
		vdpoint32(r.right-1, r.bottom-1),
		vdpoint32(r.right-1, r.top),
	};

	rdr.SetColorRGB(tlColor);
	rdr.PolyLine(pts, 2);
	rdr.SetColorRGB(brColor);
	rdr.PolyLine(pts+2, 2);
}

void ATUIDrawThin3DRect(IVDDisplayRenderer& rdr, const vdrect32& r, bool depressed) {
	ATUIDrawBevel(rdr, r, depressed ? 0x404040 : 0xFFFFFF, depressed ? 0xFFFFFF : 0x404040);
}

void ATUIDraw3DRect(IVDDisplayRenderer& rdr, const vdrect32& r, bool depressed) {
	ATUIDrawBevel(rdr, r, depressed ? 0x404040 : 0xD4D0C8, depressed ? 0xD4D0C8 : 0x404040);
	ATUIDrawBevel(rdr, vdrect32(r.left+1, r.top+1, r.right-1, r.bottom-1), depressed ? 0x404040 : 0xFFFFFF, depressed ? 0xFFFFFF : 0x404040);
}
