/* File : example.i */
%module(directors="1") LuaWrap
%include "std_string.i"

/* csharp wrapper code from http://archive.is/tpn6f */
// These typemaps cause parameters of type TYPE named ARGNAME to become IntPtr in C#. 
%define %cs_marshal_intptr(TYPE, ARGNAME...) 
        %typemap(ctype)  TYPE ARGNAME "void*" 
        %typemap(imtype) TYPE ARGNAME "System.IntPtr" 
        %typemap(cstype) TYPE ARGNAME "System.IntPtr" 
        %typemap(in)     TYPE ARGNAME %{ $1 = ($1_ltype)$input; /* IntPtr */ %} 
        %typemap(csin)   TYPE ARGNAME "$csinput" 
        
        %typemap(out)    TYPE ARGNAME %{ $result = $1; %} 
        %typemap(csout, excode=SWIGEXCODE) TYPE ARGNAME { 
                System.IntPtr cPtr = $imcall;$excode 
                return cPtr; 
        } 
        %typemap(csvarout, excode=SWIGEXCODE2) TYPE ARGNAME %{ 
                get { 
                        System.IntPtr cPtr = $imcall;$excode 
                        return cPtr; 
                } 
        %} 

        %typemap(ctype)  TYPE& ARGNAME "void**" 
        %typemap(imtype) TYPE& ARGNAME "ref System.IntPtr" 
        %typemap(cstype) TYPE& ARGNAME "ref System.IntPtr" 
        %typemap(in)     TYPE& ARGNAME %{ $1 = ($1_ltype)$input; %} 
        %typemap(csin)   TYPE& ARGNAME "ref $csinput" 
%enddef 

%cs_marshal_intptr(void*);
%cs_marshal_intptr(lua_State*);
%cs_marshal_intptr(lua_CFunction);
%cs_marshal_intptr(int*);

// Additional typemapping is needed to generate valid directors
%typemap(csdirectorin) lua_State* "$iminput";

/* turn on director wrapping Callback */
%feature("director") LuaCallback;
%feature("director") LuaHook;



%{
    #include "../LuaDLL.hpp"
%}

%include "../LuaDLL.hpp"
