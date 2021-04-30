#pragma once
#warning "these defines break if statements with multiple parameters to templates"

#define if1(statement) if(statement)
#define if2(condition, false) ([&](auto&& value) -> decltype(condition) { \
  return (bool)value ? value : (decltype(condition))false; \
})(condition)
#define if3(condition, true, false) ((condition) ? (true) : (decltype(true))(false))
#define if4(type, condition, true, false) ((condition) ? (type)(true) : (type)(false))
#define if_(_1, _2, _3, _4, name, ...) name
#define if(...) if_(__VA_ARGS__, if4, if3, if2, if1)(__VA_ARGS__)
