#pragma once

//SQLite3 C++ RAII wrapper for nall
//note: it is safe (no-op) to call sqlite3_* functions on null sqlite3 objects

#include <sqlite3.h>

#include <nall/stdint.hpp>
#include <nall/string.hpp>

namespace nall::Database {

struct SQLite3 {
  struct Statement {
    Statement(const Statement& source) = delete;
    auto operator=(const Statement& source) -> Statement& = delete;

    Statement(sqlite3_stmt* statement) : _statement(statement) {}
    Statement(Statement&& source) { operator=(move(source)); }

    auto operator=(Statement&& source) -> Statement& {
      _statement = source._statement;
      _response = source._response;
      _output = source._output;
      source._statement = nullptr;
      source._response = SQLITE_OK;
      source._output = 0;
      return *this;
    }

    explicit operator bool() {
      return sqlite3_data_count(statement());
    }

    auto columns() -> u32 {
      return sqlite3_column_count(statement());
    }

    auto boolean(u32 column) -> bool {
      return sqlite3_column_int64(statement(), column) != 0;
    }

    auto integer(u32 column) -> s64 {
      return sqlite3_column_int64(statement(), column);
    }

    auto natural(u32 column) -> u64 {
      return sqlite3_column_int64(statement(), column);
    }

    auto real(u32 column) -> f64 {
      return sqlite3_column_double(statement(), column);
    }

    auto string(u32 column) -> nall::string {
      nall::string result;
      if(auto text = sqlite3_column_text(statement(), column)) {
        result.resize(sqlite3_column_bytes(statement(), column));
        memory::copy(result.get(), text, result.size());
      }
      return result;
    }

    auto data(u32 column) -> vector<u8> {
      vector<u8> result;
      if(auto data = sqlite3_column_blob(statement(), column)) {
        result.resize(sqlite3_column_bytes(statement(), column));
        memory::copy(result.data(), data, result.size());
      }
      return result;
    }

    auto boolean() -> bool { return boolean(_output++); }
    auto integer() -> s64 { return integer(_output++); }
    auto natural() -> u64 { return natural(_output++); }
    auto real() -> f64 { return real(_output++); }
    auto string() -> nall::string { return string(_output++); }
    auto data() -> vector<u8> { return data(_output++); }

  protected:
    virtual auto statement() -> sqlite3_stmt* { return _statement; }

    sqlite3_stmt* _statement = nullptr;
    s32 _response = SQLITE_OK;
    u32 _output = 0;
  };

  struct Query : Statement {
    Query(const Query& source) = delete;
    auto operator=(const Query& source) -> Query& = delete;

    Query(sqlite3_stmt* statement) : Statement(statement) {}
    Query(Query&& source) : Statement(source._statement) { operator=(move(source)); }

    ~Query() {
      sqlite3_finalize(statement());
      _statement = nullptr;
    }

    auto operator=(Query&& source) -> Query& {
      _statement = source._statement;
      _input = source._input;
      source._statement = nullptr;
      source._input = 0;
      return *this;
    }

    auto& bind(u32 column, nullptr_t) { sqlite3_bind_null(_statement, 1 + column); return *this; }
    auto& bind(u32 column, bool value) { sqlite3_bind_int(_statement, 1 + column, value); return *this; }
    auto& bind(u32 column, s32 value) { sqlite3_bind_int(_statement, 1 + column, value); return *this; }
    auto& bind(u32 column, u32 value) { sqlite3_bind_int(_statement, 1 + column, value); return *this; }
    auto& bind(u32 column, s64 value) { sqlite3_bind_int64(_statement, 1 + column, value); return *this; }
    auto& bind(u32 column, u64 value) { sqlite3_bind_int64(_statement, 1 + column, value); return *this; }
    auto& bind(u32 column, intmax value) { sqlite3_bind_int64(_statement, 1 + column, value); return *this; }
    auto& bind(u32 column, uintmax value) { sqlite3_bind_int64(_statement, 1 + column, value); return *this; }
    auto& bind(u32 column, nall::boolean value) { sqlite3_bind_int64(_statement, 1 + column, value); return *this; }
    auto& bind(u32 column, nall::integer value) { sqlite3_bind_int64(_statement, 1 + column, value); return *this; }
    auto& bind(u32 column, nall::natural value) { sqlite3_bind_int64(_statement, 1 + column, value); return *this; }
    auto& bind(u32 column, f64 value) { sqlite3_bind_double(_statement, 1 + column, value); return *this; }
    auto& bind(u32 column, const nall::string& value) { sqlite3_bind_text(_statement, 1 + column, value.data(), value.size(), SQLITE_TRANSIENT); return *this; }
    auto& bind(u32 column, const vector<u8>& value) { sqlite3_bind_blob(_statement, 1 + column, value.data(), value.size(), SQLITE_TRANSIENT); return *this; }

    auto& bind(nullptr_t) { return bind(_input++, nullptr); }
    auto& bind(bool value) { return bind(_input++, value); }
    auto& bind(s32 value) { return bind(_input++, value); }
    auto& bind(u32 value) { return bind(_input++, value); }
    auto& bind(s64 value) { return bind(_input++, value); }
    auto& bind(u64 value) { return bind(_input++, value); }
    auto& bind(intmax value) { return bind(_input++, value); }
    auto& bind(uintmax value) { return bind(_input++, value); }
    auto& bind(nall::boolean value) { return bind(_input++, value); }
    auto& bind(nall::integer value) { return bind(_input++, value); }
    auto& bind(nall::natural value) { return bind(_input++, value); }
    auto& bind(f64 value) { return bind(_input++, value); }
    auto& bind(const nall::string& value) { return bind(_input++, value); }
    auto& bind(const vector<u8>& value) { return bind(_input++, value); }

    auto step() -> bool {
      _stepped = true;
      return sqlite3_step(_statement) == SQLITE_ROW;
    }

    struct Iterator {
      Iterator(Query& query, bool finished) : query(query), finished(finished) {}
      auto operator*() -> Statement { return query._statement; }
      auto operator!=(const Iterator& source) const -> bool { return finished != source.finished; }
      auto operator++() -> Iterator& { finished = !query.step(); return *this; }

    protected:
      Query& query;
      bool finished = false;
    };

    auto begin() -> Iterator { return Iterator(*this, !step()); }
    auto end() -> Iterator { return Iterator(*this, true); }

  private:
    auto statement() -> sqlite3_stmt* override {
      if(!_stepped) step();
      return _statement;
    }

    u32 _input = 0;
    bool _stepped = false;
  };

  SQLite3() = default;
  SQLite3(const string& filename) { open(filename); }
  ~SQLite3() { close(); }

  explicit operator bool() const { return _database; }

  auto open(const string& filename) -> bool {
    close();
    sqlite3_open(filename, &_database);
    return _database;
  }

  auto close() -> void {
    sqlite3_close(_database);
    _database = nullptr;
  }

  template<typename... P> auto execute(const string& statement, P&&... p) -> Query {
    if(!_database) return {nullptr};

    sqlite3_stmt* _statement = nullptr;
    sqlite3_prepare_v2(_database, statement.data(), statement.size(), &_statement, nullptr);
    if(!_statement) {
      if(_debug) print("[sqlite3_prepare_v2] ", sqlite3_errmsg(_database), "\n");
      return {nullptr};
    }

    Query query{_statement};
    bind(query, forward<P>(p)...);
    return query;
  }

  auto lastInsertID() const -> u64 {
    return _database ? sqlite3_last_insert_rowid(_database) : 0;
  }

  auto setDebug(bool debug = true) -> void {
    _debug = debug;
  }

protected:
  auto bind(Query&) -> void {}
  template<typename T, typename... P> auto bind(Query& query, const T& value, P&&... p) -> void {
    query.bind(value);
    bind(query, forward<P>(p)...);
  }

  bool _debug = false;
  sqlite3* _database = nullptr;
};

}
