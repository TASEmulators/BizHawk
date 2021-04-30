#pragma once

//set
//implementation: red-black tree
//
//search: O(log n) average; O(log n) worst
//insert: O(log n) average; O(log n) worst
//remove: O(log n) average; O(log n) worst
//
//requirements:
//  bool T::operator==(const T&) const;
//  bool T::operator< (const T&) const;

#include <nall/utility.hpp>
#include <nall/vector.hpp>

namespace nall {

template<typename T> struct set {
  struct node_t {
    T value;
    bool red = 1;
    node_t* link[2] = {nullptr, nullptr};
    node_t() = default;
    node_t(const T& value) : value(value) {}
  };

  node_t* root = nullptr;
  uint nodes = 0;

  set() = default;
  set(const set& source) { operator=(source); }
  set(set&& source) { operator=(move(source)); }
  set(std::initializer_list<T> list) { for(auto& value : list) insert(value); }
  ~set() { reset(); }

  auto operator=(const set& source) -> set& {
    if(this == &source) return *this;
    reset();
    copy(root, source.root);
    nodes = source.nodes;
    return *this;
  }

  auto operator=(set&& source) -> set& {
    if(this == &source) return *this;
    root = source.root;
    nodes = source.nodes;
    source.root = nullptr;
    source.nodes = 0;
    return *this;
  }

  explicit operator bool() const { return nodes; }
  auto size() const -> uint { return nodes; }

  auto reset() -> void {
    reset(root);
    nodes = 0;
  }

  auto find(const T& value) -> maybe<T&> {
    if(node_t* node = find(root, value)) return node->value;
    return nothing;
  }

  auto find(const T& value) const -> maybe<const T&> {
    if(node_t* node = find(root, value)) return node->value;
    return nothing;
  }

  auto insert(const T& value) -> maybe<T&> {
    uint count = size();
    node_t* v = insert(root, value);
    root->red = 0;
    if(size() == count) return nothing;
    return v->value;
  }

  template<typename... P> auto insert(const T& value, P&&... p) -> bool {
    bool result = insert(value);
    insert(forward<P>(p)...) | result;
    return result;
  }

  auto remove(const T& value) -> bool {
    uint count = size();
    bool done = 0;
    remove(root, &value, done);
    if(root) root->red = 0;
    return size() < count;
  }

  template<typename... P> auto remove(const T& value, P&&... p) -> bool {
    bool result = remove(value);
    return remove(forward<P>(p)...) | result;
  }

  struct base_iterator {
    auto operator!=(const base_iterator& source) const -> bool { return position != source.position; }

    auto operator++() -> base_iterator& {
      if(++position >= source.size()) { position = source.size(); return *this; }

      if(stack.right()->link[1]) {
        stack.append(stack.right()->link[1]);
        while(stack.right()->link[0]) stack.append(stack.right()->link[0]);
      } else {
        node_t* child;
        do child = stack.takeRight();
        while(child == stack.right()->link[1]);
      }

      return *this;
    }

    base_iterator(const set& source, uint position) : source(source), position(position) {
      node_t* node = source.root;
      while(node) {
        stack.append(node);
        node = node->link[0];
      }
    }

  protected:
    const set& source;
    uint position;
    vector<node_t*> stack;
  };

  struct iterator : base_iterator {
    iterator(const set& source, uint position) : base_iterator(source, position) {}
    auto operator*() const -> T& { return base_iterator::stack.right()->value; }
  };

  auto begin() -> iterator { return iterator(*this, 0); }
  auto end() -> iterator { return iterator(*this, size()); }

  struct const_iterator : base_iterator {
    const_iterator(const set& source, uint position) : base_iterator(source, position) {}
    auto operator*() const -> const T& { return base_iterator::stack.right()->value; }
  };

  auto begin() const -> const const_iterator { return const_iterator(*this, 0); }
  auto end() const -> const const_iterator { return const_iterator(*this, size()); }

private:
  auto reset(node_t*& node) -> void {
    if(!node) return;
    if(node->link[0]) reset(node->link[0]);
    if(node->link[1]) reset(node->link[1]);
    delete node;
    node = nullptr;
  }

  auto copy(node_t*& target, const node_t* source) -> void {
    if(!source) return;
    target = new node_t(source->value);
    target->red = source->red;
    copy(target->link[0], source->link[0]);
    copy(target->link[1], source->link[1]);
  }

  auto find(node_t* node, const T& value) const -> node_t* {
    if(node == nullptr) return nullptr;
    if(node->value == value) return node;
    return find(node->link[node->value < value], value);
  }

  auto red(node_t* node) const -> bool { return node && node->red; }
  auto black(node_t* node) const -> bool { return !red(node); }

  auto rotate(node_t*& a, bool dir) -> void {
    node_t*& b = a->link[!dir];
    node_t*& c = b->link[dir];
    a->red = 1, b->red = 0;
    std::swap(a, b);
    std::swap(b, c);
  }

  auto rotateTwice(node_t*& node, bool dir) -> void {
    rotate(node->link[!dir], !dir);
    rotate(node, dir);
  }

  auto insert(node_t*& node, const T& value) -> node_t* {
    if(!node) { nodes++; node = new node_t(value); return node; }
    if(node->value == value) { node->value = value; return node; }  //prevent duplicate entries

    bool dir = node->value < value;
    node_t* v = insert(node->link[dir], value);
    if(black(node->link[dir])) return v;

    if(red(node->link[!dir])) {
      node->red = 1;
      node->link[0]->red = 0;
      node->link[1]->red = 0;
    } else if(red(node->link[dir]->link[dir])) {
      rotate(node, !dir);
    } else if(red(node->link[dir]->link[!dir])) {
      rotateTwice(node, !dir);
    }

    return v;
  }

  auto balance(node_t*& node, bool dir, bool& done) -> void {
    node_t* p = node;
    node_t* s = node->link[!dir];
    if(!s) return;

    if(red(s)) {
      rotate(node, dir);
      s = p->link[!dir];
    }

    if(black(s->link[0]) && black(s->link[1])) {
      if(red(p)) done = 1;
      p->red = 0, s->red = 1;
    } else {
      bool save = p->red;
      bool head = node == p;

      if(red(s->link[!dir])) rotate(p, dir);
      else rotateTwice(p, dir);

      p->red = save;
      p->link[0]->red = 0;
      p->link[1]->red = 0;

      if(head) node = p;
      else node->link[dir] = p;

      done = 1;
    }
  }

  auto remove(node_t*& node, const T* value, bool& done) -> void {
    if(!node) { done = 1; return; }

    if(node->value == *value) {
      if(!node->link[0] || !node->link[1]) {
        node_t* save = node->link[!node->link[0]];

        if(red(node)) done = 1;
        else if(red(save)) save->red = 0, done = 1;

        nodes--;
        delete node;
        node = save;
        return;
      } else {
        node_t* heir = node->link[0];
        while(heir->link[1]) heir = heir->link[1];
        node->value = heir->value;
        value = &heir->value;
      }
    }

    bool dir = node->value < *value;
    remove(node->link[dir], value, done);
    if(!done) balance(node, dir, done);
  }
};

}
