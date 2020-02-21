//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2008-2017 Avery Lee
//	Debugger module - execution history tree data model
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

//=========================================================================
// History tree
//
// The history tree holds a nested structure deduced from a linear CPU
// execution trace, where interrupts, calls, and loops are extracted
// into a hierarchy. It is designed for efficient display, much better
// that can be attained if the nodes have to be reflected into a view
// that keeps its own model (Win32 treeview, particularly).
//
// The tree itself uses doubly linked lists throughout, allowing for
// quick traversal in all four directions. Each level of the tree requires
// a linear walk to find a particular node, however. This is not generally
// an issue in practice because the tree represents batches of instructions
// as single nodes and a search by position is only needed once per point
// lookup, used by click handling and for paints.
//
// Note that the tree itself does not store instructions and does not
// interface with a view; it is model-only and only contains views onto
// an unspecified instruction buffer. In particular, this allows the
// instructions to be compressed and/or paged, since the instruction stream
// is generally much bigger than the tree.
//
// Addressing in the tree itself is in terms of lines, where each visible
// instruction or parent node occupies a line. Position is the zero-indexed
// visible line number from the top of the tree; relative position is the
// zero-indexed visible line number from the first child node with the same
// parent. Height-changing modifications require flowing relative height
// changes only to later nodes within the same or higher parent, not through
// the entire rest of the tree.
//
// The root node is special and used for tracking the start, end, and height
// of the entire tree. It is not intended to be shown, and thus the visible
// height of the tree is one less than the height of the root node.
//
// A history tree owns its nodes; it is invalid to attempt to splice nodes
// between trees.
//

#ifndef f_AT_ATDEBUGGER_HISTORYTREE_H
#define f_AT_ATDEBUGGER_HISTORYTREE_H

#include <vd2/system/vdtypes.h>
#include <vd2/system/function.h>
#include <vd2/system/linearalloc.h>

enum ATHTNodeType : uint8 {
	// One or more instructions (mInsn valid, multiple supported).
	kATHTNodeType_Insn,

	// Preview of the next instruction (mInsn valid).
	kATHTNodeType_InsnPreview,

	// A parent node containing loop iterations (mRepeat valid).
	kATHTNodeType_Repeat,

	// An instruction entry corresponding to an interrupt response (mInsn valid).
	kATHTNodeType_Interrupt,

	// A plain text label.
	kATHTNodeType_Label
};

struct ATHTNode {
	// Position of first line in lines within the parent. This is equal
	// to the partial sum of heights of all previous siblings.
	uint32	mRelYPos;

	// Total visible height in lines, including children if expanded
	// and equal to mVisibleLines otherwise.
	uint32	mHeight;

	// Node has children and is showing them. This must only be set
	// when the node has children; otherwise views show a busted-looking
	// expansion.
	bool	mbExpanded;

	// Node is showing some but not all lines, not considering children.
	// This cannot be set if the node is entirely visible or invisible;
	// it is therefore never set for nodes that have children.
	bool	mbFiltered;

	// True if the node is showing at least one visible line. If false,
	// children are not shown either.
	bool	mbVisible;

	ATHTNodeType	mNodeType;
	uint32	mVisibleLines;		// Number of visible lines, not counting children
	ATHTNode *mpParent;
	ATHTNode *mpPrevSibling;	// Prev node with same parent
	ATHTNode *mpNextSibling;	// Next node with same parent
	ATHTNode *mpFirstChild;
	ATHTNode *mpLastChild;
	union {
		struct {
			uint32	mOffset;
			uint32	mCount;
		} mInsn;

		struct {
			uint32	mCount;
			uint32	mSize;
		} mRepeat;

		const char *mpLabel;
	};
};

struct ATHTLineIterator {
	ATHTNode *mpNode;
	uint32 mLineIndex;

	bool operator==(const ATHTLineIterator& other) const {
		return mpNode == other.mpNode && mLineIndex == other.mLineIndex;
	}

	bool operator!=(const ATHTLineIterator& other) const {
		return mpNode != other.mpNode || mLineIndex != other.mLineIndex;
	}

	explicit operator bool() const {
		return mpNode != nullptr;
	}
};

class ATHistoryTree {
	ATHistoryTree(const ATHistoryTree&) = delete;
	ATHistoryTree& operator=(const ATHistoryTree&) = delete;

public:
	ATHistoryTree();
	~ATHistoryTree();

	ATHTNode *GetRootNode() { return &mRootNode; }
	const ATHTNode *GetRootNode() const { return &mRootNode; }

	// Retrieves the first and last nodes of the tree in in-order traversal order.
	// Ignores visibility.
	ATHTNode *GetFrontNode() const;
	ATHTNode *GetBackNode() const;

	// Retrieve the next (successor) node in in-order traversal order. Ignores visibility.
	ATHTNode *GetNextNode(ATHTNode *node) const;

	uint32 GetNodeYPos(ATHTNode *node) const;
	uint32 GetLineYPos(const ATHTLineIterator& it) const;
	ATHTLineIterator GetLineFromPos(uint32 pos) const;

	ATHTNode *GetPrevVisibleNode(ATHTNode *node) const;
	ATHTNode *GetNextVisibleNode(ATHTNode *node) const;

	bool IsLineVisible(const ATHTLineIterator& it) const;
	ATHTLineIterator GetPrevVisibleLine(const ATHTLineIterator& it) const;
	ATHTLineIterator GetNextVisibleLine(const ATHTLineIterator& it) const;

	// Returns the first visible line at or after the given location.
	ATHTLineIterator GetNearestVisibleLine(const ATHTLineIterator& it) const;

	bool Verify();
	bool VerifyNode(ATHTNode *node, uint32 depth);

	void Clear();

	// Expand or collapse a node that has children. Returns true if the
	// expansion state was changed; false if the node was already in desired
	// state or can't be expanded because it has no children.
	bool ExpandNode(ATHTNode *node);
	bool CollapseNode(ATHTNode *node);

	ATHTNode *InsertLabelNode(ATHTNode *parent, ATHTNode *insertAfter, const char *text);
	ATHTNode *InsertNode(ATHTNode *parent, ATHTNode *insertAfter, uint32 insnOffset, ATHTNodeType nodeType);
	void InsertNode(ATHTNode *parent, ATHTNode *insertAfter, ATHTNode *node);
	void RemoveNode(ATHTNode *node);

	void MoveNodesUpAfter(ATHTNode *node, uint32 ht);
	void MoveNodesDownAfter(ATHTNode *node, uint32 ht);

	void SpliceNodes(ATHTNode *front, ATHTNode *back, ATHTNode *newParent, ATHTNode *insertAfter);
	ATHTNode *SplitInsnNode(ATHTNode *node, uint32 splitOffset);

	void Search(const vdfunction<uint32(const ATHTNode&)>& filter);
	void Unsearch();

private:
	uint32 SearchNodes(ATHTNode *node, const vdfunction<uint32(const ATHTNode&)>& filter);
	uint32 UnsearchNodes(ATHTNode *node);
	ATHTNode *AllocNode();
	void FreeNode(ATHTNode *);
	void FreeNodes(ATHTNode *);

	ATHTNode *mpNodeFreeList = nullptr;
	ATHTNode mRootNode = {};
	VDLinearAllocator mNodeAllocator;
};

#endif
