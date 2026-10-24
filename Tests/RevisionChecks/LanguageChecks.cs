using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using WorkshopMatch;

partial class Program
{
    static void RunLanguageChecks()
    {
        using var data=JsonDocument.Parse(File.ReadAllText("Assets/WorkshopMatch/Resources/WorkshopMatch/languages.json"));
        var rows=data.RootElement.GetProperty("entries").EnumerateArray().Select(e=>new GameLanguage.Entry {
            source=e.GetProperty("source").GetString(),tr=e.GetProperty("tr").GetString(),en=e.GetProperty("en").GetString()
        }).ToArray();
        var lookup=GameLanguage.BuildLookup(rows);
        string Text(string text,bool english)=>GameLanguage.Translate(text,english,lookup);
        Check(Text("DEVAM",true)=="CONTINUE" && Text("CONTINUE",false)=="DEVAM",
            "tutorial Continue caption resolves correctly from either authored language");
        Check(Text("NEXT",false)=="ILERI" && Text("NEXT",true)=="NEXT","help Next button has both languages");
        foreach(var row in rows)
        {
            Check(!string.IsNullOrWhiteSpace(row.en)&&!string.IsNullOrWhiteSpace(row.tr),"catalog has two captions: "+row.source);
            foreach(bool english in new[]{false,true})
            {
                string once=Text(row.source,english);
                Check(GameLanguage.Key(Text(once,english))==GameLanguage.Key(once),
                    "repeated UI localization preserves selected language: "+row.source+" en="+english);
            }
        }
        foreach(var path in Directory.GetFiles("Assets/WorkshopMatch/Scripts","BoardBlockoutController*.cs"))
        {
            string source=File.ReadAllText(path);
            foreach(Match match in Regex.Matches(source,"new GUIContent\\(\"((?:\\\\.|[^\"\\\\])*)\"\\)"))
            {
                string label=Regex.Unescape(match.Groups[1].Value);
                Check(lookup.ContainsKey(GameLanguage.Key(label)) || Text(label,false)!=label,
                    "button caption covered by catalog or numeric template: "+Path.GetFileName(path)+" / "+label);
            }
        }
        Check(Text("MOVE BANK: 23",false)=="HAMLE KASASI: 23" && Text("SPIN - 10 MOVES",false)=="CEVIR - 10 HAMLE",
            "numeric wheel labels localize while preserving values");
        Check(Text("+2 HAMMER",false)=="+2 CEKIC" && Text("+1 FIREWORK",false)=="+1 FISEK",
            "wheel prize tool names use selected language");
    }
}
