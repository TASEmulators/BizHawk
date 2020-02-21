#ifndef f_AT_DIRECTORYWATCHER_H
#define f_AT_DIRECTORYWATCHER_H

#include <vd2/system/thread.h>
#include <vd2/system/VDString.h>
#include <vd2/system/vdstl_hashset.h>

class ATDirectoryWatcher : public VDThread {
	ATDirectoryWatcher(const ATDirectoryWatcher&);
	ATDirectoryWatcher& operator=(const ATDirectoryWatcher&);
public:
	ATDirectoryWatcher();
	~ATDirectoryWatcher();

	void Init(const wchar_t *basePath);
	void Shutdown();

	bool CheckForChanges(vdfastvector<wchar_t>& strheap);

protected:
	void ThreadRun();
	void NotifyAllChanged();

	VDStringW mBasePath;
	void *mhDir;
	void *mhExitEvent;
	void *mhDirChangeEvent;
	void *mpChangeBuffer;
	uint32 mChangeBufferSize;

	VDCriticalSection mMutex;
	typedef vdhashset<VDStringW, vdstringhashi, vdstringpredi> ChangedDirs;
	ChangedDirs mChangedDirs;
	bool mbAllChanged;
};

#endif
