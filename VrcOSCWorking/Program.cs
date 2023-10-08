// See https://aka.ms/new-console-template for more information

using EmbedIO.Utilities;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharpOSC;
using SpotifyAPI.Web;
using SpotifyAPI.Web.Auth;
using Swan;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Runtime;
using System.Runtime.ExceptionServices;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using System.Xml.Linq;
using System.Xml.Serialization;
using static SpotifyAPI.Web.PlayerSetRepeatRequest;
using static SpotifyAPI.Web.PlaylistRemoveItemsRequest;
using static System.Net.Mime.MediaTypeNames;

namespace VrcOSCWorking // https://github.com/VRCWizard/TTS-Voice-Wizard/blob/d5ee39dab5ff4692c7ee2069b65639a9b3a44e2a/OSCVRCWiz/Services/Text/OutputText.cs#L117
{
    public static class Program
    {
        private static string ip = "127.0.0.1";
        private static int port = 9000;

        private static void Log(string msg)
        {
            Console.Write($"VRCOSC: {msg} \n");
        }
        public static string LimitLength(this string source, int maxLength)
        {
            if (source.Length <= maxLength)
            {
                return source;
            }

            return source.Substring(0, maxLength);
        }
        private static string CenterString(this string s, int width)
        {
            if (s.Length >= width)
            {
                return s;
            }

            int leftPadding = (width - s.Length) / 2;
            int rightPadding = width - s.Length - leftPadding;

            return new string('\u2060', leftPadding) + s + new string('\u2060', rightPadding);
        }

        public static string RemoveBetween(string sourceString, string startTag, string endTag)
        {
            Regex regex = new Regex(string.Format("{0}(.*?){1}", Regex.Escape(startTag), Regex.Escape(endTag)), RegexOptions.RightToLeft);
            return regex.Replace(sourceString, startTag + endTag);
        }

        private static void ChatMsg(SharpOSC.UDPSender sender, string[] msgs)
        {
            string totalmessage = "";
            int limit = 32;
            string newline = " \v ";
            //int limit = new string("this is the max limit of").Length - newline.Length;
            int currmsg = 0;

            foreach (var startmsg in msgs)
            {
                currmsg += 1;
                string msg = startmsg;

                //if (startmsg.Contains("(") || startmsg.Contains(")"))
                //{
                //    msg = RemoveBetween(startmsg, "(", ")").Replace("(", "").Replace(")", ""); // removes features in song titles

                //}

                if (msg.Length >= limit)
                {
                    totalmessage += (LimitLength(msg, limit - 3) + "...").CenterString(limit);

                }
                else
                {
                    totalmessage += msg;

                }
                //else
                //{
                //    float charstomake = limit - msg.Length;
                //    for (float i = 0; i <= charstomake; i++)
                //    {
                //        if (i == (int)Math.Floor(charstomake / 2))
                //        {
                //            totalmessage += msg;
                //        }
                //        totalmessage += "\u2060";
                //    }

                //    //totalmessage += msg.CenterString(limit);
                //}

                if(msgs.Count() > currmsg)
                {
                    totalmessage += newline;

                }



            }

            

            foreach (var i in totalmessage.Split(newline))
            {
                Console.WriteLine($"|{i}|   {i.Length}");
            }
            sender.Send(new SharpOSC.OscMessage("/chatbox/input", totalmessage, true, false));
        }
        public class OSCSettings
        {
            public string CLIENTID = ""; 
            public string CLIENTSECRET = "";
            public string REDIRECTURL= "";
            public bool USEBROWSERTOOL;
            public int REDIRECTPORT;
        }

        static OSCSettings settings = new OSCSettings();

        private static int UPDATES = 2;

        private static EmbedIOAuthServer _server;
        public static string AddDoubleQuotes(this string value)
        {
            return "\"" + value + "\"";
        }
        public static async Task StartServer()
        {
            Log($"Path: {AppDomain.CurrentDomain.BaseDirectory}");

            Log($"Client id #######   Client secret ######  Redirect url {settings.REDIRECTURL}  Port {settings.REDIRECTPORT}");

            _server = new EmbedIOAuthServer(new Uri(settings.REDIRECTURL), settings.REDIRECTPORT);
            await _server.Start();

            _server.AuthorizationCodeReceived += OnAuthorizationCodeReceived;
            _server.ErrorReceived += OnErrorReceived;

            var request = new LoginRequest(_server.BaseUri, settings.CLIENTID, LoginRequest.ResponseType.Code)
            {
                Scope = new List<string> { Scopes.UserReadEmail, Scopes.Streaming, Scopes.UserReadPrivate, Scopes.UserReadPlaybackState, Scopes.UserReadRecentlyPlayed, Scopes.UserReadPlaybackPosition, Scopes.UserReadCurrentlyPlaying, Scopes.UserReadPlaybackState }
            };

            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "/BT/BrowseTool.exe") && settings.USEBROWSERTOOL)
            {
                Log($"Launch #BrowseTool by Dragon#");
                string args = $"{AddDoubleQuotes(request.ToUri().ToString())} True {AddDoubleQuotes("callback?Code")}";

                Log(args);

                Process.Start(AppDomain.CurrentDomain.BaseDirectory + "/BT/BrowseTool.exe", args);



            }
            else
            {
                Log($"BrowseTool not found or not enabled, launching browser");
                BrowserUtil.Open(request.ToUri());
            }
            // BrowserUtil.Open(request.ToUri());

        }
        private static async Task OnErrorReceived(object sender, string error, string state)
        {
            Console.WriteLine($"Aborting authorization, error received: {error}");
            await _server.Stop();
        }

        private static string lastplayed = null;
        private static void ErrorReport(string err)
        {
            Log($"Error: {err}");

        }
        private static SpotifyClient spotify;

        private static async Task OnAuthorizationCodeReceived(object sender, AuthorizationCodeResponse response)
        {
            Log("Got auth code");
            await _server.Stop();
            Log("Set settings");

            var config = SpotifyClientConfig.CreateDefault();
            var tokenResponse = await new OAuthClient(config).RequestToken(

              new AuthorizationCodeTokenRequest(
                settings.CLIENTID, settings.CLIENTSECRET, response.Code, new Uri(settings.REDIRECTURL)
              )
            );
            Log("Create Client");




            spotify =  new SpotifyClient(tokenResponse.AccessToken);
            if(spotify == null)
            {
                ErrorReport("Error creating spotify client");
            }
            Start().Start();
            // do calls with Spotify and save token?
        }

        public class CurrentSong
        {
            public static FullTrack Data = new FullTrack();
            public static bool Playing;
            public static CurrentlyPlaying Object = new CurrentlyPlaying();
            public static bool IsNull;
        }

        public class Chars
        {
            public static string play = @"▶";
            public static string pause = @"⏸";
            public static string Explicit = @"🅴";
            public static string Shaded = @"█";
            public static string UnShaded = @"░";
            public static string Note = @"🎵";

        }

        private static async void Update()
        {
            Log("Start update thread");

            for (; ; )
            {



                var npReq = new PlayerCurrentlyPlayingRequest();
                var track = await spotify.Player.GetCurrentlyPlaying(npReq);
                if (track != null)
                {

                     var item =  (FullTrack)track.Item;
                    CurrentSong.Data = item;
                    CurrentSong.Playing = track.IsPlaying;
                    CurrentSong.Object = track;
                    lastplayed = $"{Chars.Note}{CurrentSong.Data.Name} - {CurrentSong.Data.Artists[0].Name}";
                    Thread.Sleep(1000);
                    CurrentSong.IsNull = false;

                }
                else
                {
                    CurrentSong.IsNull = true;

                    Thread.Sleep(1000);
                }

            }


        }

        private static void WriteUtf8String(UDPSender sender, string data)
        {
            var utf8String = Encoding.Unicode.GetBytes(data);
            Array.Resize(ref utf8String, utf8String.Length + (4 - utf8String.Length % 4));

            for (int i = 3; i < utf8String.Length; i += 4)
            {
                int data1 =
                    utf8String[i - 3] << 8 * 3
                    | utf8String[i - 2] << 8 * 2
                    | utf8String[i - 1] << 8 * 1
                    | utf8String[i - 0] << 8 * 0;
                sender.Send(new SharpOSC.OscMessage("/chatbox/input", data1, true, false));
            }
        }

        private static string Symbol(bool condition, string tru, string fal)
        {
            if(condition == true)
            {
                return tru;
            }
            else
            {
                return fal;

            }
        }

        private static void RestartApp()
        {
            Process.Start("VrcOSCWorking.exe");
            Environment.Exit(0);
        }
        public static string gettokenexperation(DateTime endTime)
        {
            if(endTime < DateTime.UtcNow)
            {
                RestartApp();
            }
            return (endTime - DateTime.UtcNow).ToString("mm\\:ss");
        }


        private static async Task Start()
        {

            Console.Clear();
            Log("Welcome to VrcOSC");
            Log("Connecting to osc...");
            Log($"OSC: IP {ip}  PORT {port}");
            var sender = new SharpOSC.UDPSender(ip, port);
            Log("Connecting to spotify....");
            //var npReq = new PlayerCurrentlyPlayingRequest();

            //var track = await spotify.Player.GetCurrentlyPlaying(npReq);
            //Log("Starting Namechange");

            new Thread(Update).Start();
            int stage = 1;
            int stages = 2;
            Thread.Sleep(1000);

            var start = DateTime.UtcNow; // Use UtcNow instead of Now
            var endTime = start.AddMinutes(60); //endTime is a member, not a local variable

            for (; ; )
            {
                try
                {

                    if (CurrentSong.IsNull)
                    {
                        Console.Clear();
                        Console.WriteLine($"Token refreshes in {gettokenexperation(endTime)}");

                        Console.WriteLine($"Stage {stage}/{stages}");
                        Console.WriteLine($"-----------------------");
                        switch (stage)
                        {



                            case 1:

                                ChatMsg(sender, new string[] { $"Open spotify to start", "-VrcOsc-" });
                                break;

                            case 2:

                                if (lastplayed == null)
                                {
                                    ChatMsg(sender, new string[] { $"VrcOsc", "A free vrchat OSC tool"});

                                }
                                else
                                {
                                    ChatMsg(sender, new string[] { $"Last played", lastplayed });

                                }


                                break;
                            default:
                                ErrorReport($"Error loading stage {stage} of {stages}");
                                ChatMsg(sender, new string[] { $"Error loading stage {stage} of {stages}", "-VrcOsc-" });
                                break;

                        }
                        Thread.Sleep(2000);

                    }
                    else
                    {
                        for (int a = 0; a < UPDATES; a++)
                        {
                            Console.Clear();
                            Console.WriteLine($"Token refreshes in {gettokenexperation(endTime)}");
                            Console.WriteLine($"Stage {stage}/{stages} (Update {a})");
                            Console.WriteLine($"-----------------------");


                            switch (stage)
                            {

                                

                                case 1:

                                    ChatMsg(sender, new string[] { $"{Symbol(CurrentSong.Playing, Chars.play, Chars.pause)}      {CurrentSong.Data.Name}", $"{CurrentSong.Data.Artists[0].Name}" });
                                    break;

                                case 2:
                                    //byte[] utf8Bytes = System.Text.Encoding.Unicode.GetBytes("♪");


                                    //sender.Send(new SharpOSC.OscMessage("/chatbox/input", $"{utf8Bytes}", true, false ));
     
                                    int bars = 6;
                                    float calculated = MathF.Round(bars * (float)((decimal)CurrentSong.Object.ProgressMs / CurrentSong.Data.DurationMs));
                                    string barstring = "";

                                    for (int i = 0; i < bars; i += 1)
                                    {
                                        if (i <= calculated)
                                        {
                                            barstring += Chars.Shaded;
                                        }
                                        else
                                        {
                                            barstring += Chars.UnShaded;
                                        }
                                    }
                                    ChatMsg(sender, new string[] { $"{TimeSpan.FromMilliseconds((double)CurrentSong.Object.ProgressMs).ToString(@"mm\:ss")} [{barstring}] {TimeSpan.FromMilliseconds((double)CurrentSong.Data.DurationMs).ToString(@"mm\:ss")}", $"{Symbol(CurrentSong.Data.Explicit, Chars.Explicit, " ")} {CurrentSong.Data.Album.Name}" });
                                    //sender.Send(new SharpOSC.OscMessage("/chatbox/input", "♪"));
                                    break;
                                //case 3:
                                //    ChatMsg(sender, new string[] { $"Popularity: {CurrentSong.Data.Popularity}/100", $"{CurrentSong.Data.Type}", $"Experation {gettokenexperation(endTime)}" });
                                //    break;

                                default:
                                    ErrorReport($"Error loading stage {stage} of {stages}");
                                    ChatMsg(sender, new string[] { $"Error loading stage {stage} of {stages}", "-VrcOsc-" });

                                    // code block
                                    break;

                            }
                            Thread.Sleep(2000);

                        }
                    }

                }
                catch (Exception ex)
                {
                    ErrorReport(ex.ToString());
                }
                stage++;
                if(stage > stages)
                {
                    stage = 1;
                }

            }

            //if (track.Item.GetType() == typeof(FullTrack))
            //{
            //    var item = (FullTrack)track.Item;
            //    ChatMsg(sender, item.Name);

            //}



        }
        private static bool completesettings = true;

        private static void checkvar(string var, string str)
        {
            if(str == "" || str == "0" || str.Length < 1)
            {
                Log($"{var} is null");
                completesettings =  false;
            }

        }

        private static bool Setup()
        {
            string path  = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + "\\";
            string settingspath = path + "Settings.json";
            if (File.Exists(settingspath))
            {
                
                Log("Found settings");
                string readText = File.ReadAllText(settingspath);
                if (readText.Length < 1)
                {
                    File.Delete(settingspath);
                    Setup();
                }
                OSCSettings deserializedProduct = JsonConvert.DeserializeObject<OSCSettings>(readText);
                checkvar("CLIENTID", deserializedProduct.CLIENTID);
                checkvar("CLIENTSECRET", deserializedProduct.CLIENTSECRET);
                checkvar("REDIRECTURL", deserializedProduct.REDIRECTURL);
                checkvar("REDIRECTPORT", deserializedProduct.REDIRECTPORT.ToString());
                checkvar("USEBROWSERTOOL", deserializedProduct.USEBROWSERTOOL.ToString());

                if (completesettings == false)
                {
                    Log("Incomplete settings, open settings.json and fill out the required information with your spotify app info");
                    return false;

                }
                else
                {
                    settings.CLIENTID = deserializedProduct.CLIENTID;
                    settings.CLIENTSECRET = deserializedProduct.CLIENTSECRET;
                    settings.REDIRECTURL = deserializedProduct.REDIRECTURL;
                    settings.REDIRECTPORT = deserializedProduct.REDIRECTPORT;
                    settings.USEBROWSERTOOL = deserializedProduct.USEBROWSERTOOL;

                    return true;

                }
            }
            else
            {
                string jsonpath = settingspath;
                var fs = File.Create(jsonpath);
                fs.Close();
                using (StreamWriter writer = new StreamWriter(jsonpath))
                {
                    Log("Settings null, create");
                    OSCSettings tempate = new OSCSettings()
                    {
                        CLIENTID = "",
                        CLIENTSECRET = "",
                        REDIRECTURL = "http://localhost:5000/callback",
                        REDIRECTPORT = 5000,
                        USEBROWSERTOOL = true,
                    };
                    string json = JsonConvert.SerializeObject(tempate, Formatting.Indented);

                    writer.Write(json);
                    writer.Close();
                    Setup();
                }
                return false;

            }
        }


        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            Console.OutputEncoding = System.Text.Encoding.UTF8;


            Console.Title = "VrcOSC By Dragon";



            if (Setup())
            {
                try
                {
                    StartServer();
                    for (; ; )
                    {
                        Thread.Sleep(100);
                    }
                }
                catch (Exception ex)
                {
                    ErrorReport(ex.ToString());
                }
            }
            Thread.Sleep(10000);

        }






        static void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            ErrorReport(e.ToString());
            Log("Restarting in 5 seconds due to fatal error...");
            Thread.Sleep(5000);
            RestartApp();

        }
    }
}


