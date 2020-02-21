//	Altirra - Atari 800/800XL/5200 emulator
//	Core library -- cartridge port interfaces
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
// Cartridge port interface
//
// The cartridge port allows a cartridge to map one or more memory regions
// to either ROM or RAM. On the computers, this includes the left cartridge
// window ($A000-BFFF), the right cartridge window ($8000-9FFF), and the
// cartridge control region ($D500-D5FF). The left cartridge can map all
// three regions, while the right cartridge can only map CCTL and right
// windows. On the 5200, there is only one cartridge and one window of
// $4000-BFFF.
//
// Mapping the windows is done through the memory mappers, but what we
// do here is manage the signals to enable the windows. On the XL/XE line,
// the TRIG3 register in GTIA senses whether the left cartridge window is
// mapped, and so the core needs to know when a cartridge is mapping that
// window. On the real hardware, asserting the RD4/RD5 lines also disables
// the corresponding ROM or RAM mappings at that region; we don't do that
// here and simply require that any cartridge device assert the mapping
// state if and only if it is overlaying that entire window region.
// Memory layer priorities ensure that BASIC/Game ROMs and RAM are
// disabled.
//
// The other task we have here is to manage pass-throughs. It is possible
// for a cartridge to be plugged into the pass-through port of a SpartaDOS
// X cartridge, and for that to be plugged into an Ultimate1MB-equipped
// system. For this to work a cartridge has to be able to enable or disable
// its passthrough and also sense whether the downstream cartridge is trying
// to map the left window, regardless of whether it is enabled or disabled.
// Currently the CCTL region is never masked.
//
// In summary:
//	- Cartridges create and manage their own memory layers, but must enable
//	  them only when their left/right windows are enabled. This is actively
//	  notified via SetCartEnables() and passively sensed via
//	  IsLeftCartEnabled() and IsRightCartEnabled().
//
//	- A cartridge's window is enabled if the previous cart has an enabled
//	  window and has pass-through enabled. Thus, a cartridge can enable a
//	  window memory layer only when all previous cartridges have that window
//	  enabled.
//
//	- A cartridge must report whether its window is mapped regardless of
//	  whether it is enabled. Cartridges must not report the memory layer
//	  status as the mapping status.
//
//	- A cartridge senses an active window from its child if the child has
//	  the window mapped, or has pass-through enabled and is sensing an
//	  active window. This means that a cartridge disabling its pass-through
//	  does not affect its ability to see the next cart's window state, but
//	  does prevent that state from being seen by the previous cart.

#ifndef f_AT_ATCORE_DEVICECART_H
#define f_AT_ATCORE_DEVICECART_H

class IATDeviceCartridge;

enum ATCartridgePriority : uint8 {
	kATCartridgePriority_Default,
	kATCartridgePriority_PassThrough,
	kATCartridgePriority_Internal,
	kATCartridgePriority_PBI,
	kATCartridgePriority_MMU
};

class IATDeviceCartridgePort {
public:
	/// Add a cartridge to the stack with the given priority.
	///
	/// The ID is passed back by reference because it the cartridge port can call back
	/// into the cartridge to initialize cart state before AddCartridge() returns. The
	/// ID field is filled in before any callbacks occur.
	///
	virtual void AddCartridge(IATDeviceCartridge *cart, ATCartridgePriority priority, uint32& id) = 0;

	/// Remove a cartridge from the stack. The cart pointer must match the ID.
	virtual void RemoveCartridge(uint32 id, IATDeviceCartridge *cart) = 0;

	/// Returns true if the left cartridge window ($A000-BFFF) is enabled
	/// for an external cart.
	virtual bool IsLeftWindowEnabled(uint32 id) const = 0;

	/// Returns true if the right cartridge window ($A000-BFFF) is enabled
	/// for an external cart.
	virtual bool IsRightWindowEnabled(uint32 id) const = 0;

	/// Returns true if the cartridge control region ($D500-D5FF) is enabled
	/// for an external cart.
	virtual bool IsCCTLEnabled(uint32 id) const = 0;

	/// Changes the pass-through state for a cartridge. This defaults to enabled for newly
	/// added carts.
	virtual void EnablePassThrough(uint32 id, bool leftEnabled, bool rightEnabled, bool cctlEnabled) = 0;

	/// Notifies that a cartridge has changed its mapping state for the
	/// left cartridge window ($A000-BFFF). Note that this should NOT include
	/// the global left/right cart enable states.
	virtual void OnLeftWindowChanged(uint32 id, bool active) = 0;

	/// Returns true if the a lower priority cartridge has mapped the
	/// left cartridge window.
	virtual bool IsLeftWindowActiveDownstream(uint32 id) const = 0;
};

class IATDeviceCartridge {
public:
	enum : uint32 { kTypeID = 'atct' };

	virtual void InitCartridge(IATDeviceCartridgePort *cartPort) = 0;

	/// Returns true if the RD5 signal is active, which maps $A000-BFFF.
	virtual bool IsLeftCartActive() const = 0;

	/// Notifies that the inherited enable state for the left or right windows
	/// has changed. Carts must not call remap or enable methods in response.
	/// This is guaranteed to be called at least once on AddCartridge().
	virtual void SetCartEnables(bool leftEnable, bool rightEnable, bool cctlEnable) = 0;

	/// Notifies that the active state of the downstream cart's left window
	/// has changed.
	virtual void UpdateCartSense(bool leftActive) = 0;
};

#endif
