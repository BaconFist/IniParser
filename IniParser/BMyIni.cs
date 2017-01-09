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

        public BMyIni(string data, string currentNamespace)
        {
            this.currentNamespace = currentNamespace;
            Data = (new Serializer()).deserialize(data, out diff);
        }

        public string getSerialized()
        {
            return (new Serializer()).serialize(Data, diff);
        }

        private string normalizeSection(string section)
        {
            return currentNamespace + "." + section;
        }
        
        public string read(string section, string key)
        {
            return (Data.ContainsKey(normalizeSection(section)) && Data[normalizeSection(section)].ContainsKey(key)) ? Data[normalizeSection(section)][key] : null;
        }

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

        public Dictionary<string, string> getSection(string section)
        {
            return Data.ContainsKey(normalizeSection(section)) ? Data[normalizeSection(section)] : null;
        }

        private class Serializer
        {
            private System.Text.RegularExpressions.Regex RgxKeyValuePair = new System.Text.RegularExpressions.Regex(@"^[^=]+[=][\S\s]*$");
            private System.Text.RegularExpressions.Regex RgxSection = new System.Text.RegularExpressions.Regex(@"^\[[^\]]+\]\s*$");
            private System.Text.RegularExpressions.Regex RgxEncapsulated = new System.Text.RegularExpressions.Regex(@"^""[\S\s]*""");
            private System.Text.RegularExpressions.Regex RgxDiffMarker = new System.Text.RegularExpressions.Regex(@"^--@@@SECTION::[^\[\]]+@@@$");

            public string serialize(Dictionary<string,Dictionary<string,string>> Data, string[] diff)
            {
                StringBuilder Buffer = new StringBuilder();
                foreach(string diffLine in diff)
                {
                    if (RgxDiffMarker.IsMatch(diffLine))
                    {
                        string section = diffLine.Substring(14, diffLine.Length - 17);
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
