#pragma once

namespace nall {

struct Locale {
  struct Dictionary {
    string location;
    string language;
    Markup::Node document;
  };

  auto scan(string pathname) -> void {
    dictionaries.reset();
    selected.reset();
    for(auto filename : directory::icontents(pathname, "*.bml")) {
      Dictionary dictionary;
      dictionary.location = {pathname, filename};
      dictionary.document = BML::unserialize(string::read(dictionary.location));
      dictionary.language = dictionary.document["locale/language"].text();
      dictionaries.append(dictionary);
    }
  }

  auto available() const -> vector<string> {
    vector<string> result;
    for(auto& dictionary : dictionaries) {
      result.append(dictionary.language);
    }
    return result;
  }

  auto select(string option) -> bool {
    selected.reset();
    for(auto& dictionary : dictionaries) {
      if(option == Location::prefix(dictionary.location) || option == dictionary.language) {
        selected = dictionary;
        return true;
      }
    }
    return false;
  }

  template<typename... P>
  auto operator()(string ns, string input, P&&... p) const -> string {
    vector<string> arguments{forward<P>(p)...};
    if(selected) {
      for(auto node : selected().document) {
        if(node.name() == "namespace" && node.text() == ns) {
          for(auto map : node) {
            if(map.name() == "map" && map["input"].text() == input) {
              input = map["value"].text();
              break;
            }
          }
        }
      }
    }
    for(uint index : range(arguments.size())) {
      input.replace({"{", index, "}"}, arguments[index]);
    }
    return input;
  }

  struct Namespace {
    Namespace(Locale& _locale, string _namespace) : _locale(_locale), _namespace(_namespace) {}

    template<typename... P>
    auto operator()(string input, P&&... p) const -> string {
      return _locale(_namespace, input, forward<P>(p)...);
    }

    template<typename... P>
    auto tr(string input, P&&... p) const -> string {
      return _locale(_namespace, input, forward<P>(p)...);
    }

  private:
    Locale& _locale;
    string _namespace;
  };

private:
  vector<Dictionary> dictionaries;
  maybe<Dictionary&> selected;
};

}
