#ifndef f_VD2_SYSTEM_LINEARALLOC_H
#define f_VD2_SYSTEM_LINEARALLOC_H

#include <vd2/system/vdtypes.h>
#include <new>

class VDLinearAllocator {
	VDLinearAllocator(const VDLinearAllocator&) = delete;
	VDLinearAllocator& operator=(const VDLinearAllocator&) = delete;
public:
	explicit VDLinearAllocator(uint32 blockSize = 4096);
	~VDLinearAllocator();
	
	void Clear();
	void Swap(VDLinearAllocator& other);

	// Deallocates all allocated storage, and frees all but the last allocated block.
	// Very fast if only zero or one block is allocated.
	void Reset();

	void *Allocate(size_t bytes) {
		void *p = mpAllocPtr;

		bytes = (bytes + sizeof(void *) - 1) & ((size_t)0 - (size_t)sizeof(void *));

		if (mAllocLeft < bytes)
			p = AllocateSlow(bytes);
		else {
			mAllocLeft -= bytes;
			mpAllocPtr += bytes;
		}

		return p;
	}

	void *Allocate(size_t bytes, size_t align) {
		void *p = mpAllocPtr;

		const size_t alignUpSize = ((uintptr)0 - (uintptr)mpAllocPtr) & (align - 1);
		bytes = (bytes + sizeof(void *) - 1) & ((size_t)0 - (size_t)sizeof(void *));

		if (mAllocLeft < bytes + alignUpSize)
			p = AllocateSlow(bytes, align);
		else {
			p = (char *)p + alignUpSize;
			mAllocLeft -= bytes;
			mpAllocPtr += bytes;
		}

		return p;
	}

	template<class T, typename... Args>
	T *Allocate(Args&&... args) {
		return new(Allocate(sizeof(T))) T(std::forward<Args>(args)...);
	}

	bool Contains(const void *addr) const;

protected:
	void *AllocateSlow(size_t bytes);
	void *AllocateSlow(size_t bytes, size_t align);

	struct alignas(double) Block {
		Block *mpNext;
		size_t mSize;
	};

	Block *mpBlocks;
	char *mpAllocPtr;
	size_t mAllocLeft;
	size_t mBlockSize;
};

class VDFixedLinearAllocator {
public:
	VDFixedLinearAllocator(void *mem, size_t size)
		: mpAllocPtr((char *)mem)
		, mAllocLeft(size)
	{
	}

	void *Allocate(size_t bytes) {
		void *p = mpAllocPtr;

		if (mAllocLeft < bytes)
			ThrowException();

		mAllocLeft -= bytes;
		mpAllocPtr += bytes;
		return p;
	}

protected:
	void ThrowException();

	char *mpAllocPtr;
	size_t mAllocLeft;
};

#endif
