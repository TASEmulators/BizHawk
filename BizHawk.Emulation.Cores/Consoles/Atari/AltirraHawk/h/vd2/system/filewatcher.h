#ifndef f_VD2_SYSTEM_FILEWATCHER_H
#define f_VD2_SYSTEM_FILEWATCHER_H

#ifdef _MSC_VER
	#pragma once
#endif

#include <vd2/system/vdtypes.h>
#include <vd2/system/VDString.h>

class VDFunctionThunkInfo;

class IVDFileWatcherCallback {
public:
	virtual bool OnFileUpdated(const wchar_t *path) = 0;
};

class VDFileWatcher {
public:
	VDFileWatcher();
	~VDFileWatcher();

	bool IsActive() const;

	void Init(const wchar_t *file, IVDFileWatcherCallback *cb);
	void InitDir(const wchar_t *path, bool subdirs, IVDFileWatcherCallback *cb);
	void Shutdown();

	bool Wait(uint32 delay = 0xFFFFFFFFU);

protected:
#if VD_PTR_SIZE > 4
	void StaticTimerCallback(void *, unsigned, unsigned __int64, unsigned long);
#else
	void StaticTimerCallback(void *, unsigned, unsigned, unsigned long);
#endif

	void *mChangeHandle;
	uint64 mLastWriteTime;
	bool mbWatchDir;
	VDStringW mPath;

	IVDFileWatcherCallback *mpCB;

	bool mbRepeatRequested;
	bool mbThunksInited;
	VDFunctionThunkInfo *mpThunk;
	uint32 mTimerId;
};

#endif
