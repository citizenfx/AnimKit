using System.Linq;
using System.Collections.Generic;

namespace AnimKit.UI
{
    /// <summary>
    /// A very simple variables manager.
    /// Used for command line arguments and as global state storage.
    /// Might be global, per asset, per animation and etc.
    /// </summary>
    internal class Variables
    {
        /// <summary>
        /// Global instance that can be used in whole app.
        /// </summary>
        public static Variables Instance { get; } = new Variables();

        /// <summary>
        /// List of stored variables.
        /// </summary>
        private List<KeyValuePair<string, object>> List { get; } = new();

        /// <summary>
        /// Add raw variables from local roaming file.
        /// Basically used only for commandline.txt
        /// Raw variables are pre-processed before adding.
        /// </summary>
        /// <param name="filePath">File path</param>
        /// <param name="separator">Content separator</param>
        public void AddRawFromFile(string filePath, string separator = " ")
        {
            string content = Utils.ReadRoamingFileContent(filePath);
            AddRaw(content.Split(separator));
        }

        /// <summary>
        /// Add raw variables from string array.
        /// Raw variables are pre-processed before adding.
        /// </summary>
        /// <param name="array">Array of raw variables</param>
        public void AddRaw(string[] array)
        {
            AddRaw(array.ToList());
        }

        /// <summary>
        /// Add raw variables from string list.
        /// Raw variables are pre-processed before adding.
        /// </summary>
        /// <param name="array">List of raw variables</param>
        public void AddRaw(List<string> list)
        {
            foreach (string entry in list)
            {
                AddRaw(entry);
            }
        }

        /// <summary>
        /// Add raw variable from string content.
        /// Raw variables are pre-processed before adding.
        /// </summary>
        /// <param name="rawData">Raw text data</param>
        public void AddRaw(string rawContent)
        {
            // Make sure content is trimmed.
            string rawData = rawContent.Trim();

            // If content not starts from "-" then most likely it won't fit our format.
            if (!rawData.StartsWith("-"))
            {
                return;
            }

            // If variable is a key-value style.
            if (rawData.Contains("="))
            {
                string[] parts = rawData.Split("=");

                if (parts.Length > 1)
                {
                    // Getting variable name.
                    string rawName = parts[0].Trim();
                    string name = rawName[1..];

                    // Getting variable value.
                    string rawValue = string.Join("=", parts.Skip(1).ToArray());
                    KeyValuePair<string, object> variable;

                    if (rawValue is "" or "null" or "undefined")
                    {
                        // Is variable set to some undefined values - skip it.
                        return;
                    }
                    else if (rawValue is "true" or "false")
                    {
                        variable = new(name, rawValue == "true");
                    }
                    else if (int.TryParse(rawValue, out int parsedInt))
                    {
                        variable = new(name, parsedInt);
                    }
                    else if (float.TryParse(rawValue, out float parsedFloat))
                    {
                        variable = new(name, parsedFloat);
                    }
                    else
                    {
                        variable = new(name, rawValue);
                    }

                    // Add variable with value.
                    Add(variable);
                    return;
                }
            }

            // Just add variable as existing otherwise.
            Add(rawData[1..], true);
        }

        /// <summary>
        /// Add variable from key value pair.
        /// </summary>
        /// <param name="variable">Variable data</param>
        public void Add(KeyValuePair<string, object> variable)
        {
            if (!Has(variable.Key))
            {
                List.Add(variable);
            }
        }

        /// <summary>
        /// Add variable from name and value.
        /// </summary>
        /// <typeparam name="T">Variable type</typeparam>
        /// <param name="name">Variable name</param>
        /// <param name="value">Variable value</param>
        public void Add<T>(string name, T value)
        {
            // If value is null, don't add variable.
            if (value == null)
            {
                return;
            }

            Add(new KeyValuePair<string, object>(name, value));
        }

        /// <summary>
        /// Set variable by name to value.
        /// </summary>
        /// <typeparam name="T">Variable type</typeparam>
        /// <param name="name">Variable name</param>
        /// <param name="value">Variable value</param>
        public void Set<T>(string name, T value)
        {
            // Remove old variable first.
            Clear(name);

            // If value is null, just remove variable.
            if (value == null)
            {
                return;
            }

            // Add new variable with the same name but new value.
            Add(name, value);
        }

        /// <summary>
        /// Returns value of variable.
        /// </summary>
        /// <typeparam name="T">Expected type</typeparam>
        /// <param name="name">Variable name</param>
        /// <param name="defaultValue">Default value if failed</param>
        /// <returns>Variable value</returns>
        public T Get<T>(string name, T defaultValue)
        {
            if (!Has(name))
            {
                return defaultValue;
            }

            KeyValuePair<string, object> variable = List.Find(v => v.Key == name);

            return variable.Value is T defautValue ? (T)variable.Value : defaultValue;
        }

        /// <summary>
        /// Returns if variable exist in list.
        /// </summary>
        /// <param name="name">Variable name</param>
        /// <returns>Whether or not variable exist</returns>
        public bool Has(string name)
        {
            return List.Any(v => v.Key == name);
        }

        /// <summary>
        /// Clear variable by index.
        /// </summary>
        /// <param name="index">Variable index</param>
        public void Clear(int index)
        {
            if (index != -1)
            {
                List.RemoveAt(index);
            }
        }

        /// <summary>
        /// Clear variable by name.
        /// </summary>
        /// <param name="name">Varable name</param>
        public void Clear(string name)
        {
            int entryIndex = GetIndexByName(name);
            Clear(entryIndex);
        }

        /// <summary>
        /// Clear all variables.
        /// </summary>
        public void ClearAll()
        {
            List.Clear();
        }

        /// <summary>
        /// Returns variable index from name.
        /// </summary>
        /// <param name="name">Variable name</param>
        /// <returns>Entry index or -1</returns>
        private int GetIndexByName(string name)
        {
            return List.FindIndex(v => v.Key == name);
        }
    }
}
