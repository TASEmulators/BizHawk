//	Altirra - Atari 800/800XL/5200 emulator
//	Cartridge port manager
//	Copyright (C) 2008-2016 Avery Lee
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

#ifndef AT_CARTRIDGEPORT_H
#define AT_CARTRIDGEPORT_H

#include <vd2/system/function.h>
#include <vd2/system/vdstl.h>
#include <at/atcore/devicecart.h>

class ATCartridgePort final : public IATDeviceCartridgePort {
	ATCartridgePort(const ATCartridgePort&) = delete;
	ATCartridgePort& operator=(const ATCartridgePort&) = delete;

public:
	ATCartridgePort();
	~ATCartridgePort();

	bool IsLeftMapped() const { return mCarts[0].mbLeftMapSense; }

	void SetLeftMapChangedHandler(const vdfunction<void(bool)>& fn);

	void EnableCarts(bool leftEnable, bool rightEnable, bool cctlEnable);

	void AddCartridge(IATDeviceCartridge *cart, ATCartridgePriority priority, uint32& id) override;
	void RemoveCartridge(uint32 id, IATDeviceCartridge *cart) override;
	bool IsLeftWindowEnabled(uint32 id) const override;
	bool IsRightWindowEnabled(uint32 id) const override;
	bool IsCCTLEnabled(uint32 id) const override;
	void EnablePassThrough(uint32 id, bool leftEnabled, bool rightEnabled, bool cctlEnabled) override;
	void OnLeftWindowChanged(uint32 id, bool enabled) override;
	bool IsLeftWindowActiveDownstream(uint32 id) const override;

private:
	void NotifyLeftMapChanged();
	void EnableCartPassThroughInternal(uint32 id, bool leftEnabled, bool rightEnabled, bool cctlEnabled);
	void UpdateUpstreamSense(uint32 id);

	struct CartInfo {
		uint32 mCartNext;	// next (downstream) cartridge
		uint32 mCartPrev;	// prev (upstream) cartridge

		IATDeviceCartridge *mpCart;
		ATCartridgePriority mPriority;

		// True if this cartridge is mapping the left window.
		bool mbLeftMapped;

		// True if a downstream cartridge wants to map the window.
		bool mbLeftMapSense;

		// True if this cartridge is passing through the left window enable to downstream carts.
		bool mbLeftMapPassThrough;

		// True if this cartridge is passing through the right window enable to downstream carts.
		bool mbRightMapPassThrough;

		// True if this cartridge is passing through the cartridge control enable to downstream carts.
		bool mbCCTLPassThrough;

		// True if all upstream cartridges have left window pass-through enabled.
		bool mbLeftMapEnabled;

		// True if all upstream cartridges have right window pass-through enabled.
		bool mbRightMapEnabled;

		// True if all upstream cartridges have cartridge control pass-through enabled.
		bool mbCCTLEnabled;
	};

	// Array of unsorted carts. [0] is reserved for the root.
	vdfastvector<CartInfo> mCarts;

	vdfunction<void(bool)> mpLeftMapChangedHandler;
};

#endif
