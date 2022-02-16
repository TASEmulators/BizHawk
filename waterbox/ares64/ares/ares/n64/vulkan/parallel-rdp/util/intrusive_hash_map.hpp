/* Copyright (c) 2017-2020 Hans-Kristian Arntzen
 *
 * Permission is hereby granted, free of charge, to any person obtaining
 * a copy of this software and associated documentation files (the
 * "Software"), to deal in the Software without restriction, including
 * without limitation the rights to use, copy, modify, merge, publish,
 * distribute, sublicense, and/or sell copies of the Software, and to
 * permit persons to whom the Software is furnished to do so, subject to
 * the following conditions:
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
 * EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
 * MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT.
 * IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
 * CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT,
 * TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE
 * SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */

#pragma once

#include "hash.hpp"
#include "intrusive_list.hpp"
#include "object_pool.hpp"
#include "read_write_lock.hpp"
#include <assert.h>
#include <vector>

namespace Util
{
template <typename T>
class IntrusiveHashMapEnabled : public IntrusiveListEnabled<T>
{
public:
	IntrusiveHashMapEnabled() = default;
	IntrusiveHashMapEnabled(Util::Hash hash)
		: intrusive_hashmap_key(hash)
	{
	}

	void set_hash(Util::Hash hash)
	{
		intrusive_hashmap_key = hash;
	}

	Util::Hash get_hash() const
	{
		return intrusive_hashmap_key;
	}

private:
	Hash intrusive_hashmap_key = 0;
};

template <typename T>
struct IntrusivePODWrapper : public IntrusiveHashMapEnabled<IntrusivePODWrapper<T>>
{
	template <typename U>
	explicit IntrusivePODWrapper(U&& value_)
		: value(std::forward<U>(value_))
	{
	}

	IntrusivePODWrapper() = default;

	T& get()
	{
		return value;
	}

	const T& get() const
	{
		return value;
	}

	T value = {};
};

// This HashMap is non-owning. It just arranges a list of pointers.
// It's kind of special purpose container used by the Vulkan backend.
// Dealing with memory ownership is done through composition by a different class.
// T must inherit from IntrusiveHashMapEnabled<T>.
// Each instance of T can only be part of one hashmap.

template <typename T>
class IntrusiveHashMapHolder
{
public:
	enum { InitialSize = 16, InitialLoadCount = 3 };

	T *find(Hash hash) const
	{
		if (values.empty())
			return nullptr;

		Hash hash_mask = values.size() - 1;
		auto masked = hash & hash_mask;
		for (unsigned i = 0; i < load_count; i++)
		{
			if (values[masked] && get_hash(values[masked]) == hash)
				return values[masked];
			masked = (masked + 1) & hash_mask;
		}

		return nullptr;
	}

	template <typename P>
	bool find_and_consume_pod(Hash hash, P &p) const
	{
		T *t = find(hash);
		if (t)
		{
			p = t->get();
			return true;
		}
		else
			return false;
	}

	// Inserts, if value already exists, insertion does not happen.
	// Return value is the data which is not part of the hashmap.
	// It should be deleted or similar.
	// Returns nullptr if nothing was in the hashmap for this key.
	T *insert_yield(T *&value)
	{
		if (values.empty())
			grow();

		Hash hash_mask = values.size() - 1;
		auto hash = get_hash(value);
		auto masked = hash & hash_mask;

		for (unsigned i = 0; i < load_count; i++)
		{
			if (values[masked] && get_hash(values[masked]) == hash)
			{
				T *ret = value;
				value = values[masked];
				return ret;
			}
			else if (!values[masked])
			{
				values[masked] = value;
				list.insert_front(value);
				return nullptr;
			}
			masked = (masked + 1) & hash_mask;
		}

		grow();
		return insert_yield(value);
	}

	T *insert_replace(T *value)
	{
		if (values.empty())
			grow();

		Hash hash_mask = values.size() - 1;
		auto hash = get_hash(value);
		auto masked = hash & hash_mask;

		for (unsigned i = 0; i < load_count; i++)
		{
			if (values[masked] && get_hash(values[masked]) == hash)
			{
				std::swap(values[masked], value);
				list.erase(value);
				list.insert_front(values[masked]);
				return value;
			}
			else if (!values[masked])
			{
				assert(!values[masked]);
				values[masked] = value;
				list.insert_front(value);
				return nullptr;
			}
			masked = (masked + 1) & hash_mask;
		}

		grow();
		return insert_replace(value);
	}

	T *erase(Hash hash)
	{
		Hash hash_mask = values.size() - 1;
		auto masked = hash & hash_mask;

		for (unsigned i = 0; i < load_count; i++)
		{
			if (values[masked] && get_hash(values[masked]) == hash)
			{
				auto *value = values[masked];
				list.erase(value);
				values[masked] = nullptr;
				return value;
			}
			masked = (masked + 1) & hash_mask;
		}
		return nullptr;
	}

	void erase(T *value)
	{
		erase(get_hash(value));
	}

	void clear()
	{
		list.clear();
		values.clear();
		load_count = 0;
	}

	typename IntrusiveList<T>::Iterator begin()
	{
		return list.begin();
	}

	typename IntrusiveList<T>::Iterator end()
	{
		return list.end();
	}

	IntrusiveList<T> &inner_list()
	{
		return list;
	}

private:

	inline bool compare_key(Hash masked, Hash hash) const
	{
		return get_key_for_index(masked) == hash;
	}

	inline Hash get_hash(const T *value) const
	{
		return static_cast<const IntrusiveHashMapEnabled<T> *>(value)->get_hash();
	}

	inline Hash get_key_for_index(Hash masked) const
	{
		return get_hash(values[masked]);
	}

	bool insert_inner(T *value)
	{
		Hash hash_mask = values.size() - 1;
		auto hash = get_hash(value);
		auto masked = hash & hash_mask;

		for (unsigned i = 0; i < load_count; i++)
		{
			if (!values[masked])
			{
				values[masked] = value;
				return true;
			}
			masked = (masked + 1) & hash_mask;
		}
		return false;
	}

	void grow()
	{
		bool success;
		do
		{
			for (auto &v : values)
				v = nullptr;

			if (values.empty())
			{
				values.resize(InitialSize);
				load_count = InitialLoadCount;
				//LOGI("Growing hashmap to %u elements.\n", InitialSize);
			}
			else
			{
				values.resize(values.size() * 2);
				//LOGI("Growing hashmap to %u elements.\n", unsigned(values.size()));
				load_count++;
			}

			// Re-insert.
			success = true;
			for (auto &t : list)
			{
				if (!insert_inner(&t))
				{
					success = false;
					break;
				}
			}
		} while (!success);
	}

	std::vector<T *> values;
	IntrusiveList<T> list;
	unsigned load_count = 0;
};

template <typename T>
class IntrusiveHashMap
{
public:
	~IntrusiveHashMap()
	{
		clear();
	}

	IntrusiveHashMap() = default;
	IntrusiveHashMap(const IntrusiveHashMap &) = delete;
	void operator=(const IntrusiveHashMap &) = delete;

	void clear()
	{
		auto &list = hashmap.inner_list();
		auto itr = list.begin();
		while (itr != list.end())
		{
			auto *to_free = itr.get();
			itr = list.erase(itr);
			pool.free(to_free);
		}

		hashmap.clear();
	}

	T *find(Hash hash) const
	{
		return hashmap.find(hash);
	}

	T &operator[](Hash hash)
	{
		auto *t = find(hash);
		if (!t)
			t = emplace_yield(hash);
		return *t;
	}

	template <typename P>
	bool find_and_consume_pod(Hash hash, P &p) const
	{
		return hashmap.find_and_consume_pod(hash, p);
	}

	void erase(T *value)
	{
		hashmap.erase(value);
		pool.free(value);
	}

	void erase(Hash hash)
	{
		auto *value = hashmap.erase(hash);
		if (value)
			pool.free(value);
	}

	template <typename... P>
	T *emplace_replace(Hash hash, P&&... p)
	{
		T *t = allocate(std::forward<P>(p)...);
		return insert_replace(hash, t);
	}

	template <typename... P>
	T *emplace_yield(Hash hash, P&&... p)
	{
		T *t = allocate(std::forward<P>(p)...);
		return insert_yield(hash, t);
	}

	template <typename... P>
	T *allocate(P&&... p)
	{
		return pool.allocate(std::forward<P>(p)...);
	}

	void free(T *value)
	{
		pool.free(value);
	}

	T *insert_replace(Hash hash, T *value)
	{
		static_cast<IntrusiveHashMapEnabled<T> *>(value)->set_hash(hash);
		T *to_delete = hashmap.insert_replace(value);
		if (to_delete)
			pool.free(to_delete);
		return value;
	}

	T *insert_yield(Hash hash, T *value)
	{
		static_cast<IntrusiveHashMapEnabled<T> *>(value)->set_hash(hash);
		T *to_delete = hashmap.insert_yield(value);
		if (to_delete)
			pool.free(to_delete);
		return value;
	}

	typename IntrusiveList<T>::Iterator begin()
	{
		return hashmap.begin();
	}

	typename IntrusiveList<T>::Iterator end()
	{
		return hashmap.end();
	}

	IntrusiveHashMap &get_thread_unsafe()
	{
		return *this;
	}

private:
	IntrusiveHashMapHolder<T> hashmap;
	ObjectPool<T> pool;
};

template <typename T>
using IntrusiveHashMapWrapper = IntrusiveHashMap<IntrusivePODWrapper<T>>;

template <typename T>
class ThreadSafeIntrusiveHashMap
{
public:
	T *find(Hash hash) const
	{
		lock.lock_read();
		T *t = hashmap.find(hash);
		lock.unlock_read();

		// We can race with the intrusive list internal pointers,
		// but that's an internal detail which should never be touched outside the hashmap.
		return t;
	}

	template <typename P>
	bool find_and_consume_pod(Hash hash, P &p) const
	{
		lock.lock_read();
		bool ret = hashmap.find_and_consume_pod(hash, p);
		lock.unlock_read();
		return ret;
	}

	void clear()
	{
		lock.lock_write();
		hashmap.clear();
		lock.unlock_write();
	}

	// Assumption is that readers will not be erased while in use by any other thread.
	void erase(T *value)
	{
		lock.lock_write();
		hashmap.erase(value);
		lock.unlock_write();
	}

	void erase(Hash hash)
	{
		lock.lock_write();
		hashmap.erase(hash);
		lock.unlock_write();
	}

	template <typename... P>
	T *allocate(P&&... p)
	{
		lock.lock_write();
		T *t = hashmap.allocate(std::forward<P>(p)...);
		lock.unlock_write();
		return t;
	}

	void free(T *value)
	{
		lock.lock_write();
		hashmap.free(value);
		lock.unlock_write();
	}

	T *insert_replace(Hash hash, T *value)
	{
		lock.lock_write();
		value = hashmap.insert_replace(hash, value);
		lock.unlock_write();
		return value;
	}

	T *insert_yield(Hash hash, T *value)
	{
		lock.lock_write();
		value = hashmap.insert_yield(hash, value);
		lock.unlock_write();
		return value;
	}

	// This one is very sketchy, since callers need to make sure there are no readers of this hash.
	template <typename... P>
	T *emplace_replace(Hash hash, P&&... p)
	{
		lock.lock_write();
		T *t = hashmap.emplace_replace(hash, std::forward<P>(p)...);
		lock.unlock_write();
		return t;
	}

	template <typename... P>
	T *emplace_yield(Hash hash, P&&... p)
	{
		lock.lock_write();
		T *t = hashmap.emplace_yield(hash, std::forward<P>(p)...);
		lock.unlock_write();
		return t;
	}

	// Not supposed to be called in racy conditions,
	// we could have a global read lock and unlock while iterating if necessary.
	typename IntrusiveList<T>::Iterator begin()
	{
		return hashmap.begin();
	}

	typename IntrusiveList<T>::Iterator end()
	{
		return hashmap.end();
	}

	IntrusiveHashMap<T> &get_thread_unsafe()
	{
		return hashmap;
	}

private:
	IntrusiveHashMap<T> hashmap;
	mutable RWSpinLock lock;
};

// A special purpose hashmap which is split into a read-only, immutable portion and a plain thread-safe one.
// User can move read-write thread-safe portion to read-only portion when user knows it's safe to do so.
template <typename T>
class ThreadSafeIntrusiveHashMapReadCached
{
public:
	~ThreadSafeIntrusiveHashMapReadCached()
	{
		clear();
	}

	T *find(Hash hash) const
	{
		T *t = read_only.find(hash);
		if (t)
			return t;

		lock.lock_read();
		t = read_write.find(hash);
		lock.unlock_read();
		return t;
	}

	void move_to_read_only()
	{
		auto &list = read_write.inner_list();
		auto itr = list.begin();
		while (itr != list.end())
		{
			auto *to_move = itr.get();
			read_write.erase(to_move);
			T *to_delete = read_only.insert_yield(to_move);
			if (to_delete)
				object_pool.free(to_delete);
			itr = list.begin();
		}
	}

	template <typename P>
	bool find_and_consume_pod(Hash hash, P &p) const
	{
		if (read_only.find_and_consume_pod(hash, p))
			return true;

		lock.lock_read();
		bool ret = read_write.find_and_consume_pod(hash, p);
		lock.unlock_read();
		return ret;
	}

	void clear()
	{
		lock.lock_write();
		clear_list(read_only.inner_list());
		clear_list(read_write.inner_list());
		read_only.clear();
		read_write.clear();
		lock.unlock_write();
	}

	template <typename... P>
	T *allocate(P&&... p)
	{
		lock.lock_write();
		T *t = object_pool.allocate(std::forward<P>(p)...);
		lock.unlock_write();
		return t;
	}

	void free(T *ptr)
	{
		lock.lock_write();
		object_pool.free(ptr);
		lock.unlock_write();
	}

	T *insert_yield(Hash hash, T *value)
	{
		static_cast<IntrusiveHashMapEnabled<T> *>(value)->set_hash(hash);
		lock.lock_write();
		T *to_delete = read_write.insert_yield(value);
		if (to_delete)
			object_pool.free(to_delete);
		lock.unlock_write();
		return value;
	}

	template <typename... P>
	T *emplace_yield(Hash hash, P&&... p)
	{
		T *t = allocate(std::forward<P>(p)...);
		return insert_yield(hash, t);
	}

	IntrusiveHashMapHolder<T> &get_read_only()
	{
		return read_only;
	}

	IntrusiveHashMapHolder<T> &get_read_write()
	{
		return read_write;
	}

private:
	IntrusiveHashMapHolder<T> read_only;
	IntrusiveHashMapHolder<T> read_write;
	ObjectPool<T> object_pool;
	mutable RWSpinLock lock;

	void clear_list(IntrusiveList<T> &list)
	{
		auto itr = list.begin();
		while (itr != list.end())
		{
			auto *to_free = itr.get();
			itr = list.erase(itr);
			object_pool.free(to_free);
		}
	}
};
}
