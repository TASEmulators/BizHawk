//	Altirra - Atari 800/800XL emulator
//	Copyright (C) 2008 Avery Lee
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

#include <stdafx.h>
#include <vd2/system/VDString.h>
#include "decmath.h"
#include "cpu.h"
#include "cpumemory.h"
#include "ksyms.h"
#include "console.h"
#include "debuggerlog.h"

ATDebuggerLogChannel g_ATLCFPAccel(false, false, "FPACCEL", "Floating-point acceleration");

struct ATDecFloat {
	uint8	mSignExp;
	uint8	mMantissa[5];

	void SetZero();
	void SetOne();
	bool SetDouble(double d);

	ATDecFloat operator-() const;

	ATDecFloat Abs() const;

	VDStringA ToString() const;
	double ToDouble() const;
};

void ATDecFloat::SetZero() {
	mSignExp = 0;
	mMantissa[0] = 0;
	mMantissa[1] = 0;
	mMantissa[2] = 0;
	mMantissa[3] = 0;
	mMantissa[4] = 0;
}

void ATDecFloat::SetOne() {
	mSignExp = 0x40;
	mMantissa[0] = 0x01;
	mMantissa[1] = 0;
	mMantissa[2] = 0;
	mMantissa[3] = 0;
	mMantissa[4] = 0;
}

bool ATDecFloat::SetDouble(double v) {
	uint8 bias = 0x40;

	if (v < 0) {
		bias = 0xc0;
		v = -v;
	}

	if (v < 1e-98) {
		SetZero();
		return true;
	}

	static const double invln100 = 0.2171472409516259138255644594583025411471985029018332830572268916;
	double x = floor(log(v) * invln100);
	int ix = (int)x;
	double mantissa = v * pow(100.0, 4-x);

	// compensate for roundoff
	if (mantissa >= 10000000000.0) {
		mantissa /= 100.0;
		++ix;
	} else if (mantissa < 100000000.0) {
		mantissa *= 100.0;
		--ix;
	}

	// convert mantissa to integer (100000000 - 10000000000)
	sint64 imant64 = (sint64)(mantissa + 0.5);

	// renormalize if necessary after rounding
	if (imant64 == 10000000000) {
		imant64 = 100000000;
		++ix;
	}

	// check for underflow
	if (ix < -49) {
		SetZero();
		return true;
	}

	// check for overflow
	if (ix > 49)
		return false;

	// split mantissa into bytes
	uint8 rb[5];

	rb[0] = (uint8)(imant64 / 100000000);
	uint32 imant32 = (uint32)(imant64 % 100000000);

	rb[1] = imant32 / 1000000;
	imant32 %= 1000000;

	rb[2] = imant32 / 10000;
	imant32 %= 10000;

	rb[3] = imant32 / 100;
	imant32 %= 100;

	rb[4] = imant32;

	// convert mantissa to BCD
	for(int i=0; i<5; ++i)
		mMantissa[i] = (uint8)(((rb[i] / 10) << 4) + (rb[i] % 10));

	// encode exponent
	mSignExp = bias + ix;
	return true;
}

ATDecFloat ATDecFloat::operator-() const {
	ATDecFloat r(*this);

	if (r.mSignExp)
		r.mSignExp ^= 0x80;

	return r;
}

ATDecFloat ATDecFloat::Abs() const {
	ATDecFloat r(*this);

	r.mSignExp &= 0x7f;
	return r;
}

VDStringA ATDecFloat::ToString() const {
	char buf[18];
	char *dst = buf;

	if (!mSignExp || !mMantissa[0])
		*dst++ = '0';
	else {
		int exp = (mSignExp & 0x7f) * 2 - 0x80;

		if (mSignExp & 0x80)
			*dst++ = '-';

		if (mMantissa[0] >= 10) {
			*dst++ = '0' + (mMantissa[0] >> 4);
			*dst++ = '.';
			*dst++ = '0' + (mMantissa[0] & 15);
			++exp;
		} else {
			*dst++ = '0' + (mMantissa[0] & 15);
			*dst++ = '.';
		}

		for(int i=1; i<5; ++i) {
			int v = mMantissa[i];
			*dst++ = '0' + (v >> 4);
			*dst++ = '0' + (v & 15);
		}

		// cut off trailing zeroes
		while(dst[-1] == '0')
			--dst;

		// cut off trailing period
		if (dst[-1] == '.')
			--dst;

		// add exponent
		if (exp) {
			*dst++ = 'E';
			if (exp < 0) {
				*dst++ = '-';
				exp = -exp;
			} else {
				*dst++ = '+';
			}

			if (exp >= 100) {
				*dst++ = '1';
				exp -= 100;
			}

			*dst++ = '0' + (exp / 10);
			*dst++ = '0' + (exp % 10);
		}
	}

	return VDStringA(buf, dst);
}

double ATDecFloat::ToDouble() const {
	if (!mSignExp || !mMantissa[0])
		return 0.0;

	int exp = (mSignExp & 0x7f) - 0x40;
	double r = 0;

	for(int i=0; i<5; ++i) {
		int c = mMantissa[i];

		r = (r * 100.0) + (c >> 4)*10 + (c & 15);
	}

	r *= pow(100.0, (double)(exp - 4));

	if (mSignExp & 0x80)
		r = -r;

	return r;
}

bool operator<(const ATDecFloat& x, const ATDecFloat& y) {
	// check for sign difference
	if ((x.mSignExp ^ y.mSignExp) & 0x80)
		return x.mSignExp < 0x80;

	bool xlores = !(x.mSignExp & 0x80);
	bool ylores = !xlores;

	if (x.mSignExp < y.mSignExp)
		return xlores;
	if (x.mSignExp > y.mSignExp)
		return ylores;

	for(int i=0; i<5; ++i) {
		if (x.mMantissa[i] < y.mMantissa[i])
			return xlores;
		if (x.mMantissa[i] > y.mMantissa[i])
			return ylores;
	}

	// values are equal
	return false;
}

bool operator==(const ATDecFloat& x, const ATDecFloat& y) {
	return x.mSignExp == y.mSignExp
		&& x.mMantissa[0] == y.mMantissa[0]
		&& x.mMantissa[1] == y.mMantissa[1]
		&& x.mMantissa[2] == y.mMantissa[2]
		&& x.mMantissa[3] == y.mMantissa[3]
		&& x.mMantissa[4] == y.mMantissa[4];
}

bool operator>(const ATDecFloat& x, const ATDecFloat& y) {
	return y<x;
}

bool operator!=(const ATDecFloat& x, const ATDecFloat& y) {
	return !(x==y);
}

bool operator<=(const ATDecFloat& x, const ATDecFloat& y) {
	return !(x>y);
}

bool operator>=(const ATDecFloat& x, const ATDecFloat& y) {
	return !(x<y);
}

ATDecFloat ATReadDecFloat(ATCPUEmulatorMemory& mem, uint16 addr) {
	ATDecFloat v;

	v.mSignExp		= mem.ReadByte(addr);
	v.mMantissa[0]	= mem.ReadByte(addr+1);
	v.mMantissa[1]	= mem.ReadByte(addr+2);
	v.mMantissa[2]	= mem.ReadByte(addr+3);
	v.mMantissa[3]	= mem.ReadByte(addr+4);
	v.mMantissa[4]	= mem.ReadByte(addr+5);
	return v;
}

ATDecFloat ATDebugReadDecFloat(ATCPUEmulatorMemory& mem, uint16 addr) {
	ATDecFloat v;

	v.mSignExp		= mem.DebugReadByte(addr);
	v.mMantissa[0]	= mem.DebugReadByte(addr+1);
	v.mMantissa[1]	= mem.DebugReadByte(addr+2);
	v.mMantissa[2]	= mem.DebugReadByte(addr+3);
	v.mMantissa[3]	= mem.DebugReadByte(addr+4);
	v.mMantissa[4]	= mem.DebugReadByte(addr+5);
	return v;
}

void ATWriteDecFloat(ATCPUEmulatorMemory& mem, uint16 addr, const ATDecFloat& v) {
	mem.WriteByte(addr, v.mSignExp);
	mem.WriteByte(addr+1, v.mMantissa[0]);
	mem.WriteByte(addr+2, v.mMantissa[1]);
	mem.WriteByte(addr+3, v.mMantissa[2]);
	mem.WriteByte(addr+4, v.mMantissa[3]);
	mem.WriteByte(addr+5, v.mMantissa[4]);
}

ATDecFloat ATReadFR0(ATCPUEmulatorMemory& mem) {
	return ATReadDecFloat(mem, ATKernelSymbols::FR0);
}

ATDecFloat ATReadFR1(ATCPUEmulatorMemory& mem) {
	return ATReadDecFloat(mem, ATKernelSymbols::FR1);
}

void ATWriteFR0(ATCPUEmulatorMemory& mem, const ATDecFloat& x) {
	return ATWriteDecFloat(mem, ATKernelSymbols::FR0, x);
}

double ATDebugReadDecFloatAsBinary(ATCPUEmulatorMemory& mem, uint16 addr) {
	return ATDebugReadDecFloat(mem, addr).ToDouble();
}

double ATReadDecFloatAsBinary(ATCPUEmulatorMemory& mem, uint16 addr) {
	return ATReadDecFloat(mem, addr).ToDouble();
}

double ATReadDecFloatAsBinary(const uint8 bytes[6]) {
	ATDecFloat v;

	v.mSignExp		= bytes[0];
	v.mMantissa[0]	= bytes[1];
	v.mMantissa[1]	= bytes[2];
	v.mMantissa[2]	= bytes[3];
	v.mMantissa[3]	= bytes[4];
	v.mMantissa[4]	= bytes[5];

	return v.ToDouble();
}

///////////////////////////////////////////////////////////////////////////////

void ATAccelAFP(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem) {
	uint16 buffer = mem.ReadByte(ATKernelSymbols::INBUFF) + ((uint16)mem.ReadByte(ATKernelSymbols::INBUFF+1) << 8);
	uint8 index = mem.ReadByte(ATKernelSymbols::CIX);

	// skip leading spaces
	while(mem.ReadByte(buffer+index) == ' ') {
		++index;
		if (!index) {
			cpu.SetFlagC();
			g_ATLCFPAccel("AFP -> error\n");
			return;
		}
	}

	// check for a minus sign
	uint8 bias = 0x40;
	switch(mem.ReadByte(buffer+index)) {
		case '-':
			bias = 0xc0;
			// fall through
		case '+':
			++index;
			break;
	}

	// count number of leading digits
	ATDecFloat v;
	v.SetZero();

	int digits = 0;
	int leading = 0;
	bool period = false;
	bool nonzero = false;
	bool anydigits = false;
	for(;;) {
		uint8 c = mem.ReadByte(buffer+index);

		if (c == '.') {
			if (period)
				break;

			period = true;
		} else if ((uint32)(c-'0') < 10) {
			anydigits = true;

			if (c != '0')
				nonzero = true;

			if (nonzero) {
				if (!period)
					++leading;

				if (digits < 10) {
					int mantIndex = digits >> 1;

					if (digits & 1)
						v.mMantissa[mantIndex] += (c - '0');
					else
						v.mMantissa[mantIndex] += (c - '0') << 4;

					++digits;
				}
			} else if (period)
				--leading;
		} else
			break;

		// we need to check for wrapping to prevent an infinite loop, since this is HLE
		if (!++index) {
			mem.WriteByte(ATKernelSymbols::CIX, index);
			cpu.SetFlagC();
			g_ATLCFPAccel("AFP -> error\n");
			return;
		}
	}

	// if we couldn't get any digits, it's an error
	if (!anydigits) {
		mem.WriteByte(ATKernelSymbols::CIX, index);
		cpu.SetFlagC();
		g_ATLCFPAccel("AFP -> error\n");
		return;
	}

	// check for exponential notation -- note that this must be an uppercase E
	uint8 c = mem.ReadByte(buffer+index);
	if (c == 'E') {
		int index0 = index;

		// check for sign
		++index;

		c = mem.ReadByte(buffer+index);
		bool negexp = false;
		if (c == '+' || c == '-') {
			if (c == '-')
				negexp = true;

			++index;
			c = mem.ReadByte(buffer+index);
		}

		// check for first digit -- note if this fails, it is NOT an error; we
		// need to roll back to the E
		uint8 xd = c - '0';
		if (xd >= 10) {
			index = index0;
		} else {
			int exp = xd;

			++index;
			c = mem.ReadByte(buffer+index);
			uint8 xd2 = c - '0';
			if (xd2 < 10) {
				exp = exp*10+xd2;
				++index;
			}

			// zero is not a valid exponent
			if (!exp) {
				index = index0;
			} else {
				if (negexp)
					exp = -exp;

				leading += exp;
			}
		}
	}

	if (v.mMantissa[0]) {
		if (leading & 1) {
			v.mMantissa[4] = (v.mMantissa[4] >> 4) + (v.mMantissa[3] << 4);
			v.mMantissa[3] = (v.mMantissa[3] >> 4) + (v.mMantissa[2] << 4);
			v.mMantissa[2] = (v.mMantissa[2] >> 4) + (v.mMantissa[1] << 4);
			v.mMantissa[1] = (v.mMantissa[1] >> 4) + (v.mMantissa[0] << 4);
			v.mMantissa[0] = (v.mMantissa[0] >> 4);
		}

		int exponent100 = ((leading - 1) >> 1);

		if (exponent100 <= -49) {
			// underflow
			v.SetZero();
		} else if (exponent100 >= 49) {
			// overflow
			cpu.SetFlagC();
			g_ATLCFPAccel("AFP -> error\n");
			return;
		} else {
			v.mSignExp = exponent100 + bias;
		}
	}

	ATWriteFR0(mem, v);

	mem.WriteByte(ATKernelSymbols::CIX, index);
	cpu.ClearFlagC();

	if (g_ATLCFPAccel.IsEnabled())
		g_ATLCFPAccel("AFP -> %s\n", v.ToString().c_str());
}

void ATAccelFASC(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem) {
	// Some test cases:
	// 1
	// -1
	// 10
	// 1000000000
	// 1E+10
	// 1.0E+11
	// 1E+12
	// 0.01
	// 1.0E-03
	// 1E-04
	// 0.11
	// 0.011
	// 1.1E-03
	// 1.1E-04
	// 1.1E-05

	char buf[21];
	char *s = buf;
	const ATDecFloat v(ATReadFR0(mem));

	if (!v.mSignExp || !v.mMantissa[0])
		*s++ = '0';
	else {
		if (v.mSignExp & 0x80)
			*s++ = '-';

		int exp = ((v.mSignExp & 0x7f) - 0x40)*2;
		int expodd = 0;

		if (exp == -2) {
			*s++ = '0';
			*s++ = '.';
		}

		if (v.mMantissa[0] >= 16)
			expodd = 1;

		if (expodd || exp == -2) {
			*s++ = '0' + (v.mMantissa[0] >> 4);
		}

		if ((exp >= 10 || exp < -2) && expodd)
			*s++ = '.';

		*s++ = '0' + (v.mMantissa[0] & 15);

		if ((exp >= 10 || exp < -2) && !expodd)
			*s++ = '.';

		for(int i=1; i<5; ++i) {
			uint8 m = v.mMantissa[i];

			if (exp == i*2-2)
				*s++ = '.';

			*s++ = '0' + (m >> 4);
			*s++ = '0' + (m & 15);
		}

		int omittableDigits;

		if (exp < -2 || exp >= 10)
			omittableDigits = 8;
		else if (exp >= 0)
			omittableDigits = 8-exp;
		else
			omittableDigits = 8+expodd;

		for(int i=0; i<omittableDigits; ++i) {
			if (s[-1] != '0')
				break;
			--s;
		}

		if (s[-1] == '.')
			--s;

		exp += expodd;
		if (exp >= 10 || exp <= -3) {
			*s++ = 'E';
			*s++ = (exp < 0 ? '-' : '+');

			int absexp = abs(exp);
			*s++ = '0' + (absexp / 10);
			*s++ = '0' + (absexp % 10);
		}
	}

	mem.WriteByte(ATKernelSymbols::INBUFF, (uint8)ATKernelSymbols::LBUFF);
	mem.WriteByte(ATKernelSymbols::INBUFF+1, (uint8)(ATKernelSymbols::LBUFF >> 8));

	int len = (int)(s - buf);
	bool needPeriod = true;
	for(int i=0; i<len - 1; ++i) {
		uint8 c = buf[i];

		if (c == '.' || c == 'E')
			needPeriod = false;

		mem.WriteByte(ATKernelSymbols::LBUFF+i, c);
	}

	mem.WriteByte(ATKernelSymbols::LBUFF+len-1, (uint8)(s[-1] | 0x80));
	*s = 0;

	// SysInfo 2.19 looks for a period after non-zero numbers without checking the termination flag.
	if (needPeriod)
		mem.WriteByte(ATKernelSymbols::LBUFF+len, '.');

	if (g_ATLCFPAccel.IsEnabled())
		g_ATLCFPAccel("FASC(%s) -> %s\n", v.ToString().c_str(), buf);
}

void ATAccelIPF(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem) {
	const int value0 = mem.ReadByte(ATKernelSymbols::FR0) + ((uint32)mem.ReadByte(ATKernelSymbols::FR0+1) << 8);
	int value = value0;
	ATDecFloat r;

	if (!value) {
		r.SetZero();
	} else {
		r.mSignExp = 0x42;

		int d0 = value % 10;	value /= 10;
		int d1 = value % 10;	value /= 10;
		int d2 = value % 10;	value /= 10;
		int d3 = value % 10;	value /= 10;
		int d4 = value;

		uint8 d01 = (uint8)((d1 << 4) + d0);
		uint8 d23 = (uint8)((d3 << 4) + d2);
		uint8 d45 = (uint8)d4;

		while(!d45) {
			d45 = d23;
			d23 = d01;
			d01 = 0;
			--r.mSignExp;
		}

		r.mMantissa[0] = d45;
		r.mMantissa[1] = d23;
		r.mMantissa[2] = d01;
		r.mMantissa[3] = 0;
		r.mMantissa[4] = 0;
	}

	ATWriteFR0(mem, r);

	if (g_ATLCFPAccel.IsEnabled())
		g_ATLCFPAccel("IPF($%04X) -> %s\n", value0, r.ToString().c_str());
}

void ATAccelFPI(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem) {
	const ATDecFloat x = ATReadFR0(mem);

	// 40 01 00 00 00 00 = 1.0
	// 42 06 55 35 00 00 = 65535.5
	// 3F 50 00 00 00 00 = 0.5
	uint32 value = 0;

	uint8 exp = x.mSignExp;
	if (exp >= 0x43)
		value = 0x10000;
	else {
		if (exp >= 0x3F) {
			uint8 roundbyte = x.mMantissa[exp - 0x3F];
			
			for(int i=0; i<exp-0x3F; ++i) {
				value *= 100;

				const uint8 c = x.mMantissa[i];
				value += (c >> 4)*10 + (c & 15);
			}

			if (roundbyte >= 0x50)
				++value;
		}
	}

	if (value >= 0x10000) {
		cpu.SetFlagC();

		if (g_ATLCFPAccel.IsEnabled())
			g_ATLCFPAccel("FPI(%s) -> error\n", x.ToString().c_str());
	} else {
		mem.WriteByte(0xD4, (uint8)value);
		mem.WriteByte(0xD5, (uint8)(value >> 8));
		cpu.ClearFlagC();

		if (g_ATLCFPAccel.IsEnabled())
			g_ATLCFPAccel("FPI(%s) -> $%04X\n", x.ToString().c_str(), value);
	}
}

bool ATDecFloatAdd(ATDecFloat& dst, const ATDecFloat& x, const ATDecFloat& y) {
	// Extract exponents
	int xexp = x.mSignExp & 0x7f;
	int yexp = y.mSignExp & 0x7f;

	// Make sure x is larger in magnitude
	if (x.Abs() < y.Abs())
		return ATDecFloatAdd(dst, y, x);

	// Check for y=0.
	if (!y.mSignExp) {
		dst = x;
		return true;
	}

	// Denormalize y.
	ATDecFloat z(y);
	int expdiff = xexp - yexp;
	uint32 round = 0;
	if (expdiff) {
		if (expdiff > 5) {
			dst = x;
			return true;
		}

		// shift 
		while(expdiff--) {
			// keep sticky bit for rounding
			if (round && z.mMantissa[4] == 0x50)
				round = 0x51;
			else
				round = z.mMantissa[4];

			z.mMantissa[4] = z.mMantissa[3];
			z.mMantissa[3] = z.mMantissa[2];
			z.mMantissa[2] = z.mMantissa[1];
			z.mMantissa[1] = z.mMantissa[0];
			z.mMantissa[0] = 0;
		}
	}

	// Set mantissa.
	dst.mSignExp = x.mSignExp;

	// Check if we need to add or subtract.
	if ((x.mSignExp ^ y.mSignExp) & 0x80) {
		// subtract
		uint32 borrow = 0;

		if (round > 0x50 || (round == 0x50 && (z.mMantissa[4] & 0x01)))
			borrow = 1;

		for(int i=4; i>=0; --i) {
			sint32 lo = ((sint32)x.mMantissa[i] & 0x0f) - ((sint32)z.mMantissa[i] & 0x0f) - borrow;
			sint32 hi = ((sint32)x.mMantissa[i] & 0xf0) - ((sint32)z.mMantissa[i] & 0xf0);

			if (lo < 0) {
				lo += 10;
				hi -= 0x10;
			}

			borrow = 0;
			if (hi < 0) {
				hi += 0xA0;
				borrow = 1;
			}

			dst.mMantissa[i] = (uint8)(lo + hi);
		}

		// a borrow out isn't possible
		VDASSERT(!borrow);

		// renormalize as necessary
		for(int i=0; i<5; ++i) {
			if (dst.mMantissa[0])
				break;

			--dst.mSignExp;
			if ((dst.mSignExp & 0x7f) < 64-49) {
				dst.SetZero();
				return true;
			}

			dst.mMantissa[0] = dst.mMantissa[1];
			dst.mMantissa[1] = dst.mMantissa[2];
			dst.mMantissa[2] = dst.mMantissa[3];
			dst.mMantissa[3] = dst.mMantissa[4];
			dst.mMantissa[4] = 0;
		}

		// check for zero
		if (!dst.mMantissa[0])
			dst.mSignExp = 0;
	} else {
		// add
		uint32 carry = 0;

		if (round > 0x50 || (round == 0x50 && (z.mMantissa[4] & 0x01)))
			carry = 1;

		for(int i=4; i>=0; --i) {
			uint32 lo = ((uint32)x.mMantissa[i] & 0x0f) + ((uint32)z.mMantissa[i] & 0x0f) + carry;
			uint32 hi = ((uint32)x.mMantissa[i] & 0xf0) + ((uint32)z.mMantissa[i] & 0xf0);

			if (lo >= 10) {
				lo -= 10;
				hi += 0x10;
			}

			carry = 0;
			if (hi >= 0xA0) {
				hi -= 0xA0;
				carry = 1;
			}

			dst.mMantissa[i] = (uint8)(lo + hi);
		}

		// if we had a carry out, we need to renormalize again
		if (carry) {
			++dst.mSignExp;

			// check for overflow
			if ((dst.mSignExp & 0x7f) > 48+64)
				return false;

			// determine if we need to round up
			uint32 carry2 = 0;
			if (dst.mMantissa[4] > 0x50 || (dst.mMantissa[4] == 0x50 && (dst.mMantissa[3] & 0x01)))
				carry2 = 1;

			// renormalize
			for(int i=3; i>=0; --i) {
				uint32 r = dst.mMantissa[i] + carry2;

				if ((r & 0x0f) >= 0x0A)
					r += 0x06;
				if ((r & 0xf0) >= 0xA0)
					r += 0x60;

				carry2 = r >> 8;
				dst.mMantissa[i+1] = (uint8)r;
			}

			// Unlike base 2 FP, it isn't possible for this to require another renormalize.
			dst.mMantissa[0] = carry + carry2;
		}
	}

	return true;
}

bool ATDecFloatMul(ATDecFloat& dst, const ATDecFloat& x, const ATDecFloat& y) {
	// compute new exponent
	uint8 sign = (x.mSignExp^y.mSignExp) & 0x80;
	sint32 exp = (uint32)(x.mSignExp & 0x7f) + (uint32)(y.mSignExp & 0x7f) - 0x80;

	// convert both mantissae to binary
	int xb[5];
	int yb[5];

	for(int i=0; i<5; ++i) {
		int xm = x.mMantissa[i];
		int ym = y.mMantissa[i];
		xb[i] = ((xm & 0xf0) >> 4)*10 + (xm & 0x0f);
		yb[i] = ((ym & 0xf0) >> 4)*10 + (ym & 0x0f);
	}

	// compute result
	int rb[10] = {0};

	for(int i=0; i<5; ++i) {
		int xbi = xb[i];
		for(int j=0; j<5; ++j)
			rb[i+j] += xbi * yb[j];
	}

	// propagate carries
	int carry = 0;
	for(int i=9; i>=0; --i) {
		rb[i] += carry;
		carry = rb[i] / 100;
		rb[i] %= 100;
	}

	// determine rounding constant
	bool sticky = false;
	if (rb[6] | rb[7] | rb[8] | rb[9])
		sticky = true;

	// shift if necessary
	if (carry) {
		++exp;

		if (rb[5])
			sticky = true;

		rb[5] = rb[4];
		rb[4] = rb[3];
		rb[3] = rb[2];
		rb[2] = rb[1];
		rb[1] = rb[0];
		rb[0] = carry;
	}

	// check if we need to round up
	if (rb[5] > 50 || (rb[5] == 50 && sticky)) {
		if (++rb[4] >= 100) {
			rb[4] = 0;
			if (++rb[3] >= 100) {
				rb[3] = 0;
				if (++rb[2] >= 100) {
					rb[2] = 0;
					if (++rb[1] >= 100) {
						rb[1] = 0;
						if (++rb[0] >= 100) {
							rb[0] = 1;

							++exp;
						}
					}
				}
			}
		}
	}

	// check for underflow
	if (exp < -49) {
		dst.SetZero();
		return true;
	}

	// check for overflow
	if (exp > 49)
		return false;

	// convert digits back to BCD
	for(int i=0; i<5; ++i) {
		int rbi = rb[i];
		dst.mMantissa[i] = (uint8)(((rbi/10) << 4) + (rbi % 10));
	}

	// encode exponent
	dst.mSignExp = (uint8)(sign + exp + 0x40);

	return true;
}

bool ATDecFloatDiv(ATDecFloat& dst, const ATDecFloat& x, const ATDecFloat& y) {
	// check for zero divisor
	if (!y.mSignExp || !y.mMantissa[0])
		return false;

	// check for zero dividend
	if (!x.mSignExp || !x.mMantissa[0]) {
		dst.SetZero();
		return true;
	}

	// compute new exponent
	uint8 sign = (x.mSignExp^y.mSignExp) & 0x80;
	sint32 exp = (uint32)(x.mSignExp & 0x7f) - (uint32)(y.mSignExp & 0x7f);

	// convert both mantissae to binary
	uint64 xb = 0;
	uint64 yb = 0;

	for(int i=0; i<5; ++i) {
		int xm = x.mMantissa[i];
		int ym = y.mMantissa[i];

		xb = (xb * 100) + ((xm & 0xf0) >> 4)*10 + (xm & 0x0f);
		yb = (yb * 100) + ((ym & 0xf0) >> 4)*10 + (ym & 0x0f);
	}

	// do division
	xb *= 10000;
	uint32 v1 = (uint32)(xb / yb);

	xb = (xb % yb) * 1000000;
	uint32 v2 = (uint32)(xb / yb);
	
	bool sticky = (xb % yb) != 0;

	// split digits to base 100
	uint8 rb[6];
	rb[0] = v1 / 10000;		v1 %= 10000;
	rb[1] = v1 / 100;		v1 %= 100;
	rb[2] = v1;
	rb[3] = v2 / 10000;		v2 %= 10000;
	rb[4] = v2 / 100;		v2 %= 100;
	rb[5] = v2;

	// check if we need to renormalize
	if (!rb[0]) {
		rb[0] = rb[1];
		rb[1] = rb[2];
		rb[2] = rb[3];
		rb[3] = rb[4];
		rb[4] = rb[5];
		rb[5] = 0;
		--exp;
	}

	// discard lowest mantissa byte and update rounder
	int rounder = (rb[5] - 50);
	if (!rounder && sticky)
		rounder = 1;
	
	// check if we need to round up
	if (rounder > 0 || (rounder == 0 && sticky)) {
		if (++rb[4] >= 100) {
			rb[4] = 0;
			if (++rb[3] >= 100) {
				rb[3] = 0;
				if (++rb[2] >= 100) {
					rb[2] = 0;
					if (++rb[1] >= 100) {
						rb[1] = 0;
						if (++rb[0] >= 100) {
							rb[0] = 1;
							++exp;
						}
					}
				}
			}
		}
	}

	// convert digits back to BCD
	for(int i=0; i<5; ++i) {
		int rbi = rb[i];
		dst.mMantissa[i] = (uint8)(((rbi/10) << 4) + (rbi % 10));
	}

	// check for underflow or overflow
	if (exp < -49) {
		dst.SetZero();
		return true;
	}

	if (exp > 49)
		return false;

	// encode exponent
	dst.mSignExp = (uint8)(sign + exp + 0x40);

	return true;
}

void ATAccelFADD(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem) {
	ATDecFloat fp0(ATReadFR0(mem));
	ATDecFloat fp1(ATReadFR1(mem));
	ATDecFloat fpr;

	if (ATDecFloatAdd(fpr, fp0, fp1)) {
		double r0 = fp0.ToDouble();
		double r1 = fp1.ToDouble();
		double rr = fpr.ToDouble();

		if (fabs(r0) > 1e-5 && fabs(r1) > 1e-5) {
			if (r0 > r1) {
				VDASSERT((rr - (r0 + r1)) / r0 < 1e-5);
			} else {
				VDASSERT((rr - (r0 + r1)) / r1 < 1e-5);
			}
		}

		if (g_ATLCFPAccel.IsEnabled())
			g_ATLCFPAccel("FADD(%s, %s) -> %s\n", fp0.ToString().c_str(), fp1.ToString().c_str(), fpr.ToString().c_str());

		ATWriteFR0(mem, fpr);
		cpu.ClearFlagC();
	} else {
		cpu.SetFlagC();

		if (g_ATLCFPAccel.IsEnabled())
			g_ATLCFPAccel("FADD(%s, %s) -> error\n", fp0.ToString().c_str(), fp1.ToString().c_str());
	}
}

void ATAccelFSUB(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem) {
	ATDecFloat fp0(ATReadFR0(mem));
	ATDecFloat fp1(ATReadFR1(mem));
	ATDecFloat fpr;

	if (ATDecFloatAdd(fpr, fp0, -fp1)) {
		ATWriteFR0(mem, fpr);
		cpu.ClearFlagC();

		if (g_ATLCFPAccel.IsEnabled())
			g_ATLCFPAccel("FSUB(%s, %s) -> %s\n", fp0.ToString().c_str(), fp1.ToString().c_str(), fpr.ToString().c_str());
	} else {
		cpu.SetFlagC();

		if (g_ATLCFPAccel.IsEnabled())
			g_ATLCFPAccel("FSUB(%s, %s) -> error\n", fp0.ToString().c_str(), fp1.ToString().c_str());
	}
}

void ATAccelFMUL(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem) {
	ATDecFloat fp0(ATReadFR0(mem));
	ATDecFloat fp1(ATReadFR1(mem));
	ATDecFloat fpr;

	if (ATDecFloatMul(fpr, fp0, fp1)) {
		ATWriteFR0(mem, fpr);
		cpu.ClearFlagC();

		if (g_ATLCFPAccel.IsEnabled())
			g_ATLCFPAccel("FMUL(%s, %s) -> %s\n", fp0.ToString().c_str(), fp1.ToString().c_str(), fpr.ToString().c_str());
	} else {
		cpu.SetFlagC();

		if (g_ATLCFPAccel.IsEnabled())
			g_ATLCFPAccel("FMUL(%s, %s) -> error\n", fp0.ToString().c_str(), fp1.ToString().c_str());
	}
}

void ATAccelFDIV(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem) {
	ATDecFloat fp0(ATReadFR0(mem));
	ATDecFloat fp1(ATReadFR1(mem));
	ATDecFloat fpr;

	if (ATDecFloatDiv(fpr, fp0, fp1)) {
		ATWriteFR0(mem, fpr);
		cpu.ClearFlagC();

		if (g_ATLCFPAccel.IsEnabled())
			g_ATLCFPAccel("FDIV(%s, %s) -> %s\n", fp0.ToString().c_str(), fp1.ToString().c_str(), fpr.ToString().c_str());
	} else {
		cpu.SetFlagC();

		if (g_ATLCFPAccel.IsEnabled())
			g_ATLCFPAccel("FDIV(%s, %s) -> error\n", fp0.ToString().c_str(), fp1.ToString().c_str());
	}
}

void ATAccelLOG(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem) {
	const ATDecFloat x0 = ATReadFR0(mem);
	double x = x0.ToDouble();
	if (x < 0.0) {
		cpu.SetFlagC();
		return;
	}

	double r = log(x);
	ATDecFloat fpr;
	if (!fpr.SetDouble(r)) {
		if (g_ATLCFPAccel.IsEnabled())
			g_ATLCFPAccel("LOG(%s) -> error\n", x0.ToString().c_str());

		cpu.SetFlagC();
		return;
	}

	if (g_ATLCFPAccel.IsEnabled())
		g_ATLCFPAccel("LOG(%s) -> %s\n", x0.ToString().c_str(), fpr.ToString().c_str());

	ATWriteFR0(mem, fpr);
	cpu.ClearFlagC();
}

void ATAccelLOG10(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem) {
	const ATDecFloat x0 = ATReadFR0(mem);
	double x = x0.ToDouble();
	if (x < 0.0) {
		cpu.SetFlagC();
		return;
	}

	double r = log10(x);
	ATDecFloat fpr;
	if (!fpr.SetDouble(r)) {
		if (g_ATLCFPAccel.IsEnabled())
			g_ATLCFPAccel("LOG10(%s) -> error\n", x0.ToString().c_str());

		cpu.SetFlagC();
		return;
	}

	if (g_ATLCFPAccel.IsEnabled())
		g_ATLCFPAccel("LOG10(%s) -> %s\n", x0.ToString().c_str(), fpr.ToString().c_str());

	ATWriteFR0(mem, fpr);
	cpu.ClearFlagC();
}

void ATAccelEXP(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem) {
	const ATDecFloat x0 = ATReadFR0(mem);
	double x = x0.ToDouble();

	double r = exp(x);
	if (r == HUGE_VAL) {
		g_ATLCFPAccel("EXP(%s) -> error\n", x0.ToString().c_str());
		cpu.SetFlagC();
		return;
	}

	ATDecFloat fpr;
	if (!fpr.SetDouble(r)) {
		g_ATLCFPAccel("EXP(%s) -> error\n", x0.ToString().c_str());
		cpu.SetFlagC();
		return;
	}

	g_ATLCFPAccel("EXP(%s) -> %s\n", x0.ToString().c_str(), fpr.ToString().c_str());
	ATWriteFR0(mem, fpr);
	cpu.ClearFlagC();
}

void ATAccelEXP10(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem) {
	const ATDecFloat x0 = ATReadFR0(mem);
	double x = x0.ToDouble();

	double r = pow(10.0, x);
	ATDecFloat fpr;
	if (!fpr.SetDouble(r)) {
		g_ATLCFPAccel("EXP10(%s) -> error\n", x0.ToString().c_str());
		cpu.SetFlagC();
		return;
	}

	g_ATLCFPAccel("EXP10(%s) -> %s\n", x0.ToString().c_str(), fpr.ToString().c_str());
	ATWriteFR0(mem, fpr);
	cpu.ClearFlagC();
}

void ATAccelSKPSPC(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem) {
	uint16 buffer = mem.ReadByte(ATKernelSymbols::INBUFF) + ((uint16)mem.ReadByte(ATKernelSymbols::INBUFF+1) << 8);
	uint8 index = mem.ReadByte(ATKernelSymbols::CIX);
	uint8 ch;

	for(;;) {
		ch = mem.ReadByte(buffer + index);
		
		if (ch != ' ')
			break;

		++index;
		if (!index)
			break;
	}

	mem.WriteByte(ATKernelSymbols::CIX, index);
	cpu.SetY(index);
}

void ATAccelISDIGT(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem) {
	uint16 buffer = mem.ReadByte(ATKernelSymbols::INBUFF) + ((uint16)mem.ReadByte(ATKernelSymbols::INBUFF+1) << 8);
	uint8 index = mem.ReadByte(ATKernelSymbols::CIX);

	uint8 c = mem.ReadByte(buffer + index);
	if ((uint8)(c - '0') >= 10)
		cpu.SetFlagC();
	else
		cpu.ClearFlagC();

	cpu.SetA((uint8)(c - '0'));
	cpu.SetY(index);
}

void ATAccelNORMALIZE(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem) {
	ATDecFloat fr0(ATReadFR0(mem));
	int count = 0;

	while(count < 5 && !fr0.mMantissa[count])
		++count;

	if (count) {
		if (count >= 5)
			fr0.SetZero();
		else {
			for(int i=0; i<5-count; ++i)
				fr0.mMantissa[i] = fr0.mMantissa[i+count];

			for(int i=5-count; i<5; ++i)
				fr0.mMantissa[5-i] = 0;

			if ((fr0.mSignExp & 0x7f) < 64 - 49 + count)
				fr0.SetZero();
			else
				fr0.mSignExp -= count;
		}

		ATWriteFR0(mem, fr0);
	}

	cpu.ClearFlagC();
}

void ATAccelPLYEVL(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem) {
	const uint16 addr0 = ((uint16)cpu.GetY() << 8) + cpu.GetX();
	const uint8 coeffs0 = cpu.GetA();
	uint16 addr = addr0;
	uint8 coeffs = coeffs0;

	ATDecFloat z(ATReadFR0(mem));
	ATDecFloat accum;
	ATDecFloat t;

	accum.SetZero();

	for(;;) {
		if (!ATDecFloatAdd(t, accum, ATReadDecFloat(mem, addr))) {
			cpu.SetFlagC();
			return;
		}

		addr += 6;

		if (!--coeffs)
			break;

		if (!ATDecFloatMul(accum, t, z)) {
			cpu.SetFlagC();
			if (g_ATLCFPAccel.IsEnabled())
				g_ATLCFPAccel("PLYEVL(%s,$%04X,%u) -> error\n", z.ToString().c_str(), addr0, coeffs0);
			return;
		}
	}

	ATWriteFR0(mem, t);
	cpu.ClearFlagC();

	if (g_ATLCFPAccel.IsEnabled())
		g_ATLCFPAccel("PLYEVL(%s,$%04X,%u) -> %s\n", z.ToString().c_str(), addr0, coeffs0, t.ToString().c_str());
}

void ATAccelZFR0(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem) {
	ATDecFloat z;
	z.SetZero();
	ATWriteFR0(mem, z);

	// Note: must preserve C for Basic XE compatibility.
	cpu.SetA(0);
	cpu.SetX((uint8)(ATKernelSymbols::FR0 + 6));
	cpu.Ldy(0);

	g_ATLCFPAccel("ZFR0\n");
}

void ATAccelZF1(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem) {
	uint8 addr = cpu.GetX();

	for(int i=0; i<6; ++i)
		mem.WriteByte(addr++, 0);

	cpu.SetA(0);
	cpu.SetX(addr);
	cpu.Ldy(0);

	g_ATLCFPAccel("ZF1\n");
}

void ATAccelZFL(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem) {
	uint8 addr = cpu.GetX();
	uint8 len = cpu.GetY();

	do {
		mem.WriteByte(addr++, 0);
	} while(--len);

	cpu.SetA(0);
	cpu.SetX(addr);
	cpu.Ldy(0);

	g_ATLCFPAccel("ZFL\n");
}

void ATAccelLDBUFA(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem) {
	mem.WriteByte(ATKernelSymbols::INBUFF+1, (uint8)(ATKernelSymbols::LBUFF >> 8));
	mem.WriteByte(ATKernelSymbols::INBUFF, (uint8)ATKernelSymbols::LBUFF);
}

void ATAccelFLD0R(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem) {
	mem.WriteByte(ATKernelSymbols::FLPTR+1, cpu.GetY());
	mem.WriteByte(ATKernelSymbols::FLPTR, cpu.GetX());
	ATAccelFLD0P(cpu, mem);
}

void ATAccelFLD0P(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem) {
	const uint16 addr = ((uint16)mem.ReadByte(ATKernelSymbols::FLPTR+1) << 8) + mem.ReadByte(ATKernelSymbols::FLPTR);
	const ATDecFloat x = ATReadDecFloat(mem, addr);

	ATWriteFR0(mem, x);

	cpu.SetY(0);
	cpu.SetA(x.mSignExp);
	cpu.SetP((cpu.GetP() & ~AT6502::kFlagZ) | AT6502::kFlagN);

	if (g_ATLCFPAccel.IsEnabled())
		g_ATLCFPAccel("FLD0P($%04X) -> %s\n", addr, x.ToString().c_str());
}

void ATAccelFLD1R(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem) {
	mem.WriteByte(ATKernelSymbols::FLPTR+1, cpu.GetY());
	mem.WriteByte(ATKernelSymbols::FLPTR, cpu.GetX());
	ATAccelFLD1P(cpu, mem);
}

void ATAccelFLD1P(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem) {
	const uint16 addr = ((uint16)mem.ReadByte(ATKernelSymbols::FLPTR+1) << 8) + mem.ReadByte(ATKernelSymbols::FLPTR);

	const ATDecFloat x = ATReadDecFloat(mem, addr);

	ATWriteDecFloat(mem, ATKernelSymbols::FR1, x);

	cpu.SetY(0);
	cpu.SetA(mem.ReadByte(ATKernelSymbols::FR1));

	// This is critical for Atari Basic to work, even though it's not guaranteed.
	cpu.SetP((cpu.GetP() & ~AT6502::kFlagZ) | AT6502::kFlagN);

	if (g_ATLCFPAccel.IsEnabled())
		g_ATLCFPAccel("FLD1P($%04X) -> %s\n", addr, x.ToString().c_str());
}

void ATAccelFST0R(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem) {
	mem.WriteByte(ATKernelSymbols::FLPTR+1, cpu.GetY());
	mem.WriteByte(ATKernelSymbols::FLPTR, cpu.GetX());
	ATAccelFST0P(cpu, mem);
}

void ATAccelFST0P(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem) {
	uint16 addr = ((uint16)mem.ReadByte(ATKernelSymbols::FLPTR+1) << 8) + mem.ReadByte(ATKernelSymbols::FLPTR);

	for(int i=0; i<6; ++i)
		mem.WriteByte(addr+i, mem.ReadByte(ATKernelSymbols::FR0+i));
}

void ATAccelFMOVE(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem) {
	ATDecFloat x = ATReadFR0(mem);

	ATWriteDecFloat(mem, ATKernelSymbols::FR1, x);

	if (g_ATLCFPAccel.IsEnabled())
		g_ATLCFPAccel("FMOVE(%s)\n", x.ToString().c_str());
}

void ATAccelREDRNG(ATCPUEmulator& cpu, ATCPUEmulatorMemory& mem) {
	ATDecFloat one;
	one.SetOne();

	ATDecFloat x = ATReadFR0(mem);
	ATDecFloat y = ATReadDecFloat(mem, cpu.GetY()*256U + cpu.GetX());

	ATDecFloat num;
	ATDecFloat den;
	ATDecFloat res;

	cpu.ClearFlagC();
	if (!ATDecFloatAdd(num, x, -y) ||
		!ATDecFloatAdd(den, x, y) ||
		!ATDecFloatDiv(res, num, den)) {
		cpu.SetFlagC();
	} else {
		ATWriteFR0(mem, res);
	}
}
