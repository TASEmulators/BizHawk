#include <stdafx.h>
#include <vd2/system/vdalloc.h>
#include <at/atcore/propertyset.h>

ATPropertySet::ATPropertySet() {
}

ATPropertySet::ATPropertySet(const ATPropertySet& src) {
	operator=(src);
}

ATPropertySet::~ATPropertySet() {
	Clear();
}

ATPropertySet& ATPropertySet::operator=(const ATPropertySet& src) {
	if (&src != this) {
		Clear();

		auto it = src.mProperties.begin(), itEnd = src.mProperties.end();

		for(; it != itEnd; ++it) {
			const char *name = it->first;
			size_t len = strlen(name);

			vdautoarrayptr<char> newname(new char[len+1]);
			memcpy(newname.get(), name, len+1);

			vdautoarrayptr<wchar_t> newstr;
			if (it->second.mType == kATPropertyType_String16) {
				size_t len2 = wcslen(it->second.mValStr16);
				newstr = new wchar_t[len2 + 1];
				memcpy(newstr.get(), it->second.mValStr16, sizeof(wchar_t)*(len2 + 1));
			}

			ATPropertyValue& newVal = mProperties.insert(newname.get()).first->second;
			newname.release();

			newVal = it->second;

			if (it->second.mType == kATPropertyType_String16) {
				newVal.mValStr16 = newstr.get();
				newstr.release();
			}
		}
	}

	return *this;
}

void ATPropertySet::Clear() {
	auto it = mProperties.begin(), itEnd = mProperties.end();

	while(it != itEnd) {
		const char *name = it->first;

		if (it->second.mType == kATPropertyType_String16) {
			wchar_t *s = it->second.mValStr16;
			it->second.mValStr16 = nullptr;

			delete[] s;
		}

		it = mProperties.erase(it);

		delete[] name;
	}
}

void ATPropertySet::EnumProperties(void (*fn)(const char *name, const ATPropertyValue& val, void *data), void *data) const {
	for(auto it = mProperties.begin(), itEnd = mProperties.end();
		it != itEnd;
		++it)
	{
		fn(it->first, it->second, data);
	}
}

void ATPropertySet::Unset(const char *name) {
	auto it = mProperties.find(name);

	if (it != mProperties.end()) {
		const char *name = it->first;

		mProperties.erase(it);
		delete[] name;
	}
}

void ATPropertySet::SetBool(const char *name, bool val) {
	CreateProperty(name, kATPropertyType_Bool).mValBool = val;
}

void ATPropertySet::SetInt32(const char *name, sint32 val) {
	CreateProperty(name, kATPropertyType_Int32).mValI32 = val;
}

void ATPropertySet::SetUint32(const char *name, uint32 val) {
	CreateProperty(name, kATPropertyType_Uint32).mValU32 = val;
}

void ATPropertySet::SetFloat(const char *name, float val) {
	CreateProperty(name, kATPropertyType_Float).mValF = val;
}

void ATPropertySet::SetDouble(const char *name, double val) {
	CreateProperty(name, kATPropertyType_Double).mValD = val;
}

void ATPropertySet::SetString(const char *name, const wchar_t *val) {
	size_t len = wcslen(val);
	vdautoarrayptr<wchar_t> newstr(new wchar_t[len+1]);
	memcpy(newstr.get(), val, sizeof(wchar_t)*(len+1));

	ATPropertyValue& propVal = CreateProperty(name, kATPropertyType_String16);
	propVal.mValStr16 = newstr.get();
	newstr.release();
}

bool ATPropertySet::GetBool(const char *name, bool def) const {
	bool val = def;
	TryGetBool(name, val);
	return val;
}

sint32 ATPropertySet::GetInt32(const char *name, sint32 def) const {
	sint32 val = def;
	TryGetInt32(name, val);
	return val;
}

uint32 ATPropertySet::GetUint32(const char *name, uint32 def) const {
	uint32 val = def;
	TryGetUint32(name, val);
	return val;
}

float ATPropertySet::GetFloat(const char *name, float def) const {
	float val = def;
	TryGetFloat(name, val);
	return val;
}

double ATPropertySet::GetDouble(const char *name, double def) const {
	double val = def;
	TryGetDouble(name, val);
	return val;
}

const wchar_t *ATPropertySet::GetString(const char *name, const wchar_t *def) const {
	const wchar_t *val = def;
	TryGetString(name, val);
	return val;
}

bool ATPropertySet::TryGetBool(const char *name, bool& val) const {
	const ATPropertyValue *propVal = GetProperty(name);

	if (!propVal)
		return false;

	switch(propVal->mType) {
		case kATPropertyType_Float:
			val = propVal->mValF != 0;
			return true;

		case kATPropertyType_Double:
			val = propVal->mValD != 0;
			return true;

		case kATPropertyType_Int32:
			val = propVal->mValI32 != 0;
			return true;

		case kATPropertyType_Uint32:
			val = propVal->mValU32 != 0;
			return true;

		case kATPropertyType_Bool:
			val = propVal->mValBool;
			return true;

		default:
			return false;
	}
}

bool ATPropertySet::TryGetInt32(const char *name, sint32& val) const {
	const ATPropertyValue *propVal = GetProperty(name);

	if (!propVal)
		return false;

	switch(propVal->mType) {
		case kATPropertyType_Bool:
			val = propVal->mValBool ? 1 : 0;
			return true;

		case kATPropertyType_Float:
			if ((double)propVal->mValF < -0x7FFFFFFF-1 || (double)propVal->mValF > 0x7FFFFFFF)
				return false;

			val = (sint32)propVal->mValF;
			return true;

		case kATPropertyType_Double:
			if (propVal->mValD < -0x7FFFFFFF-1 || propVal->mValD > 0x7FFFFFFF)
				return false;

			val = (sint32)propVal->mValD;
			return true;

		case kATPropertyType_Int32:
			val = propVal->mValI32;
			return true;

		case kATPropertyType_Uint32:
			if (propVal->mValU32 > 0x7FFFFFFF)
				return false;

			val = (sint32)propVal->mValU32;
			return true;

		default:
			return false;
	}
}

bool ATPropertySet::TryGetUint32(const char *name, uint32& val) const {
	const ATPropertyValue *propVal = GetProperty(name);

	if (!propVal)
		return false;

	switch(propVal->mType) {
		case kATPropertyType_Bool:
			val = propVal->mValBool ? 1 : 0;
			return true;

		case kATPropertyType_Float:
			if (propVal->mValF < 0 || (double)propVal->mValF > 0xFFFFFFFF)
				return false;

			val = (uint32)propVal->mValF;
			return true;

		case kATPropertyType_Double:
			if (propVal->mValD < 0 || propVal->mValD > 0xFFFFFFFF)
				return false;

			val = (uint32)propVal->mValD;
			return true;

		case kATPropertyType_Int32:
			if (propVal->mValI32 < 0)
				return false;

			val = (uint32)propVal->mValI32;
			return true;

		case kATPropertyType_Uint32:
			val = propVal->mValU32;
			return true;

		default:
			return false;
	}
}

bool ATPropertySet::TryGetFloat(const char *name, float& val) const {
	const ATPropertyValue *propVal = GetProperty(name);

	if (!propVal)
		return false;

	switch(propVal->mType) {
		case kATPropertyType_Bool:
			val = propVal->mValBool ? 1 : 0;
			return true;

		case kATPropertyType_Int32:
			val = (float)propVal->mValI32;
			return true;

		case kATPropertyType_Uint32:
			val = (float)propVal->mValU32;
			return true;

		case kATPropertyType_Double:
			val = (float)propVal->mValD;
			return true;

		case kATPropertyType_Float:
			val = propVal->mValF;
			return true;

		default:
			return false;
	}
}

bool ATPropertySet::TryGetDouble(const char *name, double& val) const {
	const ATPropertyValue *propVal = GetProperty(name);

	if (!propVal)
		return false;

	switch(propVal->mType) {
		case kATPropertyType_Bool:
			val = propVal->mValBool ? 1 : 0;
			return true;

		case kATPropertyType_Int32:
			val = propVal->mValI32;
			return true;

		case kATPropertyType_Uint32:
			val = propVal->mValU32;
			return true;

		case kATPropertyType_Float:
			val = propVal->mValF;
			return true;

		case kATPropertyType_Double:
			val = propVal->mValD;
			return true;

		default:
			return false;
	}
}

bool ATPropertySet::TryGetString(const char *name, const wchar_t *& val) const {
	const ATPropertyValue *propVal = GetProperty(name);

	if (!propVal || propVal->mType != kATPropertyType_String16)
		return false;

	val = propVal->mValStr16;
	return true;
}

const ATPropertyValue *ATPropertySet::GetProperty(const char *name) const {
	auto it = mProperties.find(name);

	return it != mProperties.end() ? &it->second : nullptr;
}

ATPropertyValue& ATPropertySet::CreateProperty(const char *name, ATPropertyType type) {
	auto it = mProperties.find(name);

	if (it != mProperties.end()) {
		if (it->second.mType == kATPropertyType_String16)
			delete[] it->second.mValStr16;

		it->second.mType = type;
		return it->second;
	}

	size_t len = strlen(name);
	vdautoarrayptr<char> newName(new char[len + 1]);
	memcpy(newName.get(), name, len+1);

	ATPropertyValue& newVal = mProperties.insert(newName.get()).first->second;
	newName.release();

	newVal.mType = type;
	return newVal;
}
