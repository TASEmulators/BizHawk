//	Altirra - Atari 800/800XL/5200 emulator
//	Core library -- PBI device interfaces
//	Copyright (C) 2009-2016 Avery Lee
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

//=========================================================================
// Parallel Bus Interface (PBI) device interface
//
// The Parallel Bus Interface connection allows the emulator to efficiently
// multiplex the PBI regions without requiring each device to map the
// select register at PDVs [$D1FF]. On real hardware, each device has to
// decode this address to implement its device select bit. Having up to 8
// devices each inserting a memory mapping on the $D1xx page would suck,
// so we have a central manager do it instead. This also makes it easier
// to monitor which PBI device is active.
//
// Each PBI device is responsible for hooking itself into the PBI manager
// and responding to select/deselect requests. When selected, a PBI device
// should map any device firmware into the math pack region ($D800-DFFF).
// Each device must supply its own math pack layer, but this is not a issue
// because only one layer is active at a time and the memory manager is
// optimized to skip inactive layers. Also, devices tend to have different
// requirements as to how the MP region is handled with regard to banking
// or even whether part of the address range is used for RAM.
//
// The PBI manager does not implement multiple devices being selected at
// the same time (which may result in garbled data and devices getting warm
// on the real hardware), nor does it multiplex the interrupt status register
// at PDVI [$D1FF].
//
// Devices do not have to implement PBI through this interface; they may
// directly handle $D1FF instead. This may be a useful alternative for
// devices that aren't actually PBI compliant.

#ifndef f_AT_ATCORE_DEVICEPBI_H
#define f_AT_ATCORE_DEVICEPBI_H

struct ATPBIDeviceInfo {
	// Device ID bit in PDVS, i.e. $01, $02, etc.
	uint8	mDeviceId;

	// True if the device has an IRQ selection bit. This avoids mapping
	// $D1FF if it is not needed (which can conflict with non-PBI devices).
	bool	mbHasIrq;
};

class IATPBIDevice {
public:
	virtual void GetPBIDeviceInfo(ATPBIDeviceInfo& devInfo) const = 0;
	virtual void SelectPBIDevice(bool enable) = 0;

	/// Returns true if the device is currently overlaying the math pack.
	virtual bool IsPBIOverlayActive() const = 0;

	/// Returns status bits in $D1FF. busData is incoming bus data to be
	/// modified. If the device has IRQ status, the device's ID bit should
	/// be modified in busData and the result returned. Otherwise, busData
	/// should be returned unmodified. debugOnly=true indicates that side
	/// effects of the read cycle should be suppressed (can be ignored by
	/// most devices).
	virtual uint8 ReadPBIStatus(uint8 busData, bool debugOnly) = 0;
};

class IATDevicePBIManager {
public:
	virtual void AddDevice(IATPBIDevice *dev) = 0;
	virtual void RemoveDevice(IATPBIDevice *dev) = 0;
	virtual void DeselectSelf(IATPBIDevice *dev) = 0;
};

class IATDevicePBIConnection {
public:
	enum : uint32 { kTypeID = 'atpc' };

	virtual void InitPBI(IATDevicePBIManager *pbiman) = 0;
};

#endif
