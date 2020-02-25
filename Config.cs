using System;
using System.Configuration;
using System.Text;
using System.IO;

namespace SyncBox {

 public class Config {
  public string Host { get; set; }
  public string SshUser { get; set;}
  public string ServerPath { get;set;}
  public string ClientPath { get;set;}
  
  public static Config GetConfig() {
   Config ret = new Config(System.Environment.GetEnvironmentVariable("HOME") + "/.config/syncbox/syncbox.config");
   return ret;
  }

  public static bool ExistsConfigFile() {
   return File.Exists(System.Environment.GetEnvironmentVariable("HOME") + "/.config/syncbox/syncbox.config");
  }
  
  private Config(string inifile) {
   FileInfo fi = new FileInfo(inifile);
   if(!fi.Directory.Exists) fi.Directory.Create();
   DirectoryInfo di = new DirectoryInfo(fi.Directory.FullName);
   if (!fi.Exists) fi.Create().Dispose();
   Ini = new IniFile(inifile);

   Host = Ini.GetSetting("Server", "Host");
   SshUser = Ini.GetSetting("Server", "SshUser");
   ServerPath = Ini.GetSetting("Server", "ServerPath");
   Host = Ini.GetSetting("Server", "Host");
   ClientPath = Ini.GetSetting("Client", "ClientPath");
  } 
  private IniFile Ini; 
  
  public byte[] Replace(byte[] html) {
   string ret = Encoding.UTF8.GetString(html);
   ret = ret.Replace("${serverpath}",ServerPath);
   ret = ret.Replace("${host}",Host);
   ret = ret.Replace("${sshuser}",SshUser);
   ret = ret.Replace("${clientpath}",ClientPath);
   return Encoding.UTF8.GetBytes(ret);
  }

  public void StoreConfig() {
   Ini.AddSetting("Server", "Host", Host);
   Ini.AddSetting("Server", "SshUser", SshUser);
   Ini.AddSetting("Server", "ServerPath", ServerPath);
   Ini.AddSetting("Client", "ClientPath", ClientPath);
   Ini.SaveSettings();
  }
  
  private Config() {
   Host = Ini.GetSetting("Server", "Host");
   SshUser = Ini.GetSetting("Server", "SshUser");
   ServerPath = Ini.GetSetting("Server", "ServerPath");;
   ClientPath  = Ini.GetSetting("Server", "ClientPath");;
  }
 }
 
}
