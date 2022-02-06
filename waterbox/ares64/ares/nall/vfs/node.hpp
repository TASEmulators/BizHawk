namespace nall::vfs {

enum class mode : u32 { read, write };
static constexpr auto read  = mode::read;
static constexpr auto write = mode::write;

enum class index : u32 { absolute, relative };
static constexpr auto absolute = index::absolute;
static constexpr auto relative = index::relative;

struct node {
  virtual ~node() = default;

  auto isFile() const -> bool;
  auto isDirectory() const -> bool;

  auto name() const -> string { return _name; }
  auto setName(const string& name) -> void { _name = name; }

  template<typename T = string>
  auto attribute(const string& name) const -> T {
    if(auto attribute = _attributes.find(name)) {
      if(attribute->value.is<T>()) return attribute->value.get<T>();
    }
    return {};
  }

  template<typename T = string>
  auto hasAttribute(const string& name) const -> bool {
    if(auto attribute = _attributes.find(name)) {
      if(attribute->value.is<T>()) return true;
    }
    return false;
  }

  template<typename T = string, typename U = string>
  auto setAttribute(const string& name, const U& value = {}) -> void {
    if constexpr(is_same_v<T, string> && !is_same_v<U, string>) return setAttribute(name, string{value});
    if(auto attribute = _attributes.find(name)) {
      if((const T&)value) attribute->value = (const T&)value;
      else _attributes.remove(*attribute);
    } else {
      if((const T&)value) _attributes.insert({name, (const T&)value});
    }
  }

protected:
  string _name;
  set<vfs::attribute> _attributes;
};

}
