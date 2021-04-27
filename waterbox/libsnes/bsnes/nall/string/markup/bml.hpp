#pragma once

//BML v1.0 parser
//revision 0.04

namespace nall::BML {

//metadata is used to store nesting level

struct ManagedNode;
using SharedNode = shared_pointer<ManagedNode>;

struct ManagedNode : Markup::ManagedNode {
protected:
  //test to verify if a valid character for a node name
  auto valid(char p) const -> bool {  //A-Z, a-z, 0-9, -.
    return p - 'A' < 26u || p - 'a' < 26u || p - '0' < 10u || p - '-' < 2u;
  }

  //determine indentation level, without incrementing pointer
  auto readDepth(const char* p) -> uint {
    uint depth = 0;
    while(p[depth] == '\t' || p[depth] == ' ') depth++;
    return depth;
  }

  //determine indentation level
  auto parseDepth(const char*& p) -> uint {
    uint depth = readDepth(p);
    p += depth;
    return depth;
  }

  //read name
  auto parseName(const char*& p) -> void {
    uint length = 0;
    while(valid(p[length])) length++;
    if(length == 0) throw "Invalid node name";
    _name = slice(p, 0, length);
    p += length;
  }

  auto parseData(const char*& p, string_view spacing) -> void {
    if(*p == '=' && *(p + 1) == '\"') {
      uint length = 2;
      while(p[length] && p[length] != '\n' && p[length] != '\"') length++;
      if(p[length] != '\"') throw "Unescaped value";
      _value = {slice(p, 2, length - 2), "\n"};
      p += length + 1;
    } else if(*p == '=') {
      uint length = 1;
      while(p[length] && p[length] != '\n' && p[length] != '\"' && p[length] != ' ') length++;
      if(p[length] == '\"') throw "Illegal character in value";
      _value = {slice(p, 1, length - 1), "\n"};
      p += length;
    } else if(*p == ':') {
      uint length = 1;
      while(p[length] && p[length] != '\n') length++;
      _value = {slice(p, 1, length - 1).trimLeft(spacing, 1L), "\n"};
      p += length;
    }
  }

  //read all attributes for a node
  auto parseAttributes(const char*& p, string_view spacing) -> void {
    while(*p && *p != '\n') {
      if(*p != ' ') throw "Invalid node name";
      while(*p == ' ') p++;  //skip excess spaces
      if(*(p + 0) == '/' && *(p + 1) == '/') break;  //skip comments

      SharedNode node(new ManagedNode);
      uint length = 0;
      while(valid(p[length])) length++;
      if(length == 0) throw "Invalid attribute name";
      node->_name = slice(p, 0, length);
      node->parseData(p += length, spacing);
      node->_value.trimRight("\n", 1L);
      _children.append(node);
    }
  }

  //read a node and all of its child nodes
  auto parseNode(const vector<string>& text, uint& y, string_view spacing) -> void {
    const char* p = text[y++];
    _metadata = parseDepth(p);
    parseName(p);
    parseData(p, spacing);
    parseAttributes(p, spacing);

    while(y < text.size()) {
      uint depth = readDepth(text[y]);
      if(depth <= _metadata) break;

      if(text[y][depth] == ':') {
        _value.append(slice(text[y++], depth + 1).trimLeft(spacing, 1L), "\n");
        continue;
      }

      SharedNode node(new ManagedNode);
      node->parseNode(text, y, spacing);
      _children.append(node);
    }

    _value.trimRight("\n", 1L);
  }

  //read top-level nodes
  auto parse(string document, string_view spacing) -> void {
    //in order to simplify the parsing logic; we do an initial pass to normalize the data
    //the below code will turn '\r\n' into '\n'; skip empty lines; and skip comment lines
    char* p = document.get(), *output = p;
    while(*p) {
      char* origin = p;
      bool empty = true;
      while(*p) {
        //scan for first non-whitespace character. if it's a line feed or comment; skip the line
        if(p[0] == ' ' || p[0] == '\t') { p++; continue; }
        empty = p[0] == '\r' || p[0] == '\n' || (p[0] == '/' && p[1] == '/');
        break;
      }
      while(*p) {
        if(p[0] == '\r') p[0] = '\n';  //turns '\r\n' into '\n\n' (second '\n' will be skipped)
        if(*p++ == '\n') break;        //include '\n' in the output to be copied
      }
      if(empty) continue;

      memory::move(output, origin, p - origin);
      output += p - origin;
    }
    document.resize(document.size() - (p - output)).trimRight("\n");
    if(document.size() == 0) return;  //empty document

    auto text = document.split("\n");
    uint y = 0;
    while(y < text.size()) {
      SharedNode node(new ManagedNode);
      node->parseNode(text, y, spacing);
      if(node->_metadata > 0) throw "Root nodes cannot be indented";
      _children.append(node);
    }
  }

  friend auto unserialize(const string&, string_view) -> Markup::Node;
};

inline auto unserialize(const string& markup, string_view spacing = {}) -> Markup::Node {
  SharedNode node(new ManagedNode);
  try {
    node->parse(markup, spacing);
  } catch(const char* error) {
    node.reset();
  }
  return (Markup::SharedNode&)node;
}

inline auto serialize(const Markup::Node& node, string_view spacing = {}, uint depth = 0) -> string {
  if(!node.name()) {
    string result;
    for(auto leaf : node) {
      result.append(serialize(leaf, spacing, depth));
    }
    return result;
  }

  string padding;
  padding.resize(depth * 2);
  padding.fill(' ');

  vector<string> lines;
  if(auto value = node.value()) lines = value.split("\n");

  string result;
  result.append(padding);
  result.append(node.name());
  if(lines.size() == 1) result.append(":", spacing, lines[0]);
  result.append("\n");
  if(lines.size() > 1) {
    padding.append("  ");
    for(auto& line : lines) {
      result.append(padding, ":", spacing, line, "\n");
    }
  }
  for(auto leaf : node) {
    result.append(serialize(leaf, spacing, depth + 1));
  }
  return result;
}

}
