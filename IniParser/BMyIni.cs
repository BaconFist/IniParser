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
            set {
                if( -1 == currentNamespace.IndexOfAny(new char[] { '[', ']' }))
                {
                    currentNamespace = value.Trim();
                }                
            }
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
        public string GetSerialized()
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
        public string Read(string section, string key)
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
        public bool Write(string section, string key, string value)
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
            private System.Text.RegularExpressions.Regex RgxDiffMarkerSection = new System.Text.RegularExpressions.Regex(@"^---@@@SECTION::([^\[\]]+)@@@$");
            private System.Text.RegularExpressions.Regex RgxDiffMarkerKey = new System.Text.RegularExpressions.Regex(@"^---@@@KEY::\[([^\[\]]+)\]([^=]+)@@@$");

            const string LINEBREAK = "\r\n";

            public string serialize(Dictionary<string,Dictionary<string,string>> Data, string[] diff)
            {
                //throw new NotImplementedException();
                List<string> Buffer = new List<string>();
                string section = null;
                foreach(string line in diff)
                {
                    if (RgxDiffMarkerSection.IsMatch(line))
                    {
                        string buff_section = RgxDiffMarkerSection.Match(line).Groups[1].Value;
                        if (section != null && !buff_section.Equals(section))
                        {
                            if(Data.ContainsKey(section) && Data[section].Count > 0)
                            {
                                foreach(KeyValuePair<string, string> key in Data[section])
                                {
                                    Buffer.Add(string.Format(@"{0}={1}", key.Key, encapsulate(key.Value)));
                                }
                                Data.Remove(section);
                            }
                        }
                        section = buff_section;
                        if (Data.ContainsKey(section))
                        {
                            Buffer.Add(string.Format(@"[{0}]", section));
                        }
                    } else if (RgxDiffMarkerKey.IsMatch(line))
                    {
                        string tmp_section = RgxDiffMarkerKey.Match(line).Groups[1].Value;
                        string key = RgxDiffMarkerKey.Match(line).Groups[2].Value;

                        if (tmp_section.Equals(section) && Data.ContainsKey(tmp_section) && Data[tmp_section].ContainsKey(key))
                        {
                            string value = Data[tmp_section][key];
                            Buffer.Add(string.Format(@"{0}={1}", key, encapsulate(value)));
                            Data[tmp_section].Remove(key);
                            if(Data[tmp_section].Count <= 0)
                            {
                                Data.Remove(tmp_section);
                            }
                        }
                    } else
                    {
                        Buffer.Add(line);
                    }
                }  
                        
                foreach(KeyValuePair<string, Dictionary<string,string>> Section in Data)
                {
                    Buffer.Add(string.Format(@"[{0}]", Section.Key));
                    foreach (KeyValuePair<string,string> kvp in Section.Value)
                    {
                        Buffer.Add(string.Format(@"{0}=""{1}""", kvp.Key, kvp.Value));
                    }
                }
                return string.Join(LINEBREAK, Buffer.ToArray());
            }

            public Dictionary<string, Dictionary<string, string>> deserialize(string serialized, out string[] diff)
            {
                string[] linesSource = (serialized.Trim().Length == 0)?new string[] { } : serialized.Split(new string[] { LINEBREAK }, StringSplitOptions.None);
                List<string> serializedBuffer = new List<string>();
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
                        serializedBuffer.Add(getDiffMarkerSection(section));
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
                        serializedBuffer.Add(getDiffMarkerKey(section,key));
                    }
                    else if (section != null 
                        && Data.ContainsKey(section) 
                        && key != null 
                        && Data[section].ContainsKey(key)
                        && isSingleValue(currentLine))
                    {
                        //add line to last key
                        string val = getEncapsulated(currentLine.Trim().Substring(1));
                        Data[section][key] = Data[section][key] + LINEBREAK + val;
                    }
                    else
                    {
                        serializedBuffer.Add(currentLine);
                    }
                }
                diff = serializedBuffer.ToArray();
                return Data;
            }

            private string encapsulate(string raw)
            {
                if(raw.StartsWith(" ") || raw.EndsWith(" "))
                {
                    return "\"" + raw + "\"";
                } else
                {
                    return raw;
                }
            }

            private string getEncapsulated(string val)
            {
                if (RgxEncapsulated.IsMatch(val))
                {
                    val = val.Substring(1, val.Length - 2);
                }
                return val;
            }

            private string getDiffMarkerSection(string section)
            {
                return string.Format(@"---@@@SECTION::{0}@@@", section);
            }
            private string getDiffMarkerKey(string section, string key)
            {
                return string.Format(@"---@@@KEY::[{0}]{1}@@@", section, key);
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
