namespace ares::Core {
  struct Object;
  struct System;
  struct Peripheral;
  struct Port;
  namespace Component {
    struct Component;
    struct RealTimeClock;
  }
  namespace Video {
    struct Video;
    struct Sprite;
    struct Screen;
  }
  namespace Audio {
    struct Audio;
    struct Stream;
  }
  namespace Input {
    struct Input;
    struct Button;
    struct Axis;
    struct Trigger;
    struct Rumble;
  }
  namespace Setting {
    struct Setting;
    struct Boolean;
    struct Natural;
    struct Integer;
    struct Real;
    struct String;
  }
  namespace Debugger {
    struct Debugger;
    struct Memory;
    struct Graphics;
    struct Properties;
    namespace Tracer {
      struct Tracer;
      struct Notification;
      struct Instruction;
    }
  }
}

namespace ares::Node {
  using Object           = shared_pointer<Core::Object>;
  using System           = shared_pointer<Core::System>;
  using Peripheral       = shared_pointer<Core::Peripheral>;
  using Port             = shared_pointer<Core::Port>;
  namespace Component {
    using Component      = shared_pointer<Core::Component::Component>;
    using RealTimeClock  = shared_pointer<Core::Component::RealTimeClock>;
  }
  namespace Video {
    using Video          = shared_pointer<Core::Video::Video>;
    using Sprite         = shared_pointer<Core::Video::Sprite>;
    using Screen         = shared_pointer<Core::Video::Screen>;
  }
  namespace Audio {
    using Audio          = shared_pointer<Core::Audio::Audio>;
    using Stream         = shared_pointer<Core::Audio::Stream>;
  }
  namespace Input {
    using Input          = shared_pointer<Core::Input::Input>;
    using Button         = shared_pointer<Core::Input::Button>;
    using Axis           = shared_pointer<Core::Input::Axis>;
    using Trigger        = shared_pointer<Core::Input::Trigger>;
    using Rumble         = shared_pointer<Core::Input::Rumble>;
  }
  namespace Setting {
    using Setting        = shared_pointer<Core::Setting::Setting>;
    using Boolean        = shared_pointer<Core::Setting::Boolean>;
    using Natural        = shared_pointer<Core::Setting::Natural>;
    using Integer        = shared_pointer<Core::Setting::Integer>;
    using Real           = shared_pointer<Core::Setting::Real>;
    using String         = shared_pointer<Core::Setting::String>;
  }
  namespace Debugger {
    using Debugger       = shared_pointer<Core::Debugger::Debugger>;
    using Memory         = shared_pointer<Core::Debugger::Memory>;
    using Graphics       = shared_pointer<Core::Debugger::Graphics>;
    using Properties     = shared_pointer<Core::Debugger::Properties>;
    namespace Tracer {
      using Tracer       = shared_pointer<Core::Debugger::Tracer::Tracer>;
      using Notification = shared_pointer<Core::Debugger::Tracer::Notification>;
      using Instruction  = shared_pointer<Core::Debugger::Tracer::Instruction>;
    }
  }
}

namespace ares::Core {
  // <ares/platform.hpp> forward declarations
  static auto PlatformAttach(Node::Object) -> void;
  static auto PlatformDetach(Node::Object) -> void;
  static auto PlatformLog(string_view) -> void;

  #include <ares/node/attribute.hpp>
  #include <ares/node/class.hpp>
  #include <ares/node/object.hpp>
  #include <ares/node/system.hpp>
  #include <ares/node/peripheral.hpp>
  #include <ares/node/port.hpp>
  namespace Component {
    #include <ares/node/component/component.hpp>
    #include <ares/node/component/real-time-clock.hpp>
  }
  namespace Video {
    #include <ares/node/video/video.hpp>
    #include <ares/node/video/sprite.hpp>
    #include <ares/node/video/screen.hpp>
  }
  namespace Audio {
    #include <ares/node/audio/audio.hpp>
    #include <ares/node/audio/stream.hpp>
  }
  namespace Input {
    #include <ares/node/input/input.hpp>
    #include <ares/node/input/button.hpp>
    #include <ares/node/input/axis.hpp>
    #include <ares/node/input/trigger.hpp>
    #include <ares/node/input/rumble.hpp>
  }
  namespace Setting {
    #include <ares/node/setting/setting.hpp>
    #include <ares/node/setting/boolean.hpp>
    #include <ares/node/setting/natural.hpp>
    #include <ares/node/setting/integer.hpp>
    #include <ares/node/setting/real.hpp>
    #include <ares/node/setting/string.hpp>
  }
  namespace Debugger {
    #include <ares/node/debugger/debugger.hpp>
    #include <ares/node/debugger/memory.hpp>
    #include <ares/node/debugger/graphics.hpp>
    #include <ares/node/debugger/properties.hpp>
    namespace Tracer {
      #include <ares/node/debugger/tracer/tracer.hpp>
      #include <ares/node/debugger/tracer/notification.hpp>
      #include <ares/node/debugger/tracer/instruction.hpp>
    }
  }
}

namespace ares::Node {
  static inline auto create(string identifier) -> Object {
    return Core::Class::create(identifier);
  }

  static inline auto serialize(Object node) -> string {
    if(!node) return {};
    string result;
    node->serialize(result, {});
    return result;
  }

  static inline auto unserialize(string markup) -> Object {
    auto document = BML::unserialize(markup);
    if(!document) return {};
    auto node = Core::Class::create(document["node"].string());
    node->unserialize(document["node"]);
    return node;
  }

  static inline auto parent(Object child) -> Object {
    if(!child || !child->parent()) return {};
    if(auto parent = child->parent().acquire()) return parent;
    return {};
  }

  template<typename T>
  static inline auto find(Object from, string name) -> Object {
    if(!from) return {};
    if(auto object = from->find<T>(name)) return object;
    return {};
  }

  template<typename T>
  static inline auto enumerate(Object from) -> vector<T> {
    vector<T> objects;
    if(from) from->enumerate<T>(objects);
    return objects;
  }
}
