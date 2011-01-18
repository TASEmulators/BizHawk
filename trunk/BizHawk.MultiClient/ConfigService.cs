using System;
using System.IO;

namespace BizHawk.MultiClient
{
    public interface IConfigSerializable
    {
        void Deserialize(string str);
    }

    public static class ConfigService
    {
        public static T Load<T>(string filepath) where T : new()
        {
            T config = new T();

            try
            {
                var file = new FileInfo(filepath);
                var reader = file.OpenText();
                var type = config.GetType();

                while (reader.EndOfStream == false)
                {
                    try
                    {
                        string line = reader.ReadLine();
                        if (string.IsNullOrEmpty(line))
                            continue;

                        int seperatorIndex = line.IndexOf(' ');
                        string name = line.Substring(0, seperatorIndex);
                        string value = line.Substring(seperatorIndex).Trim();

                        var field = type.GetField(name);
                        if (field == null) // look at properties instead of fields? or just abort.
                            continue;

                        Type fieldType = field.FieldType;
                        if (fieldType == typeof(string))
                            field.SetValue(config, value);
                        else if (fieldType == typeof(int))
                            field.SetValue(config, int.Parse(value));
                        else if (fieldType == typeof(long))
                            field.SetValue(config, long.Parse(value));
                        else if (fieldType == typeof(byte))
                            field.SetValue(config, byte.Parse(value));
                        else if (fieldType == typeof(short))
                            field.SetValue(config, short.Parse(value));
                        else if (fieldType == typeof(float))
                            field.SetValue(config, Single.Parse(value));
                        else if (fieldType == typeof(double))
                            field.SetValue(config, Double.Parse(value));
                        else if (fieldType == typeof(bool))
                            field.SetValue(config, bool.Parse(value));
                        else if (fieldType == typeof(char))
                            field.SetValue(config, char.Parse(value));
                        else
                        {
                            var iface = fieldType.GetInterface("IConfigSerializable");
                            if (iface != null)
                            {
                                IConfigSerializable i = (IConfigSerializable) Activator.CreateInstance(fieldType);
                                i.Deserialize(value);
                                field.SetValue(config, i);
                            }
                        }
                    }
                    catch { } // If anything fails to parse, just move on / use defaults, don't crash.
                }
                reader.Close();
            }
            catch { }
            return config;
        }

        public static void Save(string filepath, object config)
        {
            var file = new FileInfo(filepath);
            var writer = file.CreateText();

            var type = config.GetType();
            var fields = type.GetFields();

            foreach (var field in fields)
            {
                writer.WriteLine("{0} {1}", field.Name, field.GetValue(config));
            }
            writer.Close();
        }
    }
}