#ifndef _PSXCORE_H_
#define _PSXCORE_H_

#include <stdio.h>

class PsxCore
{
public:
	PsxCore(void* _opaque)
		: opaque(_opaque)
	{
	}

private:
	void* opaque;

public:
	void* Construct(void* ManagedOpaque)
	{
		return new PsxCore(ManagedOpaque);
	}

	struct Size
	{
		int width,height;
	};

	Size GetResolution();
	void FrameAdvance();
	void UpdateVideoBuffer(void* target);
};

#endif //_PSXCORE_H_
