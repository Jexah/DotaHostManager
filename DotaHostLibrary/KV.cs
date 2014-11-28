﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotaHostLibrary
{
    public class KV
    {
        // The sort of element this is
        private byte sort;

        // List of keys this KV contains if it is an object
        private Dictionary<string, KV> keys;

        // List of values for this key
        private List<string> values;

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
        public bool addKey(string key, KV kv)
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

        // Adds a value to the KV
        public bool addValue(string key, string value)
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
        public bool addValue(string value)
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
            if (!this.keys.ContainsKey(key)) return null;

            // Return the KV
            return this.keys[key];
        }

        // Gets the nth value at the given key
        public string getValue(string key, int n=0)
        {
            // Ensure this is the object sort
            if (this.sort != SORT_OBJECT) return null;

            // Ensure we have the key
            if(!this.keys.ContainsKey(key)) return null;

            // Grab the kv
            KV kv = this.keys[key];

            // Ensure a valid reference
            if(kv == null) return null;

            // Return the key
            return kv.getValue(n);
        }

        // Returns the nth value of this KV
        public string getValue(int n=0)
        {
            // Ensure this is the correct type
            if (this.sort != SORT_VALUE) return null;

            // Ensure we have enough values
            if (n < 0 || n >= this.values.Count) return null;

            // Return the value
            return this.values[n];
        }

        // Compiles this KV into a string
        public string toString(string key=null)
        {
            string output = "";

            if(this.sort == SORT_VALUE)
            {
                bool first = true;

                for(int i=0; i<this.values.Count; ++i)
                {
                    if(first)
                    {
                        first = false;   
                    }
                    else
                    {
                        output += " ";
                    }

                    output += '"' + key + "\" \"" + this.values[i] + '"';
                }
            }
            else if(this.sort == SORT_OBJECT)
            {
                bool first = true;

                foreach(KeyValuePair<string,KV> entry in this.keys)
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

                if(key == null)
                {
                    output = "{" + output + "}";
                }
                else
                {
                    output = '"' + key + "\" {" + output + "}";
                }
                
            }

            return output;
        }

        // Reads the KV file at the given path
        public static KV read(string path)
        {
            // The file may fail to read
            try
            {
                // Load up the file
                using (FileStream fs = File.Open(path, FileMode.Open))
                {
                    // Read the data
                    byte[] data = new BinaryReader(fs).ReadBytes((int)fs.Length);

                    // Pass the data
                    return parse(data);
                }
            }
            catch
            {
                // Failed to load the file
                return null;
            }
            
        }

        // Parses KV Data
        public static KV parse(byte[] kvString)
        {
            // Ensure nothing bad happens
            try
            {
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

                while(i < kvString.Length)
                {
                    // Grab the next character
                    Byte chr = kvString[i];

                    if(chr == ' ' || chr == '\t')
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
                    else if(chr == '/')
                    {
                        if(kvString[i + 1] == '/')
                        {
                            // We found a comment, ignore rest of the line
                            while(++i < kvString.Length)
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
                    else if(chr == '"')
                    {
                        // Create string to read into
                        string resultString = "";
                        ++i;

                        while(i < kvString.Length)
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
                            else if(chr == '\\')
                            {
                                ++i;
                                // Grab the next character
                                chr = kvString[i + 1];

                                // Check for escaped characters
                                switch(chr)
                                {
                                    case (byte)'\\': chr = (byte)'\\'; break;
                                    case (byte)'"': chr = (byte)'"'; break;
                                    case (byte)'\'': chr = (byte)'\\'; break;
                                    case (byte)'n': chr = (byte)'\n'; break;
                                    case (byte)'r': chr = (byte)'\r'; break;
                                    default:
                                        chr = (byte)'\\';
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
                    else if(chr == '{')
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
                    else if(chr == '}')
                    {
                        // Error Checking
                        if (tree.Count == 1)
                        {
                            Helpers.log("Mismatching bracket at line " + line + " (offset " + i + ")");
                            return null;
                        }

                        // Grab the tree type
                        byte tt = treeType[treeType.Count-1];
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
                if(tree.Count != 1)
                {
                    Helpers.log("Missing brackets");
                    return null;
                }

                return tree[0];
            }
            catch
            {
                // Bad KV file
                return null;
            }
        }
    }
}