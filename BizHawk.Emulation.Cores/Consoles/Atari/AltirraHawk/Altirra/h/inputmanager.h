//	Altirra - Atari 800/800XL emulator
//	Copyright (C) 2009 Avery Lee
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

#ifndef f_AT_INPUTMANAGER_H
#define f_AT_INPUTMANAGER_H

#include <map>
#include <set>
#include <vd2/system/VDString.h>
#include <vd2/system/vdstl.h>
#include <vd2/system/refcount.h>

class ATPortController;
class ATPortInputController;
class ATJoystickController;
class ATMouseController;
class ATPaddleController;
class IATJoystickManager;
class ATScheduler;
class VDRegistryKey;
class ATLightPenPort;

class IATInputConsoleCallback {
public:
	virtual void SetConsoleTrigger(uint32 id, bool state) = 0;
};

class IATInputUnitNameSource {
public:
	virtual bool GetInputCodeName(uint32 id, VDStringW& name) const = 0;
};

enum ATInputCode : uint32 {
	kATInputCode_None			= 0x00,

	kATInputCode_KeyBack		= 0x08,		// VK_BACK
	kATInputCode_KeyTab			= 0x09,		// VK_TAB
	kATInputCode_KeyReturn		= 0x0D,		// VK_RETURN
	kATInputCode_KeyEscape		= 0x1B,		// VK_ESCAPE
	kATInputCode_KeySpace		= 0x20,		// VK_SPACE
	kATInputCode_KeyPrior		= 0x21,		// VK_PRIOR
	kATInputCode_KeyNext		= 0x22,		// VK_NEXT
	kATInputCode_KeyEnd			= 0x23,		// VK_END
	kATInputCode_KeyHome		= 0x24,		// VK_HOME
	kATInputCode_KeyLeft		= 0x25,		// VK_LEFT
	kATInputCode_KeyUp			= 0x26,		// VK_UP
	kATInputCode_KeyRight		= 0x27,		// VK_RIGHT
	kATInputCode_KeyDown		= 0x28,		// VK_DOWN
	kATInputCode_KeyInsert		= 0x2D,		// VK_INSERT
	kATInputCode_KeyDelete		= 0x2E,		// VK_DELETE
	kATInputCode_Key0			= 0x30,		// VK_0
	kATInputCode_Key1			= 0x31,		//
	kATInputCode_Key2			= 0x32,		//
	kATInputCode_Key3			= 0x33,		//
	kATInputCode_Key4			= 0x34,		//
	kATInputCode_Key5			= 0x35,		//
	kATInputCode_Key6			= 0x36,		//
	kATInputCode_Key7			= 0x37,		//
	kATInputCode_Key8			= 0x38,		//
	kATInputCode_Key9			= 0x39,		//
	kATInputCode_KeyA			= 0x41,		// VK_A
	kATInputCode_KeyP			= 0x50,		//
	kATInputCode_KeyR			= 0x52,		//
	kATInputCode_KeyS			= 0x53,		//
	kATInputCode_KeyNumpad0		= 0x60,		// VK_NUMPAD0
	kATInputCode_KeyNumpad1		= 0x61,		// VK_NUMPAD1
	kATInputCode_KeyNumpad2		= 0x62,		// VK_NUMPAD2
	kATInputCode_KeyNumpad3		= 0x63,		// VK_NUMPAD3
	kATInputCode_KeyNumpad4		= 0x64,		// VK_NUMPAD4
	kATInputCode_KeyNumpad5		= 0x65,		// VK_NUMPAD5
	kATInputCode_KeyNumpad6		= 0x66,		// VK_NUMPAD6
	kATInputCode_KeyNumpad7		= 0x67,		// VK_NUMPAD7
	kATInputCode_KeyNumpad8		= 0x68,		// VK_NUMPAD8
	kATInputCode_KeyNumpad9		= 0x69,		// VK_NUMPAD9
	kATInputCode_KeyMultiply	= 0x6A,		// VK_MULTIPLY
	kATInputCode_KeyAdd			= 0x6B,		// VK_ADD
	kATInputCode_KeySubtract	= 0x6D,		// VK_SUBTRACT
	kATInputCode_KeyDecimal		= 0x6E,		// VK_DECIMAL
	kATInputCode_KeyDivide		= 0x6F,		// VK_DIVIDE
	kATInputCode_KeyF1			= 0x70,		// VK_F1
	kATInputCode_KeyF2			= 0x71,		// VK_F2
	kATInputCode_KeyF3			= 0x72,		// VK_F3
	kATInputCode_KeyF4			= 0x73,		// VK_F4
	kATInputCode_KeyF5			= 0x74,		// VK_F5
	kATInputCode_KeyF6			= 0x75,		// VK_F6
	kATInputCode_KeyF7			= 0x76,		// VK_F7
	kATInputCode_KeyF8			= 0x77,		// VK_F8
	kATInputCode_KeyF9			= 0x78,		// VK_F9
	kATInputCode_KeyF10			= 0x79,		// VK_F10
	kATInputCode_KeyF11			= 0x7A,		// VK_F11
	kATInputCode_KeyF12			= 0x7B,		// VK_F12
	kATInputCode_KeyLShift		= 0xA0,		// VK_LSHIFT
	kATInputCode_KeyRShift		= 0xA1,		// VK_RSHIFT
	kATInputCode_KeyLControl	= 0xA2,		// VK_LCONTROL
	kATInputCode_KeyRControl	= 0xA3,		// VK_RCONTROL
	kATInputCode_KeyOem1		= 0xBA,		// VK_OEM_1   // ';:' for US
	kATInputCode_KeyOemPlus		= 0xBB,		// VK_OEM_PLUS   // '+' any country
	kATInputCode_KeyOemComma	= 0xBC,		// VK_OEM_COMMA   // ',' any country
	kATInputCode_KeyOemMinus	= 0xBD,		// VK_OEM_MINUS   // '-' any country
	kATInputCode_KeyOemPeriod	= 0xBE,		// VK_OEM_PERIOD   // '.' any country
	kATInputCode_KeyOem2		= 0xBF,		// VK_OEM_2   // '/?' for US
	kATInputCode_KeyOem3		= 0xC0,		// VK_OEM_3   // '`~' for US
	kATInputCode_KeyOem4		= 0xDB,		// VK_OEM_4  //  '[{' for US
	kATInputCode_KeyOem5		= 0xDC,		// VK_OEM_5  //  '\|' for US
	kATInputCode_KeyOem6		= 0xDD,		// VK_OEM_6  //  ']}' for US
	kATInputCode_KeyOem7		= 0xDE,		// VK_OEM_7  //  ''"' for US
	kATInputCode_KeyNumpadEnter	= 0x10D,	// VK_RETURN (extended)

	kATInputCode_MouseClass		= 0x1000,
	kATInputCode_MouseHoriz		= 0x1000,
	kATInputCode_MouseVert		= 0x1001,
	kATInputCode_MousePadX		= 0x1002,
	kATInputCode_MousePadY		= 0x1003,
	kATInputCode_MouseBeamX		= 0x1004,
	kATInputCode_MouseBeamY		= 0x1005,
	kATInputCode_MouseLeft		= 0x1100,
	kATInputCode_MouseRight		= 0x1101,
	kATInputCode_MouseUp		= 0x1102,
	kATInputCode_MouseDown		= 0x1103,
	kATInputCode_MouseLMB		= 0x1800,
	kATInputCode_MouseMMB		= 0x1801,
	kATInputCode_MouseRMB		= 0x1802,
	kATInputCode_MouseX1B		= 0x1803,
	kATInputCode_MouseX2B		= 0x1804,

	kATInputCode_JoyClass		= 0x2000,
	kATInputCode_JoyHoriz1		= 0x2000,
	kATInputCode_JoyVert1		= 0x2001,
	kATInputCode_JoyVert2		= 0x2002,
	kATInputCode_JoyHoriz3		= 0x2003,
	kATInputCode_JoyVert3		= 0x2004,
	kATInputCode_JoyVert4		= 0x2005,
	kATInputCode_JoyPOVHoriz	= 0x2006,
	kATInputCode_JoyPOVVert		= 0x2007,
	kATInputCode_JoyStick1Left	= 0x2100,
	kATInputCode_JoyStick1Right	= 0x2101,
	kATInputCode_JoyStick1Up	= 0x2102,
	kATInputCode_JoyStick1Down	= 0x2103,
	kATInputCode_JoyStick2Up	= 0x2104,
	kATInputCode_JoyStick2Down	= 0x2105,
	kATInputCode_JoyStick3Left	= 0x2106,
	kATInputCode_JoyStick3Right	= 0x2107,
	kATInputCode_JoyStick3Up	= 0x2108,
	kATInputCode_JoyStick3Down	= 0x2109,
	kATInputCode_JoyStick4Up	= 0x210A,
	kATInputCode_JoyStick4Down	= 0x210B,
	kATInputCode_JoyPOVLeft		= 0x210C,
	kATInputCode_JoyPOVRight	= 0x210D,
	kATInputCode_JoyPOVUp		= 0x210E,
	kATInputCode_JoyPOVDown		= 0x210F,
	kATInputCode_JoyButton0		= 0x2800,

	kATInputCode_ClassMask		= 0xF000,
	kATInputCode_IdMask			= 0xFFFF,

	kATInputCode_FlagCheck0		= 0x00010000,
	kATInputCode_FlagCheck1		= 0x00020000,
	kATInputCode_FlagCheckMask	= 0x00030000,
	kATInputCode_FlagValue0		= 0x00040000,
	kATInputCode_FlagValue1		= 0x00080000,
	kATInputCode_FlagValueMask	= 0x000C0000,
	kATInputCode_FlagMask		= 0x000F0000,

	kATInputCode_SpecificUnit	= 0x80000000,
	kATInputCode_UnitScale		= 0x01000000,
	kATInputCode_UnitShift		= 24
};

enum ATInputControllerType {
	kATInputControllerType_None,
	kATInputControllerType_Joystick,
	kATInputControllerType_Paddle,
	kATInputControllerType_STMouse,
	kATInputControllerType_Console,
	kATInputControllerType_5200Controller,
	kATInputControllerType_InputState,
	kATInputControllerType_LightPen,
	kATInputControllerType_Tablet,
	kATInputControllerType_KoalaPad,
	kATInputControllerType_AmigaMouse,
	kATInputControllerType_Keypad,
	kATInputControllerType_Trackball_CX80_V1,
	kATInputControllerType_5200Trackball,
	kATInputControllerType_Driving,
	kATInputControllerType_Keyboard
};

struct atfixedhash_basenode {
	atfixedhash_basenode *mpNext;
	atfixedhash_basenode *mpPrev;
};

template<typename T>
struct atfixedhash_node : public atfixedhash_basenode {
	T mValue;

	atfixedhash_node(const T& v) : mValue(v) {}
};

template<typename T>
struct athash
{
	size_t operator()(const T& value) const {
		return (size_t)value;
	}
};

template<typename K, typename V, size_t N>
class atfixedhash {
public:
	typedef K key_type;
	typedef V mapped_type;
	typedef typename std::pair<K, V> value_type;
	typedef value_type *pointer;
	typedef const value_type *const_pointer;
	typedef value_type& reference;
	typedef const value_type& const_reference;
	typedef athash<K> hasher;

	typedef pointer iterator;
	typedef const_pointer const_iterator;

	atfixedhash();
	~atfixedhash();

	const_iterator end() const;

	void clear();

	template<class T>
	void get_keys(T& result) const {
		for(const auto& bucket : m.table) {
			for(const auto *p = bucket.mpNext; p != &bucket; p = p->mpNext)
				result.push_back(static_cast<const atfixedhash_node<value_type> *>(p)->mValue.first);
		}
	}

	iterator find(const key_type& k);
	std::pair<pointer, bool> insert(const value_type& v);

protected:
	struct : hasher {
		atfixedhash_basenode table[N];
	} m;
};

template<typename K, typename V, size_t N>
atfixedhash<K,V,N>::atfixedhash() {
	for(int i=0; i<N; ++i) {
		atfixedhash_basenode& n = m.table[i];

		n.mpNext = n.mpPrev = &n;
	}
}

template<typename K, typename V, size_t N>
atfixedhash<K,V,N>::~atfixedhash() {
	clear();
}

template<typename K, typename V, size_t N>
typename atfixedhash<K,V,N>::const_iterator atfixedhash<K,V,N>::end() const {
	return NULL;
}

template<typename K, typename V, size_t N>
void atfixedhash<K,V,N>::clear() {
	for(int i=0; i<N; ++i) {
		atfixedhash_basenode *bucket = &m.table[i];
		atfixedhash_basenode *p = bucket->mpNext;

		if (p != bucket) {
			do {
				atfixedhash_basenode *n = p->mpNext;

				delete static_cast<atfixedhash_node<value_type> *>(p);

				p = n;
			} while(p != bucket);

			bucket->mpPrev = bucket->mpNext = bucket;
		}
	}
}

template<typename K, typename V, size_t N>
typename atfixedhash<K,V,N>::iterator atfixedhash<K,V,N>::find(const key_type& k) {
	size_t bucketIdx = m(k) % N;
	const atfixedhash_basenode *bucket = &m.table[bucketIdx];

	for(atfixedhash_basenode *p = bucket->mpNext; p != bucket; p = p->mpNext) {
		atfixedhash_node<value_type>& n = static_cast<atfixedhash_node<value_type>&>(*p);

		if (n.mValue.first == k)
			return &n.mValue;
	}

	return NULL;
}

template<typename K, typename V, size_t N>
std::pair<typename atfixedhash<K,V,N>::iterator, bool> atfixedhash<K,V,N>::insert(const value_type& v) {
	size_t bucketIdx = m(v.first) % N;
	
	atfixedhash_basenode *bucket = &m.table[bucketIdx];

	for(atfixedhash_basenode *p = bucket->mpNext; p != bucket; p = p->mpNext) {
		atfixedhash_node<value_type>& n = static_cast<atfixedhash_node<value_type>&>(*p);

		if (n.mValue.first == v.first)
			return std::pair<iterator, bool>(&n.mValue, false);
	}

	atfixedhash_node<value_type> *node = new atfixedhash_node<value_type>(v);
	atfixedhash_basenode *last = bucket->mpPrev;
	node->mpPrev = last;
	node->mpNext = bucket;
	last->mpNext = node;
	bucket->mpPrev = node;
	return std::pair<iterator, bool>(&node->mValue, true);
}

struct ATInputUnitIdentifier {
	char buf[16];

	bool IsZero() const {
		for(int i=0; i<16; ++i) {
			if (buf[i])
				return false;
		}

		return true;
	}

	void SetZero() {
		memset(buf, 0, sizeof buf);
	}

	bool operator==(const ATInputUnitIdentifier& x) const {
		return !memcmp(buf, x.buf, 16);
	}
};

class ATInputMap final : public vdrefcounted<IVDRefCount> {
public:
	struct Controller {
		ATInputControllerType mType;
		uint32 mIndex;
	};

	struct Mapping {
		uint32 mInputCode;
		uint32 mControllerId;
		uint32 mCode;
	};

	ATInputMap();
	~ATInputMap();

	const wchar_t *GetName() const;
	void SetName(const wchar_t *name);

	bool IsQuickMap() const { return mbQuickMap; }
	void SetQuickMap(bool q) { mbQuickMap = q; }

	bool UsesPhysicalPort(int portIdx) const;

	void Clear();

	int GetSpecificInputUnit() const {
		return mSpecificInputUnit;
	}

	void SetSpecificInputUnit(int index) {
		mSpecificInputUnit = index;
	}

	uint32 GetControllerCount() const;
	bool HasControllerType(ATInputControllerType type) const;
	const Controller& GetController(uint32 i) const;
	uint32 AddController(ATInputControllerType type, uint32 index);
	void AddControllers(std::initializer_list<Controller> controllers);

	uint32 GetMappingCount() const;
	const Mapping& GetMapping(uint32 i) const;
	void AddMapping(uint32 inputCode, uint32 controllerId, uint32 code);
	void AddMappings(std::initializer_list<Mapping> mappings);

	bool Load(VDRegistryKey& key, const char *name);
	void Save(VDRegistryKey& key, const char *name);

protected:
	typedef vdfastvector<Controller> Controllers;
	Controllers mControllers;

	typedef vdfastvector<Mapping> Mappings;
	Mappings mMappings;

	VDStringW mName;
	int mSpecificInputUnit;

	bool mbQuickMap;
};

class ATInputManager {
public:
	ATInputManager();
	~ATInputManager();

	void Init(ATScheduler *fastScheduler, ATScheduler *slowScheduler, ATPortController *porta, ATPortController *portb, ATLightPenPort *lightPen);
	void Shutdown();

	void Set5200Mode(bool is5200);

	void ResetToDefaults();

	IATInputConsoleCallback *GetConsoleCallback() const { return mpCB; }
	void SetConsoleCallback(IATInputConsoleCallback *cb) { mpCB = cb; }

	void Select5200Controller(int index, bool potsEnabled);
	void SelectMultiJoy(int multiIndex);

	void Poll(float dt);

	int GetInputUnitCount() const;
	const wchar_t *GetInputUnitName(int index) const;
	int GetInputUnitIndexById(const ATInputUnitIdentifier& id) const;
	int RegisterInputUnit(const ATInputUnitIdentifier& id, const wchar_t *name, IATInputUnitNameSource *nameSource);
	void UnregisterInputUnit(int unit);

	/// Enables or disables restricted mode. Restricted mode limits triggers
	/// UI triggers only. All other triggers are forced off.
	void SetRestrictedMode(bool restricted);

	bool IsInputMapped(int unit, uint32 inputCode) const;
	bool IsMouseMapped() const { return mbMouseMapped; }
	bool IsMouseAbsoluteMode() const { return mbMouseAbsMode; }
	bool IsMouseActiveTarget() const { return mbMouseActiveTarget; }

	void OnButtonDown(int unit, int id);
	void OnButtonUp(int unit, int id);
	void OnAxisInput(int unit, int axis, sint32 value, sint32 deadifiedValue);
	void OnMouseMove(int unit, int dx, int dy);
	void SetMouseBeamPos(int x, int y);
	void SetMousePadPos(int x, int y);
	void ActivateFlag(uint32 id, bool state);
	void ReleaseButtons(uint32 idmin, uint32 idmax);

	void GetNameForInputCode(uint32 code, VDStringW& name) const;
	void GetNameForTargetCode(uint32 code, ATInputControllerType type, VDStringW& name) const;
	bool IsAnalogTrigger(uint32 code, ATInputControllerType type) const;

	uint32 GetInputMapCount() const;
	bool GetInputMapByIndex(uint32 index, ATInputMap **imap) const;
	bool IsInputMapEnabled(ATInputMap *imap) const;
	void AddInputMap(ATInputMap *imap);
	void RemoveInputMap(ATInputMap *imap);
	void RemoveAllInputMaps();
	void ActivateInputMap(ATInputMap *imap, bool enable);
	ATInputMap *CycleQuickMaps();

	uint32 GetPresetInputMapCount() const;
	bool GetPresetInputMapByIndex(uint32 index, ATInputMap **imap) const;

	bool LoadMaps(VDRegistryKey& key);
	void LoadSelections(VDRegistryKey& key, ATInputControllerType defaultControllerType);
	void SaveMaps(VDRegistryKey& key);
	void SaveSelections(VDRegistryKey& key);

protected:
	struct Mapping;
	struct Trigger;
	struct PresetMapDef;

	void RebuildMappings();
	void ActivateMappings(uint32 id, bool state);
	void ActivateAnalogMappings(uint32 id, int ds, int dsdead);
	void ActivateImpulseMappings(uint32 id, int ds);
	void ClearTriggers();
	void SetTrigger(Mapping& mapping, bool state);
	void Update5200Controller();
	uint32 GetPresetMapDefCount() const;
	const PresetMapDef *GetPresetMapDef(uint32 index) const;
	void InitPresetMap(const PresetMapDef& def, ATInputMap **ppMap) const;
	void InitPresetMaps();
	bool IsTriggerRestricted(const Trigger& trigger) const;

	ATScheduler *mpSlowScheduler;
	ATScheduler *mpFastScheduler;
	ATLightPenPort *mpLightPen;
	ATPortController *mpPorts[2];
	IATInputConsoleCallback *mpCB;
	bool mbRestrictedMode;
	int m5200ControllerIndex;
	bool mb5200PotsEnabled;
	bool mb5200Mode;
	bool mbMouseAbsMode;
	bool mbMouseMapped;
	bool mbMouseActiveTarget;

	uint32 mMouseAvgQueue[4];
	int mMouseAvgIndex;

	typedef atfixedhash<int, uint32, 64> Buttons;
	Buttons mButtons;

	struct Mapping {
		uint32 mTriggerIdx;
		uint32 mFlagIndex1;
		uint32 mFlagIndex2;
		bool mbFlagValue1;
		bool mbFlagValue2;
		bool mbMotionActive;
		bool mbTriggerActivated;
		uint8 mAutoCounter;
		uint8 mAutoPeriod;
		uint8 mAutoValue;
		float mMotionSpeed;
		float mMotionAccel;
		float mMotionDrag;
	};

	typedef vdfastvector<bool> Flags;
	Flags mFlags;

	typedef std::multimap<uint32, Mapping> Mappings;
	Mappings mMappings;

	struct Trigger {
		uint32 mId;
		uint32 mCount;
		ATPortInputController *mpController;
	};

	typedef vdfastvector<Trigger> Triggers;
	Triggers mTriggers;

	typedef std::map<ATInputMap *, bool> InputMaps;
	InputMaps mInputMaps;

	struct ControllerInfo {
		ATPortInputController *mpInputController;
		bool mbBoundToMouseAbs;
	};

	typedef vdfastvector<ControllerInfo> InputControllers;
	InputControllers mInputControllers;

	uint32	mAllocatedUnits;
	ATInputUnitIdentifier mUnitIds[32];
	VDStringW	mUnitNames[32];
	IATInputUnitNameSource *mpUnitNameSources[32];

	static const PresetMapDef kPresetMapDefs[];
};

#endif	// f_AT_INPUTMANAGER_H
