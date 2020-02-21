#include <stdafx.h>
#include <vd2/system/math.h>
#include <vd2/system/int128.h>
#include <vd2/system/fraction.h>
#include <test.h>

namespace {
	vduint128 rand128_64(vduint128 v) {
		return vduint128(v.getLo(), ~(v.getHi() ^ (uint64)(v >> (126 - 64)) ^ (uint64)(v >> (101 - 64)) ^ (uint64)(v >> (99 - 64))));
	}

	vduint128 rand128(vduint128 v) {
		return rand128_64(rand128_64(v));
	}

	vduint128 slowmul(vduint128 x, vduint128 y) {
		vdint128 shifter;
		shifter.q[0] = x.getLo();
		shifter.q[1] = x.getHi();
		vduint128 result(0);
		vdint128 zero(0);

		for(int i=0; i<128; ++i) {
			result += result;
			if (shifter < zero)
				result += y;
			shifter += shifter;
		}

		return result;
	}
}

DEFINE_TEST(System_Int128) {
	// addition (unsigned)
	TEST_ASSERT(	vduint128(0x0000000080000000, 0x8000000080000000)
				+	vduint128(0x0000000080000000, 0x8000000080000000)
				==	vduint128(0x0000000100000001, 0x0000000100000000));

	// addition (signed)
	TEST_ASSERT(	vdint128(0x0000000080000000, 0x8000000080000000)
				+	vdint128(0x0000000080000000, 0x8000000080000000)
				==	vdint128(0x0000000100000001, 0x0000000100000000));

	// subtraction (unsigned)
	TEST_ASSERT(	vduint128(0x0000000080000000, 0x8000000080000000)
				+	vduint128(0x0000000080000000, 0x8000000080000000)
				==	vduint128(0x0000000100000001, 0x0000000100000000));

	// subtraction (unsigned)
	TEST_ASSERT(	vduint128(0x0000000000000000, 0x0000000000000000)
				-	vduint128(0x0000000000000000, 0x0000000000000001)
				==	vduint128(0xFFFFFFFFFFFFFFFF, 0xFFFFFFFFFFFFFFFF));

	// subtraction (signed)
	TEST_ASSERT(	vdint128(0x0000000000000000, 0x0000000000000000)
				-	vdint128(0x0000000000000000, 0x0000000000000001)
				==	vdint128(0xFFFFFFFFFFFFFFFF, 0xFFFFFFFFFFFFFFFF));

	// multiplication (unsigned/signed)
	{
		vduint128 a(1);
		vduint128 b(1);
		vdint128 c(1);
		vdint128 d(1);

		for(int i=0; i<128; ++i) {
			TEST_ASSERT(a == (vduint128(1) << i));
			TEST_ASSERT(b == (vduint128(1) << i));
			TEST_ASSERT(c == (vdint128(1) << i));
			TEST_ASSERT(d == (vdint128(1) << i));

			a = a * vduint128(2);
			b = vduint128(2) * b;
			c = c * vdint128(2);
			d = vdint128(2) * d;
		}
	}

	// shift left (unsigned)
	for(int i=0; i<64; ++i) {
		auto a = vduint128(1) << i;
		TEST_ASSERT(a.getLo() == (UINT64_C(1) << i));
		TEST_ASSERT(a.getHi() == 0);

		auto b = vduint128(1) << (i + 64);
		TEST_ASSERT(b.getLo() == 0);
		TEST_ASSERT(b.getHi() == (UINT64_C(1) << i));
	}

	// shift left (signed)
	for(int i=0; i<64; ++i) {
		auto a = vdint128(1) << i;
		TEST_ASSERT(a.getLo() == (UINT64_C(1) << i));
		TEST_ASSERT(a.getHi() == 0);

		auto b = vdint128(1) << (i + 64);
		TEST_ASSERT(b.getLo() == 0);
		TEST_ASSERT(b.getHi() == (UINT64_C(1) << i));
	}

	// shift right (unsigned)
	for(int i=0; i<64; ++i) {
		auto a = vduint128(0x8000000000000000, 0) >> i;
		TEST_ASSERT(a.getLo() == 0);
		TEST_ASSERT(a.getHi() == (UINT64_C(0x8000000000000000) >> i));

		auto b = vduint128(0x8000000000000000, 0) >> (i + 64);
		TEST_ASSERT(b.getLo() == (UINT64_C(0x8000000000000000) >> i));
		TEST_ASSERT(b.getHi() == 0);
	}

	// shift right (signed)
	for(int i=0; i<64; ++i) {
		auto a = vdint128(0x8000000000000000, 0) >> i;
		TEST_ASSERT(a.getLo() == 0);
		TEST_ASSERT(a.getHi() == (INT64_C(0x8000000000000000) >> i));

		auto b = vdint128(0x8000000000000000, 0) >> (i + 64);
		TEST_ASSERT(b.getLo() == (INT64_C(0x8000000000000000) >> i));
		TEST_ASSERT(b.getHi() == -1);
	}

	// relops (unsigned)
	TEST_ASSERT( (vduint128(0, 0) == vduint128(0, 0)));
	TEST_ASSERT(!(vduint128(0, 0) != vduint128(0, 0)));
	TEST_ASSERT(!(vduint128(0, 0) == vduint128(0, 1)));
	TEST_ASSERT( (vduint128(0, 0) != vduint128(0, 1)));
	TEST_ASSERT(!(vduint128(0, 0) == vduint128(1, 0)));
	TEST_ASSERT( (vduint128(0, 0) != vduint128(1, 0)));
	TEST_ASSERT(!(vduint128(0, 1) == vduint128(0, 0)));
	TEST_ASSERT( (vduint128(0, 1) != vduint128(0, 0)));
	TEST_ASSERT(!(vduint128(1, 0) == vduint128(0, 0)));
	TEST_ASSERT( (vduint128(1, 0) != vduint128(0, 0)));

	TEST_ASSERT(!(vduint128(0, 0) <  vduint128(0, 0)));
	TEST_ASSERT( (vduint128(0, 0) <= vduint128(0, 0)));
	TEST_ASSERT(!(vduint128(0, 0) >  vduint128(0, 0)));
	TEST_ASSERT( (vduint128(0, 0) >= vduint128(0, 0)));

	TEST_ASSERT(!(vduint128(0, 1) <  vduint128(0, 0)));
	TEST_ASSERT(!(vduint128(0, 1) <= vduint128(0, 0)));
	TEST_ASSERT( (vduint128(0, 1) >  vduint128(0, 0)));
	TEST_ASSERT( (vduint128(0, 1) >= vduint128(0, 0)));

	TEST_ASSERT(!(vduint128(1, 0) <  vduint128(0, 2)));
	TEST_ASSERT(!(vduint128(1, 0) <= vduint128(0, 2)));
	TEST_ASSERT( (vduint128(1, 0) >  vduint128(0, 2)));
	TEST_ASSERT( (vduint128(1, 0) >= vduint128(0, 2)));

	// misc

	TEST_ASSERT(VDUMul64x64To128(1, 1) == vduint128(1));
	TEST_ASSERT(VDUMul64x64To128(3, 7) == vduint128(21));
	TEST_ASSERT(VDUMul64x64To128(0xFFFFFFFF, 0xFFFFFFFF) == vduint128(0xFFFFFFFE00000001));
	TEST_ASSERT(VDUMul64x64To128(0x123456789ABCDEF0, 0xBAADF00DDEADBEEF) == vduint128(0x0D4665441D7CFEBC, 0xD182EA976BFA4210));
	TEST_ASSERT(VDUMul64x64To128(0xFFFFFFFFFFFFFFFF, 0xFFFFFFFFFFFFFFFF) == vduint128(0xFFFFFFFFFFFFFFFE, 0x0000000000000001));

	uint64 quotient;
	uint64 remainder;
	TEST_ASSERT(((quotient = VDUDiv128x64To64(vduint128(21), 7, remainder)), quotient == 3 && remainder == 0));
	TEST_ASSERT(((quotient = VDUDiv128x64To64(vduint128(27), 7, remainder)), quotient == 3 && remainder == 6));
	TEST_ASSERT(((quotient = VDUDiv128x64To64(vduint128(0xFFFFFFFFFFFFFFFE, 0x0000000000000001), 0xFFFFFFFFFFFFFFFF, remainder)), quotient == 0xFFFFFFFFFFFFFFFF && remainder == 0));
	TEST_ASSERT(((quotient = VDUDiv128x64To64(vduint128(0xFFFFFFFFFFFFFFFF, 0x0000000000000000), 0xFFFFFFFFFFFFFFFF, remainder)), quotient == 0xFFFFFFFFFFFFFFFF && remainder == 0xFFFFFFFFFFFFFFFF));
	TEST_ASSERT(((quotient = VDUDiv128x64To64(vduint128(0x123456789ABCDEF0, 0xBAADF00DDEADBEEF), 0xFEDCBA9876543210, remainder)), quotient == 0x1249249249249238 && remainder == 0xA72CB7FA5D75AB6F));

	TEST_ASSERT(VDMulDiv64(-10000000000000000, -10000000000000000, -10000000000000000) == -10000000000000000);
	TEST_ASSERT(VDMulDiv64(-1000000000000, -100000, 17) == 5882352941176471);

	vduint128 seed(0);

	for(int i=0; i<10000; ++i) {
		vduint128 x(seed);	seed = rand128(seed);
		vduint128 y(seed);	seed = rand128(seed);
		vduint128 p(x*y);
		vduint128 q(slowmul(x, y));

		TEST_ASSERT(p == q);
	}

	return 0;
}
