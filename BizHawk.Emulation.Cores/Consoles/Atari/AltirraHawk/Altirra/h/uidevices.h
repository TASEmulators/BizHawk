//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2018 Avery Lee
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
//	You should have received a copy of the GNU General Public License along
//	with this program. If not, see <http://www.gnu.org/licenses/>.

#ifndef f_AT_UIDEVICES_H
#define f_AT_UIDEVICES_H

#include <at/atnativeui/dialog.h>
#include <at/atnativeui/uiproxies.h>

class IATDevice;
class ATPropertySet;
class ATDeviceManager;

class ATUIControllerDevices final {
public:
	ATUIControllerDevices(VDDialogFrameW32& parent, ATDeviceManager& devMgr, VDUIProxyTreeViewControl& treeView, VDUIProxyButtonControl& settingsView, VDUIProxyButtonControl& removeView);

	void OnDataExchange(bool write);
	void OnDpiChanged();
	void Add();
	void Remove();
	void RemoveAll();
	void Settings();
	void CreateDeviceNode(VDUIProxyTreeViewControl::NodeRef parentNode, IATDevice *dev, const wchar_t *prefix);
	void SaveSettings(const char *configTag, const ATPropertySet& props);

private:
	void OnItemSelectionChanged(VDUIProxyTreeViewControl *sender, int idx);
	void OnItemDoubleClicked(VDUIProxyTreeViewControl *sender, bool *handled);
	void OnItemGetDisplayAttributes(VDUIProxyTreeViewControl *sender, VDUIProxyTreeViewControl::GetDispAttrEvent *event);

	void UpdateIcons();

	VDDialogFrameW32& mParent;
	ATDeviceManager& mDevMgr;
	VDUIProxyTreeViewControl& mTreeView;
	VDUIProxyButtonControl& mSettingsView;
	VDUIProxyButtonControl& mRemoveView;

	struct DeviceNode;

	VDDelegate mDelSelectionChanged;
	VDDelegate mDelDoubleClicked;
	VDDelegate mDelGetDisplayAttributes;
};

#endif
