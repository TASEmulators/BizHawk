//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2017-2018 Avery Lee
//
//	This program is free software; you can redistribute it and/or modify
//	it under the terms of the GNU General Public License as published by
//	the Free Software Foundation; either version 2 of the License, or
//	(at your option) any later version.
//
//	This program is distributed in the hope that it will be useful,
//	but WITHOUT ANY WARRANTY; without even the implied warranty of
//	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//	GNU General Public License for more details.
//
//	You should have received a copy of the GNU General Public License
//	along with this program; if not, write to the Free Software
//	Foundation, Inc., 675 Mass Ave, Cambridge, MA 02139, USA.

#ifndef f_AT_DISKINTERFACE_H
#define f_AT_DISKINTERFACE_H

#include <vd2/system/file.h>
#include <vd2/system/time.h>
#include <vd2/system/vdtypes.h>
#include <at/atcore/media.h>
#include <at/atcore/notifylist.h>
#include <at/atio/diskimage.h>

class IATUIRenderer;
class IATDiskImage;

class IATDiskInterfaceClient {
public:
	virtual void OnDiskChanged(bool mediaRemoved) = 0;
	virtual void OnWriteModeChanged() = 0;
	virtual void OnTimingModeChanged() = 0;
	virtual void OnAudioModeChanged() = 0;
};

// ATDiskInterface
//
// A disk interface object represents a numbered disk drive slot, i.e. D1:.
// It allows a common interface between disk drive emulation and front-end
// components that control media and settings associated with each slot.
//
class ATDiskInterface {
	ATDiskInterface(const ATDiskInterface&) = delete;
	ATDiskInterface& operator=(const ATDiskInterface&) = delete;
public:
	ATDiskInterface();
	~ATDiskInterface();

	void SwapSettings(ATDiskInterface& other);

	void Init(uint32 index, IATUIRenderer *uirenderer);
	void Shutdown();

	bool IsAccurateSectorTimingEnabled() const { return mbAccurateSectorTiming; }
	void SetAccurateSectorTimingEnabled(bool enabled);

	bool AreDriveSoundsEnabled() const { return mbDriveSoundsEnabled; }
	void SetDriveSoundsEnabled(bool enabled);

	void SetShowSectorCounter(bool enabled) { mbShowSectorCounter = enabled; }

	void SetSectorBreakpoint(int sector) { mSectorBreakpoint = sector; }
	int GetSectorBreakpoint() const { return mSectorBreakpoint; }

	bool IsDirty() const;
	bool IsDiskLoaded() const { return mpDiskImage != nullptr; }
	bool IsDiskBacked() const { return mbHasPersistentSource; }

	// Returns true if a writable disk image is mounted and writing is enabled on the
	// interface.
	bool IsDiskWritable() const;

	// Returns true if a writable disk image is mounted and formatting is enabled on
	// the interface.
	bool IsFormatAllowed() const;

	// If the disk is not writable, try to enable it for virtual read/write. This is
	// used when a drive forces writes onto a read only disk via a WP override switch
	// (Happy 1050). Since we don't want to allow software to force writes to R/O media,
	// we use VRW mode as a compromise. This can still fail if the underlying image is
	// not R/W capable.
	bool TryEnableWrite();

	const wchar_t *GetPath() const { return mPath.empty() ? NULL : mPath.c_str(); }
	IATDiskImage *GetDiskImage() const { return mpDiskImage; }

	ATMediaWriteMode GetWriteMode() const { return mWriteMode; }
	void SetWriteMode(ATMediaWriteMode mode);

	// If disk is modified and auto-flush is enabled, flush modifications to disk.
	// This will do nothing if auto-flush is disabled. Explicit Flush() is not
	// normally needed, as the disk interface will automatically do lazy flushes
	// whenever the disk is modified.
	void Flush();

	// Reload the existing bound disk, discarding any changes. Returns false if the
	// disk image cannot be reloaded (has no suitable backing store). Silently
	// succeeds if the disk is not dirty.
	bool CanRevert() const;
	bool RevertDisk();

	void MountFolder(const wchar_t *path, bool sdfs);
	void LoadDisk(const wchar_t *s);
	void LoadDisk(const wchar_t *origPath, const wchar_t *imagePath, IATDiskImage *image);

	// Save any changes back to the persistent store.
	void SaveDisk();

	// Save the disk to a new location, regardless of the 
	void SaveDiskAs(const wchar_t *s, ATDiskImageFormat format);

	void CreateDisk(const ATDiskGeometryInfo& geom);
	void CreateDisk(uint32 sectorCount, uint32 bootSectorCount, uint32 sectorSize);
	void FormatDisk(const ATDiskGeometryInfo& geom);
	void FormatDisk(uint32 sectorCount, uint32 bootSectorCount, uint32 sectorSize);
	void UnloadDisk();

	// Signals that a change has been made to the disk image that may require
	// a flush; queues a lazy flush if enabled. The lazy flush occurs about
	// two real-time seconds after the last write or whenever the disk is
	// unmounted, whichever occurs first.
	void OnDiskModified();

	// Signals that the disk itself has been changed in-place -- still the same image, but
	// possibly changed in geometry. Implies a bigger change than OnDiskModified().
	// MediaRemoved=true indicates that the disk was physically removed and/or reinserted;
	// false indicates an in-place change (format).
	void OnDiskChanged(bool mediaRemoved);

	// Signals that an error has occurred at host level when accessing the disk image.
	void OnDiskError();

	VDStringW GetMountedImageLabel() const;

	uint32 GetSectorSize(uint16 sector) const;
	uint32 GetSectorPhantomCount(uint16 sector) const;

	struct SectorInfo {
		float mRotPos;
		uint8 mFDCStatus;
	};

	bool GetSectorInfo(uint16 sector, int phantomIdx, SectorInfo& info) const;

	void AddStateChangeHandler(const vdfunction<void()> *fn);
	void RemoveStateChangeHandler(const vdfunction<void()> *fn);

public:
	size_t GetClientCount() const;
	void AddClient(IATDiskInterfaceClient *client);
	void RemoveClient(IATDiskInterfaceClient *client);

	void CheckSectorBreakpoint(uint32 sector);

	void SetShowMotorActive(bool active);
	void SetShowActivity(bool active, uint32 sector);
	void SetShowLEDReadout(sint32 ledDisplay = -1);

private:
	void Flush(bool ignoreAutoFlush, bool rethrowErrors);
	bool CanFlush(bool ignoreAutoFlush) const;
	void OnFlushTimerFire();
	void SetFlushError(bool);

	void CheckForModifiedChange();

	void NotifyStateSuspend();
	void NotifyStateResume(bool notify);
	void NotifyStateChange();

	uint32 mIndex;
	IATUIRenderer *mpUIRenderer;

	bool mbDriveSoundsEnabled = false;
	bool mbAccurateSectorTiming = false;
	bool mbShowSectorCounter = false;
	bool mbHasPersistentSource = false;

	bool mbModified = false;

	sint32 mSectorBreakpoint = -1;
	ATMediaWriteMode mWriteMode = {};
	VDStringW mPath;
	vdrefptr<IATDiskImage> mpDiskImage;

	vdfastfixedvector<IATDiskInterfaceClient *, 1> mClients;

	ATNotifyList<const vdfunction<void()> *> mStateChangeHandlers;
	uint32 mStateChangeSuspendCount = 0;

	uint32 mFlushTimeStart = 0;
	VDLazyTimer mFlushTimer;
};

#endif	// f_AT_DISKINTERFACE_H
