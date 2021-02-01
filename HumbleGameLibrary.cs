using System;
using System.IO;
using System.Collections.Generic;
using Playnite.SDK.Plugins;
using Playnite.SDK.Models;
using Playnite.SDK;
using Playnite.SDK.Metadata;
using static Playnite.SDK.Models.GameActionType;
//using System.Text.Json;
//using System.Text.Json.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Net;
using System.Windows.Controls;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using Microsoft.Scripting;
//using Microsoft.Scripting.Hosting;
using System.Threading;

namespace humble
{
    



    public class HumbleGameLibrary : LibraryPlugin
    {
        public override Guid Id { get; } = Guid.Parse("D625A3B7-1ABC-41CB-9CD7-74448D28E99B");

        public override string Name { get; } = "HumbleLibrary";

       // public string RootFolder { get; } = @"D:\HumbleGames";

        private ILogger logger = LogManager.GetLogger();

        internal HumbleLibSettings LibrarySettings { get; private set; }

        public Dictionary<string, GameMetadata> metadata { get; } = new Dictionary<string, GameMetadata>();

        public HumbleGameLibrary(IPlayniteAPI api) : base(api)
        {
            LibrarySettings = new HumbleLibSettings(this, api);
        }

        public override IGameController GetGameController(Game game)
        {
            return new HumbleGameController(game, this, LibrarySettings, PlayniteApi); ;
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
            logger.Info("hello HumbleGames:"+LibrarySettings.GamesLocation);
            foreach (var file in Directory.GetDirectories(LibrarySettings.GamesLocation))
            {
                if (String.Equals(file, ".install"))
                {
                    continue;
                }
                
                foreach (GameInfo gam in GetGamesOffline())
                {
                    string exe = IsInstalledGames(gam.Name,gam.GameId,false);
                    if(exe != null){
                        var game = new GameInfo()
                        {
                            Source = "HumbleLibrary",
                            GameId = gam.GameId,
                            Name = gam.Name,
                            Platform = "PC",
                            InstallDirectory = System.IO.Path.GetDirectoryName(exe),
                            PlayAction = new GameAction()
                            {
                                Type = GameActionType.File,
                                Path = exe
                            },
                            IsInstalled = true
                        };
                        if(!result.ContainsKey(gam.GameId)){
                            result.Add(gam.GameId,game);
                        }
                    }
                }
                
            }

            return result;
        }

        public string IsInstalledGames(string x_name, string x_gameId,Boolean prin)
        {
            if(prin){logger.Info("looking for: "+x_name+", "+x_gameId);}
            foreach (var file in Directory.GetDirectories(LibrarySettings.GamesLocation))
            {
                if (String.Equals(file, ".install"))
                {
                    continue;
                }
                

                string[] spl = file.Split('\\');
                string lastpart = spl[spl.Length - 1];

                if (String.Equals(lastpart.ToLower().Replace(" ", ""), x_name.ToLower().Replace(" ", "")) || String.Equals(lastpart.ToLower().Replace(" ", ""), x_gameId.ToLower().Replace(" ", "")))
                {
                    foreach (var innerfile in Directory.GetFiles(file))
                    {
                        if (System.IO.File.Exists(innerfile) && innerfile.ToLower().EndsWith("exe"))
                        {
                            //logger.Info("exe found: "+innerfile);
                            if (!innerfile.ToLower().Contains("unity") && !innerfile.ToLower().Contains("unins"))
                            {
                                if(prin){logger.Info("found it!: "+innerfile);}
                                return innerfile;
                            }
                        }
                    }
                    foreach (var innerfile in Directory.GetDirectories(file))
                    {
                        if(System.IO.Directory.Exists(innerfile) && (innerfile.ToLower().EndsWith(x_name.ToLower()) || innerfile.ToLower().EndsWith("bin")))
                        {
                            foreach (var eveninnerfile in Directory.GetFiles(innerfile))
                            {
                                if(prin && String.Equals(x_gameId,"syzygy")){logger.Info("file 1 "+eveninnerfile);}
                                if (System.IO.File.Exists(eveninnerfile) && eveninnerfile.ToLower().EndsWith("exe"))
                                {
                                    if (!eveninnerfile.ToLower().Contains("unity") && !innerfile.ToLower().Contains("unins")) 
                                    {
                                        if(prin){logger.Info("found it!: "+eveninnerfile);}
                                        return eveninnerfile;
                                    }
                                }
                            }
                            foreach (var eveninnerfile in Directory.GetDirectories(innerfile))
                            {
                                if(System.IO.Directory.Exists(eveninnerfile) && (eveninnerfile.ToLower().EndsWith(x_name.ToLower()) || eveninnerfile.ToLower().EndsWith("bin")))
                                {
                                    foreach (var superinnerfile in Directory.GetFiles(eveninnerfile))
                                    {
                                        if (System.IO.File.Exists(superinnerfile) && superinnerfile.ToLower().EndsWith("exe"))
                                        {
                                            if (!superinnerfile.ToLower().Contains("unity") && !innerfile.ToLower().Contains("unins"))
                                            {
                                                if(prin){logger.Info("found it!: "+superinnerfile);}
                                                return superinnerfile;
                                            }
                                        }
                                    }
                                }
                            }
                        }

                    }
                }


            }
            return null;
        }

        public class CookieAwareWebClient : WebClient
        {
            public CookieAwareWebClient()
            {
                CookieContainer = new CookieContainer();
            }
            public CookieContainer CookieContainer { get; private set; }

            protected override WebRequest GetWebRequest(Uri address)
            {
                var request = (HttpWebRequest)base.GetWebRequest(address);
                request.CookieContainer = CookieContainer;
                return request;
            }
        }

        public class HumbleGameData
        {
            public string Name { get; set; }
            public string PurchaseID { get; set; }
            public string MachineName { get; set; }
            public GameInfo Info { get; set; }
            public GameMetadata Meta { get; set; }
            public string Json { get; set; }
        }

        public class ThreadWithState
        {
            // State information used in the task.
            private string token;
            private string gaem;

            private HumbleGameLibrary library;

            public string Res { get; private set; }
            private ILogger logger = LogManager.GetLogger();
            public Dictionary<string, HumbleGameData> Values { get; private set; }

            // The constructor obtains the state information.
            public ThreadWithState(HumbleGameLibrary library, string token, string gaem)
            {
                this.gaem = gaem;
                this.token = token;
                this.library = library;
                Values = new Dictionary<string, HumbleGameData>();
            }

            // The thread procedure performs the task, such as formatting
            // and printing a document.
            public void ThreadProc()
            {
                CookieAwareWebClient cln = new CookieAwareWebClient();
                cln.CookieContainer.SetCookies(new Uri("https://www.humblebundle.com"), "_simpleauth_sess=" + token + ";");
                this.Res = cln.DownloadString("https://www.humblebundle.com/api/v1/order/" + gaem);
                Dictionary<string, object> vals = JsonConvert.DeserializeObject<Dictionary<string, object>>(this.Res, new JsonConverter[] { new MyConverter() });
                if (vals.TryGetValue("subproducts", out var o))
                {
                    var jarr = (JArray)o;
                    foreach (JObject root in jarr)
                    {
                        var subp = root.ToObject<Dictionary<string, object>>();
                        if (library.GameFromDict(cln, subp, out var info1, out var meta))
                        {
                            var data = new HumbleGameData()
                            {
                                PurchaseID = gaem,
                                Name = info1.Name,
                                Info = info1,
                                Meta = meta,
                                Json = root.ToString(),
                                MachineName = info1.GameId
                            };
                            if (Values.Keys.Contains(info1.Name))
                            {
                                logger.Info("DOUBLE: " + gaem + " " + info1.Name);
                            }
                            else
                            {
                                Values.Add(info1.Name, data);
                            }
                        }
                    }
                }

            }

        }

        public string GetCachePath(string dirName)
        {
            return Path.Combine(GetPluginUserDataPath(), dirName);
        }

        public bool GameFromDict(WebClient cln, Dictionary<string, object> item, out GameInfo info, out GameMetadata meta)
        {
            item.TryGetValue("downloads", out var o);
            item.TryGetValue("human_name", out var human);
            item.TryGetValue("icon", out var icn);
            item.TryGetValue("machine_name", out var gid);
            item.TryGetValue("url", out var gameURL);
            object plat;
            meta = new GameMetadata();
            info = null;
            JArray jo = (JArray)o;
            foreach (JToken subtoken in jo.Children())
            {
                Type t = subtoken.GetType();
                Dictionary<string, object> subitem = ((JObject)subtoken).ToObject<Dictionary<string, object>>();
                subitem.TryGetValue("platform", out plat);
                if (!plat.ToString().Equals("windows"))
                {
                    continue;
                }
                subitem.TryGetValue("download_struct", out var downloadstruct);
                JArray strucs = (JArray)downloadstruct;
                Dictionary<string, object> downloadstructd = ((JObject)strucs[0]).ToObject<Dictionary<string, object>>();
                downloadstructd.TryGetValue("url", out var myurl);
                Dictionary<string, object> myurld = ((JObject)myurl).ToObject<Dictionary<string, object>>();
                myurld.TryGetValue("web", out var myweburl);

                string icn2 = Path.Combine(GetCachePath("icons"), gid + ".png");
                if (plat.ToString().Equals("windows"))
                {
                    //System.IO.File.AppendAllText("C:/TEMP/games.txt", human.ToString() + "\n", Encoding.UTF8);
                    string x_path = IsInstalledGames(human.ToString(), gid.ToString(),false);
                    bool ins = false;
                    string inspath = null;

                    if (x_path != null)
                    {
                        ins = true;
                        inspath = System.IO.Path.GetDirectoryName(x_path);
                        //System.IO.File.AppendAllText("C:/TEMP/games.txt", x_path + "\n", Encoding.UTF8);
                        //logger.Info("installpath:"+inspath);
                    }
                    else
                    {

                    }


                    info = new GameInfo()
                    {
                        Name = human.ToString(),
                        GameId = gid.ToString(),
                        PlayAction = new GameAction()
                        {
                            Type = GameActionType.File,
                            Path = x_path
                        },
                        Links = new List<Playnite.SDK.Models.Link>(){
                            new Playnite.SDK.Models.Link("downloadURL",myweburl.ToString())
                        },
                        IsInstalled = ins,
                        InstallDirectory = inspath,
                        Platform = "PC",
                        Icon = icn2.ToString()
                    };
                    if(gameURL != null){
                        info.Links.Add(
                                new Playnite.SDK.Models.Link("Website",gameURL.ToString())
                        );
                    }
                    if (!(icn is null) && !System.IO.File.Exists(icn2))
                    {
                        cln.DownloadFile(
                                    // Param1 = Link of file
                                    new System.Uri(icn.ToString()),
                                    // Param2 = Path to save
                                    icn2
                                );
                    }
                    return true;
                    // System.IO.File.AppendAllText("C:/TEMP/games.txt", icn.ToString()+"\n", Encoding.UTF8);
                    //  return result;
                }
            }
            return false;
        }



        public override IEnumerable<GameInfo> GetGames()
        {
            System.IO.Directory.CreateDirectory(GetCachePath("icons"));
            
            List<GameInfo> result = new List<GameInfo>();
            string logintoken = LibrarySettings.LoginToken;

            if(string.Equals("",logintoken)){
                return result;
            }

            CookieAwareWebClient cln = new CookieAwareWebClient();
            //cln.Headers.Add(HttpRequestHeader.Cookie,"_simpleauth_sess="+logintoken);
            //cln.Headers.Add(HttpRequestHeader.Cookie, "csrf_cookie="+secret);
            var cacheFile = GetCachePath("games.json");
            var purchaseFile = GetCachePath("purchases.json");
            List<string> purchases = new List<string>();
            if(!LibrarySettings.AlwaysScanEverything && System.IO.File.Exists(purchaseFile)){
                using (StreamReader r = new StreamReader(purchaseFile))
                {
                    string jsonx = r.ReadToEnd();
                    
                    var Items = JsonConvert.DeserializeObject<JArray>(jsonx);
                    foreach(JValue item in Items){
                        purchases.Add(item.ToString());
                    }
                }
            }
            
            
            cln.CookieContainer.SetCookies(new Uri("https://www.humblebundle.com"), "_simpleauth_sess=" + logintoken + ";");

            string myjson = cln.DownloadString("https://www.humblebundle.com/api/v1/user/order");
            //logger.Info(myjson);
            JArray objects = null;
            try{
                objects = JArray.Parse(myjson);
            } catch(Exception e){
               logger.Info(e.StackTrace) ;
               throw new Exception("Login Token Wrong, please reauthenticate");
            }
            // Dictionary<int,string> threads = new Dictionary<int,string>(objects.Count);
            Dictionary<Thread, ThreadWithState> threads = new Dictionary<Thread, ThreadWithState>(objects.Count);

            foreach (JObject root in objects)
            {
                foreach (KeyValuePair<String, JToken> app in root)
                {
                    if(!purchases.Contains(app.Value.ToString())){
                        logger.Info("new purchase id: "+app.Value.ToString());
                        purchases.Add(app.Value.ToString());
                        ThreadWithState tws = new ThreadWithState(this,  logintoken, app.Value.ToString());
                        Thread t = new Thread(new ThreadStart(tws.ThreadProc));
                        t.Start();
                        threads.Add(t, tws);
                    }
                }
                //break;
            }

            
            string json = JsonConvert.SerializeObject(purchases.ToArray());
            System.IO.File.WriteAllText(purchaseFile, json);



            //System.IO.File.AppendAllText(cacheFile, "[\n", Encoding.UTF8);
            
            List<string> already = new List<string>();

            string filecontents = "{\r\n";

            if(System.IO.File.Exists(cacheFile)){
                json = System.IO.File.ReadAllText(cacheFile);
            }else{
                json = "{}";
            }
            

            Dictionary<string, object> values = JsonConvert.DeserializeObject<Dictionary<string, object>>(json, new JsonConverter[] { new MyConverter() });

            foreach (KeyValuePair<string, object> entry in values)
            {
                Dictionary<string, object> item;
                item = (Dictionary<string, object>)entry.Value;

                var output = JsonConvert.SerializeObject(item,Formatting.Indented);

                //already.Add(entry.Key);
                //logger.Info("xxxXXXxxx  "+output);
                already.Add(entry.Key);

                if (already.Count > 1)
                {
                    filecontents += ",\r\n\"" + entry.Key + "\" : " + output;
                }
                else
                {
                    filecontents +=  "\""+entry.Key + "\" : " + output;
                }

            }
            
            foreach (KeyValuePair<Thread, ThreadWithState> entry in threads)
            {
                entry.Key.Join();
                foreach (KeyValuePair<string, HumbleGameData> pair in entry.Value.Values)
                {

                    if (!already.Contains(pair.Key))
                    {
                        already.Add(pair.Key);
                        result.Add(pair.Value.Info);
                        metadata[pair.Key] = pair.Value.Meta;
                        if (already.Count > 1)
                        {
                            filecontents += ",\r\n\"" + pair.Value.MachineName + "\" : " + pair.Value.Json;
                        }
                        else
                        {
                            filecontents +=  "\""+pair.Value.MachineName + "\" : " + pair.Value.Json;
                        }

                    }
                }
            }
            //logger.Info(filecontents+"\r\n}");
            System.IO.File.WriteAllText(cacheFile, filecontents+"\r\n}", Encoding.UTF8);


            //Directory.CreateDirectory(cacheDir);


            return result;
            //return GetGamesOffline();
        }

        public override LibraryMetadataProvider GetMetadataDownloader()
        {
            return new HumbleMetadataProvider(this, PlayniteApi);
        }

        public IEnumerable<GameInfo> GetGamesOffline()
        {
            //string json = System.IO.File.ReadAllText("C:/Users/Carsten Schipmann/.config/humblebundle/games.json");

            string json = System.IO.File.ReadAllText(GetCachePath("games.json"));
            Dictionary<string, object> values = JsonConvert.DeserializeObject<Dictionary<string, object>>(json, new JsonConverter[] { new MyConverter() });

            List<GameInfo> result = new List<GameInfo>();
            WebClient cln = new WebClient();
            List<string> humanList = new List<string>();

            foreach (KeyValuePair<string, object> entry in values)
            {
                Dictionary<string, object> item;
                item = (Dictionary<string, object>)entry.Value;

                if (GameFromDict(cln, item, out var info, out var meta))
                {
                    var human = info.Name;
                    if (humanList.Contains(human.ToString()))
                    {
                        continue;
                    }

                    humanList.Add(human.ToString());
                    result.Add(info);
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
}
