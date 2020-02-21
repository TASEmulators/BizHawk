#ifndef f_VD2_KASUMI_BLT_SPANUTILS_H
#define f_VD2_KASUMI_BLT_SPANUTILS_H

#include <vd2/system/vdtypes.h>

namespace nsVDPixmapSpanUtils {
	void VDCDECL horiz_expand2x_centered	(uint8 *dst, const uint8 *src, sint32 w);
	void VDCDECL horiz_expand2x_coaligned	(uint8 *dst, const uint8 *src, sint32 w);
	void VDCDECL horiz_expand4x_coaligned	(uint8 *dst, const uint8 *src, sint32 w);
	void VDCDECL horiz_compress2x_coaligned	(uint8 *dst, const uint8 *src, sint32 w);
	void VDCDECL horiz_compress2x_centered	(uint8 *dst, const uint8 *src, sint32 w);
	void VDCDECL horiz_compress4x_coaligned	(uint8 *dst, const uint8 *src, sint32 w);
	void VDCDECL horiz_compress4x_centered	(uint8 *dst, const uint8 *src, sint32 w);
	void VDCDECL horiz_realign_to_centered	(uint8 *dst, const uint8 *src, sint32 w);
	void VDCDECL horiz_realign_to_coaligned	(uint8 *dst, const uint8 *src, sint32 w);
	void VDCDECL vert_expand2x_centered		(uint8 *dst, const uint8 *const *srcs, sint32 w, uint8 phase);
	void VDCDECL vert_expand4x_centered		(uint8 *dst, const uint8 *const *srcs, sint32 w, uint8 phase);
	void VDCDECL vert_compress2x_centered_fast	(uint8 *dst, const uint8 *const *srcarray, sint32 w, uint8 phase);
	void VDCDECL vert_compress2x_centered	(uint8 *dst, const uint8 *const *srcarray, sint32 w, uint8 phase);
	void VDCDECL vert_compress4x_centered(uint8 *dst, const uint8 *const *srcarray, sint32 w, uint8 phase);
}

#endif
