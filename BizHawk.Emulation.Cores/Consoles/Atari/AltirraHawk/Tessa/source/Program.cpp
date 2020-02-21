#include <stdafx.h>
#include <vd2/system/binary.h>
#include "Program.h"

bool VDTExtractMultiTargetProgram(VDTData srcdata, const uint32 *targets, VDTData& program) {
	const uint8 *data8 = (const uint8 *)srcdata.mpData;

	for(;;) {
		uint32 target_id = VDReadUnalignedU32(data8);

		if (!target_id)
			break;

		for(const uint32 *p = targets; *p; ++p) {
			if (*p == target_id) {
				program.mpData = (const uint8 *)srcdata.mpData + VDReadUnalignedU32(data8 + 4);
				program.mLength = VDReadUnalignedU32(data8 + 8);
				return true;
			}
		}

		data8 += 12;
	}

	return false;
}
