//	Altirra - Atari 800/800XL/5200 emulator
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
#include <vd2/system/strutil.h>
#include <at/atcore/propertyset.h>
#include "idephysdisk.h"
#include "iderawimage.h"
#include "idevhdimage.h"

void ATCreateDeviceHardDiskPhysical(const ATPropertySet& pset, IATDevice **dev) {
	vdrefptr<ATIDEPhysicalDisk> p(new ATIDEPhysicalDisk);

	p->Init(pset.GetString("path"));

	*dev = p;
	(*dev)->AddRef();
}

void ATCreateDeviceHardDiskRawImage(const ATPropertySet& pset, IATDevice **dev) {
	vdrefptr<ATIDERawImage> p(new ATIDERawImage);

	p->Init(
		pset.GetString("path"),
		pset.GetBool("write_enabled"),
		pset.GetBool("solid_state"),
		pset.GetUint32("sectors"),
		pset.GetUint32("cylinders"),
		pset.GetUint32("heads"),
		pset.GetUint32("sectors_per_track"));
	p->SetSettings(pset);

	*dev = p;
	(*dev)->AddRef();
}

void ATCreateDeviceHardDiskVHDImage(const ATPropertySet& pset, IATDevice **dev) {
	vdrefptr<ATIDEVHDImage> p(new ATIDEVHDImage);

	p->Init(pset.GetString("path"), pset.GetBool("write_enabled"), pset.GetBool("solid_state"));

	*dev = p;
	(*dev)->AddRef();
}

void ATCreateDeviceHardDisk(const ATPropertySet& pset, IATDevice **dev) {
	const wchar_t *path = pset.GetString("path");

	if (path) {
		if (ATIDEIsPhysicalDiskPath(path))
			return ATCreateDeviceHardDiskPhysical(pset, dev);

		size_t pathlen = wcslen(path);

		if (pathlen > 4 && !vdwcsicmp(path + pathlen - 4, L".vhd"))
			return ATCreateDeviceHardDiskVHDImage(pset, dev);
	}

	return ATCreateDeviceHardDiskRawImage(pset, dev);
}

extern const ATDeviceDefinition g_ATDeviceDefHardDisks = { "harddisk", "harddisk", L"Hard disk", ATCreateDeviceHardDisk };
