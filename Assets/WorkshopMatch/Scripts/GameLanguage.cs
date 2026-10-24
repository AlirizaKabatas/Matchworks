using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
namespace WorkshopMatch {
 internal static class GameLanguage {
  internal const string PreferenceKey="WM_Language";
  internal static bool English=>PlayerPrefs.GetInt(PreferenceKey,1)==1;
  [Serializable] internal sealed class Entry { public string source="",tr="",en=""; }
  [Serializable] private sealed class Catalog { public Entry[] entries=Array.Empty<Entry>(); }
  private static Dictionary<string,Entry> entries;
  internal static void Select(bool english) {PlayerPrefs.SetInt(PreferenceKey,english?1:0);PlayerPrefs.Save();}
  internal static string Key(string text) {
   var b=new StringBuilder();
   foreach(char c in (text??"").Replace('ı','i').Replace('İ','I').Normalize(NormalizationForm.FormD))
    if(CharUnicodeInfo.GetUnicodeCategory(c)!=UnicodeCategory.NonSpacingMark)b.Append(c);
   return Regex.Replace(b.ToString(),@"\s+"," ").Trim().ToUpperInvariant();
  }
  internal static string Text(string text) {
   if(string.IsNullOrEmpty(text))return text;
   if(entries==null) {
    var asset=Resources.Load<TextAsset>("WorkshopMatch/languages");
    entries=BuildLookup(asset==null?Array.Empty<Entry>():JsonUtility.FromJson<Catalog>(asset.text).entries);
   }
   return Translate(text,English,entries);
  }
  internal static Dictionary<string,Entry> BuildLookup(Entry[] rows) {
   var lookup=new Dictionary<string,Entry>();
   foreach(var row in rows)lookup[Key(row.source)]=row;
   // Captions may already have passed through localization before DrawUiLabel,
   // or be authored in Turkish. Resolve both languages, preserving source priority.
   var aliases=new Dictionary<string,Entry>();var ambiguous=new HashSet<string>();
   foreach(var row in rows)foreach(string caption in new[]{row.tr,row.en}) {
    string key=Key(caption);if(string.IsNullOrEmpty(key)||lookup.ContainsKey(key))continue;
    if(aliases.TryGetValue(key,out var prior)&&
       (Key(prior.tr)!=Key(row.tr)||Key(prior.en)!=Key(row.en)))ambiguous.Add(key);
    else aliases[key]=row;
   }
   foreach(var pair in aliases)if(!ambiguous.Contains(pair.Key))lookup[pair.Key]=pair.Value;
   return lookup;
  }
  internal static string Translate(string text,bool english,IReadOnlyDictionary<string,Entry> lookup) {
   if(string.IsNullOrEmpty(text))return text;
   if(lookup.TryGetValue(Key(text),out var item))return english?item.en:item.tr;
   if(!english) {
    text=Regex.Replace(text,@"^LEVEL (\d+)$","BOLUM $1");
    text=Regex.Replace(text,@"^MOVE BANK: (\d+)$","HAMLE KASASI: $1");
    text=Regex.Replace(text,@"^SPIN - (\d+) MOVES$","CEVIR - $1 HAMLE");
    text=text.Replace("FIREWORK","FISEK").Replace("HAMMER","CEKIC").Replace("MAGNET","MIKNATIS").Replace("moves to the wheel bank","hamle kasaya eklendi");
   }
   return text;
  }
 }
}
