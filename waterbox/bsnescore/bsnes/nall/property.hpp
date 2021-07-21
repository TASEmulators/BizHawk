#if !defined(property)
  #define property1(declaration) public: declaration
  #define property2(declaration, getter) public: __declspec(property(get=getter)) declaration; protected: declaration##_
  #define property3(declaration, getter, setter) public: __declspec(property(get=getter, put=setter)) declaration; protected: declaration##_
  #define property_(_1, _2, _3, name, ...) name
  #define property(...) property_(__VA_ARGS__, property3, property2, property1)(__VA_ARGS__)
#else
  #undef property1
  #undef property2
  #undef property3
  #undef property_
  #undef property
#endif
