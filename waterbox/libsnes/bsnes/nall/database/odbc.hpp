#pragma once

//legacy code; no longer used

#include <nall/string.hpp>

#include <sql.h>
#include <sqltypes.h>
#include <sqlext.h>

namespace nall::Database {

struct ODBC {
  struct Statement {
    Statement(const Statement& source) = delete;
    auto operator=(const Statement& source) -> Statement& = delete;

    Statement(SQLHANDLE statement) : _statement(statement) {}
    Statement(Statement&& source) { operator=(move(source)); }

    auto operator=(Statement&& source) -> Statement& {
      _statement = source._statement;
      _output = source._output;
      _values = move(source._values);
      source._statement = nullptr;
      source._output = 0;
      return *this;
    }

    auto columns() -> unsigned {
      SQLSMALLINT columns = 0;
      if(statement()) SQLNumResultCols(statement(), &columns);
      return columns;
    }

    auto integer(unsigned column) -> int64_t {
      if(auto value = _values(column)) return value.get<int64_t>(0);
      int64_t value = 0;
      SQLGetData(statement(), 1 + column, SQL_C_SBIGINT, &value, 0, nullptr);
      _values(column) = (int64_t)value;
      return value;
    }

    auto natural(unsigned column) -> uint64_t {
      if(auto value = _values(column)) return value.get<uint64_t>(0);
      uint64_t value = 0;
      SQLGetData(statement(), 1 + column, SQL_C_UBIGINT, &value, 0, nullptr);
      _values(column) = (uint64_t)value;
      return value;
    }

    auto real(unsigned column) -> double {
      if(auto value = _values(column)) return value.get<double>(0.0);
      double value = 0.0;
      SQLGetData(statement(), 1 + column, SQL_C_DOUBLE, &value, 0, nullptr);
      _values(column) = (double)value;
      return value;
    }

    auto text(unsigned column) -> string {
      if(auto value = _values(column)) return value.get<string>({});
      string value;
      value.resize(65535);
      SQLLEN size = 0;
      SQLGetData(statement(), 1 + column, SQL_C_CHAR, value.get(), value.size(), &size);
      value.resize(size);
      _values(column) = (string)value;
      return value;
    }

    auto data(unsigned column) -> vector<uint8_t> {
      if(auto value = _values(column)) return value.get<vector<uint8_t>>({});
      vector<uint8_t> value;
      value.resize(65535);
      SQLLEN size = 0;
      SQLGetData(statement(), 1 + column, SQL_C_CHAR, value.data(), value.size(), &size);
      value.resize(size);
      _values(column) = (vector<uint8_t>)value;
      return value;
    }

    auto integer() -> int64_t { return integer(_output++); }
    auto natural() -> uint64_t { return natural(_output++); }
    auto real() -> double { return real(_output++); }
    auto text() -> string { return text(_output++); }
    auto data() -> vector<uint8_t> { return data(_output++); }

  protected:
    virtual auto statement() -> SQLHANDLE { return _statement; }

    SQLHANDLE _statement = nullptr;
    unsigned _output = 0;
    vector<any> _values;  //some ODBC drivers (eg MS-SQL) do not allow the same column to be read more than once
  };

  struct Query : Statement {
    Query(const Query& source) = delete;
    auto operator=(const Query& source) -> Query& = delete;

    Query(SQLHANDLE statement) : Statement(statement) {}
    Query(Query&& source) : Statement(source._statement) { operator=(move(source)); }

    ~Query() {
      if(statement()) {
        SQLFreeHandle(SQL_HANDLE_STMT, _statement);
        _statement = nullptr;
      }
    }

    auto operator=(Query&& source) -> Query& {
      Statement::operator=(move(source));
      _bindings = move(source._bindings);
      _result = source._result;
      _input = source._input;
      _stepped = source._stepped;
      source._result = SQL_SUCCESS;
      source._input = 0;
      source._stepped = false;
      return *this;
    }

    explicit operator bool() {
      //this is likely not the best way to test if the query has returned data ...
      //but I wasn't able to find an ODBC API for this seemingly simple task
      return statement() && success();
    }

    //ODBC SQLBindParameter only holds pointers to data values
    //if the bound paramters go out of scope before the query is executed, binding would reference dangling pointers
    //so to work around this, we cache all parameters inside Query until the query is executed

    auto& bind(unsigned column, nullptr_t) { return _bindings.append({column, any{(nullptr_t)nullptr}}), *this; }
    auto& bind(unsigned column, int32_t value) { return _bindings.append({column, any{(int32_t)value}}), *this; }
    auto& bind(unsigned column, uint32_t value) { return _bindings.append({column, any{(uint32_t)value}}), *this; }
    auto& bind(unsigned column, int64_t value) { return _bindings.append({column, any{(int64_t)value}}), *this; }
    auto& bind(unsigned column, uint64_t value) { return _bindings.append({column, any{(uint64_t)value}}), *this; }
    auto& bind(unsigned column, double value) { return _bindings.append({column, any{(double)value}}), *this; }
    auto& bind(unsigned column, const string& value) { return _bindings.append({column, any{(string)value}}), *this; }
    auto& bind(unsigned column, const vector<uint8_t>& value) { return _bindings.append({column, any{(vector<uint8_t>)value}}), *this; }

    auto& bind(nullptr_t) { return bind(_input++, nullptr); }
    auto& bind(int32_t value) { return bind(_input++, value); }
    auto& bind(uint32_t value) { return bind(_input++, value); }
    auto& bind(int64_t value) { return bind(_input++, value); }
    auto& bind(uint64_t value) { return bind(_input++, value); }
    auto& bind(double value) { return bind(_input++, value); }
    auto& bind(const string& value) { return bind(_input++, value); }
    auto& bind(const vector<uint8_t>& value) { return bind(_input++, value); }

    auto step() -> bool {
      if(!_stepped) {
        for(auto& binding : _bindings) {
          if(binding.value.is<nullptr_t>()) {
            SQLLEN length = SQL_NULL_DATA;
            SQLBindParameter(_statement, 1 + binding.column, SQL_PARAM_INPUT, SQL_C_NUMERIC, SQL_NUMERIC, 0, 0, nullptr, 0, &length);
          } else if(binding.value.is<int32_t>()) {
            SQLBindParameter(_statement, 1 + binding.column, SQL_PARAM_INPUT, SQL_C_SLONG, SQL_INTEGER, 0, 0, &binding.value.get<int32_t>(), 0, nullptr);
          } else if(binding.value.is<uint32_t>()) {
            SQLBindParameter(_statement, 1 + binding.column, SQL_PARAM_INPUT, SQL_C_ULONG, SQL_INTEGER, 0, 0, &binding.value.get<uint32_t>(), 0, nullptr);
          } else if(binding.value.is<int64_t>()) {
            SQLBindParameter(_statement, 1 + binding.column, SQL_PARAM_INPUT, SQL_C_SBIGINT, SQL_INTEGER, 0, 0, &binding.value.get<int64_t>(), 0, nullptr);
          } else if(binding.value.is<uint64_t>()) {
            SQLBindParameter(_statement, 1 + binding.column, SQL_PARAM_INPUT, SQL_C_UBIGINT, SQL_INTEGER, 0, 0, &binding.value.get<uint64_t>(), 0, nullptr);
          } else if(binding.value.is<double>()) {
            SQLBindParameter(_statement, 1 + binding.column, SQL_PARAM_INPUT, SQL_C_DOUBLE, SQL_DOUBLE, 0, 0, &binding.value.get<double>(), 0, nullptr);
          } else if(binding.value.is<string>()) {
            auto& value = binding.value.get<string>();
            SQLLEN length = SQL_NTS;
            SQLBindParameter(_statement, 1 + binding.column, SQL_PARAM_INPUT, SQL_C_CHAR, SQL_VARCHAR, value.size(), 0, (SQLPOINTER)value.data(), 0, &length);
          } else if(binding.value.is<vector<uint8_t>>()) {
            auto& value = binding.value.get<vector<uint8_t>>();
            SQLLEN length = value.size();
            SQLBindParameter(_statement, 1 + binding.column, SQL_PARAM_INPUT, SQL_C_CHAR, SQL_VARBINARY, value.size(), 0, (SQLPOINTER)value.data(), 0, &length);
          }
        }

        _stepped = true;
        _result = SQLExecute(_statement);
        if(!success()) return false;
      }

      _values.reset();  //clear previous row's cached read results
      _result = SQLFetch(_statement);
      _output = 0;
      return success();
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
    auto success() const -> bool {
      return _result == SQL_SUCCESS || _result == SQL_SUCCESS_WITH_INFO;
    }

    auto statement() -> SQLHANDLE override {
      if(!_stepped) step();
      return _statement;
    }

    struct Binding {
      unsigned column;
      any value;
    };
    vector<Binding> _bindings;

    SQLRETURN _result = SQL_SUCCESS;
    unsigned _input = 0;
    bool _stepped = false;
  };

  ODBC() {
    _result = SQLAllocHandle(SQL_HANDLE_ENV, SQL_NULL_HANDLE, &_environment);
    if(!success()) return;

    SQLSetEnvAttr(_environment, SQL_ATTR_ODBC_VERSION, (void*)SQL_OV_ODBC3, 0);
  }

  ODBC(const string& database, const string& username, const string& password) : ODBC() {
    open(database, username, password);
  }

  ~ODBC() {
    if(_environment) {
      close();
      SQLFreeHandle(SQL_HANDLE_ENV, _environment);
      _environment = nullptr;
    }
  }

  explicit operator bool() const { return _connection; }

  auto open(const string& database, const string& username, const string& password) -> bool {
    if(!_environment) return false;
    close();

    _result = SQLAllocHandle(SQL_HANDLE_DBC, _environment, &_connection);
    if(!success()) return false;

    SQLSetConnectAttr(_connection, SQL_LOGIN_TIMEOUT, (SQLPOINTER)5, 0);
    _result = SQLConnectA(_connection,
      (SQLCHAR*)database.data(), SQL_NTS,
      (SQLCHAR*)username.data(), SQL_NTS,
      (SQLCHAR*)password.data(), SQL_NTS
    );
    if(!success()) return close(), false;

    return true;
  }

  auto close() -> void {
    if(_connection) {
      SQLDisconnect(_connection);
      SQLFreeHandle(SQL_HANDLE_DBC, _connection);
      _connection = nullptr;
    }
  }

  template<typename... P> auto execute(const string& statement, P&&... p) -> Query {
    if(!_connection) return {nullptr};

    SQLHANDLE _statement = nullptr;
    _result = SQLAllocHandle(SQL_HANDLE_STMT, _connection, &_statement);
    if(!success()) return {nullptr};

    Query query{_statement};
    _result = SQLPrepareA(_statement, (SQLCHAR*)statement.data(), SQL_NTS);
    if(!success()) return {nullptr};

    bind(query, forward<P>(p)...);
    return query;
  }

private:
  auto success() const -> bool { return _result == SQL_SUCCESS || _result == SQL_SUCCESS_WITH_INFO; }

  auto bind(Query&) -> void {}
  template<typename T, typename... P> auto bind(Query& query, const T& value, P&&... p) -> void {
    query.bind(value);
    bind(query, forward<P>(p)...);
  }

  SQLHANDLE _environment = nullptr;
  SQLHANDLE _connection = nullptr;
  SQLRETURN _result = SQL_SUCCESS;
};

}
