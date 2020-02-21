//	Altirra - Atari 800/800XL/5200 emulator
//	Copyright (C) 2009-2016 Avery Lee
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
#include <set>
#include <unordered_map>
#include <vd2/system/error.h>
#include <vd2/system/file.h>
#include <vd2/system/linearalloc.h>
#include <vd2/vdjson/jsonvalue.h>
#include <vd2/vdjson/jsonoutput.h>
#include <vd2/vdjson/jsonwriter.h>
#include <vd2/vdjson/jsonreader.h>
#include "compatdb.h"
#include "compatedb.h"

VDStringW ATCompatEDBAliasRule::ToDisplayString() const {
	VDStringW s;

	switch(mRuleType) {
		case kATCompatRuleType_CartChecksum:
			s.sprintf(L"Cartridge:[%016llX]", (unsigned long long)mChecksum);
			break;

		case kATCompatRuleType_DiskChecksum:
			s.sprintf(L"Disk:[%016llX]", (unsigned long long)mChecksum);
			break;

		case kATCompatRuleType_DOSBootChecksum:
			s.sprintf(L"Disk DOS:[%016llX]", (unsigned long long)mChecksum);
			break;

		case kATCompatRuleType_ExeChecksum:
			s.sprintf(L"Exe:[%016llX]", (unsigned long long)mChecksum);
			break;
	}

	return s;
}

///////////////////////////////////////////////////////////////////////////

VDStringW ATCompatEDBSourcedAliasRule::ToDisplayString() const {
	VDStringW s;

	s += L'[';
	s += mSource;
	s += L"] ";
	s += mRule.ToDisplayString();
	return s;
}

///////////////////////////////////////////////////////////////////////////

void ATLoadCompatEDB(const wchar_t *path, ATCompatEDB& edb) {
	VDJSONDocument doc;

	{
		VDFile f(path);
		auto size = f.size();
		if (size > 500*1024*1024)
			throw MyError("Compatibility database is too large: %llu bytes.", (unsigned long long)size);

		uint32 size32 = (uint32)size;
		vdblock<char> data(size32);
		f.read(data.data(), (long)size32);

		VDJSONReader reader;
		reader.Parse(data.data(), size32, doc);
	}

	edb.mTitleTable.Clear();
	edb.mTagTable.clear();

	try {
		const auto& rootNode = doc.Root();
		if (!rootNode.IsObject())
			throw VDParseException();

		if (wcscmp(rootNode[".type"].AsString(), L"compatdb"))
			throw VDParseException();

		// load tags
		for(const auto& tagNode : rootNode.GetRequiredArray("tags")) {
			tagNode.RequireObject();

			const wchar_t *keyStr = tagNode.GetRequiredString("key");
			for(const wchar_t *p = keyStr; *p; ++p) {
				wchar_t c = *p;

				if (c < 32 || c > 127)
					throw VDParseException();
			}

			VDStringA key = VDTextWToA(keyStr);

			auto r = edb.mTagTable.insert(key);
			if (!r.second)
				throw VDParseException();

			ATCompatEDBTag& tag = r.first->second;;
			tag.mKey = key;
			tag.mDisplayName = tagNode.GetRequiredString("displayname");
		}

		// load titles
		for(const auto& titleNode : rootNode.GetRequiredArray("titles")) {
			auto *title = edb.mTitleTable.Create();
			title->mName = titleNode.GetRequiredString("name");

			for(const auto& aliasNode : titleNode.GetRequiredArray("aliases")) {
				aliasNode.RequireObject();

				title->mAliases.push_back();
				ATCompatEDBAlias& alias = title->mAliases.back();

				for(const auto& ruleNode : aliasNode.GetRequiredArray("rules")) {
					auto& rule = alias.mRules.push_back();

					const wchar_t *typeStr = ruleNode.GetRequiredString("type");
					const VDStringSpanW typeSpan { typeStr };

					if (typeSpan == L"cart")
						rule.mRuleType = kATCompatRuleType_CartChecksum;
					else if (typeSpan == L"disk")
						rule.mRuleType = kATCompatRuleType_DiskChecksum;
					else if (typeSpan == L"dosboot")
						rule.mRuleType = kATCompatRuleType_DOSBootChecksum;
					else if (typeSpan == L"exe")
						rule.mRuleType = kATCompatRuleType_ExeChecksum;
					else
						throw VDParseException("Unsupported alias rule type: %ls", typeStr);

					const wchar_t *valueStr = ruleNode.GetRequiredString("value");

					wchar_t c;
					unsigned long long v;
					if (1 != swscanf(valueStr, L"%llx%lc", &v, &c))
						throw VDParseException();

					rule.mChecksum = (uint64)v;
				}
			}

			for(const auto& tagNode : titleNode.GetRequiredArray("tags")) {
				tagNode.RequireString();

				VDStringA tag = VDTextWToA(tagNode.AsString());

				if (edb.mTagTable.find(tag) == edb.mTagTable.end())
					throw VDParseException("Title '%ls' references unknown tag '%s'.", title->mName.c_str(), tag.c_str());

				title->mTags.push_back_as(std::move(tag));
			}
		}
	} catch(const VDParseException&) {
		throw MyError("\"%ls\" is not a valid compatibility database.", path);
	}
}

void ATSaveCompatEDB(const wchar_t *path, const ATCompatEDB& edb) {
	VDFileStream fs(path, nsVDFile::kWrite | nsVDFile::kDenyAll | nsVDFile::kCreateAlways);
	VDJSONStreamOutputSysLE streamWriter(fs);

	VDJSONWriter writer;

	writer.Begin(&streamWriter);
	writer.OpenObject();

	writer.WriteMemberName(L".type");
	writer.WriteString(L"compatdb");

	// write titles
	writer.WriteMemberName(L"titles");
	writer.OpenArray();

	VDStringW s;
	for(const auto *title : edb.mTitleTable) {
		writer.OpenObject();
		writer.WriteMemberName(L"name");
		writer.WriteString(title->mName.c_str());
		writer.WriteMemberName(L"aliases");
		writer.OpenArray();
		for(const auto& alias : title->mAliases) {
			writer.OpenObject();
			writer.WriteMemberName(L"rules");
			writer.OpenArray();
			for(auto&& rule : alias.mRules) {
				writer.OpenObject();
				writer.WriteMemberName(L"type");
				switch(rule.mRuleType) {
					case kATCompatRuleType_CartChecksum:
						writer.WriteString(L"cart");
						break;
					case kATCompatRuleType_DiskChecksum:
						writer.WriteString(L"disk");
						break;
					case kATCompatRuleType_DOSBootChecksum:
						writer.WriteString(L"dosboot");
						break;
					case kATCompatRuleType_ExeChecksum:
						writer.WriteString(L"exe");
						break;
					default:
						writer.WriteNull();
						break;
				}
				writer.WriteMemberName(L"value");
				s.sprintf(L"%llX", (unsigned long long)rule.mChecksum);
				writer.WriteString(s.c_str());
				writer.Close();
			}
			writer.Close();
			writer.Close();
		}
		writer.Close();
		writer.WriteMemberName(L"tags");
		writer.OpenArray();
		for(auto&& tag : title->mTags)
			writer.WriteString(VDTextAToW(tag).c_str());
		writer.Close();
		writer.Close();
	}
	writer.Close();

	// write tags
	writer.WriteMemberName(L"tags");
	writer.OpenArray();
	for(const auto& tagEntry : edb.mTagTable) {
		writer.OpenObject();
		writer.WriteMemberName(L"key");
		writer.WriteString(VDTextAToW(tagEntry.second.mKey.c_str()).c_str());
		writer.WriteMemberName(L"displayname");
		writer.WriteString(tagEntry.second.mDisplayName.c_str());
		writer.Close();
	}

	writer.Close();

	writer.Close();
	writer.End();

	streamWriter.Flush();

	fs.close();
}

///////////////////////////////////////////////////////////////////////////

class ATSegmentBuilder {
public:
	uint32 AddSegment(uint32 alignment);

	uint32 GetLink(uint32 segmentIndex, uint32 segmentOffset) const {
		return (segmentIndex << 24) + segmentOffset;
	}

	uint32 GetNextLink(uint32 segmentIndex) const {
		return GetLink(segmentIndex, mSegments[segmentIndex - 1].mTotalSize);
	}

	void *Allocate(uint32 segmentIndex, size_t len);

	template<typename T>
	T *Allocate(uint32 segmentIndex) {
		static_assert(alignof(T) <= 4, "Alignment needs to be no greater than 4 for .res compatibility.");

		return (T *)Allocate(segmentIndex, sizeof(T));
	}

	template<typename T>
	T *AllocateArray(uint32 segmentIndex, size_t n) {
		static_assert(alignof(T) <= 4, "Alignment needs to be no greater than 4 for .res compatibility.");

		return (T *)Allocate(segmentIndex, sizeof(T) * n);
	}

	uint32 Store(uint32 segmentIndex, const void *src, size_t len);

	template<typename T>
	uint32 Store(uint32 segmentIndex, const T& v) {
		return Store(segmentIndex, &v, sizeof v);
	}

	sint32 LinkToOffset(uint32 link) const {
		return link ? mSegments[(link >> 24) - 1].mOffset + (link & 0xFFFFFF) : 1;
	}

	const void *GetSerializedSegment(uint32 segmentIndex) const {
		return (const char *)mpSerializedBase + mSegments[segmentIndex - 1].mOffset;
	}

	uint32 GetSegmentSize(uint32 segmentIndex) const {
		return mSegments[segmentIndex - 1].mTotalSize;
	}

	uint32 Layout(uint32 initialOffset);
	void Serialize(void *dst);

	void ConvertLinkToOffset(sint32& link);

private:
	VDLinearAllocator mLinearAlloc;

	struct Fragment {
		Fragment *mpNext;
		size_t mSize;
	};

	struct Segment {
		Fragment *mpLastFragment;
		uint32 mAlignment;
		uint32 mTotalSize;
		uint32 mOffset;
	};

	vdfastvector<Segment> mSegments;

	void *mpSerializedBase = nullptr;
	uint32 mSerializedLength = 0;
};

uint32 ATSegmentBuilder::AddSegment(uint32 alignment) {
	mSegments.push_back({nullptr, alignment, 0, 0});

	return (uint32)mSegments.size();
}

void *ATSegmentBuilder::Allocate(uint32 segmentIndex, size_t len) {
	Segment& s = mSegments[segmentIndex - 1];

	Fragment *f = (Fragment *)mLinearAlloc.Allocate(sizeof(Fragment) + len);

	f->mpNext = s.mpLastFragment;
	s.mpLastFragment = f;
	s.mTotalSize += len;
	f->mSize = len;

	memset(f + 1, 0, len);

	return f + 1;
}

uint32 ATSegmentBuilder::Store(uint32 segmentIndex, const void *src, size_t len) {
	uint32 link = GetNextLink(segmentIndex);
	void *dst = Allocate(segmentIndex, len);

	memcpy(dst, src, len);

	return link;
}

uint32 ATSegmentBuilder::Layout(uint32 initialOffset) {
	uint32 offset = initialOffset;

	for(auto& seg : mSegments) {
		seg.mOffset = (offset + seg.mAlignment - 1) & ((uint32)0 - seg.mAlignment);
		offset += seg.mTotalSize;
	}

	mSerializedLength = offset;
	return offset;
}

void ATSegmentBuilder::Serialize(void *dst) {
	mpSerializedBase = dst;

	memset(dst, 0, mSerializedLength);

	for(const Segment& seg : mSegments) {
		char *dst2 = (char *)dst + seg.mOffset + seg.mTotalSize;

		for(auto *p = seg.mpLastFragment; p; p = p->mpNext) {
			dst2 -= p->mSize;
			memcpy(dst2, p + 1, p->mSize);
		}
	}
}

void ATSegmentBuilder::ConvertLinkToOffset(sint32& link) {
	uint32 targetOffset = LinkToOffset((uint32)link);
	uint32 selfOffset = (uint32)((const char *)&link - (const char *)mpSerializedBase);
	link = (sint32)(targetOffset - selfOffset);
}

void ATCompileCompatEDB(vdblock<char>& dst, const ATCompatEDB& edb) {
	ATSegmentBuilder builder;

	// allocate segments for various tables
	const uint32 segRuleSets = builder.AddSegment(alignof(ATCompatDBRuleSet));
	const uint32 segRules = builder.AddSegment(alignof(ATCompatDBRule));
	const uint32 segTitles = builder.AddSegment(alignof(ATCompatDBTitle));
	const uint32 segAliases = builder.AddSegment(alignof(ATCompatDBAlias));
	const uint32 segTags = builder.AddSegment(alignof(ATCompatDBTag));
	const uint32 segTagIds = builder.AddSegment(alignof(uint32));
	const uint32 segText = builder.AddSegment(1);

	// build lists of used aliases and tags
	vdfastvector<const ATCompatEDBAlias *> usedAliases;
	vdfastvector<const ATCompatEDBTag *> usedTags;
	std::unordered_map<VDStringA, uint32, vdhash<VDStringA>, vdstringpred> tagLookup;

	{
		uint32 numAliases = 0;
		uint32 numTags = 0;
		uint32 titleId = 0;

		for(const auto *srcTitle : edb.mTitleTable) {
			for(const auto& alias : srcTitle->mAliases) {
				usedAliases.push_back(&alias);

				builder.Store(segAliases, ATCompatDBAlias { (uint32)alias.mRules.size(), titleId } );
			}

			for(const auto& tag : srcTitle->mTags) {
				if (tagLookup.emplace(tag, numTags).second) {
					++numTags;
					usedTags.push_back(&edb.mTagTable.find(tag)->second);
				}
			}

			++titleId;
		}
	}

	// build list of rule types
	std::set<uint32> ruleTypes;

	for(const auto *alias : usedAliases) {
		for(const auto& rule : alias->mRules)
			ruleTypes.insert(rule.mRuleType);
	}

	// process each rule type
	vdfastvector<ATCompatDBRule *> allRules;
	const uint32 numUsedAliases = (uint32)usedAliases.size();

	for(const uint32 ruleType : ruleTypes) {
		// compile inverted index for rules
		struct Rule {
			const ATCompatEDBAliasRule *mpRule;
			uint32 mTitleIndex;
		};

		vdfastvector<ATCompatDBRule> rules;

		for(uint32 i = 0; i < numUsedAliases; ++i) {
			for(const auto& rule : usedAliases[i]->mRules) {
				if (rule.mRuleType == ruleType)
					rules.push_back( { (uint32)rule.mChecksum, (uint32)(rule.mChecksum >> 32), i } );
			}
		}

		// sort rules for binary search
		std::sort(rules.begin(), rules.end(),
			[](auto x, auto y) {
				return (((uint64)x.mValueHi << 32) + x.mValueLo) < (((uint64)y.mValueHi << 32) + y.mValueLo);
			}
		);

		// add entry for rule set
		ATCompatDBRuleSet *ruleSet = builder.Allocate<ATCompatDBRuleSet>(segRuleSets);
		const uint32 numRules = (uint32)rules.size();

		ruleSet->mRuleType = ruleType;
		ruleSet->mRules.mOffset = (sint32)builder.GetNextLink(segRules);
		ruleSet->mRules.mSize = numRules;

		// load into builder
		ATCompatDBRule *finalRules = builder.AllocateArray<ATCompatDBRule>(segRules, numRules);

		allRules.reserve(allRules.size() + numRules);
		for(uint32 i=0; i<numRules; ++i)
			allRules.push_back(finalRules + i);

		std::copy(rules.begin(), rules.end(), finalRules);
	}

	// create circular linked lists for all rules in each alias, across all rule types
	{
		vdfastvector<sint32> aliasRuleTails(numUsedAliases, -1);
		const uint32 numRules = (uint32)allRules.size();

		for(uint32 i=0; i<numRules; ++i) {
			auto& rule = *allRules[i];
			sint32& tail = aliasRuleTails[rule.mAliasId];

			if (tail < 0)
				rule.mNextRuleId = i;
			else {
				rule.mNextRuleId = allRules[tail]->mNextRuleId;
				allRules[tail]->mNextRuleId = i;
			}

			tail = (sint32)i;
		}
	}

	// process titles
	struct VecHash {
		size_t operator()(const vdvector<VDStringA> *p) const {
			size_t hash = p->size();
			vdhash<VDStringA> hs;

			for(const auto& v : *p)
				hash = (hash * 33) + hs(v);

			return hash;
		}
	};

	struct VecEq {
		bool operator()(const vdvector<VDStringA> *x, const vdvector<VDStringA> *y) const {
			if (x == y)
				return true;

			size_t n = x->size();

			if (y->size() != n)
				return false;

			for(size_t i=0; i<n; ++i) {
				if ((*x)[i] != (*y)[i])
					return false;
			}

			return true;
		}
	};

	std::unordered_map<const vdvector<VDStringA> *, uint32, VecHash, VecEq> tagListLookup;

	for(const auto *srcTitle : edb.mTitleTable) {
		auto *dstTitle = builder.Allocate<ATCompatDBTitle>(segTitles);

		VDStringA name8 = VDTextWToU8(srcTitle->mName);
		dstTitle->mName.mOffset = (sint32)builder.Store(segText, name8.c_str(), name8.size() + 1);

		auto r = tagListLookup.emplace(&srcTitle->mTags, builder.GetNextLink(segTagIds));
		const size_t numTags = srcTitle->mTags.size();
		dstTitle->mTagIds.mOffset = r.first->second;
		dstTitle->mTagIds.mSize = (uint32)numTags;

		if (r.second) {
			auto *newTagIds = builder.AllocateArray<uint32>(segTagIds, numTags);
			std::transform(srcTitle->mTags.begin(), srcTitle->mTags.end(), newTagIds,
				[&](const VDStringA& id) { return tagLookup.find(id)->second; }
				);
		}
	}

	// process tags
	for(const auto *srcTag : usedTags) {
		auto *dstTag = builder.Allocate<ATCompatDBTag>(segTags);

		dstTag->mKey.mOffset = (sint32)builder.Store(segText, srcTag->mKey.c_str(), srcTag->mKey.size() + 1);
	}

	// compute linear layout
	uint32 requiredSize = builder.Layout(sizeof(ATCompatDBHeader));

	dst.resize(requiredSize);

	void *base = dst.data();
	builder.Serialize(base);

	ATCompatDBHeader& hdr = *(ATCompatDBHeader *)base;
	memset(&hdr, 0, sizeof hdr);
	memcpy(hdr.mSignature, hdr.kSignature, sizeof hdr.mSignature);
	hdr.mVersion = 0x0100;
	hdr.mRuleSetTable.retarget(builder.GetSerializedSegment(segRuleSets));
	hdr.mRuleSetTable.mSize = builder.GetSegmentSize(segRuleSets) / sizeof(ATCompatDBRuleSet);
	hdr.mRuleTable.retarget(builder.GetSerializedSegment(segRules));
	hdr.mRuleTable.mSize = builder.GetSegmentSize(segRules) / sizeof(ATCompatDBRule);
	hdr.mAliasTable.retarget(builder.GetSerializedSegment(segAliases));
	hdr.mAliasTable.mSize = builder.GetSegmentSize(segAliases) / sizeof(ATCompatDBAlias);
	hdr.mTitleTable.retarget(builder.GetSerializedSegment(segTitles));
	hdr.mTitleTable.mSize = builder.GetSegmentSize(segTitles) / sizeof(ATCompatDBTitle);
	hdr.mTagTable.retarget(builder.GetSerializedSegment(segTags));
	hdr.mTagTable.mSize = builder.GetSegmentSize(segTags) / sizeof(ATCompatDBTag);
	hdr.mTagIdTable.retarget(builder.GetSerializedSegment(segTagIds));
	hdr.mTagIdTable.mSize = builder.GetSegmentSize(segTagIds) / sizeof(uint32);
	hdr.mCharTable.retarget(builder.GetSerializedSegment(segText));
	hdr.mCharTable.mSize = builder.GetSegmentSize(segText);

	// convert links to offsets
	for(auto& ruleSet : hdr.mRuleSetTable)
		builder.ConvertLinkToOffset(ruleSet.mRules.mOffset);

	for(auto& title : hdr.mTitleTable) {
		builder.ConvertLinkToOffset(title.mName.mOffset);
		builder.ConvertLinkToOffset(title.mTagIds.mOffset);
	}

	for(auto& tag : hdr.mTagTable) {
		builder.ConvertLinkToOffset(tag.mKey.mOffset);
	}

	VDASSERT(hdr.Validate(requiredSize));
}
