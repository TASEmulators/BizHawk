#ifndef f_VD2_DITA_ACCEL_H
#define f_VD2_DITA_ACCEL_H

#include <vd2/system/vdstl.h>

class VDRegistryKey;

struct VDUIAccelerator {
	enum {
		kModCtrl		= 0x01,
		kModShift		= 0x02,
		kModAlt			= 0x04,
		kModExtended	= 0x08,
		kModUp			= 0x10,
		kModCooked		= 0x20
	};

	uint32		mVirtKey;
	uint32		mModifiers;
};

struct VDAccelToCommandEntry {
	uint32 mId;
	const char *mpName;
};

struct VDAccelTableEntry {
	const char *mpCommand;
	uint32 mCommandId;
	VDUIAccelerator mAccel;
};

class VDAccelTableDefinition {
public:
	VDAccelTableDefinition();
	VDAccelTableDefinition(const VDAccelTableDefinition&);
	~VDAccelTableDefinition();

	VDAccelTableDefinition& operator=(const VDAccelTableDefinition&);

	uint32 GetSize() const;

	const VDAccelTableEntry& operator[](uint32 index) const;
	const VDAccelTableEntry* operator()(const VDUIAccelerator& accel) const;

	void Clear();
	void Add(const VDAccelTableEntry& ent);
	void AddRange(const VDAccelTableEntry *ent, uint32 n);
	void RemoveAt(uint32 index);

	void Swap(VDAccelTableDefinition& dst);

	void Save(VDRegistryKey& key) const;
	void Load(VDRegistryKey& key, const VDAccelToCommandEntry *pCommands, uint32 nCommands);

public:
	typedef vdfastvector<VDAccelTableEntry> Accelerators;
	Accelerators	mAccelerators;
};

inline bool operator==(const VDUIAccelerator& x, const VDUIAccelerator& y) {
	return x.mVirtKey == y.mVirtKey && x.mModifiers == y.mModifiers;
}

void VDUIGetAcceleratorString(const VDUIAccelerator& accel, VDStringW& s);
bool VDUIGetVkAcceleratorForChar(VDUIAccelerator& accel, wchar_t c);
bool VDUIGetCharAcceleratorForVk(VDUIAccelerator& accel);

#endif
