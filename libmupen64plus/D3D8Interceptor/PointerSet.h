#pragma once

#include <objidl.h>

const UINT PointerSetHashSize = 128;

struct PointerLinkedList
{
	PVOID			   pKey;
	PVOID			   pData;
	PointerLinkedList* pNext;
};

class PointerSet
{
public:
	PointerSet()
	{
		for(UINT i = 0; i < PointerSetHashSize; i++)
		{
			m_pHead[i] = NULL;
		}
	}

	PVOID GetDataPtr(PVOID pKey)
	{
		PointerLinkedList* pThis = m_pHead[GetHash(pKey)];
		while(pThis)
		{
			if(pThis->pKey == pKey)
			{
				return pThis->pData;
			}
			pThis = pThis->pNext;
		}
		return NULL;
	}
	bool AddMember(PVOID pKey, PVOID pData)
	{
		UINT Hash = GetHash(pKey);
		PointerLinkedList* pThis = new PointerLinkedList;
		if(pThis == NULL)
		{
			return false;
		}

		pThis->pNext = m_pHead[Hash];
		pThis->pKey = pKey;
		pThis->pData = pData;
		m_pHead[Hash] = pThis;
		return true;
	}
	bool DeleteMember(PVOID pKey)
	{
		UINT Hash = GetHash(pKey);
		PointerLinkedList* pThis = m_pHead[Hash];
		PointerLinkedList* pLast = 0L;

		if( m_pHead[Hash]->pKey == pKey )
		{
			m_pHead[Hash] = pThis->pNext;
			delete pThis;
			return true;
		}
		else
		{
			pLast = pThis;
			pThis = pThis->pNext;
		}

		while( pThis )
		{
			if( pThis->pKey == pKey )
			{
				pLast->pNext = pThis->pNext;
				delete pThis;
				return true;
			}
			pLast = pThis;
			pThis = pThis->pNext;
		}
		return false;
	}
	__forceinline UINT GetHash(PVOID pKey)
	{
		DWORD Key = (DWORD)pKey;
		return (( Key >> 3 ^ Key >> 7 ^ Key >> 11 ^ Key >> 17 ) & (PointerSetHashSize - 1));
	}

private:
	PointerLinkedList* m_pHead[PointerSetHashSize];
};


class ThreadSafePointerSet : public PointerSet
{
public:
	ThreadSafePointerSet()
	{
		InitializeCriticalSection(&m_CritSec);
	}
	~ThreadSafePointerSet()
	{
		DeleteCriticalSection(&m_CritSec);
	}
	PVOID GetDataPtr(PVOID pKey)
	{
		EnterCriticalSection(&m_CritSec);
		PVOID p = PointerSet::GetDataPtr(pKey);
		LeaveCriticalSection(&m_CritSec);
		return p;
	}
	bool AddMember(PVOID pKey, PVOID pData)
	{
		EnterCriticalSection(&m_CritSec);
		bool Result = PointerSet::AddMember(pKey, pData);
		LeaveCriticalSection(&m_CritSec);
		return Result;
	}
	bool DeleteMember(PVOID pKey)
	{
		EnterCriticalSection(&m_CritSec);
		bool Result = PointerSet::DeleteMember(pKey);
		LeaveCriticalSection(&m_CritSec);
		return Result;
	}

private:
	CRITICAL_SECTION m_CritSec;
};
