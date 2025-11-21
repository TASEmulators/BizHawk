using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;

namespace BizHawk.Common
{
	/// <summary>
	/// High-performance LRU cache implementation for reducing memory allocations
	/// and improving emulation performance by caching frequently accessed data.
	/// Thread-safe for concurrent access.
	/// </summary>
	/// <typeparam name="TKey">Type of the cache key</typeparam>
	/// <typeparam name="TValue">Type of the cached value</typeparam>
	public class OptimizedLRUCache<TKey, TValue> where TKey : notnull
	{
		private sealed class CacheNode
		{
			public readonly TKey Key;
			public TValue Value;
			public CacheNode? Prev;
			public CacheNode? Next;

			public CacheNode(TKey key, TValue value)
			{
				Key = key;
				Value = value;
			}
		}

		private readonly int _capacity;
		private readonly Dictionary<TKey, CacheNode> _cache;
		private readonly CacheNode _head;
		private readonly CacheNode _tail;
		private readonly object _lock = new object();
		private int _count;

		/// <summary>
		/// Gets the current number of items in the cache.
		/// </summary>
		public int Count
		{
			get
			{
				lock (_lock)
				{
					return _count;
				}
			}
		}

		/// <summary>
		/// Gets the maximum capacity of the cache.
		/// </summary>
		public int Capacity => _capacity;

		/// <summary>
		/// Initializes a new instance of the OptimizedLRUCache class.
		/// </summary>
		/// <param name="capacity">Maximum number of items to cache</param>
		public OptimizedLRUCache(int capacity)
		{
			if (capacity <= 0)
				throw new ArgumentOutOfRangeException(nameof(capacity), "Capacity must be greater than 0");

			_capacity = capacity;
			_cache = new Dictionary<TKey, CacheNode>(capacity);
			_head = new CacheNode(default!, default!);
			_tail = new CacheNode(default!, default!);
			_head.Next = _tail;
			_tail.Prev = _head;
			_count = 0;
		}

		/// <summary>
		/// Gets a value from the cache. Returns true if found, false otherwise.
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public bool TryGetValue(TKey key, out TValue value)
		{
			lock (_lock)
			{
				if (_cache.TryGetValue(key, out var node))
				{
					// Move to front (most recently used)
					MoveToFront(node);
					value = node.Value;
					return true;
				}

				value = default;
				return false;
			}
		}

		/// <summary>
		/// Adds or updates a value in the cache.
		/// </summary>
		public void Set(TKey key, TValue value)
		{
			lock (_lock)
			{
				if (_cache.TryGetValue(key, out var existingNode))
				{
					// Update existing value
					existingNode.Value = value;
					MoveToFront(existingNode);
				}
				else
				{
					// Add new node
					var newNode = new CacheNode(key, value);
					_cache[key] = newNode;
					AddToFront(newNode);
					_count++;

					// Evict least recently used if over capacity
					if (_count > _capacity)
					{
						RemoveLeastRecentlyUsed();
					}
				}
			}
		}

		/// <summary>
		/// Removes an item from the cache.
		/// </summary>
		public bool Remove(TKey key)
		{
			lock (_lock)
			{
				if (_cache.TryGetValue(key, out var node))
				{
					RemoveNode(node);
					_cache.Remove(key);
					_count--;
					return true;
				}
				return false;
			}
		}

		/// <summary>
		/// Clears all items from the cache.
		/// </summary>
		public void Clear()
		{
			lock (_lock)
			{
				_cache.Clear();
				_head.Next = _tail;
				_tail.Prev = _head;
				_count = 0;
			}
		}

		/// <summary>
		/// Gets or adds a value to the cache using a factory function.
		/// </summary>
		/// <remarks>
		/// Warning: The factory function is executed while holding the cache lock.
		/// Ensure the factory is fast and does not call back into this cache or acquire other locks
		/// to avoid deadlocks and performance issues.
		/// </remarks>
		public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
		{
			lock (_lock)
			{
				if (_cache.TryGetValue(key, out var node))
				{
					MoveToFront(node);
					return node.Value;
				}

				var value = valueFactory(key);
				var newNode = new CacheNode(key, value);
				_cache[key] = newNode;
				AddToFront(newNode);
				_count++;

				if (_count > _capacity)
				{
					RemoveLeastRecentlyUsed();
				}

				return value;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void MoveToFront(CacheNode node)
		{
			RemoveNode(node);
			AddToFront(node);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void AddToFront(CacheNode node)
		{
			node.Next = _head.Next;
			node.Prev = _head;
			_head.Next.Prev = node;
			_head.Next = node;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void RemoveNode(CacheNode node)
		{
			node.Prev.Next = node.Next;
			node.Next.Prev = node.Prev;
		}

		private void RemoveLeastRecentlyUsed()
		{
			var lru = _tail.Prev;
			if (lru != _head)
			{
				RemoveNode(lru);
				_cache.Remove(lru.Key);
				_count--;
			}
		}
	}

	/// <summary>
	/// Simple thread-safe cache with time-based expiration.
	/// Useful for caching expensive computations with limited lifetime.
	/// </summary>
	/// <typeparam name="TKey">Type of the cache key</typeparam>
	/// <typeparam name="TValue">Type of the cached value</typeparam>
	public class TimedCache<TKey, TValue> where TKey : notnull
	{
		private sealed class CacheEntry
		{
			public readonly TValue Value;
			public readonly DateTime ExpirationTime;

			public CacheEntry(TValue value, DateTime expirationTime)
			{
				Value = value;
				ExpirationTime = expirationTime;
			}

			public bool IsExpired => DateTime.UtcNow >= ExpirationTime;
		}

		private readonly Dictionary<TKey, CacheEntry> _cache;
		private readonly TimeSpan _defaultExpiration;
		private readonly object _lock = new object();

		/// <summary>
		/// Initializes a new instance of the TimedCache class.
		/// </summary>
		/// <param name="defaultExpiration">Default time-to-live for cached items</param>
		public TimedCache(TimeSpan defaultExpiration)
		{
			if (defaultExpiration <= TimeSpan.Zero)
				throw new ArgumentOutOfRangeException(nameof(defaultExpiration), "Expiration must be greater than zero");

			_defaultExpiration = defaultExpiration;
			_cache = new Dictionary<TKey, CacheEntry>();
		}

		/// <summary>
		/// Gets a value from the cache if it exists and hasn't expired.
		/// </summary>
		public bool TryGetValue(TKey key, out TValue value)
		{
			lock (_lock)
			{
				if (_cache.TryGetValue(key, out var entry))
				{
					if (!entry.IsExpired)
					{
						value = entry.Value;
						return true;
					}

					// Remove expired entry
					_cache.Remove(key);
				}

				value = default;
				return false;
			}
		}

		/// <summary>
		/// Sets a value in the cache with default expiration time.
		/// </summary>
		public void Set(TKey key, TValue value)
		{
			Set(key, value, _defaultExpiration);
		}

		/// <summary>
		/// Sets a value in the cache with custom expiration time.
		/// </summary>
		public void Set(TKey key, TValue value, TimeSpan expiration)
		{
			lock (_lock)
			{
				var expirationTime = DateTime.UtcNow + expiration;
				_cache[key] = new CacheEntry(value, expirationTime);
			}
		}

		/// <summary>
		/// Gets or adds a value to the cache using a factory function.
		/// </summary>
		/// <remarks>
		/// Warning: The factory function is executed while holding the cache lock.
		/// Ensure the factory is fast and does not call back into this cache or acquire other locks
		/// to avoid deadlocks and performance issues.
		/// </remarks>
		public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory)
		{
			return GetOrAdd(key, valueFactory, _defaultExpiration);
		}

		/// <summary>
		/// Gets or adds a value to the cache using a factory function with custom expiration.
		/// </summary>
		/// <remarks>
		/// Warning: The factory function is executed while holding the cache lock.
		/// Ensure the factory is fast and does not call back into this cache or acquire other locks
		/// to avoid deadlocks and performance issues.
		/// </remarks>
		public TValue GetOrAdd(TKey key, Func<TKey, TValue> valueFactory, TimeSpan expiration)
		{
			lock (_lock)
			{
				if (_cache.TryGetValue(key, out var entry) && !entry.IsExpired)
				{
					return entry.Value;
				}

				var value = valueFactory(key);
				var expirationTime = DateTime.UtcNow + expiration;
				_cache[key] = new CacheEntry(value, expirationTime);
				return value;
			}
		}

		/// <summary>
		/// Removes an item from the cache.
		/// </summary>
		public bool Remove(TKey key)
		{
			lock (_lock)
			{
				return _cache.Remove(key);
			}
		}

		/// <summary>
		/// Clears all items from the cache.
		/// </summary>
		public void Clear()
		{
			lock (_lock)
			{
				_cache.Clear();
			}
		}

		/// <summary>
		/// Removes all expired entries from the cache.
		/// Call this periodically to prevent memory buildup.
		/// </summary>
		public int CleanupExpired()
		{
			lock (_lock)
			{
				var keysToRemove = _cache.Where(kvp => kvp.Value.IsExpired).Select(kvp => kvp.Key).ToList();

				foreach (var key in keysToRemove)
				{
					_cache.Remove(key);
				}

				return keysToRemove.Count;
			}
		}
	}
}
