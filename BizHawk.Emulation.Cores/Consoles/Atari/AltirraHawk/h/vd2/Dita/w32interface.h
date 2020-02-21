#ifndef f_VD2_DITA_W32INTERFACE_H
#define f_VD2_DITA_W32INTERFACE_H

#include <vd2/system/unknown.h>

class IVDUIWindowW32 : public IVDUnknown {
public:
	enum { kTypeID = 'uw32' };

	virtual HWND GetHandleW32() const = 0;
};

#endif
