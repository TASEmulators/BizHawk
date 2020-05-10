#ifndef NEXT_M0_TIME_H_
#define NEXT_M0_TIME_H_

#include "newstate.h"

namespace gambatte {

class NextM0Time {
public:
	NextM0Time() : predictedNextM0Time_(0) {}
	void predictNextM0Time(class PPU const &v);
	void invalidatePredictedNextM0Time() { predictedNextM0Time_ = 0; }
	unsigned long predictedNextM0Time() const { return predictedNextM0Time_; }

private:
	unsigned long predictedNextM0Time_;

public:
	template<bool isReader>void SyncState(NewState *ns);
};

}

#endif
