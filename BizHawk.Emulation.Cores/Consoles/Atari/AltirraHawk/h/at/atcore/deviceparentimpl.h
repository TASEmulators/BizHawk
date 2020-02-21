//	Altirra - Atari 800/800XL/5200 emulator
//	Core library - device parent implementation
//	Copyright (C) 2009-2017 Avery Lee
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

#ifndef f_AT_ATCORE_DEVICEPARENTIMPL_H
#define f_AT_ATCORE_DEVICEPARENTIMPL_H

#include <vd2/system/refcount.h>
#include <vd2/system/function.h>
#include <vd2/system/VDString.h>
#include <at/atcore/device.h>
#include <at/atcore/deviceparent.h>

class ATDeviceBus : public IATDeviceBus {
public:
	virtual void GetChildDevicePrefix(uint32 index, VDStringW& s) override;
};

class ATDeviceBusSingleChild : public ATDeviceBus {
public:
	ATDeviceBusSingleChild();
	~ATDeviceBusSingleChild();

	void Init(IATDeviceParent *parent, uint32 busIndex, uint32 iid, const char *supportedType, const wchar_t *name, const char *tag);
	void Shutdown();

	void SetOnAttach(vdfunction<void()> fn);
	void SetOnDetach(vdfunction<void()> fn);
	
	template<class T>
	T *GetChild() const {
		VDASSERT(mIID == T::kTypeID);
		return (T *)mpChildInterface;
	}

public:
	const wchar_t *GetBusName() const override;
	const char *GetBusTag() const override;
	const char *GetSupportedType(uint32 index) override;
	void GetChildDevices(vdfastvector<IATDevice *>& devs) override;
	void AddChildDevice(IATDevice *dev) override;
	void RemoveChildDevice(IATDevice *dev) override;

private:
	IATDevice *mpChildDevice = nullptr;
	void *mpChildInterface = nullptr;
	const char *mpSupportedType = nullptr;
	uint32 mIID = 0;
	IATDeviceParent *mpParent = nullptr;
	uint32 mBusIndex = 0;
	const wchar_t *mpName = nullptr;
	const char *mpTag = nullptr;
	vdfunction<void()> mpOnAttach;
	vdfunction<void()> mpOnDetach;
};


class ATDeviceParentSingleChild : public IATDeviceParent {
public:
	void Init(uint32 iid, const char *supportedType, const wchar_t *name, const char *tag, IVDUnknown *owner);
	void Shutdown();

	void SetOnAttach(vdfunction<void()> fn);
	void SetOnDetach(vdfunction<void()> fn);

	template<class T>
	T *GetChild() const {
		return mBus.GetChild<T>();
	}

public:
	void *AsInterface(uint32 iid) override;
	IATDeviceBus *GetDeviceBus(uint32 index) override;

private:
	ATDeviceBusSingleChild mBus;
	IVDUnknown *mpOwner;
};

#endif
