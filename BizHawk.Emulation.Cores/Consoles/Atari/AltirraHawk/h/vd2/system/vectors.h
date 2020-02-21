//	VirtualDub - Video processing and capture application
//	System library component
//	Copyright (C) 1998-2004 Avery Lee, All Rights Reserved.
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

#ifndef f_VD2_SYSTEM_VECTORS_H
#define f_VD2_SYSTEM_VECTORS_H

#ifdef _MSC_VER
	#pragma once
#endif

#include <vd2/system/vdtypes.h>
#include <vd2/system/math.h>
#include <math.h>
#include <limits>

///////////////////////////////////////////////////////////////////////////

bool VDSolveLinearEquation(double *src, int n, ptrdiff_t stride_elements, double *b, double tolerance = 1e-5);

///////////////////////////////////////////////////////////////////////////

#include <vd2/system/vectors_float.h>
#include <vd2/system/vectors_int.h>

///////////////////////////////////////////////////////////////////////////

class vdfloat2x2 {
public:
	static vdfloat2x2 zero() {
		return vdfloat2x2 {{0,0}, {0,0}};
	}

	static vdfloat2x2 identity() {
		return vdfloat2x2 {{1,0}, {0,1}};
	}

	static vdfloat2x2 rotation(float angle) {
		const float s(sinf(angle));
		const float c(cosf(angle));

		return vdfloat2x2 {
			{ c,-s },
			{ s, c }
		};
	}

	vdfloat2x2 operator*(const vdfloat2x2& v) const {
		return vdfloat2x2 {
#define DO(i,j) i.x*v.x.j + i.y*v.y.j
			{ DO(x,x), DO(x,y) },
			{ DO(y,x), DO(y,y) }
#undef DO
		};
	}

	vdfloat2 operator*(const vdfloat2& r) const {
		return vdfloat2 {
				x.x*r.x + x.y*r.y,
				y.x*r.x + y.y*r.y };
	}

	vdfloat2x2 transpose() const {
		return vdfloat2x2 {
			vdfloat2 { x.x, y.x },
			vdfloat2 { x.y, y.y }
		};
	}

	vdfloat2x2 adjunct() const {
		return vdfloat2x2 {
			vdfloat2 { y.y, -x.y },
			vdfloat2 { -y.x, x.x }
		};
	}

	float det() const {
		return x.x*y.y - x.y*y.x;
	}

	vdfloat2x2 operator~() const {
		return adjunct() / det();
	}

	vdfloat2x2& operator*=(float factor) {
		x *= factor;
		y *= factor;

		return *this;
	}

	vdfloat2x2 operator/=(float factor) {
		float inv = 1.0f / factor;
		x *= inv;
		y *= inv;
		return *this;
	}

	vdfloat2x2 operator*(float factor) const {
		return vdfloat2x2 { x * factor, y * factor };
	}

	vdfloat2x2 operator/(float factor) const {
		return vdfloat2x2 { x / factor, y / factor };
	}

	vdfloat2 x;
	vdfloat2 y;
};

class vdfloat3x3 {
public:
	static constexpr vdfloat3x3 zero() {
		return vdfloat3x3 {
			{ 0, 0, 0 },
			{ 0, 0, 0 },
			{ 0, 0, 0 }
		};
	}

	static constexpr vdfloat3x3 identity() {
		return vdfloat3x3 {
			{ 1, 0, 0 },
			{ 0, 1, 0 },
			{ 0, 0, 1 }
		};
	}

	static vdfloat3x3 rotation_x(float angle) {
		const float s(sinf(angle));
		const float c(cosf(angle));

		return vdfloat3x3 {
			{ 1, 0, 0 },
			{ 0, c,-s },
			{ 0, s, c }
		};
	}

	static vdfloat3x3 rotation_y_type(float angle) {
		const float s(sinf(angle));
		const float c(cosf(angle));

		return vdfloat3x3 {
			{ c, 0, s },
			{ 0, 1, 0 },
			{-s, 0, c }
		};
	}

	static vdfloat3x3 rotation_z_type(float angle) {
		const float s(sinf(angle));
		const float c(cosf(angle));

		return vdfloat3x3 {
			{ c,-s, 0 },
			{ s, c, 0 },
			{ 0, 0, 1 }
		};
	}

	constexpr vdfloat3x3 operator*(const vdfloat3x3& v) const {
		return vdfloat3x3 {
#define DO(i,j) i.x*v.x.j + i.y*v.y.j + i.z*v.z.j
		{ DO(x,x), DO(x,y), DO(x,z) },
		{ DO(y,x), DO(y,y), DO(y,z) },
		{ DO(z,x), DO(z,y), DO(z,z) }
#undef DO
		};
	}

	constexpr vdfloat3 operator*(const vdfloat3& r) const {
		return vdfloat3 {
				x.x*r.x + x.y*r.y + x.z*r.z,
				y.x*r.x + y.y*r.y + y.z*r.z,
				z.x*r.x + z.y*r.y + z.z*r.z };
	}

	constexpr vdfloat3x3 transpose() const {
		return vdfloat3x3 {
			{ x.x, y.x, z.x },
			{ x.y, y.y, z.y },
			{ x.z, y.z, z.z },
		};
	}

	constexpr vdfloat3x3 adjunct() const {
		using namespace nsVDMath;

		vdfloat3x3 res = {
			cross(y, z),
			cross(z, x),
			cross(x, y)
		};

		return res.transpose();
	}

	constexpr float det() const {
		return	+ x.x * y.y * z.z
				+ y.x * z.y * x.z
				+ z.x * x.y * y.z
				- x.x * z.y * y.z
				- y.x * x.y * z.z
				- z.x * y.y * x.z;
	}

	constexpr vdfloat3x3 operator~() const {
		return adjunct() / det();
	}

	constexpr vdfloat3x3& operator*=(float factor) {
		x *= factor;
		y *= factor;
		z *= factor;

		return *this;
	}

	constexpr vdfloat3x3& operator/=(float factor) {
		return operator*=(1.0f/factor);
	}

	constexpr vdfloat3x3 operator*(float factor) const {
		return vdfloat3x3 { x*factor, y*factor, z*factor };
	}

	constexpr vdfloat3x3 operator/(float factor) const {
		float inv = 1.0f / factor;
		return vdfloat3x3 { x*inv, y*inv, z*inv };
	}

	vdfloat3 x;
	vdfloat3 y;
	vdfloat3 z;
};

inline vdfloat3 operator*(const vdfloat3& v, const vdfloat3x3& m) {
	return v.x * m.x + v.y * m.y + v.z * m.z;
}

class vdfloat4x4 {
public:
	static vdfloat4x4 from3(const vdfloat3x3& v) {
		return vdfloat4x4 {
			{ v.x.x, v.x.y, v.x.z, 0.0f },
			{ v.y.x, v.y.y, v.y.z, 0.0f },
			{ v.z.x, v.z.y, v.z.z, 0.0f },
			{ 0, 0, 0, 1 }
		};
	}

	static vdfloat4x4 zero() {
		return vdfloat4x4 {
			{ 0, 0, 0, 0 },
			{ 0, 0, 0, 0 },
			{ 0, 0, 0, 0 },
			{ 0, 0, 0, 0 }
		};
	}

	static vdfloat4x4 identity() {
		return vdfloat4x4 {
			{ 1, 0, 0, 0 },
			{ 0, 1, 0, 0 },
			{ 0, 0, 1, 0 },
			{ 0, 0, 0, 1 },
		};
	}

	static vdfloat4x4 rotation_x(float angle) {
		const float s(sinf(angle));
		const float c(cosf(angle));

		return vdfloat4x4 {
			{ 1, 0, 0, 0 },
			{ 0, c,-s, 0 },
			{ 0, s, c, 0 },
			{ 0, 0, 0, 1 },
		};
	}

	static vdfloat4x4 rotation_y(float angle) {
		const float s(sinf(angle));
		const float c(cosf(angle));

		return vdfloat4x4 {
			{ c, 0, s, 0 },
			{ 0, 1, 0, 0 },
			{-s, 0, c, 0 },
			{ 0, 0, 0, 1 },
		};
	}

	static vdfloat4x4 rotation_z(float angle) {
		const float s(sinf(angle));
		const float c(cosf(angle));

		return vdfloat4x4 {
			{ c,-s, 0, 0 },
			{ s, c, 0, 0 },
			{ 0, 0, 1, 0 },
			{ 0, 0, 0, 1 },
		};
	}

	const float *data() const { return &x.x; }

	vdfloat4x4 operator*(const vdfloat4x4& v) const {
		return vdfloat4x4 {
#define DO(i,j) i.x*v.x.j + i.y*v.y.j + i.z*v.z.j + i.w*v.w.j
			{ DO(x,x), DO(x,y), DO(x,z), DO(x,w) },
			{ DO(y,x), DO(y,y), DO(y,z), DO(y,w) },
			{ DO(z,x), DO(z,y), DO(z,z), DO(z,w) },
			{ DO(w,x), DO(w,y), DO(w,z), DO(w,w) },
#undef DO
		};
	}

	vdfloat4x4& operator*=(const vdfloat4x4& v) {
		return operator=(operator*(v));
	}

	vdfloat4 operator*(const vdfloat4& r) const {
		return vdfloat4 {
				x.x*r.x + x.y*r.y + x.z*r.z + x.w*r.w,
				y.x*r.x + y.y*r.y + y.z*r.z + y.w*r.w,
				z.x*r.x + z.y*r.y + z.z*r.z + z.w*r.w,
				w.x*r.x + w.y*r.y + w.z*r.z + w.w*r.w };
	}

	vdfloat4 x;
	vdfloat4 y;
	vdfloat4 z;
	vdfloat4 w;
};

///////////////////////////////////////////////////////////////////////////

namespace nsVDMath {
	VDFORCEINLINE vdint2 intround(const vdfloat2& v) {
		return { VDRoundToInt(v.x), VDRoundToInt(v.y) };
	}

	VDFORCEINLINE vdint3 intround(const vdfloat3& v) {
		return { VDRoundToInt(v.x), VDRoundToInt(v.y), VDRoundToInt(v.z) };
	}

	VDFORCEINLINE vdint4 intround(const vdfloat4& v) {
		return { VDRoundToInt(v.x), VDRoundToInt(v.y), VDRoundToInt(v.z), VDRoundToInt(v.w) };
	}
}

///////////////////////////////////////////////////////////////////////////

template<class T>
struct VDSize {
	typedef T value_type;

	int w, h;

	VDSize() = default;
	VDSize(int _w, int _h) : w(_w), h(_h) {}

	bool operator==(const VDSize& s) const { return w==s.w && h==s.h; }
	bool operator!=(const VDSize& s) const { return w!=s.w || h!=s.h; }

	VDSize& operator+=(const VDSize& s) {
		w += s.w;
		h += s.h;
		return *this;
	}

	T area() const { return w*h; }

	void include(const VDSize& s) {
		if (w < s.w)
			w = s.w;
		if (h < s.h)
			h = s.h;
	}
};

///////////////////////////////////////////////////////////////////////////

template<class T>
class VDPoint {
public:
	VDPoint() = default;
	VDPoint(T x_, T y_);

	bool operator==(const VDPoint& pt) const;
	bool operator!=(const VDPoint& pt) const;

	T x;
	T y;
};

template<class T>
VDPoint<T>::VDPoint(T x_, T y_)
	: x(x_), y(y_)
{
}

template<class T>
bool VDPoint<T>::operator==(const VDPoint& pt) const {
	return x == pt.x && y == pt.y;
}

template<class T>
bool VDPoint<T>::operator!=(const VDPoint& pt) const {
	return x != pt.x || y != pt.y;
}

///////////////////////////////////////////////////////////////////////////

template<class T>
class VDRect {
public:
	typedef T value_type;

	VDRect() = default;
	VDRect(T left_, T top_, T right_, T bottom_);

	bool empty() const;
	bool valid() const;

	void clear();
	void invalidate();
	void set(T l, T t, T r, T b);

	void add(T x, T y);
	void add(const VDRect& r);
	void translate(T x, T y);
	void scale(T x, T y);
	void transform(T scaleX, T scaleY, T offsetX, T offsety);
	void resize(T w, T h);

	bool operator==(const VDRect& r) const;
	bool operator!=(const VDRect& r) const;

	T width() const;
	T height() const;
	T area() const;
	VDSize<T> size() const;
	VDPoint<T> top_left() const;
	VDPoint<T> bottom_right() const;

	bool contains(const VDPoint<T>& pt) const;

public:
	T left, top, right, bottom;
};

template<class T>
VDRect<T>::VDRect(T left_, T top_, T right_, T bottom_)
	: left(left_)
	, top(top_)
	, right(right_)
	, bottom(bottom_)
{
}

template<class T>
bool VDRect<T>::empty() const {
	return left >= right || top >= bottom;
}

template<class T>
bool VDRect<T>::valid() const {
	return left <= right;
}

template<class T>
void VDRect<T>::clear() {
	left = top = right = bottom = 0;
}

template<class T>
void VDRect<T>::invalidate() {
	left = top = (std::numeric_limits<T>::max)();
	right = bottom = std::numeric_limits<T>::is_signed ? -(std::numeric_limits<T>::max)() : T(0);
}

template<class T>
void VDRect<T>::set(T l, T t, T r, T b) {
	left = l;
	top = t;
	right = r;
	bottom = b;
}

template<class T>
void VDRect<T>::add(T x, T y) {
	if (left > x)
		left = x;
	if (top > y)
		top = y;
	if (right < x)
		right = x;
	if (bottom < y)
		bottom = y;
}

template<class T>
void VDRect<T>::add(const VDRect& src) {
	if (left > src.left)
		left = src.left;
	if (top > src.top)
		top = src.top;
	if (right < src.right)
		right = src.right;
	if (bottom < src.bottom)
		bottom = src.bottom;
}

template<class T>
void VDRect<T>::translate(T x, T y) {
	left += x;
	top += y;
	right += x;
	bottom += y;
}

template<class T>
void VDRect<T>::scale(T x, T y) {
	left *= x;
	top *= y;
	right *= x;
	bottom *= y;
}

template<class T>
void VDRect<T>::transform(T scaleX, T scaleY, T offsetX, T offsetY) {
	left	= left		* scaleX + offsetX;
	top		= top		* scaleY + offsetY;
	right	= right		* scaleX + offsetX;
	bottom	= bottom	* scaleY + offsetY;
}

template<class T>
void VDRect<T>::resize(T w, T h) {
	right = left + w;
	bottom = top + h;
}

template<class T>
bool VDRect<T>::operator==(const VDRect& r) const { return left==r.left && top==r.top && right==r.right && bottom==r.bottom; }

template<class T>
bool VDRect<T>::operator!=(const VDRect& r) const { return left!=r.left || top!=r.top || right!=r.right || bottom!=r.bottom; }

template<class T>
T VDRect<T>::width() const { return right-left; }

template<class T>
T VDRect<T>::height() const { return bottom-top; }

template<class T>
T VDRect<T>::area() const { return (right-left)*(bottom-top); }

template<class T>
VDPoint<T> VDRect<T>::top_left() const { return VDPoint<T>(left, top); }

template<class T>
VDPoint<T> VDRect<T>::bottom_right() const { return VDPoint<T>(right, bottom); }

template<class T>
VDSize<T> VDRect<T>::size() const { return VDSize<T>(right-left, bottom-top); }

template<class T>
bool VDRect<T>::contains(const VDPoint<T>& pt) const {
	return pt.x >= left
		&& pt.x < right
		&& pt.y >= top
		&& pt.y < bottom;
}

///////////////////////////////////////////////////////////////////////////////
typedef VDPoint<sint32>	vdpoint32;
typedef VDSize<sint32>	vdsize32;
typedef VDSize<float>	vdsize32f;
typedef	VDRect<sint32>	vdrect32;
typedef	VDRect<float>	vdrect32f;

template<> bool vdrect32::contains(const vdpoint32& pt) const;

#endif
