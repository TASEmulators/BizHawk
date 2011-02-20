namespace LuaInterface 
{
	using System;
	using System.IO;
	using System.Collections;
	using System.Reflection;
    using System.Collections.Generic;
    using System.Diagnostics;
    using Lua511;

	/*
	 * Cached method
	 */
	struct MethodCache 
	{
		public MethodBase cachedMethod;
		// List or arguments
		public object[] args;
		// Positions of out parameters
		public int[] outList;
		// Types of parameters
		public MethodArgs[] argTypes;
	}

	/*
	 * Parameter information
	 */
	struct MethodArgs 
	{
		// Position of parameter
		public int index;
		// Type-conversion function
		public ExtractValue extractValue;
	}

	/*
	 * Argument extraction with type-conversion function
	 */
	delegate object ExtractValue(IntPtr luaState, int stackPos);

	/*
	 * Wrapper class for methods/constructors accessed from Lua.
	 * 
	 * Author: Fabio Mascarenhas
	 * Version: 1.0
	 */
	class LuaMethodWrapper 
	{
		ObjectTranslator translator;
		MethodBase method;
		MethodCache lastCalledMethod=new MethodCache();
		string methodName;
		MemberInfo[] members;
		IReflect targetType;
		ExtractValue extractTarget;
		object target;
		BindingFlags bindingType;
		/*
		 * Constructs the wrapper for a known MethodBase instance
		 */
		public LuaMethodWrapper(ObjectTranslator translator, object target, IReflect targetType, MethodBase method) 
		{
			this.translator=translator;
			this.target=target;
			this.targetType=targetType;
			if(targetType!=null)
				extractTarget=translator.typeChecker.getExtractor(targetType);
			this.method=method;
			this.methodName=method.Name;
			if(method.IsStatic) { bindingType=BindingFlags.Static; } 
			else { bindingType=BindingFlags.Instance; }
		}
		/*
		 * Constructs the wrapper for a known method name
		 */
		public LuaMethodWrapper(ObjectTranslator translator, IReflect targetType, string methodName, BindingFlags bindingType) 
		{
			this.translator=translator;
			this.methodName=methodName;
			this.targetType=targetType;
			if(targetType!=null)
				extractTarget=translator.typeChecker.getExtractor(targetType);
			this.bindingType=bindingType;
			members=targetType.UnderlyingSystemType.GetMember(methodName,MemberTypes.Method,bindingType|BindingFlags.Public|BindingFlags.NonPublic);
		}


                /// <summary>
        /// Convert C# exceptions into Lua errors
        /// </summary>
        /// <returns>num of things on stack</returns>
        /// <param name="e">null for no pending exception</param>
        int SetPendingException(Exception e)
        {
            return translator.interpreter.SetPendingException(e);
        }


		/*
		 * Calls the method. Receives the arguments from the Lua stack
		 * and returns values in it.
		 */
		public int call(IntPtr luaState) 
		{
            MethodBase methodToCall=method;
			object targetObject=target;
			bool failedCall=true;
			int nReturnValues=0;
			if(!LuaDLL.lua_checkstack(luaState,5))
				throw new LuaException("Lua stack overflow");

            bool isStatic = (bindingType & BindingFlags.Static) == BindingFlags.Static;

            SetPendingException(null);

			if(methodToCall==null) // Method from name
			{
                if (isStatic) 
					targetObject=null;
				else 
					targetObject=extractTarget(luaState,1);
				//LuaDLL.lua_remove(luaState,1); // Pops the receiver
				if(lastCalledMethod.cachedMethod!=null) // Cached?
				{
                    int numStackToSkip = isStatic ? 0 : 1; // If this is an instance invoe we will have an extra arg on the stack for the targetObject
                    int numArgsPassed = LuaDLL.lua_gettop(luaState) - numStackToSkip;   

					if(numArgsPassed == lastCalledMethod.argTypes.Length) // No. of args match?
					{
						if(!LuaDLL.lua_checkstack(luaState,lastCalledMethod.outList.Length+6))
							throw new LuaException("Lua stack overflow");
						try 
						{
							for(int i=0;i<lastCalledMethod.argTypes.Length;i++) 
							{
								lastCalledMethod.args[lastCalledMethod.argTypes[i].index]=
									lastCalledMethod.argTypes[i].extractValue(luaState, i + 1 + numStackToSkip);

								if(lastCalledMethod.args[lastCalledMethod.argTypes[i].index]==null &&
                                    !LuaDLL.lua_isnil(luaState, i + 1 + numStackToSkip)) 
								{
									throw new LuaException("argument number "+(i+1)+" is invalid"); 
								}
							}
							if((bindingType & BindingFlags.Static)==BindingFlags.Static) 
							{
								translator.push(luaState,lastCalledMethod.cachedMethod.Invoke(null,lastCalledMethod.args));
							} 
							else 
							{
								if(lastCalledMethod.cachedMethod.IsConstructor)
									translator.push(luaState,((ConstructorInfo)lastCalledMethod.cachedMethod).Invoke(lastCalledMethod.args));
								else
									translator.push(luaState,lastCalledMethod.cachedMethod.Invoke(targetObject,lastCalledMethod.args));
							}
							failedCall=false;
						} 
						catch(TargetInvocationException e) 
						{
							// Failure of method invocation
							return SetPendingException(e.GetBaseException());
						}
						catch(Exception e)
						{
							if(members.Length==1) // Is the method overloaded?
								// No, throw error
								return SetPendingException(e);
						}
					}
				}

				// Cache miss
				if(failedCall) 
				{
                    // System.Diagnostics.Debug.WriteLine("cache miss on " + methodName);

					// If we are running an instance variable, we can now pop the targetObject from the stack
                    if (!isStatic)
                    {
                        if (targetObject == null)
                        {
                            translator.throwError(luaState, String.Format("instance method '{0}' requires a non null target object", methodName));
                            LuaDLL.lua_pushnil(luaState);
                            return 1;
                        }

                        LuaDLL.lua_remove(luaState, 1); // Pops the receiver
                    }

					bool hasMatch=false;
                    string candidateName = null;

					foreach(MemberInfo member in members) 
					{
                        candidateName = member.ReflectedType.Name + "." + member.Name;

						MethodBase m=(MethodInfo)member;

						bool isMethod=translator.matchParameters(luaState,m,ref lastCalledMethod);
						if(isMethod) 
						{
							hasMatch=true;
							break;
						}
					}
					if(!hasMatch) 
					{
                        string msg = (candidateName == null) 
                            ? "invalid arguments to method call"
                            : ("invalid arguments to method: " + candidateName);

						translator.throwError(luaState, msg); 
						LuaDLL.lua_pushnil(luaState);
						return 1;
					}
				}
			} 
			else // Method from MethodBase instance 
			{
				if(!methodToCall.IsStatic && !methodToCall.IsConstructor && targetObject==null) 
				{
					targetObject=extractTarget(luaState,1);
					LuaDLL.lua_remove(luaState,1); // Pops the receiver
				}
				if(!translator.matchParameters(luaState,methodToCall,ref lastCalledMethod)) 
				{
					translator.throwError(luaState,"invalid arguments to method call"); 
					LuaDLL.lua_pushnil(luaState);
					return 1;
				}
			}

			if(failedCall) 
			{
				if(!LuaDLL.lua_checkstack(luaState,lastCalledMethod.outList.Length+6))
					throw new LuaException("Lua stack overflow");
				try 
				{
                    if (isStatic) 
					{
						translator.push(luaState,lastCalledMethod.cachedMethod.Invoke(null,lastCalledMethod.args));
					} 
					else 
					{
						if(lastCalledMethod.cachedMethod.IsConstructor)
							translator.push(luaState,((ConstructorInfo)lastCalledMethod.cachedMethod).Invoke(lastCalledMethod.args));
						else
							translator.push(luaState,lastCalledMethod.cachedMethod.Invoke(targetObject,lastCalledMethod.args));
					}
				} 
				catch(TargetInvocationException e) 
				{
					return SetPendingException(e.GetBaseException());
				}
				catch(Exception e)
				{
                    return SetPendingException(e);
				}
			}

			// Pushes out and ref return values
			for(int index=0;index<lastCalledMethod.outList.Length;index++)
			{
				nReturnValues++;
				//for(int i=0;i<lastCalledMethod.outList.Length;i++)
				translator.push(luaState,lastCalledMethod.args[lastCalledMethod.outList[index]]);
			}
			return nReturnValues < 1 ? 1 : nReturnValues;
		}
	}




    /// <summary>
    /// We keep track of what delegates we have auto attached to an event - to allow us to cleanly exit a LuaInterface session
    /// </summary>
    class EventHandlerContainer  : IDisposable
    {
        Dictionary<Delegate, RegisterEventHandler> dict = new Dictionary<Delegate, RegisterEventHandler>();

        public void Add(Delegate handler, RegisterEventHandler eventInfo)
        {
            dict.Add(handler, eventInfo);
        }

        public void Remove(Delegate handler)
        {
            bool found = dict.Remove(handler);
            Debug.Assert(found);
        }

        /// <summary>
        /// Remove any still registered handlers
        /// </summary>
        public void Dispose()
        {
            foreach (KeyValuePair<Delegate, RegisterEventHandler> pair in dict)
            {
                pair.Value.RemovePending(pair.Key);
            }

            dict.Clear();
        }
    }


	/*
	 * Wrapper class for events that does registration/deregistration
	 * of event handlers.
	 * 
	 * Author: Fabio Mascarenhas
	 * Version: 1.0
	 */
	class RegisterEventHandler 
	{
		object target;
		EventInfo eventInfo;
        EventHandlerContainer pendingEvents;

		public RegisterEventHandler(EventHandlerContainer pendingEvents, object target, EventInfo eventInfo) 
		{
			this.target=target;
			this.eventInfo=eventInfo;
            this.pendingEvents = pendingEvents;
		}


		/*
		 * Adds a new event handler
		 */
		public Delegate Add(LuaFunction function) 
		{
			MethodInfo mi = eventInfo.EventHandlerType.GetMethod("Invoke");
			ParameterInfo[] pi = mi.GetParameters();
			LuaEventHandler handler=CodeGeneration.Instance.GetEvent(pi[1].ParameterType,function);

			Delegate handlerDelegate=Delegate.CreateDelegate(eventInfo.EventHandlerType,handler,"HandleEvent");
			eventInfo.AddEventHandler(target,handlerDelegate);
            pendingEvents.Add(handlerDelegate, this);

			return handlerDelegate;
		}

		/*
		 * Removes an existing event handler
		 */
		public void Remove(Delegate handlerDelegate) 
		{
			RemovePending(handlerDelegate);
            pendingEvents.Remove(handlerDelegate);
		}

        /*
         * Removes an existing event handler (without updating the pending handlers list)
         */
        internal void RemovePending(Delegate handlerDelegate)
        {
            eventInfo.RemoveEventHandler(target, handlerDelegate);
        }
	}

	/*
	 * Base wrapper class for Lua function event handlers.
	 * Subclasses that do actual event handling are created
	 * at runtime.
	 * 
	 * Author: Fabio Mascarenhas
	 * Version: 1.0
	 */
	public class LuaEventHandler 
	{
		public LuaFunction handler = null;

		public void handleEvent(object sender,object data) 
		{
			handler.call(new object[] { sender,data },new Type[0]);
		}
	}

	/*
	 * Wrapper class for Lua functions as delegates
	 * Subclasses with correct signatures are created
	 * at runtime.
	 * 
	 * Author: Fabio Mascarenhas
	 * Version: 1.0
	 */
	public class LuaDelegate
	{
		public Type[] returnTypes;
		public LuaFunction function;
		public LuaDelegate() 
		{
			function=null;
			returnTypes=null;
		}
		public object callFunction(object[] args,object[] inArgs,int[] outArgs) 
		{
			// args is the return array of arguments, inArgs is the actual array
			// of arguments passed to the function (with in parameters only), outArgs
			// has the positions of out parameters
			object returnValue;
			int iRefArgs;
			object[] returnValues=function.call(inArgs,returnTypes);
			if(returnTypes[0] == typeof(void)) 
			{
				returnValue=null;
				iRefArgs=0;
			}
			else 
			{
				returnValue=returnValues[0];
				iRefArgs=1;
			}
			// Sets the value of out and ref parameters (from
			// the values returned by the Lua function).
			for(int i=0;i<outArgs.Length;i++) 
			{
				args[outArgs[i]]=returnValues[iRefArgs];
				iRefArgs++;
			}
			return returnValue;
		}
	}

	/*
	 * Static helper methods for Lua tables acting as CLR objects.
	 * 
	 * Author: Fabio Mascarenhas
	 * Version: 1.0
	 */
	public class LuaClassHelper
	{
		/*
		 *  Gets the function called name from the provided table,
		 * returning null if it does not exist
		 */
		public static LuaFunction getTableFunction(LuaTable luaTable,string name) 
		{
			object funcObj=luaTable.rawget(name);
			if(funcObj is LuaFunction)
				return (LuaFunction)funcObj;
            else
				return null;
		}
		/*
		 * Calls the provided function with the provided parameters
		 */
		public static object callFunction(LuaFunction function,object[] args,Type[] returnTypes,object[] inArgs,int[] outArgs) 
		{
			// args is the return array of arguments, inArgs is the actual array
			// of arguments passed to the function (with in parameters only), outArgs
			// has the positions of out parameters
			object returnValue;
			int iRefArgs;
			object[] returnValues=function.call(inArgs,returnTypes);
			if(returnTypes[0] == typeof(void)) 
			{
				returnValue=null;
				iRefArgs=0;
			}
			else 
			{
				returnValue=returnValues[0];
				iRefArgs=1;
			}
			for(int i=0;i<outArgs.Length;i++) 
			{
				args[outArgs[i]]=returnValues[iRefArgs];
				iRefArgs++;
			}
			return returnValue;
		}
	}
}
