#include "next_m0_time.h"
#include "ppu.h"

namespace gambatte {

void NextM0Time::predictNextM0Time(const PPU &ppu) {
	predictedNextM0Time_ = ppu.predictedNextXposTime(167);
}


void NextM0Time::SaveS(NewState *ns)
{
	NSS(predictedNextM0Time_);
}

void NextM0Time::LoadS(NewState *ns)
{
	NSL(predictedNextM0Time_);
}

}
