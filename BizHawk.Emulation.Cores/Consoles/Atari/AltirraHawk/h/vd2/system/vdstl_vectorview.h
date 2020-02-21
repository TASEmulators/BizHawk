//	VirtualDub - Video processing and capture application
//	System library component
//	Copyright (C) 1998-2007 Avery Lee, All Rights Reserved.
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

#ifndef VD2_SYSTEM_VDSTL_VECTORVIEW_H
#define VD2_SYSTEM_VDSTL_VECTORVIEW_H

#include <iterator>
#include <type_traits>

template<typename T>
class vdvector_view {
public:
	typedef T&					reference;
	typedef const T&			const_reference;
	typedef T*					iterator;
	typedef const T*			const_iterator;
	typedef size_t				size_type;
	typedef ptrdiff_t			difference_type;
	typedef T					value_type;
	typedef T*					pointer;
	typedef const T*			const_pointer;
	typedef std::reverse_iterator<iterator>			reverse_iterator;
	typedef std::reverse_iterator<const_iterator>	const_reverse_iterator;

	constexpr vdvector_view() : mpBegin(nullptr), mSize(0) {}
	constexpr vdvector_view(T *p, size_t n) : mpBegin(p), mSize(n) {}

	constexpr vdvector_view(const vdvector_view<std::remove_const_t<T>>& v)
		: mpBegin(v.data())
		, mSize(v.size())
	{
	}

	// do not make this constexpr -- 15.7.3/15.8.0p3 has broken constexpr array arithmetic
	template<typename U>
	vdvector_view(const U& v)
		: mpBegin(&*std::begin(v))
		, mSize(size_type(std::end(v) - std::begin(v)))
	{

	}

	bool			empty() const { return !mSize; }
	size_type		size() const { return mSize; }

	iterator				begin() const { return mpBegin; }
	const_iterator			cbegin() const { return mpBegin; }
	reverse_iterator		rbegin() const { return reverse_iterator(mpBegin + mSize); }
	const_reverse_iterator	crbegin() const { return const_reverse_iterator(mpBegin + mSize); }

	iterator				end() const { return mpBegin + mSize; }
	const_iterator			cend() const { return mpBegin + mSize; }
	reverse_iterator		rend() const { return reverse_iterator(mpBegin); }
	const_reverse_iterator	crend() const { return const_reverse_iterator(mpBegin); }

	reference		front() const { return *mpBegin; }
	const_reference	cfront() const { return *mpBegin; }

	reference		back() const { return mpBegin[mSize - 1]; }
	const_reference	cback() const { return mpBegin[mSize - 1]; }

	reference		operator[](size_type i) const { return mpBegin[i]; }
	pointer			data() const { return mpBegin; }

private:
	T *mpBegin;
	size_t mSize;
};

#endif
