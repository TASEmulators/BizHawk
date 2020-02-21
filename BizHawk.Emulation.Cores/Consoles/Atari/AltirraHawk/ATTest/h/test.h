#ifndef f_TEST_H
#define f_TEST_H

#include <vd2/system/error.h>

typedef int (*TestFn)();

extern void AddTest(TestFn, const char *, bool autoRun);

#define DEFINE_TEST(name) int Test##name(); namespace { struct TestAutoInit_##name { TestAutoInit_##name() { AddTest(Test##name, #name, true); } } g_testAutoInit_##name; } int Test##name()
#define DEFINE_TEST_NONAUTO(name) int Test##name(); namespace { struct TestAutoInit_##name { TestAutoInit_##name() { AddTest(Test##name, #name, false); } } g_testAutoInit_##name; } int Test##name()

class AssertionException : public MyError {
public:
	AssertionException(const char *s) : MyError(s) {}
};

bool ShouldBreak();

#define TEST_ASSERT_STRINGIFY(x) TEST_ASSERT_STRINGIFY1(x)
#define TEST_ASSERT_STRINGIFY1(x) #x

#define TEST_ASSERT(condition) if (!(condition)) { ShouldBreak() ? __debugbreak() : throw AssertionException("Test assertion failed at line " TEST_ASSERT_STRINGIFY(__LINE__) ": " #condition); volatile int _x = 0; _x = 1; } else ((void)0)

#endif
