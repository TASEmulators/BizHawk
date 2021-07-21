#pragma once

//XML v1.0 subset parser
//revision 0.04

namespace nall::XML {

//metadata:
//  0 = element
//  1 = attribute

struct ManagedNode;
using SharedNode = shared_pointer<ManagedNode>;

struct ManagedNode : Markup::ManagedNode {
protected:
  inline string escape() const {
    string result = _value;
    result.replace("&", "&amp;");
    result.replace("<", "&lt;");
    result.replace(">", "&gt;");
    if(_metadata == 1) {
      result.replace("\'", "&apos;");
      result.replace("\"", "&quot;");
    }
    return result;
  }

  inline bool isName(char c) const {
    if(c >= 'A' && c <= 'Z') return true;
    if(c >= 'a' && c <= 'z') return true;
    if(c >= '0' && c <= '9') return true;
    if(c == '.' || c == '_') return true;
    if(c == '?') return true;
    return false;
  }

  inline bool isWhitespace(char c) const {
    if(c ==  ' ' || c == '\t') return true;
    if(c == '\r' || c == '\n') return true;
    return false;
  }

  //copy part of string from source document into target string; decode markup while copying
  inline void copy(string& target, const char* source, uint length) {
    target.reserve(length + 1);

    #if defined(NALL_XML_LITERAL)
    memory::copy(target.pointer(), source, length);
    target[length] = 0;
    return;
    #endif

    char* output = target.get();
    while(length) {
      if(*source == '&') {
        if(!memory::compare(source, "&lt;",   4)) { *output++ = '<';  source += 4; length -= 4; continue; }
        if(!memory::compare(source, "&gt;",   4)) { *output++ = '>';  source += 4; length -= 4; continue; }
        if(!memory::compare(source, "&amp;",  5)) { *output++ = '&';  source += 5; length -= 5; continue; }
        if(!memory::compare(source, "&apos;", 6)) { *output++ = '\''; source += 6; length -= 6; continue; }
        if(!memory::compare(source, "&quot;", 6)) { *output++ = '\"'; source += 6; length -= 6; continue; }
      }

      if(_metadata == 0 && source[0] == '<' && source[1] == '!') {
        //comment
        if(!memory::compare(source, "<!--", 4)) {
          source += 4, length -= 4;
          while(memory::compare(source, "-->", 3)) source++, length--;
          source += 3, length -= 3;
          continue;
        }

        //CDATA
        if(!memory::compare(source, "<![CDATA[", 9)) {
          source += 9, length -= 9;
          while(memory::compare(source, "]]>", 3)) *output++ = *source++, length--;
          source += 3, length -= 3;
          continue;
        }
      }

      *output++ = *source++, length--;
    }
    *output = 0;
  }

  inline bool parseExpression(const char*& p) {
    if(*(p + 1) != '!') return false;

    //comment
    if(!memory::compare(p, "<!--", 4)) {
      while(*p && memory::compare(p, "-->", 3)) p++;
      if(!*p) throw "unclosed comment";
      p += 3;
      return true;
    }

    //CDATA
    if(!memory::compare(p, "<![CDATA[", 9)) {
      while(*p && memory::compare(p, "]]>", 3)) p++;
      if(!*p) throw "unclosed CDATA";
      p += 3;
      return true;
    }

    //DOCTYPE
    if(!memory::compare(p, "<!DOCTYPE", 9)) {
      uint counter = 0;
      do {
        char n = *p++;
        if(!n) throw "unclosed DOCTYPE";
        if(n == '<') counter++;
        if(n == '>') counter--;
      } while(counter);
      return true;
    }

    return false;
  }

  //returns true if tag closes itself (<tag/>); false if not (<tag>)
  inline bool parseHead(const char*& p) {
    //parse name
    const char* nameStart = ++p;  //skip '<'
    while(isName(*p)) p++;
    const char* nameEnd = p;
    copy(_name, nameStart, nameEnd - nameStart);
    if(!_name) throw "missing element name";

    //parse attributes
    while(*p) {
      while(isWhitespace(*p)) p++;
      if(!*p) throw "unclosed attribute";
      if(*p == '?' || *p == '/' || *p == '>') break;

      //parse attribute name
      SharedNode attribute(new ManagedNode);
      attribute->_metadata = 1;

      const char* nameStart = p;
      while(isName(*p)) p++;
      const char* nameEnd = p;
      copy(attribute->_name, nameStart, nameEnd - nameStart);
      if(!attribute->_name) throw "missing attribute name";

      //parse attribute data
      if(*p++ != '=') throw "missing attribute value";
      char terminal = *p++;
      if(terminal != '\'' && terminal != '\"') throw "attribute value not quoted";
      const char* dataStart = p;
      while(*p && *p != terminal) p++;
      if(!*p) throw "missing attribute data terminal";
      const char* dataEnd = p++;  //skip closing terminal

      copy(attribute->_value, dataStart, dataEnd - dataStart);
      _children.append(attribute);
    }

    //parse closure
    if(*p == '?' && *(p + 1) == '>') { p += 2; return true; }
    if(*p == '/' && *(p + 1) == '>') { p += 2; return true; }
    if(*p == '>') { p += 1; return false; }
    throw "invalid element tag";
  }

  //parse element and all of its child elements
  inline void parseElement(const char*& p) {
    SharedNode node(new ManagedNode);
    if(node->parseHead(p) == false) node->parse(p);
    _children.append(node);
  }

  //return true if </tag> matches this node's name
  inline bool parseClosureElement(const char*& p) {
    if(p[0] != '<' || p[1] != '/') return false;
    p += 2;
    const char* nameStart = p;
    while(*p && *p != '>') p++;
    if(*p != '>') throw "unclosed closure element";
    const char* nameEnd = p++;
    if(memory::compare(_name.data(), nameStart, nameEnd - nameStart)) throw "closure element name mismatch";
    return true;
  }

  //parse contents of an element
  inline void parse(const char*& p) {
    const char* dataStart = p;
    const char* dataEnd = p;

    while(*p) {
      while(*p && *p != '<') p++;
      if(!*p) break;
      dataEnd = p;
      if(parseClosureElement(p) == true) break;
      if(parseExpression(p) == true) continue;
      parseElement(p);
    }

    copy(_value, dataStart, dataEnd - dataStart);
  }

  friend auto unserialize(const string&) -> Markup::SharedNode;
};

inline auto unserialize(const string& markup) -> Markup::SharedNode {
  auto node = new ManagedNode;
  try {
    const char* p = markup;
    node->parse(p);
  } catch(const char* error) {
    delete node;
    node = nullptr;
  }
  return node;
}

}
