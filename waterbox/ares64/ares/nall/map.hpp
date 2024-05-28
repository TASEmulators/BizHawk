#pragma once

#include <nall/set.hpp>

namespace nall {

template<typename T, typename U> struct map {
  struct node_t {
    T key;
    U value;
    node_t() = default;
    node_t(const T& key) : key(key) {}
    node_t(const T& key, const U& value) : key(key), value(value) {}
    auto operator< (const node_t& source) const -> bool { return key <  source.key; }
    auto operator==(const node_t& source) const -> bool { return key == source.key; }
  };

  auto find(const T& key) const -> maybe<U&> {
    if(auto node = root.find({key})) return node().value;
    return nothing;
  }

  auto insert(const T& key, const U& value) -> void { root.insert({key, value}); }
  auto remove(const T& key) -> void { root.remove({key}); }
  auto size() const -> unsigned { return root.size(); }
  auto reset() -> void { root.reset(); }

  auto begin() -> typename set<node_t>::iterator { return root.begin(); }
  auto end() -> typename set<node_t>::iterator { return root.end(); }

  auto begin() const -> const typename set<node_t>::iterator { return root.begin(); }
  auto end() const -> const typename set<node_t>::iterator { return root.end(); }

protected:
  set<node_t> root;
};

template<typename T, typename U> struct bimap {
  auto find(const T& key) const -> maybe<U&> { return tmap.find(key); }
  auto find(const U& key) const -> maybe<T&> { return umap.find(key); }
  auto insert(const T& key, const U& value) -> void { tmap.insert(key, value); umap.insert(value, key); }
  auto remove(const T& key) -> void { if(auto p = tmap.find(key)) { umap.remove(p().value); tmap.remove(key); } }
  auto remove(const U& key) -> void { if(auto p = umap.find(key)) { tmap.remove(p().value); umap.remove(key); } }
  auto size() const -> unsigned { return tmap.size(); }
  auto reset() -> void { tmap.reset(); umap.reset(); }

  auto begin() -> typename set<typename map<T, U>::node_t>::iterator { return tmap.begin(); }
  auto end() -> typename set<typename map<T, U>::node_t>::iterator { return tmap.end(); }

  auto begin() const -> const typename set<typename map<T, U>::node_t>::iterator { return tmap.begin(); }
  auto end() const -> const typename set<typename map<T, U>::node_t>::iterator { return tmap.end(); }

protected:
  map<T, U> tmap;
  map<U, T> umap;
};

}
