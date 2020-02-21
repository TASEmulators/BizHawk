#include <stdafx.h>
#include <vd2/system/error.h>
#include <vd2/system/linearalloc.h>

VDLinearAllocator::VDLinearAllocator(uint32 blockSize)
	: mpBlocks(NULL)
	, mpAllocPtr(NULL)
	, mAllocLeft(0)
	, mBlockSize(blockSize)
{
}

VDLinearAllocator::~VDLinearAllocator() {
	Clear();
}

void VDLinearAllocator::Clear() {
	Block *p = mpBlocks;

	while(p) {
		Block *next = p->mpNext;

		free(p);

		p = next;
	}

	mpBlocks = NULL;
	mpAllocPtr = NULL;
	mAllocLeft = 0;
}

void VDLinearAllocator::Swap(VDLinearAllocator& other) {
	std::swap(mpBlocks, other.mpBlocks);
	std::swap(mpAllocPtr, other.mpAllocPtr);
	std::swap(mAllocLeft, other.mAllocLeft);
	std::swap(mBlockSize, other.mBlockSize);
}

void VDLinearAllocator::Reset() {
	if (!mpBlocks)
		return;
	
	// free all but the last block
	Block *p = mpBlocks->mpNext;
	while(p) {
		Block *next = p->mpNext;

		free(p);

		p = next;
	}

	mpBlocks->mpNext = nullptr;

	size_t reclaimed = mpAllocPtr - (char *)(mpBlocks + 1);
	mpAllocPtr = (char *)(mpBlocks + 1);
	mAllocLeft += reclaimed;
}

void *VDLinearAllocator::AllocateSlow(size_t bytes) {
	Block *block;
	void *p;

	if ((bytes + bytes) >= mBlockSize) {
		block = (Block *)malloc(sizeof(Block) + bytes);

		if (!block)
			throw MyMemoryError();

		block->mSize = bytes;

        mAllocLeft = 0;

	} else {
		block = (Block *)malloc(sizeof(Block) + mBlockSize);

		if (!block)
			throw MyMemoryError();

		block->mSize = mBlockSize;

		mAllocLeft = mBlockSize - bytes;

	}

	p = block + 1;
	mpAllocPtr = (char *)p + bytes;

	block->mpNext = mpBlocks;
	mpBlocks = block;

	return p;
}

void *VDLinearAllocator::AllocateSlow(size_t bytes, size_t align) {
	if (align <= alignof(double))
		return Allocate(bytes);

	void *p = Allocate(bytes + align - 1);

	p = (char *)p + (((uintptr)0 - (uintptr)p) & (align - 1));

	return p;
}

bool VDLinearAllocator::Contains(const void *addr) const {
	for(const Block *block = mpBlocks; block; block = block->mpNext) {
		if ((uintptr)addr - (uintptr)(block + 1) < block->mSize)
			return true;
	}

	return false;
}

void VDFixedLinearAllocator::ThrowException() {
	throw MyMemoryError();
}

