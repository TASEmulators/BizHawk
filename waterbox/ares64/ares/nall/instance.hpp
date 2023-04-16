#pragma once

namespace nall {

template<typename T>
struct Instance {
  ~Instance() {
    destruct();
  }

  auto operator()() -> T& {
    return instance.object;
  }

  template<typename... P>
  auto construct(P&&... p) {
    if(constructed) return;
    constructed = true;
    new((void*)(&instance.object)) T(forward<P>(p)...);
  }

  auto destruct() -> void {
    if(!constructed) return;
    constructed = false;
    instance.object.~T();
  }

private:
  bool constructed = false;
  union Union {
    Union() {}
    ~Union() {}

    T object;
    char storage[sizeof(T)];
  } instance;
};

}
