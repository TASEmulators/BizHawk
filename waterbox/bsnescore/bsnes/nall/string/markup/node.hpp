#pragma once

namespace nall::Markup {

struct Node;
struct ManagedNode;
using SharedNode = shared_pointer<ManagedNode>;

struct ManagedNode {
  ManagedNode() = default;
  ManagedNode(const string& name) : _name(name) {}
  ManagedNode(const string& name, const string& value) : _name(name), _value(value) {}

  auto clone() const -> SharedNode {
    SharedNode clone{new ManagedNode(_name, _value)};
    for(auto& child : _children) {
      clone->_children.append(child->clone());
    }
    return clone;
  }

  auto copy(SharedNode source) -> void {
    _name = source->_name;
    _value = source->_value;
    _metadata = source->_metadata;
    _children.reset();
    for(auto child : source->_children) {
      _children.append(child->clone());
    }
  }

protected:
  string _name;
  string _value;
  uintptr _metadata = 0;
  vector<SharedNode> _children;

  inline auto _evaluate(string query) const -> bool;
  inline auto _find(const string& query) const -> vector<Node>;
  inline auto _lookup(const string& path) const -> Node;
  inline auto _create(const string& path) -> Node;

  friend class Node;
};

struct Node {
  Node() : shared(new ManagedNode) {}
  Node(const SharedNode& source) : shared(source ? source : new ManagedNode) {}
  Node(const nall::string& name) : shared(new ManagedNode(name)) {}
  Node(const nall::string& name, const nall::string& value) : shared(new ManagedNode(name, value)) {}

  auto unique() const -> bool { return shared.unique(); }
  auto clone() const -> Node { return shared->clone(); }
  auto copy(Node source) -> void { return shared->copy(source.shared); }

  explicit operator bool() const { return shared->_name || shared->_children; }
  auto name() const -> nall::string { return shared->_name; }
  auto value() const -> nall::string { return shared->_value; }

  auto value(nall::string& target) const -> bool { if(shared) target = string(); return (bool)shared; }
  auto value(bool& target) const -> bool { if(shared) target = boolean(); return (bool)shared; }
  auto value(int& target) const -> bool { if(shared) target = integer(); return (bool)shared; }
  auto value(uint& target) const -> bool { if(shared) target = natural(); return (bool)shared; }
  auto value(double& target) const -> bool { if(shared) target = real(); return (bool)shared; }

  auto text() const -> nall::string { return value().strip(); }
  auto string() const -> nall::string { return value().strip(); }
  auto boolean() const -> bool { return text() == "true"; }
  auto integer() const -> int64_t { return text().integer(); }
  auto natural() const -> uint64_t { return text().natural(); }
  auto real() const -> double { return text().real(); }

  auto text(const nall::string& fallback) const -> nall::string { return bool(*this) ? text() : fallback; }
  auto string(const nall::string& fallback) const -> nall::string { return bool(*this) ? string() : fallback; }
  auto boolean(bool fallback) const -> bool { return bool(*this) ? boolean() : fallback; }
  auto integer(int64_t fallback) const -> int64_t { return bool(*this) ? integer() : fallback; }
  auto natural(uint64_t fallback) const -> uint64_t { return bool(*this) ? natural() : fallback; }
  auto real(double fallback) const -> double { return bool(*this) ? real() : fallback; }

  auto setName(const nall::string& name = "") -> Node& { shared->_name = name; return *this; }
  auto setValue(const nall::string& value = "") -> Node& { shared->_value = value; return *this; }

  auto reset() -> void { shared->_children.reset(); }
  auto size() const -> uint { return shared->_children.size(); }

  auto prepend(const Node& node) -> void { shared->_children.prepend(node.shared); }
  auto append(const Node& node) -> void { shared->_children.append(node.shared); }
  auto remove(const Node& node) -> bool {
    for(auto n : range(size())) {
      if(node.shared == shared->_children[n]) {
        return shared->_children.remove(n), true;
      }
    }
    return false;
  }

  auto insert(uint position, const Node& node) -> bool {
    if(position > size()) return false;  //used > instead of >= to allow indexed-equivalent of append()
    return shared->_children.insert(position, node.shared), true;
  }

  auto remove(uint position) -> bool {
    if(position >= size()) return false;
    return shared->_children.remove(position), true;
  }

  auto swap(uint x, uint y) -> bool {
    if(x >= size() || y >= size()) return false;
    return std::swap(shared->_children[x], shared->_children[y]), true;
  }

  auto sort(function<bool (Node, Node)> comparator = [](auto x, auto y) {
    return nall::string::compare(x.shared->_name, y.shared->_name) < 0;
  }) -> void {
    nall::sort(shared->_children.data(), shared->_children.size(), [&](auto x, auto y) {
      return comparator(x, y);  //this call converts SharedNode objects to Node objects
    });
  }

  auto operator[](int position) -> Node {
    if(position >= size()) return {};
    return shared->_children[position];
  }

  auto operator[](const nall::string& path) const -> Node { return shared->_lookup(path); }
  auto operator()(const nall::string& path) -> Node { return shared->_create(path); }
  auto find(const nall::string& query) const -> vector<Node> { return shared->_find(query); }

  struct iterator {
    auto operator*() -> Node { return {source.shared->_children[position]}; }
    auto operator!=(const iterator& source) const -> bool { return position != source.position; }
    auto operator++() -> iterator& { return position++, *this; }
    iterator(const Node& source, uint position) : source(source), position(position) {}

  private:
    const Node& source;
    uint position;
  };

  auto begin() const -> iterator { return iterator(*this, 0); }
  auto end() const -> iterator { return iterator(*this, size()); }

protected:
  SharedNode shared;
};

}
