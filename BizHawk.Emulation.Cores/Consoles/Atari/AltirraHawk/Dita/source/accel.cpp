#include <stdafx.h>
#include <vd2/system/error.h>
#include <vd2/system/registry.h>
#include <vd2/Dita/accel.h>

VDAccelTableDefinition::VDAccelTableDefinition() {
}

VDAccelTableDefinition::VDAccelTableDefinition(const VDAccelTableDefinition& src) {
	mAccelerators = src.mAccelerators;

	Accelerators::iterator it(mAccelerators.begin()), itEnd(mAccelerators.end());
	for(; it != itEnd; ++it) {
		VDAccelTableEntry& ent = *it;

		ent.mpCommand = _strdup(ent.mpCommand);
		if (!ent.mpCommand) {
			while(it != mAccelerators.begin()) {
				--it;
				free((void *)it->mpCommand);
			}

			throw MyMemoryError();
		}
	}
}

VDAccelTableDefinition::~VDAccelTableDefinition() {
	Clear();
}

VDAccelTableDefinition& VDAccelTableDefinition::operator=(const VDAccelTableDefinition& src) {
	if (&src != this) {
		VDAccelTableDefinition tmp(src);

		Swap(tmp);
	}

	return *this;
}

uint32 VDAccelTableDefinition::GetSize() const {
	return mAccelerators.size();
}

const VDAccelTableEntry& VDAccelTableDefinition::operator[](uint32 index) const {
	return mAccelerators[index];
}

const VDAccelTableEntry* VDAccelTableDefinition::operator()(const VDUIAccelerator& accel) const {
	for(Accelerators::const_iterator it(mAccelerators.begin()), itEnd(mAccelerators.end());
		it != itEnd;
		++it)
	{
		const VDAccelTableEntry& entry = *it;

		if (entry.mAccel.mVirtKey == accel.mVirtKey
			&& entry.mAccel.mModifiers == accel.mModifiers)
		{
			return &entry;
		}
	}

	return NULL;
}

void VDAccelTableDefinition::Clear() {
	while(!mAccelerators.empty()) {
		VDAccelTableEntry& ent = mAccelerators.back();
		free((void *)ent.mpCommand);
		mAccelerators.pop_back();
	}
}

void VDAccelTableDefinition::Add(const VDAccelTableEntry& src) {
	const char *s = _strdup(src.mpCommand);

	if (!s)
		throw MyMemoryError();

	VDAccelTableEntry& acc = mAccelerators.push_back();
	acc.mpCommand = s;
	acc.mCommandId = src.mCommandId;
	acc.mAccel = src.mAccel;
}

void VDAccelTableDefinition::AddRange(const VDAccelTableEntry *ent, uint32 n) {
	while(n--)
		Add(*ent++);
}

void VDAccelTableDefinition::RemoveAt(uint32 index) {
	if (index < mAccelerators.size()) {
		VDAccelTableEntry& acc = mAccelerators[index];

		free((void *)acc.mpCommand);

		mAccelerators.erase(mAccelerators.begin() + index);
	}
}

void VDAccelTableDefinition::Swap(VDAccelTableDefinition& dst) {
	mAccelerators.swap(dst.mAccelerators);
}

void VDAccelTableDefinition::Save(VDRegistryKey& key) const {
	VDRegistryValueIterator it(key);

	while(const char *name = it.Next()) {
		unsigned v;
		char term;
		if (sscanf(name, "%08x%c", &v, &term) != 1)
			continue;

		VDUIAccelerator accel;
		accel.mVirtKey = (v & 0xffff);
		accel.mModifiers = v >> 16;

		bool found = false;
		for(Accelerators::const_iterator it(mAccelerators.begin()), itEnd(mAccelerators.end()); it != itEnd; ++it) {
			const VDAccelTableEntry& ent = *it;

			if (ent.mAccel == accel) {
				found = true;
				break;
			}
		}

		if (!found)
			key.removeValue(name);
	}

	char buf[16];
	for(Accelerators::const_iterator it(mAccelerators.begin()), itEnd(mAccelerators.end()); it != itEnd; ++it) {
		const VDAccelTableEntry& ent = *it;

		sprintf(buf, "%08x", (ent.mAccel.mVirtKey & 0xffff) + (ent.mAccel.mModifiers << 16));

		key.setString(buf, ent.mpCommand);
	}
}

void VDAccelTableDefinition::Load(VDRegistryKey& key, const VDAccelToCommandEntry *pCommands, uint32 nCommands) {
	Clear();

	VDRegistryValueIterator it(key);

	VDStringA cmd;
	while(const char *name = it.Next()) {
		unsigned v;
		char term;
		if (sscanf(name, "%08x%c", &v, &term) != 1 || !v)
			continue;

		if (!key.getString(name, cmd))
			continue;

		VDAccelTableEntry& ent = mAccelerators.push_back();
		ent.mAccel.mVirtKey = (v & 0xffff);
		ent.mAccel.mModifiers = v >> 16;
		ent.mCommandId = 0;
		ent.mpCommand = _strdup(cmd.c_str());
		if (!ent.mpCommand) {
			mAccelerators.pop_back();
			throw MyMemoryError();
		}

		for(uint32 i=0; i<nCommands; ++i) {
			const VDAccelToCommandEntry& cmdent = pCommands[i];

			if (!_stricmp(ent.mpCommand, cmdent.mpName)) {
				ent.mCommandId = cmdent.mId;
				break;
			}
		}
	}
}
