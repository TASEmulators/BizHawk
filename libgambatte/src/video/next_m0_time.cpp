#include "next_m0_time.h"
#include "ppu.h"

namespace gambatte {

void NextM0Time::predictNextM0Time(const PPU &ppu) {
	predictedNextM0Time_ = ppu.predictedNextXposTime(167);
}

SYNCFUNC(NextM0Time)
{
	NSS(predictedNextM0Time_);
}

}
