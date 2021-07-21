#pragma once

//httpMessage: base class for httpRequest and httpResponse
//provides shared functionality

namespace nall::HTTP {

struct Variable {
  string name;
  string value;
};

struct SharedVariable {
  SharedVariable(const nall::string& name = "", const nall::string& value = "") : shared(new Variable{name, value}) {}

  explicit operator bool() const { return (bool)shared->name; }
  auto operator()() const { return shared->value; }
  auto& operator=(const nall::string& value) { shared->value = value; return *this; }

  auto name() const { return shared->name; }
  auto value() const { return shared->value; }
  auto string() const { return nall::string{shared->value}.strip().replace("\r", ""); }
  auto boolean() const { return string() == "true"; }
  auto integer() const { return string().integer(); }
  auto natural() const { return string().natural(); }
  auto real() const { return string().real(); }

  auto& setName(const nall::string& name) { shared->name = name; return *this; }
  auto& setValue(const nall::string& value = "") { shared->value = value; return *this; }

  shared_pointer<Variable> shared;
};

struct Variables {
  auto operator[](const string& name) const -> SharedVariable {
    for(auto& variable : variables) {
      if(variable.shared->name.iequals(name)) return variable;
    }
    return {};
  }

  auto operator()(const string& name) -> SharedVariable {
    for(auto& variable : variables) {
      if(variable.shared->name.iequals(name)) return variable;
    }
    return append(name);
  }

  auto find(const string& name) const -> vector<SharedVariable> {
    vector<SharedVariable> result;
    for(auto& variable : variables) {
      if(variable.shared->name.iequals(name)) result.append(variable);
    }
    return result;
  }

  auto assign(const string& name, const string& value = "") -> SharedVariable {
    for(auto& variable : variables) {
      if(variable.shared->name.iequals(name)) {
        variable.shared->value = value;
        return variable;
      }
    }
    return append(name, value);
  }

  auto append(const string& name, const string& value = "") -> SharedVariable {
    SharedVariable variable{name, value};
    variables.append(variable);
    return variable;
  }

  auto remove(const string& name) -> void {
    for(auto n : reverse(range(variables.size()))) {
      if(variables[n].shared->name.iequals(name)) variables.remove(n);
    }
  }

  auto size() const { return variables.size(); }
  auto begin() const { return variables.begin(); }
  auto end() const { return variables.end(); }
  auto begin() { return variables.begin(); }
  auto end() { return variables.end(); }

  vector<SharedVariable> variables;
};

struct Message {
  using type = Message;

  virtual auto head(const function<bool (const uint8_t* data, uint size)>& callback) const -> bool = 0;
  virtual auto setHead() -> bool = 0;

  virtual auto body(const function<bool (const uint8_t* data, uint size)>& callback) const -> bool = 0;
  virtual auto setBody() -> bool = 0;

  Variables header;

//private:
  string _head;
  string _body;
};

}
