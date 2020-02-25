using System.Collections.Generic;

namespace SyncBox {
 public class Utils {
  
  public static string ListToJSArray(List<string> L) {
   string ret = "[";
   for (int i = 0; i < L.Count; i++) {
    if (ret != "[") ret += ",";
    ret += "\""+L[i].Replace("\"","&quot;")+"\"";
   }
   return ret + "]";
  }
  
 }
}