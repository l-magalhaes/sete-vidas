// Compara duas versões de uma cena (.unity): o que sumiu, mudou de lugar/escala/ativo ou foi acrescentado (por nome e
// pai, o mais perto). Usado por comparar_floresta.ps1 (a cena do último commit = o que o gerador fez × a cena salva). C# 5.
using System; using System.Collections.Generic; using System.IO; using System.Text; using System.Text.RegularExpressions; using System.Globalization; using System.Linq;
public static class SceneCmp {
  public class T { public long Id, Go; public double X, Y, SX=1, SY=1; public long Father; public string Name; public bool Active=true; }
  static string Clean(string s) {
    s = s.Trim();
    if (s.Length >= 2 && s[0] == '"' && s[s.Length - 1] == '"') {
      s = s.Substring(1, s.Length - 2);
      s = Regex.Replace(s, @"\\x([0-9A-Fa-f]{2})", m => ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString());
      s = Regex.Replace(s, @"\\u([0-9A-Fa-f]{4})", m => ((char)Convert.ToInt32(m.Groups[1].Value, 16)).ToString());
      s = s.Replace("\\\"", "\"");
    } else s = s.Trim('\'');
    return s;
  }
  static double D(string s) { return double.Parse(s, CultureInfo.InvariantCulture); }
  static string F(double v) { return v.ToString("0.##", CultureInfo.InvariantCulture); }
  public class Scene { public Dictionary<long,T> tr = new Dictionary<long,T>(); public Dictionary<long,string> goName = new Dictionary<long,string>(); public Dictionary<long,bool> goActive = new Dictionary<long,bool>();
    public string NameOf(long id){ T t=tr[id]; if(t.Name!=null) return t.Name; string n; return goName.TryGetValue(t.Go,out n)?n:"?"; }
    public double[] World(long id){ T t=tr[id]; double x=t.X,y=t.Y; long f=t.Father; int g=0; while(f!=0&&tr.ContainsKey(f)&&g++<30){x+=tr[f].X;y+=tr[f].Y;f=tr[f].Father;} return new[]{x,y}; }
    public string Path(long id){ var parts=new List<string>(); long f=tr[id].Father; int g=0; while(f!=0&&tr.ContainsKey(f)&&g++<30){parts.Insert(0,NameOf(f)); f=tr[f].Father;} return string.Join("/",parts); }
  }
  public static Scene Load(string path){
    var s=new Scene(); var text=File.ReadAllText(path); var blocks=Regex.Split(text,@"\n--- ");
    var prefabs=new Dictionary<long,Dictionary<string,string>>(); var prefabParent=new Dictionary<long,long>(); var strippedTr=new Dictionary<long,long>();
    foreach(var b in blocks){
      var head=Regex.Match(b,@"^!u!(\d+) &(-?\d+)( stripped)?"); if(!head.Success) continue;
      int cls=int.Parse(head.Groups[1].Value); long id=long.Parse(head.Groups[2].Value); bool stripped=head.Groups[3].Success;
      if(cls==1&&!stripped){ var m=Regex.Match(b,@"m_Name: (.*)"); if(m.Success) s.goName[id]=Clean(m.Groups[1].Value); var a=Regex.Match(b,@"m_IsActive: (\d)"); if(a.Success) s.goActive[id]=a.Groups[1].Value=="1"; }
      else if(cls==4){
        if(stripped){ var p=Regex.Match(b,@"m_PrefabInstance: \{fileID: (-?\d+)\}"); if(p.Success) strippedTr[id]=long.Parse(p.Groups[1].Value); continue; }
        var t=new T(); t.Id=id; t.Go=long.Parse(Regex.Match(b,@"m_GameObject: \{fileID: (-?\d+)\}").Groups[1].Value);
        var pos=Regex.Match(b,@"m_LocalPosition: \{x: ([^,]+), y: ([^,]+),"); t.X=D(pos.Groups[1].Value); t.Y=D(pos.Groups[2].Value);
        var sc=Regex.Match(b,@"m_LocalScale: \{x: ([^,]+), y: ([^,]+),"); if(sc.Success){t.SX=D(sc.Groups[1].Value);t.SY=D(sc.Groups[2].Value);}
        var f=Regex.Match(b,@"m_Father: \{fileID: (-?\d+)\}"); t.Father=f.Success?long.Parse(f.Groups[1].Value):0;
        s.tr[id]=t;
      } else if(cls==1001){
        var mods=new Dictionary<string,string>();
        foreach(Match mm in Regex.Matches(b,@"propertyPath: ([^\n]+)\n\s+value: ([^\n]*)")){ string k=mm.Groups[1].Value.Trim(); if(!mods.ContainsKey(k)) mods[k]=mm.Groups[2].Value.Trim(); }
        prefabs[id]=mods; var tp=Regex.Match(b,@"m_TransformParent: \{fileID: (-?\d+)\}"); prefabParent[id]=tp.Success?long.Parse(tp.Groups[1].Value):0;
      }
    }
    foreach(var kv in strippedTr){ Dictionary<string,string> mods; if(!prefabs.TryGetValue(kv.Value,out mods)) continue; var t=new T(); t.Id=kv.Key; string v; t.X=mods.TryGetValue("m_LocalPosition.x",out v)?D(v):0; t.Y=mods.TryGetValue("m_LocalPosition.y",out v)?D(v):0; t.Father=prefabParent[kv.Value]; t.Name=mods.TryGetValue("m_Name",out v)?Clean(v):"(prefab)"; s.tr[kv.Key]=t; }
    foreach(var t in s.tr.Values){ bool a; if(s.goActive.TryGetValue(t.Go,out a)) t.Active=a; }
    return s;
  }
  public static string Run(string before, string after){
    var A=Load(before); var B=Load(after); var sb=new StringBuilder();
    var keyB=new Dictionary<string,List<long>>();
    foreach(var id in B.tr.Keys){ string k=B.Path(id)+"|"+B.NameOf(id); List<long> l; if(!keyB.TryGetValue(k,out l)) keyB[k]=l=new List<long>(); l.Add(id); }
    var used=new HashSet<long>();
    // pass 1: exact positions first
    var pending=new List<long>();
    var match=new Dictionary<long,long>();
    foreach(var id in A.tr.Keys){ string k=A.Path(id)+"|"+A.NameOf(id); List<long> l; var wa=A.World(id); long best=0; if(keyB.TryGetValue(k,out l)) foreach(var b in l){ if(used.Contains(b)) continue; var wb=B.World(b); if(Math.Abs(wa[0]-wb[0])+Math.Abs(wa[1]-wb[1])<0.01){best=b;break;} } if(best!=0){used.Add(best); match[id]=best;} else pending.Add(id); }
    foreach(var id in pending){ string k=A.Path(id)+"|"+A.NameOf(id); List<long> l; var wa=A.World(id); long best=0; double bd=1e9; if(keyB.TryGetValue(k,out l)) foreach(var b in l){ if(used.Contains(b)) continue; var wb=B.World(b); double d=Math.Abs(wa[0]-wb[0])+Math.Abs(wa[1]-wb[1]); if(d<bd){bd=d;best=b;} }
      if(best==0){ sb.AppendLine("SUMIU  "+A.Path(id)+" / "+A.NameOf(id)+" ("+F(wa[0])+", "+F(wa[1])+")"); continue; }
      used.Add(best); match[id]=best; }
    foreach(var kv in match){ var a=A.tr[kv.Key]; var b=B.tr[kv.Value]; var wa=A.World(kv.Key); var wb=B.World(kv.Value);
      bool mv=Math.Abs(wa[0]-wb[0])>0.01||Math.Abs(wa[1]-wb[1])>0.01; bool scl=Math.Abs(a.SX-b.SX)>0.001||Math.Abs(a.SY-b.SY)>0.001; bool act=a.Active!=b.Active;
      if(mv||scl||act) sb.AppendLine("MUDOU  "+B.Path(kv.Value)+" / "+B.NameOf(kv.Value)+" ("+F(wa[0])+", "+F(wa[1])+") -> ("+F(wb[0])+", "+F(wb[1])+")"+(scl?" escala "+F(a.SX)+"x"+F(a.SY)+" -> "+F(b.SX)+"x"+F(b.SY):"")+(act?" ativo "+a.Active+" -> "+b.Active:"")); }
    foreach(var id in B.tr.Keys) if(!used.Contains(id)){ var w=B.World(id); var t=B.tr[id]; sb.AppendLine("NOVO   "+B.Path(id)+" / "+B.NameOf(id)+" ("+F(w[0])+", "+F(w[1])+") escala "+F(t.SX)+"x"+F(t.SY)); }
    return sb.ToString();
  }
}
