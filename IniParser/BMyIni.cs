﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IniParser
{
    public class BMyIni
    {
        public Dictionary<string, Dictionary<string, string>> Data;
        private string[] MarkedIniData = null;

        /// <summary>
        /// create values from INI Formatted data
        /// </summary>
        /// <param name="ini">serialized data with INI sutff in it.</param>
        public BMyIni(string ini)
        {
            Data = (new Serializer()).deserialize(ini, out MarkedIniData);
        }

        /// <summary>
        /// serialized data with setting in INI-Format (includes former comments and all unknown stuff that could be parsed in the first place
        /// </summary>
        /// <returns>string</returns>
        public string GetSerialized()
        {
            return (new Serializer()).serialize(Data, MarkedIniData);
        }
        
        /// <summary>
        /// get value for given properties
        /// </summary>
        /// <param name="section">section without namespace</param>
        /// <param name="key"></param>
        /// <returns>(string)value or null if not found</returns>
        public string Read(string section, string key)
        {
            return (Data.ContainsKey(section) && Data[section].ContainsKey(key)) ? Data[section][key] : null;
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
            // section must not contain square brakets
            if(-1 != section.IndexOfAny(new Char[] { '[', ']' }))
            {
                return false;
            }

            if (!Data.ContainsKey(section))
            {
                Data.Add(section, new Dictionary<string, string>());
            }
            if (Data[section].ContainsKey(key))
            {
                Data[section][key] = value;
            }
            else
            {
                Data[section].Add(key, value);
            }

            return Data[section].ContainsKey(key);
        }

        /// <summary>
        /// remove key from a section
        /// </summary>
        /// <param name="section"></param>
        /// <param name="key"></param>
        /// <returns>bool on success</returns>
        public bool Remove(string section, string key)
        {
            // section must not contain square brakets
            if (-1 != section.IndexOfAny(new Char[] { '[', ']' }))
            {
                return false;
            }
            if(Data.ContainsKey(section) && Data[section].ContainsKey(key))
            {
                Data[section].Remove(key);
                return !Data[section].ContainsKey(key);
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
        public bool Remove(string section)
        {
            // section must not contain square brakets
            if (-1 != section.IndexOfAny(new Char[] { '[', ']' }))
            {
                return false;
            }
            if (Data.ContainsKey(section))
            {
                Data.Remove(section);
                return !Data.ContainsKey(section);
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
            return Data.ContainsKey(section) ? Data[section] : null;
        }

        private class Serializer
        {
            private System.Text.RegularExpressions.Regex RgxKeyValuePair = new System.Text.RegularExpressions.Regex(@"^[^=]+[=][\S\s]*$");
            private System.Text.RegularExpressions.Regex RgxSection = new System.Text.RegularExpressions.Regex(@"^\[[^\]]+\]\s*$");
            private System.Text.RegularExpressions.Regex RgxEncapsulated = new System.Text.RegularExpressions.Regex(@"^""[\S\s]*""");
            private System.Text.RegularExpressions.Regex RgxDiffMarkerSection = new System.Text.RegularExpressions.Regex(@"^---@@@SECTION::([^\[\]]+)@@@$");
            private System.Text.RegularExpressions.Regex RgxDiffMarkerKey = new System.Text.RegularExpressions.Regex(@"^---@@@KEY::\[([^\[\]]+)\]([^=]+)@@@$");


            //i'm so sick of this linebreak stuff. 
            const string LINEBREAK = "\r\n";

            public string serialize(Dictionary<string,Dictionary<string,string>> UnserializedData, string[] MarkedIniData)
            {
                //this is the new stuff
                List<string> SerializedDataBuffer = new List<string>();
                string currentSection = null;
                foreach(string originLine in MarkedIniData)
                {
                    if (RgxDiffMarkerSection.IsMatch(originLine))
                    {
                        string matchedSection = RgxDiffMarkerSection.Match(originLine).Groups[1].Value;
                        if (currentSection != null && !matchedSection.Equals(currentSection))
                        {
                            if(UnserializedData.ContainsKey(currentSection) && UnserializedData[currentSection].Count > 0)
                            {
                                SerializedDataBuffer.AddRange(GetSectionItems(UnserializedData[currentSection]));
                                UnserializedData.Remove(currentSection);
                            }
                        }
                        currentSection = matchedSection;
                        if (UnserializedData.ContainsKey(currentSection))
                        {
                            SerializedDataBuffer.Add(string.Format(@"[{0}]", currentSection));
                        }
                    } else if (RgxDiffMarkerKey.IsMatch(originLine))
                    {
                        System.Text.RegularExpressions.Match KeyMatch = RgxDiffMarkerKey.Match(originLine);
                        string matchedSection = KeyMatch.Groups[1].Value;
                        string matchedKey = KeyMatch.Groups[2].Value;

                        if (matchedSection.Equals(currentSection) && UnserializedData.ContainsKey(matchedSection) && UnserializedData[matchedSection].ContainsKey(matchedKey))
                        {
                            string value = UnserializedData[matchedSection][matchedKey];
                            SerializedDataBuffer.Add(string.Format(@"{0}={1}", matchedKey, encapsulate(value)));
                            UnserializedData[matchedSection].Remove(matchedKey);
                            if(UnserializedData[matchedSection].Count <= 0)
                            {
                                UnserializedData.Remove(matchedSection);
                            }
                        }
                    } else
                    {
                        SerializedDataBuffer.Add(originLine);
                    }
                }
                SerializedDataBuffer.AddRange(GetRemainingSections(UnserializedData));        
                
                return string.Join(LINEBREAK, SerializedDataBuffer.ToArray());
            }

            private List<string> GetSectionItems(Dictionary<string,string> Section)
            {
                List<string> SerializedDataBuffer = new List<string>();
                foreach (KeyValuePair<string, string> key in Section)
                {
                    string[] valueLines = key.Value.Split(new String[] { LINEBREAK }, StringSplitOptions.None);
                    SerializedDataBuffer.Add(string.Format(@"{0}={1}", key.Key, encapsulate(valueLines[0])));
                    if (valueLines.Length > 1)
                    {
                        //add as multiline value
                        for(int indexLine = 1; indexLine < valueLines.Length - 1; indexLine++)
                        {
                            SerializedDataBuffer.Add(string.Format(@"={1}", key.Key, encapsulate(valueLines[indexLine])));
                        }
                    }
                }
                return SerializedDataBuffer;
            }

            private List<string> GetRemainingSections(Dictionary<string, Dictionary<string, string>> UnserializedData)
            {
                List<string> SerializedDataBuffer = new List<string>();
                foreach (KeyValuePair<string, Dictionary<string, string>> Section in UnserializedData)
                {
                    SerializedDataBuffer.Add(string.Format(@"[{0}]", Section.Key));
                    foreach (KeyValuePair<string, string> iniItem in Section.Value)
                    {
                        SerializedDataBuffer.Add(string.Format(@"{0}=""{1}""", iniItem.Key, iniItem.Value));
                    }
                }
                return SerializedDataBuffer;
            }

            public Dictionary<string, Dictionary<string, string>> deserialize(string serializedIni, out string[] MarkedIniData)
            {
                string[] OriginIni = (serializedIni.Trim().Length == 0)?new string[] { } : serializedIni.Split(new string[] { LINEBREAK }, StringSplitOptions.None);
                List<string> serializedBuffer = new List<string>();
                string currentSection = null;
                string currentKey = null;
                Dictionary<string, Dictionary<string, string>> Data = new Dictionary<string, Dictionary<string, string>>();
                foreach(string currentLine in OriginIni)
                {
                    if (RgxSection.IsMatch(currentLine))
                    {
                        // prepare section
                        currentKey = null;
                        currentSection = currentLine.Trim().TrimStart(new Char[] { '[' }).TrimEnd(new Char[] { ']' });
                        if (!Data.ContainsKey(currentSection))
                        {
                            Data.Add(currentSection, new Dictionary<string, string>());
                        }
                        serializedBuffer.Add(string.Format(@"---@@@SECTION::{0}@@@", currentSection));
                    } else if (currentSection != null 
                        && Data.ContainsKey(currentSection) 
                        && RgxKeyValuePair.IsMatch(currentLine))
                    {
                        // add Key Value to Section
                        int indexOfEqualSign = currentLine.IndexOf('=');
                        currentKey = currentLine.Substring(0, indexOfEqualSign).Trim();

                        if (Data[currentSection].ContainsKey(currentKey))
                        {
                            Data[currentSection][currentKey] = readEncapsulated(currentLine.Substring(indexOfEqualSign + 1).Trim());
                        } else
                        {
                            Data[currentSection].Add(currentKey, readEncapsulated(currentLine.Substring(indexOfEqualSign + 1).Trim()));
                        }
                        serializedBuffer.Add(string.Format(@"---@@@KEY::[{0}]{1}@@@", currentSection, currentKey));
                    }
                    else if (currentSection != null 
                        && Data.ContainsKey(currentSection) 
                        && currentKey != null 
                        && Data[currentSection].ContainsKey(currentKey)
                        && currentLine.StartsWith("="))
                    {
                        //add line to last key
                        Data[currentSection][currentKey] = Data[currentSection][currentKey] + LINEBREAK + readEncapsulated(currentLine.Trim().Substring(1));
                    }
                    else
                    {
                        serializedBuffer.Add(currentLine);
                    }
                }
                MarkedIniData = serializedBuffer.ToArray();
                return Data;
            }

            private string encapsulate(string raw)
            {
                return (raw.StartsWith(" ") || raw.EndsWith(" "))?"\"" + raw + "\"":raw;
            }

            private string readEncapsulated(string encapsualtedValue)
            {
                return (RgxEncapsulated.IsMatch(encapsualtedValue)) ? encapsualtedValue.Substring(1, encapsualtedValue.Length - 2) : encapsualtedValue;
            }
        }
    }
}
