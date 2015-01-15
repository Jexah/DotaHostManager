using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DotaHostClientLibrary
{
    public class Kv
    {
        // The sort of element this is
        protected byte Sort;

        // List of keys this KV contains if it is an object
        protected Dictionary<string, Kv> Keys;

        // List of values for this key
        protected List<string> Values;

        // This KV is an object
        private const byte SortObject = 1;

        // This KV is a value
        private const byte SortValue = 2;

        // Block type
        private const byte TypeBlock = 0;

        // Array type
        private const byte TypeArray = 1;

        // Create a new KV that can store keys
        public Kv()
        {
            InitObject();
        }

        protected void InitObject()
        {
            // This is an object sort
            Sort = SortObject;

            // Create store for keys
            Keys = new Dictionary<string, Kv>();
        }

        protected void InheritSource(Kv source)
        {
            if (source == null)
            {
                Sort = 1;
                Keys = new Dictionary<string, Kv>();
                Values = new List<string>();
                return;
            }
            Sort = source.GetSort() == 0 ? (byte)SortObject : source.GetSort();
            Keys = source.GetKeys() ?? new Dictionary<string, Kv>();
            Values = source.GetValues() ?? new List<string>();
        }

        // Create a new KV that can store objects
        public Kv(string value)
        {
            // This is a value kind
            Sort = SortValue;

            // Create store for values and add the value
            Values = new List<string> { value };

        }

        // Adds a key to the KV
        protected bool AddKey(string key, Kv kv)
        {
            // Ensure this is an object sort
            if (Sort != SortObject) return false;

            // Check if the key already exists
            if (Keys.ContainsKey(key)) return false;

            // Add the key
            Keys.Add(key, kv);

            // Success
            return true;
        }

        // Removes a key from the KV
        protected bool RemoveKey(string key)
        {
            // Ensure this is an object sort
            if (Sort != SortObject) return false;

            // Remove the key
            Keys.Remove(key);

            // Success
            return true;
        }

        // Removes a key with given value from the KV
        protected bool RemoveKey(Kv kv)
        {
            // Ensure this is an object sort
            if (Sort != SortObject) return false;

            foreach (var kvp in GetKeys().Where(kvp => kvp.Value == kv))
            {
                // Remove key
                Keys.Remove(kvp.Key);

                // Success
                return true;
            }

            // Remove failed
            return false;
        }

        // Clears the a given key, then recreates it with the given KV
        protected bool SetKey(string key, Kv kv)
        {
            if (ContainsKey(key))
            {
                RemoveKey(key);
            }
            return AddKey(key, kv);
        }

        // Clears the a given value at key, then recreates that key with the given value
        protected bool SetValue(string key, string value)
        {
            return (RemoveKey(key) && AddValue(key, value));
        }

        // Adds a value to the KV
        protected bool AddValue(string key, string value)
        {
            // Ensure this is an object sort
            if (Sort != SortObject) return false;

            // Check if the key already exists
            if (Keys.ContainsKey(key))
            {
                // Grab the KV
                var kv = Keys[key];

                // The key exists, it needs to accept values
                if (kv == null || kv.GetSort() != SortValue) return false;

                // Add the new value
                kv.AddValue(value);

                // Syccess
                return true;
            }

            // The key doesn't exist, make it
            var newKey = new Kv(value);

            // Store the key
            AddKey(key, newKey);

            // Success
            return true;
        }

        // Stores a value into a value KV
        protected bool AddValue(string value)
        {
            // Ensure this is the value sort
            if (Sort != SortValue) return false;

            // Add the value
            Values.Add(value);

            // Success
            return true;
        }

        // Returns the sort of this KV
        public byte GetSort()
        {
            return Sort;
        }

        // Gets a KV at the given key
        public Kv GetKv(string key)
        {
            // Ensure this is the object sort
            if (Sort != SortObject) return null;

            // Ensure we have the key
            if (Keys == null || !Keys.ContainsKey(key)) return null;

            // Return the KV
            return Keys[key];
        }

        // Checks if the KV has the given key
        public bool ContainsKey(string key)
        {
            return Keys.ContainsKey(key);
        }

        // Gets the nth value at the given key
        public string GetValue(string key, int n = 0)
        {
            // Ensure this is the object sort
            if (Sort != SortObject) return null;

            // Ensure we have the key
            if (Keys == null || !Keys.ContainsKey(key)) return null;

            // Grab the kv
            var kv = Keys[key];

            // Ensure a valid reference or return the key
            return kv == null ? null : kv.GetValue(n);
        }

        // Returns the nth value of this KV
        public string GetValue(int n = 0)
        {
            // Ensure this is the correct type
            if (Sort != SortValue) return null;

            // Ensure we have enough values
            if (n < 0 || n >= Values.Count) return null;

            // Return the value
            return Values[n];
        }

        // Returns all the values (WARNING: THIS EXPOSES THE INSIDES, BADNESS COULD HAPPEN)
        public List<string> GetValues(string key)
        {
            // Ensure this is the object sort
            if (Sort != SortObject) return null;

            // Ensure we have the key
            if (!Keys.ContainsKey(key)) return null;

            // Grab the kv
            var kv = Keys[key];

            // Ensure a valid reference or return the key
            return kv == null ? null : kv.GetValues();
        }

        // Returns all the values for this kv (WARNING: THIS EXPOSES THE INSIDES, BADNESS COULD HAPPEN)
        public List<string> GetValues()
        {
            // Ensure this is the correct type and return the value
            return Sort != SortValue ? null : Values;
        }

        // Returns all the keys for this kv (WARNING: THIS EXPOSES THE INSIDES, BADNESS COULD HAPPEN)
        public Dictionary<string, Kv> GetKeys()
        {
            // Ensure this is the correct type and return the value
            return Sort != SortObject ? null : Keys;
        }

        // Escapes a string for output
        public static string EscapeString(string toEscape)
        {
            return toEscape.Replace(@"\", @"\\").Replace("\"", "\\\"");
        }

        // Compiles this KV into a string
        public string ToString(string key = null)
        {
            var output = "";

            switch (Sort)
            {
                case SortValue:
                    {
                        bool first = true;

                        foreach (var value in Values)
                        {
                            if (first)
                            {
                                first = false;
                            }
                            else
                            {
                                output += " ";
                            }

                            output += '"' + key + "\" \"" + EscapeString(value) + '"';
                        }
                    }
                    break;
                case SortObject:
                    {
                        bool first = true;

                        foreach (var entry in Keys)
                        {
                            if (first)
                            {
                                first = false;
                            }
                            else
                            {
                                output += " ";
                            }

                            output += entry.Value.ToString(entry.Key);
                        }

                        if (key != null)
                        {
                            output = '"' + EscapeString(key) + "\" {" + output + "}";
                        }

                    }
                    break;
            }

            return output;
        }

        // Compiles this KV into a string (JSON FORMATTED)
        public string ToJson(string key = null)
        {
            string output = "";

            if (Sort == SortValue)
            {
                // Ensure we have a key
                if (key == null) return "";

                // Return only the first value
                return '"' + EscapeString(key) + "\":\"" + EscapeString(Values[0]) + '"';
            }

            if (Sort != SortObject) return output;

            bool first = true;

            foreach (var entry in Keys)
            {
                if (first)
                {
                    first = false;
                }
                else
                {
                    output += ",";
                }

                output += entry.Value.ToJson(entry.Key);
            }

            if (key == null)
            {
                output = "{" + output + "}";
            }
            else
            {
                output = '"' + EscapeString(key) + "\":{" + output + "}";
            }

            return output;
        }

        // Merges in the given KV (WARNING: This will MODIFY the original KV)
        // NOTE: This will some what link the two data structures, use this carefully!
        public void Merge(Kv kv)
        {
            // Validate input
            if (kv == null) return;
            if (Sort != kv.GetSort()) return;

            if (Sort == SortObject)
            {
                // Grab all the keys that need merging
                var mergeKeys = kv.GetKeys();

                foreach (var entry in mergeKeys)
                {
                    if (!Keys.ContainsKey(entry.Key))
                    {
                        Keys.Add(entry.Key, entry.Value);
                    }
                    else
                    {
                        if (Keys[entry.Key] != null)
                        {
                            // Do a proper merge
                            Keys[entry.Key].Merge(entry.Value);
                        }
                    }
                }
            }
            else
            {
                if (Sort != SortValue) return;

                // Decide if we are doing an override, or an addition
                if (Values.Count == 1)
                {
                    // Since there is only one key, lets just copy the new value over
                    Values[0] = kv.GetValue();
                }
                else
                {
                    // Add each value to the end
                    foreach (var value in kv.GetValues())
                    {
                        Values.Add(value);
                    }
                }
            }
        }

        // Reads the KV file at the given path
        public static Kv Read(string path, bool isJson = false)
        {
            // The file may fail to read
            try
            {
                // Load up the file
                var data = File.ReadAllText(path, Encoding.UTF8);

                // Pass the data
                return Parse(data, isJson);
            }
            catch
            {
                // Failed to load the file
                return null;
            }
        }

        // Parses KV Data
        public static Kv Parse(string kvString, bool isJson = false)
        {
            // Ensure nothing bad happens
            try
            {
                // If it's json, we need to strip the first and last character
                if (isJson)
                {
                    kvString = kvString.Substring(1, kvString.Length - 2);
                }

                // Create initial trees
                var tree = new List<Kv> { new Kv() };

                var treeType = new List<byte> { TypeBlock };

                var keys = new List<string> { null };

                // Index into kvString and the line
                int i = 0;
                int line = 1;

                while (i < kvString.Length)
                {
                    // Grab the next character
                    var chr = kvString[i];

                    if (chr == ' ' || chr == '\t')
                    {
                        // Ignore white space
                    }
                    else if (isJson && chr == ':')
                    {
                        // Ignore, JSON character
                    }
                    else if (isJson && chr == ',')
                    {
                        // Ignore, JSON character
                    }
                    else if (chr == '\n')
                    {
                        // We moved onto the next line
                        ++line;
                        if (kvString[i + 1] == '\r') i++;
                    }
                    else if (chr == '\r')
                    {
                        // We moved onto the next line
                        ++line;
                        if (kvString[i + 1] == '\n') i++;
                    }
                    else if (chr == '/')
                    {
                        if (kvString[i + 1] == '/')
                        {
                            // We found a comment, ignore rest of the line
                            while (++i < kvString.Length)
                            {
                                chr = kvString[i];
                                if (chr == '\n')
                                {
                                    if (kvString[i + 1] == '\r') ++i;
                                    break;
                                }

                                if (chr != '\r') continue;

                                if (kvString[i + 1] == '\n') ++i;
                                break;
                            }

                            // We are on a new line
                            ++line;
                        }
                    }
                    else if (chr == '"')
                    {
                        // Create string to read into
                        string resultString = "";
                        ++i;

                        while (i < kvString.Length)
                        {
                            chr = kvString[i];
                            if (chr == '"') break;

                            switch (chr)
                            {
                                case '\n':
                                    // We moved onto the next line
                                    ++line;
                                    if (kvString[i + 1] == '\r') ++i;
                                    break;
                                case '\r':
                                    // We moved onto the next line
                                    ++line;
                                    if (kvString[i + 1] == '\n') ++i;
                                    break;
                                case '\\':
                                    ++i;
                                    // Grab the next character
                                    chr = kvString[i + 1];

                                    // Check for escaped characters
                                    switch (chr)
                                    {
                                        case '\\': chr = '\\'; break;
                                        case '"': chr = '"'; break;
                                        case '\'': chr = '\\'; break;
                                        case 'n': chr = '\n'; break;
                                        case 'r': chr = '\r'; break;
                                        default:
                                            chr = '\\';
                                            --i;
                                            break;
                                    }
                                    break;
                            }

                            // Add to the result string
                            resultString += chr;
                            ++i;
                        }

                        // Error checking
                        if (i == kvString.Length || chr == '\n' || chr == '\r')
                        {
                            Helpers.Log("UNTERMINATED STRING AT LINE " + line + " IGNORING");
                            return null;
                        }

                        // Check if object or array
                        if (treeType[treeType.Count - 1] == TypeBlock)
                        {
                            // Check if this is a key or a value
                            if (keys[keys.Count - 1] == null)
                            {
                                // A Key
                                keys[keys.Count - 1] = resultString;
                            }
                            else
                            {
                                // A value

                                // Grab the dictonary
                                var e = tree[tree.Count - 1];

                                // Grab the key
                                string key = keys[keys.Count - 1];

                                // Add the value
                                e.AddValue(key, resultString);

                                // Cleanup the current key
                                keys[keys.Count - 1] = null;
                            }
                        }
                        else
                        {
                            Helpers.Log("ARRAYS NOT IMPLEMENTED line " + line);
                            return null;
                        }

                        // Check if we need to reparse the character that ended this string
                        if (chr != '"') --i;
                    }
                    else if (chr == '{')
                    {
                        if (treeType[treeType.Count - 1] == TypeBlock)
                        {
                            // Error checking
                            if (keys[keys.Count - 1] == null)
                            {
                                Helpers.Log("A block needs a key at line " + line + " (offset " + i + ")");
                                return null;
                            }

                            tree.Add(new Kv());
                            treeType.Add(TypeBlock);
                            keys.Add(null);
                        }
                    }
                    else if (chr == '}')
                    {
                        // Error Checking
                        if (tree.Count == 1)
                        {
                            Helpers.Log("Mismatching bracket at line " + line + " (offset " + i + ")");
                            return null;
                        }

                        // Grab the tree type
                        byte tt = treeType[treeType.Count - 1];
                        treeType.RemoveAt(treeType.Count - 1);

                        // Ensure correct tree type
                        if (tt != TypeBlock)
                        {
                            Helpers.Log("Mismatching brackets at line " + line + " (offset " + i + ")");
                            return null;
                        }

                        // Drop the current key
                        keys.RemoveAt(keys.Count - 1);

                        // Grab the current tree
                        Kv obj = tree[tree.Count - 1];
                        tree.RemoveAt(tree.Count - 1);

                        // Attempt to store the tree
                        if (treeType[treeType.Count - 1] == TypeBlock)
                        {
                            Kv e = tree[tree.Count - 1];
                            e.AddKey(keys[keys.Count - 1], obj);
                            keys[keys.Count - 1] = null;
                        }
                        else
                        {
                            Helpers.Log("ARRAYS NOT IMPLEMENTED line " + line);
                            return null;
                        }
                    }
                    else
                    {
                        // Unknwon character
                        Helpers.Log("Unexpected character \"" + chr + "\" at line " + line + " (offset " + i + ")");

                        // Skip to next line
                        while (++i < kvString.Length)
                        {
                            chr = kvString[i];

                            // Check for new line
                            if (chr == '\n' || chr == '\r') break;
                        }

                        // We are on a new line
                        line++;

                        // Move onto the next char
                        i++;
                    }

                    // Move onto the next character
                    ++i;
                }

                // Ensure everything is good
                if (tree.Count == 1) return tree[0];

                Helpers.Log("Missing brackets");
                return null;
            }
            catch
            {
                // Bad kv file
                Helpers.Log("Bad KV file");
                return null;
            }
        }



        // Parses KV Data
        public static void Parse<T>(string kvString, string atKey, dynamic output)
        {
            // Ensure nothing bad happens
            try
            {
                // Create initial trees
                var tree = new List<T> { (T)Activator.CreateInstance(typeof(T)) };

                var treeType = new List<byte> { TypeBlock };

                var keys = new List<string> { null };

                // Index into kvString and the line
                int i = 0;
                int line = 1;

                while (i < kvString.Length)
                {
                    // Grab the next character
                    var chr = kvString[i];

                    switch (chr)
                    {
                        case ' ':
                        case '\t':
                            // Ignore white space
                            break;
                        case '\n':
                            // We moved onto the next line
                            ++line;
                            if (kvString[i + 1] == '\r') i++;
                            break;
                        case '\r':
                            // We moved onto the next line
                            ++line;
                            if (kvString[i + 1] == '\n') i++;
                            break;
                        case '/':
                            if (kvString[i + 1] == '/')
                            {
                                // We found a comment, ignore rest of the line
                                while (++i < kvString.Length)
                                {
                                    chr = kvString[i];
                                    if (chr == '\n' || chr == '\r') break;
                                }

                                // We are on a new line
                                ++line;

                                // Move onto the next char
                                ++i;
                            }
                            break;
                        case '"':
                            // Create string to read into
                            string resultString = "";
                            ++i;

                            while (i < kvString.Length)
                            {
                                chr = kvString[i];
                                if (chr == '"') break;

                                switch (chr)
                                {
                                    case '\n':
                                        // We moved onto the next line
                                        ++line;
                                        if (kvString[i + 1] == '\r') ++i;
                                        break;
                                    case '\r':
                                        // We moved onto the next line
                                        ++line;
                                        if (kvString[i + 1] == '\n') ++i;
                                        break;
                                    case '\\':
                                        ++i;
                                        // Grab the next character
                                        chr = kvString[i + 1];

                                        // Check for escaped characters
                                        switch (chr)
                                        {
                                            case '\\': chr = '\\'; break;
                                            case '"': chr = '"'; break;
                                            case '\'': chr = '\\'; break;
                                            case 'n': chr = '\n'; break;
                                            case 'r': chr = '\r'; break;
                                            default:
                                                chr = '\\';
                                                --i;
                                                break;
                                        }
                                        break;
                                }

                                // Add to the result string
                                resultString += (char)chr;
                                ++i;
                            }

                            // Error checking
                            if (i == kvString.Length || chr == '\n' || chr == '\r')
                            {
                                Helpers.Log("UNTERMINATED STRING AT LINE " + line + " IGNORING");
                                return;
                            }

                            // Check if object or array
                            if (treeType[treeType.Count - 1] == TypeBlock)
                            {
                                // Check if this is a key or a value
                                if (keys[keys.Count - 1] == null)
                                {
                                    // A Key
                                    keys[keys.Count - 1] = resultString;
                                }
                                else
                                {
                                    // A value

                                    // Grab the dictonary
                                    dynamic e = tree[tree.Count - 1];

                                    // Grab the key
                                    string key = keys[keys.Count - 1];

                                    // Add the value
                                    e.addValue(key, resultString);

                                    // Cleanup the current key
                                    keys[keys.Count - 1] = null;
                                }
                            }
                            else
                            {
                                Helpers.Log("ARRAYS NOT IMPLEMENTED line " + line);
                                return;
                            }

                            // Check if we need to reparse the character that ended this string
                            if (chr != '"') --i;
                            break;
                        case '{':
                            if (treeType[treeType.Count - 1] == TypeBlock)
                            {
                                // Error checking
                                if (keys[keys.Count - 1] == null)
                                {
                                    Helpers.Log("A block needs a key at line " + line + " (offset " + i + ")");
                                    return;
                                }

                                tree.Add((T)Activator.CreateInstance(typeof(T)));
                                treeType.Add(TypeBlock);
                                keys.Add(null);
                            }
                            break;
                        case '}':
                            // Error Checking
                            if (tree.Count == 1)
                            {
                                Helpers.Log("Mismatching bracket at line " + line + " (offset " + i + ")");
                                return;
                            }

                            // Grab the tree type
                            byte tt = treeType[treeType.Count - 1];
                            treeType.RemoveAt(treeType.Count - 1);

                            // Ensure correct tree type
                            if (tt != TypeBlock)
                            {
                                Helpers.Log("Mismatching brackets at line " + line + " (offset " + i + ")");
                                return;
                            }

                            // Drop the current key
                            keys.RemoveAt(keys.Count - 1);

                            // Grab the current tree
                            T obj = tree[tree.Count - 1];
                            tree.RemoveAt(tree.Count - 1);

                            // Attempt to store the tree
                            if (treeType[treeType.Count - 1] == TypeBlock)
                            {
                                dynamic e = tree[tree.Count - 1];
                                e.addKey(keys[keys.Count - 1], obj);
                                keys[keys.Count - 1] = null;
                            }
                            else
                            {
                                Helpers.Log("ARRAYS NOT IMPLEMENTED line " + line);
                                return;
                            }
                            break;
                        default:
                            // Unknwon character
                            Helpers.Log("Unexpected character \"" + chr + "\" at line " + line + " (offset " + i + ")");

                            // Skip to next line
                            while (++i < kvString.Length)
                            {
                                chr = kvString[i];

                                // Check for new line
                                if (chr == '\n' || chr == '\r') break;
                            }

                            // We are on a new line
                            line++;

                            // Move onto the next char
                            i++;
                            break;
                    }

                    // Move onto the next character
                    ++i;
                }

                // Ensure everything is good
                if (tree.Count == 1) return;
                Helpers.Log("Missing brackets");
            }
            catch
            {
                // Bad kv file
                Helpers.Log("Bad KV file");
            }
        }

    }
}
