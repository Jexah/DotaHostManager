using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotaHostLibrary
{
    public static class KV
    {
        // Block type
        private static readonly byte TYPE_BLOCK = 0;

        // Array type
        private static readonly byte TYPE_ARRAY = 1;

        // Reads the KV file at the given path
        public static Dictionary<string, object> read(string path)
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

        // Parses KV Data
        public static Dictionary<string, object> parse(byte[] kvString)
        {
            // Ensure nothing bad happens
            try
            {
                // Create initial trees
                List<Dictionary<string, object>> tree = new List<Dictionary<string, object>>();
                tree.Add(new Dictionary<string, object>());

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
                                Dictionary<string, object> e = tree[tree.Count - 1] as Dictionary<string, object>;

                                // Grab the key
                                string key = keys[keys.Count - 1];

                                // Ensure a list exists for this entry
                                if(!e.ContainsKey(key))
                                {
                                    e.Add(key, new List<string>());
                                }

                                List<string> vals = (List<string>)e[key];
                            
                                vals.Add(resultString);
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

                            tree.Add(new Dictionary<string, object>());
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
                        Dictionary<string, object> obj = tree[tree.Count - 1];
                        tree.RemoveAt(tree.Count - 1);

                        // Attempt to store the tree
                        if (treeType[treeType.Count - 1] == TYPE_BLOCK)
                        {
                            Dictionary<string, object> e = tree[tree.Count - 1] as Dictionary<string, object>;
                            e.Add(keys[keys.Count - 1], obj);
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
