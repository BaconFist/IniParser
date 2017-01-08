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
            public string serialize(Dictionary<string, Dictionary<string, string>> Data)
            {
                StringBuilder SerializedData = new StringBuilder();
                foreach (KeyValuePair<string, Dictionary<string, string>> Section in Data)
                {
                    if (Section.Value.Count > 0)
                    {
                        SerializedData.AppendLine(string.Format(@"[{0}]", Section.Key));
                        foreach (KeyValuePair<string, string> keyValueBuff in Section.Value)
                        {
                            if (keyValueBuff.Value.EndsWith(" "))
                            {
                                SerializedData.AppendLine(string.Format(@"{0}=""{1}""", keyValueBuff.Key, keyValueBuff.Value));
                            }
                            else
                            {
                                SerializedData.AppendLine(string.Format(@"{0}={1}", keyValueBuff.Key, keyValueBuff.Value.Trim()));
                            }
                        }
                    }
                }
                return SerializedData.ToString();
            }

            public Dictionary<string, Dictionary<string, string>> deserialize(string[] sourceRaw)
            {
                Dictionary<string, Dictionary<string, string>> DeserialzedData = new Dictionary<string, Dictionary<string, string>>();
                string currentSection = null;
                foreach (string line in sourceRaw)
                {
                    if (isComment(line))
                    {
                        continue;
                    }
                    else if (isSection(line))
                    {
                        currentSection = getSectionName(line);
                    }
                    else if (currentSection != null && isKeyValuePair(line))
                    {
                        string keyBuffer = "";
                        string valueBuffer = "";
                        if (TryGetKeyValuePair(line, out keyBuffer, out valueBuffer))
                        {
                            if (!DeserialzedData.ContainsKey(currentSection))
                            {
                                DeserialzedData.Add(currentSection, new Dictionary<string, string>());
                            }
                            if (DeserialzedData[currentSection].ContainsKey(keyBuffer))
                            {
                                DeserialzedData[currentSection][keyBuffer] = DeserialzedData[currentSection][keyBuffer] + "\n" + valueBuffer;
                            }
                            else
                            {
                                DeserialzedData[currentSection].Add(keyBuffer, valueBuffer);
                            }
                        }
                    }
                }

                return DeserialzedData;
            }

            private bool TryGetKeyValuePair(string source, out string key, out string value)
            {
                KeyValuePair<string, string> result = new KeyValuePair<string, string>(null, null);
                string slug = source.Trim();
                string[] data = slug.Split(new Char[] { '=' }, 2);
                key = null;
                value = null;
                if (data.Length == 2)
                {
                    key = data[0];
                    value = data[1].Trim();
                    if (value.StartsWith("\""))
                    {
                        int lastIndexQuote = value.LastIndexOf('"');
                        if (lastIndexQuote != -1)
                        {
                            value = value.Substring(1, lastIndexQuote-1);
                        }
                    }
                    return true;
                }
                return false;
            }

            private string getSectionName(string value)
            {
                return value.Trim().TrimStart(new Char[] { '[' }).TrimEnd(new Char[] { ']' });
            }

            private bool isKeyValuePair(string value)
            {
                return (new System.Text.RegularExpressions.Regex(@"^[^=]+=[\S\s]*$")).IsMatch(value);
            }

            private bool isComment(string value)
            {
                return (new System.Text.RegularExpressions.Regex(@"^\s*;")).IsMatch(value);
            }

            private bool isSection(string value)
            {
                return (new System.Text.RegularExpressions.Regex(@"^\s*\[[^\]]+\]\s*$")).IsMatch(value);
            }
        }
    }
}
