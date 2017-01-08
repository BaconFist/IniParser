using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IniParser
{
    public class BMyCustomData
    {
        private Dictionary<string, Dictionary<string, string>> Data;

        public BMyCustomData(string data)
        {
            Data = (new Serializer()).deserialize(data.Split(new Char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries));
        }

        public string getSerialized()
        {
            return (new Serializer()).serialize(Data);
        }

        public int CountSections()
        {
            return Data.Count;
        }

        public int CountValues(string section)
        {
            if (hasSection(section))
            {
                return Data[section].Count;
            }
            else
            {
                return 0;
            }
        }

        public bool hasSection(string section)
        {
            return Data.ContainsKey(section);
        }

        public bool hasValue(string section, string key)
        {
            return hasSection(section) && Data[section].ContainsKey(key);
        }

        public string getValue(string section, string key)
        {
            return hasValue(section, key) ? Data[section][key] : null;
        }

        public bool addValue(string section, string key, string value)
        {
            if (!addSection(section))
            {
                return false;
            }
            if (hasValue(section, key))
            {
                Data[section][key] = value;
            }
            else
            {
                Data[section].Add(key, value);
            }

            return hasValue(section, key);
        }

        public bool addSection(string section)
        {
            if (-1 == section.IndexOfAny(new Char[] { '[', ']' }))
            {
                if (!hasSection(section))
                {
                    Data.Add(section, new Dictionary<string, string>());
                }
            }

            return hasSection(section);
        }

        public Dictionary<string, string> getSection(string section)
        {
            return hasSection(section) ? Data[section] : null;
        }

        private class Serializer
        {
            private System.Text.RegularExpressions.Regex RgxKeyValuePair = new System.Text.RegularExpressions.Regex(@"^[^=]+[=][\S\s]*$");
            private System.Text.RegularExpressions.Regex RgxSection = new System.Text.RegularExpressions.Regex(@"^\[[^\]]+\]\s*$");
            private System.Text.RegularExpressions.Regex RgxEncapsulated = new System.Text.RegularExpressions.Regex(@"^""[\S\s]*""");

            public string serialize(Dictionary<string, Dictionary<string, string>> Data)
            {
                StringBuilder Buffer = new StringBuilder();
                foreach(KeyValuePair<string, Dictionary<string,string>> Section in Data)
                {
                    if (Section.Value.Count > 0)
                    {
                        Buffer.AppendLine("["+Section.Key+"]");
                        foreach (KeyValuePair<string, string> KVP in Section.Value)
                        {
                            string[] lines = KVP.Value.Replace("\r\n", "\n").Split(new Char[] { '\n' });
                            if(lines.Length > 1)
                            {
                                for(int i = 0; i < lines.Length; i++)
                                {
                                    if (i == 0)
                                    {
                                        Buffer.AppendLine(KVP.Key + "=" + serializedValue(lines[i]));
                                    } else
                                    {
                                        Buffer.AppendLine("=" + serializedValue(lines[i]));
                                    }
                                }
                            } else
                            {
                                Buffer.AppendLine(KVP.Key + "=" + serializedValue(KVP.Value));
                            }
                        }
                    }
                }
                return Buffer.ToString();
            }

            public string serializedValue(string value)
            {
                if(value.StartsWith(" ") || value.EndsWith(" "))
                {
                    return "\"" + value + "\"";
                } else
                {
                    return value;
                }
            }

            public Dictionary<string, Dictionary<string, string>> deserialize(string[] sourceRaw)
            {
                Dictionary<string, Dictionary<string, string>> Data = new Dictionary<string, Dictionary<string, string>>();
                string currentSection = null;
                string key = null;
                foreach (string line in sourceRaw)
                {
                    if (isSection(line))
                    {
                        currentSection = getSectionName(line);
                        if (!Data.ContainsKey(currentSection))
                        {
                            Data.Add(currentSection, new Dictionary<string, string>());
                        }
                        key = null;
                    } else if(currentSection != null) {
                        string val = null;
                        if (isKeyValuePair(line))
                        {
                            int i_seperator = line.IndexOf('=');
                            if(i_seperator != -1)
                            {
                                key = line.Substring(0, i_seperator).Trim();
                                val = line.Substring(i_seperator + 1).Trim();
                                if (RgxEncapsulated.IsMatch(val))
                                {
                                    val = val.Substring(1, val.Length - 2);
                                }
                                if (!Data[currentSection].ContainsKey(key))
                                {
                                    Data[currentSection].Add(key, val);
                                } else
                                {
                                    Data[currentSection][key] = val;
                                }
                                
                            }
                        } else if (key != null && isSingleValue(line))
                        {
                            val = line.Substring(1).Trim();
                            if (RgxEncapsulated.IsMatch(val))
                            {
                                val = val.Substring(1, val.Length - 2);
                            }
                            if (Data[currentSection].ContainsKey(key))
                            {
                                Data[currentSection][key] = Data[currentSection][key] + "\n" + val;
                            }
                        }
                    }
                }
                return Data;
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

            private bool isComment(string line)
            {
                return line.StartsWith(";");
            }

            private bool isSection(string line)
            {
                return RgxSection.IsMatch(line);
            }
        }
    }
}
