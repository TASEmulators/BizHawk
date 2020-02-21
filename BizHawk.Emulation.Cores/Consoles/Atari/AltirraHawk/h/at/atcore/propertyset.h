#ifndef f_AT_ATCORE_PROPERTYSET_H
#define f_AT_ATCORE_PROPERTYSET_H

#include <vd2/system/hash.h>
#include <vd2/system/vdstl.h>
#include <vd2/system/vdstl_hashmap.h>
#include <vd2/system/VDString.h>

enum ATPropertyType {
	kATPropertyType_None,
	kATPropertyType_Bool,
	kATPropertyType_Int32,
	kATPropertyType_Uint32,
	kATPropertyType_Float,
	kATPropertyType_Double,
	kATPropertyType_String16
};

struct ATPropertyValue {
	ATPropertyType mType;

	union {
		bool mValBool;
		sint32 mValI32;
		uint32 mValU32;
		float mValF;
		double mValD;
		wchar_t *mValStr16;
	};
};

class ATPropertySet {
public:
	ATPropertySet();
	ATPropertySet(const ATPropertySet&);
	~ATPropertySet();

	ATPropertySet& operator=(const ATPropertySet&);

	bool IsEmpty() const { return mProperties.empty(); }

	void Clear();

	template<class T>
	void EnumProperties(const T& functor) const;

	void EnumProperties(void (*fn)(const char *name, const ATPropertyValue& val, void *data), void *data) const;

	void Unset(const char *name);

	void SetBool(const char *name, bool val);
	void SetInt32(const char *name, sint32 val);
	void SetUint32(const char *name, uint32 val);
	void SetFloat(const char *name, float val);
	void SetDouble(const char *name, double val);
	void SetString(const char *name, const wchar_t *val);

	bool GetBool(const char *name, bool def = 0) const;
	sint32 GetInt32(const char *name, sint32 def = 0) const;
	uint32 GetUint32(const char *name, uint32 def = 0) const;
	float GetFloat(const char *name, float def = 0) const;
	double GetDouble(const char *name, double def = 0) const;
	const wchar_t *GetString(const char *name, const wchar_t *def = 0) const;

	bool TryGetBool(const char *name, bool& val) const;
	bool TryGetInt32(const char *name, sint32& val) const;
	bool TryGetUint32(const char *name, uint32& val) const;
	bool TryGetFloat(const char *name, float& val) const;
	bool TryGetDouble(const char *name, double& val) const;
	bool TryGetString(const char *name, const wchar_t *& val) const;

protected:
	const ATPropertyValue *GetProperty(const char *name) const;
	ATPropertyValue& CreateProperty(const char *name, ATPropertyType type);

	template<class T>
	static void EnumPropsAdapter(const char *name, const ATPropertyValue& val, void *data);

	typedef vdhashmap<const char *, ATPropertyValue, vdhash<VDStringA>, vdstringpred> Properties;
	Properties mProperties;
};

template<class T>
void ATPropertySet::EnumPropsAdapter(const char *name, const ATPropertyValue& val, void *data) {
	(*(T *)data)(name, val);
}

template<class T>
void ATPropertySet::EnumProperties(const T& functor) const {
	EnumProperties(EnumPropsAdapter<T>, (void *)&functor);
}

#endif
