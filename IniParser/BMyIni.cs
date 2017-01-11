using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IniParser
{
    public class BMyIni
    {
        private Dictionary<string, Dictionary<string, string>> Data;
        private string currentNamespace = null;
        private string[] diff = null;

        /// <summary>
        /// change namespace
        /// </summary>
        public string Namespace
        {
            get { return currentNamespace;  }
            set { currentNamespace = value.Trim(); }
        }

        /// <summary>
        /// create values from INI Formatted data
        /// </summary>
        /// <param name="data">serialized data with INI sutff in it.</param>
        /// <param name="currentNamespace">namespace to be used (can be changed by Namespace property at any time)</param>
        public BMyIni(string data, string currentNamespace)
        {
            Namespace = currentNamespace;
            Data = (new Serializer()).deserialize(data, out diff);
        }

        /// <summary>
        /// serialized data with setting in INI-Format (includes former comments and all unknown stuff that could be parsed in the first place
        /// </summary>
        /// <returns>string</returns>
        public string getSerialized()
        {
            return (new Serializer()).serialize(Data, diff);
        }


        /// <summary>
        /// adds namespace prefix to a sectoinname
        /// </summary>
        /// <param name="section"></param>
        /// <returns>full qualified section</returns>
        private string normalizeSection(string section)
        {
            return currentNamespace + "." + section;
        }
        
        /// <summary>
        /// get value for given properties
        /// </summary>
        /// <param name="section">section without namespace</param>
        /// <param name="key"></param>
        /// <returns>(string)value or null if not found</returns>
        public string read(string section, string key)
        {
            return (Data.ContainsKey(normalizeSection(section)) && Data[normalizeSection(section)].ContainsKey(key)) ? Data[normalizeSection(section)][key] : null;
        }

        /// <summary>
        /// add new value or update existing one
        /// </summary>
        /// <param name="section"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns>true on successs</returns>
        public bool write(string section, string key, string value)
        {
            if(-1 != section.IndexOfAny(new Char[] { '[', ']' }))
            {
                return false;
            }
            if (!Data.ContainsKey(normalizeSection(section)))
            {
                Data.Add(normalizeSection(section), new Dictionary<string, string>());
            }
            if (Data[normalizeSection(section)].ContainsKey(key))
            {
                Data[normalizeSection(section)][key] = value;
            }
            else
            {
                Data[normalizeSection(section)].Add(key, value);
            }

            return Data[normalizeSection(section)].ContainsKey(key);
        }

        /// <summary>
        /// remove key from a section
        /// </summary>
        /// <param name="section"></param>
        /// <param name="key"></param>
        /// <returns>bool on success</returns>
        public bool remove(string section, string key)
        {
            if (-1 != section.IndexOfAny(new Char[] { '[', ']' }))
            {
                return false;
            }
            string FQSection = normalizeSection(section);
            if(Data.ContainsKey(FQSection) && Data[FQSection].ContainsKey(key))
            {
                Data[FQSection].Remove(key);
                return !Data[FQSection].ContainsKey(key);
            } else
            {
                return false;
            }
        }

        /// <summary>
        /// remove a complete sectoin
        /// </summary>
        /// <param name="section"></param>
        /// <returns>true on success</returns>
        public bool remove(string section)
        {
            if (-1 != section.IndexOfAny(new Char[] { '[', ']' }))
            {
                return false;
            }
            string FQSection = normalizeSection(section);
            if (Data.ContainsKey(FQSection))
            {
                Data.Remove(FQSection);
                return !Data.ContainsKey(FQSection);
            }
            else
            {
                return false;
            }
        }
        

        /// <summary>
        /// get all values from a section
        /// </summary>
        /// <param name="section"></param>
        /// <returns></returns>
        public Dictionary<string, string> getSection(string section)
        {
            return Data.ContainsKey(normalizeSection(section)) ? Data[normalizeSection(section)] : null;
        }

        private class Serializer
        {
            private System.Text.RegularExpressions.Regex RgxKeyValuePair = new System.Text.RegularExpressions.Regex(@"^[^=]+[=][\S\s]*$");
            private System.Text.RegularExpressions.Regex RgxSection = new System.Text.RegularExpressions.Regex(@"^\[[^\]]+\]\s*$");
            private System.Text.RegularExpressions.Regex RgxEncapsulated = new System.Text.RegularExpressions.Regex(@"^""[\S\s]*""");
            private System.Text.RegularExpressions.Regex RgxDiffMarker = new System.Text.RegularExpressions.Regex(@"^---@@@SECTION::[^\[\]]+@@@$");

            public string serialize(Dictionary<string,Dictionary<string,string>> Data, string[] diff)
            {
                StringBuilder Buffer = new StringBuilder();
                foreach(string diffLine in diff)
                {
                    if (RgxDiffMarker.IsMatch(diffLine))
                    {
                        string section = diffLine.Substring(15, diffLine.Length - 18);
                        if (Data.ContainsKey(section))
                        {
                            Buffer.AppendLine(string.Format(@"[{0}]",section));
                            foreach(KeyValuePair<string,string> keyvalue in Data[section])
                            {
                                string[] valueLines = keyvalue.Value.Split(new Char[] { '\n', '\r' });
                                for(int i = 0; i < valueLines.Length; i++)
                                {
                                    if (i == 0)
                                    {
                                        Buffer.AppendLine(string.Format("{0}=\"{1}\"", keyvalue.Key, valueLines[i]));
                                    } else
                                    {
                                        Buffer.AppendLine(string.Format("=\"{0}\"", valueLines[i]));
                                    }
                                }
                            }
                            Data.Remove(section);
                        }
                    } else
                    {
                        Buffer.AppendLine(diffLine);
                    }
                }
                foreach(KeyValuePair<string, Dictionary<string,string>> section in Data)
                {
                    Buffer.AppendLine(string.Format(@"[{0}]", section.Key));
                    foreach (KeyValuePair<string, string> keyvalue in section.Value)
                    {
                        string[] valueLines = keyvalue.Value.Split(new Char[] { '\n', '\r' });
                        for (int i = 0; i < valueLines.Length; i++)
                        {
                            if (i == 0)
                            {
                                Buffer.AppendLine(string.Format("{0}=\"{1}\"", keyvalue.Key, valueLines[i]));
                            }
                            else
                            {
                                Buffer.AppendLine(string.Format("=\"{0}\"", valueLines[i]));
                            }
                        }
                    }
                }
                return Buffer.ToString();
            }

            public Dictionary<string, Dictionary<string, string>> deserialize(string serialized, out string[] diff)
            {
                string[] linesSource = serialized.Split(new Char[] {'\n','\r'}, StringSplitOptions.RemoveEmptyEntries);
                StringBuilder serializedBuffer = new StringBuilder();
                string section = null;
                string key = null;
                Dictionary<string, Dictionary<string, string>> Data = new Dictionary<string, Dictionary<string, string>>();
                foreach(string currentLine in linesSource)
                {
                    if (isSection(currentLine))
                    {
                        // prepare section
                        key = null;
                        section = getSectionName(currentLine);
                        if (!Data.ContainsKey(section))
                        {
                            Data.Add(section, new Dictionary<string, string>());
                        }
                        serializedBuffer.AppendLine(getDiffMarker(section));
                    } else if (section != null 
                        && Data.ContainsKey(section) 
                        && isKeyValuePair(currentLine))
                    {
                        // add Key Value to Section
                        int i_seperator = currentLine.IndexOf('=');
                        key = currentLine.Substring(0, i_seperator).Trim();
                        string val = getEncapsulated(currentLine.Substring(i_seperator + 1).Trim());

                        if (Data[section].ContainsKey(key))
                        {
                            Data[section][key] = val;
                        } else
                        {
                            Data[section].Add(key,val);
                        }
                    }
                    else if (section != null 
                        && Data.ContainsKey(section) 
                        && key != null 
                        && Data[section].ContainsKey(key)
                        && isSingleValue(currentLine))
                    {
                        //add line to last key
                        string val = getEncapsulated(currentLine.Trim().Substring(1));
                        Data[section][key] = Data[section][key] + "\n" + val;
                    }
                    else
                    {
                        serializedBuffer.AppendLine(currentLine);
                    }
                }
                diff = serializedBuffer.ToString().Split(new Char[] {'\n','\r'}, StringSplitOptions.RemoveEmptyEntries);
                return Data;
            }

            private string getEncapsulated(string val)
            {
                if (RgxEncapsulated.IsMatch(val))
                {
                    val = val.Substring(1, val.Length - 2);
                }
                return val;
            }

            private string getDiffMarker(string section)
            {
                return string.Format(@"---@@@SECTION::{0}@@@", section);
            }

            private string getSectionName(string line)
            {
                return line.Trim().TrimStart(new Char[] { '[' }).TrimEnd(new Char[] { ']' });
            }

            private bool isSingleValue(string line)
            {
                return line.StartsWith("=");
            }

            private bool isKeyValuePair(string line)
            {
                return RgxKeyValuePair.IsMatch(line);
            }

            private bool isSection(string line)
            {
                return RgxSection.IsMatch(line);
            }
        }
    }
}
