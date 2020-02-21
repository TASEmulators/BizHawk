#ifndef f_AT_UIQUEUE_H
#define f_AT_UIQUEUE_H

#include <vd2/system/function.h>
#include <vd2/system/refcount.h>
#include <vd2/system/vdstl.h>

typedef vdfunction<void()> ATUIStep;

class ATUIFuture : public vdrefcount {
public:
	ATUIFuture();
	virtual ~ATUIFuture();

	ATUIStep GetStep();

	bool Run();
	virtual void RunInner();

protected:
	void MarkCompleted() { mStage = -1; }
	void Wait(ATUIFuture *f);

	sint32 mStage;

private:
	virtual void RunStep();

	vdrefptr<ATUIFuture> mpWait;
};

template<class T>
class ATUIFutureWithResult : public ATUIFuture {
public:
	ATUIFutureWithResult() {}

	explicit ATUIFutureWithResult(const T& immediateResult) {
		mResult = immediateResult;
		MarkCompleted();
	}

	const T& GetResult() const { return mResult; }

protected:
	using ATUIFuture::MarkCompleted;
	void MarkCompleted(const T& result) {
		mResult = result;
		ATUIFuture::MarkCompleted();
	}

	T mResult;
};

class ATUIQueue {
public:
	bool Run();

	void PushStep(const ATUIStep& step);

protected:
	typedef vdvector<ATUIStep> Steps;
	Steps mSteps;
};

ATUIQueue& ATUIGetQueue();
void ATUIPushStep(const ATUIStep& step);

#endif
