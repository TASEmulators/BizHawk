//	VirtualDub - Video processing and capture application
//	System library component
//	Copyright (C) 1998-2014 Avery Lee, All Rights Reserved.
//
//	Beginning with 1.6.0, the VirtualDub system library is licensed
//	differently than the remainder of VirtualDub.  This particular file is
//	thus licensed as follows (the "zlib" license):
//
//	This software is provided 'as-is', without any express or implied
//	warranty.  In no event will the authors be held liable for any
//	damages arising from the use of this software.
//
//	Permission is granted to anyone to use this software for any purpose,
//	including commercial applications, and to alter it and redistribute it
//	freely, subject to the following restrictions:
//
//	1.	The origin of this software must not be misrepresented; you must
//		not claim that you wrote the original software. If you use this
//		software in a product, an acknowledgment in the product
//		documentation would be appreciated but is not required.
//	2.	Altered source versions must be plainly marked as such, and must
//		not be misrepresented as being the original software.
//	3.	This notice may not be removed or altered from any source
//		distribution.

#ifndef f_VD2_SYSTEM_FUNCTION_H
#define f_VD2_SYSTEM_FUNCTION_H

#include <functional>
#include <cstddef>

#ifdef _MSC_VER
	#pragma warning(push)
	#pragma warning(disable: 4521 4522)	// multiple copy/assignment constructors defined
#endif

//////////////////////////////////////////////////////////////////////////////
// vdfunction
//
// vdfunction<T> is an implementation of std::function<T>. It mostly works
// the same way:
//
// + Supports small object optimization for up to 2*sizeof(void *).
// + Supports lambdas on VS2010.
// + Supports reference optimization when std::reference_wrapper used.
// + Fast dispatch: function pointer, no virtual calls.
// - target() and target_type() are not supported.
// - assign() is not supported.
// - bad_function_call is not supported -- calling through an unbound function
//   object invokes undefined behavior.
//
//////////////////////////////////////////////////////////////////////////////

class vdfuncbase;

struct vdfunctraits {
	void (*mpDestroy)(void *obj);
	void (*mpCopy)(void *dst, const void *src);
	void (*mpMove)(void *dst, void *src);
};

template<class F>
struct vdfunc_ti {
	static void destroy(void *dst);
	static void copy(void *dst, const void *src);
	static void move(void *dst, void *src);

	static const vdfunctraits sObject;
};

template<class F>
void vdfunc_ti<F>::destroy(void *dst) {
	((F *)dst)->~F();
}

template<class F>
void vdfunc_ti<F>::copy(void *dst, const void *src) {
	new(dst) F(*(F *)src);
}

template<class F>
void vdfunc_ti<F>::move(void *dst, void *src) {
	new(dst) F(std::move(*(F *)src));
	((F *)src)->~F();
}

template<class F>
const vdfunctraits vdfunc_ti<F>::sObject = { destroy, copy, move };

template<class F>
struct vdfunc_th {
	static void destroy(void *obj);
	static void copy(void *dst, const void *src);

	static const vdfunctraits sObject;
};

template<class F>
void vdfunc_th<F>::destroy(void *dst) {
	delete (F *)*(void **)dst;
}

template<class F>
void vdfunc_th<F>::copy(void *dst, const void *src) {
	*(void **)dst = new F(*(F *)*(void *const *)src);
}

template<class F>
const vdfunctraits vdfunc_th<F>::sObject = { destroy, copy, nullptr };

//////////////////////////////////////////////////////////////////////////////

template<class> class vdfunction;

//////////////////////////////////////////////////////////////////////////////

class vdfuncbase {
public:
	vdfuncbase() = default;
	vdfuncbase(const vdfuncbase&);
	inline vdfuncbase(vdfuncbase&&);
	inline ~vdfuncbase();

	vdfuncbase& operator=(const vdfuncbase&);
	vdfuncbase& operator=(vdfuncbase&&);

	inline operator bool() const;

	void rebind(const vdfuncbase& src) {
		operator=(src);
	}

	void rebind(vdfuncbase&& src) {
		operator=(std::forward<vdfuncbase>(src));
	}

protected:
	void swap(vdfuncbase& other);
	void clear();

public:
	void (*mpFn)() = nullptr;
	union Data {
		void *p[2];
		void (*fn)();
	} mData;
	const vdfunctraits *mpTraits = nullptr;
};

inline vdfuncbase::vdfuncbase(vdfuncbase&& src)
	: mpFn(src.mpFn)
	, mData(src.mData)
	, mpTraits(src.mpTraits)
{

	if (mpTraits && mpTraits->mpMove)
		mpTraits->mpMove(mData.p, src.mData.p);

	src.mpFn = nullptr;
	src.mpTraits = nullptr;
}

inline vdfuncbase::~vdfuncbase() {
	clear();
	if (mpTraits)
		mpTraits->mpDestroy(mData.p);
}

inline vdfuncbase::operator bool() const {
	return mpFn != nullptr;
}

inline bool operator==(const vdfuncbase& fb, std::nullptr_t) { return fb.mpFn == nullptr; }
inline bool operator==(std::nullptr_t, const vdfuncbase& fb) { return fb.mpFn == nullptr; }
inline bool operator!=(const vdfuncbase& fb, std::nullptr_t) { return fb.mpFn != nullptr; }
inline bool operator!=(std::nullptr_t, const vdfuncbase& fb) { return fb.mpFn != nullptr; }

//////////////////////////////////////////////////////////////////////////////

template<class T>
struct vdfunc_mode
	: public std::integral_constant<unsigned,
			sizeof(T) <= sizeof(vdfuncbase::Data) && std::alignment_of<void *>::value % std::alignment_of<T>::value == 0
				? std::is_pod<T>::value
					? 0
					: 1
				: 2
	> {};

template<unsigned char Mode> struct vdfunc_construct;

template<>
struct vdfunc_construct<0> {		// trivial
	template<class F>
	static void go(vdfuncbase& func, const F& f) {
		new(&func.mData) F(f);
	}
};

template<>
struct vdfunc_construct<1> {		// direct (requires copy/destruction)
	template<class F, class Arg>
	static void go(vdfuncbase& func, Arg&& f) {
		new(&func.mData) F(std::forward<Arg>(f));
		func.mpTraits = &vdfunc_ti<F>::sObject;
	}
};

template<>
struct vdfunc_construct<2> {		// indirect (uses heap)
	template<class F, class Arg>
	static void go(vdfuncbase& func, Arg&& f) {
		func.mData.p[0] = new F(std::forward<Arg>(f));
		func.mpTraits = &vdfunc_th<F>::sObject;
	}
};

//////////////////////////////////////////////////////////////////////////////

template<class R, class ...Args>
class vdfunction<R(Args...)> : public vdfuncbase {
public:
	typedef R result_type;

	vdfunction() = default;
	vdfunction(std::nullptr_t) {}
	vdfunction(vdfunction&& src) : vdfuncbase(static_cast<vdfuncbase&&>(src)) {}
	vdfunction(vdfunction& src) : vdfuncbase(src) {}	// needed to avoid invoking (F&&)
	vdfunction(const vdfunction& src) : vdfuncbase(src) {}
	template<class F> vdfunction(std::reference_wrapper<F> f);
	template<class F, typename = decltype(std::declval<F>()(std::declval<Args>()...))>
	vdfunction(F&& f);

	vdfunction& operator=(std::nullptr_t) { vdfuncbase::clear(); return *this; }

	vdfunction& operator=(vdfunction&& src) {
		vdfuncbase::operator=(static_cast<vdfuncbase&&>(src));
		return *this;
	}

	vdfunction& operator=(vdfunction& src) {
		vdfuncbase::operator=(src);
		return *this;
	}

	vdfunction& operator=(const vdfunction& src) {
		vdfuncbase::operator=(src);
		return *this;
	}

	template<class F, typename = decltype(std::declval<F>()(std::declval<Args>()...))>
	vdfunction& operator=(F&& f) {
		vdfuncbase::operator=(std::move(vdfunction(std::forward<F>(f))));
		return *this;
	}

	template<class F>
	vdfunction& operator=(std::reference_wrapper<F> f) { vdfunction(f).swap(*this); }

	void swap(vdfunction& other) {
		vdfuncbase::swap(other);
	}

	R operator()(Args... args) const {
		return reinterpret_cast<R (*)(const vdfuncbase *, Args...)>(mpFn)(this, std::forward<Args>(args)...);
	}
};

struct vdfunc_rd {
	template<class R, class F, class ...Args>
	static R go(const vdfuncbase *p, Args... args) {
		return (*(F *)&p->mData)(std::forward<Args>(args)...);
	}
};

struct vdfunc_ri {
	template<class R, class F, class ...Args>
	static R go(const vdfuncbase *p, Args... args) {
		return (*(F *)p->mData.p[0])(std::forward<Args>(args)...);
	}
};

template<class R, class... Args>
template<class F>
vdfunction<R(Args...)>::vdfunction(std::reference_wrapper<F> f) {
	auto fn = vdfunc_ri::go<R, F, Args...>;
	mpFn = reinterpret_cast<void (*)()>(fn);
	mData.p[0] = (void *)&f.get();
}

template<class R, class... Args>
template<class F, typename>
vdfunction<R(Args...)>::vdfunction(F&& f) {
	typedef decltype(f((*(typename std::remove_reference<Args>::type *)nullptr)...)) validity_test;
	(void)sizeof(validity_test*);

	typedef typename std::decay<F>::type BaseF;
	vdfunc_construct<vdfunc_mode<BaseF>::value>::template go<BaseF>(*this, std::forward<F>(f));

	// We may get invoked with F being a reference, which we must strip to
	// properly instantiate the worker templates.
	const auto fn = std::conditional<vdfunc_mode<BaseF>::value == 2, vdfunc_ri, vdfunc_rd>::type::template go<R, BaseF, Args...>;
	mpFn = reinterpret_cast<void (*)()>(fn);
}

//////////////////////////////////////////////////////////////////////////////

#ifdef _MSC_VER
	#pragma warning(pop)
#endif

#endif
