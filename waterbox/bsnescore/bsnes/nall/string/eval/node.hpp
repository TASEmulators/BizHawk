#pragma once

namespace nall::Eval {

struct Node {
  enum class Type : uint {
    Null,
    Literal,
    Function, Subscript, Member, SuffixIncrement, SuffixDecrement,
    Reference, Dereference, LogicalNot, BitwiseNot, Positive, Negative, PrefixIncrement, PrefixDecrement,
    Multiply, Divide, Modulo,
    Add, Subtract,
    RotateLeft, RotateRight, ShiftLeft, ShiftRight,
    BitwiseAnd, BitwiseOr, BitwiseXor,
    Concatenate,
    Equal, NotEqual, LessThanEqual, GreaterThanEqual, LessThan, GreaterThan,
    LogicalAnd, LogicalOr,
    Coalesce, Condition,
    Assign, Create,  //all assignment operators have the same precedence
      AssignMultiply, AssignDivide, AssignModulo,
      AssignAdd, AssignSubtract,
      AssignRotateLeft, AssignRotateRight, AssignShiftLeft, AssignShiftRight,
      AssignBitwiseAnd, AssignBitwiseOr, AssignBitwiseXor,
      AssignConcatenate,
    Separator,
  };

  Type type;
  string literal;
  vector<Node*> link;

  Node() : type(Type::Null) {}
  Node(Type type) : type(type) {}
  ~Node() { for(auto& node : link) delete node; }
};

}
