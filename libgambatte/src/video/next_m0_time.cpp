#include "next_m0_time.h"
#include "ppu.h"

void gambatte::NextM0Time::predictNextM0Time(PPU const &ppu) {
	predictedNextM0Time_ = ppu.predictedNextXposTime(lcd_hres + 7);
}

SYNCFUNC(gambatte::NextM0Time)
{
	NSS(predictedNextM0Time_);
}
