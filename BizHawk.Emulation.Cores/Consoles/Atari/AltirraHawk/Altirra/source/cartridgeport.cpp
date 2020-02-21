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

#include <stdafx.h>
#include "cartridgeport.h"

#include <at/atcore/devicecart.h>

ATCartridgePort::ATCartridgePort()
	: mpLeftMapChangedHandler([](bool) {})
{
	mCarts.reserve(4);
	mCarts.push_back();
	auto& root = mCarts[0];
	root.mCartNext = 0;
	root.mCartPrev = 0;
	root.mpCart = nullptr;
	root.mbLeftMapped = false;
	root.mbLeftMapSense = false;
	root.mbLeftMapPassThrough = true;
	root.mbRightMapPassThrough = true;
	root.mbCCTLPassThrough = true;
	root.mbLeftMapEnabled = true;
	root.mbRightMapEnabled = true;
	root.mbCCTLEnabled = true;
}

ATCartridgePort::~ATCartridgePort() {
	VDASSERT(mCarts[0].mCartNext == 0);
}

void ATCartridgePort::SetLeftMapChangedHandler(const vdfunction<void(bool)>& fn) {
	if (!fn)
		mpLeftMapChangedHandler = [](bool) {};
	else
		mpLeftMapChangedHandler = fn;
}

void ATCartridgePort::EnableCarts(bool leftEnabled, bool rightEnabled, bool cctlEnabled) {
	EnableCartPassThroughInternal(0, leftEnabled, rightEnabled, cctlEnabled);
}

void ATCartridgePort::AddCartridge(IATDeviceCartridge *cart, ATCartridgePriority priority, uint32& id) {
	VDASSERT(std::find_if(mCarts.begin() + 1, mCarts.end(), [cart](const CartInfo& info) { return info.mpCart == cart; }) == mCarts.end());

	auto it = std::find_if(mCarts.begin() + 1, mCarts.end(), [](const CartInfo& info) { return info.mpCart == nullptr; });

	if (it == mCarts.end()) {
		mCarts.push_back();
		it = mCarts.end();
		--it;
	}

	CartInfo& info = *it;
	info.mpCart = cart;
	info.mPriority = priority;
	info.mbLeftMapped = false;
	info.mbLeftMapSense = false;
	info.mbLeftMapPassThrough = true;
	info.mbRightMapPassThrough = true;
	info.mbCCTLPassThrough = true;

	id = (uint32)(it - mCarts.begin());

	// find out where we can link this
	uint32 insertAfterIdx = mCarts[0].mCartPrev;
	for(; insertAfterIdx; insertAfterIdx = mCarts[insertAfterIdx].mCartPrev) {
		if (mCarts[insertAfterIdx].mPriority >= priority)
			break;
	}

	// link in cart
	CartInfo& infoBefore = mCarts[insertAfterIdx];
	const uint32 insertBeforeIdx = infoBefore.mCartNext;
	CartInfo& infoAfter = mCarts[insertBeforeIdx];
	VDASSERT(infoAfter.mCartPrev == insertAfterIdx);

	infoBefore.mCartNext = id;
	infoAfter.mCartPrev = id;
	info.mCartPrev = insertAfterIdx;
	info.mCartNext = insertBeforeIdx;

	// copy upstream sense state into this cartridge state, since local enables
	// default to enabled
	info.mbLeftMapSense = infoBefore.mbLeftMapSense;

	// recompute inherited enable state (since infoAfter may be the root again)
	info.mbLeftMapEnabled = infoBefore.mbLeftMapEnabled && infoBefore.mbLeftMapPassThrough;
	info.mbRightMapEnabled = infoBefore.mbRightMapEnabled && infoBefore.mbRightMapPassThrough;
	info.mbCCTLEnabled = infoBefore.mbCCTLEnabled && infoBefore.mbCCTLPassThrough;

	// notify cart of current enable state
	cart->SetCartEnables(info.mbLeftMapEnabled, info.mbRightMapEnabled, info.mbCCTLEnabled);
}

void ATCartridgePort::RemoveCartridge(uint32 id, IATDeviceCartridge *cart) {
	VDASSERT(id != 0);
	auto& cartInfo = mCarts[id];
	VDASSERT(cartInfo.mpCart == cart);

	// turn on enables, then turn off local mappings
	EnablePassThrough(id, true, true, true);
	OnLeftWindowChanged(id, false);

	// delink the cartridge
	auto& cartPrev = mCarts[cartInfo.mCartPrev];
	auto& cartNext = mCarts[cartInfo.mCartNext];

	cartPrev.mCartNext = cartInfo.mCartNext;
	cartNext.mCartPrev = cartInfo.mCartPrev;

	// clear cart entry
	cartInfo.mpCart = nullptr;
}

bool ATCartridgePort::IsLeftWindowEnabled(uint32 id) const {
	VDASSERT(id != 0 && id <= mCarts.size());
	auto& cartInfo = mCarts[id];
	VDASSERT(cartInfo.mpCart);

	return mCarts[cartInfo.mCartPrev].mbLeftMapEnabled;
}

bool ATCartridgePort::IsRightWindowEnabled(uint32 id) const {
	VDASSERT(id != 0 && id <= mCarts.size());
	auto& cartInfo = mCarts[id];
	VDASSERT(cartInfo.mpCart);
	
	return mCarts[cartInfo.mCartPrev].mbRightMapEnabled;
}

bool ATCartridgePort::IsCCTLEnabled(uint32 id) const {
	VDASSERT(id != 0 && id <= mCarts.size());
	auto& cartInfo = mCarts[id];
	VDASSERT(cartInfo.mpCart);
	
	return mCarts[cartInfo.mCartPrev].mbRightMapEnabled;
}

void ATCartridgePort::EnablePassThrough(uint32 id, bool leftEnabled, bool rightEnabled, bool cctlEnabled) {
	VDASSERT(id != 0 && id <= mCarts.size());
	VDASSERT(mCarts[id].mpCart);

	EnableCartPassThroughInternal(id, leftEnabled, rightEnabled, cctlEnabled);
}

void ATCartridgePort::OnLeftWindowChanged(uint32 id, bool enabled) {
	VDASSERT(id != 0 && id <= mCarts.size());
	auto& cartInfo = mCarts[id];

	VDASSERT(cartInfo.mpCart);

	if (cartInfo.mbLeftMapped != enabled) {
		cartInfo.mbLeftMapped = enabled;

		// propagate the state upward
		UpdateUpstreamSense(id);
	}
}

bool ATCartridgePort::IsLeftWindowActiveDownstream(uint32 id) const {
	VDASSERT(id != 0 && id <= mCarts.size());
	const auto& cartInfo = mCarts[id];

	VDASSERT(cartInfo.mpCart);

	return cartInfo.mbLeftMapSense;
}

void ATCartridgePort::NotifyLeftMapChanged() {
	mpLeftMapChangedHandler(mCarts[0].mbLeftMapSense != 0);
}

void ATCartridgePort::EnableCartPassThroughInternal(uint32 id, bool leftEnabled, bool rightEnabled, bool cctlEnabled) {
	auto& cartInfo = mCarts[id];

	// check if we actually have a state change
	if (cartInfo.mbLeftMapPassThrough == leftEnabled && cartInfo.mbRightMapPassThrough == rightEnabled
		&& cartInfo.mbCCTLPassThrough == cctlEnabled)
		return;

	cartInfo.mbLeftMapPassThrough = leftEnabled;
	cartInfo.mbRightMapPassThrough = rightEnabled;
	cartInfo.mbCCTLPassThrough = cctlEnabled;

	// combine pass through with upstream enables
	if (!cartInfo.mbLeftMapEnabled)
		leftEnabled = false;

	if (!cartInfo.mbRightMapEnabled)
		rightEnabled = false;

	if (!cartInfo.mbCCTLEnabled)
		cctlEnabled = false;

	// propagate enable change downstream
	uint32 nextId = cartInfo.mCartNext;
	while(nextId) {
		auto& nextCart = mCarts[nextId];

		// check if local enable states are changing
		if (nextCart.mbLeftMapEnabled == leftEnabled && nextCart.mbRightMapEnabled == rightEnabled
			&& nextCart.mbCCTLEnabled == cctlEnabled)
			break;

		// change local enable states
		nextCart.mbLeftMapEnabled = leftEnabled;
		nextCart.mbRightMapEnabled = rightEnabled;
		nextCart.mbCCTLEnabled = cctlEnabled;

		nextCart.mpCart->SetCartEnables(leftEnabled, rightEnabled, cctlEnabled);

		// update cumulative enables
		if (!nextCart.mbLeftMapEnabled)
			leftEnabled = false;

		if (!nextCart.mbRightMapEnabled)
			rightEnabled = false;

		if (!nextCart.mbCCTLEnabled)
			cctlEnabled = false;

		nextId = nextCart.mCartNext;
	}

	// propagate sense change upstream
	UpdateUpstreamSense(id);
}

void ATCartridgePort::UpdateUpstreamSense(uint32 id) {
	auto& cartInfo = mCarts[id];
	bool leftMapState = (cartInfo.mbLeftMapPassThrough && cartInfo.mbLeftMapSense) || cartInfo.mbLeftMapped;

	uint32 prevId = cartInfo.mCartPrev;
	for(;;) {
		auto& prevCart = mCarts[prevId];
		bool changed = false;

		if (prevCart.mbLeftMapSense == leftMapState)
			break;

		prevCart.mbLeftMapSense = leftMapState;

		// if we're at the root, signal a change and stop
		if (!prevId) {
			NotifyLeftMapChanged();
			break;
		}

		prevCart.mpCart->UpdateCartSense(leftMapState);
			
		// propagation stops if a cart isn't passing through or this cart is mapping
		if (!prevCart.mbLeftMapPassThrough || prevCart.mbLeftMapped)
			break;

		prevId = prevCart.mCartPrev;
	}
}
