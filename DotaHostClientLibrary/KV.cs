using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace DotaHostClientLibrary
{
    public class KV
    {
        // The sort of element this is
        protected byte sort;

        // List of keys this KV contains if it is an object
        protected Dictionary<string, KV> keys;

        // List of values for this key
        protected List<string> values;

        // This KV is an object
        private static byte SORT_OBJECT = 1;

        // This KV is a value
        private static byte SORT_VALUE = 2;

        // Block type
        private static readonly byte TYPE_BLOCK = 0;

        // Array type
        private static readonly byte TYPE_ARRAY = 1;

        // Create a new KV that can store keys
        public KV()
        {
            initObject();
        }

        protected void initObject()
        {
            // This is an object sort
            this.sort = SORT_OBJECT;

            // Create store for keys
            keys = new Dictionary<string, KV>();
        }

        // Create a new KV that can store objects
        public KV(string value)
        {
            // This is a value kind
            this.sort = SORT_VALUE;

            // Create store for values
            values = new List<string>();

            // Add the value
            values.Add(value);
        }

        // Adds a key to the KV
        protected bool addKey(string key, KV kv)
        {
            // Ensure this is an object sort
            if (this.sort != SORT_OBJECT) return false;

            // Check if the key already exists
            if (this.keys.ContainsKey(key)) return false;

            // Add the key
            this.keys.Add(key, kv);

            // Success
            return true;
        }

        // Removes a key from the KV
        protected bool removeKey(string key)
        {
            // Ensure this is an object sort
            if (this.sort != SORT_OBJECT) return false;

            // Remove the key
            this.keys.Remove(key);

            // Success
            return true;
        }

        // Removes a key with given value from the KV
        protected bool removeKey(KV kv)
        {
            // Ensure this is an object sort
            if (this.sort != SORT_OBJECT) return false;

            foreach (KeyValuePair<string, KV> kvp in getKeys())
            {
                if (kvp.Value == kv)
                {
                    // Remove key
                    this.keys.Remove(kvp.Key);

                    // Success
                    return true;
                }
            }

            // Remove failed
            return false;
        }

        // Clears the a given key, then recreates it with the given KV
        protected bool setKey(string key, KV kv)
        {
            return (removeKey(key) && addKey(key, kv));
        }

        // Clears the a given value at key, then recreates that key with the given value
        protected bool setValue(string key, string value)
        {
            return (removeKey(key) && addValue(key, value));
        }

        // Adds a value to the KV
        protected bool addValue(string key, string value)
        {
            // Ensure this is an object sort
            if (this.sort != SORT_OBJECT) return false;

            // Check if the key already exists
            if (this.keys.ContainsKey(key))
            {
                // Grab the KV
                KV kv = this.keys[key];

                // The key exists, it needs to accept values
                if (kv == null || kv.getSort() != SORT_VALUE) return false;

                // Add the new value
                kv.addValue(value);

                // Syccess
                return true;
            }
            else
            {
                // The key doesn't exist, make it
                KV newKey = new KV(value);

                // Store the key
                this.addKey(key, newKey);

                // Success
                return true;
            }
        }

        // Stores a value into a value KV
        protected bool addValue(string value)
        {
            // Ensure this is the value sort
            if (this.sort != SORT_VALUE) return false;

            // Add the value
            this.values.Add(value);

            // Success
            return true;
        }

        // Returns the sort of this KV
        public byte getSort()
        {
            return this.sort;
        }

        // Gets a KV at the given key
        public KV getKV(string key)
        {
            // Ensure this is the object sort
            if (this.sort != SORT_OBJECT) return null;

            // Ensure we have the key
            if (this.keys == null || !this.keys.ContainsKey(key)) return null;

            // Return the KV
            return this.keys[key];
        }

        // Checks if the KV has the given key
        public bool containsKey(string key)
        {
            return keys.ContainsKey(key);
        }

        // Gets the nth value at the given key
        public string getValue(string key, int n = 0)
        {
            // Ensure this is the object sort
            if (this.sort != SORT_OBJECT) return null;

            // Ensure we have the key
            if (this.keys == null || !this.keys.ContainsKey(key)) return null;

            // Grab the kv
            KV kv = this.keys[key];

            // Ensure a valid reference
            if (kv == null) return null;

            // Return the key
            return kv.getValue(n);
        }

        // Returns the nth value of this KV
        public string getValue(int n = 0)
        {
            // Ensure this is the correct type
            if (this.sort != SORT_VALUE) return null;

            // Ensure we have enough values
            if (n < 0 || n >= this.values.Count) return null;

            // Return the value
            return this.values[n];
        }

        // Returns all the values (WARNING: THIS EXPOSES THE INSIDES, BADNESS COULD HAPPEN)
        public List<string> getValues(string key)
        {
            // Ensure this is the object sort
            if (this.sort != SORT_OBJECT) return null;

            // Ensure we have the key
            if (!this.keys.ContainsKey(key)) return null;

            // Grab the kv
            KV kv = this.keys[key];

            // Ensure a valid reference
            if (kv == null) return null;

            // Return the key
            return kv.getValues();
        }

        // Returns all the values for this kv (WARNING: THIS EXPOSES THE INSIDES, BADNESS COULD HAPPEN)
        public List<string> getValues()
        {
            // Ensure this is the correct type
            if (this.sort != SORT_VALUE) return null;

            // Return the value
            return this.values;
        }

        // Returns all the keys for this kv (WARNING: THIS EXPOSES THE INSIDES, BADNESS COULD HAPPEN)
        public Dictionary<string, KV> getKeys()
        {
            // Ensure this is the correct type
            if (this.sort != SORT_OBJECT) return null;

            // Return the value
            return this.keys;
        }

        // Escapes a string for output
        public static string escapeString(string toEscape)
        {
            return toEscape.Replace(@"\", @"\\").Replace("\"", "\\\"");
        }

        // Compiles this KV into a string
        public string toString(string key = null)
        {
            string output = "";

            if (this.sort == SORT_VALUE)
            {
                bool first = true;

                for (int i = 0; i < this.values.Count; ++i)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        output += " ";
                    }

                    output += '"' + key + "\" \"" + escapeString(this.values[i]) + '"';
                }
            }
            else if (this.sort == SORT_OBJECT)
            {
                bool first = true;

                foreach (KeyValuePair<string, KV> entry in this.keys)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        output += " ";
                    }

                    output += entry.Value.toString(entry.Key);
                }

                if (key != null)
                {
                    output = '"' + escapeString(key) + "\" {" + output + "}";
                }

            }

            return output;
        }

        // Compiles this KV into a string (JSON FORMATTED)
        public string toJSON(string key = null)
        {
            string output = "";

            if (this.sort == SORT_VALUE)
            {
                // Ensure we have a key
                if (key == null) return "";

                // Return only the first value
                return '"' + escapeString(key) + "\":\"" + escapeString(this.values[0]) + '"';
            }
            else if (this.sort == SORT_OBJECT)
            {
                bool first = true;

                foreach (KeyValuePair<string, KV> entry in this.keys)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        output += ",";
                    }

                    output += entry.Value.toJSON(entry.Key);
                }

                if (key == null)
                {
                    output = "{" + output + "}";
                }
                else
                {
                    output = '"' + escapeString(key) + "\":{" + output + "}";
                }

            }

            return output;
        }

        // Merges in the given KV (WARNING: This will MODIFY the original KV)
        // NOTE: This will some what link the two data structures, use this carefully!
        public void merge(KV kv)
        {
            // Validate input
            if (kv == null) return;
            if (this.sort != kv.getSort()) return;

            if (this.sort == SORT_OBJECT)
            {
                // Grab all the keys that need merging
                Dictionary<string, KV> mergeKeys = kv.getKeys();

                foreach (KeyValuePair<String, KV> entry in mergeKeys)
                {
                    if (!this.keys.ContainsKey(entry.Key))
                    {
                        this.keys.Add(entry.Key, entry.Value);
                    }
                    else
                    {
                        if (this.keys[entry.Key] != null)
                        {
                            // Do a proper merge
                            this.keys[entry.Key].merge(entry.Value);
                        }
                    }
                }
            }
            else if (this.sort == SORT_VALUE)
            {
                // Decide if we are doing an override, or an addition
                if (this.values.Count == 1)
                {
                    // Since there is only one key, lets just copy the new value over
                    this.values[0] = kv.getValue();
                }
                else
                {
                    // Add each value to the end
                    foreach (string value in kv.getValues())
                    {
                        this.values.Add(value);
                    }
                }
            }

        }

        // Reads the KV file at the given path
        public static KV read(string path, bool isJSON = false)
        {
            // The file may fail to read
            try
            {
                // Load up the file
                string data = File.ReadAllText(path, Encoding.UTF8);

                // Pass the data
                return parse(data, isJSON);
            }
            catch
            {
                // Failed to load the file
                return null;
            }
        }

        // Parses KV Data
        public static KV parse(string kvString, bool isJSON = false)
        {
            // Ensure nothing bad happens
            try
            {
                // If it's json, we need to strip the first and last character
                if (isJSON)
                {
                    kvString = kvString.Substring(1, kvString.Length - 2);
                }

                // Create initial trees
                List<KV> tree = new List<KV>();
                tree.Add(new KV());

                List<byte> treeType = new List<byte>();
                treeType.Add(TYPE_BLOCK);

                List<string> keys = new List<string>();
                keys.Add(null);

                // Index into kvString and the line
                int i = 0;
                int line = 1;

                while (i < kvString.Length)
                {
                    // Grab the next character
                    Char chr = kvString[i];

                    if (chr == ' ' || chr == '\t')
                    {
                        // Ignore white space
                    }
                    else if (isJSON && chr == ':')
                    {
                        // Ignore, JSON character
                    }
                    else if (isJSON && chr == ',')
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
                                if (chr == '\n' || chr == '\r') break;
                            }

                            // We are on a new line
                            ++line;

                            // Move onto the next char
                            ++i;
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

                            if (chr == '\n')
                            {
                                // We moved onto the next line
                                ++line;
                                if (kvString[i + 1] == '\r') ++i;
                            }
                            else if (chr == '\r')
                            {
                                // We moved onto the next line
                                ++line;
                                if (kvString[i + 1] == '\n') ++i;
                            }
                            else if (chr == '\\')
                            {
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
                            }

                            // Add to the result string
                            resultString += (char)chr;
                            ++i;
                        }

                        // Error checking
                        if (i == kvString.Length || chr == '\n' || chr == '\r')
                        {
                            Helpers.log("UNTERMINATED STRING AT LINE " + line + " IGNORING");
                            return null;
                        }

                        // Check if object or array
                        if (treeType[treeType.Count - 1] == TYPE_BLOCK)
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
                                KV e = tree[tree.Count - 1];

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
                            Helpers.log("ARRAYS NOT IMPLEMENTED line " + line);
                            return null;
                        }

                        // Check if we need to reparse the character that ended this string
                        if (chr != '"') --i;
                    }
                    else if (chr == '{')
                    {
                        if (treeType[treeType.Count - 1] == TYPE_BLOCK)
                        {
                            // Error checking
                            if (keys[keys.Count - 1] == null)
                            {
                                Helpers.log("A block needs a key at line " + line + " (offset " + i + ")");
                                return null;
                            }

                            tree.Add(new KV());
                            treeType.Add(TYPE_BLOCK);
                            keys.Add(null);
                        }
                    }
                    else if (chr == '}')
                    {
                        // Error Checking
                        if (tree.Count == 1)
                        {
                            Helpers.log("Mismatching bracket at line " + line + " (offset " + i + ")");
                            return null;
                        }

                        // Grab the tree type
                        byte tt = treeType[treeType.Count - 1];
                        treeType.RemoveAt(treeType.Count - 1);

                        // Ensure correct tree type
                        if (tt != TYPE_BLOCK)
                        {
                            Helpers.log("Mismatching brackets at line " + line + " (offset " + i + ")");
                            return null;
                        }

                        // Drop the current key
                        keys.RemoveAt(keys.Count - 1);

                        // Grab the current tree
                        KV obj = tree[tree.Count - 1];
                        tree.RemoveAt(tree.Count - 1);

                        // Attempt to store the tree
                        if (treeType[treeType.Count - 1] == TYPE_BLOCK)
                        {
                            KV e = tree[tree.Count - 1];
                            e.addKey(keys[keys.Count - 1], obj);
                            keys[keys.Count - 1] = null;
                        }
                        else
                        {
                            Helpers.log("ARRAYS NOT IMPLEMENTED line " + line);
                            return null;
                        }
                    }
                    else
                    {
                        // Unknwon character
                        Helpers.log("Unexpected character \"" + chr + "\" at line " + line + " (offset " + i + ")");

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
                if (tree.Count != 1)
                {
                    Helpers.log("Missing brackets");
                    return null;
                }

                return tree[0];
            }
            catch
            {
                // Bad kv file
                Helpers.log("Bad KV file");
                return null;
            }
        }



        // Parses KV Data
        public static void parse<T>(string kvString, string atKey, dynamic output)
        {
            // Ensure nothing bad happens
            try
            {
                // Create initial trees
                List<T> tree = new List<T>();
                tree.Add((T)Activator.CreateInstance(typeof(T)));

                List<byte> treeType = new List<byte>();
                treeType.Add(TYPE_BLOCK);

                List<string> keys = new List<string>();
                keys.Add(null);

                // Index into kvString and the line
                int i = 0;
                int line = 1;

                while (i < kvString.Length)
                {
                    // Grab the next character
                    Char chr = kvString[i];

                    if (chr == ' ' || chr == '\t')
                    {
                        // Ignore white space
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
                                if (chr == '\n' || chr == '\r') break;
                            }

                            // We are on a new line
                            ++line;

                            // Move onto the next char
                            ++i;
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

                            if (chr == '\n')
                            {
                                // We moved onto the next line
                                ++line;
                                if (kvString[i + 1] == '\r') ++i;
                            }
                            else if (chr == '\r')
                            {
                                // We moved onto the next line
                                ++line;
                                if (kvString[i + 1] == '\n') ++i;
                            }
                            else if (chr == '\\')
                            {
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
                            }

                            // Add to the result string
                            resultString += (char)chr;
                            ++i;
                        }

                        // Error checking
                        if (i == kvString.Length || chr == '\n' || chr == '\r')
                        {
                            Helpers.log("UNTERMINATED STRING AT LINE " + line + " IGNORING");
                            return;
                        }

                        // Check if object or array
                        if (treeType[treeType.Count - 1] == TYPE_BLOCK)
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
                            Helpers.log("ARRAYS NOT IMPLEMENTED line " + line);
                            return;
                        }

                        // Check if we need to reparse the character that ended this string
                        if (chr != '"') --i;
                    }
                    else if (chr == '{')
                    {
                        if (treeType[treeType.Count - 1] == TYPE_BLOCK)
                        {
                            // Error checking
                            if (keys[keys.Count - 1] == null)
                            {
                                Helpers.log("A block needs a key at line " + line + " (offset " + i + ")");
                                return;
                            }

                            tree.Add((T)Activator.CreateInstance(typeof(T)));
                            treeType.Add(TYPE_BLOCK);
                            keys.Add(null);
                        }
                    }
                    else if (chr == '}')
                    {
                        // Error Checking
                        if (tree.Count == 1)
                        {
                            Helpers.log("Mismatching bracket at line " + line + " (offset " + i + ")");
                            return;
                        }

                        // Grab the tree type
                        byte tt = treeType[treeType.Count - 1];
                        treeType.RemoveAt(treeType.Count - 1);

                        // Ensure correct tree type
                        if (tt != TYPE_BLOCK)
                        {
                            Helpers.log("Mismatching brackets at line " + line + " (offset " + i + ")");
                            return;
                        }

                        // Drop the current key
                        keys.RemoveAt(keys.Count - 1);

                        // Grab the current tree
                        T obj = tree[tree.Count - 1];
                        tree.RemoveAt(tree.Count - 1);

                        // Attempt to store the tree
                        if (treeType[treeType.Count - 1] == TYPE_BLOCK)
                        {
                            dynamic e = tree[tree.Count - 1];
                            e.addKey(keys[keys.Count - 1], obj);
                            keys[keys.Count - 1] = null;
                        }
                        else
                        {
                            Helpers.log("ARRAYS NOT IMPLEMENTED line " + line);
                            return;
                        }
                    }
                    else
                    {
                        // Unknwon character
                        Helpers.log("Unexpected character \"" + chr + "\" at line " + line + " (offset " + i + ")");

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
                if (tree.Count != 1)
                {
                    Helpers.log("Missing brackets");
                    return;
                }

                output = (T)((tree[0] as dynamic).getKV(atKey));
                return;
            }
            catch
            {
                // Bad kv file
                Helpers.log("Bad KV file");
                return;
            }
        }

    }
}
