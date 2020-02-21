//	Altirra - Atari 800/800XL/5200 emulator
//	Filter kernel routines
//	Copyright (C) 2009-2011 Avery Lee
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

#ifndef f_AT_ARTIFACTING_FILTERS_H
#define f_AT_ARTIFACTING_FILTERS_H

#include <vd2/system/vdstl.h>

struct ATFilterKernel {
	typedef vdfastvector<float> Coeffs;
	int mOffset;
	Coeffs mCoeffs;

	struct Initer {
		ATFilterKernel& k;

		Initer(ATFilterKernel& k2) : k(k2) {}

		Initer& operator=(float f) {
			k.mCoeffs.push_back(f);
			return *this;
		}

		Initer& operator,(float f) {
			k.mCoeffs.push_back(f);
			return *this;
		}
	};

	Initer Init(int off) {
		mOffset = off;

		return Initer(*this);
	}

	ATFilterKernel operator-() const {
		ATFilterKernel r(*this);

		for(Coeffs::iterator it(r.mCoeffs.begin()), itEnd(r.mCoeffs.end()); it != itEnd; ++it)
			*it = -*it;

		return r;
	}

	ATFilterKernel operator<<(int offset) const {
		ATFilterKernel r(*this);

		r.mOffset -= offset;
		return r;
	}

	ATFilterKernel operator>>(int offset) const {
		ATFilterKernel r(*this);

		r.mOffset += offset;
		return r;
	}

	inline ATFilterKernel& operator+=(const ATFilterKernel& src);
	inline ATFilterKernel& operator-=(const ATFilterKernel& src);
	inline ATFilterKernel& operator*=(float scale);
	inline ATFilterKernel& operator*=(const ATFilterKernel& src);

	inline ATFilterKernel trim() const;
};

inline void ATFilterKernelSetBicubic(ATFilterKernel& k, float offset, float A) {
	const float t = offset;
	const float t2 = t*t;
	const float t3 = t2*t;

	const float c1 =     + A*t -        2.0f*A*t2 +        A*t3;
	const float c2 = 1.0f      -      (A+3.0f)*t2 + (A+2.0f)*t3;
	const float c3 =     - A*t + (2.0f*A+3.0f)*t2 - (A+2.0f)*t3;
	const float c4 =           +             A*t2 -        A*t3;

	k.mCoeffs.resize(4);
	k.mCoeffs[0] = c1;
	k.mCoeffs[1] = c2;
	k.mCoeffs[2] = c3;
	k.mCoeffs[3] = c4;
	k.mOffset = -1;
}

inline void ATFilterKernelConvolve(ATFilterKernel& r, const ATFilterKernel& x, const ATFilterKernel& y) {
	r.mOffset = x.mOffset + y.mOffset;

	size_t m = x.mCoeffs.size();
	size_t n = y.mCoeffs.size();

	r.mCoeffs.clear();
	r.mCoeffs.resize(m + n - 1, 0);

	float *dst = r.mCoeffs.data();

	for(size_t i = 0; i < m; ++i) {
		float s = x.mCoeffs[i];

		for(size_t j = 0; j < n; ++j) {
			dst[j] += s * y.mCoeffs[j];
		}

		++dst;
	}
}

inline void ATFilterKernelReverse(ATFilterKernel& r) {
	const int n = (int)r.mCoeffs.size();

	r.mOffset = -(r.mOffset + n - 1);

	const int n2 = n >> 1;

	float *x = r.mCoeffs.data();
	float *y = x + (n - 1);

	for(int i=0; i<n2; ++i) {
		std::swap(*x, *y);
		++x;
		--y;
	}
}

inline float ATFilterKernelEvaluate(const ATFilterKernel& k, const float *src) {
	src += k.mOffset;

	ATFilterKernel::Coeffs::const_iterator it(k.mCoeffs.begin()), itEnd(k.mCoeffs.end());
	float sum = 0;

	do {
		sum += *src++ * *it;

		++it;
	} while(it != itEnd);

	return sum;
}

inline void ATFilterKernelAccumulate(const ATFilterKernel& k, float *dst) {
	dst += k.mOffset;

	ATFilterKernel::Coeffs::const_iterator it(k.mCoeffs.begin()), itEnd(k.mCoeffs.end());

	while(it != itEnd) {
		*dst++ += *it;

		++it;
	}
}

inline void ATFilterKernelAccumulateSub(const ATFilterKernel& k, float *dst) {
	dst += k.mOffset;

	ATFilterKernel::Coeffs::const_iterator it(k.mCoeffs.begin()), itEnd(k.mCoeffs.end());

	while(it != itEnd) {
		*dst++ -= *it;

		++it;
	}
}

inline void ATFilterKernelAccumulate(const ATFilterKernel& k, float *dst, float scale) {
	dst += k.mOffset;

	ATFilterKernel::Coeffs::const_iterator it(k.mCoeffs.begin()), itEnd(k.mCoeffs.end());

	while(it != itEnd) {
		*dst++ += *it * scale;

		++it;
	}
}

inline void ATFilterKernelAccumulateWindow(const ATFilterKernel& k, float *dst, int offset, int limit, float scale) {
	int start = k.mOffset + offset;
	int end = start + (int)k.mCoeffs.size();
	int lo = start;
	int hi = end;

	if (lo < 0)
		lo = 0;

	if (hi > limit)
		hi = limit;

	const float *src = k.mCoeffs.data();

#ifdef _DEBUG
	for(int i=start; i<lo; ++i) {
		VDASSERT(fabsf(src[i - start]) < 1e-4f);
	}

	for(int i=hi; i<end; ++i) {
		VDASSERT(fabsf(src[i - start]) < 1e-4f);
	}
#endif

	src += (lo - start);

	int n = hi - lo;
	for(int i = 0; i < n; ++i)
		dst[lo + i] += src[i] * scale;
}

inline ATFilterKernel& ATFilterKernel::operator*=(float scale) {
	for(Coeffs::iterator it(mCoeffs.begin()), itEnd(mCoeffs.end()); it != itEnd; ++it)
		*it *= scale;

	return *this;
}

inline ATFilterKernel& ATFilterKernel::operator*=(const ATFilterKernel& src) {
	ATFilterKernel tmp;

	ATFilterKernelConvolve(tmp, *this, src);

	tmp.mCoeffs.swap(mCoeffs);
	mOffset = tmp.mOffset;

	return *this;
}

inline ATFilterKernel operator~(const ATFilterKernel& src) {
	ATFilterKernel r(src);

	ATFilterKernelReverse(r);
	return r;
}

inline ATFilterKernel operator*(const ATFilterKernel& x, const ATFilterKernel& y) {
	ATFilterKernel r(x);

	r *= y;

	return r;
}

inline ATFilterKernel operator*(const ATFilterKernel& src, float scale) {
	ATFilterKernel r(src);

	r.mCoeffs.resize(src.mCoeffs.size());

	ATFilterKernel::Coeffs::iterator itDst(r.mCoeffs.begin());
	for(ATFilterKernel::Coeffs::const_iterator it(src.mCoeffs.begin()), itEnd(src.mCoeffs.end());
		it != itEnd;
		++it, ++itDst)
	{
		*itDst = *it * scale;
	}

	return r;
}

inline ATFilterKernel operator+(const ATFilterKernel& x, const ATFilterKernel& y) {
	ATFilterKernel r;

	r.mOffset = std::min(x.mOffset, y.mOffset);

	size_t m = x.mCoeffs.size();
	size_t n = y.mCoeffs.size();
	r.mCoeffs.resize(std::max(x.mOffset + (int)m, y.mOffset + (int)n) - r.mOffset, 0);

	float *dst = r.mCoeffs.data();
	const float *src = y.mCoeffs.data();

	memcpy(dst + (x.mOffset - r.mOffset), x.mCoeffs.data(), m * sizeof(float));

	dst += (y.mOffset - r.mOffset);
	for(size_t i = 0; i < n; ++i)
		dst[i] += src[i];

	return r;
}

inline ATFilterKernel operator-(const ATFilterKernel& x, const ATFilterKernel& y) {
	ATFilterKernel r;

	r.mOffset = std::min(x.mOffset, y.mOffset);

	size_t m = x.mCoeffs.size();
	size_t n = y.mCoeffs.size();
	r.mCoeffs.resize(std::max(x.mOffset + (int)m, y.mOffset + (int)n) - r.mOffset, 0);

	float *dst = r.mCoeffs.data();
	const float *src = y.mCoeffs.data();

	memcpy(dst + (x.mOffset - r.mOffset), x.mCoeffs.data(), m * sizeof(float));

	dst += (y.mOffset - r.mOffset);
	for(size_t i = 0; i < n; ++i)
		dst[i] -= src[i];

	return r;
}

inline ATFilterKernel& ATFilterKernel::operator+=(const ATFilterKernel& x) {
	ATFilterKernel r(*this + x);

	mCoeffs.swap(r.mCoeffs);
	mOffset = r.mOffset;

	return *this;
}

inline ATFilterKernel& ATFilterKernel::operator-=(const ATFilterKernel& x) {
	ATFilterKernel r(*this - x);

	mCoeffs.swap(r.mCoeffs);
	mOffset = r.mOffset;

	return *this;
}

inline ATFilterKernel ATFilterKernel::trim() const {
	Coeffs::const_iterator itBegin(mCoeffs.begin());
	Coeffs::const_iterator it(itBegin);
	Coeffs::const_iterator itEnd(mCoeffs.end());

	while(it != itEnd && fabsf(*it) < 1e-4f)
		++it;

	while(itEnd != it && fabsf(itEnd[-1]) < 1e-4f)
		--itEnd;

	ATFilterKernel r;
	r.mOffset = mOffset + (int)(it - itBegin);
	r.mCoeffs.assign(it, itEnd);

	return r;
}

inline ATFilterKernel operator^(const ATFilterKernel& x, const ATFilterKernel& y) {
	ATFilterKernel r(x);

	size_t n = y.mCoeffs.size();
	int off = y.mOffset;

	while(off > x.mOffset)
		off -= (int)n;

	while(off + (int)n <= x.mOffset)
		off += (int)n;

	VDASSERT(off <= x.mOffset);
	VDASSERT(x.mOffset - off < (int)n);

	ATFilterKernel::Coeffs::const_iterator itMod1(y.mCoeffs.begin());
	ATFilterKernel::Coeffs::const_iterator itMod2(y.mCoeffs.end());
	ATFilterKernel::Coeffs::const_iterator itMod(itMod1 + (x.mOffset - off));

	ATFilterKernel::Coeffs::iterator itDst1 = r.mCoeffs.begin();
	ATFilterKernel::Coeffs::iterator itDst2 = r.mCoeffs.end();

	for(; itDst1 != itDst2; ++itDst1) {
		*itDst1 *= *itMod;

		if (++itMod == itMod2)
			itMod = itMod1;
	}

	return r;
}

inline void ATFilterKernelEvalCubic4(float co[4], float offset, float A) {
	const float t = offset;
	const float t2 = t*t;
	const float t3 = t2*t;

	co[0] =     + A*t -        2.0f*A*t2 +        A*t3;
	co[1] = 1.0f      -      (A+3.0f)*t2 + (A+2.0f)*t3;
	co[2] =     - A*t + (2.0f*A+3.0f)*t2 - (A+2.0f)*t3;
	co[3] =           +             A*t2 -        A*t3;
}

inline ATFilterKernel ATFilterKernelSampleBicubic(const ATFilterKernel& src, float offset, float step, float A) {
	const int lo = src.mOffset;
	const int hi = src.mOffset + (int)src.mCoeffs.size();

	float fstart = ceil(((float)lo - 3.0f - offset) / step);
	float flimit = (float)hi;
	int istart = (int)fstart;

	ATFilterKernel r;
	r.mOffset = istart;

	const float *srcd = src.mCoeffs.data();
	float co[4];

	float fpos = fstart * step + offset;
	while(fpos < flimit) {
		float fposf = floorf(fpos);
		int ipos = (int)fposf;

		ATFilterKernelEvalCubic4(co, fpos - fposf, A);

		float sum = 0;

		int iend = ipos + 4;
		if (ipos >= lo) {
			int ilen = 4;

			if (iend > hi)
				ilen = hi - ipos;

			for(int i=0; i<ilen; ++i)
				sum += co[i] * srcd[(ipos - lo) + i];
		} else if (iend >= lo) {
			if (iend > hi)
				iend = hi;

			for(int i=lo; i<iend; ++i)
				sum += co[i - ipos] * srcd[i - lo];
		}

		VDASSERT(sum >= -10000 && sum <= 10000);
		r.mCoeffs.push_back(sum);

		fpos += step;
	}

	return r;
}

inline ATFilterKernel ATFilterKernelSamplePoint(const ATFilterKernel& src, int offset, int step) {
	ATFilterKernel r;

	int delta = src.mOffset - offset;

	r.mOffset = delta / step;

	int pos = -delta % step;

	if (pos < 0) {
		++r.mOffset;
		pos += step;
	}

	int n = (int)src.mCoeffs.size();
	while(pos < n) {
		r.mCoeffs.push_back(src.mCoeffs[pos]);

		pos += step;
	}

	if (r.mCoeffs.empty())
		r.mCoeffs.push_back(0);

	return r;
}

#endif
