//	Altirra - Atari 800/800XL/5200 emulator
//	Core library - device parent implementation
//	Copyright (C) 2009-2014 Avery Lee
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

#include <stdafx.h>
#include <at/atcore/deviceparentimpl.h>

void ATDeviceBus::GetChildDevicePrefix(uint32 index, VDStringW& s) {
}

///////////////////////////////////////////////////////////////////////////

ATDeviceBusSingleChild::ATDeviceBusSingleChild() {
	mpOnAttach = [] {};
	mpOnDetach = [] {};
}

ATDeviceBusSingleChild::~ATDeviceBusSingleChild() {
	VDASSERT(!mpChildDevice);
}

void ATDeviceBusSingleChild::Init(IATDeviceParent *parent, uint32 busIndex, uint32 iid, const char *supportedType, const wchar_t *name, const char *tag) {
	VDASSERT(!mpChildDevice);

	mpParent = parent;
	mBusIndex = busIndex;
	mIID = iid;
	mpSupportedType = supportedType;
	mpName = name;
	mpTag = tag;
}

void ATDeviceBusSingleChild::Shutdown() {
	if (mpChildDevice) {
		mpOnDetach();

		mpChildInterface = nullptr;
		mpChildDevice->SetParent(nullptr, 0);
		mpChildDevice->Release();
		mpChildDevice = nullptr;
	}
}

void ATDeviceBusSingleChild::SetOnAttach(vdfunction<void()> fn) {
	mpOnAttach = std::move(fn);
}

void ATDeviceBusSingleChild::SetOnDetach(vdfunction<void()> fn) {
	mpOnDetach = std::move(fn);
}

const wchar_t *ATDeviceBusSingleChild::GetBusName() const {
	return mpName;
}

const char *ATDeviceBusSingleChild::GetBusTag() const {
	return mpTag;
}

const char *ATDeviceBusSingleChild::GetSupportedType(uint32 index) {
	return index == 0 ? mpSupportedType : nullptr;
}

void ATDeviceBusSingleChild::GetChildDevices(vdfastvector<IATDevice *>& devs) {
	if (mpChildDevice)
		devs.push_back(mpChildDevice);
}

void ATDeviceBusSingleChild::AddChildDevice(IATDevice *dev) {
	if (mpChildDevice)
		return;

	void *iface = dev->AsInterface(mIID);
	if (!iface)
		return;

	mpChildDevice = dev;
	mpChildInterface = iface;

	dev->AddRef();
	dev->SetParent(mpParent, mBusIndex);

	mpOnAttach();
}

void ATDeviceBusSingleChild::RemoveChildDevice(IATDevice *dev) {
	if (dev && mpChildDevice == dev) {
		mpOnDetach();

		mpChildDevice = nullptr;
		mpChildInterface = nullptr;
		dev->SetParent(nullptr, 0);
		dev->Release();
	}
}

///////////////////////////////////////////////////////////////////////////

void ATDeviceParentSingleChild::Init(uint32 iid, const char *supportedType, const wchar_t *name, const char *tag, IVDUnknown *owner) {
	mpOwner = owner;
	mBus.Init(this, 0, iid, supportedType, name, tag);
}

void ATDeviceParentSingleChild::Shutdown() {
	mBus.Shutdown();
}

void ATDeviceParentSingleChild::SetOnAttach(vdfunction<void()> fn) {
	mBus.SetOnAttach(std::move(fn));
}

void ATDeviceParentSingleChild::SetOnDetach(vdfunction<void()> fn) {
	mBus.SetOnDetach(std::move(fn));
}

void *ATDeviceParentSingleChild::AsInterface(uint32 iid) {
	return mpOwner->AsInterface(iid);
}

IATDeviceBus *ATDeviceParentSingleChild::GetDeviceBus(uint32 index) {
	return index == 0 ? &mBus : nullptr;
}
