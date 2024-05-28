#pragma once

namespace nall::Eval {

inline auto whitespace(char n) -> bool {
  return n == ' ' || n == '\t' || n == '\r' || n == '\n';
}

//operator associativity and precedence:
//  right:  a[] a() a.b a++ a--
//   left:  &a *a !a ~a +a -a ++a --a
//   left:  a*b a/b a%b
//   left:  a+b a-b
//   left:  a<<<b a>>>b a<<b a>>b
//   left:  a&b
//   left:  a^b
//   left:  a|b
//   left:  a~b
//   left:  a<=b a>=b a<b a>b
//   left:  a==b a!=b
//   left:  a&&b
//   left:  a||b
//  right:  a??b a?b:c
//  right:  a=b a:=b a*=b a/=b a%=b a+=b a-=b a<<<=b a>>>=b a<<=b a>>=b a&=b a^=b a|=b a~=b
//   left:  a,b
//differences from C++17:
//  a&b, a^b, a|b precedence was elevated above comparison operators
//  a?b:c precedence was elevated above assignment operators
//  a<<<b a>>>b a<<<=b a>>>=b rotation operators were added
//  a~b a~=b concatenation operators were added
//  a??b coalesce operator was added
inline auto parse(Node*& node, const char*& s, u32 depth) -> void {
  auto unaryPrefix = [&](Node::Type type, u32 seek, u32 depth) {
    auto parent = new Node(type);
    parse(parent->link(0) = new Node, s += seek, depth);
    node = parent;
  };

  auto unarySuffix = [&](Node::Type type, u32 seek, u32 depth) {
    auto parent = new Node(type);
    parent->link(0) = node;
    parse(parent, s += seek, depth);
    node = parent;
  };

  auto binary = [&](Node::Type type, u32 seek, u32 depth) {
    auto parent = new Node(type);
    parent->link(0) = node;
    parse(parent->link(1) = new Node, s += seek, depth);
    node = parent;
  };

  auto ternary = [&](Node::Type type, u32 seek, u32 depth) {
    auto parent = new Node(type);
    parent->link(0) = node;
    parse(parent->link(1) = new Node, s += seek, depth);
    if(s[0] != ':') throw "mismatched ternary";
    parse(parent->link(2) = new Node, s += seek, depth);
    node = parent;
  };

  auto separator = [&](Node::Type type, u32 seek, u32 depth) {
    if(node->type != Node::Type::Separator) return binary(type, seek, depth);
    u32 n = node->link.size();
    parse(node->link(n) = new Node, s += seek, depth);
  };

  while(whitespace(s[0])) s++;
  if(!s[0]) return;

  if(s[0] == '(' && !node->link) {
    parse(node, s += 1, 1);
    if(*s++ != ')') throw "mismatched group";
  }

  if(isLiteral(s)) {
    node->type = Node::Type::Literal;
    node->literal = literal(s);
  }

  #define p() (!node->literal && !node->link)
  while(true) {
    while(whitespace(s[0])) s++;
    if(!s[0]) return;

    if(depth >= 17) break;  //left associativity
    if(s[0] == '(' && !p()) {
      binary(Node::Type::Function, 1, 1);
      if(*s++ != ')') throw "mismatched function";
      continue;
    }
    if(s[0] == '[') {
      binary(Node::Type::Subscript, 1, 1);
      if(*s++ != ']') throw "mismatched subscript";
      continue;
    }
    if(s[0] == '.') { binary(Node::Type::Member, 1, 17); continue; }
    if(s[0] == '+' && s[1] == '+' && !p()) { unarySuffix(Node::Type::SuffixIncrement, 2, 17); continue; }
    if(s[0] == '-' && s[1] == '-' && !p()) { unarySuffix(Node::Type::SuffixDecrement, 2, 17); continue; }

    if(s[0] == '&' && p()) { unaryPrefix(Node::Type::Reference, 1, 16); continue; }
    if(s[0] == '*' && p()) { unaryPrefix(Node::Type::Dereference, 1, 16); continue; }
    if(s[0] == '!' && p()) { unaryPrefix(Node::Type::LogicalNot, 1, 16); continue; }
    if(s[0] == '~' && p()) { unaryPrefix(Node::Type::BitwiseNot, 1, 16); continue; }
    if(s[0] == '+' && s[1] != '+' && p()) { unaryPrefix(Node::Type::Positive, 1, 16); continue; }
    if(s[0] == '-' && s[1] != '-' && p()) { unaryPrefix(Node::Type::Negative, 1, 16); continue; }
    if(s[0] == '+' && s[1] == '+' && p()) { unaryPrefix(Node::Type::PrefixIncrement, 2, 16); continue; }
    if(s[0] == '-' && s[1] == '-' && p()) { unaryPrefix(Node::Type::PrefixDecrement, 2, 16); continue; }
    if(depth >= 16) break;  //right associativity

    if(depth >= 15) break;  //left associativity
    if(s[0] == '*' && s[1] != '=') { binary(Node::Type::Multiply, 1, 15); continue; }
    if(s[0] == '/' && s[1] != '=') { binary(Node::Type::Divide, 1, 15); continue; }
    if(s[0] == '%' && s[1] != '=') { binary(Node::Type::Modulo, 1, 15); continue; }

    if(depth >= 14) break;  //left associativity
    if(s[0] == '+' && s[1] != '=') { binary(Node::Type::Add, 1, 14); continue; }
    if(s[0] == '-' && s[1] != '=') { binary(Node::Type::Subtract, 1, 14); continue; }

    if(depth >= 13) break;  //left associativity
    if(s[0] == '<' && s[1] == '<' && s[2] == '<' && s[3] != '=') { binary(Node::Type::RotateLeft, 3, 13); continue; }
    if(s[0] == '>' && s[1] == '>' && s[2] == '>' && s[3] != '=') { binary(Node::Type::RotateRight, 3, 13); continue; }
    if(s[0] == '<' && s[1] == '<' && s[2] != '=') { binary(Node::Type::ShiftLeft, 2, 13); continue; }
    if(s[0] == '>' && s[1] == '>' && s[2] != '=') { binary(Node::Type::ShiftRight, 2, 13); continue; }

    if(depth >= 12) break;  //left associativity
    if(s[0] == '&' && s[1] != '&' && s[1] != '=') { binary(Node::Type::BitwiseAnd, 1, 12); continue; }

    if(depth >= 11) break;  //left associativity
    if(s[0] == '^' && s[1] != '^' && s[1] != '=') { binary(Node::Type::BitwiseXor, 1, 11); continue; }

    if(depth >= 10) break;  //left associativity
    if(s[0] == '|' && s[1] != '|' && s[1] != '=') { binary(Node::Type::BitwiseOr, 1, 10); continue; }

    if(depth >= 9) break;  //left associativity
    if(s[0] == '~' && s[1] != '=') { binary(Node::Type::Concatenate, 1, 9); continue; }

    if(depth >= 8) break;  //left associativity
    if(s[0] == '<' && s[1] == '=') { binary(Node::Type::LessThanEqual, 2, 8); continue; }
    if(s[0] == '>' && s[1] == '=') { binary(Node::Type::GreaterThanEqual, 2, 8); continue; }
    if(s[0] == '<') { binary(Node::Type::LessThan, 1, 8); continue; }
    if(s[0] == '>') { binary(Node::Type::GreaterThan, 1, 8); continue; }

    if(depth >= 7) break;  //left associativity
    if(s[0] == '=' && s[1] == '=') { binary(Node::Type::Equal, 2, 7); continue; }
    if(s[0] == '!' && s[1] == '=') { binary(Node::Type::NotEqual, 2, 7); continue; }

    if(depth >= 6) break;  //left associativity
    if(s[0] == '&' && s[1] == '&') { binary(Node::Type::LogicalAnd, 2, 6); continue; }

    if(depth >= 5) break;  //left associativity
    if(s[0] == '|' && s[1] == '|') { binary(Node::Type::LogicalOr, 2, 5); continue; }

    if(s[0] == '?' && s[1] == '?') { binary(Node::Type::Coalesce, 2, 4); continue; }
    if(s[0] == '?' && s[1] != '?') { ternary(Node::Type::Condition, 1, 4); continue; }
    if(depth >= 4) break;  //right associativity

    if(s[0] == '=') { binary(Node::Type::Assign, 1, 3); continue; }
    if(s[0] == ':' && s[1] == '=') { binary(Node::Type::Create, 2, 3); continue; }
    if(s[0] == '*' && s[1] == '=') { binary(Node::Type::AssignMultiply, 2, 3); continue; }
    if(s[0] == '/' && s[1] == '=') { binary(Node::Type::AssignDivide, 2, 3); continue; }
    if(s[0] == '%' && s[1] == '=') { binary(Node::Type::AssignModulo, 2, 3); continue; }
    if(s[0] == '+' && s[1] == '=') { binary(Node::Type::AssignAdd, 2, 3); continue; }
    if(s[0] == '-' && s[1] == '=') { binary(Node::Type::AssignSubtract, 2, 3); continue; }
    if(s[0] == '<' && s[1] == '<' && s[2] == '<' && s[3] == '=') { binary(Node::Type::AssignRotateLeft, 4, 3); continue; }
    if(s[0] == '>' && s[1] == '>' && s[2] == '>' && s[3] == '=') { binary(Node::Type::AssignRotateRight, 4, 3); continue; }
    if(s[0] == '<' && s[1] == '<' && s[2] == '=') { binary(Node::Type::AssignShiftLeft, 3, 3); continue; }
    if(s[0] == '>' && s[1] == '>' && s[2] == '=') { binary(Node::Type::AssignShiftRight, 3, 3); continue; }
    if(s[0] == '&' && s[1] == '=') { binary(Node::Type::AssignBitwiseAnd, 2, 3); continue; }
    if(s[0] == '^' && s[1] == '=') { binary(Node::Type::AssignBitwiseXor, 2, 3); continue; }
    if(s[0] == '|' && s[1] == '=') { binary(Node::Type::AssignBitwiseOr, 2, 3); continue; }
    if(s[0] == '~' && s[1] == '=') { binary(Node::Type::AssignConcatenate, 2, 3); continue; }
    if(depth >= 3) break;  //right associativity

    if(depth >= 2) break;  //left associativity
    if(s[0] == ',') { separator(Node::Type::Separator, 1, 2); continue; }

    if(depth >= 1 && (s[0] == ')' || s[0] == ']')) break;

    while(whitespace(s[0])) s++;
    if(!s[0]) break;

    throw "unrecognized terminal";
  }
  #undef p
}

inline auto parse(const string& expression) -> Node* {
  auto result = new Node;
  const char* p = expression;
  parse(result, p, 0);
  return result;
}

}
