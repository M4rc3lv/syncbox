using System;
using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Diagnostics;
using System.IO;

namespace SyncBox {
 class MainClass {
  public static void Main(string[] args) {
   // Create local webserver (that serves as our UI)
   HttpListener http = new HttpListener();
   http.Prefixes.Add("http://localhost:8082/");
   http.Prefixes.Add("http://127.0.0.1:8082/");
   http.Start();

   bool MustStop = false;
   if (FirstTimeUser())
    Process.Start("xdg-open", "http://localhost:8082/config.html");
   else
    Process.Start("xdg-open", "http://localhost:8082");
   while (!MustStop) {
    HttpListenerContext context = http.GetContext(); // Blocks
    HttpListenerRequest request = context.Request;
    HttpListenerResponse response = context.Response;
    MainClass.GetHttpResponse(request, response);
   }

   http.Stop();
  }

  protected static byte[] ParseTemplate(byte[] html) {
   return Config.GetConfig().Replace(html);
  }

  protected static void StaticFile(string f, HttpListenerResponse response) {
   try {
    if (File.Exists("pages" + f)) {
     switch (Path.GetExtension("pages" + f).ToLower()) {
      case ".html":
       response.ContentType = "text/html"; break;
      case ".css":
       response.ContentType = "text/css"; break;
      case ".js":
       response.ContentType = "text/javascript"; break;
      default:
       response.ContentType = "application/octet-stream"; break;
     }

     byte[] content = File.ReadAllBytes("pages" + f);
     if (Path.GetExtension("pages" + f).ToLower() == ".html") content = ParseTemplate(content);
     response.ContentLength64 = content.Length;
     response.OutputStream.Write(content, 0, content.Length);
     response.OutputStream.Close();
    }
    else
     response.StatusCode = (int) HttpStatusCode.NotFound;

    return; //File.ReadAllText("pages"+f);
   }
   catch (Exception e) {
    WriteOutputString(e.ToString(), response);
   }
  }

  protected static void WriteOutputString(string s, HttpListenerResponse response) {
   response.ContentType = "text/plain";
   byte[] buf = Encoding.UTF8.GetBytes(s);
   response.ContentLength64 = buf.Length;
   System.IO.Stream output = response.OutputStream;
   output.Write(buf, 0, buf.Length);
   output.Close();
  }

  protected static void GetHttpResponse(HttpListenerRequest req, HttpListenerResponse response) {
   if (!string.IsNullOrEmpty(req.QueryString["cmd"]))
    WriteOutputString(new Command(req.QueryString["cmd"]).Run(req), response);
   else {
    if (req.RawUrl == "/")
     StaticFile("/index.html", response);
    else if (string.IsNullOrEmpty(req.RawUrl) || req.RawUrl.Length <= 1)
     StaticFile("Huh?", response);
    else if (req.RawUrl == "/storeconfig")
     StoreConfig(req, response);
    else
     StaticFile(req.RawUrl, response);
   }
  }
  
  private static void StoreConfig(HttpListenerRequest req, HttpListenerResponse response) {
   if (req.RawUrl == "/storeconfig") {
    // Form data 
    byte[] buf=new byte[req.ContentLength64];
    req.InputStream.Read(buf, 0, (int)req.ContentLength64);
    string s = Encoding.UTF8.GetString(buf);
    var a=s.Split('&');
    Config Cfg = Config.GetConfig();
    for (int i = 0; i < a.Length; i++) {
     string[] v = a[i].Split('=');
     if (v.Length == 2) {
      string Key = WebUtility.UrlDecode(v[0]), Value=WebUtility.UrlDecode(v[1]);
      switch (Key) {
       case "host": Cfg.Host = Value; break;
       case "serverpath": Cfg.ServerPath = Value; break;
       case "clientpath": Cfg.ClientPath = Value; break;
       case "sshuser": Cfg.SshUser = Value; break;
      }
     }
    }
    Cfg.StoreConfig();
    StaticFile("/index.html", response);
   }
  }

  private static bool FirstTimeUser() {
   // If configuraton file doesn't exist assume first time user
   return !Config.ExistsConfigFile();
  }

 }
}
