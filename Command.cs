using System;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.IO;
using System.Linq;
using System.Collections.Generic;

namespace SyncBox{

 public class Command {
  private string cmd;
  private string CurrentState;

  private static Thread T;
  private static bool Killed = false;
  private static int Percenttotal = 0;
  private static string LastSyncedFile = "";

  private class CmdInfo {
   public string cmd { get; }

   public CmdInfo(string cmd) {
    this.cmd = cmd;
   }
  }

  public Command(string cmd) {
   this.cmd = cmd;
  }

  public string Run(HttpListenerRequest req) {
   Config Cfg = Config.GetConfig();
   string cmd2, path, folder;
   switch (cmd) {
    case "deletefolder":
     cmd2 = "ssh -o ConnectTimeout=10 {0}@{1} \"rm -rf {2}\" 2>error.txt;exit 0";
     folder = (Cfg.ServerPath + req.QueryString["folder"]).Replace(" ", "\\ ");
     cmd2 = string.Format(cmd2, Cfg.SshUser, Cfg.Host, folder);
     return CommandWaitFor(cmd2);
    case "download":
     if (T != null || Killed) return "Another command is running";
     path = req.QueryString["path"];
     cmd2 = "rsync -ahvuEPi --progress --exclude \'**._sync_**\' --exclude \'lost+found\' --stats \"{0}@{1}:{2}\" \"{3}\" 1>out.txt 2>error.txt;exit 0";
     cmd2 = string.Format(cmd2, Cfg.SshUser, Cfg.Host, (Cfg.ServerPath + path).Replace(" ", "\\ "), (Cfg.ClientPath + path));
     T = new Thread(new ParameterizedThreadStart(ThreadProc));
     T.Start(new CmdInfo(cmd2));
     return "OK";
    case "upload":
     if (T != null || Killed) return "Another command is running";
     path = req.QueryString["path"];
     cmd2 = "rsync -ahvuEPi --progress --exclude \'**._sync_**\' --exclude \'lost+found\' --stats \"{0}\" \"{1}@{2}:{3}\" 1>out.txt 2>error.txt;exit 0";
     cmd2 = string.Format(cmd2, (Cfg.ClientPath + path), Cfg.SshUser, Cfg.Host, (Cfg.ServerPath + path).Replace(" ", "\\ "));
     T = new Thread(new ParameterizedThreadStart(ThreadProc));
     T.Start(new CmdInfo(cmd2));
     return "OK";
    case "getrsyncresult":
     return CommandWaitFor("tail -n 15 out.txt");
    case "status":
     return GetStatus();
    case "getlastrsyncdetails":
     return GetLastRsyncDetails();
    case "ls":
     path = req.QueryString["path"];
     cmd2 = "ssh -o ConnectTimeout=20 {0}@{1} bash -s < dols.sh '{2}' 2>error.txt;exit 0";
     cmd2 = string.Format(cmd2, Cfg.SshUser, Cfg.Host,
      (Cfg.ServerPath + path).Replace("'", "\\'").Replace(" ", "\\ ").Replace(")","\\)").Replace("(","\\("));
     return CommandWaitFor(cmd2);
    case "numfilesinfolder":
     folder = req.QueryString["folder"];
     cmd2 = "ssh -o ConnectTimeout=20 {0}@{1} bash -s < dirsize.sh '{2}' 2>error.txt;exit 0";
     cmd2 = string.Format(cmd2, Cfg.SshUser, Cfg.Host, (Cfg.ServerPath + folder).Replace("'", "\\'").Replace(" ", "\\ "));
     return CommandWaitFor(cmd2);
    case "geterrorlog":
     if (File.Exists("error.txt")) return File.ReadAllText("error.txt");
     else return "";
    default:
     return "Unkown command '" + cmd + "'!";
   }
  }

  protected string GetLastRsyncDetails() {
   //Na een sync wil ik een lijst van CREATED files
   // >f+ File create on client
   // cd+ Directory create (on server or client) 
   // <f+ File create on server
   List<string> FsDownloaded = new List<string>();
   List<string> DCreatedClient = new List<string>();
   List<string> DCreatedServer = new List<string>();
   List<string> FsUploaded = new List<string>();
   List<string> mem=File.ReadAllLines("out.txt").ToList();
   int ixS = mem.FindLastIndex(it=>it.IndexOf("sending incremental file list")==0);
   int ixR = mem.FindLastIndex(it=>it.IndexOf("receiving incremental file list")==0);
   if (ixR == 0 || ixS==0) {
    for (int i = 0; i < mem.Count - 1; i++) {
     if (mem[i].Length > 11) {
      if (mem[i].StartsWith(">f+")) FsDownloaded.Add(mem[i].Substring(12));
      else if (ixR == 0 && mem[i].StartsWith("cd+")) DCreatedClient.Add(mem[i].Substring(12));
      else if (ixS == 0 && mem[i].StartsWith("cd+")) DCreatedServer.Add(mem[i].Substring(12));
      else if (mem[i].StartsWith("<f+")) FsUploaded.Add(mem[i].Substring(12));
     }
    }
   }
   // Return 4 arrays seperated by a funny sepator for easy splitting in JavaScript 
   string result = Utils.ListToJSArray(FsDownloaded)+"$$$|$$$"+
    Utils.ListToJSArray(DCreatedClient)+"$$$|$$$"+
    Utils.ListToJSArray(DCreatedServer)+"$$$|$$$"+
    Utils.ListToJSArray(FsUploaded);
   return result;
  }

  protected string GetLastLine() {  
   string ret=CommandWaitFor("tail -n 1 out.txt"); // Last line of output
   // #  16.99M 100%    4.66MB/s    0:00:03 (xfr#6, ir-chk=1058/1095)
   List<string> mem=File.ReadAllLines("out.txt").ToList();
   int ix = mem.FindLastIndex(it=>it.IndexOf("-chk=")>0);
   if(ix>=0) {
    string[] a = mem[ix].Split(new string[]{"=",")" },StringSplitOptions.RemoveEmptyEntries);
    if(a.Length>=2) {
     a=a[1].Split(new char[] {'/'}, StringSplitOptions.RemoveEmptyEntries);
     try {
      Percenttotal = (int) Math.Round((double) Convert.ToInt32(a[a.Length - 2]) /
       (double) Convert.ToInt32(a[a.Length - 1]) * 100.0);
     }
     catch (Exception) { Percenttotal = 0; }
    }
   }
   // Guess the last file
   // Find last line that does not start with spaces and starst with >f+++++++++ or something else
   ix = mem.FindLastIndex(it => !it.StartsWith("   ") && it.Length>=12 && it[11]==' ');
   if(ix>=0)
    LastSyncedFile = mem[ix];
   return ret;
  }
  
  protected string GetStatus() {
   string RunStatus,State = GetLastLine();
   // Strip everything before \r
   State = State.Split('\r').Last();
   string percent="", speed="", mb="";
   this.CurrentState = State;
   if(State.StartsWith("    ")) {
    // The line is a progress indicater line from rsync
    string[] t = State.Split(new char[]{' '},StringSplitOptions.RemoveEmptyEntries); 
    if(t.Length==4) {
     // '420.88M', '11%', '132.68MB/s', '0:00:24'
     percent = t[1].Substring(0,t[1].Length-1);
		   speed = t[2];
		   mb = t[0];
			 }
			}
			 
			if(T!=null && !T.IsAlive) {
			 T=null;
		 }
		 RunStatus="running";
		 if(Killed) T=null;
		 if(T==null) RunStatus="end";

   string ret = State+"<>"+speed+"<>"+Percenttotal+"<>"+LastSyncedFile+"<>"+mb+"<>"+RunStatus+"<>"+percent;
   Console.WriteLine(ret);
   return ret;
	 }
  
  private static void ThreadProc(object c) {
   DelLogFiles();
   CmdInfo cmd=(CmdInfo)c;
   StartRSync(cmd.cmd);
		}
		
		protected static void StartRSync(string cmd)  { 
		var proc = new Process {
    StartInfo = new ProcessStartInfo {
     FileName = "/bin/bash",
     Arguments  = "-c \""+cmd+"\"",	
     UseShellExecute = false,     
     CreateNoWindow = true
    }
   };
   proc.Start();
   proc.WaitForExit();
  }
   
  protected static string CommandWaitFor(string command)  {        
   command = command.Replace("\"","\"\"");
   var proc = new Process {
    StartInfo = new ProcessStartInfo {
     FileName = "/bin/bash",
     Arguments = "-c \""+ command + "\"",
     UseShellExecute = false,
     RedirectStandardOutput = true,
     CreateNoWindow = true
    }
   };
   proc.Start();
   proc.WaitForExit();
   string s = proc.StandardOutput.ReadToEnd();
   //if(!string.IsNullOrEmpty(s))
   //Console.WriteLine("Commandoutput="+s);
   return s;
  }

  private static void DelLogFiles() {
   try {
    File.Delete("out.txt");
    File.Delete("error.txt");
   }
   catch (Exception) { }
  }
 } 
}
