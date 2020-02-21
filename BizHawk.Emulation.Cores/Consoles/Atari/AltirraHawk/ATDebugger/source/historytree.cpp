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

#include "stdafx.h"
#include <at/atdebugger/historytree.h>

ATHistoryTree::ATHistoryTree()
	: mNodeAllocator(sizeof(ATHTNode) * 1022)
{
	Clear();
}

ATHistoryTree::~ATHistoryTree() {
}

ATHTNode *ATHistoryTree::GetFrontNode() const {
	ATHTNode *node = mRootNode.mpFirstChild;

	if (node) {
		while(ATHTNode *child = node->mpFirstChild)
			node = child;
	}

	return node;
}

ATHTNode *ATHistoryTree::GetBackNode() const {
	ATHTNode *node = mRootNode.mpLastChild;

	if (node) {
		while(ATHTNode *child = node->mpLastChild)
			node = child;
	}

	return node;
}

ATHTNode *ATHistoryTree::GetNextNode(ATHTNode *node) const {
	while(node) {
		if (ATHTNode *next = node->mpNextSibling)
			return next;

		node = node->mpParent;
	}

	return nullptr;
}

uint32 ATHistoryTree::GetNodeYPos(ATHTNode *node) const {
	sint32 y = node->mRelYPos;

	for(ATHTNode *p = node->mpParent; p != &mRootNode; p = p->mpParent)
		y += p->mRelYPos + 1;

	return y;
}

uint32 ATHistoryTree::GetLineYPos(const ATHTLineIterator& it) const {
	return GetNodeYPos(it.mpNode) + it.mLineIndex;
}

ATHTLineIterator ATHistoryTree::GetLineFromPos(uint32 pos) const {
	ATHTNode *p = mRootNode.mpFirstChild;
	while(p) {
		if (pos < p->mRelYPos)
			break;

		uint32 offset = pos - p->mRelYPos;
		if (p->mbVisible && offset < p->mHeight) {
			if (offset < p->mVisibleLines)
				return ATHTLineIterator { p, offset };

			pos = offset - p->mVisibleLines;
			p = p->mpFirstChild;
		} else
			p = p->mpNextSibling;
	}

	return {};
}

ATHTNode *ATHistoryTree::GetPrevVisibleNode(ATHTNode *node) const {
	do {
		if (node->mpPrevSibling) {
			node = node->mpPrevSibling;

			while(node->mbExpanded && node->mpLastChild && node->mbVisible)
				node = node->mpLastChild;
		} else {
			do {
				node = node->mpParent;
				if (node == &mRootNode)
					return nullptr;
			} while(!node->mpPrevSibling);
		}
	} while(!node->mbVisible);

	return node;
}

ATHTNode *ATHistoryTree::GetNextVisibleNode(ATHTNode *node) const {
	do {
		if (node->mbExpanded && node->mpFirstChild)
			node = node->mpFirstChild;
		else {
			while(!node->mpNextSibling) {
				node = node->mpParent;
				if (node == &mRootNode)
					return nullptr;
			}

			node = node->mpNextSibling;
		}
	} while(!node->mbVisible);

	return node;
}

bool ATHistoryTree::IsLineVisible(const ATHTLineIterator& it) const {
	ATHTNode *node = it.mpNode;

	if (!node->mbVisible)
		return false;

	return true;
}

ATHTLineIterator ATHistoryTree::GetPrevVisibleLine(const ATHTLineIterator& it) const {
	if (!it)
		return {};

	if (it.mLineIndex)
		return ATHTLineIterator { it.mpNode, it.mLineIndex - 1 };

	ATHTNode *prevNode = GetPrevVisibleNode(it.mpNode);

	return ATHTLineIterator { prevNode, prevNode ? prevNode->mVisibleLines - 1 : 0 };
}

ATHTLineIterator ATHistoryTree::GetNextVisibleLine(const ATHTLineIterator& it) const {
	if (!it)
		return {};

	if (it.mLineIndex + 1 < it.mpNode->mVisibleLines)
		return ATHTLineIterator { it.mpNode, it.mLineIndex + 1 };

	return ATHTLineIterator { GetNextVisibleNode(it.mpNode), 0 };
}

ATHTLineIterator ATHistoryTree::GetNearestVisibleLine(const ATHTLineIterator& it) const {
	if (!it)
		return it;

	ATHTNode *firstCollapsedParent = NULL;

	for(ATHTNode *p = it.mpNode->mpParent; p; p = p->mpParent) {
		if (!p->mbExpanded)
			firstCollapsedParent = p;
	}

	if (firstCollapsedParent)
		return GetNextVisibleLine(ATHTLineIterator { firstCollapsedParent, 0 });
	else if (!IsLineVisible(it))
		return GetNextVisibleLine(it);

	return it;
}

void ATHistoryTree::Clear() {
	mpNodeFreeList = nullptr;
	mRootNode = {};
	mRootNode.mbVisible = true;
	mRootNode.mbExpanded = true;
	mRootNode.mVisibleLines = 1;
	mRootNode.mHeight = 1;
	mRootNode.mNodeType = kATHTNodeType_Label;
	mRootNode.mpLabel = "";
	mNodeAllocator.Clear();
}

bool ATHistoryTree::Verify() {
	return VerifyNode(&mRootNode, (uint32)-1);
}

bool ATHistoryTree::VerifyNode(ATHTNode *node, uint32 depth) {
	uint32 h = 0;

	for(ATHTNode *c = node->mpFirstChild; c; c = c->mpNextSibling) {
		if (depth)
			VerifyNode(c, depth - 1);

		VDASSERT(c->mRelYPos == h);
		h += c->mHeight;
	}

	if (!node->mbExpanded)
		h = 0;

	if (node->mbVisible) {
		if (node->mNodeType == kATHTNodeType_Insn) {
			VDASSERT(node->mVisibleLines > 0);

			if (node->mbFiltered) {
				VDASSERT(node->mVisibleLines < node->mInsn.mCount);
			} else {
				VDASSERT(node->mVisibleLines == node->mInsn.mCount);
			}
		} else {
			VDASSERT(node->mVisibleLines == 1);
			VDASSERT(!node->mbFiltered);
		}
	} else {
		VDASSERT(!node->mbFiltered);
		VDASSERT(node->mVisibleLines == 0);
	}

	h += node->mVisibleLines;
	VDASSERT(h == node->mHeight);

	return true;
}

bool ATHistoryTree::ExpandNode(ATHTNode *node) {
	if (node->mbExpanded || !node->mpFirstChild)
		return false;

	VDASSERT(node->mHeight == 1);

	node->mbExpanded = true;

	uint32 newHeight = 1;

	ATHTNode *lastChild = node->mpLastChild;
	if (lastChild)
		newHeight = lastChild->mRelYPos + lastChild->mHeight + 1;

	node->mHeight = newHeight;

	VDASSERT(newHeight >= 1);
	uint32 delta = newHeight - 1;

	MoveNodesDownAfter(node, delta);
	return true;
}

bool ATHistoryTree::CollapseNode(ATHTNode *node) {
	if (!node->mbExpanded)
		return false;

	node->mbExpanded = false;

	uint32 delta = node->mHeight - 1;
	node->mHeight = 1;

	MoveNodesUpAfter(node, delta);
	return true;
}

ATHTNode *ATHistoryTree::InsertLabelNode(ATHTNode *parent, ATHTNode *insertAfter, const char *text) {
	ATHTNode *node = AllocNode();
	node->mRelYPos = 0;
	node->mbExpanded = false;
	node->mbFiltered = false;
	node->mbVisible = true;
	node->mpFirstChild = NULL;
	node->mpLastChild = NULL;
	node->mVisibleLines = 1;
	node->mHeight = 1;
	node->mNodeType = kATHTNodeType_Label;
	node->mpLabel = text;

	InsertNode(parent, insertAfter, node);

	return node;
}

ATHTNode *ATHistoryTree::InsertNode(ATHTNode *parent, ATHTNode *insertAfter, uint32 insnOffset, ATHTNodeType nodeType) {
	ATHTNode *node = AllocNode();
	node->mRelYPos = 0;
	node->mbExpanded = false;
	node->mbFiltered = false;
	node->mbVisible = true;
	node->mpFirstChild = NULL;
	node->mpLastChild = NULL;
	node->mVisibleLines = 1;
	node->mHeight = 1;
	node->mNodeType = nodeType;
	node->mInsn.mOffset = insnOffset;
	node->mInsn.mCount = 1;

	InsertNode(parent, insertAfter, node);

	return node;
}

void ATHistoryTree::InsertNode(ATHTNode *parent, ATHTNode *insertAfter, ATHTNode *node) {
	if (!parent)
		parent = &mRootNode;

	VDASSERT(parent != node);
	node->mpParent = parent;

	VDASSERT(parent->mNodeType != kATHTNodeType_Insn || parent->mInsn.mCount == 1);

	if (insertAfter) {
		ATHTNode *next = insertAfter->mpNextSibling;
		node->mpNextSibling = next;

		if (next)
			next->mpPrevSibling = node;
		else
			parent->mpLastChild = node;

		insertAfter->mpNextSibling = node;
		node->mpPrevSibling = insertAfter;

		node->mRelYPos = insertAfter->mRelYPos + insertAfter->mHeight;
	} else {
		ATHTNode *next = parent->mpFirstChild;
		node->mpNextSibling = next;

		node->mpPrevSibling = NULL;
		parent->mpFirstChild = node;

		if (next)
			next->mpPrevSibling = node;
		else
			parent->mpLastChild = node;

		node->mRelYPos = 0;
	}

	// adjust positions of siblings
	MoveNodesDownAfter(node, node->mHeight);
}

void ATHistoryTree::RemoveNode(ATHTNode *node) {
	VDASSERT(node);

	// adjust heights of parents and siblings
	MoveNodesUpAfter(node, node->mHeight);

	// unlink nodes
	ATHTNode *nextNode = node->mpNextSibling;
	ATHTNode *prevNode = node->mpPrevSibling;

	if (prevNode)
		prevNode->mpNextSibling = nextNode;
	else
		node->mpParent->mpFirstChild = nextNode;

	if (nextNode)
		nextNode->mpPrevSibling = prevNode;
	else
		node->mpParent->mpLastChild = prevNode;
}

void ATHistoryTree::MoveNodesUpAfter(ATHTNode *node, uint32 ht) {
	for(;;) {
		// adjust positions of siblings
		for(ATHTNode *s = node->mpNextSibling; s; s = s->mpNextSibling) {
			VDASSERT(s->mRelYPos >= ht);
			s->mRelYPos -= ht;
		}

		// advance to parent
		node = node->mpParent;
		if (!node || !node->mbExpanded)
			break;

		// adjust height
		VDASSERT(node->mHeight >= ht);
		node->mHeight -= ht;
	}
}

void ATHistoryTree::MoveNodesDownAfter(ATHTNode *node, uint32 ht) {
	for(;;) {
		// adjust positions of siblings
		for(ATHTNode *s = node->mpNextSibling; s; s = s->mpNextSibling)
			s->mRelYPos += ht;

		// advance to parent
		node = node->mpParent;
		if (!node || !node->mbExpanded)
			break;

		// adjust height
		node->mHeight += ht;
	}
}

void ATHistoryTree::SpliceNodes(ATHTNode *front, ATHTNode *back, ATHTNode *newParent, ATHTNode *insertAfter) {
	VDASSERT(front->mpParent == back->mpParent);
	VDASSERT(front->mRelYPos <= back->mRelYPos);
	VDASSERT(!insertAfter || insertAfter->mpParent == newParent);

	// compute height of sequence and reassign parents and ypos
	ATHTNode *oldParent = front->mpParent;
	VDASSERT(!oldParent->mbExpanded || oldParent->mHeight == back->mRelYPos + back->mHeight + oldParent->mVisibleLines);

	uint32 ht = back->mRelYPos + back->mHeight - front->mRelYPos;

	VDASSERT(!oldParent->mbExpanded || oldParent->mHeight > ht);

	// delink sequence
	ATHTNode *beforeFront = front->mpPrevSibling;
	ATHTNode *afterBack = back->mpNextSibling;

	if (beforeFront) {
		VDASSERT(beforeFront->mpNextSibling == front);
		beforeFront->mpNextSibling = afterBack;
	} else {
		VDASSERT(oldParent->mpFirstChild == front);
		oldParent->mpFirstChild = afterBack;
	}

	if (afterBack) {
		VDASSERT(afterBack->mpPrevSibling == back);
		afterBack->mpPrevSibling = beforeFront;
	} else {
		VDASSERT(oldParent->mpLastChild == back);
		oldParent->mpLastChild = beforeFront;
	}

	// adjust heights of parents and younger siblings
	MoveNodesUpAfter(back, ht);

	// relink sequence
	ATHTNode *newBeforeFront = insertAfter;
	ATHTNode *newAfterBack = insertAfter ? insertAfter->mpNextSibling : newParent->mpFirstChild;

	if (newBeforeFront) {
		VDASSERT(newBeforeFront->mpNextSibling == newAfterBack);
		newBeforeFront->mpNextSibling = front;
	} else {
		VDASSERT(newParent->mpFirstChild == newAfterBack);
		newParent->mpFirstChild = front;
	}

	if (newAfterBack) {
		VDASSERT(newAfterBack->mpPrevSibling == newBeforeFront);
		newAfterBack->mpPrevSibling = back;
	} else {
		VDASSERT(newParent->mpLastChild == newBeforeFront);
		newParent->mpLastChild = back;
	}

	front->mpPrevSibling = newBeforeFront;
	back->mpNextSibling = newAfterBack;

	// redo heights and parents
	uint32 ypos = insertAfter ? insertAfter->mRelYPos + insertAfter->mHeight : 0;

	for(ATHTNode *p = front; ; p = p->mpNextSibling) {
		VDASSERT(p->mpParent == oldParent);
		p->mpParent = newParent;
		p->mRelYPos = ypos;
		ypos += p->mHeight;

		if (p == back)
			break;
	}

	// adjust heights of new parents and younger siblings
	MoveNodesDownAfter(back, ht);
}

ATHTNode *ATHistoryTree::SplitInsnNode(ATHTNode *node, uint32 splitOffset) {
	VDASSERT(node->mNodeType == kATHTNodeType_Insn);
	VDASSERT(splitOffset > 0 && splitOffset < node->mInsn.mCount);

	ATHTNode *splitNode = AllocNode();
	splitNode->mRelYPos = node->mRelYPos + splitOffset;
	splitNode->mbExpanded = false;
	splitNode->mbFiltered = false;
	splitNode->mbVisible = true;
	splitNode->mNodeType = kATHTNodeType_Insn;
	splitNode->mpParent = node->mpParent;
	splitNode->mpPrevSibling = node;
	splitNode->mpNextSibling = node->mpNextSibling;
	splitNode->mpFirstChild = nullptr;
	splitNode->mpLastChild = nullptr;
	splitNode->mInsn.mOffset = node->mInsn.mOffset + splitOffset;
	splitNode->mInsn.mCount = splitNode->mVisibleLines = splitNode->mHeight = node->mInsn.mCount - splitOffset;

	node->mpNextSibling = splitNode;
	if (splitNode->mpNextSibling)
		splitNode->mpNextSibling->mpPrevSibling = splitNode;
	else
		splitNode->mpParent->mpLastChild = splitNode;

	node->mHeight = node->mVisibleLines = node->mInsn.mCount = splitOffset;

	return splitNode;
}

void ATHistoryTree::Search(const vdfunction<uint32(const ATHTNode&)>& filter) {
	if (mRootNode.mpFirstChild) {
		mRootNode.mHeight = SearchNodes(&mRootNode, filter) + 1;

		VDASSERT(Verify());
	}
}

void ATHistoryTree::Unsearch() {
	const uint32 ht = UnsearchNodes(&mRootNode);
	mRootNode.mHeight = ht + 1;
}

uint32 ATHistoryTree::SearchNodes(ATHTNode *node, const vdfunction<uint32(const ATHTNode&)>& filter) {
	uint32 ht = 0;

	for(ATHTNode *child = node->mpFirstChild; child; child = child->mpNextSibling) {
		child->mbExpanded = true;
		child->mbFiltered = false;

		uint32 visibleLines = 0;
		uint32 nestedHt = 0;

		// if the node has any visible children, the node itself is visible (this will always be
		// a single line node)
		if (child->mpFirstChild) {
			nestedHt = SearchNodes(child, filter);
			visibleLines = nestedHt ? 1 : 0;
		}

		// if the node does not have visible children, check if the node itself matches the
		// filter
		if (!visibleLines) {
			const bool isInsnNode = (child->mNodeType == kATHTNodeType_Insn);
			const uint32 lineCount = isInsnNode ? child->mInsn.mCount : 1;
			const uint32 startOffset = isInsnNode ? child->mInsn.mOffset : 0;

			// Reset the visible state to unfiltered before running through the filter again.
			child->mVisibleLines = lineCount;

			visibleLines = filter(*child);
			child->mbFiltered = visibleLines > 0 && visibleLines < lineCount;
		}

		child->mRelYPos = ht;
		child->mVisibleLines = visibleLines;
		child->mbVisible = visibleLines > 0;
		child->mHeight = (child->mbExpanded ? nestedHt : 0) + child->mVisibleLines;

		ht += child->mHeight;
	}

	return ht;
}

uint32 ATHistoryTree::UnsearchNodes(ATHTNode *node) {
	uint32 ht = 0;

	for(ATHTNode *child = node->mpFirstChild; child; child = child->mpNextSibling) {
		child->mbExpanded = false;

		uint32 childHt = 0;
		if (child->mpFirstChild)
			childHt = UnsearchNodes(child);

		if (child->mNodeType == kATHTNodeType_Insn)
			child->mVisibleLines = child->mInsn.mCount;
		else
			child->mVisibleLines = 1;

		child->mHeight = child->mVisibleLines;
		child->mRelYPos = ht;
		child->mbVisible = true;
		child->mbFiltered = false;

		ht += child->mHeight;
	}

	return ht;
}

ATHTNode *ATHistoryTree::AllocNode() {
	if (!mpNodeFreeList) {
		mpNodeFreeList = mNodeAllocator.Allocate<ATHTNode>();
		mpNodeFreeList->mpPrevSibling = mpNodeFreeList;
		mpNodeFreeList->mpNextSibling = nullptr;
	}

	ATHTNode *p = mpNodeFreeList;

	VDASSERT(p->mpPrevSibling == p);

	mpNodeFreeList = p->mpNextSibling;

	p->mpPrevSibling = nullptr;
	return p;
}

void ATHistoryTree::FreeNode(ATHTNode *node) {
	node->mpFirstChild = nullptr;
	node->mpLastChild = nullptr;
	node->mpNextSibling = nullptr;
	node->mpPrevSibling = nullptr;

	if (node != &mRootNode) {
		VDASSERT(node->mpPrevSibling != node);

		node->mpPrevSibling = node;
		node->mpNextSibling = mpNodeFreeList;
		mpNodeFreeList = node;
	}
}

void ATHistoryTree::FreeNodes(ATHTNode *node) {
	if (!node)
		return;

	ATHTNode *p = node;

	for(;;) {
		// find first descendent that doesn't have its own children
		while(p->mpFirstChild) {
			VDASSERT(p->mpFirstChild->mpParent == p);

			p = p->mpFirstChild;
		}

		// free current node
		ATHTNode *next = p->mpNextSibling;
		ATHTNode *parent = p->mpParent;

		FreeNode(p);

		// exit if this is the original node
		if (p == node)
			break;

		// advance to next sibling if we have one
		if (next)
			p = next;
		else {
			p = parent;

			// this node no longer has any children as we just freed them all
			p->mpFirstChild = NULL;
		}
	}
}
