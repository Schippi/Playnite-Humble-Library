using System;
using System.IO;
using System.Collections.Generic;
using Playnite.SDK.Plugins;
using Playnite.SDK.Models;
using Playnite.SDK;
using static Playnite.SDK.Models.GameActionType;
//using System.Text.Json;
//using System.Text.Json.Serialization;
using Newtonsoft.Json;
using  Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Net;
using System.Windows.Controls;

namespace humble
{
    class MyConverter : CustomCreationConverter<IDictionary<string, object>>
{
    public override IDictionary<string, object> Create(Type objectType)
    {
        return new Dictionary<string, object>();
    }

    public override bool CanConvert(Type objectType)
    {
        // in addition to handling IDictionary<string, object>
        // we want to handle the deserialization of dict value
        // which is of type object
        return objectType == typeof(object) || base.CanConvert(objectType);
    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.StartObject
            || reader.TokenType == JsonToken.Null)
            return base.ReadJson(reader, objectType, existingValue, serializer);

        // if the next token is not an object
        // then fall back on standard deserializer (strings, numbers etc.)
        return serializer.Deserialize(reader);
    }
}

    public class HumbleGameLibrary : LibraryPlugin
{
    public override Guid Id { get; } = Guid.Parse("D625A3B7-1ABC-41CB-9CD7-74448D28E99B");

    public override string Name { get; } = "HumbleLibrary";

    public string RootFolder { get; } = @"D:\HumbleGames";

    internal HumbleLibSettings LibrarySettings { get; private set; }

    public HumbleGameLibrary(IPlayniteAPI api) : base (api)
    {
        LibrarySettings = new HumbleLibSettings(this, api);
        //LibrarySettings = new HumbleLibSettings();
    } 

    public override IGameController GetGameController(Game game){
         return new HumbleGameController(game, this, LibrarySettings, PlayniteApi);;
    }

    public override ISettings GetSettings(bool asd)
    {
        return LibrarySettings;
    }

     public override UserControl GetSettingsView(bool asd)
    {
        return new HumbleLibSettingsView();
    }

    public Dictionary<string, GameInfo> GetInstalledGames()
    {
        Dictionary<string, GameInfo> result = new Dictionary<string, GameInfo>();
        foreach (var file in Directory.GetFiles(RootFolder))
        {
            if(String.Equals(file,".install")){
                continue;
            }
            else if(String.Equals(file,".icons")){
                continue;
            }
            string innerdir = Path.Combine(RootFolder,file);
            if(Directory.Exists(innerdir)){
                foreach(GameInfo gam in GetGamesOffline()){
                    if(String.Equals(file,gam.Name) || String.Equals(file,gam.GameId)){
                        foreach (var innerfile in Directory.GetFiles(RootFolder))
                        {
                            string[] p = innerfile.ToString().Split('/');
                            string datei = p[p.Length-1];
                            if(System.IO.File.Exists(innerfile) && innerfile.ToLower().EndsWith("exe") ){
                                if(innerfile.ToLower().Contains("unity") || innerfile.Contains("unins")){
                                    continue;
                                }
                                
                                var game = new GameInfo()
                                {
                                    Source = "HumbleLibrary",
                                    GameId = gam.GameId,
                                    Name = gam.Name,
                                    InstallDirectory = innerdir,
                                    PlayAction = new GameAction()
                                    {
                                        Type = GameActionType.File,
                                        Path = innerfile
                                    },
                                    IsInstalled = true
                                };
                            }     
                        }
                    }
                }
            }
        }
        return result;
    }

    public string IsInstalledGames(string x_name, string x_gameId)
    {
        foreach (var file in Directory.GetDirectories(RootFolder))
        {
            if(String.Equals(file,"install")){
                continue;
            }
            else if(String.Equals(file,".icons")){
                continue;
            }
            
            string[] spl = file.Split('\\');
            string lastpart = spl[spl.Length-1];
            System.IO.File.AppendAllText("C:/TEMP/games.txt", lastpart.ToLower().Replace(" ","")+" - "+x_gameId.ToLower().Replace(" ","")+"\n", Encoding.UTF8);
            System.IO.File.AppendAllText("C:/TEMP/games.txt", lastpart.ToLower().Replace(" ","")+" - "+x_name.ToLower().Replace(" ","")+"\n", Encoding.UTF8);
            if(String.Equals(lastpart.ToLower().Replace(" ",""),x_name.ToLower().Replace(" ","")) || String.Equals(lastpart.ToLower().Replace(" ",""),x_gameId.ToLower().Replace(" ",""))){
                System.IO.File.AppendAllText("C:/TEMP/games.txt", "3", Encoding.UTF8);
                foreach (var innerfile in Directory.GetFiles(file))
                {
                    //string filepath = Path.Combine(file,innerfile);
                    //System.IO.File.AppendAllText("C:/TEMP/games.txt", "4" + innerfile, Encoding.UTF8);
                    
                    if(System.IO.File.Exists(innerfile) && innerfile.ToLower().EndsWith("exe")){
                        if(!innerfile.ToLower().Contains("unity")){
                            return innerfile;
                        }
                        
                        
                    }     
                }
            }
            
            
        }
        return null;
    }

    public override IEnumerable<GameInfo> GetGames()
    {
        System.IO.Directory.CreateDirectory(Path.Combine(RootFolder,".icons"));
        return GetGamesOffline();
    }

    public IEnumerable<GameInfo> GetGamesOffline()
    {
        string json = System.IO.File.ReadAllText("C:/Users/Carsten Schipmann/.config/humblebundle/games.json");
        Dictionary<string, object> values = JsonConvert.DeserializeObject<Dictionary<string, object>>(json, new JsonConverter[] {new MyConverter()});
    
        List<GameInfo> result = new List<GameInfo>();
        WebClient cln = new WebClient();

        foreach(KeyValuePair<string, object> entry in values)
        {
            Dictionary<string, object> item ;
            Console.WriteLine(entry.Key);
            item = (Dictionary<string, object>)entry.Value;
            object o;
            item.TryGetValue("downloads",out o);
            object human;
            item.TryGetValue("human_name",out human);
            object icn;
            item.TryGetValue("icon",out icn);
            object gid;
            item.TryGetValue("machine_name",out gid);
            object plat;

            JArray jo = (JArray)o;
            foreach(JToken subtoken in jo.Children()){
                Type t = subtoken.GetType();
                
                Dictionary<string, object> subitem =((JObject)subtoken).ToObject<Dictionary<string, object>>();
        //       System.IO.File.AppendAllText("C:/TEMP/games.txt", subitem.Count+" * "+subitem.ToString()+"\n", Encoding.UTF8);
                /*foreach(KeyValuePair<string, object> di in subitem)
                {
                    if(!(di.Value is null)){
                        System.IO.File.AppendAllText("C:/TEMP/games.txt", di.ToString()+"\n", Encoding.UTF8);
                        System.IO.File.AppendAllText("C:/TEMP/games.txt", di.Key.ToString()+" = "+di.Value.ToString()+"\n", Encoding.UTF8);
                    }
                }
*/

                subitem.TryGetValue("platform",out plat);
                if(!plat.ToString().Equals("windows")){
                    continue;
                }
                subitem.TryGetValue("download_struct",out var downloadstruct);
                //System.IO.File.AppendAllText("C:/TEMP/games.txt", downloadstruct.ToString()+"   2downloadstruct\n", Encoding.UTF8);
                JArray strucs = (JArray)downloadstruct;
                Dictionary<string, object> downloadstructd =((JObject)strucs[0]).ToObject<Dictionary<string, object>>();   
                downloadstructd.TryGetValue("url",out var myurl);
                Dictionary<string, object> myurld =((JObject)myurl).ToObject<Dictionary<string, object>>();
                myurld.TryGetValue("web",out var myweburl);

               // System.IO.File.AppendAllText("C:/TEMP/games.txt", plat.ToString()+"\n", Encoding.UTF8);
                string icn2 = Path.Combine(Path.Combine(RootFolder,".icons"),gid+".png");
                if(plat.ToString().Equals("windows")){
                    System.IO.File.AppendAllText("C:/TEMP/games.txt", human.ToString()+"\n", Encoding.UTF8);
                    string x_path = IsInstalledGames(human.ToString(),gid.ToString());
                    bool ins = false;
                    
                    if(x_path != null){
                        ins = true;
                        System.IO.File.AppendAllText("C:/TEMP/games.txt", x_path+"\n", Encoding.UTF8);     
                    }else{
                        
                    }
                    result.Add(new GameInfo(){
                        Name = human.ToString(),
                        GameId = gid.ToString(),
                        PlayAction = new GameAction()
                        {
                            Type = GameActionType.File,
                            Path = x_path ?? "C:/Program Files (x86)/Notepad++/notepad++.exe"
                        },
                        Links = new List<Playnite.SDK.Models.Link>(){
                            new Playnite.SDK.Models.Link("downloadURL",myweburl.ToString())
                        },
                        IsInstalled = ins,
                        Icon = icn2.ToString()
                    });
                    if(!(icn is null) && !System.IO.File.Exists(icn2)){
                        cln.DownloadFile (
                                    // Param1 = Link of file
                                    new System.Uri(icn.ToString()),
                                    // Param2 = Path to save
                                    icn2
                                );
                    }

                   // System.IO.File.AppendAllText("C:/TEMP/games.txt", icn.ToString()+"\n", Encoding.UTF8);
                  //  return result;
                }
            }
            //System.IO.File.AppendAllText("C:/TEMP/games.txt", t.ToString()+"\n", Encoding.UTF8);

        }

    
        return result;
        /*return new List<GameInfo>()
        {
            new GameInfo()
            {
                Name = "Notepad",
                GameId = "notepad",
                PlayAction = new GameAction()
                {
                    Type = GameActionType.File,
                    Path = "C:/Program Files (x86)/Notepad++/notepad++.exe"
                },
                IsInstalled = true,
                Icon = @"C:\Program Files (x86)\Notepad++\notepad++.exe"
            }
        };*/
    }
}
}
