//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2008-2012 Avery Lee
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

#ifndef f_AT_CALLBACK_H
#define f_AT_CALLBACK_H

#include <vd2/system/vdstl.h>

template<typename T_Return>
struct ATCallbackHandler0 {
	T_Return (*mpFn)(void *);
	void *mpData;

	void operator()() const {
		mpFn(mpData);
	}

	operator bool() const {
		return mpFn != NULL;
	}
};

template<typename T_Return, typename T_Arg1>
struct ATCallbackHandler1 {
	T_Return (*mpFn)(void *, T_Arg1 arg1);
	void *mpData;

	void operator()(T_Arg1 arg1) const {
		mpFn(mpData, arg1);
	}

	operator bool() const {
		return mpFn != NULL;
	}
};

template<typename T_Return, typename T_Arg1, typename T_Arg2>
struct ATCallbackHandler2 {
	T_Return (*mpFn)(void *, T_Arg1 arg1, T_Arg2 arg2);
	void *mpData;

	void operator()(T_Arg1 arg1, T_Arg2 arg2) const {
		mpFn(mpData, arg1, arg2);
	}

	operator bool() const {
		return mpFn != NULL;
	}
};

template<typename T, typename T_Return>
struct ATCallbackBinder0 {
	template<T_Return (T::*T_Method)()>
	static T_Return Handler(void *data) {
		(((T *)data)->*T_Method)();
	}

	template<T_Return (T::*T_Method)()>
	static ATCallbackHandler0<T_Return> Bind(T *thisptr) {
		const ATCallbackHandler0<T_Return> h0 = { Handler<T_Method>, thisptr };

		return h0;
	}
};

template<typename T, typename T_Return, typename T_Arg1>
struct ATCallbackBinder1 {
	template<T_Return (T::*T_Method)(T_Arg1)>
	static T_Return Handler(void *data, T_Arg1 arg1) {
		(((T *)data)->*T_Method)(arg1);
	}

	template<T_Return (T::*T_Method)(T_Arg1)>
	static ATCallbackHandler1<T_Return, T_Arg1> Bind(T *thisptr) {
		const ATCallbackHandler1<T_Return, T_Arg1> h = { Handler<T_Method>, thisptr };

		return h;
	}
};

template<typename T, typename T_Return, typename T_Arg1, typename T_Arg2>
struct ATCallbackBinder2 {
	template<T_Return (T::*T_Method)(T_Arg1, T_Arg2)>
	static T_Return Handler(void *data, T_Arg1 arg1, T_Arg2 arg2) {
		(((T *)data)->*T_Method)(arg1, arg2);
	}

	template<T_Return (T::*T_Method)(T_Arg1, T_Arg2)>
	static ATCallbackHandler2<T_Return, T_Arg1, T_Arg2> Bind(T *thisptr) {
		const ATCallbackHandler2<T_Return, T_Arg1, T_Arg2> h = { Handler<T_Method>, thisptr };

		return h;
	}
};

template<class T, typename T_Return>
ATCallbackBinder0<T, T_Return> ATMakeCallbackHandler(T *thisptr, T_Return (T::*method)()) {
	return ATCallbackBinder0<T, T_Return>();
}

template<class T, typename T_Return, typename T_Arg1>
ATCallbackBinder1<T, T_Return, T_Arg1> ATMakeCallbackHandler(T *thisptr, T_Return (T::*method)(T_Arg1)) {
	return ATCallbackBinder1<T, T_Return, T_Arg1>();
}

template<class T, typename T_Return, typename T_Arg1, typename T_Arg2>
ATCallbackBinder2<T, T_Return, T_Arg1, T_Arg2> ATMakeCallbackHandler(T *thisptr, T_Return (T::*method)(T_Arg1, T_Arg2)) {
	return ATCallbackBinder2<T, T_Return, T_Arg1, T_Arg2>();
}

///////////////////////////////////////////////////////////////////////////

template<typename T_Return>
ATCallbackHandler0<T_Return> ATMakeCallbackHandlerFn(T_Return (*fn)(), void *data = 0) {
	ATCallbackHandler0<T_Return> h = { fn, data };

	return h;
}

template<typename T_Return, typename T_Arg1>
ATCallbackHandler1<T_Return, T_Arg1> ATMakeCallbackHandlerFn(T_Return (*fn)(T_Arg1), void *data = 0) {
	ATCallbackHandler1<T_Return, T_Arg1> h = { fn, data };

	return h;
}

template<typename T_Return, typename T_Arg1, typename T_Arg2>
ATCallbackHandler2<T_Return, T_Arg1, T_Arg2> ATMakeCallbackHandlerFn(T_Return (*fn)(T_Arg1, T_Arg2), void *data = 0) {
	ATCallbackHandler2<T_Return, T_Arg1, T_Arg2> h = { fn, data };

	return h;
}

///////////////////////////////////////////////////////////////////////////

#define ATBINDCALLBACK(thisptr, method) (ATMakeCallbackHandler(thisptr, method).Bind<method>(thisptr))

#endif
