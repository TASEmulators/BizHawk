//horrible implementation of run-time introspection:
//allow converting a unique class string to a derived Node type.

struct Class {
  struct Instance {
    const string identifier;
    const function<Node::Object ()> create;
  };

  static auto classes() -> vector<Instance>& {
    static vector<Instance> classes;
    return classes;
  }

  template<typename T> static auto register() -> void {
    if(!classes().find([&](auto instance) { return instance.identifier == T::identifier(); })) {
      classes().append({T::identifier(), &T::create});
    } else {
      throw;
    }
  }

  static auto create(string identifier) -> Node::Object {
    if(auto index = classes().find([&](auto instance) { return instance.identifier == identifier; })) {
      return classes()[*index].create();
    }
    if(identifier == "Object") throw;  //should never occur: detects unregistered classes
    return create("Object");
  }

  template<typename T> struct Register {
    Register() { Class::register<T>(); }
  };
};
