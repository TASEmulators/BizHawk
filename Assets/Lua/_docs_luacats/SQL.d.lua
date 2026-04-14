-- Lua functions available in EmuHawk 2.11
-- https://tasvideos.org/Bizhawk

error("This is a definition file for Lua Language Server and not a usable script")

---@meta _

---A library for performing SQLite operations.
---@class SQL
SQL = {}

---Creates a SQLite Database. Name should end with .db
---
---Example:
---
---	local stSQLcre = SQL.createdatabase( "eg_db" );
---@param name string
---@return string
function SQL.createdatabase(name) end

---Opens a SQLite database. Name should end with .db
---
---Example:
---
---	local stSQLope = SQL.opendatabase( "eg_db" );
---@param name string
---@return string
function SQL.opendatabase(name) end

---Run a SQLite read command which includes Select. Returns all rows into a LuaTable.Ex: select * from rewards
---
---Example:
---
---	local obSQLrea = SQL.readcommand( "SELECT * FROM eg_tab WHERE eg_tab_id = 1;" );
---@param query? string
---@return any
function SQL.readcommand(query) end

---Runs a SQLite write command which includes CREATE,INSERT, UPDATE. Ex: create TABLE rewards (ID integer  PRIMARY KEY, action VARCHAR(20)) 
---
---Example:
---
---	local stSQLwri = SQL.writecommand( "CREATE TABLE eg_tab ( eg_tab_id integer PRIMARY KEY, eg_tab_row_name text NOT NULL ); INSERT INTO eg_tab ( eg_tab_id, eg_tab_row_name ) VALUES ( 1, 'Example table row' );" );
---@param query? string
---@return string
function SQL.writecommand(query) end

