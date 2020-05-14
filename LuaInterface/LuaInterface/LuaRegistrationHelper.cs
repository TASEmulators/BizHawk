using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace NLua
{
    public static class LuaRegistrationHelper
    {

        /// <summary>
        /// Registers all public instance methods in an object tagged with <see cref="LuaGlobalAttribute"/> as Lua global functions
        /// </summary>
        /// <param name="lua">The Lua VM to add the methods to</param>
        /// <param name="o">The object to get the methods from</param>
        public static void TaggedInstanceMethods(Lua lua, object o)
        {

            if (lua == null) throw new ArgumentNullException("lua");
            if (o == null) throw new ArgumentNullException("o");


            foreach (MethodInfo method in o.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public))
            {
                foreach (LuaGlobalAttribute attribute in method.GetCustomAttributes(typeof(LuaGlobalAttribute), true))
                {
                    if (string.IsNullOrEmpty(attribute.Name))
                        lua.RegisterFunction(method.Name, o, method); // CLR name
                    else
                        lua.RegisterFunction(attribute.Name, o, method); // Custom name
                }
            }
        }



        /// <summary>
        /// Registers all public static methods in a class tagged with <see cref="LuaGlobalAttribute"/> as Lua global functions
        /// </summary>
        /// <param name="lua">The Lua VM to add the methods to</param>
        /// <param name="type">The class type to get the methods from</param>
        public static void TaggedStaticMethods(Lua lua, Type type)
        {

            if (lua == null) throw new ArgumentNullException("lua");
            if (type == null) throw new ArgumentNullException("type");
            if (!type.IsClass) throw new ArgumentException("The type must be a class!", "type");


            foreach (MethodInfo method in type.GetMethods(BindingFlags.Static | BindingFlags.Public))
            {
                foreach (LuaGlobalAttribute attribute in method.GetCustomAttributes(typeof(LuaGlobalAttribute), false))
                {
                    if (string.IsNullOrEmpty(attribute.Name))
                        lua.RegisterFunction(method.Name, null, method); // CLR name
                    else
                        lua.RegisterFunction(attribute.Name, null, method); // Custom name
                }
            }
        }



        /// <summary>
        /// Registers an enumeration's values for usage as a Lua variable table
        /// </summary>
        /// <typeparam name="T">The enum type to register</typeparam>
        /// <param name="lua">The Lua VM to add the enum to</param>
        [SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Justification = "The type parameter is used to select an enum type")]
        public static void Enumeration<T>(Lua lua)
        {

            if (lua == null) throw new ArgumentNullException("lua");


            Type type = typeof(T);
            if (!type.IsEnum) throw new ArgumentException("The type must be an enumeration!");

            string[] names = Enum.GetNames(type);
            var values = (T[])Enum.GetValues(type);

            lua.NewTable(type.Name);
            for (int i = 0; i < names.Length; i++)
            {
                string path = type.Name + "." + names[i];
                lua[path] = values[i];
            }
        }

    }
}
