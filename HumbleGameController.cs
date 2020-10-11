using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using static System.IO.Directory;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Playnite;
using Playnite.Common;
using Playnite.SDK;
using Playnite.SDK.Events;
using Playnite.SDK.Models;
using System.Net;
using WindowManager;
using System.IO.Compression;
using System.Windows.Controls;

namespace humble
{
    public class HumbleGameController : BaseGameController
    {
        private CancellationTokenSource watcherToken;
        private FileSystemWatcher fileWatcher;
        private HumbleLibSettings settings;
        private ProcessMonitor procMon;
        private Stopwatch stopWatch;
        private HumbleGameLibrary library;
        private IPlayniteAPI api;

        private ILogger logger = LogManager.GetLogger();
        public string FileName { get; set;} = "";
         public string Extension { get; set;} = "";
         private string GameName;

        public HumbleGameController(Game game, HumbleGameLibrary library, HumbleLibSettings settings, IPlayniteAPI api) : base(game)
        {
            this.settings = settings;
            this.library = library;
            this.api = api;
            this.GameName = game.Name;
        }

        public override void Dispose()
        {
            ReleaseResources();
        }

        public void ReleaseResources()
        {
            fileWatcher?.Dispose();
            procMon?.Dispose();
        }

        public override void Play()
        {
            ReleaseResources();
            if (Game.PlayAction.Type == GameActionType.Emulator)
            {
                throw new NotSupportedException();
            }

            var playAction = api.ExpandGameVariables(Game, Game.PlayAction);
            OnStarting(this, new GameControllerEventArgs(this, 0));
            var proc = GameActionActivator.ActivateAction(playAction);
            OnStarted(this, new GameControllerEventArgs(this, 0));

            if (Game.PlayAction.Type != GameActionType.URL)
            {
                stopWatch = Stopwatch.StartNew();
                procMon = new ProcessMonitor();
                procMon.TreeDestroyed += Monitor_TreeDestroyed;
                procMon.WatchProcessTree(proc);
            }
            else
            {
                OnStopped(this, new GameControllerEventArgs(this, 0));
            }
        }

        public void wc_ProgessChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            //this.Game.Name = GameName +$"{e.ProgressPercentage}%";

            //System.IO.File.AppendAllText("C:/TEMP/games.txt", $"Download status: {e.ProgressPercentage}%."+"\n", Encoding.UTF8);
            //Console.WriteLine($"Download status: {e.ProgressPercentage}%.");
        }
        public void wc_OnDownloadFileCompleted (object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            //var x = library.IsInstalledGames(Game.Name,Game.GameId,true);
            
            if(String.Equals(Extension,"exe")){  
                WindowManager.MsgBoxResult result = WindowManager.Interaction.MsgBox("please install into either of these:\n"+Path.Combine(settings.GamesLocation,Game.Name)+"\n"+Path.Combine(settings.GamesLocation,Game.GameId), "Notice", MsgBoxStyle.OkOnly);           
                AsyncProcess(FileName);             
            }else if(String.Equals(Extension,"zip") || String.Equals(Extension,"rar")){
                //System.IO.File.AppendAllText("C:/TEMP/games.txt", "before asyc\n", Encoding.UTF8);
                AsyncUnzip(FileName,Path.Combine(settings.GamesLocation,Game.Name));
                //ZipFile.ExtractToDirectory(FileName,Path.Combine(settings.GamesLocation,Game.Name));
               // System.IO.File.AppendAllText("C:/TEMP/games.txt", "after asyc\n", Encoding.UTF8);
            }else{
                string[] p = this.FileName.ToString().Split('/');
                string datei = p[p.Length-1];
                WindowManager.MsgBoxResult result = WindowManager.Interaction.MsgBox("Cannot determine how to install "+Game.Name
                +"\nInstall this file: "+datei
                +"\n\ninto either of these Folders:\n"+Path.Combine(settings.GamesLocation,Game.Name)+"\n"+Path.Combine(settings.GamesLocation,Game.GameId), "Notice", MsgBoxStyle.OkOnly);           
                AsyncProcess(Path.Combine(settings.GamesLocation,".install"));
            }
            StartInstallWatcher();
            
        }

         public async Task AsyncProcess(string file)
        {
            await Task.Delay(1);

            using (Process exeProcess = Process.Start(file))
            {
                exeProcess.WaitForExit();
            }
            StartInstallWatcher();
        }
        public async Task AsyncUnzip(string file,string target)
        {
            await Task.Delay(1);
            //System.IO.File.AppendAllText("C:/TEMP/games.txt", "before zip\n", Encoding.UTF8);
            ZipFile.ExtractToDirectory(file,target);
            Game.InstallDirectory = target;
            //System.IO.File.AppendAllText("C:/TEMP/games.txt", "after zip\n", Encoding.UTF8);
            StartInstallWatcher();
        }
        
        public override void Install()
        {
            ReleaseResources();
            //ProcessStarter.StartUrl(@"https://www.gog.com/account");
            ObservableCollection<Playnite.SDK.Models.Link> links = this.Game.Links;
            foreach(Link mylink in links){
                if(String.Equals(mylink.Name,"downloadURL")){
                    //System.IO.File.AppendAllText("C:/TEMP/games.txt", mylink.Url.ToString()+"\n", Encoding.UTF8);

                    string[] exarra = mylink.Url.ToString().Split('?')[0].Split('/');
                    this.Extension = exarra[exarra.Length-1].Split('.')[1].ToLower();

                    this.FileName = Path.Combine(Path.Combine(settings.GamesLocation,".install"),Game.GameId+"."+Extension);
                    
                    System.IO.Directory.CreateDirectory(Path.Combine(settings.GamesLocation,".install"));
                    

                    if(!System.IO.File.Exists(FileName)){
                        using (WebClient wc = new WebClient())
                        {
                        wc.DownloadProgressChanged += wc_ProgessChanged;
                        wc.DownloadFileCompleted += wc_OnDownloadFileCompleted;
                        wc.DownloadFileAsync (
                                    // Param1 = Link of file
                                    new System.Uri(mylink.Url.ToString()),
                                    // Param2 = Path to save
                                    FileName
                                );
                        }
                    }else{
                        wc_OnDownloadFileCompleted(this,null);
                    }
                    
                }
            }
            
        }

        public override void Uninstall()
        {
            ReleaseResources();
            logger.Info("uninstall");
            logger.Info(Game.InstallDirectory);
            var uninstaller = Path.Combine(Game.InstallDirectory, "unins000.exe");
            
            if (!File.Exists(uninstaller))
            {
                throw new FileNotFoundException("Uninstaller not found.");
            }

            Process.Start(uninstaller);
            var infoFile = string.Format("goggame-{0}.info", Game.GameId);
            if (File.Exists(Path.Combine(Game.InstallDirectory, infoFile)))
            {
                fileWatcher = new FileSystemWatcher()
                {
                    NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName,
                    Path = Game.InstallDirectory,
                    Filter = Path.GetFileName(infoFile)
                };

                fileWatcher.Deleted += FileWatcher_Deleted;
                fileWatcher.EnableRaisingEvents = true;
            }
            else
            {
                OnUninstalled(this, new GameControllerEventArgs(this, 0));
            }
        }

        private void ProcMon_TreeStarted(object sender, EventArgs args)
        {
            OnStarted(this, new GameControllerEventArgs(this, 0));
        }

        private void Monitor_TreeDestroyed(object sender, EventArgs args)
        {
            stopWatch.Stop();
            OnStopped(this, new GameControllerEventArgs(this, stopWatch.Elapsed.TotalSeconds));
        }

        private void FileWatcher_Deleted(object sender, FileSystemEventArgs e)
        {
            fileWatcher.EnableRaisingEvents = false;
            fileWatcher.Dispose();
            OnUninstalled(this, new GameControllerEventArgs(this, 0));
        }

        public async void StartInstallWatcher()
        {
            logger.Info("StartInstallWatcher started:"+Game.GameId);
            if(watcherToken != null)
            {
                watcherToken.CancelAfter(200);
                await Task.Delay(1000);
            }
            watcherToken = new CancellationTokenSource();  
            var stopWatch = Stopwatch.StartNew();
            watcherToken.CancelAfter(18000);
            while (true)
            {
                if (watcherToken.IsCancellationRequested)
                {
                    return;
                }

                var games = library.GetInstalledGames();

                if (games.ContainsKey(Game.GameId))
                {
                    logger.Info("Installer "+Game.GameId);
                    var game = games[Game.GameId];
                    stopWatch.Stop();
                    var installInfo = new GameInfo()
                    {
                        PlayAction = game.PlayAction,
                        OtherActions = game.OtherActions,
                        InstallDirectory = game.InstallDirectory
                    };

                    OnInstalled(this, new GameInstalledEventArgs(game, this, stopWatch.Elapsed.TotalSeconds));
                    return;
                }

                await Task.Delay(5000);
            }
        }
    }
}
