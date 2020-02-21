#include <stdafx.h>
#include <vd2/system/error.h>
#include <vd2/system/filesys.h>
#include <windows.h>
#include "directorywatcher.h"

ATDirectoryWatcher::ATDirectoryWatcher()
	: mhDir(INVALID_HANDLE_VALUE)
	, mhExitEvent(NULL)
	, mhDirChangeEvent(NULL)
	, mpChangeBuffer(NULL)
	, mChangeBufferSize(0)
	, mbAllChanged(false)
{
}

ATDirectoryWatcher::~ATDirectoryWatcher() {
	Shutdown();
}

void ATDirectoryWatcher::Init(const wchar_t *basePath) {
	Shutdown();

	mBasePath = VDGetLongPath(basePath);

	try {
		mChangeBufferSize = 32768;
		mpChangeBuffer = new char[mChangeBufferSize];

		mhDir = CreateFileW(basePath, FILE_LIST_DIRECTORY, FILE_SHARE_READ | FILE_SHARE_WRITE | FILE_SHARE_DELETE, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL | FILE_FLAG_OVERLAPPED | FILE_FLAG_BACKUP_SEMANTICS, NULL);
		if (mhDir == INVALID_HANDLE_VALUE)
			throw (DWORD)GetLastError();

		mhExitEvent = CreateEventW(NULL, FALSE, FALSE, NULL);
		if (!mhExitEvent)
			throw (DWORD)GetLastError();

		mhDirChangeEvent = CreateEventW(NULL, TRUE, FALSE, NULL);
		if (!mhDirChangeEvent)
			throw (DWORD)GetLastError();

		ThreadStart();
	} catch(DWORD err) {
		Shutdown();

		throw MyWin32Error("Unable to watch directory \"%ls\": %%s.", err, basePath);
	}
}

void ATDirectoryWatcher::Shutdown() {
	if (isThreadAttached()) {
		SetEvent((HANDLE)mhExitEvent);
		ThreadWait();
	}

	if (mhExitEvent) {
		CloseHandle((HANDLE)mhExitEvent);
		mhExitEvent = NULL;
	}

	if (mhDirChangeEvent) {
		CloseHandle((HANDLE)mhDirChangeEvent);
		mhDirChangeEvent = NULL;
	}

	if (mpChangeBuffer) {
		delete[] mpChangeBuffer;
		mpChangeBuffer = NULL;
	}
}

bool ATDirectoryWatcher::CheckForChanges(vdfastvector<wchar_t>& strheap) {
	bool allChanged = false;
	strheap.clear();

	vdsynchronized(mMutex) {
		allChanged = mbAllChanged;

		if (allChanged) {
			mbAllChanged = false;
		} else {
			for(ChangedDirs::const_iterator it(mChangedDirs.begin()), itEnd(mChangedDirs.end());
				it != itEnd;
				++it)
			{
				const VDStringW& s = *it;
				const wchar_t *t = s.c_str();

				strheap.insert(strheap.end(), t, t + s.size() + 1);
			}
		}

		mChangedDirs.clear();
	}

	return allChanged;
}

void ATDirectoryWatcher::ThreadRun() {
	OVERLAPPED ov;
	HANDLE h[2] = { (HANDLE)mhExitEvent, (HANDLE)mhDirChangeEvent };
	const DWORD dwNotifyFilter
		= FILE_NOTIFY_CHANGE_ATTRIBUTES
		| FILE_NOTIFY_CHANGE_CREATION
		| FILE_NOTIFY_CHANGE_DIR_NAME
		| FILE_NOTIFY_CHANGE_FILE_NAME
		| FILE_NOTIFY_CHANGE_LAST_WRITE
		| FILE_NOTIFY_CHANGE_SIZE;

	VDStringW relPath;
	vdfastvector<WCHAR> longPath;

	for(;;) {
		DWORD dummyActual;
		memset(&ov, 0, sizeof ov);
		ov.hEvent = (HANDLE)mhDirChangeEvent;

		ResetEvent(ov.hEvent);

		BOOL rdcResult = ReadDirectoryChangesW((HANDLE)mhDir, mpChangeBuffer, mChangeBufferSize, TRUE, dwNotifyFilter, &dummyActual, &ov, NULL);
		DWORD waitResult = WaitForMultipleObjects(rdcResult != 0 ? 2 : 1, h, FALSE, INFINITE);

		if (waitResult != WAIT_OBJECT_0 + 1) {
			// Timeout is impossible, so this must be error or wait signaled for the exit
			// flag. Either way, time to go.
			break;
		}

		// Retrieve the overlapped I/O result.
		DWORD actual = 0;
		if (!GetOverlappedResult((HANDLE)mhDir, &ov, &actual, FALSE))
			continue;

		// Zero bytes returned means we ran out of buffer, in which case we should just tag all
		// directories as changed.
		if (actual == 0) {
			NotifyAllChanged();
			continue;
		}

		// Process notification data structures.
		const FILE_NOTIFY_INFORMATION *pfni = (const FILE_NOTIFY_INFORMATION *)mpChangeBuffer;

		for(;;) {
			// We may get either the short name or long name here, so we need to convert
			// the relative path to a full path and then from there go to a long path.
			relPath.assign(pfni->FileName, pfni->FileName + (pfni->FileNameLength / sizeof(WCHAR)));

			// convert to full path
			const VDStringW& absPath = VDMakePath(mBasePath.c_str(), VDFileSplitPathLeft(relPath).c_str());
			const VDStringW& absLongPath = VDGetLongPath(absPath.c_str());
			const VDStringW& relPath = VDFileGetRelativePath(mBasePath.c_str(), absLongPath.c_str(), false);

			if (!relPath.empty()) {
				VDDEBUG("Change detected: %ls\n", relPath.c_str());

				vdsynchronized(mMutex) {
					// VDFileGetRelativePath() will give us the dot dir for the root... we don't want that.
					if (relPath == L".")
						mChangedDirs.insert(VDStringW());
					else
						mChangedDirs.insert(relPath);
				}
			}

			if (!pfni->NextEntryOffset)
				break;

			pfni = (const FILE_NOTIFY_INFORMATION *)((const char *)pfni + pfni->NextEntryOffset);
		}
	}

	// Cancel any outstanding watch on the handle (must be done in this thread).
	CancelIo((HANDLE)mhDir);
}

void ATDirectoryWatcher::NotifyAllChanged() {
	vdsynchronized(mMutex) {
		mbAllChanged = true;
	}
}
