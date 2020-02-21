//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2018 Avery Lee
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
//	You should have received a copy of the GNU General Public License along
//	with this program. If not, see <http://www.gnu.org/licenses/>.

#include <stdafx.h>
#include <ctype.h>
#include <at/atcore/address.h>
#include <vd2/system/vdstl.h>
#include <vd2/system/VDString.h>
#include "debuggerexp.h"
#include "cpu.h"
#include "cpumemory.h"
#include "debugger.h"
#include "antic.h"
#include "mmu.h"

namespace {
	void FreeNodes(vdfastvector<ATDebugExpNode *>& nodes) {
		while(!nodes.empty()) {
			delete nodes.back();
			nodes.pop_back();
		}
	}

	enum {
		kNodePrecTernary,
		kNodePrecOr,
		kNodePrecAnd,
		kNodePrecBitOr,
		kNodePrecBitXor,
		kNodePrecBitAnd,
		kNodePrecRel,
		kNodePrecAdd,
		kNodePrecMul,
		kNodePrecUnary
	};

	uint32 AdjustAddress(uint32 addr) {
		// Correct for underflow from CPU space.
		if (addr >= 0xFF800000)
			addr = (addr + 0x800000) & 0xFFFFFF;

		return addr;
	}
};

///////////////////////////////////////////////////////////////////////////

const ATCPUExecState *ATDebugExpEvalCache::GetExecState(const ATDebugExpEvalContext& ctx) {
	if (!mbExecStateValid) {
		if (!ctx.mpTarget)
			return nullptr;

		mbExecStateValid = true;
		mExecMode = ctx.mpTarget->GetDisasmMode();

		ctx.mpTarget->GetExecState(mExecState);
	}

	return &mExecState;
}

bool ATDebugExpNode::Evaluate(sint32& result, const ATDebugExpEvalContext& context) const {
	ATDebugExpEvalCache cache;

	return Evaluate(result, context, cache);
}

///////////////////////////////////////////////////////////////////////////

class ATDebugExpNodeConst final : public ATDebugExpNode {
public:
	ATDebugExpNodeConst(sint32 v, bool hex, bool addr)
		: ATDebugExpNode(kATDebugExpNodeType_Const)
		, mVal(v)
		, mbHex(hex)
		, mbAddress(addr)
	{
	}

	bool IsHex() const { return mbHex; }
	bool IsAddress() const { return mbAddress; }
	sint32 GetValue() const { return mVal; }

	ATDebugExpNode *Clone() const override { return new ATDebugExpNodeConst(mVal, mbHex, mbAddress); }

	bool Evaluate(sint32& result, const ATDebugExpEvalContext& context, ATDebugExpEvalCache& cache) const {
		result = mVal;
		return true;
	}

	void ToString(VDStringA& s, int prec) {
		sint32 v = mVal;

		if (mbAddress) {
			s += ATAddressGetSpacePrefix((uint32)v);

			v &= kATAddressOffsetMask;
		}

		s.append_sprintf(mbHex ? mVal >= 0x100 ? mVal >= 0x10000 ? "$%08X" : "$%04X" : "$%02X" : "%d", mVal);
	}

protected:
	const sint32 mVal;
	const bool mbHex;
	const bool mbAddress;
};

///////////////////////////////////////////////////////////////////////////

class ATDebugExpNodeUnary : public ATDebugExpNode {
public:
	ATDebugExpNodeUnary(ATDebugExpNodeType type, ATDebugExpNode *arg)
		: ATDebugExpNode(type)
		, mpArg(arg)
	{
	}

	bool Optimize(ATDebugExpNode **result) {
		ATDebugExpNode *newArg;

		if (mpArg->Optimize(&newArg))
			mpArg = newArg;

		if (mpArg->mType == kATDebugExpNodeType_Const) {
			sint32 v;

			if (Evaluate(v, ATDebugExpEvalContext())) {
				*result = new ATDebugExpNodeConst(v, static_cast<ATDebugExpNodeConst *>(&*mpArg)->IsHex(), false);
				return true;
			}
		}

		return false;	
	}

protected:
	void ToString(VDStringA& s, int prec) {
		EmitUnaryOp(s);
		mpArg->ToString(s, kNodePrecUnary);
	}

	virtual void EmitUnaryOp(VDStringA& s) = 0;

	template<class T>
	ATDebugExpNode *CloneUnary() const {
		vdautoptr<ATDebugExpNode> arg(mpArg->Clone());

		ATDebugExpNode *result = new typename std::remove_const<typename std::remove_reference<T>::type>::type(arg);
		arg.release();

		return result;
	}

	vdautoptr<ATDebugExpNode> mpArg;
};

///////////////////////////////////////////////////////////////////////////

class ATDebugExpNodeNegate final : public ATDebugExpNodeUnary {
public:
	ATDebugExpNodeNegate(ATDebugExpNode *x)
		: ATDebugExpNodeUnary(kATDebugExpNodeType_Negate, x)
	{
	}

	ATDebugExpNode *Clone() const override { return CloneUnary<decltype(*this)>(); }

	bool Evaluate(sint32& result, const ATDebugExpEvalContext& context, ATDebugExpEvalCache& cache) const {
		sint32 x;

		if (!mpArg->Evaluate(x, context, cache))
			return false;

		result = -x;
		return true;
	}

	void EmitUnaryOp(VDStringA& s) {
		s += '-';
	}
};

///////////////////////////////////////////////////////////////////////////

class ATDebugExpNodeBinary : public ATDebugExpNode {
public:
	ATDebugExpNodeBinary(ATDebugExpNodeType type, ATDebugExpNode *left, ATDebugExpNode *right, bool inheritAddress)
		: ATDebugExpNode(type)
		, mpLeft(left)
		, mpRight(right)
		, mbAddress(inheritAddress && (left->IsAddress() || right->IsAddress()))
	{
	}

	bool IsAddress() const {
		return mbAddress;
	}

	bool Optimize(ATDebugExpNode **result) {
		ATDebugExpNode *newArg;
		if (mpLeft->Optimize(&newArg))
			mpLeft = newArg;

		if (mpRight->Optimize(&newArg))
			mpRight = newArg;

		if (mpLeft->mType == kATDebugExpNodeType_Const && mpRight->mType == kATDebugExpNodeType_Const) {
			sint32 v;

			if (Evaluate(v, ATDebugExpEvalContext())) {
				*result = new ATDebugExpNodeConst(v,
					static_cast<ATDebugExpNodeConst *>(&*mpLeft)->IsHex()
					|| static_cast<ATDebugExpNodeConst *>(&*mpRight)->IsHex(),
					mbAddress
					);
				return true;
			}
		}

		return false;	
	}

protected:
	void ToString(VDStringA& s, int prec) {
		int thisprec = GetPrecedence();
		int assoc = GetAssociativity();

		if (prec > thisprec)
			s += '(';

		mpLeft->ToString(s, assoc < 0 ? thisprec+1 : thisprec);

		EmitBinaryOp(s);

		mpRight->ToString(s, assoc > 0 ? thisprec+1 : thisprec);

		if (prec > thisprec)
			s += ')';
	}

	virtual int GetAssociativity() const { return -1; }
	virtual int GetPrecedence() const = 0;
	virtual void EmitBinaryOp(VDStringA& s) = 0;

	template<class T>
	ATDebugExpNode *CloneBinary() const {
		vdautoptr<ATDebugExpNode> l(mpLeft->Clone());
		vdautoptr<ATDebugExpNode> r(mpRight->Clone());

		ATDebugExpNode *result = new typename std::remove_const<typename std::remove_reference<T>::type>::type(l, r);
		l.release();
		r.release();

		return result;
	}

	vdautoptr<ATDebugExpNode> mpLeft;
	vdautoptr<ATDebugExpNode> mpRight;
	bool mbAddress;
};

///////////////////////////////////////////////////////////////////////////

class ATDebugExpNodeAnd final : public ATDebugExpNodeBinary {
public:
	ATDebugExpNodeAnd(ATDebugExpNode *x, ATDebugExpNode *y)
		: ATDebugExpNodeBinary(kATDebugExpNodeType_And, x, y, false)
	{
	}

	ATDebugExpNode *Clone() const override { return CloneBinary<decltype(*this)>(); }

	bool Optimize(ATDebugExpNode **result) {
		if (ATDebugExpNodeBinary::Optimize(result))
			return true;

		if (mpLeft->mType == kATDebugExpNodeType_Const) {
			sint32 v;

			if (mpLeft->Evaluate(v, ATDebugExpEvalContext()) && !v) {
				*result = new ATDebugExpNodeConst(0, false, false);
				return true;
			}
		}

		if (mpRight->mType == kATDebugExpNodeType_Const) {
			sint32 v;

			if (mpRight->Evaluate(v, ATDebugExpEvalContext()) && !v) {
				*result = new ATDebugExpNodeConst(0, false, false);
				return true;
			}
		}

		return false;
	}

	bool Evaluate(sint32& result, const ATDebugExpEvalContext& context, ATDebugExpEvalCache& cache) const {
		sint32 x;
		sint32 y;

		if (!mpLeft->Evaluate(x, context, cache) ||
			!mpRight->Evaluate(y, context, cache))
			return false;

		result = x && y;
		return true;
	}

	bool ExtractEqConst(ATDebugExpNodeType type, ATDebugExpNode **extracted, ATDebugExpNode **remainder) {
		vdautoptr<ATDebugExpNode> rem;

		if (mpLeft->ExtractEqConst(type, extracted, ~rem)) {
			if (rem) {
				*remainder = new ATDebugExpNodeAnd(rem, mpRight);
				rem.release();
			} else
				*remainder = mpRight;

			mpLeft.reset();
			mpRight.release();
			return true;
		}

		if (mpRight->ExtractEqConst(type, extracted, ~rem)) {
			if (rem) {
				*remainder = new ATDebugExpNodeAnd(mpLeft, rem);
				rem.release();
			} else
				*remainder = mpLeft;

			mpLeft.release();
			mpRight.reset();
			return true;
		}

		return false;
	}

	bool ExtractRelConst(ATDebugExpNodeType type, ATDebugExpNode **extracted, ATDebugExpNode **remainder, ATDebugExpNodeType *relop) {
		vdautoptr<ATDebugExpNode> rem;

		if (mpLeft->ExtractRelConst(type, extracted, ~rem, relop)) {
			if (rem) {
				*remainder = new ATDebugExpNodeAnd(rem, mpRight);
				rem.release();
			} else
				*remainder = mpRight;

			mpLeft.reset();
			mpRight.release();
			return true;
		}

		if (mpRight->ExtractRelConst(type, extracted, ~rem, relop)) {
			if (rem) {
				*remainder = new ATDebugExpNodeAnd(mpLeft, rem);
				rem.release();
			} else
				*remainder = mpLeft;

			mpLeft.release();
			mpRight.reset();
			return true;
		}

		return false;
	}

	bool OptimizeInvert(ATDebugExpNode **result);

	bool CanOptimizeInvert() const {
		return mpLeft->CanOptimizeInvert() && mpRight->CanOptimizeInvert();
	}

	int GetAssociativity() const { return 0; }
	int GetPrecedence() const { return kNodePrecAnd; }
	void EmitBinaryOp(VDStringA& s) {
		s += " and ";
	}
};

///////////////////////////////////////////////////////////////////////////

class ATDebugExpNodeOr final : public ATDebugExpNodeBinary {
public:
	ATDebugExpNodeOr(ATDebugExpNode *x, ATDebugExpNode *y)
		: ATDebugExpNodeBinary(kATDebugExpNodeType_Or, x, y, false)
	{
	}
	
	ATDebugExpNode *Clone() const override { return CloneBinary<decltype(*this)>(); }

	bool Evaluate(sint32& result, const ATDebugExpEvalContext& context, ATDebugExpEvalCache& cache) const {
		sint32 x;
		sint32 y;

		if (!mpLeft->Evaluate(x, context, cache) ||
			!mpRight->Evaluate(y, context, cache))
			return false;

		result = x || y;
		return true;
	}

	bool Optimize(ATDebugExpNode **result) {
		if (ATDebugExpNodeBinary::Optimize(result))
			return true;

		if (mpLeft->mType == kATDebugExpNodeType_Const) {
			sint32 v;

			if (mpLeft->Evaluate(v, ATDebugExpEvalContext()) && v) {
				*result = new ATDebugExpNodeConst(1, false, false);
				return true;
			}
		}

		if (mpRight->mType == kATDebugExpNodeType_Const) {
			sint32 v;

			if (mpRight->Evaluate(v, ATDebugExpEvalContext()) && v) {
				*result = new ATDebugExpNodeConst(1, false, false);
				return true;
			}
		}

		return false;
	}

	bool OptimizeInvert(ATDebugExpNode **result);

	bool CanOptimizeInvert() const {
		return mpLeft->CanOptimizeInvert() && mpRight->CanOptimizeInvert();
	}

	int GetAssociativity() const { return 0; }
	int GetPrecedence() const { return kNodePrecOr; }
	void EmitBinaryOp(VDStringA& s) {
		s += " or ";
	}
};

///////////////////////////////////////////////////////////////////////////

bool ATDebugExpNodeAnd::OptimizeInvert(ATDebugExpNode **result) {
	if (!CanOptimizeInvert())
		return false;


	ATDebugExpNode *newNode;
	VDVERIFY(mpLeft->OptimizeInvert(&newNode));
	mpLeft = newNode;

	VDVERIFY(mpRight->OptimizeInvert(&newNode));
	mpRight = newNode;

	*result = new ATDebugExpNodeOr(mpLeft, mpRight);
	mpLeft.release();
	mpRight.release();

	return true;
}

bool ATDebugExpNodeOr::OptimizeInvert(ATDebugExpNode **result) {
	if (!CanOptimizeInvert())
		return false;

	ATDebugExpNode *newNode;
	VDVERIFY(mpLeft->OptimizeInvert(&newNode));
	mpLeft = newNode;

	VDVERIFY(mpRight->OptimizeInvert(&newNode));
	mpRight = newNode;

	*result = new ATDebugExpNodeAnd(mpLeft, mpRight);
	mpLeft.release();
	mpRight.release();

	return true;
}

///////////////////////////////////////////////////////////////////////////

class ATDebugExpNodeBitwiseAnd final : public ATDebugExpNodeBinary {
public:
	ATDebugExpNodeBitwiseAnd(ATDebugExpNode *x, ATDebugExpNode *y)
		: ATDebugExpNodeBinary(kATDebugExpNodeType_BitwiseAnd, x, y, true)
	{
	}
	
	ATDebugExpNode *Clone() const override { return CloneBinary<decltype(*this)>(); }

	bool Evaluate(sint32& result, const ATDebugExpEvalContext& context, ATDebugExpEvalCache& cache) const {
		sint32 x;
		sint32 y;

		if (!mpLeft->Evaluate(x, context, cache) ||
			!mpRight->Evaluate(y, context, cache))
			return false;

		result = x & y;
		return true;
	}

	int GetAssociativity() const { return 0; }
	int GetPrecedence() const { return kNodePrecBitAnd; }
	void EmitBinaryOp(VDStringA& s) {
		s += " & ";
	}
};

///////////////////////////////////////////////////////////////////////////

class ATDebugExpNodeBitwiseOr final : public ATDebugExpNodeBinary {
public:
	ATDebugExpNodeBitwiseOr(ATDebugExpNode *x, ATDebugExpNode *y)
		: ATDebugExpNodeBinary(kATDebugExpNodeType_BitwiseOr, x, y, true)
	{
	}

	ATDebugExpNode *Clone() const override { return CloneBinary<decltype(*this)>(); }

	bool Evaluate(sint32& result, const ATDebugExpEvalContext& context, ATDebugExpEvalCache& cache) const {
		sint32 x;
		sint32 y;

		if (!mpLeft->Evaluate(x, context, cache) ||
			!mpRight->Evaluate(y, context, cache))
			return false;

		result = x | y;
		return true;
	}

	int GetAssociativity() const { return 0; }
	int GetPrecedence() const { return kNodePrecBitOr; }
	void EmitBinaryOp(VDStringA& s) {
		s += " | ";
	}
};

///////////////////////////////////////////////////////////////////////////

class ATDebugExpNodeBitwiseXor final : public ATDebugExpNodeBinary {
public:
	ATDebugExpNodeBitwiseXor(ATDebugExpNode *x, ATDebugExpNode *y)
		: ATDebugExpNodeBinary(kATDebugExpNodeType_BitwiseXor, x, y, true)
	{
	}

	ATDebugExpNode *Clone() const override { return CloneBinary<decltype(*this)>(); }

	bool Evaluate(sint32& result, const ATDebugExpEvalContext& context, ATDebugExpEvalCache& cache) const {
		sint32 x;
		sint32 y;

		if (!mpLeft->Evaluate(x, context, cache) ||
			!mpRight->Evaluate(y, context, cache))
			return false;

		result = x ^ y;
		return true;
	}

	int GetAssociativity() const { return 0; }
	int GetPrecedence() const { return kNodePrecBitXor; }
	void EmitBinaryOp(VDStringA& s) {
		s += " ^ ";
	}
};

///////////////////////////////////////////////////////////////////////////

class ATDebugExpNodeAdd final : public ATDebugExpNodeBinary {
public:
	ATDebugExpNodeAdd(ATDebugExpNode *x, ATDebugExpNode *y)
		: ATDebugExpNodeBinary(kATDebugExpNodeType_Add, x, y, true)
	{
	}

	ATDebugExpNode *Clone() const override { return CloneBinary<decltype(*this)>(); }

	bool Evaluate(sint32& result, const ATDebugExpEvalContext& context, ATDebugExpEvalCache& cache) const override {
		sint32 x;
		sint32 y;

		if (!mpLeft->Evaluate(x, context, cache) ||
			!mpRight->Evaluate(y, context, cache))
			return false;

		result = x + y;
		return true;
	}

	bool Optimize(ATDebugExpNode **result) override {
		if (ATDebugExpNodeBinary::Optimize(result))
			return true;

		if (mpLeft->mType == kATDebugExpNodeType_Const) {
			sint32 v = static_cast<ATDebugExpNodeConst *>(&*mpLeft)->GetValue();

			if (v == 0) {
				*result = mpRight.release();
				return true;
			}
		}

		if (mpRight->mType == kATDebugExpNodeType_Const) {
			sint32 v = static_cast<ATDebugExpNodeConst *>(&*mpRight)->GetValue();

			if (v == 0) {
				*result = mpLeft.release();
				return true;
			}
		}

		return false;
	}

	int GetAssociativity() const { return 0; }
	int GetPrecedence() const { return kNodePrecAdd; }
	void EmitBinaryOp(VDStringA& s) {
		s += '+';
	}
};

///////////////////////////////////////////////////////////////////////////

class ATDebugExpNodeSub final : public ATDebugExpNodeBinary {
public:
	ATDebugExpNodeSub(ATDebugExpNode *x, ATDebugExpNode *y)
		: ATDebugExpNodeBinary(kATDebugExpNodeType_Sub, x, y, true)
	{
	}

	ATDebugExpNode *Clone() const override { return CloneBinary<decltype(*this)>(); }

	bool Evaluate(sint32& result, const ATDebugExpEvalContext& context, ATDebugExpEvalCache& cache) const override {
		sint32 x;
		sint32 y;

		if (!mpLeft->Evaluate(x, context, cache) ||
			!mpRight->Evaluate(y, context, cache))
			return false;

		result = x - y;
		return true;
	}

	bool Optimize(ATDebugExpNode **result) override {
		if (ATDebugExpNodeBinary::Optimize(result))
			return true;

		if (mpLeft->mType == kATDebugExpNodeType_Const) {
			sint32 v = static_cast<ATDebugExpNodeConst *>(&*mpLeft)->GetValue();

			if (v == 0) {
				*result = new ATDebugExpNodeNegate(mpRight);
				mpRight.release();
				return true;
			}
		}

		if (mpRight->mType == kATDebugExpNodeType_Const) {
			sint32 v = static_cast<ATDebugExpNodeConst *>(&*mpRight)->GetValue();

			if (v == 0) {
				*result = mpLeft.release();
				return true;
			}
		}

		return false;
	}

	int GetPrecedence() const { return kNodePrecAdd; }
	void EmitBinaryOp(VDStringA& s) {
		s += '-';
	}
};

///////////////////////////////////////////////////////////////////////////

class ATDebugExpNodeMul final : public ATDebugExpNodeBinary {
public:
	ATDebugExpNodeMul(ATDebugExpNode *x, ATDebugExpNode *y)
		: ATDebugExpNodeBinary(kATDebugExpNodeType_Mul, x, y, true)
	{
	}

	ATDebugExpNode *Clone() const override { return CloneBinary<decltype(*this)>(); }

	bool Evaluate(sint32& result, const ATDebugExpEvalContext& context, ATDebugExpEvalCache& cache) const {
		sint32 x;
		sint32 y;

		if (!mpLeft->Evaluate(x, context, cache) ||
			!mpRight->Evaluate(y, context, cache))
			return false;

		result = x * y;
		return true;
	}

	bool Optimize(ATDebugExpNode **result) override {
		if (ATDebugExpNodeBinary::Optimize(result))
			return true;

		if (mpLeft->mType == kATDebugExpNodeType_Const) {
			sint32 v = static_cast<ATDebugExpNodeConst *>(&*mpLeft)->GetValue();

			if (v == 0) {
				*result = mpLeft.release();
				return true;
			}

			if (v == 1) {
				*result = mpRight.release();
				return true;
			}
		}

		if (mpRight->mType == kATDebugExpNodeType_Const) {
			sint32 v = static_cast<ATDebugExpNodeConst *>(&*mpRight)->GetValue();

			if (v == 0) {
				*result = mpRight.release();
				return true;
			}

			if (v == 1) {
				*result = mpLeft.release();
				return true;
			}
		}

		return false;
	}

	int GetAssociativity() const { return 0; }
	int GetPrecedence() const { return kNodePrecMul; }
	void EmitBinaryOp(VDStringA& s) {
		s += '*';
	}
};

///////////////////////////////////////////////////////////////////////////

class ATDebugExpNodeMod final : public ATDebugExpNodeBinary {
public:
	ATDebugExpNodeMod(ATDebugExpNode *x, ATDebugExpNode *y)
		: ATDebugExpNodeBinary(kATDebugExpNodeType_Div, x, y, false)
	{
	}

	ATDebugExpNode *Clone() const override { return CloneBinary<decltype(*this)>(); }

	bool Evaluate(sint32& result, const ATDebugExpEvalContext& context, ATDebugExpEvalCache& cache) const {
		sint32 x;
		sint32 y;

		if (!mpLeft->Evaluate(x, context, cache) ||
			!mpRight->Evaluate(y, context, cache))
			return false;

		if (!y)
			return false;

		result = x % y;
		return true;
	}

	int GetPrecedence() const { return kNodePrecMul; }
	void EmitBinaryOp(VDStringA& s) {
		s += '%';
	}
};

///////////////////////////////////////////////////////////////////////////

class ATDebugExpNodeDiv final : public ATDebugExpNodeBinary {
public:
	ATDebugExpNodeDiv(ATDebugExpNode *x, ATDebugExpNode *y)
		: ATDebugExpNodeBinary(kATDebugExpNodeType_Div, x, y, false)
	{
	}

	ATDebugExpNode *Clone() const override { return CloneBinary<decltype(*this)>(); }

	bool Evaluate(sint32& result, const ATDebugExpEvalContext& context, ATDebugExpEvalCache& cache) const {
		sint32 x;
		sint32 y;

		if (!mpLeft->Evaluate(x, context, cache) ||
			!mpRight->Evaluate(y, context, cache))
			return false;

		if (!y)
			return false;

		// suppress integer overflow exception
		if (x == -0x7FFFFFFF-1 && y == -1)
			result = x;
		else
			result = x / y;

		return true;
	}

	int GetPrecedence() const { return kNodePrecMul; }
	void EmitBinaryOp(VDStringA& s) {
		s += '/';
	}
};

///////////////////////////////////////////////////////////////////////////

class ATDebugExpNodeLT final : public ATDebugExpNodeBinary {
public:
	ATDebugExpNodeLT(ATDebugExpNode *x, ATDebugExpNode *y)
		: ATDebugExpNodeBinary(kATDebugExpNodeType_LT, x, y, false)
	{
	}

	ATDebugExpNode *Clone() const override { return CloneBinary<decltype(*this)>(); }

	bool Evaluate(sint32& result, const ATDebugExpEvalContext& context, ATDebugExpEvalCache& cache) const {
		sint32 x;
		sint32 y;

		if (!mpLeft->Evaluate(x, context, cache) ||
			!mpRight->Evaluate(y, context, cache))
			return false;

		result = x < y;
		return true;
	}

	bool ExtractRelConst(ATDebugExpNodeType type, ATDebugExpNode **extracted, ATDebugExpNode **remainder, ATDebugExpNodeType *relop) {
		if (mpLeft->mType == type && mpRight->mType == kATDebugExpNodeType_Const) {
			*remainder = NULL;
			*extracted = mpRight;
			*relop = kATDebugExpNodeType_LT;
			mpLeft.reset();
			mpRight.release();
			return true;
		}

		if (mpRight->mType == type && mpLeft->mType == kATDebugExpNodeType_Const) {
			*remainder = NULL;
			*extracted = mpLeft;
			*relop = kATDebugExpNodeType_GT;
			mpLeft.release();
			mpRight.reset();
			return true;
		}

		return false;
	}

	bool OptimizeInvert(ATDebugExpNode **result);
	bool CanOptimizeInvert() const { return true; }

	int GetPrecedence() const { return kNodePrecRel; }
	void EmitBinaryOp(VDStringA& s) {
		s += '<';
	}
};

///////////////////////////////////////////////////////////////////////////

class ATDebugExpNodeLE final : public ATDebugExpNodeBinary {
public:
	ATDebugExpNodeLE(ATDebugExpNode *x, ATDebugExpNode *y)
		: ATDebugExpNodeBinary(kATDebugExpNodeType_LE, x, y, false)
	{
	}

	ATDebugExpNode *Clone() const override { return CloneBinary<decltype(*this)>(); }

	bool Evaluate(sint32& result, const ATDebugExpEvalContext& context, ATDebugExpEvalCache& cache) const {
		sint32 x;
		sint32 y;

		if (!mpLeft->Evaluate(x, context, cache) ||
			!mpRight->Evaluate(y, context, cache))
			return false;

		result = x <= y;
		return true;
	}

	bool ExtractRelConst(ATDebugExpNodeType type, ATDebugExpNode **extracted, ATDebugExpNode **remainder, ATDebugExpNodeType *relop) {
		if (mpLeft->mType == type && mpRight->mType == kATDebugExpNodeType_Const) {
			*remainder = NULL;
			*extracted = mpRight;
			*relop = kATDebugExpNodeType_LE;
			mpLeft.reset();
			mpRight.release();
			return true;
		}

		if (mpRight->mType == type && mpLeft->mType == kATDebugExpNodeType_Const) {
			*remainder = NULL;
			*extracted = mpLeft;
			*relop = kATDebugExpNodeType_GE;
			mpLeft.release();
			mpRight.reset();
			return true;
		}

		return false;
	}

	bool OptimizeInvert(ATDebugExpNode **result);
	bool CanOptimizeInvert() const { return true; }

	int GetPrecedence() const { return kNodePrecRel; }
	void EmitBinaryOp(VDStringA& s) {
		s += "<=";
	}
};

///////////////////////////////////////////////////////////////////////////

class ATDebugExpNodeGT final : public ATDebugExpNodeBinary {
public:
	ATDebugExpNodeGT(ATDebugExpNode *x, ATDebugExpNode *y)
		: ATDebugExpNodeBinary(kATDebugExpNodeType_GT, x, y, false)
	{
	}

	ATDebugExpNode *Clone() const override { return CloneBinary<decltype(*this)>(); }

	bool Evaluate(sint32& result, const ATDebugExpEvalContext& context, ATDebugExpEvalCache& cache) const {
		sint32 x;
		sint32 y;

		if (!mpLeft->Evaluate(x, context, cache) ||
			!mpRight->Evaluate(y, context, cache))
			return false;

		result = x > y;
		return true;
	}

	bool ExtractRelConst(ATDebugExpNodeType type, ATDebugExpNode **extracted, ATDebugExpNode **remainder, ATDebugExpNodeType *relop) {
		if (mpLeft->mType == type && mpRight->mType == kATDebugExpNodeType_Const) {
			*remainder = NULL;
			*extracted = mpRight;
			*relop = kATDebugExpNodeType_GT;
			mpLeft.reset();
			mpRight.release();
			return true;
		}

		if (mpRight->mType == type && mpLeft->mType == kATDebugExpNodeType_Const) {
			*remainder = NULL;
			*extracted = mpLeft;
			*relop = kATDebugExpNodeType_LT;
			mpLeft.release();
			mpRight.reset();
			return true;
		}

		return false;
	}

	bool OptimizeInvert(ATDebugExpNode **result);
	bool CanOptimizeInvert() const { return true; }

	int GetPrecedence() const { return kNodePrecRel; }
	void EmitBinaryOp(VDStringA& s) {
		s += '>';
	}
};

///////////////////////////////////////////////////////////////////////////

class ATDebugExpNodeGE final : public ATDebugExpNodeBinary {
public:
	ATDebugExpNodeGE(ATDebugExpNode *x, ATDebugExpNode *y)
		: ATDebugExpNodeBinary(kATDebugExpNodeType_GE, x, y, false)
	{
	}

	ATDebugExpNode *Clone() const override { return CloneBinary<decltype(*this)>(); }

	bool Evaluate(sint32& result, const ATDebugExpEvalContext& context, ATDebugExpEvalCache& cache) const {
		sint32 x;
		sint32 y;

		if (!mpLeft->Evaluate(x, context, cache) ||
			!mpRight->Evaluate(y, context, cache))
			return false;

		result = x >= y;
		return true;
	}

	bool ExtractRelConst(ATDebugExpNodeType type, ATDebugExpNode **extracted, ATDebugExpNode **remainder, ATDebugExpNodeType *relop) {
		if (mpLeft->mType == type && mpRight->mType == kATDebugExpNodeType_Const) {
			*remainder = NULL;
			*extracted = mpRight;
			*relop = kATDebugExpNodeType_GE;
			mpLeft.reset();
			mpRight.release();
			return true;
		}

		if (mpRight->mType == type && mpLeft->mType == kATDebugExpNodeType_Const) {
			*remainder = NULL;
			*extracted = mpLeft;
			*relop = kATDebugExpNodeType_LE;
			mpLeft.release();
			mpRight.reset();
			return true;
		}

		return false;
	}

	bool OptimizeInvert(ATDebugExpNode **result);
	bool CanOptimizeInvert() const { return true; }

	int GetPrecedence() const { return kNodePrecRel; }
	void EmitBinaryOp(VDStringA& s) {
		s += ">=";
	}
};

///////////////////////////////////////////////////////////////////////////

class ATDebugExpNodeEQ final : public ATDebugExpNodeBinary {
public:
	ATDebugExpNodeEQ(ATDebugExpNode *x, ATDebugExpNode *y)
		: ATDebugExpNodeBinary(kATDebugExpNodeType_EQ, x, y, false)
	{
	}

	ATDebugExpNode *Clone() const override { return CloneBinary<decltype(*this)>(); }

	bool Evaluate(sint32& result, const ATDebugExpEvalContext& context, ATDebugExpEvalCache& cache) const {
		sint32 x;
		sint32 y;

		if (!mpLeft->Evaluate(x, context, cache) ||
			!mpRight->Evaluate(y, context, cache))
			return false;

		result = x == y;
		return true;
	}

	bool ExtractEqConst(ATDebugExpNodeType type, ATDebugExpNode **extracted, ATDebugExpNode **remainder) {
		if (mpLeft->mType == type && mpRight->mType == kATDebugExpNodeType_Const) {
			*remainder = NULL;
			*extracted = mpRight;
			mpLeft.reset();
			mpRight.release();
			return true;
		}

		if (mpRight->mType == type && mpLeft->mType == kATDebugExpNodeType_Const) {
			*remainder = NULL;
			*extracted = mpLeft;
			mpLeft.release();
			mpRight.reset();
			return true;
		}

		return false;
	}

	bool OptimizeInvert(ATDebugExpNode **result);
	bool CanOptimizeInvert() const { return true; }

	int GetPrecedence() const { return kNodePrecRel; }
	void EmitBinaryOp(VDStringA& s) {
		s += '=';
	}
};

///////////////////////////////////////////////////////////////////////////

class ATDebugExpNodeNE final : public ATDebugExpNodeBinary {
public:
	ATDebugExpNodeNE(ATDebugExpNode *x, ATDebugExpNode *y)
		: ATDebugExpNodeBinary(kATDebugExpNodeType_NE, x, y, false)
	{
	}

	ATDebugExpNode *Clone() const override { return CloneBinary<decltype(*this)>(); }

	bool Evaluate(sint32& result, const ATDebugExpEvalContext& context, ATDebugExpEvalCache& cache) const {
		sint32 x;
		sint32 y;

		if (!mpLeft->Evaluate(x, context, cache) ||
			!mpRight->Evaluate(y, context, cache))
			return false;

		result = x != y;
		return true;
	}

	bool OptimizeInvert(ATDebugExpNode **result);
	bool CanOptimizeInvert() const { return true; }

	int GetPrecedence() const { return kNodePrecRel; }
	void EmitBinaryOp(VDStringA& s) {
		s += "!=";
	}
};

///////////////////////////////////////////////////////////////////////////

bool ATDebugExpNodeLT::OptimizeInvert(ATDebugExpNode **result) {
	*result = new ATDebugExpNodeGE(mpLeft, mpRight);
	mpLeft.release();
	mpRight.release();
	return true;
}

bool ATDebugExpNodeLE::OptimizeInvert(ATDebugExpNode **result) {
	*result = new ATDebugExpNodeGT(mpLeft, mpRight);
	mpLeft.release();
	mpRight.release();
	return true;
}

bool ATDebugExpNodeGT::OptimizeInvert(ATDebugExpNode **result) {
	*result = new ATDebugExpNodeLE(mpLeft, mpRight);
	mpLeft.release();
	mpRight.release();
	return true;
}

bool ATDebugExpNodeGE::OptimizeInvert(ATDebugExpNode **result) {
	*result = new ATDebugExpNodeLT(mpLeft, mpRight);
	mpLeft.release();
	mpRight.release();
	return true;
}

bool ATDebugExpNodeEQ::OptimizeInvert(ATDebugExpNode **result) {
	*result = new ATDebugExpNodeNE(mpLeft, mpRight);
	mpLeft.release();
	mpRight.release();
	return true;
}

bool ATDebugExpNodeNE::OptimizeInvert(ATDebugExpNode **result) {
	*result = new ATDebugExpNodeEQ(mpLeft, mpRight);
	mpLeft.release();
	mpRight.release();
	return true;
}

///////////////////////////////////////////////////////////////////////////

class ATDebugExpNodeInvert final : public ATDebugExpNodeUnary {
public:
	ATDebugExpNodeInvert(ATDebugExpNode *x)
		: ATDebugExpNodeUnary(kATDebugExpNodeType_Invert, x)
	{
	}

	ATDebugExpNode *Clone() const override { return CloneUnary<decltype(*this)>(); }

	bool Optimize(ATDebugExpNode **result) {
		if (mpArg->OptimizeInvert(result)) {
			return true;
		}

		return ATDebugExpNodeUnary::Optimize(result);
	}

	bool OptimizeInvert(ATDebugExpNode **result) {
		*result = mpArg;
		mpArg.release();
		return true;
	}

	bool CanOptimizeInvert() const { return true; }

	bool Evaluate(sint32& result, const ATDebugExpEvalContext& context, ATDebugExpEvalCache& cache) const {
		sint32 x;

		if (!mpArg->Evaluate(x, context, cache))
			return false;

		result = !x;
		return true;
	}

	void EmitUnaryOp(VDStringA& s) {
		s += '!';
	}
};

///////////////////////////////////////////////////////////////////////////

class ATDebugExpNodeDerefByte final : public ATDebugExpNodeUnary {
public:
	ATDebugExpNodeDerefByte(ATDebugExpNode *x)
		: ATDebugExpNodeUnary(kATDebugExpNodeType_DerefByte, x)
	{
	}

	ATDebugExpNode *Clone() const override { return CloneUnary<decltype(*this)>(); }

	bool Evaluate(sint32& result, const ATDebugExpEvalContext& context, ATDebugExpEvalCache& cache) const {
		sint32 x;

		if (!mpArg->Evaluate(x, context, cache))
			return false;

		if (!context.mpTarget)
			return false;

		result = context.mpTarget->DebugReadByte(AdjustAddress(x));
		return true;
	}

	void EmitUnaryOp(VDStringA& s) {
		s += "db ";
	}
};

///////////////////////////////////////////////////////////////////////////

class ATDebugExpNodeDerefSignedByte final : public ATDebugExpNodeUnary {
public:
	ATDebugExpNodeDerefSignedByte(ATDebugExpNode *x)
		: ATDebugExpNodeUnary(kATDebugExpNodeType_DerefSignedByte, x)
	{
	}

	ATDebugExpNode *Clone() const override { return CloneUnary<decltype(*this)>(); }

	bool Evaluate(sint32& result, const ATDebugExpEvalContext& context, ATDebugExpEvalCache& cache) const {
		sint32 x;

		if (!mpArg->Evaluate(x, context, cache))
			return false;

		if (!context.mpTarget)
			return false;

		result = (sint8)context.mpTarget->DebugReadByte(AdjustAddress(x));
		return true;
	}

	void EmitUnaryOp(VDStringA& s) {
		s += "dsb ";
	}
};

///////////////////////////////////////////////////////////////////////////

class ATDebugExpNodeDerefSignedWord final : public ATDebugExpNodeUnary {
public:
	ATDebugExpNodeDerefSignedWord(ATDebugExpNode *x)
		: ATDebugExpNodeUnary(kATDebugExpNodeType_DerefSignedWord, x)
	{
	}

	ATDebugExpNode *Clone() const override { return CloneUnary<decltype(*this)>(); }

	bool Evaluate(sint32& result, const ATDebugExpEvalContext& context, ATDebugExpEvalCache& cache) const {
		sint32 x;

		if (!mpArg->Evaluate(x, context, cache))
			return false;

		if (!context.mpTarget)
			return false;

		uint8 c0 = context.mpTarget->DebugReadByte(AdjustAddress(x));
		uint8 c1 = context.mpTarget->DebugReadByte(AdjustAddress(x+1));
		result = (sint16)(c0 + (c1 << 8));
		return true;
	}

	void EmitUnaryOp(VDStringA& s) {
		s += "dsw ";
	}
};

///////////////////////////////////////////////////////////////////////////

class ATDebugExpNodeDerefSignedDoubleWord final : public ATDebugExpNodeUnary {
public:
	ATDebugExpNodeDerefSignedDoubleWord(ATDebugExpNode *x)
		: ATDebugExpNodeUnary(kATDebugExpNodeType_DerefSignedDoubleWord, x)
	{
	}

	ATDebugExpNode *Clone() const override { return CloneUnary<decltype(*this)>(); }

	bool Evaluate(sint32& result, const ATDebugExpEvalContext& context, ATDebugExpEvalCache& cache) const {
		sint32 x;

		if (!mpArg->Evaluate(x, context, cache))
			return false;

		if (!context.mpTarget)
			return false;

		uint8 c0 = context.mpTarget->DebugReadByte(AdjustAddress(x));
		uint8 c1 = context.mpTarget->DebugReadByte(AdjustAddress(x+1));
		uint8 c2 = context.mpTarget->DebugReadByte(AdjustAddress(x+2));
		uint8 c3 = context.mpTarget->DebugReadByte(AdjustAddress(x+3));
		result = (sint32)(c0 + (c1 << 8) + (c2 << 16) + (c3 << 24));
		return true;
	}

	void EmitUnaryOp(VDStringA& s) {
		s += "dd ";
	}
};

///////////////////////////////////////////////////////////////////////////

class ATDebugExpNodeDerefWord final : public ATDebugExpNodeUnary {
public:
	ATDebugExpNodeDerefWord(ATDebugExpNode *x)
		: ATDebugExpNodeUnary(kATDebugExpNodeType_DerefWord, x)
	{
	}

	ATDebugExpNode *Clone() const override { return CloneUnary<decltype(*this)>(); }

	bool Evaluate(sint32& result, const ATDebugExpEvalContext& context, ATDebugExpEvalCache& cache) const {
		sint32 x;

		if (!mpArg->Evaluate(x, context, cache))
			return false;

		if (!context.mpTarget)
			return false;

		result = context.mpTarget->DebugReadByte(AdjustAddress(x))
			+ ((sint32)context.mpTarget->DebugReadByte(AdjustAddress(x + 1)) << 8);
		return true;
	}

	void EmitUnaryOp(VDStringA& s) {
		s += "dw ";
	}
};

///////////////////////////////////////////////////////////////////////////

class ATDebugExpNodeLoByte final : public ATDebugExpNodeUnary {
public:
	ATDebugExpNodeLoByte(ATDebugExpNode *x)
		: ATDebugExpNodeUnary(kATDebugExpNodeType_LoByte, x)
	{
	}

	ATDebugExpNode *Clone() const override { return CloneUnary<decltype(*this)>(); }

	bool Evaluate(sint32& result, const ATDebugExpEvalContext& context, ATDebugExpEvalCache& cache) const {
		sint32 x;

		if (!mpArg->Evaluate(x, context, cache))
			return false;

		result = x & 0xff;
		return true;
	}

	void EmitUnaryOp(VDStringA& s) {
		s += '<';
	}
};

///////////////////////////////////////////////////////////////////////////

class ATDebugExpNodeHiByte final : public ATDebugExpNodeUnary {
public:
	ATDebugExpNodeHiByte(ATDebugExpNode *x)
		: ATDebugExpNodeUnary(kATDebugExpNodeType_HiByte, x)
	{
	}

	ATDebugExpNode *Clone() const override { return CloneUnary<decltype(*this)>(); }

	bool Evaluate(sint32& result, const ATDebugExpEvalContext& context, ATDebugExpEvalCache& cache) const {
		sint32 x;

		if (!mpArg->Evaluate(x, context, cache))
			return false;

		result = (x & 0xff00) >> 8;
		return true;
	}

	void EmitUnaryOp(VDStringA& s) {
		s += '>';
	}
};

///////////////////////////////////////////////////////////////////////////

class ATDebugExpNodePC final : public ATDebugExpNode {
public:
	ATDebugExpNodePC() : ATDebugExpNode(kATDebugExpNodeType_PC) {}

	ATDebugExpNode *Clone() const override { return new ATDebugExpNodePC; }

	bool Evaluate(sint32& result, const ATDebugExpEvalContext& context, ATDebugExpEvalCache& cache) const {
		const ATCPUExecState *state = cache.GetExecState(context);
		if (!state)
			return false;

		if (cache.mExecMode == kATDebugDisasmMode_Z80)
			result = state->mZ80.mPC;
		else
			result = state->m6502.mPC;
		return true;
	}

	void ToString(VDStringA& s, int prec) {
		s += "@pc";
	}
};

///////////////////////////////////////////////////////////////////////////

class ATDebugExpNodeA final : public ATDebugExpNode {
public:
	ATDebugExpNodeA() : ATDebugExpNode(kATDebugExpNodeType_A) {}

	ATDebugExpNode *Clone() const override { return new ATDebugExpNodeA; }

	bool Evaluate(sint32& result, const ATDebugExpEvalContext& context, ATDebugExpEvalCache& cache) const {
		const ATCPUExecState *state = cache.GetExecState(context);
		if (!state)
			return false;

		if (cache.mExecMode == kATDebugDisasmMode_Z80)
			result = state->mZ80.mA;
		else
			result = state->m6502.mA + ((sint32)state->m6502.mAH << 8);
		return true;
	}

	void ToString(VDStringA& s, int prec) {
		s += "@a";
	}
};

///////////////////////////////////////////////////////////////////////////

class ATDebugExpNodeX final : public ATDebugExpNode {
public:
	ATDebugExpNodeX() : ATDebugExpNode(kATDebugExpNodeType_X) {}

	ATDebugExpNode *Clone() const override { return new ATDebugExpNodeX; }

	bool Evaluate(sint32& result, const ATDebugExpEvalContext& context, ATDebugExpEvalCache& cache) const {
		const ATCPUExecState *state = cache.GetExecState(context);
		if (!state)
			return false;

		if (cache.mExecMode == kATDebugDisasmMode_Z80)
			return false;

		result = state->m6502.mX + ((sint32)state->m6502.mXH << 8);
		return true;
	}

	void ToString(VDStringA& s, int prec) {
		s += "@x";
	}
};

///////////////////////////////////////////////////////////////////////////

class ATDebugExpNodeY final : public ATDebugExpNode {
public:
	ATDebugExpNodeY() : ATDebugExpNode(kATDebugExpNodeType_Y) {}

	ATDebugExpNode *Clone() const override { return new ATDebugExpNodeY; }

	bool Evaluate(sint32& result, const ATDebugExpEvalContext& context, ATDebugExpEvalCache& cache) const {
		const ATCPUExecState *state = cache.GetExecState(context);
		if (!state)
			return false;

		if (cache.mExecMode == kATDebugDisasmMode_Z80)
			return false;

		result = state->m6502.mY + ((sint32)state->m6502.mYH << 8);
		return true;
	}

	void ToString(VDStringA& s, int prec) {
		s += "@y";
	}
};

///////////////////////////////////////////////////////////////////////////

class ATDebugExpNodeS final : public ATDebugExpNode {
public:
	ATDebugExpNodeS() : ATDebugExpNode(kATDebugExpNodeType_S) {}

	ATDebugExpNode *Clone() const override { return new ATDebugExpNodeS; }

	bool Evaluate(sint32& result, const ATDebugExpEvalContext& context, ATDebugExpEvalCache& cache) const {
		const ATCPUExecState *state = cache.GetExecState(context);
		if (!state)
			return false;

		if (cache.mExecMode == kATDebugDisasmMode_Z80)
			result = state->mZ80.mSP;
		else
			result = state->m6502.mS;

		return true;
	}

	void ToString(VDStringA& s, int prec) {
		s += "@s";
	}
};

///////////////////////////////////////////////////////////////////////////

class ATDebugExpNodeP final : public ATDebugExpNode {
public:
	ATDebugExpNodeP() : ATDebugExpNode(kATDebugExpNodeType_P) {}

	ATDebugExpNode *Clone() const override { return new ATDebugExpNodeP; }

	bool Evaluate(sint32& result, const ATDebugExpEvalContext& context, ATDebugExpEvalCache& cache) const {
		const ATCPUExecState *state = cache.GetExecState(context);
		if (!state)
			return false;

		if (cache.mExecMode == kATDebugDisasmMode_Z80)
			return false;

		result = state->m6502.mP;
		return true;
	}

	void ToString(VDStringA& s, int prec) {
		s += "@p";
	}
};

////////////////////////////////////////////////////////////////////////////////////////////////

class ATDebugExpNodeRead final : public ATDebugExpNode {
public:
	ATDebugExpNodeRead() : ATDebugExpNode(kATDebugExpNodeType_Read) {}

	ATDebugExpNode *Clone() const override { return new ATDebugExpNodeRead; }

	bool Evaluate(sint32& result, const ATDebugExpEvalContext& context, ATDebugExpEvalCache& cache) const {
		if (!context.mbAccessReadValid)
			return false;

		result = context.mAccessAddress;
		return true;
	}

	void ToString(VDStringA& s, int prec) {
		s += "@read";
	}
};

////////////////////////////////////////////////////////////////////////////////////////////////

class ATDebugExpNodeWrite final : public ATDebugExpNode {
public:
	ATDebugExpNodeWrite() : ATDebugExpNode(kATDebugExpNodeType_Write) {}

	ATDebugExpNode *Clone() const override { return new ATDebugExpNodeWrite; }

	bool Evaluate(sint32& result, const ATDebugExpEvalContext& context, ATDebugExpEvalCache& cache) const {
		if (!context.mbAccessWriteValid)
			return false;

		result = context.mAccessAddress;
		return true;
	}

	void ToString(VDStringA& s, int prec) {
		s += "@write";
	}
};

////////////////////////////////////////////////////////////////////////////////////////////////

class ATDebugExpNodeHPOS final : public ATDebugExpNode {
public:
	ATDebugExpNodeHPOS() : ATDebugExpNode(kATDebugExpNodeType_HPOS) {}

	ATDebugExpNode *Clone() const override { return new ATDebugExpNodeHPOS; }

	bool Evaluate(sint32& result, const ATDebugExpEvalContext& context, ATDebugExpEvalCache& cache) const {
		if (!context.mpAntic)
			return false;

		result = context.mpAntic->GetBeamX();
		return true;
	}

	void ToString(VDStringA& s, int prec) {
		s += "@hpos";
	}
};

////////////////////////////////////////////////////////////////////////////////////////////////

class ATDebugExpNodeVPOS final : public ATDebugExpNode {
public:
	ATDebugExpNodeVPOS() : ATDebugExpNode(kATDebugExpNodeType_VPOS) {}

	ATDebugExpNode *Clone() const override { return new ATDebugExpNodeVPOS; }

	bool Evaluate(sint32& result, const ATDebugExpEvalContext& context, ATDebugExpEvalCache& cache) const {
		if (!context.mpAntic)
			return false;

		result = context.mpAntic->GetBeamY();
		return true;
	}

	void ToString(VDStringA& s, int prec) {
		s += "@vpos";
	}
};

////////////////////////////////////////////////////////////////////////////////////////////////

class ATDebugExpNodeAddress final : public ATDebugExpNode {
public:
	ATDebugExpNodeAddress() : ATDebugExpNode(kATDebugExpNodeType_Address) {}

	ATDebugExpNode *Clone() const override { return new ATDebugExpNodeAddress; }

	bool Evaluate(sint32& result, const ATDebugExpEvalContext& context, ATDebugExpEvalCache& cache) const {
		if (!context.mbAccessValid)
			return false;

		result = context.mAccessAddress;
		return true;
	}

	void ToString(VDStringA& s, int prec) {
		s += "@address";
	}
};

////////////////////////////////////////////////////////////////////////////////////////////////

class ATDebugExpNodeValue final : public ATDebugExpNode {
public:
	ATDebugExpNodeValue() : ATDebugExpNode(kATDebugExpNodeType_Value) {}

	ATDebugExpNode *Clone() const override { return new ATDebugExpNodeValue; }

	bool Evaluate(sint32& result, const ATDebugExpEvalContext& context, ATDebugExpEvalCache& cache) const {
		if (!context.mbAccessValid)
			return false;

		result = context.mAccessValue;
		return true;
	}

	void ToString(VDStringA& s, int prec) {
		s += "@value";
	}
};

////////////////////////////////////////////////////////////////////////////////////////////////

class ATDebugExpNodeXBankReg final : public ATDebugExpNode {
public:
	ATDebugExpNodeXBankReg() : ATDebugExpNode(kATDebugExpNodeType_XBankReg) {}

	ATDebugExpNode *Clone() const override { return new ATDebugExpNodeXBankReg; }

	bool Evaluate(sint32& result, const ATDebugExpEvalContext& context, ATDebugExpEvalCache& cache) const {
		if (!context.mpMMU)
			return false;

		result = context.mpMMU->GetBankRegister();
		return true;
	}

	void ToString(VDStringA& s, int prec) {
		s += "@xbankreg";
	}
};

////////////////////////////////////////////////////////////////////////////////////////////////

class ATDebugExpNodeXBankCPU final : public ATDebugExpNode {
public:
	ATDebugExpNodeXBankCPU() : ATDebugExpNode(kATDebugExpNodeType_XBankCPU) {}

	ATDebugExpNode *Clone() const override { return new ATDebugExpNodeXBankCPU; }

	bool Evaluate(sint32& result, const ATDebugExpEvalContext& context, ATDebugExpEvalCache& cache) const {
		if (!context.mpMMU)
			return false;

		result = (sint32)context.mpMMU->GetCPUBankBase() - 0x10000;
		return true;
	}

	void ToString(VDStringA& s, int prec) {
		s += "@xbankcpu";
	}
};

////////////////////////////////////////////////////////////////////////////////////////////////

class ATDebugExpNodeXBankANTIC final : public ATDebugExpNode {
public:
	ATDebugExpNodeXBankANTIC() : ATDebugExpNode(kATDebugExpNodeType_XBankANTIC) {}

	ATDebugExpNode *Clone() const override { return new ATDebugExpNodeXBankANTIC; }

	bool Evaluate(sint32& result, const ATDebugExpEvalContext& context, ATDebugExpEvalCache& cache) const {
		if (!context.mpMMU)
			return false;

		result = context.mpMMU->GetAnticBankBase() - 0x10000;
		return true;
	}

	void ToString(VDStringA& s, int prec) {
		s += "@xbankantic";
	}
};

//////////////////////////////////////////////////////

class ATDebugExpNodeAddrSpace final : public ATDebugExpNodeUnary {
public:
	ATDebugExpNodeAddrSpace(uint32 space, ATDebugExpNode *x)
		: ATDebugExpNodeUnary(kATDebugExpNodeType_AddrSpace, x)
		, mSpace(space)
	{
	}

	ATDebugExpNode *Clone() const override {
		vdautoptr<ATDebugExpNode> arg(mpArg->Clone());

		ATDebugExpNode *result = new ATDebugExpNodeAddrSpace(mSpace, arg);

		arg.release();
		return result;
	}

	bool IsAddress() const { return true; }

	bool Evaluate(sint32& result, const ATDebugExpEvalContext& context, ATDebugExpEvalCache& cache) const {
		sint32 x;

		if (!mpArg->Evaluate(x, context, cache))
			return false;

		result = (sint32)(((uint32)x & kATAddressOffsetMask) + mSpace);
		return true;
	}

	void EmitUnaryOp(VDStringA& s) {
		s += ATAddressGetSpacePrefix(mSpace);
	}

private:
	const uint32 mSpace;
};

class ATDebugExpNodeTernary : public ATDebugExpNode {
public:
	ATDebugExpNodeTernary(ATDebugExpNode *x, ATDebugExpNode *y, ATDebugExpNode *z)
		: ATDebugExpNode(kATDebugExpNodeType_Ternary)
		, mpArgCond(x)
		, mpArgTrue(y)
		, mpArgFalse(z)
	{
	}

	ATDebugExpNode *Clone() const override {
		vdautoptr<ATDebugExpNode> c(mpArgCond->Clone());
		vdautoptr<ATDebugExpNode> t(mpArgTrue->Clone());
		vdautoptr<ATDebugExpNode> f(mpArgFalse->Clone());

		ATDebugExpNode *r = new ATDebugExpNodeTernary(c, t, f);
		c.release();
		t.release();
		f.release();

		return r;
	}

	bool Evaluate(sint32& result, const ATDebugExpEvalContext& context, ATDebugExpEvalCache& cache) const {
		sint32 cond;

		if (!mpArgCond->Evaluate(cond, context, cache))
			return false;

		return (cond ? mpArgTrue : mpArgFalse)->Evaluate(result, context, cache);
	}

	bool Optimize(ATDebugExpNode **result);
	bool Optimize2(ATDebugExpNode **result);

	bool OptimizeInvert(ATDebugExpNode **result) {
		ATDebugExpNode *newArg;

		if (CanOptimizeInvert())
			return false;

		VDVERIFY(mpArgTrue->OptimizeInvert(&newArg));
		mpArgTrue = newArg;

		VDVERIFY(mpArgFalse->OptimizeInvert(&newArg));
		mpArgFalse = newArg;

		return true;
	}

	// We can always swap around arguments for free.
	bool CanOptimizeInvert() const {
		return mpArgFalse->CanOptimizeInvert() && mpArgTrue->CanOptimizeInvert();
	}

	void ToString(VDStringA& s, int prec) {
		if (prec > kNodePrecTernary)
			s += '(';

		// Ternary operators are right associative on their ? side. This means that
		// if we have another ternary operator on the left side ((a ? b : c) ?), we
		// need parens.
		mpArgCond->ToString(s, kNodePrecTernary + 1);

		s += " ? ";

		// The middle argument doesn't matter, as there is no parsing ambiguity for [? x ? y : z :].
		mpArgTrue->ToString(s, kNodePrecTernary);

		s += " : ";

		// Ternary operators are left associative on their : side. This means that
		// if we have another ternary operator on the right side, we don't need parens.
		mpArgFalse->ToString(s, kNodePrecTernary);

		if (prec > kNodePrecTernary)
			s += ')';
	}

private:
	vdautoptr<ATDebugExpNode> mpArgCond;
	vdautoptr<ATDebugExpNode> mpArgTrue;
	vdautoptr<ATDebugExpNode> mpArgFalse;
};

bool ATDebugExpNodeTernary::Optimize(ATDebugExpNode **result) {
	// Optimize our arguments first.
	ATDebugExpNode *newArg;
	if (mpArgCond->Optimize(&newArg))
		mpArgCond = newArg;

	if (mpArgTrue->Optimize(&newArg))
		mpArgTrue = newArg;

	if (mpArgFalse->Optimize(&newArg))
		mpArgFalse = newArg;

	// Check if the condition is a constant. If so, we can optimize ourselves out.
	if (mpArgCond->mType == kATDebugExpNodeType_Const) {
		sint32 v;

		if (mpArgCond->Evaluate(v, ATDebugExpEvalContext())) {
			if (v)
				*result = mpArgTrue.release();
			else
				*result = mpArgFalse.release();

			return true;
		}
	}

	// At this point, there are a couple of transformations we can do:
	//
	// - If the condition has an inversion, we can remove it and swap the two
	//   arguments.
	//
	// - If both arguments have a common chain of unary operators, we can
	//   unify them.
	//
	// The first one is the more attractive to do here; the second really
	// should be done via common subexpression elimination, and is not a common
	// case anyway. Besides, it would be complicated to try to do both.

	if (mpArgCond->mType == kATDebugExpNodeType_Invert
		&& mpArgCond->OptimizeInvert(&newArg))
	{
		mpArgCond = newArg;

		vdautoptr<ATDebugExpNode> newNode(new ATDebugExpNodeTernary(mpArgCond, mpArgFalse, mpArgTrue));
		mpArgCond.release();
		mpArgFalse.release();
		mpArgTrue.release();

		// continue with stage 2 optimization
		if (static_cast<ATDebugExpNodeTernary *>(&*newNode)->Optimize2(&newArg))
			newNode = newArg;

		*result = newNode.release();
		return true;
	}

	return Optimize2(result);
}

bool ATDebugExpNodeTernary::Optimize2(ATDebugExpNode **result) {
	ATDebugExpNode *newArg;

	// Check if we have an inversion on both paths. If so, unify it.
	if (mpArgFalse->mType == kATDebugExpNodeType_Invert
		&& mpArgTrue->mType == kATDebugExpNodeType_Invert)
	{
		// strip the invert node from both paths
		VDVERIFY(mpArgFalse->OptimizeInvert(&newArg));
		mpArgFalse = newArg;

		VDVERIFY(mpArgTrue->OptimizeInvert(&newArg));
		mpArgTrue = newArg;

		// create a new ternary node
		newArg = new ATDebugExpNodeTernary(mpArgCond, mpArgTrue, mpArgFalse);
		mpArgCond.release();
		mpArgTrue.release();
		mpArgFalse.release();
		mpArgCond = newArg;

		// wrap it in an inversion node
		*result = new ATDebugExpNodeInvert(mpArgCond);
		mpArgCond.release();

		return true;
	}

	return false;
}

////////////////////////////////////////////////////////////////////////////////////////////////

class ATDebugExpNodeTemporary : public ATDebugExpNode {
public:
	ATDebugExpNodeTemporary(int index) : ATDebugExpNode(kATDebugExpNodeType_Temporary), mIndex(index) {}

	ATDebugExpNode *Clone() const override { return new ATDebugExpNodeTemporary(mIndex); }

	bool Evaluate(sint32& result, const ATDebugExpEvalContext& context, ATDebugExpEvalCache& cache) const {
		if (!context.mpTemporaries)
			return false;

		result = context.mpTemporaries[mIndex];
		return true;
	}

	void ToString(VDStringA& s, int prec) {
		s += '@';
		s += 't';
		s += (char)('0' + mIndex);
	}

protected:
	const int mIndex;
};

////////////////////////////////////////////////////////////////////////////////////////////////

class ATDebugExpNodeReturnAddress : public ATDebugExpNode {
public:
	ATDebugExpNodeReturnAddress() : ATDebugExpNode(kATDebugExpNodeType_ReturnAddress) {}

	ATDebugExpNode *Clone() const override { return new ATDebugExpNodeReturnAddress; }

	bool Evaluate(sint32& result, const ATDebugExpEvalContext& context, ATDebugExpEvalCache& cache) const {
		const ATCPUExecState *state = cache.GetExecState(context);
		if (!state)
			return false;

		uint32 addr = 0;
		uint32 s;

		switch(context.mpTarget->GetDisasmMode()) {
			case kATDebugDisasmMode_Z80:
				s = state->mZ80.mSP;
				addr = context.mpTarget->DebugReadByte(s);
				addr += (uint32)context.mpTarget->DebugReadByte((s + 1) & 0xffff) << 8;
				break;

			case kATDebugDisasmMode_65C816:
				if (!state->m6502.mbEmulationFlag) {
					s = state->m6502.mS + ((uint32)state->m6502.mSH << 8);
					addr = context.mpTarget->DebugReadByte((s + 1) & 0xffff);
					addr += (uint32)context.mpTarget->DebugReadByte((s + 2) & 0xffff) << 8;
					result = (addr + 1) & 0xffff;
					break;
				}
				// fall through
			default:
				s = state->m6502.mS;
				addr = context.mpTarget->DebugReadByte(0x100 + ((s + 1) & 0xff));
				addr += (uint32)context.mpTarget->DebugReadByte(0x100 + ((s + 2) & 0xff)) << 8;
				result = (addr + 1) & 0xffff;
				break;
		}


		return true;
	}

	void ToString(VDStringA& s, int prec) {
		s += "@ra";
	}
};

////////////////////////////////////////////////////////////////////////////////////////////////

class ATDebugExpNodeFrame : public ATDebugExpNode {
public:
	ATDebugExpNodeFrame() : ATDebugExpNode(kATDebugExpNodeType_Frame) {}

	ATDebugExpNode *Clone() const override { return new ATDebugExpNodeFrame; }

	bool Evaluate(sint32& result, const ATDebugExpEvalContext& context, ATDebugExpEvalCache& cache) const {
		if (!context.mpAntic)
			return false;

		result = context.mpAntic->GetFrameCounter();
		return true;
	}

	void ToString(VDStringA& s, int prec) {
		s += "@frame";
	}
};

///////////////////////////////////////////////////////////////////////////

class ATDebugExpNodeClock : public ATDebugExpNode {
public:
	ATDebugExpNodeClock() : ATDebugExpNode(kATDebugExpNodeType_Clock) {}

	ATDebugExpNode *Clone() const override { return new ATDebugExpNodeClock; }

	bool Evaluate(sint32& result, const ATDebugExpEvalContext& context, ATDebugExpEvalCache& cache) const {
		if (!context.mpClockFn)
			return false;

		result = context.mpClockFn(context.mpClockFnData);
		return true;
	}

	void ToString(VDStringA& s, int prec) {
		s += "@clk";
	}
};

///////////////////////////////////////////////////////////////////////////

class ATDebugExpNodeCpuClock : public ATDebugExpNode {
public:
	ATDebugExpNodeCpuClock() : ATDebugExpNode(kATDebugExpNodeType_CpuClock) {}

	ATDebugExpNode *Clone() const override { return new ATDebugExpNodeCpuClock; }

	bool Evaluate(sint32& result, const ATDebugExpEvalContext& context, ATDebugExpEvalCache& cache) const {
		if (!context.mpCpuClockFn)
			return false;

		result = context.mpCpuClockFn(context.mpCpuClockFnData);
		return true;
	}

	void ToString(VDStringA& s, int prec) {
		s += "@cclk";
	}
};

///////////////////////////////////////////////////////////////////////////

class ATDebugExpNodeXPC final : public ATDebugExpNode {
public:
	ATDebugExpNodeXPC() : ATDebugExpNode(kATDebugExpNodeType_XPC) {}

	ATDebugExpNode *Clone() const override { return new ATDebugExpNodeXPC; }

	bool IsAddress() const override { return true; }

	bool Evaluate(sint32& result, const ATDebugExpEvalContext& context, ATDebugExpEvalCache& cache) const {
		if (context.mpXPCFn) {
			result = context.mpXPCFn(context.mpXPCFnData);
		} else {
			const ATCPUExecState *state = cache.GetExecState(context);
			if (!state)
				return false;

			if (cache.mExecMode == kATDebugDisasmMode_Z80)
				result = state->mZ80.mPC;
			else
				result = state->m6502.mPC;
		}

		return true;
	}

	void ToString(VDStringA& s, int prec) {
		s += "@xpc";
	}
};

///////////////////////////////////////////////////////////////////////////

ATDebugExpNode *ATDebuggerParseExpression(const char *s, IATDebuggerSymbolLookup *dbg, const ATDebuggerExprParseOpts& opts, ATDebugExpEvalContext *immContext) {
	enum {
		kOpNone,
		kOpOpenParen,
		kOpCloseParen,
		kOpTernary1,
		kOpTernary2,
		kOpOr,
		kOpAnd,
		kOpBitwiseOr,
		kOpBitwiseXor,
		kOpBitwiseAnd,
		kOpLT,
		kOpLE,
		kOpGT,
		kOpGE,
		kOpEQ,
		kOpNE,
		kOpAdd,
		kOpSub,
		kOpMul,
		kOpDiv,
		kOpMod,
		kOpInvert,
		kOpNegate,
		kOpDerefByte,
		kOpDerefSignedByte,
		kOpDerefSignedWord,
		kOpDerefWord,
		kOpDerefSignedDoubleWord,
		kOpLoByte,
		kOpHiByte,
		kOpAddrSpace
	};

	// The LSBs of these precedence values control associativity. Odd values
	// cause left associativity, and even values cause right associativity.
	// Unary operators are handled specially and always force a shift, so they
	// are right associative regardless of the LSB here (although we set it
	// for consistency anyway).
	static const uint8 kOpPrecedence[]={
		0,

		// open paren
		2,

		// close paren
		0,

		// ternary ?, :
		//
		// This is really nasty. We need ? to be right associative so that
		// a ? b causes a shift, and : to be left associative so that a ? b : c :
		// causes the first : to be reduced before we shift the second.

		// That just leaves ? vs. :. Well, when ? is on the stack, we need :
		// to force a shift to pull in the third value for the reduction. However,
		// when : is on the stack, ? needs to force a shift as well so the ternary
		// operator is right associative. With what we have here, ?: works out
		// but :? doesn't, so we need to special-case the latter.
		4, 5,

		// or
		7,

		// and
		9,

		// bitwise or
		11,

		// bitwise xor
		13,

		// bitwise and
		15,

		// comparisons
		17,17,17,17,17,17,

		// additive
		19,19,

		// multiplicative
		21,21,21,

		// unary
		23,23,23,23,23,23,23,23,23,

		// addr space
		25
	};

	enum {
		kTokEOL,
		kTokInt		= 256,
		kTokDB,
		kTokDSB,
		kTokDW,
		kTokDSW,
		kTokDSD,
		kTokAnd,
		kTokOr,
		kTokLT,
		kTokLE,
		kTokGT,
		kTokGE,
		kTokEQ,
		kTokNE,
		kTokPC,
		kTokA,
		kTokX,
		kTokY,
		kTokS,
		kTokP,
		kTokRead,
		kTokWrite,
		kTokHpos,
		kTokVpos,
		kTokAddress,
		kTokValue,
		kTokAddrSpace,
		kTokXBankReg,
		kTokXBankCPU,
		kTokXBankANTIC,
		kTokTemp,
		kTokReturnAddr,
		kTokFrame,
		kTokClock,
		kTokCpuClock,
		kTokXPC
	};

	vdfastvector<ATDebugExpNode *> valstack;
	vdfastvector<uint8> opstack;

	bool needValue = true;
	sint32 intVal;
	bool hexVal;

	if (dbg && opts.mbAllowUntaggedHex && *s && isxdigit((unsigned char)*s)) {
		char *t = const_cast<char *>(s);
		unsigned long result = strtoul(s, &t, 16);

		if (*t == '\'' || *t == ':') {
			bool is_portb = (*t == '\'');
			++t;
			unsigned long result2 = strtoul(t, &t, 16);

			if (!*t && result < 0x100 && result2 < 0x10000) {
				result = (result << 16) + result2;
				
				if (is_portb)
					result += kATAddressSpace_PORTB;
			} else {
				--t;	// force failure
			}
		}

		if (!*t) {
			s = t;
			needValue = false;

			valstack.push_back(new ATDebugExpNodeConst((sint32)result, true, true));
		}
	}

	try {
		for(;;) {
			char c = *s++;
			int tok = c;

			if (c == ' ')
				continue;

			if (!strchr("+-*/%()&|^?:", c)) {
				if (!c) {
					tok = kTokEOL;
				} else if (c == '<') {
					if (*s == '=') {
						++s;
						tok = kTokLE;
					} else
						tok = kTokLT;
				} else if (c == '>') {
					if (*s == '=') {
						++s;
						tok = kTokGE;
					} else
						tok = kTokGT;
				} else if (c == '=') {
					tok = kTokEQ;
				} else if (c == '!') {
					if (*s == '=') {
						++s;
						tok = kTokNE;
					}
					// fall through if just !
				} else if (c == '$' || (isxdigit((unsigned char)c) && opts.mbDefaultHex)) {
					if (c == '$') {
						c = *s;

						if (!isxdigit((unsigned char)c)) {
							FreeNodes(valstack);
							throw ATDebuggerExprParseException("Expected hex number after $");
						}

						++s;
					}

					bool has_bank = false;
					bool is_portb = false;
					uint32 bank = 0;
					uint32 v = 0;
					for(;;) {
						v = (v << 4) + (c & 0x0f);

						if (c & 0x40)
							v += 9;

						c = *s;

						if (!has_bank && (c == ':' || c == '\'')) {
							has_bank = true;
							is_portb = (c == '\'');

							if (v > 0xff)
								throw ATDebuggerExprParseException("Bank too large");

							bank = v << 16;
							v = 0;
							c = 0;
						} else if (!isxdigit((unsigned char)c))
							break;

						++s;
					}

					if (has_bank) {
						v = (v & 0xffff) + bank;

						if (is_portb)
							v += kATAddressSpace_PORTB;
					}

					intVal = v;
					hexVal = true;
					tok = kTokInt;
				} else if (isdigit((unsigned char)c)) {
					uint32 v = (unsigned char)(c - '0');
					bool has_bank = false;
					uint32 bank = 0;

					for(;;) {
						c = *s;

						if (!has_bank && c == ':') {
							has_bank = true;

							if (v > 0xff)
								throw ATDebuggerExprParseException("Bank too large");

							bank = v << 16;
							v = 0;
						} else if (isdigit((unsigned char)c))
							v = (v * 10) + ((unsigned char)c - '0');
						else
							break;

						++s;

					}

					if (has_bank)
						v += bank;

					intVal = v;
					tok = kTokInt;
					hexVal = false;
				} else if (isalpha((unsigned char)c) || c == '_' || c == '#') {
					const char *idstart = s-1;

					if (c == '#')
						++idstart;

					// we allow a single exclamation mark for module name
					bool seenModuleSplit = false;
					for(;;) {
						char d = *s;

						if (!d)
							break;

						if (d == '!') {
							// prohibit interpreting this as module split if it's part of a !=
							// operator
							if (s[1] == '=')
								break;

							if (seenModuleSplit)
								break;

							seenModuleSplit = true;
						} else if (!isalnum((unsigned char)*s) && *s != '_' && *s != '.')
							break;

						++s;
					}

					VDStringSpanA ident(idstart, s);

					if (c == '#' || seenModuleSplit)
						goto force_ident;

					if (*s == ':') {
						++s;

						if (ident == "v")
							intVal = (sint32)kATAddressSpace_VBXE;
						else if (ident == "n")
							intVal = (sint32)kATAddressSpace_ANTIC;
						else if (ident == "x")
							intVal = (sint32)kATAddressSpace_EXTRAM;
						else if (ident == "r")
							intVal = (sint32)kATAddressSpace_RAM;
						else if (ident == "rom")
							intVal = (sint32)kATAddressSpace_ROM;
						else if (ident == "cart")
							intVal = (sint32)kATAddressSpace_CART;
						else if (ident == "t")
							intVal = (sint32)kATAddressSpace_CB;
						else
							throw ATDebuggerExprParseException("Unknown address space: '%.*s'", ident.size(), ident.data());

						tok = kTokAddrSpace;
					} else if (ident == "pc")
						tok = kTokPC;
					else if (ident == "a")
						tok = kTokA;
					else if (ident == "x")
						tok = kTokX;
					else if (ident == "y")
						tok = kTokY;
					else if (ident == "s")
						tok = kTokS;
					else if (ident == "p")
						tok = kTokP;
					else if (ident == "db")
						tok = kTokDB;
					else if (ident == "dsb")
						tok = kTokDSB;
					else if (ident == "dw")
						tok = kTokDW;
					else if (ident == "dsw")
						tok = kTokDSW;
					else if (ident == "dsd")
						tok = kTokDSD;
					else if (ident == "and")
						tok = kTokAnd;
					else if (ident == "or")
						tok = kTokOr;
					else if (ident == "read")
						tok = kTokRead;
					else if (ident == "write")
						tok = kTokWrite;
					else if (ident == "hpos")
						tok = kTokHpos;
					else if (ident == "vpos")
						tok = kTokVpos;
					else if (ident == "address")
						tok = kTokAddress;
					else if (ident == "value")
						tok = kTokValue;
					else if (ident == "xbankreg")
						tok = kTokXBankReg;
					else if (ident == "xbankcpu")
						tok = kTokXBankCPU;
					else if (ident == "xbankantic")
						tok = kTokXBankANTIC;
					else {
force_ident:
						VDString identstr(ident);
						if (dbg && (intVal = dbg->ResolveSymbol(identstr.c_str(), false, false)) != -1) {
							tok = kTokInt;
							hexVal = opts.mbDefaultHex;
						} else {
							FreeNodes(valstack);
							throw ATDebuggerExprParseException("Unable to resolve symbol \"%s\"", identstr.c_str());
						}
					}
				} else if (c == '@') {
					const char *nameStart = s;

					if (isalpha((unsigned char)*s)) {
						++s;

						while(isalnum((unsigned char)*s))
							++s;
					}

					const char *nameEnd = s;

					if (nameStart == nameEnd && *s == '(') {
						// @(...) -> immediate context
						const char *exprStart = ++s;
						int parens = 0;

						while(*s != ')' || parens > 0) {
							if (!*s)
								throw ATDebuggerExprParseException("Unterminated immediate subexpression");

							if (*s == '(')
								++parens;
							else if (*s == ')')
								--parens;

							++s;
						}

						if (!immContext)
							throw ATDebuggerExprParseException("Cannot evaluate @(...) immediate subexpression in this context");
						
						// recurse
						VDStringA subExprString(exprStart, s);
						vdautoptr<ATDebugExpNode> subExpr { ATDebuggerParseExpression(subExprString.c_str(), dbg, opts, nullptr) };

						// evaluate
						sint32 r;
						if (!subExpr->Evaluate(r, *immContext))
							throw ATDebuggerExprParseException("Could not evaluate immediate sub-expression: %s", subExprString.c_str());

						vdautoptr<ATDebugExpNode> constNode(new ATDebugExpNodeConst(r, false, subExpr->IsAddress()));
						valstack.push_back(constNode);
						constNode.release();

						++s;
						needValue = false;
						continue;
					} else if (nameEnd - nameStart == 2 && nameStart[0] == 't' && nameStart[1] >= '0' && nameStart[1] <= '9') {
						tok = kTokTemp;
						intVal = (int)(nameStart[1] - '0');
					} else if (nameEnd - nameStart == 2 && nameStart[0] == 'r' && nameStart[1] == 'a') {
						tok = kTokReturnAddr;
					} else {
						VDStringSpanA name(nameStart, nameEnd);

						if (name == "frame")
							tok = kTokFrame;
						else if (name == "clk")
							tok = kTokClock;
						else if (name == "cclk")
							tok = kTokCpuClock;
						else if (name == "pc")
							tok = kTokPC;
						else if (name == "xpc")
							tok = kTokXPC;
						else if (name == "a")
							tok = kTokA;
						else if (name == "x")
							tok = kTokX;
						else if (name == "y")
							tok = kTokY;
						else if (name == "s")
							tok = kTokS;
						else if (name == "p")
							tok = kTokP;
						else if (name == "read")
							tok = kTokRead;
						else if (name == "write")
							tok = kTokWrite;
						else if (name == "hpos")
							tok = kTokHpos;
						else if (name == "vpos")
							tok = kTokVpos;
						else if (name == "address")
							tok = kTokAddress;
						else if (name == "value")
							tok = kTokValue;
						else if (name == "xbankreg")
							tok = kTokXBankReg;
						else if (name == "xbankcpu")
							tok = kTokXBankCPU;
						else if (name == "xbankantic")
							tok = kTokXBankANTIC;
						else
							throw ATDebuggerExprParseException("Unknown special variable '@%.*s'", nameEnd - nameStart, nameStart);
					}
				} else
					throw ATDebuggerExprParseException("Unexpected character '%c'", c);
			}

			if (needValue) {
				if (tok == '(') {
					opstack.push_back(kOpOpenParen);
				} else if (tok == '+') {
					// unary plus - nothing to do
				} else if (tok == '-') {
					// unary minus
					opstack.push_back(kOpNegate);
				} else if (tok == '!') {
					// unary minus
					opstack.push_back(kOpInvert);
				} else if (tok == '%') {
					// binary number
					if (*s != '0' && *s != '1')
						throw ATDebuggerExprParseException("Invalid binary number.");

					intVal = 0;
					do {
						intVal = (intVal << 1) + (*s == '1');
						++s;
					} while(*s == '0' || *s == '1');

					vdautoptr<ATDebugExpNode> node(new ATDebugExpNodeConst(intVal, true, false));

					valstack.push_back(node);
					node.release();

					needValue = false;

				} else if (tok == kTokPC) {
					vdautoptr<ATDebugExpNode> node(new ATDebugExpNodePC);

					valstack.push_back(node);
					node.release();

					needValue = false;
				} else if (tok == kTokXPC) {
					vdautoptr<ATDebugExpNode> node(new ATDebugExpNodeXPC);

					valstack.push_back(node);
					node.release();

					needValue = false;
				} else if (tok == kTokA) {
					vdautoptr<ATDebugExpNode> node(new ATDebugExpNodeA);

					valstack.push_back(node);
					node.release();

					needValue = false;
				} else if (tok == kTokX) {
					vdautoptr<ATDebugExpNode> node(new ATDebugExpNodeX);

					valstack.push_back(node);
					node.release();

					needValue = false;
				} else if (tok == kTokY) {
					vdautoptr<ATDebugExpNode> node(new ATDebugExpNodeY);

					valstack.push_back(node);
					node.release();

					needValue = false;
				} else if (tok == kTokS) {
					vdautoptr<ATDebugExpNode> node(new ATDebugExpNodeS);

					valstack.push_back(node);
					node.release();

					needValue = false;
				} else if (tok == kTokP) {
					vdautoptr<ATDebugExpNode> node(new ATDebugExpNodeP);

					valstack.push_back(node);
					node.release();

					needValue = false;
				} else if (tok == kTokRead) {
					vdautoptr<ATDebugExpNode> node(new ATDebugExpNodeRead);

					valstack.push_back(node);
					node.release();

					needValue = false;
				} else if (tok == kTokWrite) {
					vdautoptr<ATDebugExpNode> node(new ATDebugExpNodeWrite);

					valstack.push_back(node);
					node.release();

					needValue = false;
				} else if (tok == kTokHpos) {
					vdautoptr<ATDebugExpNode> node(new ATDebugExpNodeHPOS);

					valstack.push_back(node);
					node.release();

					needValue = false;
				} else if (tok == kTokVpos) {
					vdautoptr<ATDebugExpNode> node(new ATDebugExpNodeVPOS);

					valstack.push_back(node);
					node.release();

					needValue = false;
				} else if (tok == kTokDB) {
					opstack.push_back(kOpDerefByte);
				} else if (tok == kTokDSB) {
					opstack.push_back(kOpDerefSignedByte);
				} else if (tok == kTokDW) {
					opstack.push_back(kOpDerefWord);
				} else if (tok == kTokDSW) {
					opstack.push_back(kOpDerefSignedWord);
				} else if (tok == kTokDSD) {
					opstack.push_back(kOpDerefSignedDoubleWord);
				} else if (tok == kTokInt) {
					vdautoptr<ATDebugExpNode> node(new ATDebugExpNodeConst(intVal, hexVal, false));

					valstack.push_back(node);
					node.release();

					needValue = false;
				} else if (tok == kTokLT) {
					opstack.push_back(kOpLoByte);
				} else if (tok == kTokGT) {
					opstack.push_back(kOpHiByte);
				} else if (tok == kTokAddress) {
					vdautoptr<ATDebugExpNode> node(new ATDebugExpNodeAddress);

					valstack.push_back(node);
					node.release();

					needValue = false;
				} else if (tok == kTokValue) {
					vdautoptr<ATDebugExpNode> node(new ATDebugExpNodeValue);

					valstack.push_back(node);
					node.release();

					needValue = false;
				} else if (tok == kTokAddrSpace) {
					valstack.push_back(new ATDebugExpNodeConst(intVal, true, true));
					opstack.push_back(kOpAddrSpace);
				} else if (tok == kTokXBankReg) {
					vdautoptr<ATDebugExpNode> node(new ATDebugExpNodeXBankReg);

					valstack.push_back(node);
					node.release();

					needValue = false;
				} else if (tok == kTokXBankCPU) {
					vdautoptr<ATDebugExpNode> node(new ATDebugExpNodeXBankCPU);

					valstack.push_back(node);
					node.release();

					needValue = false;
				} else if (tok == kTokXBankANTIC) {
					vdautoptr<ATDebugExpNode> node(new ATDebugExpNodeXBankANTIC);

					valstack.push_back(node);
					node.release();

					needValue = false;
				} else if (tok == kTokTemp) {
					vdautoptr<ATDebugExpNode> node(new ATDebugExpNodeTemporary(intVal));

					valstack.push_back(node);
					node.release();
					needValue = false;
				} else if (tok == kTokReturnAddr) {
					vdautoptr<ATDebugExpNode> node(new ATDebugExpNodeReturnAddress);

					valstack.push_back(node);
					node.release();
					needValue = false;
				} else if (tok == kTokFrame) {
					vdautoptr<ATDebugExpNode> node(new ATDebugExpNodeFrame);

					valstack.push_back(node);
					node.release();
					needValue = false;
				} else if (tok == kTokClock) {
					vdautoptr<ATDebugExpNode> node(new ATDebugExpNodeClock);

					valstack.push_back(node);
					node.release();
					needValue = false;
				} else if (tok == kTokCpuClock) {
					vdautoptr<ATDebugExpNode> node(new ATDebugExpNodeCpuClock);

					valstack.push_back(node);
					node.release();
					needValue = false;
				} else {
					throw ATDebuggerExprParseException("Expected value");
				}
			} else {
				uint8 op;

				switch(tok) {
					case ')':
						op = kOpCloseParen;
						break;

					case '&':
						op = kOpBitwiseAnd;
						break;

					case '|':
						op = kOpBitwiseOr;
						break;

					case '^':
						op = kOpBitwiseXor;
						break;

					case kTokEOL:
						op = kOpNone;
						break;


					case kTokAnd:
						op = kOpAnd;
						break;

					case kTokOr:
						op = kOpOr;
						break;

					case kTokLT:
						op = kOpLT;
						break;

					case kTokLE:
						op = kOpLE;
						break;

					case kTokGT:
						op = kOpGT;
						break;

					case kTokGE:
						op = kOpGE;
						break;

					case kTokEQ:
						op = kOpEQ;
						break;

					case kTokNE:
						op = kOpNE;
						break;

					case '+':
						op = kOpAdd;
						break;

					case '-':
						op = kOpSub;
						break;

					case '*':
						op = kOpMul;
						break;

					case '/':
						op = kOpDiv;
						break;

					case '%':
						op = kOpMod;
						break;

					case '?':
						op = kOpTernary1;
						break;

					case ':':
						op = kOpTernary2;
						break;

					default:
						throw ATDebuggerExprParseException("Expected operator");
				}

				// begin reduction
				uint8 prec = kOpPrecedence[op] | 1;
				for(;;) {
					if (opstack.empty()) {
						if (op == kOpCloseParen)
							throw ATDebuggerExprParseException("Unmatched '('");
						break;
					}

					// Stop and do shift if top op on stack has lower precedence.
					// Continue with reduce if top op on stack has higher precedence.
					// 
					// The |1 on prec allows us to control associativity with equal
					// precedence. Even values force shift (right associative) and
					// odd values force reduce (left associative).
					uint8 redop = opstack.back();

					if (kOpPrecedence[redop] < prec)
						break;

					// special case for ternary operator -- force shift in ?: case
					if (op == kOpTernary1 && redop == kOpTernary2)
						break;

					opstack.pop_back();

					if (redop == kOpOpenParen) {
						if (op == kOpNone)
							throw ATDebuggerExprParseException("Unmatched ')'");
						break;
					}

					vdautoptr<ATDebugExpNode> node;
					ATDebugExpNode **sp = &valstack.back();

					int argcount = 2;

					switch(redop) {
						case kOpOr:
							node = new ATDebugExpNodeOr(sp[-1], sp[0]);
							break;

						case kOpAnd:
							node = new ATDebugExpNodeAnd(sp[-1], sp[0]);
							break;

						case kOpLT:
							node = new ATDebugExpNodeLT(sp[-1], sp[0]);
							break;

						case kOpLE:
							node = new ATDebugExpNodeLE(sp[-1], sp[0]);
							break;

						case kOpGT:
							node = new ATDebugExpNodeGT(sp[-1], sp[0]);
							break;

						case kOpGE:
							node = new ATDebugExpNodeGE(sp[-1], sp[0]);
							break;

						case kOpEQ:
							node = new ATDebugExpNodeEQ(sp[-1], sp[0]);
							break;

						case kOpNE:
							node = new ATDebugExpNodeNE(sp[-1], sp[0]);
							break;

						case kOpAdd:
							node = new ATDebugExpNodeAdd(sp[-1], sp[0]);
							break;

						case kOpSub:
							node = new ATDebugExpNodeSub(sp[-1], sp[0]);
							break;

						case kOpMul:
							node = new ATDebugExpNodeMul(sp[-1], sp[0]);
							break;

						case kOpDiv:
							node = new ATDebugExpNodeDiv(sp[-1], sp[0]);
							break;

						case kOpMod:
							node = new ATDebugExpNodeMod(sp[-1], sp[0]);
							break;

						case kOpInvert:
							node = new ATDebugExpNodeInvert(sp[0]);
							argcount = 1;
							break;


						case kOpNegate:
							node = new ATDebugExpNodeNegate(sp[0]);
							argcount = 1;
							break;

						case kOpDerefByte:
							node = new ATDebugExpNodeDerefByte(sp[0]);
							argcount = 1;
							break;

						case kOpDerefWord:
							node = new ATDebugExpNodeDerefWord(sp[0]);
							argcount = 1;
							break;

						case kOpDerefSignedByte:
							node = new ATDebugExpNodeDerefSignedByte(sp[0]);
							argcount = 1;
							break;

						case kOpDerefSignedWord:
							node = new ATDebugExpNodeDerefSignedWord(sp[0]);
							argcount = 1;
							break;

						case kOpDerefSignedDoubleWord:
							node = new ATDebugExpNodeDerefSignedDoubleWord(sp[0]);
							argcount = 1;
							break;

						case kOpLoByte:
							node = new ATDebugExpNodeLoByte(sp[0]);
							argcount = 1;
							break;

						case kOpHiByte:
							node = new ATDebugExpNodeHiByte(sp[0]);
							argcount = 1;
							break;

						case kOpBitwiseAnd:
							node = new ATDebugExpNodeBitwiseAnd(sp[-1], sp[0]);
							break;

						case kOpBitwiseOr:
							node = new ATDebugExpNodeBitwiseOr(sp[-1], sp[0]);
							break;

						case kOpBitwiseXor:
							node = new ATDebugExpNodeBitwiseXor(sp[-1], sp[0]);
							break;

						case kOpAddrSpace:
							sp[-1]->Evaluate(intVal, ATDebugExpEvalContext());
							delete sp[-1];
							sp[-1] = NULL;
							node = new ATDebugExpNodeAddrSpace(intVal, sp[0]);
							break;

						case kOpTernary1:
							// We should never get here. If we did, we lost the : at some point.
							throw ATDebuggerExprParseException("'?' without ':' on ternary operator");

						case kOpTernary2:
							// We should be reducing both ?: at the same time... if not, oops.
							if (opstack.empty() || opstack.back() != kOpTernary1)
								throw ATDebuggerExprParseException("':' without '?' on ternary operator");
							opstack.pop_back();

							node = new ATDebugExpNodeTernary(sp[-2], sp[-1], sp[0]);
							argcount = 3;
							break;
					}

					while(argcount--)
						valstack.pop_back();

					valstack.push_back(node);
					node.release();
				}

				if (op == kOpNone)
					break;

				if (op != kOpCloseParen) {
					opstack.push_back(op);
					needValue = true;
				}
			}
		}
	} catch(const ATDebuggerExprParseException&) {
		while(!valstack.empty()) {
			delete valstack.back();
			valstack.pop_back();
		}

		throw;
	}

	VDASSERT(valstack.size() == 1);

	ATDebugExpNode *result = valstack.back();
	ATDebugExpNode *optResult;

	if (result->Optimize(&optResult)) {
		delete result;
		result = optResult;
	}

	return result;
}

ATDebugExpNode *ATDebuggerInvertExpression(ATDebugExpNode *node) {
	ATDebugExpNode *result = new ATDebugExpNodeInvert(node);
	ATDebugExpNode *optResult;

	if (result->Optimize(&optResult)) {
		delete result;
		result = optResult;
	}

	return result;
}
