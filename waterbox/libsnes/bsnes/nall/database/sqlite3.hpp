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

    auto columns() -> uint {
      return sqlite3_column_count(statement());
    }

    auto boolean(uint column) -> bool {
      return sqlite3_column_int64(statement(), column) != 0;
    }

    auto integer(uint column) -> int64_t {
      return sqlite3_column_int64(statement(), column);
    }

    auto natural(uint column) -> uint64_t {
      return sqlite3_column_int64(statement(), column);
    }

    auto real(uint column) -> double {
      return sqlite3_column_double(statement(), column);
    }

    auto string(uint column) -> nall::string {
      nall::string result;
      if(auto text = sqlite3_column_text(statement(), column)) {
        result.resize(sqlite3_column_bytes(statement(), column));
        memory::copy(result.get(), text, result.size());
      }
      return result;
    }

    auto data(uint column) -> vector<uint8_t> {
      vector<uint8_t> result;
      if(auto data = sqlite3_column_blob(statement(), column)) {
        result.resize(sqlite3_column_bytes(statement(), column));
        memory::copy(result.data(), data, result.size());
      }
      return result;
    }

    auto boolean() -> bool { return boolean(_output++); }
    auto integer() -> int64_t { return integer(_output++); }
    auto natural() -> uint64_t { return natural(_output++); }
    auto real() -> double { return real(_output++); }
    auto string() -> nall::string { return string(_output++); }
    auto data() -> vector<uint8_t> { return data(_output++); }

  protected:
    virtual auto statement() -> sqlite3_stmt* { return _statement; }

    sqlite3_stmt* _statement = nullptr;
    int _response = SQLITE_OK;
    uint _output = 0;
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

    auto& bind(uint column, nullptr_t) { sqlite3_bind_null(_statement, 1 + column); return *this; }
    auto& bind(uint column, bool value) { sqlite3_bind_int(_statement, 1 + column, value); return *this; }
    auto& bind(uint column, int32_t value) { sqlite3_bind_int(_statement, 1 + column, value); return *this; }
    auto& bind(uint column, uint32_t value) { sqlite3_bind_int(_statement, 1 + column, value); return *this; }
    auto& bind(uint column, int64_t value) { sqlite3_bind_int64(_statement, 1 + column, value); return *this; }
    auto& bind(uint column, uint64_t value) { sqlite3_bind_int64(_statement, 1 + column, value); return *this; }
    auto& bind(uint column, intmax value) { sqlite3_bind_int64(_statement, 1 + column, value); return *this; }
    auto& bind(uint column, uintmax value) { sqlite3_bind_int64(_statement, 1 + column, value); return *this; }
    auto& bind(uint column, nall::boolean value) { sqlite3_bind_int64(_statement, 1 + column, value); return *this; }
    auto& bind(uint column, nall::integer value) { sqlite3_bind_int64(_statement, 1 + column, value); return *this; }
    auto& bind(uint column, nall::natural value) { sqlite3_bind_int64(_statement, 1 + column, value); return *this; }
    auto& bind(uint column, double value) { sqlite3_bind_double(_statement, 1 + column, value); return *this; }
    auto& bind(uint column, const nall::string& value) { sqlite3_bind_text(_statement, 1 + column, value.data(), value.size(), SQLITE_TRANSIENT); return *this; }
    auto& bind(uint column, const vector<uint8_t>& value) { sqlite3_bind_blob(_statement, 1 + column, value.data(), value.size(), SQLITE_TRANSIENT); return *this; }

    auto& bind(nullptr_t) { return bind(_input++, nullptr); }
    auto& bind(bool value) { return bind(_input++, value); }
    auto& bind(int32_t value) { return bind(_input++, value); }
    auto& bind(uint32_t value) { return bind(_input++, value); }
    auto& bind(int64_t value) { return bind(_input++, value); }
    auto& bind(uint64_t value) { return bind(_input++, value); }
    auto& bind(intmax value) { return bind(_input++, value); }
    auto& bind(uintmax value) { return bind(_input++, value); }
    auto& bind(nall::boolean value) { return bind(_input++, value); }
    auto& bind(nall::integer value) { return bind(_input++, value); }
    auto& bind(nall::natural value) { return bind(_input++, value); }
    auto& bind(double value) { return bind(_input++, value); }
    auto& bind(const nall::string& value) { return bind(_input++, value); }
    auto& bind(const vector<uint8_t>& value) { return bind(_input++, value); }

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

    uint _input = 0;
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

  auto lastInsertID() const -> uint64_t {
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
