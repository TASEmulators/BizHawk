#pragma once

namespace nall::Markup {

auto ManagedNode::_evaluate(string query) const -> bool {
  if(!query) return true;

  for(auto& rule : query.split(",")) {
    enum class Comparator : uint { ID, EQ, NE, LT, LE, GT, GE };
    auto comparator = Comparator::ID;
         if(rule.match("*!=*")) comparator = Comparator::NE;
    else if(rule.match("*<=*")) comparator = Comparator::LE;
    else if(rule.match("*>=*")) comparator = Comparator::GE;
    else if(rule.match ("*=*")) comparator = Comparator::EQ;
    else if(rule.match ("*<*")) comparator = Comparator::LT;
    else if(rule.match ("*>*")) comparator = Comparator::GT;

    if(comparator == Comparator::ID) {
      if(_find(rule).size()) continue;
      return false;
    }

    vector<string> side;
    switch(comparator) {
    case Comparator::EQ: side = rule.split ("=", 1L); break;
    case Comparator::NE: side = rule.split("!=", 1L); break;
    case Comparator::LT: side = rule.split ("<", 1L); break;
    case Comparator::LE: side = rule.split("<=", 1L); break;
    case Comparator::GT: side = rule.split (">", 1L); break;
    case Comparator::GE: side = rule.split(">=", 1L); break;
    }

    string data = string{_value}.strip();
    if(side(0)) {
      auto result = _find(side(0));
      if(result.size() == 0) return false;
      data = result[0].text();  //strips whitespace so rules can match without requiring it
    }

    switch(comparator) {
    case Comparator::EQ: if(data.match(side(1)) ==  true)      continue; break;
    case Comparator::NE: if(data.match(side(1)) == false)      continue; break;
    case Comparator::LT: if(data.natural()  < side(1).natural()) continue; break;
    case Comparator::LE: if(data.natural() <= side(1).natural()) continue; break;
    case Comparator::GT: if(data.natural()  > side(1).natural()) continue; break;
    case Comparator::GE: if(data.natural() >= side(1).natural()) continue; break;
    }

    return false;
  }

  return true;
}

auto ManagedNode::_find(const string& query) const -> vector<Node> {
  vector<Node> result;

  auto path = query.split("/");
  string name = path.take(0), rule;
  uint lo = 0u, hi = ~0u;

  if(name.match("*[*]")) {
    auto p = name.trimRight("]", 1L).split("[", 1L);
    name = p(0);
    if(p(1).find("-")) {
      p = p(1).split("-", 1L);
      lo = !p(0) ?  0u : p(0).natural();
      hi = !p(1) ? ~0u : p(1).natural();
    } else {
      lo = hi = p(1).natural();
    }
  }

  if(name.match("*(*)")) {
    auto p = name.trimRight(")", 1L).split("(", 1L);
    name = p(0);
    rule = p(1);
  }

  uint position = 0;
  for(auto& node : _children) {
    if(!node->_name.match(name)) continue;
    if(!node->_evaluate(rule)) continue;

    bool inrange = position >= lo && position <= hi;
    position++;
    if(!inrange) continue;

    if(path.size() == 0) {
      result.append(node);
    } else for(auto& item : node->_find(path.merge("/"))) {
      result.append(item);
    }
  }

  return result;
}

//operator[](string)
auto ManagedNode::_lookup(const string& path) const -> Node {
  auto result = _find(path);
  return result ? result[0] : Node{};

/*//faster, but cannot search
  if(auto position = path.find("/")) {
    auto name = slice(path, 0, *position);
    for(auto& node : _children) {
      if(name == node->_name) {
        return node->_lookup(slice(path, *position + 1));
      }
    }
  } else for(auto& node : _children) {
    if(path == node->_name) return node;
  }
  return {};
*/
}

auto ManagedNode::_create(const string& path) -> Node {
  if(auto position = path.find("/")) {
    auto name = slice(path, 0, *position);
    for(auto& node : _children) {
      if(name == node->_name) {
        return node->_create(slice(path, *position + 1));
      }
    }
    _children.append(new ManagedNode(name));
    return _children.right()->_create(slice(path, *position + 1));
  }
  for(auto& node : _children) {
    if(path == node->_name) return node;
  }
  _children.append(new ManagedNode(path));
  return _children.right();
}

}
