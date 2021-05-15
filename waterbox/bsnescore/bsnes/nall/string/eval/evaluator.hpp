#pragma once

namespace nall::Eval {

inline auto evaluateExpression(Node* node) -> string {
  #define p(n) evaluateExpression(node->link[n])
  switch(node->type) {
  case Node::Type::Null: return "Null";
  case Node::Type::Literal: return {"Literal:", node->literal};
  case Node::Type::Function: return {"Function(0:", p(0), ", 1:", p(1), ")"};
  case Node::Type::Subscript: return {"Subscript(0:", p(0), ", 1:", p(1), ")"};
  case Node::Type::Member: return {"Member(0:", p(0), ", 1:", p(1), ")"};
  case Node::Type::SuffixIncrement: return {"SuffixIncrement(0:", p(0), ")"};
  case Node::Type::SuffixDecrement: return {"SuffixDecrement(0:", p(0), ")"};
  case Node::Type::Reference: return {"Reference(0:", p(0), ")"};
  case Node::Type::Dereference: return {"Dereference(0:", p(0), ")"};
  case Node::Type::BitwiseNot: return {"Complement(0:", p(0), ")"};
  case Node::Type::PrefixIncrement: return {"PrefixIncrement(0:", p(0), ")"};
  case Node::Type::PrefixDecrement: return {"PrefixDecrement(0:", p(0), ")"};
  case Node::Type::Add: return {"Add(0:", p(0), ", 1:", p(1), ")"};
  case Node::Type::Multiply: return {"Multiply(0:", p(0), ", 1:", p(1), ")"};
  case Node::Type::Concatenate: return {"Concatenate(0:", p(0), ", ", p(1), ")"};
  case Node::Type::Coalesce: return {"Coalesce(0:", p(0), ", ", p(1), ")"};
  case Node::Type::Condition: return {"Condition(0:", p(0), ", ", p(1), ", ", p(2), ")"};
  case Node::Type::Assign: return {"Assign(0:", p(0), ", ", p(1), ")"};
  case Node::Type::Separator: {
    string result = "Separator(";
    for(auto& link : node->link) {
      result.append(evaluateExpression(link), ", ");
    }
    return result.trimRight(", ", 1L).append(")");
  }
  }
  #undef p

  throw "invalid operator";
}

inline auto evaluateInteger(Node* node) -> int64_t {
  if(node->type == Node::Type::Literal) return toInteger(node->literal);

  #define p(n) evaluateInteger(node->link[n])
  switch(node->type) {
  case Node::Type::SuffixIncrement: return p(0);
  case Node::Type::SuffixDecrement: return p(0);
  case Node::Type::LogicalNot: return !p(0);
  case Node::Type::BitwiseNot: return ~p(0);
  case Node::Type::Positive: return +p(0);
  case Node::Type::Negative: return -p(0);
  case Node::Type::PrefixIncrement: return p(0) + 1;
  case Node::Type::PrefixDecrement: return p(0) - 1;
  case Node::Type::Multiply: return p(0) * p(1);
  case Node::Type::Divide: return p(0) / p(1);
  case Node::Type::Modulo: return p(0) % p(1);
  case Node::Type::Add: return p(0) + p(1);
  case Node::Type::Subtract: return p(0) - p(1);
  case Node::Type::ShiftLeft: return p(0) << p(1);
  case Node::Type::ShiftRight: return p(0) >> p(1);
  case Node::Type::BitwiseAnd: return p(0) & p(1);
  case Node::Type::BitwiseOr: return p(0) | p(1);
  case Node::Type::BitwiseXor: return p(0) ^ p(1);
  case Node::Type::Equal: return p(0) == p(1);
  case Node::Type::NotEqual: return p(0) != p(1);
  case Node::Type::LessThanEqual: return p(0) <= p(1);
  case Node::Type::GreaterThanEqual: return p(0) >= p(1);
  case Node::Type::LessThan: return p(0) < p(1);
  case Node::Type::GreaterThan: return p(0) > p(1);
  case Node::Type::LogicalAnd: return p(0) && p(1);
  case Node::Type::LogicalOr: return p(0) || p(1);
  case Node::Type::Condition: return p(0) ? p(1) : p(2);
  case Node::Type::Assign: return p(1);
  case Node::Type::AssignMultiply: return p(0) * p(1);
  case Node::Type::AssignDivide: return p(0) / p(1);
  case Node::Type::AssignModulo: return p(0) % p(1);
  case Node::Type::AssignAdd: return p(0) + p(1);
  case Node::Type::AssignSubtract: return p(0) - p(1);
  case Node::Type::AssignShiftLeft: return p(0) << p(1);
  case Node::Type::AssignShiftRight: return p(0) >> p(1);
  case Node::Type::AssignBitwiseAnd: return p(0) & p(1);
  case Node::Type::AssignBitwiseOr: return p(0) | p(1);
  case Node::Type::AssignBitwiseXor: return p(0) ^ p(1);
  }
  #undef p

  throw "invalid operator";
}

inline auto integer(const string& expression) -> maybe<int64_t> {
  try {
    auto tree = new Node;
    const char* p = expression;
    parse(tree, p, 0);
    auto result = evaluateInteger(tree);
    delete tree;
    return result;
  } catch(const char*) {
    return nothing;
  }
}

inline auto evaluateReal(Node* node) -> long double {
  if(node->type == Node::Type::Literal) return toReal(node->literal);

  #define p(n) evaluateReal(node->link[n])
  switch(node->type) {
  case Node::Type::LogicalNot: return !p(0);
  case Node::Type::Positive: return +p(0);
  case Node::Type::Negative: return -p(0);
  case Node::Type::Multiply: return p(0) * p(1);
  case Node::Type::Divide: return p(0) / p(1);
  case Node::Type::Add: return p(0) + p(1);
  case Node::Type::Subtract: return p(0) - p(1);
  case Node::Type::Equal: return p(0) == p(1);
  case Node::Type::NotEqual: return p(0) != p(1);
  case Node::Type::LessThanEqual: return p(0) <= p(1);
  case Node::Type::GreaterThanEqual: return p(0) >= p(1);
  case Node::Type::LessThan: return p(0) < p(1);
  case Node::Type::GreaterThan: return p(0) > p(1);
  case Node::Type::LogicalAnd: return p(0) && p(1);
  case Node::Type::LogicalOr: return p(0) || p(1);
  case Node::Type::Condition: return p(0) ? p(1) : p(2);
  case Node::Type::Assign: return p(1);
  case Node::Type::AssignMultiply: return p(0) * p(1);
  case Node::Type::AssignDivide: return p(0) / p(1);
  case Node::Type::AssignAdd: return p(0) + p(1);
  case Node::Type::AssignSubtract: return p(0) - p(1);
  }
  #undef p

  throw "invalid operator";
}

inline auto real(const string& expression) -> maybe<long double> {
  try {
    auto tree = new Node;
    const char* p = expression;
    parse(tree, p, 0);
    auto result = evaluateReal(tree);
    delete tree;
    return result;
  } catch(const char*) {
    return nothing;
  }
}

}
