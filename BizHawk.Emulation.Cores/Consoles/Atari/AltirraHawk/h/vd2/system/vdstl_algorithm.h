//	VirtualDub - Video processing and capture application
//	System library component
//	Copyright (C) 1998-2017 Avery Lee, All Rights Reserved.
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

#ifndef f_VD2_SYSTEM_VDSTL_ALGORITHM_H
#define f_VD2_SYSTEM_VDSTL_ALGORITHM_H

#include <algorithm>

template<class Iterator, class T>
typename std::iterator_traits<Iterator>::difference_type vdfind_index(Iterator it1, Iterator it2, const T& value) {
	auto it = std::find(it1, it2, value);

	return it == it2 ? -1 : std::distance(it1, it);
}

template<class Iterator, class Pred>
typename std::iterator_traits<Iterator>::difference_type vdfind_index_if(Iterator it1, Iterator it2, Pred p) {
	auto it = std::find_if(it1, it2, p);

	return it == it2 ? -1 : std::distance(it1, it);
}

template<class Range, class T>
auto vdfind_index_r(const Range& r, const T& value) -> typename std::iterator_traits<decltype(std::begin(r))>::difference_type {
	return vdfind_index(std::begin(r), std::end(r), value);
}

template<class Range, class Pred>
auto vdfind_index_if_r(const Range& r, Pred p) -> typename std::iterator_traits<decltype(std::begin(r))>::difference_type {
	return vdfind_index_if(std::begin(r), std::end(r), p);
}

#endif
