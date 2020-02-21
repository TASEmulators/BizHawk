#include <stdafx.h>
#include <vd2/system/error.h>
#include "uiqueue.h"
#include "uicommondialogs.h"

ATUIFuture::ATUIFuture()
	: mStage(0)
{
}

ATUIFuture::~ATUIFuture() {
}

ATUIStep ATUIFuture::GetStep() {
	auto p = vdmakerefptr(this);
	return ATUIStep([p]() { p->RunStep(); });
}

bool ATUIFuture::Run() {
	if (mpWait) {
		if (!mpWait->Run())
			return false;

		mpWait.clear();
	}

	try {
		RunInner();
	} catch(...) {
		MarkCompleted();
		throw;
	}

	return mStage < 0;
}

void ATUIFuture::RunInner() {
}

void ATUIFuture::RunStep() {
	if (mStage >= 0) {
		ATUIPushStep(GetStep());

		if (Run())
			mStage = -1;
	}
}

void ATUIFuture::Wait(ATUIFuture *f) {
	mpWait = f;
}

///////////////////////////////////////////////////////////////////////////

bool ATUIQueue::Run() {
	if (mSteps.empty())
		return false;

	ATUIStep step(std::move(mSteps.back()));
	mSteps.pop_back();

	try {
		step();
	} catch(const MyError& e) {
		PushStep(ATUIShowAlert(VDTextAToW(e.gets()).c_str(), L"Altirra Error")->GetStep());
	}

	return true;
}

void ATUIQueue::PushStep(const ATUIStep& step) {
	mSteps.push_back(step);
}

///////////////////////////////////////////////////////////////////////////

ATUIQueue g_ATUIQueue;

ATUIQueue& ATUIGetQueue() {
	return g_ATUIQueue;
}

void ATUIPushStep(const ATUIStep& step) {
	g_ATUIQueue.PushStep(step);
}
