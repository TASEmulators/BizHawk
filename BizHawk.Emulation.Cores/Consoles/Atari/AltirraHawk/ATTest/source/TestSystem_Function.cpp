#include <stdafx.h>
#include <vd2/system/function.h>
#include <test.h>

#if 0
	#if defined(VD_COMPILER_MSVC) && !defined(VD_COMPILER_MSVC_CLANG)
		#define LOG_ME() ((void)puts(__FUNCTION__ "(" __FUNCSIG__ ")"))
	#else
		#define LOG_ME() ((void)printf("%s()\n", __func__))
	#endif
#else
	#define LOG_ME() ((void)0)
#endif

namespace ATTestSystemFunction {
	struct EmptyBase {};

	class BigBase {
	public:
		BigBase() {
			for(int i=0; i<4; ++i)
				mData[i] = i + 100;
		}

		~BigBase() vdnoexcept_false {
			for(int i=0; i<4; ++i) {
				TEST_ASSERT(mData[i] == i + 100);
			}
		}

		int mData[4];
	};

	template<class T = EmptyBase>
	class CopyableObject : public T {
	public:
		explicit CopyableObject(int id) : mpThis(this), mId(id) {
			++sTotal;
			LOG_ME();
		}

		CopyableObject(const CopyableObject& src) : mpThis(this), mId(src.mId) {
			++sTotal;
			LOG_ME();
		}

		~CopyableObject() vdnoexcept_false {
			TEST_ASSERT(mpThis == this);
			mpThis = nullptr;

			--sTotal;
			TEST_ASSERT(sTotal >= 0);
			LOG_ME();
		}

		CopyableObject& operator=(const CopyableObject& src) {
			TEST_ASSERT(mpThis == this);
			TEST_ASSERT(src.mpThis == this);
			
			mId = src.mId;
			return *this;
			LOG_ME();
		}

		int GetId() const {
			TEST_ASSERT(mpThis == this);
			return mId;
		}

		void *mpThis;
		int mId;

		static int sTotal;
	};

	template<class T>
	int CopyableObject<T>::sTotal = 0;

	template<class T = EmptyBase>
	class MovableObject : public T {
	public:
		explicit MovableObject(int id) : mpThis(this), mId(id) {
			++sTotal;
			LOG_ME();
		}

		MovableObject(MovableObject&& src) : mpThis(this), mId(src.mId) {
			src.mId = 0;
			++sTotal;
			LOG_ME();
		}

		MovableObject(const MovableObject& src) : mpThis(this), mId(src.mId) {
			++sTotal;
			LOG_ME();
		}

		~MovableObject() vdnoexcept_false {
			TEST_ASSERT(mpThis == this);
			mpThis = nullptr;

			--sTotal;
			TEST_ASSERT(sTotal >= 0);
			LOG_ME();
		}

		MovableObject& operator=(const MovableObject& src) vdnoexcept_false {
			TEST_ASSERT(mpThis == this);
			TEST_ASSERT(src.mpThis == this);
			
			mId = src.mId;
			return *this;
			LOG_ME();
		}

		MovableObject& operator=(const MovableObject&& src) vdnoexcept_false {
			TEST_ASSERT(&src != this);
			TEST_ASSERT(mpThis == this);
			TEST_ASSERT(src.mpThis == this);
			
			mId = src.mId;
			src.mId = 0;

			return *this;
			LOG_ME();
		}

		int GetId() const {
			TEST_ASSERT(mpThis == this);
			return mId;
		}

		void *mpThis;
		int mId;

		static int sTotal;
	};

	template<class T>
	int MovableObject<T>::sTotal = 0;
}


DEFINE_TEST(System_Function) {
	using namespace ATTestSystemFunction;
	int e = 0;

	vdfunction<void()> vfn;
	TEST_ASSERT(vfn == nullptr);
	TEST_ASSERT(nullptr == vfn);
	TEST_ASSERT(!vfn);

	vfn = []() {};
	TEST_ASSERT(vfn != nullptr);
	TEST_ASSERT(nullptr != vfn);
	TEST_ASSERT(!!vfn);

	{
		int called = 0;
		vfn = [&called]() { ++called; };

		vfn();
		TEST_ASSERT(called == 1);

		auto vfn2 = vfn;
		vfn();
		TEST_ASSERT(called == 2);

		vfn2();
		TEST_ASSERT(called == 3);
	}

	vfn = nullptr;
	TEST_ASSERT(vfn == nullptr);
	TEST_ASSERT(nullptr == vfn);
	TEST_ASSERT(!vfn);

	{
		vdfunction<int()> fn1;
		{
			int count = 0;
			auto mutableFn = [=]() mutable { return ++count; };
			vdfunction<int()> fn0(mutableFn);
			fn1.swap(fn0);
		}

		TEST_ASSERT(fn1() == 1);
		TEST_ASSERT(fn1() == 2);

		auto fn2 = fn1;
		TEST_ASSERT(fn1() == 3);
		TEST_ASSERT(fn1() == 4);
		TEST_ASSERT(fn2() == 3);
		TEST_ASSERT(fn2() == 4);
	}

	CopyableObject<>::sTotal = 0;
	{
		CopyableObject<> co1(1);
		vdfunction<int()> vfn1 = [=]() { return co1.GetId(); };

		TEST_ASSERT(vfn1() == 1);
		
		vfn1 = [=]() { return co1.GetId(); };

		TEST_ASSERT(vfn1() == 1);

		auto vfn2 = vfn1;
		vfn1 = nullptr;

		TEST_ASSERT(vfn2() == 1);

		auto vfn3 = std::move(vfn2);
		TEST_ASSERT(vfn2 == nullptr);

		TEST_ASSERT(vfn3() == 1);

		vfn3 = nullptr;
	}
	TEST_ASSERT(CopyableObject<>::sTotal == 0);

	CopyableObject<BigBase>::sTotal = 0;
	{
		CopyableObject<BigBase> co1(2);
		vdfunction<int()> vfn1 = [=]() { return co1.GetId(); };

		TEST_ASSERT(vfn1() == 2);
		
		vfn1 = [=]() { return co1.GetId(); };

		TEST_ASSERT(vfn1() == 2);

		auto vfn2 = vfn1;
		vfn1 = nullptr;

		TEST_ASSERT(vfn2() == 2);

		auto vfn3 = std::move(vfn2);
		TEST_ASSERT(vfn2 == nullptr);

		TEST_ASSERT(vfn3() == 2);

		vfn3 = nullptr;
	}
	TEST_ASSERT(CopyableObject<BigBase>::sTotal == 0);

	MovableObject<>::sTotal = 0;
	{
		MovableObject<> co1(1);
		vdfunction<int()> vfn1 = std::move([=]() { return co1.GetId(); });

		TEST_ASSERT(vfn1() == 1);
		
		vfn1 = [=]() { return co1.GetId(); };

		TEST_ASSERT(vfn1() == 1);

		auto vfn2 = vfn1;
		vfn1 = nullptr;

		TEST_ASSERT(vfn2() == 1);

		auto vfn3 = std::move(vfn2);
		TEST_ASSERT(vfn2 == nullptr);

		TEST_ASSERT(vfn3() == 1);

		vfn3 = nullptr;
	}
	TEST_ASSERT(MovableObject<>::sTotal == 0);

	MovableObject<BigBase>::sTotal = 0;
	{
		MovableObject<BigBase> co1(2);
		vdfunction<int()> vfn1 = std::move([=]() { return co1.GetId(); });

		TEST_ASSERT(vfn1() == 2);
		
		vfn1 = [=]() { return co1.GetId(); };

		TEST_ASSERT(vfn1() == 2);

		auto vfn2 = vfn1;
		vfn1 = nullptr;

		TEST_ASSERT(vfn2() == 2);

		auto vfn3 = std::move(vfn2);
		TEST_ASSERT(vfn2 == nullptr);

		TEST_ASSERT(vfn3() == 2);

		vfn3 = nullptr;
	}
	TEST_ASSERT(MovableObject<BigBase>::sTotal == 0);

	// check if we can assign and swap between the same function types with different attached objects
	CopyableObject<>::sTotal = 0;
	CopyableObject<BigBase>::sTotal = 0;
	{
		vdfunction<int()> fn1;
		vdfunction<int()> fn2;

		{
			CopyableObject<> smallObj(10);
			CopyableObject<BigBase> bigObj(11);
			fn1 = [=]() { return smallObj.GetId(); };
			fn2 = [=]() { return bigObj.GetId(); };
		}

		TEST_ASSERT(fn1() == 10);
		TEST_ASSERT(fn2() == 11);

		fn1.swap(fn2);

		TEST_ASSERT(fn1() == 11);
		TEST_ASSERT(fn2() == 10);

		fn1 = fn2;
		TEST_ASSERT(fn1() == 10);
	}
	TEST_ASSERT(CopyableObject<>::sTotal == 0);
	TEST_ASSERT(CopyableObject<BigBase>::sTotal == 0);

	return e;
}
