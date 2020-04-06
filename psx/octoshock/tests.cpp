// DO NOT REMOVE/DISABLE THESE MATH AND COMPILER SANITY TESTS.  THEY EXIST FOR A REASON.

/* Mednafen - Multi-system Emulator
 *
 * This program is free software; you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation; either version 2 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program; if not, write to the Free Software
 * Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
 */

// We really don't want NDEBUG defined ;)
#ifdef HAVE_CONFIG_H
#include <config.h>
#endif

#undef NDEBUG

#include <vector>
#include <algorithm>
#include <stdio.h>
#include "octoshock.h"
#include "math_ops.h"
#include "error.h"
#include "endian.h"

#ifdef WANT_TEST_LEPACKER
#include "lepacker.h"
#endif

#include "tests.h"

#ifdef WANT_TEST_HASHES
#include <mednafen/hash/sha1.h>
#include <mednafen/hash/sha256.h>
#endif

#ifdef WANT_TEST_TIME
#include <mednafen/Time.h>
#include <time.h>
#endif

#ifdef WANT_TEST_ZLIB
#include <zlib.h>
#endif

#undef NDEBUG
#include <assert.h>
#include <math.h>

#include "psx/masmem.h"

#if defined(HAVE_FENV_H)
#include <fenv.h>
#endif

#include <atomic>

namespace MDFN_TESTS_CPP
{

// Don't define this static, and don't define it const.  We want these tests to be done at run time, not compile time(although maybe we should do both...).
typedef struct
{
 int bits;
 uint32 negative_one;
 uint32 mostneg;
 int32 mostnegresult;
} MathTestEntry;

#define ADD_MTE(_bits) { _bits, ((uint32)1 << _bits) - 1, (uint32)1 << (_bits - 1), (int32)(0 - ((uint32)1 << (_bits - 1))) }

MathTestEntry math_test_vals[] =
{
 {  9, 0x01FF, 0x0100, -256 },
 { 10, 0x03FF, 0x0200, -512 },
 { 11, 0x07FF, 0x0400, -1024 },
 { 12, 0x0FFF, 0x0800, -2048 },
 { 13, 0x1FFF, 0x1000, -4096 },
 { 14, 0x3FFF, 0x2000, -8192 },
 { 15, 0x7FFF, 0x4000, -16384 },

 ADD_MTE(17),
 ADD_MTE(18),
 ADD_MTE(19),
 ADD_MTE(20),
 ADD_MTE(21),
 ADD_MTE(22),
 ADD_MTE(23),
 ADD_MTE(24),
 ADD_MTE(25),
 ADD_MTE(26),
 ADD_MTE(27),
 ADD_MTE(28),
 ADD_MTE(29),
 ADD_MTE(30),
 ADD_MTE(31),

 { 0, 0, 0, 0 },
};

static void TestSignExtend(void)
{
 MathTestEntry *itoo = math_test_vals;

 assert(sign_9_to_s16(itoo->negative_one) == -1 && sign_9_to_s16(itoo->mostneg) == itoo->mostnegresult);
 itoo++;

 assert(sign_10_to_s16(itoo->negative_one) == -1 && sign_10_to_s16(itoo->mostneg) == itoo->mostnegresult);
 itoo++;

 assert(sign_11_to_s16(itoo->negative_one) == -1 && sign_11_to_s16(itoo->mostneg) == itoo->mostnegresult);
 itoo++;

 assert(sign_12_to_s16(itoo->negative_one) == -1 && sign_12_to_s16(itoo->mostneg) == itoo->mostnegresult);
 itoo++;

 assert(sign_13_to_s16(itoo->negative_one) == -1 && sign_13_to_s16(itoo->mostneg) == itoo->mostnegresult);
 itoo++;

 assert(sign_14_to_s16(itoo->negative_one) == -1 && sign_14_to_s16(itoo->mostneg) == itoo->mostnegresult);
 itoo++;

 assert(sign_15_to_s16(itoo->negative_one) == -1 && sign_15_to_s16(itoo->mostneg) == itoo->mostnegresult);
 itoo++;

 assert(sign_x_to_s32(17, itoo->negative_one) == -1 && sign_x_to_s32(17, itoo->mostneg) == itoo->mostnegresult);
 itoo++;

 assert(sign_x_to_s32(18, itoo->negative_one) == -1 && sign_x_to_s32(18, itoo->mostneg) == itoo->mostnegresult);
 itoo++;

 assert(sign_x_to_s32(19, itoo->negative_one) == -1 && sign_x_to_s32(19, itoo->mostneg) == itoo->mostnegresult);
 itoo++;

 assert(sign_x_to_s32(20, itoo->negative_one) == -1 && sign_x_to_s32(20, itoo->mostneg) == itoo->mostnegresult);
 itoo++;

 assert(sign_x_to_s32(21, itoo->negative_one) == -1 && sign_x_to_s32(21, itoo->mostneg) == itoo->mostnegresult);
 itoo++;

 assert(sign_x_to_s32(22, itoo->negative_one) == -1 && sign_x_to_s32(22, itoo->mostneg) == itoo->mostnegresult);
 itoo++;

 assert(sign_x_to_s32(23, itoo->negative_one) == -1 && sign_x_to_s32(23, itoo->mostneg) == itoo->mostnegresult);
 itoo++;

 assert(sign_x_to_s32(24, itoo->negative_one) == -1 && sign_x_to_s32(24, itoo->mostneg) == itoo->mostnegresult);
 itoo++;

 assert(sign_x_to_s32(25, itoo->negative_one) == -1 && sign_x_to_s32(25, itoo->mostneg) == itoo->mostnegresult);
 itoo++;

 assert(sign_x_to_s32(26, itoo->negative_one) == -1 && sign_x_to_s32(26, itoo->mostneg) == itoo->mostnegresult);
 itoo++;

 assert(sign_x_to_s32(27, itoo->negative_one) == -1 && sign_x_to_s32(27, itoo->mostneg) == itoo->mostnegresult);
 itoo++;

 assert(sign_x_to_s32(28, itoo->negative_one) == -1 && sign_x_to_s32(28, itoo->mostneg) == itoo->mostnegresult);
 itoo++;

 assert(sign_x_to_s32(29, itoo->negative_one) == -1 && sign_x_to_s32(29, itoo->mostneg) == itoo->mostnegresult);
 itoo++;

 assert(sign_x_to_s32(30, itoo->negative_one) == -1 && sign_x_to_s32(30, itoo->mostneg) == itoo->mostnegresult);
 itoo++;

 assert(sign_x_to_s32(31, itoo->negative_one) == -1 && sign_x_to_s32(31, itoo->mostneg) == itoo->mostnegresult);
 itoo++;
}

static void DoSizeofTests(void)
{
 assert(sizeof(uint8) == 1);
 assert(sizeof(int8) == 1);

 assert(sizeof(uint16) == 2);
 assert(sizeof(int16) == 2);

 assert(sizeof(uint32) == 4);
 assert(sizeof(int32) == 4);

 assert(sizeof(uint64) == 8);
 assert(sizeof(int64) == 8);

 assert(sizeof(char) == 1);
 assert(sizeof(int) == 4);
 assert(sizeof(long) >= 4);
 assert(sizeof(long long) >= 8);

 assert(sizeof(float) >= 4);
 assert(sizeof(double) >= 8);
 assert(sizeof(long double) >= 8);

 assert(sizeof(void*) >= 4);

 assert(sizeof(char) == SIZEOF_CHAR);
 assert(sizeof(short) == SIZEOF_SHORT);
 assert(sizeof(int) == SIZEOF_INT);
 assert(sizeof(long) == SIZEOF_LONG);
 assert(sizeof(long long) == SIZEOF_LONG_LONG);

 assert(sizeof(off_t) == SIZEOF_OFF_T);
 assert(sizeof(ptrdiff_t) == SIZEOF_PTRDIFF_T);
 assert(sizeof(size_t) == SIZEOF_SIZE_T);
 assert(sizeof(void*) == SIZEOF_VOID_P);

 assert(sizeof(double) == SIZEOF_DOUBLE);
}

static void TestTypesSign(void)
{
 // Make sure the "char" type is signed(pass -fsigned-char to gcc).  New code in Mednafen shouldn't be written with the
 // assumption that "char" is signed, but there likely is at least some code that does.
 {
  char tmp = 255;
  assert(tmp < 0);
 }
}

static void AntiNSOBugTest_Sub1_a(int *array) NO_INLINE;
static void AntiNSOBugTest_Sub1_a(int *array)
{
 for(int value = 0; value < 127; value++)
  array[value] += (int8)value * 15;
}

static void AntiNSOBugTest_Sub1_b(int *array) NO_INLINE;
static void AntiNSOBugTest_Sub1_b(int *array)
{
 for(int value = 127; value < 256; value++)
  array[value] += (int8)value * 15;
}

static void AntiNSOBugTest_Sub2(int *array) NO_INLINE;
static void AntiNSOBugTest_Sub2(int *array)
{
 for(int value = 0; value < 256; value++)
  array[value] += (int8)value * 15;
}

static void AntiNSOBugTest_Sub3(int *array) NO_INLINE;
static void AntiNSOBugTest_Sub3(int *array)
{
 for(int value = 0; value < 256; value++)
 {
  if(value >= 128)
   array[value] = (value - 256) * 15;
  else
   array[value] = value * 15;
 }
}

static void DoAntiNSOBugTest(void)
{
 int array1[256], array2[256], array3[256];
 
 memset(array1, 0, sizeof(array1));
 memset(array2, 0, sizeof(array2));
 memset(array3, 0, sizeof(array3));

 AntiNSOBugTest_Sub1_a(array1);
 AntiNSOBugTest_Sub1_b(array1);
 AntiNSOBugTest_Sub2(array2);
 AntiNSOBugTest_Sub3(array3);

 for(int i = 0; i < 256; i++)
 {
  assert((array1[i] == array2[i]) && (array2[i] == array3[i]));
 }
 //for(int value = 0; value < 256; value++)
 // printf("%d, %d\n", (int8)value, ((int8)value) * 15);
}

//
// Related: https://gcc.gnu.org/bugzilla/show_bug.cgi?id=61741
//
// Not found to be causing problems in Mednafen(unlike the earlier no-strict-overflow problem and associated test),
// but better safe than sorry.
//
static void DoAntiNSOBugTest2014_SubA(int a) NO_INLINE NO_CLONE;
static void DoAntiNSOBugTest2014_SubA(int a)
{
 char c = 0;

 for(; a; a--)
 {
  for(; c >= 0; c++)
  {

  }
 }

 assert(c == -128);
}

static int ANSOBT_CallCount;
static void DoAntiNSOBugTest2014_SubMx_F(void) NO_INLINE NO_CLONE;
static void DoAntiNSOBugTest2014_SubMx_F(void)
{
 ANSOBT_CallCount++;

 assert(ANSOBT_CallCount < 1000);
}

static void DoAntiNSOBugTest2014_SubM1(void) NO_INLINE NO_CLONE;
static void DoAntiNSOBugTest2014_SubM1(void)
{
 char a;

 for(a = 127 - 1; a >= 0; a++)
  DoAntiNSOBugTest2014_SubMx_F();
}

static void DoAntiNSOBugTest2014_SubM3(void) NO_INLINE NO_CLONE;
static void DoAntiNSOBugTest2014_SubM3(void)
{
 char a;

 for(a = 127 - 3; a >= 0; a++)
  DoAntiNSOBugTest2014_SubMx_F();
}


static void DoAntiNSOBugTest2014(void)
{
 DoAntiNSOBugTest2014_SubA(1);

 ANSOBT_CallCount = 0;
 DoAntiNSOBugTest2014_SubM1();
 assert(ANSOBT_CallCount == 2);

 ANSOBT_CallCount = 0;
 DoAntiNSOBugTest2014_SubM3();
 assert(ANSOBT_CallCount == 4);
}

#ifdef WANT_TEST_LEPACKER
void DoLEPackerTest(void)
{
 MDFN::LEPacker mizer;
 static const uint8 correct_result[24] = { 0xed, 0xfe, 0xed, 0xde, 0xaa, 0xca, 0xef, 0xbe, 0xbe, 0xba, 0xfe, 0xca, 0xad, 0xde, 0x01, 0x9a, 0x0c, 0xa7, 0xff, 0x00, 0xff, 0xff, 0x55, 0x7f };

 uint64 u64_test = 0xDEADCAFEBABEBEEFULL;
 uint32 u32_test = 0xDEEDFEED;
 uint16 u16_test = 0xCAAA;
 uint8 u8_test = 0x55;
 int32 s32_test = -5829478;
 int16 s16_test = -1;
 int8 s8_test = 127;

 bool bool_test0 = 0;
 bool bool_test1 = 1;

 mizer ^ u32_test;
 mizer ^ u16_test;
 mizer ^ u64_test;
 mizer ^ bool_test1;
 mizer ^ s32_test;
 mizer ^ bool_test0;
 mizer ^ s16_test;
 mizer ^ u8_test;
 mizer ^ s8_test;

 assert(mizer.size() == 24);

 for(unsigned int i = 0; i < mizer.size(); i++)
 {
  assert(mizer[i] == correct_result[i]);
 }

 u64_test = 0;
 u32_test = 0;
 u16_test = 0;
 u8_test = 0;
 s32_test = 0;
 s16_test = 0;
 s8_test = 0;

 bool_test0 = 1;
 bool_test1 = 0;

 mizer.set_read_mode(true);

 mizer ^ u32_test;
 mizer ^ u16_test;
 mizer ^ u64_test;
 mizer ^ bool_test1;
 mizer ^ s32_test;
 mizer ^ bool_test0;
 mizer ^ s16_test;
 mizer ^ u8_test;
 mizer ^ s8_test;


 assert(u32_test == 0xDEEDFEED);
 assert(u16_test == 0xCAAA);
 assert(u64_test == 0xDEADCAFEBABEBEEFULL);
 assert(u8_test == 0x55);
 assert(s32_test == -5829478);
 assert(s16_test == -1);
 assert(s8_test == 127);
 assert(bool_test0 == 0);
 assert(bool_test1 == 1);
}
#endif

struct MathTestTSOEntry
{
 int32 a;
 int32 b;
};

// Don't declare as static(though whopr might mess it up anyway)
MathTestTSOEntry MathTestTSOTests[] =
{
 { 0x7FFFFFFF, 2 },
 { 0x7FFFFFFE, 0x7FFFFFFF },
 { 0x7FFFFFFF, 0x7FFFFFFF },
 { 0x7FFFFFFE, 0x7FFFFFFE },
};

volatile int32 MDFNTestsCPP_SLS_Var = (int32)0xDEADBEEF;
volatile int8 MDFNTestsCPP_SLS_Var8 = (int8)0xEF;
volatile int16 MDFNTestsCPP_SLS_Var16 = (int16)0xBEEF;
int32 MDFNTestsCPP_SLS_Var_NT = (int32)0xDEADBEEF;
int32 MDFNTestsCPP_SLS_Var_NT2 = (int32)0x7EADBEEF;

static uint64 NO_INLINE NO_CLONE Mul_U16U16U32U64_Proper(uint16 a, uint16 b)	// For reference
{
 return (uint32)a * (uint32)b;
}

static uint64 NO_INLINE NO_CLONE Mul_U16U16U32U64(uint16 a, uint16 b)
{
 return (uint32)(a * b);
}

static void TestSignedOverflow(void)
{
 assert(Mul_U16U16U32U64_Proper(65535, 65535) == 0xfffe0001ULL);
 assert(Mul_U16U16U32U64(65535, 65535) == 0xfffe0001ULL);

 for(unsigned int i = 0; i < sizeof(MathTestTSOTests) / sizeof(MathTestTSOEntry); i++)
 {
  int32 a = MathTestTSOTests[i].a;
  int32 b = MathTestTSOTests[i].b;

  assert((a + b) < a && (a + b) < b);

  assert((a + 0x7FFFFFFE) < a);
  assert((b + 0x7FFFFFFE) < b);

  assert((a + 0x7FFFFFFF) < a);
  assert((b + 0x7FFFFFFF) < b);

  assert((int32)(a + 0x80000000) < a);
  assert((int32)(b + 0x80000000) < b);

  assert((int32)(a ^ 0x80000000) < a);
  assert((int32)(b ^ 0x80000000) < b);
 }

 for(unsigned i = 0; i < 64; i++)
 {
  MDFNTestsCPP_SLS_Var = (MDFNTestsCPP_SLS_Var << 1) ^ ((MDFNTestsCPP_SLS_Var << 2) + 0x7FFFFFFF) ^ ((MDFNTestsCPP_SLS_Var >> 31) & 0x3);
  MDFNTestsCPP_SLS_Var8 = (MDFNTestsCPP_SLS_Var8 << 1) ^ ((MDFNTestsCPP_SLS_Var8 << 2) + 0x7F) ^ ((MDFNTestsCPP_SLS_Var8 >> 7) & 0x3);
  MDFNTestsCPP_SLS_Var16 = (MDFNTestsCPP_SLS_Var16 << 1) ^ ((MDFNTestsCPP_SLS_Var16 << 2) + 0x7FFF) ^ ((MDFNTestsCPP_SLS_Var16 >> 15) & 0x3);
 }

 {
  int8 a = MDFNTestsCPP_SLS_Var8;
  int16 b = MDFNTestsCPP_SLS_Var16;
  int32 c = MDFNTestsCPP_SLS_Var;
  int64 d = (int64)MDFNTestsCPP_SLS_Var * (int64)MDFNTestsCPP_SLS_Var;
  int32 e = c;
  int64 f = c;

  for(int i = 0; i < 64; i++)
  {
   a += a * i + b;
   b += b * i + c;
   c += c * i + d;
   d += d * i + a;

   e += e * i + c;
   f += f * i + c;
  }
  //printf("%08x %16llx - %02x %04x %08x %16llx\n", (uint32)e, (uint64)f, (uint8)a, (uint16)b, (uint32)c, (uint64)d);
  assert((uint32)e == (uint32)f && (uint32)e == 0x00c37de2 && (uint64)f == 0x5d17261900c37de2);
  assert((uint8)a == 0xbf);
  assert((uint16)b == 0xb77c);
  assert((uint32)c == 0xb4244622U);
  assert((uint64)d == 0xa966e02ed95c83fULL);
 }


 //printf("%02x %04x %08x\n", (uint8)MDFNTestsCPP_SLS_Var8, (uint16)MDFNTestsCPP_SLS_Var16, (uint32)MDFNTestsCPP_SLS_Var);
 assert((uint8)MDFNTestsCPP_SLS_Var8 == 0x04);
 assert((uint16)MDFNTestsCPP_SLS_Var16 == 0xa7d8);
 assert((uint32)MDFNTestsCPP_SLS_Var == 0x4ef11a23);

 for(signed i = 1; i != 0; i =~-i);	// Not really signed overflow, but meh!
 for(signed i = -1; i != 0; i <<= 1);
 for(signed i = 1; i >= 0; i *= 3);

 if(MDFNTestsCPP_SLS_Var_NT < 0)
  assert((MDFNTestsCPP_SLS_Var_NT << 2) > 0);

 if(MDFNTestsCPP_SLS_Var_NT2 > 0)
  assert((MDFNTestsCPP_SLS_Var_NT2 << 2) < 0);
}

unsigned MDFNTests_OverShiftAmounts[3] = { 8, 16, 32};
uint32 MDFNTests_OverShiftTV = 0xBEEFD00D;
static void TestDefinedOverShift(void)
{
 //for(unsigned sa = 0; sa < 4; sa++)
 {
  for(unsigned i = 0; i < 2; i++)
  {
   uint8 v8 = MDFNTests_OverShiftTV;
   uint16 v16 = MDFNTests_OverShiftTV;
   uint32 v32 = MDFNTests_OverShiftTV;

   int8 iv8 = MDFNTests_OverShiftTV;
   int16 iv16 = MDFNTests_OverShiftTV;
   int32 iv32 = MDFNTests_OverShiftTV;

   if(i == 1)
   {
    v8 >>= MDFNTests_OverShiftAmounts[0];
    v16 >>= MDFNTests_OverShiftAmounts[1];
    v32 = (uint64)v32 >> MDFNTests_OverShiftAmounts[2];

    iv8 >>= MDFNTests_OverShiftAmounts[0];
    iv16 >>= MDFNTests_OverShiftAmounts[1];
    iv32 = (int64)iv32 >> MDFNTests_OverShiftAmounts[2];
   }
   else
   {
    v8 <<= MDFNTests_OverShiftAmounts[0];
    v16 <<= MDFNTests_OverShiftAmounts[1];
    v32 = (uint64)v32 << MDFNTests_OverShiftAmounts[2];

    iv8 <<= MDFNTests_OverShiftAmounts[0];
    iv16 <<= MDFNTests_OverShiftAmounts[1];
    iv32 = (int64)iv32 << MDFNTests_OverShiftAmounts[2];
   }

   assert(v8 == 0);
   assert(v16 == 0);
   assert(v32 == 0);

   assert(iv8 == 0);
   assert(iv16 == -(int)i);
   assert(iv32 == -(int)i);
  }
 }
}

static uint8 BoolConvSupportFunc(void) MDFN_COLD NO_INLINE;
static uint8 BoolConvSupportFunc(void)
{
 return 0xFF;
}

static bool BoolConv0(void) MDFN_COLD NO_INLINE;
static bool BoolConv0(void)
{
 return BoolConvSupportFunc() & 1;
}

static void BoolTestThing(unsigned val) MDFN_COLD NO_INLINE;
static void BoolTestThing(unsigned val)
{
 if(val != 1)
  printf("%u\n", val);

 assert(val == 1);
}

static void TestBoolConv(void)
{
 BoolTestThing(BoolConv0());
}

static void TestNarrowConstFold(void) NO_INLINE MDFN_COLD;
static void TestNarrowConstFold(void)
{
 unsigned sa = 8;
 uint8 za[1] = { 0 };
 int a;

 a = za[0] < (uint8)(1 << sa);

 assert(a == 0);
}


unsigned MDFNTests_ModTern_a = 2;
unsigned MDFNTests_ModTern_b = 0;
static void ModTernTestEval(unsigned v) NO_INLINE MDFN_COLD;
static void ModTernTestEval(unsigned v)
{
 assert(v == 0);
}

static void TestModTern(void) NO_INLINE MDFN_COLD;
static void TestModTern(void)
{
 if(!MDFNTests_ModTern_b)
 {
  MDFNTests_ModTern_b = MDFNTests_ModTern_a;

  if(1 % (MDFNTests_ModTern_a ? MDFNTests_ModTern_a : 2))
   MDFNTests_ModTern_b = 0;
 }
 ModTernTestEval(MDFNTests_ModTern_b);
}

static int TestBWNotMask31GTZ_Sub(int a) NO_INLINE NO_CLONE;
static int TestBWNotMask31GTZ_Sub(int a)
{
 a = (((~a) & 0x80000000LL) > 0) + 1;
 return a;
}

static void TestBWNotMask31GTZ(void)
{
 assert(TestBWNotMask31GTZ_Sub(0) == 2);
}

int MDFN_tests_TestTernary_val = 0;
static void NO_INLINE NO_CLONE TestTernary_Sub(void)
{
 MDFN_tests_TestTernary_val++;
}

static void TestTernary(void)
{
 int a = ((MDFN_tests_TestTernary_val++) ? (MDFN_tests_TestTernary_val = 20) : (TestTernary_Sub(), MDFN_tests_TestTernary_val));

 assert(a == 2);
}

size_t TestLLVM15470_Counter;
void NO_INLINE NO_CLONE TestLLVM15470_Sub2(size_t x)
{
 assert(x == TestLLVM15470_Counter);
 TestLLVM15470_Counter++;
}

void NO_INLINE NO_CLONE TestLLVM15470_Sub(size_t m)
{
 size_t m2 = ~(size_t)0;

 for(size_t i = 1; i <= 4; i *= m)
  m2++;

 for(size_t a = 0; a < 2; a++)
 {
  for(size_t b = 1; b <= 2; b++)
  {
   TestLLVM15470_Sub2(a * m2 + b);
  }
 }
}

void NO_INLINE NO_CLONE TestLLVM15470(void)
{
 TestLLVM15470_Counter = 1;
 TestLLVM15470_Sub(2);
}

int NO_INLINE NO_CLONE TestGCC60196_Sub(const int16* data, int count)
{
 int ret = 0;

 for(int i = 0; i < count; i++)
  ret += i * data[i];

 return ret;
}

void NO_INLINE NO_CLONE TestGCC60196(void)
{
 int16 ta[16];

 for(unsigned i = 0; i < 16; i++)
  ta[i] = 1;

 assert(TestGCC60196_Sub(ta, sizeof(ta) / sizeof(ta[0])) == 120);
}


uint16 gcc69606_var = 0;
int32 NO_INLINE NO_CLONE TestGCC69606_Sub(int8 a, uint8 e)
{
 int32 f = 1;
 int32 mod = ~(int32)a;
 int32 d = 0;

 if(gcc69606_var > f)
 {
  e = gcc69606_var;
  d = gcc69606_var | e;
  mod = 8;
 }

 return (7 + d) % mod;
}

void NO_INLINE NO_CLONE TestGCC69606(void)
{
 assert(TestGCC69606_Sub(0, 0) == 0);
}

int8 gcc70941_array[2] = { 0, 0 };
void NO_INLINE NO_CLONE TestGCC70941(void)
{
 int8 tmp = (0x7F - gcc70941_array[0] - ((gcc70941_array[0] && gcc70941_array[1]) ^ 0x8080));

 assert(tmp == -1);
}

template<typename A, typename B>
void NO_INLINE NO_CLONE TestSUCompare_Sub(A a, B b)
{
 assert(a < b);
}

int16 TestSUCompare_x0 = 256;

void NO_INLINE NO_CLONE TestSUCompare(void)
{
 int8 a = 1;
 uint8 b = 255;
 int16 c = 1;
 uint16 d = 65535;
 int32 e = 1;
 uint32 f = ~0U;
 int64 g = ~(uint32)0;
 uint64 h = ~(uint64)0;

 assert(a < b);
 assert(c < d);
 assert((uint32)e < f);
 assert((uint64)g < h);

 TestSUCompare_Sub<int8, uint8>(1, 255);
 TestSUCompare_Sub<int16, uint16>(1, 65535);

 TestSUCompare_Sub<int8, uint8>(TestSUCompare_x0, 255);
}

static void DoAlignmentChecks(void)
{
 uint8 padding0[3];
 alignas(16) uint8 aligned0[7];
 alignas(4)  uint8 aligned1[2];
 alignas(16) uint32 aligned2[2];
 uint8 padding1[3];

 static uint8 g_padding0[3];
 alignas(16) static uint8 g_aligned0[7];
 alignas(4)  static uint8 g_aligned1[2];
 alignas(16) static uint32 g_aligned2[2];
 static uint8 g_padding1[3];

 // Make sure compiler doesn't removing padding vars
 assert((&padding0[1] - &padding0[0]) == 1);
 assert((&padding1[1] - &padding1[0]) == 1);
 assert((&g_padding0[1] - &g_padding0[0]) == 1);
 assert((&g_padding1[1] - &g_padding1[0]) == 1);


 assert( (((unsigned long long)&aligned0[0]) & 0xF) == 0);
 assert( (((unsigned long long)&aligned1[0]) & 0x3) == 0);
 assert( (((unsigned long long)&aligned2[0]) & 0xF) == 0);

 assert(((uint8 *)&aligned0[1] - (uint8 *)&aligned0[0]) == 1);
 assert(((uint8 *)&aligned1[1] - (uint8 *)&aligned1[0]) == 1);
 assert(((uint8 *)&aligned2[1] - (uint8 *)&aligned2[0]) == 4);


 assert( (((unsigned long long)&g_aligned0[0]) & 0xF) == 0);
 assert( (((unsigned long long)&g_aligned1[0]) & 0x3) == 0);
 assert( (((unsigned long long)&g_aligned2[0]) & 0xF) == 0);

 assert(((uint8 *)&g_aligned0[1] - (uint8 *)&g_aligned0[0]) == 1);
 assert(((uint8 *)&g_aligned1[1] - (uint8 *)&g_aligned1[0]) == 1);
 assert(((uint8 *)&g_aligned2[1] - (uint8 *)&g_aligned2[0]) == 4);
}

static uint32 NO_INLINE NO_CLONE RunMASMemTests_DoomAndGloom(uint32 offset)
{
 MultiAccessSizeMem<4, false> mt0;

 mt0.WriteU32(offset, 4);
 mt0.WriteU16(offset, 0);
 mt0.WriteU32(offset, mt0.ReadU32(offset) + 1);

 return mt0.ReadU32(offset);
}

static void RunMASMemTests(void)
{
 // Little endian:
 {
  MultiAccessSizeMem<4, false> mt0;

  mt0.WriteU16(0, 0xDEAD);
  mt0.WriteU32(0, 0xCAFEBEEF);
  mt0.WriteU16(2, mt0.ReadU16(0));
  mt0.WriteU8(1, mt0.ReadU8(0));
  mt0.WriteU16(2, mt0.ReadU16(0));
  mt0.WriteU32(0, mt0.ReadU32(0) + 0x13121111);

  assert(mt0.ReadU16(0) == 0x0100 && mt0.ReadU16(2) == 0x0302);
  assert(mt0.ReadU32(0) == 0x03020100);
 
  mt0.WriteU32(0, 0xB0B0AA55);
  mt0.WriteU24(0, 0xDEADBEEF);
  assert(mt0.ReadU32(0) == 0xB0ADBEEF);
  assert(mt0.ReadU24(1) == 0x00B0ADBE);
 }

 // Big endian:
 {
  MultiAccessSizeMem<4, true> mt0;

  mt0.WriteU16(2, 0xDEAD);
  mt0.WriteU32(0, 0xCAFEBEEF);
  mt0.WriteU16(0, mt0.ReadU16(2));
  mt0.WriteU8(2, mt0.ReadU8(3));
  mt0.WriteU16(0, mt0.ReadU16(2));
  mt0.WriteU32(0, mt0.ReadU32(0) + 0x13121111);

  assert(mt0.ReadU16(2) == 0x0100 && mt0.ReadU16(0) == 0x0302);
  assert(mt0.ReadU32(0) == 0x03020100);
 
  mt0.WriteU32(0, 0xB0B0AA55);
  mt0.WriteU24(1, 0xDEADBEEF);
  assert(mt0.ReadU32(0) == 0xB0ADBEEF);
  assert(mt0.ReadU24(0) == 0x00B0ADBE);
 }

 assert(RunMASMemTests_DoomAndGloom(0) == 1);
}

static void NO_INLINE NO_CLONE RunMiscEndianTests(uint32 arg0, uint32 arg1)
{
 uint8 mem[8];

 memset(mem, 0xFF, sizeof(mem));

 MDFN_en24msb(&mem[0], arg0);
 MDFN_en24lsb(&mem[4], arg1);

 assert(MDFN_de32lsb(&mem[0]) == 0xFF030201);
 assert(MDFN_de32lsb(&mem[4]) == 0xFF030201);

 assert(MDFN_de32msb(&mem[0]) == 0x010203FF);
 assert(MDFN_de32msb(&mem[4]) == 0x010203FF);

 assert(MDFN_de32lsb(&mem[0]) == 0xFF030201);
 assert(MDFN_de32msb(&mem[4]) == 0x010203FF);

 assert(MDFN_de24lsb(&mem[0]) == 0x030201);
 assert(MDFN_de24lsb(&mem[4]) == 0x030201);

 assert(MDFN_de24msb(&mem[0]) == 0x010203);
 assert(MDFN_de24msb(&mem[4]) == 0x010203);

 assert(MDFN_de24lsb(&mem[1]) == 0xFF0302);
 assert(MDFN_de24lsb(&mem[5]) == 0xFF0302);

 assert(MDFN_de24msb(&mem[1]) == 0x0203FF);
 assert(MDFN_de24msb(&mem[5]) == 0x0203FF);

 assert(MDFN_de64lsb(&mem[0]) == 0xFF030201FF030201ULL);
 assert(MDFN_de64msb(&mem[0]) == 0x010203FF010203FFULL);
 //
 //
 //
 uint16 mem16[8] = { 0x1122, 0x3344, 0x5566, 0x7788, 0x99AA, 0xBBCC, 0xDDEE, 0xFF00 };

 for(unsigned i = 0; i < 8; i++)
 {
  assert(ne16_rbo_le<uint16>(mem16, i << 1) == mem16[i]);
  assert(ne16_rbo_be<uint16>(mem16, i << 1) == mem16[i]);
 }

 for(unsigned i = 0; i < 4; i++)
 {
  assert(ne16_rbo_le<uint32>(mem16, i * 4) == (uint32)(mem16[i * 2] | (mem16[i * 2 + 1] << 16)));
  assert(ne16_rbo_be<uint32>(mem16, i * 4) == (uint32)(mem16[i * 2 + 1] | (mem16[i * 2] << 16)));
 }

 for(unsigned i = 0; i < 16; i++)
 {
  assert(ne16_rbo_le<uint8>(mem16, i) == (uint8)(mem16[i >> 1] >> ((i & 1) * 8)));
  assert(ne16_rbo_be<uint8>(mem16, i) == (uint8)(mem16[i >> 1] >> (8 - ((i & 1) * 8))));
 }

 ne16_rwbo_le<uint16, false>(mem16, 0, &mem16[7]);
 ne16_rwbo_le<uint16, true>(mem16, 0, &mem16[6]);
 ne16_rwbo_be<uint16, false>(mem16, 4, &mem16[3]);
 ne16_rwbo_be<uint16, true>(mem16, 4, &mem16[1]);

 assert(mem16[7] == 0x1122);
 assert(mem16[0] == 0xDDEE);
 assert(mem16[3] == 0x5566);
 assert(mem16[2] == 0x3344);

 //
 //
 //
 for(unsigned i = 0; i < 8; i++)
 {
  for(unsigned z = 0; z < 8; z++)
   mem[z] = z;

  #ifdef LSB_FIRST
  Endian_V_NE_BE(mem, i);
  #else
  Endian_V_NE_LE(mem, i);
  #endif

  for(unsigned z = 0; z < 8; z++)
   assert(mem[z] == ((z >= i) ? z : (i - 1 - z)));
 }
}

static void NO_INLINE NO_CLONE ExceptionTestSub(int v, int n, int* y)
{
 if(n)
 {
  if(n & 1)
  {
   try
   {
    ExceptionTestSub(v + n, n - 1, y);
   }
   catch(const std::exception &e)
   {
    (*y)++;
    throw;
   }
  }
  else
   ExceptionTestSub(v + n, n - 1, y);
 }
 else
  throw MDFN_Error(v, "%d", v);
}

static NO_CLONE NO_INLINE int RunExceptionTests_TEP(void* data)
{
 std::atomic_int_least32_t* sv = (std::atomic_int_least32_t*)data;

 sv->fetch_sub(1, std::memory_order_release);

 while(sv->load(std::memory_order_acquire) > 0);

 unsigned t = 0;

 for(; !t || sv->load(std::memory_order_acquire) == 0; t++)
 {
  int y = 0;
  int z = 0;

  for(int x = -8; x < 8; x++)
  {
   try
   {
    ExceptionTestSub(x, x & 3, &y);
   }
   catch(const MDFN_Error &e)
   {
    int epv = x;

    for(unsigned i = x & 3; i; i--)
     epv += i;

    z += epv;

    assert(e.GetErrno() == epv);
    assert(atoi(e.what()) == epv);
    continue;
   }
   catch(...)
   {
    abort();
   }
   abort();
  }

  assert(y == 16);
  assert(z == 32);
 }

 return t;
}

std::vector<int> stltests_vec[2];

static void NO_INLINE NO_CLONE RunSTLTests_Sub0(int v)
{
 stltests_vec[0].assign(v, v);
}

static void RunSTLTests(void)
{
 assert(stltests_vec[0] == stltests_vec[1]);
 RunSTLTests_Sub0(0);
 assert(stltests_vec[0] == stltests_vec[1]);
 RunSTLTests_Sub0(1);
 RunSTLTests_Sub0(0);
 assert(stltests_vec[0] == stltests_vec[1]);
}

static void LZTZCount_Test(void)
{
 for(uint32 i = 0, x = 0; i < 33; i++, x = (x << 1) + 1)
 {
  assert(MDFN_tzcount16(~x) == std::min<uint32>(16, i));
  assert(MDFN_tzcount32(~x) == i);
  assert(MDFN_lzcount32(x) == 32 - i);
 }

 for(uint32 i = 0, x = 0; i < 33; i++, x = (x ? (x << 1) : 1))
 {
  assert(MDFN_tzcount16(x) == (std::min<uint32>(17, i) + 16) % 17);
  assert(MDFN_tzcount32(x) == (i + 32) % 33);
  assert(MDFN_lzcount32(x) == 32 - i);
 }

 for(uint64 i = 0, x = 0; i < 65; i++, x = (x << 1) + 1)
 {
  assert(MDFN_tzcount64(~x) == i);
  assert(MDFN_lzcount64(x) == 64 - i);
 }

 for(uint64 i = 0, x = 0; i < 65; i++, x = (x ? (x << 1) : 1))
 {
  assert(MDFN_tzcount64(x) == (i + 64) % 65);
  assert(MDFN_lzcount64(x) == 64 - i);
 }

 uint32 tv = 0;
 for(uint32 i = 0, x = 1; i < 200; i++, x = (x * 9) + MDFN_lzcount32(x) + MDFN_lzcount32(x >> (x & 31)))
 {
  tv += x;
 }
 assert(tv == 0x397d920f);

 uint64 tv64 = 0;
 for(uint64 i = 0, x = 1; i < 200; i++, x = (x * 9) + MDFN_lzcount64(x) + MDFN_lzcount64(x >> (x & 63)))
 {
  tv64 += x;
 }
 assert(tv64 == 0x7b8263de01922c29);
}

// don't make this static, and don't make it local scope.  Whole-program optimization might defeat the purpose of this, though...
unsigned int mdfn_shifty_test[4] =
{
 0, 8, 16, 32
};


// Don't make static.
double mdfn_fptest0_sub(double x, double n) MDFN_COLD NO_INLINE;
double mdfn_fptest0_sub(double x, double n)
{
 double u = x / (n * n);

 return(u);
}

static void fptest0(void)
{
 assert(mdfn_fptest0_sub(36, 2) == 9);
}

volatile double mdfn_fptest1_v;
static void fptest1(void)
{
 mdfn_fptest1_v = 1.0;

 for(int i = 0; i < 128; i++)
  mdfn_fptest1_v *= 2;

 assert(mdfn_fptest1_v == 340282366920938463463374607431768211456.0);
}

#if defined(HAVE_FENV_H) && defined(HAVE_NEARBYINTF)
// For advisory/debug purposes, don't error out on failure.
static void libc_rounding_test(void)
{
 unsigned old_rm = fegetround();
 float tv = 4118966.75;
 float goodres = 4118967.0;
 float res;

 fesetround(FE_TONEAREST);

 if((res = nearbyintf(tv)) != goodres)
  fprintf(stderr, "\n***** Buggy libc nearbyintf() detected(%f != %f). *****\n\n", res, goodres);

 fesetround(old_rm);
}
#else
static void libc_rounding_test(void)
{

}
#endif

static int pow_test_sub_a(int y, double z) NO_INLINE NO_CLONE;
static int pow_test_sub_a(int y, double z)
{
 return std::min<int>(floor(pow(10, z)), std::min<int>(floor(pow(10, y)), (int)pow(10, y)));
}

static int pow_test_sub_b(int y) NO_INLINE NO_CLONE;
static int pow_test_sub_b(int y)
{
 return std::min<int>(floor(pow(2, y)), (int)pow(2, y));
}

static void pow_test(void)
{
 unsigned muller10 = 1;
 unsigned muller2 = 1;

 for(int y = 0; y < 10; y++, muller10 *= 10, muller2 <<= 1)
 {
  unsigned res10 = pow_test_sub_a(y, y);
  unsigned res2 = pow_test_sub_b(y);

  //printf("%u %u\n", res10, res2);

  assert(res10 == muller10);
  assert(res2 == muller2);
 }
}

static void RunFPTests(void)
{
 fptest0();
 fptest1();

 libc_rounding_test();
 pow_test();
}

static void NO_CLONE NO_INLINE NE1664_Test_Sub(uint16* buf)
{
 ne16_wbo_be<uint8>(buf, 3, 0xAA);
 ne16_wbo_be<uint16>(buf, 2, 0xDEAD);
 ne16_wbo_be<uint32>(buf, 0, 0xCAFEBEEF);
 ne16_wbo_be<uint32>(buf, 0, ne16_rbo_be<uint16>(buf, 2) ^ ne16_rbo_be<uint16>(buf, 0));
 ne16_wbo_be<uint16>(buf, 0, ne16_rbo_be<uint16>(buf, 0) ^ ne16_rbo_be<uint16>(buf, 2));
}

static void NO_CLONE NO_INLINE NE1664_Test_Sub64(uint64* buf)
{
 ne64_wbo_be<uint16>(buf, 0, 0x1111);
 ne64_wbo_be<uint16>(buf, 2, 0x2222);
 ne64_wbo_be<uint16>(buf, 4, 0x4444);
 ne64_wbo_be<uint16>(buf, 6, 0x6666);

 ne64_wbo_be<uint32>(buf, 0, 0x33333333);
 ne64_wbo_be<uint32>(buf, 4, 0x77777777);

 *buf += ne64_rbo_be<uint16>(buf, 0) + ne64_rbo_be<uint16>(buf, 2) + ne64_rbo_be<uint16>(buf, 4) + ne64_rbo_be<uint16>(buf, 6);
}

static void NE1664_Test(void)
{
 uint16 buf[2] = { 0, 0 };
 uint64 var64 = 0xDEADBEEFCAFEF00DULL;

 NE1664_Test_Sub(buf);
 NE1664_Test_Sub64(&var64);

 assert((buf[0] == buf[1]) && buf[0] == 0x7411);
 assert(var64 == 0x333333337778CCCBULL);
}

#ifdef WANT_TEST_ZLIB
static void zlib_test(void)
{
 auto cfl = zlibCompileFlags();

 assert((2 << ((cfl >> 0) & 0x3)) == sizeof(uInt));
 assert((2 << ((cfl >> 2) & 0x3)) == sizeof(uLong));
 assert((2 << ((cfl >> 4) & 0x3)) == sizeof(voidpf));

 #ifdef Z_LARGE64
 if((2 << ((cfl >> 6) & 0x3)) != sizeof(z_off_t))
 {
  assert(sizeof(z_off64_t) == 8);
  assert(&gztell == &gztell64);
 }
 #else
 assert((2 << ((cfl >> 6) & 0x3)) == sizeof(z_off_t));
 #endif
}
#endif

#ifdef WANT_TEST_MEMOPS
static NO_INLINE NO_CLONE void memops_test_sub(uint8* mem8)
{
 for(unsigned offs = 0; offs < 8; offs++)
 {
  for(unsigned len = 1; len < 32; len++)
  {
   memset(mem8, 0x88, 64);

   MDFN_FastArraySet(&mem8[offs], 0x55, len); 

   for(unsigned i = 0; i < 64; i++)
   {
    if(i < offs || i >= (offs + len))
     assert(mem8[i] == 0x88);
    else
     assert(mem8[i] == 0x55);
   }
  }
 }
}

static void memops_test(void)
{
 uint8 mem8[64];

 memops_test_sub(mem8);
}
#endif

static void TestLog2(void)
{
 static const struct
 {
  uint64 val;
  unsigned expected;
 } log2_test_vals[] =
 {
  { 0, 0 },
  { 1, 0 },
  { 2, 1 },
  { 3, 1 },
  { 4, 2 },
  { 5, 2 },
  { 6, 2 },
  { 7, 2 },
  { 4095, 11 },
  { 4096, 12 },
  { 4097, 12 },
  { 0x7FFE, 14 },
  { 0x7FFF, 14 },
  { 0x8000, 15 },
  { 0x8001, 15 },
  { 0xFFFF, 15 },
  { 0x7FFFFFFF, 30 },
  { 0x80000000, 31 },
  { 0xFFFFFFFF, 31 },
  { 0x7FFFFFFFFFFFFFFEULL, 62 },
  { 0x7FFFFFFFFFFFFFFFULL, 62 },
  { 0x8000000000000000ULL, 63 },
  { 0x8000000000000001ULL, 63 },
  { 0x8AAAAAAAAAAAAAAAULL, 63 },
  { 0xFFFFFFFFFFFFFFFFULL, 63 },
 };

 for(const auto& tv : log2_test_vals)
 {
  if((uint32)tv.val == tv.val)
  {
   assert(MDFN_log2((uint32)tv.val) == tv.expected);
   assert(MDFN_log2((int32)tv.val) == tv.expected);  
  }

  assert(MDFN_log2((uint64)tv.val) == tv.expected);
  assert(MDFN_log2((int64)tv.val) == tv.expected);  
 }
}

static void TestRoundPow2(void)
{
 static const struct
 {
  uint64 val;
  uint64 expected;
 } rup2_test_vals[] =
 {
  { 0, 1 },
  { 1, 1 },
  { 2, 2 },
  { 3, 4 },
  { 4, 4 },
  { 5, 8 },
  { 7, 8 },
  { 8, 8 },
  {      0x7FFF,         0x8000 },
  {      0x8000,         0x8000 },
  {      0x8001,        0x10000 },
  {     0x10000,        0x10000 },
  {     0x10001,        0x20000 },
  {  0x7FFFFFFF,     0x80000000 },
  {  0x80000000,     0x80000000 },
  {  0x80000001,    0x100000000ULL },
  { 0x100000000ULL, 0x100000000ULL },
  { 0x100000001ULL, 0x200000000ULL },
  { 0xFFFFFFFFFFFFFFFFULL, 0 },
 };

 for(auto const& tv : rup2_test_vals)
 {
  if((uint32)tv.val == tv.val)
  {
   assert(round_up_pow2((uint32)tv.val) == (uint64)tv.expected);
   assert(round_up_pow2((int32)tv.val) == (uint64)tv.expected);
  }

  assert(round_up_pow2((uint64)tv.val) == (uint64)tv.expected);
  assert(round_up_pow2((int64)tv.val) == (uint64)tv.expected);
 }

 for(unsigned i = 1; i < 64; i++)
 {
  if(i < 32)
  {
   assert(round_up_pow2((uint32)(((uint64)1 << i) + 0)) ==  ((uint64)1 << i));
   assert(round_up_pow2((uint32)(((uint64)1 << i) + 1)) == (((uint64)1 << i) << 1));
  }
  assert(round_up_pow2(((uint64)1 << i) + 0) ==  ((uint64)1 << i));
  assert(round_up_pow2(((uint64)1 << i) + 1) == (((uint64)1 << i) << 1));
 }

 assert(round_nearest_pow2((uint16)0xC000, false) ==  0x00008000U);
 assert(round_nearest_pow2( (int16)0x6000, false) ==  0x00004000U);
 assert(round_nearest_pow2( (int16)0x6000,  true) ==  0x00008000U);
 assert(round_nearest_pow2((uint16)0xC000) 	  ==  0x00010000U);
 assert(round_nearest_pow2( (int16)0xC000) 	  == 0x100000000ULL);

 for(int i = 0; i < 64; i++)
 {
  assert( round_nearest_pow2(((uint64)1 << i)                          + 0) == (((uint64)1 << i) << 0) );
  if(i > 0)
  {
   assert( round_nearest_pow2(((uint64)1 << i) + ((uint64)1 << (i - 1))    ) == (((uint64)1 << i) << 1) );
   assert( round_nearest_pow2(((uint64)1 << i) + ((uint64)1 << (i - 1)) - 1) == (((uint64)1 << i) << 0) );
  }

  assert( round_nearest_pow2(((uint64)1 << i)                          + 0, false) == (((uint64)1 << i) << 0) );
  if(i > 0)
  {
   assert( round_nearest_pow2(((uint64)1 << i) + ((uint64)1 << (i - 1)) + 1, false) == (((uint64)1 << i) << 1) );
   assert( round_nearest_pow2(((uint64)1 << i) + ((uint64)1 << (i - 1)) + 0, false) == (((uint64)1 << i) << 0) );
   assert( round_nearest_pow2(((uint64)1 << i) + ((uint64)1 << (i - 1)) - 1, false) == (((uint64)1 << i) << 0) );
  }

  {
   float tmp = i ? floor((1.0 / 2) + (float)i / (1U << MDFN_log2(i))) * (1U << MDFN_log2(i)) : 1.0;
   //printf("%d, %d %d -- %f\n", i, round_nearest_pow2(i, true), round_nearest_pow2(i, false), tmp);
   assert(tmp == round_nearest_pow2(i));
  }
 }

 #if 0
 {
  uint64 lcg = 7;
  uint64 accum = 0;

  for(unsigned i = 0; i < 256; i++, lcg = (lcg * 6364136223846793005ULL) + 1442695040888963407ULL)
   accum += round_up_pow2(lcg) * 1 + round_up_pow2((uint32)lcg) * 3 + round_up_pow2((uint16)lcg) * 5 + round_up_pow2((uint8)lcg) * 7;

  assert(accum == 0xb40001fbdb46577cULL);
 }
 #endif
}

template<typename RT, typename T, unsigned sa>
static NO_INLINE NO_CLONE RT TestCasts_Sub_L(T v)
{
	return (RT)v << sa;
}

template<typename RT, typename T, unsigned sa>
static NO_INLINE NO_CLONE RT TestCasts_Sub_R(T v)
{
	return (RT)v >> sa;
}

template<typename RT, typename T, signed sa>
static INLINE RT TestCasts_Sub(T v)
{
	if(sa < 0)
		return TestCasts_Sub_R<RT, T, (-sa) & (8 * sizeof(RT) - 1)>(v);
	else
		return TestCasts_Sub_L<RT, T, sa & (8 * sizeof(RT) - 1)>(v);
}

static void TestCastShift(void)
{
	assert((TestCasts_Sub< int64, uint8, 4>(0xEF) == 0x0000000000000EF0ULL));
	assert((TestCasts_Sub<uint64,  int8, 4>(0xEF) == 0xFFFFFFFFFFFFFEF0ULL));

	assert((TestCasts_Sub< int64, uint16, 4>(0xBEEF) == 0x00000000000BEEF0ULL));
	assert((TestCasts_Sub<uint64,  int16, 4>(0xBEEF) == 0xFFFFFFFFFFFBEEF0ULL));

	assert((TestCasts_Sub< int64, uint32, 4>(0xDEADBEEF) == 0x0000000DEADBEEF0ULL));
	assert((TestCasts_Sub<uint64,  int32, 4>(0xDEADBEEF) == 0xFFFFFFFDEADBEEF0ULL));

	assert((TestCasts_Sub< int64, uint8, 20>(0xEF) == 0x000000000EF00000ULL));
	assert((TestCasts_Sub<uint64,  int8, 20>(0xEF) == 0xFFFFFFFFFEF00000ULL));

	assert((TestCasts_Sub< int64, uint16, 20>(0xBEEF) == 0x0000000BEEF00000ULL));
	assert((TestCasts_Sub<uint64,  int16, 20>(0xBEEF) == 0xFFFFFFFBEEF00000ULL));

	assert((TestCasts_Sub< int64, uint32, 20>(0xDEADBEEF) == 0x000DEADBEEF00000ULL));
	assert((TestCasts_Sub<uint64,  int32, 20>(0xDEADBEEF) == 0xFFFDEADBEEF00000ULL));

	assert((TestCasts_Sub< int64, uint8, -4>(0xEF) == 0x000000000000000EULL));
	assert((TestCasts_Sub<uint64,  int8, -4>(0xEF) == 0x0FFFFFFFFFFFFFFEULL));

	assert((TestCasts_Sub< int64, uint16, -4>(0xBEEF) == 0x0000000000000BEEULL));
	assert((TestCasts_Sub<uint64,  int16, -4>(0xBEEF) == 0x0FFFFFFFFFFFFBEEULL));

	assert((TestCasts_Sub< int64, uint32, -4>(0xDEADBEEF) == 0x000000000DEADBEEULL));
	assert((TestCasts_Sub<uint64,  int32, -4>(0xDEADBEEF) == 0x0FFFFFFFFDEADBEEULL));

	assert((TestCasts_Sub< int64, uint8, -20>(0xEF) == 0x0000000000000000ULL));
	assert((TestCasts_Sub<uint64,  int8, -20>(0xEF) == 0x00000FFFFFFFFFFFULL));

	assert((TestCasts_Sub< int64, uint16, -20>(0xBEEF) == 0x0000000000000000ULL));
	assert((TestCasts_Sub<uint64,  int16, -20>(0xBEEF) == 0x00000FFFFFFFFFFFULL));

	assert((TestCasts_Sub< int64, uint32, -20>(0xDEADBEEF) == 0x0000000000000DEAULL));
	assert((TestCasts_Sub<uint64,  int32, -20>(0xDEADBEEF) == 0x00000FFFFFFFFDEAULL));
}

#ifdef WANT_TEST_TIME
static void Time_Test(void)
{
 {
  struct tm ut = Time::UTCTime(0);

  assert(ut.tm_sec == 0);
  assert(ut.tm_min == 0);
  assert(ut.tm_hour == 0);
  assert(ut.tm_mday == 1);
  assert(ut.tm_mon == 0);
  assert(ut.tm_year = 70);
  assert(ut.tm_wday == 4);
  assert(ut.tm_yday == 0);
  assert(ut.tm_isdst <= 0);
 }

 #ifndef WIN32
 if(sizeof(time_t) >= 8)
 #endif
 {
  struct tm ut = Time::UTCTime((int64)1 << 32);

  assert(ut.tm_sec == 16);
  assert(ut.tm_min == 28);
  assert(ut.tm_hour == 6);
  assert(ut.tm_mday == 7);
  assert(ut.tm_mon == 1);
  assert(ut.tm_year = 106);
  assert(ut.tm_wday == 0);
  assert(ut.tm_yday == 37);
  assert(ut.tm_isdst <= 0);
 }
}
#endif 

const char* MDFN_tests_stringA = "AB\0C";
const char* MDFN_tests_stringB = "AB\0CD";
const char* MDFN_tests_stringC = "AB\0X";

static void TestSStringNullChar(void)
{
 assert(MDFN_tests_stringA != MDFN_tests_stringB && MDFN_tests_stringA[3] == 'C' && MDFN_tests_stringB[4] == 'D');
 assert(MDFN_tests_stringA != MDFN_tests_stringC && MDFN_tests_stringB != MDFN_tests_stringC && MDFN_tests_stringC[3] == 'X');
}

static void TestArithRightShift(void)
{
 {
  int32 meow;

  meow = 1;
  meow >>= 1;
  assert(meow == 0);

  meow = 5;
  meow >>= 1;
  assert(meow == 2);

  meow = -1;
  meow >>= 1;
  assert(meow == -1);

  meow = -5;
  meow >>= 1;
  assert(meow == -3);

  meow = 1;
  meow /= 2;
  assert(meow == 0);

  meow = 5;
  meow /= 2;
  assert(meow == 2);

  meow = -1;
  meow /= 2;
  assert(meow == 0);

  meow = -5;
  meow /= 2;
  assert(meow == -2);

  meow = -5;
  meow = (int32)(meow + ((uint32)meow >> 31)) >> 1;
  assert(meow == -2);

  #if 0
  meow = 1 << 30;
  meow <<= 1;
  assert(meow == -2147483648);

  meow = 1 << 31;
  meow <<= 1;
  assert(meow == 0);
  #endif
 }


 // New tests added May 22, 2010 to detect MSVC compiler(and possibly other compilers) bad code generation.
 {
  uint32 test_tab[4] = { 0x2000 | 0x1000, 0x2000, 0x1000, 0x0000 };
  const uint32 result_tab[4][2] = { { 0xE, 0x7 }, { 0xE, 0x0 }, { 0x0, 0x7 }, { 0x0, 0x0 } };

  for(int i = 0; i < 4; i++)
  {
   uint32 hflip_xor;
   uint32 vflip_xor;
   uint32 bgsc;

   bgsc = test_tab[i];

   hflip_xor = ((int32)(bgsc << 18) >> 30) & 0xE;
   vflip_xor = ((int32)(bgsc << 19) >> 31) & 0x7;

   assert(hflip_xor == result_tab[i][0]);
   assert(vflip_xor == result_tab[i][1]);

   //printf("%d %d\n", hflip_xor, result_tab[i][0]);
   //printf("%d %d\n", vflip_xor, result_tab[i][1]);
  }

  uint32 lfsr = 1;

  // quick and dirty RNG(to also test non-constant-expression evaluation, at least until compilers are extremely advanced :b)
  for(int i = 0; i < 256; i++)
  {
   int feedback = ((lfsr >> 7) & 1) ^ ((lfsr >> 14) & 1);
   lfsr = ((lfsr << 1) & 0x7FFF) | feedback;
	
   uint32 hflip_xor;
   uint32 vflip_xor;
   uint32 hflip_xor_alt;
   uint32 vflip_xor_alt;
   uint32 bgsc;

   bgsc = lfsr;

   hflip_xor = ((int32)(bgsc << 18) >> 30) & 0xE;
   vflip_xor = ((int32)(bgsc << 19) >> 31) & 0x7;

   hflip_xor_alt = bgsc & 0x2000 ? 0xE : 0;
   vflip_xor_alt = bgsc & 0x1000 ? 0x7 : 0;

   assert(hflip_xor == hflip_xor_alt);
   assert(vflip_xor == vflip_xor_alt);
  }
 }
}

#ifdef WANT_TEST_THREADS
static int ThreadSafeErrno_Test_Entry(void* data)
{
 MDFN_Sem** sem = (MDFN_Sem**)data;

 errno = 0;

 MDFND_PostSem(sem[0]);
 MDFND_WaitSem(sem[1]);

 errno = 0xDEAD;
 MDFND_PostSem(sem[0]);
 return 0;
}

static void ThreadSafeErrno_Test(void)
{
 //uint64 st = Time::MonoUS();
 //
 MDFN_Sem* sem[2] = { MDFND_CreateSem(), MDFND_CreateSem() };
 MDFN_Thread* thr = MDFND_CreateThread(ThreadSafeErrno_Test_Entry, sem);

 MDFND_WaitSem(sem[0]);
 errno = 0;
 MDFND_PostSem(sem[1]);
 MDFND_WaitSem(sem[0]);
 assert(errno != 0xDEAD);
 MDFND_WaitThread(thr, nullptr);
 MDFND_DestroySem(sem[0]);
 MDFND_DestroySem(sem[1]);
 //
 //
 //
 errno = 0;
 //printf("%llu\n", (unsigned long long)Time::MonoUS() - st);
}
#endif

}

using namespace MDFN_TESTS_CPP;

#ifdef WANT_TEST_EXCEPTIONS
void MDFN_RunExceptionTests(const unsigned thread_count, const unsigned thread_delay)
{
 std::atomic_int_least32_t sv;

 if(thread_count == 1)
 {
  sv.store(-1, std::memory_order_release);
  RunExceptionTests_TEP(&sv);
 }
 else
 {
  ThreadSafeErrno_Test();
  //
  //
  std::vector<MDFN_Thread*> t;
  std::vector<int> trv;

  t.resize(thread_count);
  trv.resize(thread_count);

  sv.store(thread_count, std::memory_order_release);

  for(unsigned i = 0; i < thread_count; i++)
   t[i] = MDFND_CreateThread(RunExceptionTests_TEP, &sv);

  Time::SleepMS(thread_delay);

  sv.store(-1, std::memory_order_release);

  for(unsigned i = 0; i < thread_count; i++)
   MDFND_WaitThread(t[i], &trv[i]);

  for(unsigned i = 0; i < thread_count; i++)
   printf("%d: %d\n", i, trv[i]);
 }
}
#endif

bool MDFN_RunMathTests(void)
{
 DoSizeofTests();

 TestSStringNullChar();

 TestTypesSign();

 TestArithRightShift();

 DoAlignmentChecks();
 TestSignedOverflow();
 TestDefinedOverShift();
 TestBoolConv();
 TestNarrowConstFold();

 TestGCC60196();
 TestGCC69606();
 TestGCC70941();

 TestModTern();
 TestBWNotMask31GTZ();
 TestTernary();
 TestLLVM15470();

 TestSUCompare();

 TestSignExtend();

 DoAntiNSOBugTest();
 DoAntiNSOBugTest2014();

 #ifdef WANT_TEST_LEPACKER
 DoLEPackerTest();
 #endif

 TestLog2();

 TestRoundPow2();

 RunFPTests();

 RunMASMemTests();

 RunMiscEndianTests(0xAA010203, 0xBB030201);

 #ifdef WANT_TEST_EXCEPTIONS
 MDFN_RunExceptionTests(1, 0);
 #endif

 RunSTLTests();

 LZTZCount_Test();

 NE1664_Test();

 #ifdef WANT_TEST_HASH
 sha1_test();
 sha256_test();
 #endif

 #ifdef WANT_TEST_ZLIB
 zlib_test();
 #endif

 #ifdef WANT_TEST_MEMOPS
 memops_test();
 #endif

 #ifdef WANT_TEST_TIME
 Time_Test();
 #endif

 TestCastShift();

#if 0
// Not really a math test.
 const char *test_paths[] = { "/meow", "/meow/cow", "\\meow", "\\meow\\cow", "\\\\meow", "\\\\meow\\cow",
			      "/meow.", "/me.ow/cow.", "\\meow.", "\\me.ow\\cow.", "\\\\meow.", "\\\\meow\\cow.",
			      "/meow.txt", "/me.ow/cow.txt", "\\meow.txt", "\\me.ow\\cow.txt", "\\\\meow.txt", "\\\\meow\\cow.txt"

			      "/meow", "/meow\\cow", "\\meow", "\\meow/cow", "\\\\meow", "\\\\meow/cow",
			      "/meow.", "\\me.ow/cow.", "\\meow.", "/me.ow\\cow.", "\\\\meow.", "\\\\meow/cow.",
			      "/meow.txt", "/me.ow\\cow.txt", "\\meow.txt", "\\me.ow/cow.txt", "\\\\meow.txt", "\\\\meow/cow.txt",
			      "/bark///dog", "\\bark\\\\\\dog" };

 for(unsigned i = 0; i < sizeof(test_paths) / sizeof(const char *); i++)
 {
  std::string file_path = std::string(test_paths[i]);
  std::string dir_path;
  std::string file_base;
  std::string file_ext;

  MDFN_GetFilePathComponents(file_path, &dir_path, &file_base, &file_ext);

  printf("%s ------ dir=%s --- base=%s --- ext=%s\n", file_path.c_str(), dir_path.c_str(), file_base.c_str(), file_ext.c_str());

 }
#endif

 return(1);
}

