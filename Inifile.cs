using System;
using System.Collections;
using System.IO;

public class IniFile {
 private readonly string iniFilePath;
 private readonly Hashtable keyPairs = new Hashtable();

 public IniFile(string iniPath) {
  TextReader iniFile = null;
  string strLine = null, currentRoot = null;
  string[] keyPair = null;
  iniFilePath = iniPath;

  if (File.Exists(iniPath))
   try {
    iniFile = new StreamReader(iniPath);
    strLine = iniFile.ReadLine();
    while (strLine != null) {
     strLine = strLine.Trim();
     if (strLine != "") {
      if (strLine.StartsWith("[") && strLine.EndsWith("]")) {
       currentRoot = strLine.Substring(1, strLine.Length - 2);
      }
      else {
       keyPair = strLine.Split(new[] {'='}, 2);
       SectionPair sectionPair;
       string value = null;
       if (currentRoot == null)
        currentRoot = "ROOT";
       sectionPair.Section = currentRoot;
       sectionPair.Key = keyPair[0];
       if (keyPair.Length > 1)
        value = keyPair[1];
       keyPairs.Add(sectionPair, value);
      }
     }
     strLine = iniFile.ReadLine();
    }
   }
   catch (Exception ex) {
    throw ex;
   }
   finally {
    if (iniFile != null)
     iniFile.Close();
   }
  else
   throw new FileNotFoundException("Unable to locate " + iniPath);
 }
 
 public string GetSetting(string sectionName, string settingName) {
  SectionPair sectionPair;
  sectionPair.Section = sectionName;
  sectionPair.Key = settingName;

  return (string) keyPairs[sectionPair];
 }
 
 public string[] EnumSection(string sectionName) {
  var tmpArray = new ArrayList();
  foreach (SectionPair pair in keyPairs.Keys)
   if (pair.Section == sectionName)
    tmpArray.Add(pair.Key);
  return (string[]) tmpArray.ToArray(typeof(string));
 }
 
 public void AddSetting(string sectionName, string settingName, string settingValue) {
  SectionPair sectionPair;
  sectionPair.Section = sectionName;
  sectionPair.Key = settingName;
  if (keyPairs.ContainsKey(sectionPair))
   keyPairs.Remove(sectionPair);
  keyPairs.Add(sectionPair, settingValue);
 }
 
 public void AddSetting(string sectionName, string settingName) {
  AddSetting(sectionName, settingName, null);
 }
 
 public void DeleteSetting(string sectionName, string settingName) {
  SectionPair sectionPair;
  sectionPair.Section = sectionName;
  sectionPair.Key = settingName;
  if (keyPairs.ContainsKey(sectionPair))
   keyPairs.Remove(sectionPair);
 }

 public void SaveSettings() {
  var sections = new ArrayList();
  var tmpValue = "";
  var strToSave = "";
  foreach (SectionPair sectionPair in keyPairs.Keys)
   if (!sections.Contains(sectionPair.Section))
    sections.Add(sectionPair.Section);
  foreach (string section in sections) {
   strToSave += "[" + section + "]\r\n";
   foreach (SectionPair sectionPair in keyPairs.Keys)
    if (sectionPair.Section == section) {
     tmpValue = (string) keyPairs[sectionPair];
     if (tmpValue != null)
      tmpValue = "=" + tmpValue;
     strToSave += sectionPair.Key + tmpValue + "\r\n";
    }
   strToSave += "\r\n";
  }

  try {
   TextWriter tw = new StreamWriter(iniFilePath);
   tw.Write(strToSave);
   tw.Close();
  }
  catch (Exception ex) {
   throw ex;
  }
 }

 private struct SectionPair {
  public string Section;
  public string Key;
 }
}