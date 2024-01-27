using GTA.Math;
using GTA.Native;
using LiteDB;
using RageCoop.Resources.Race.Objects;
using RageCoop.Server;
using RageCoop.Server.Scripting;
using System.Xml.Serialization;

namespace RageCoop.Resources.Race
{
    public class Main : ServerScript
    {
        private static List<Map> Maps;
        private static readonly Session Session = new();
        private readonly List<object> Checkpoints = new();

        private readonly XmlSerializer Serializer = new(typeof(Map));
        private static readonly Random Random = new();
        private static LiteDatabase DB;
        private static ILiteCollection<Record> Records;
        private Thread RankingThread;
        private static bool Stopping = false;

        public override void OnStart()
        {
            API.Events.OnPlayerReady += (s, c) =>
            {
                if (Session.State != State.Voting && Session.State != State.Preparing)
                    c.SendChatMessage("A race has already started, wait for the next round or use /join to join the race");
                else
                    c.SendChatMessage("The race will start soon, use /vote to vote for a map");
            };
            API.Events.OnPlayerDisconnected += (s, c) =>
            {
                Leave(c, true);
            };
            API.Events.OnPlayerUpdate += OnPlayerUpdate;

            API.RegisterCommands(this);
            API.RegisterCustomEventHandler(Events.CheckpointPassed, CheckpointPassed);
            API.RegisterCustomEventHandler(Events.Cheating, Cheating);

            Session.State = State.Voting;
            Session.Votes = new Dictionary<Client, string>();
            Session.Players = new List<Player>();
            if (GetMaps().Length == 0)
            {
                Logger.Warning("No maps found, applying default maps");
                foreach (var file in CurrentResource.Files.Values)
                {
                    if (file.Name.StartsWith("Maps") && file.Name.EndsWith(".xml") && !file.IsDirectory)
                    {
                        var target = File.Create(Path.Combine(CurrentResource.DataFolder, file.Name));
                        file.GetStream().CopyTo(target);
                        target.Close();
                        target.Dispose();
                    }
                }
            }
            Maps = GetMaps()?.Select(map => (Map)Serializer.Deserialize(new StreamReader(map))).ToList();

            if (File.Exists(Path.Combine(CurrentResource.DataFolder, "times.db")))
            {
                Logger.Warning("times.db is outdated, please convert it to new database format using the converter");
            }
            DB = new LiteDatabase(@$"Filename={Path.Combine(CurrentResource.DataFolder, "Records.db")}; Connection=Shared;");
            Records = DB.GetCollection<Record>();

            RankingThread = new Thread(() =>
            {
                while (!Stopping)
                {
                    try
                    {
                        if (Session.State == State.Started)
                        {
                            Session.Rank();
                            foreach (var p in Session.Players)
                            {
                                p.Client.SendCustomEvent(Events.PositionRanking, p.Ranking, (ushort)Session.Players.Count);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        CurrentResource.Logger.Error(ex);
                    }
                    Thread.Sleep(500);
                }
            });
            RankingThread.Start();

            CurrentResource.Logger.Info("Race resource started");
        }

        public override void OnStop()
        {
            Stopping = true;
            DB.Dispose();
            RankingThread.Join();
            CurrentResource.Logger.Info($"Race resource stopped");
        }

        private void OnPlayerUpdate(object s, Client c)
        {
            if (Session.State == State.Voting)
            {
                Session.NextEvent = DateTime.Now.AddSeconds(15);
                Session.State = State.Preparing;
                Session.Votes = new Dictionary<Client, string>();
                lock (Session.Players)
                    foreach (var player in Session.Players)
                        player.CheckpointsPassed = 0;
                API.SendChatMessage("Starting in 15 seconds, use /vote to vote for a map", null, "Server", false);
            }

            if (Session.State == State.Preparing && DateTime.Now > Session.NextEvent)
            {
                Session.State = State.Starting;

                foreach (var prop in API.Entities.GetAllProps())
                    prop.Delete();
                foreach (var vehicle in API.Entities.GetAllVehicles())
                    vehicle.Delete();

                var map = Session.Votes.GroupBy(x => x.Value).OrderByDescending(vote => vote.Count()).FirstOrDefault()?.Key ?? GetRandomMap();
                Session.Map = Maps.First(x => x.Name == map);
                API.SendChatMessage("Map: " + map);
                var record = GetRecord(map);
                if (record != null)
                    API.SendChatMessage($"Record: {TimeSpan.FromMilliseconds(record.Time):m\\:ss\\.ff} by {record.Player}");

                foreach (var prop in Session.Map.DecorativeProps)
                {
                    var p = API.Entities.CreateProp(prop.Hash, prop.Position, prop.Rotation);
                    if (prop.Texture > 0 && prop.Texture < 16)
                        API.SendNativeCall(null, Hash.SET_OBJECT_TINT_INDEX, p.Handle, prop.Texture);
                }

                Checkpoints.Clear();
                foreach (var checkpoint in Session.Map.Checkpoints)
                    Checkpoints.Add(checkpoint);

                int spawnPoint = 0;
                foreach (var client in API.GetAllClients())
                {
                    Join(client.Value, spawnPoint);
                    spawnPoint++;
                }

                API.SendChatMessage("The race is about to start, get ready", null, "Server", false);

                Task.Run(() =>
                {
                    Thread.Sleep(10000);

                    API.SendCustomEvent(null, Events.CountDown);
                    Thread.Sleep(3000);

                    lock (Session.Players)
                        foreach (var player in Session.Players)
                            player.Client.Player.LastVehicle.Freeze(false);

                    Session.State = State.Started;
                    Session.RaceStart = Environment.TickCount64;
                });
            }
        }

        public void CheckpointPassed(CustomEventReceivedArgs obj)
        {
            if (Session.State == State.Started)
            {
                Player player;
                lock (Session.Players)
                    player = Session.Players.FirstOrDefault(x => x.Client == obj.Client);
                if (player != null)
                {
                    player.CheckpointsPassed = Session.Map.Checkpoints.Length - (int)obj.Args[0];
                    if (player.CheckpointsPassed == Session.Map.Checkpoints.Length)
                    {
                        Session.State = State.Voting;

                        var time = Environment.TickCount64 - Session.RaceStart;
                        var msg = $"{player.Client.Username} finished in {TimeSpan.FromMilliseconds(time):m\\:ss\\.ff}";
                        var record = GetRecord(Session.Map.Name);
                        if (record != null && time < record.Time)
                            msg += " (new record)";
                        if (Session.Players.Count > 1)
                            msg += $" ({Wins(player.Client.Username) + 1} wins)";
                        API.SendChatMessage(msg);
                        SaveTime(Session.Map.Name, player.Client.Username, time, Session.Players.Count > 1);
                    }
                }
            }
        }

        public void Join(Client client, int spawnPoint)
        {
            Player player;
            lock (Session.Players)
            {
                player = Session.Players.FirstOrDefault(x => x.Client == client);
                if (player == null)
                {
                    player = new Player(client);
                    Session.Players.Add(player);
                }
            }

            Task.Run(() =>
            {
                try
                {
                    var cayo = Session.Map.SpawnPoints[0].Position.DistanceTo2D(new Vector2(4700f, -5145f)) < 2000f;
                    client.SendNativeCall(Hash.SET_ISLAND_ENABLED, "HeistIsland", cayo);
                    var position = Session.Map.SpawnPoints[spawnPoint % Session.Map.SpawnPoints.Length].Position;
                    var heading = Session.Map.SpawnPoints[spawnPoint % Session.Map.SpawnPoints.Length].Heading;
                    client.Player.Position = position + new Vector3(4, 0, 1);
                    player.VehicleHash = (int)Session.Map.AvailableVehicles[Random.Next(Session.Map.AvailableVehicles.Length)];
                    var vehicle = API.Entities.CreateVehicle(client, player.VehicleHash, position, heading);
                    Thread.Sleep(1000);
                    client.SendNativeCall(Hash.SET_PED_INTO_VEHICLE, client.Player.Handle, vehicle.Handle, -1);
                    client.SendNativeCall(Hash.SET_ALLOW_STREAM_HEIST_ISLAND_NODES, cayo);
                    client.SendCustomEvent(Events.StartCheckpointSequence, Checkpoints.ToArray());
                    if (Session.State == State.Started)
                    {
                        client.SendCustomEvent(Events.JoinRace);
                        API.SendChatMessage($"{client.Username} joined the race");
                    }
                    else
                        vehicle.Freeze(true);
                }
                catch (Exception ex)
                {
                    Logger.Error("[Race.Join]", ex);
                }
            });
        }

        public void Leave(Client client, bool disconnected)
        {
            if (Session.Votes.ContainsKey(client))
                Session.Votes.Remove(client);

            Player player;
            lock (Session.Players)
                player = Session.Players.FirstOrDefault(x => x.Client == client);
            if (player != null)
            {
                if (!disconnected)
                {
                    player.Client.Player.LastVehicle.Delete();
                    client.SendCustomEvent(Events.LeaveRace);
                    API.SendChatMessage($"{client.Username} left the race");
                }

                lock (Session.Players)
                    Session.Players.Remove(player);
            }

            lock (Session.Players)
                if (!Session.Players.Any())
                    Session.State = State.Voting;
        }

        public void Cheating(CustomEventReceivedArgs obj)
        {
            API.SendChatMessage($"{obj.Client.Username} is cheating", null, "Server", false);
            //obj.Client.Kick("Cheating");
        }

        [Command("vote")]
        public void Vote(CommandContext ctx)
        {
            if (Session.State != State.Voting && Session.State != State.Preparing) return;

            if (ctx.Args.Length == 0)
            {
                ctx.Client.SendChatMessage("Use /vote (map), Maps: " + string.Join(", ", Maps.Select(x => x.Name)));
                return;
            }

            if (Session.Votes.ContainsKey(ctx.Client))
            {
                ctx.Client.SendChatMessage("You already voted for this round");
                return;
            }

            var voted = Maps.FirstOrDefault(x => x.Name.ToLower() == string.Join(" ", ctx.Args.ToArray()).ToLower());
            if (voted == null)
            {
                ctx.Client.SendChatMessage("No map with that name exists");
                return;
            }

            Session.Votes.Add(ctx.Client, voted.Name);
            API.SendChatMessage($"{ctx.Client.Username} voted for {voted.Name}");
        }

        [Command("join")]
        public void Join(CommandContext ctx)
        {
            if (Session.State == State.Started && !Session.Players.Any(x => x.Client == ctx.Client))
                Join(ctx.Client, Random.Next(Session.Map.SpawnPoints.Length));
        }

        [Command("leave")]
        public void Leave(CommandContext ctx)
        {
            if (Session.State == State.Started)
                Leave(ctx.Client, false);
        }

        private string[] GetMaps()
        {
            var folder = Path.Combine(AppContext.BaseDirectory, CurrentResource.DataFolder, "Maps");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            return Directory.GetFiles(folder, "*.xml");
        }

        private static string GetRandomMap()
        {
            return Maps[Random.Next(Maps.Count)].Name;
        }

        private static void SaveTime(string race, string player, long time, bool win)
        {
            Records.Insert(new Record()
            {
                Race = race,
                Player = player,
                Time = time,
                Win = win
            });
        }

        private static Record GetRecord(string race)
        {
            return Records.Query().Where(x => x.Race == race).OrderBy(x => x.Time).Limit(1).FirstOrDefault();
        }

        private static int Wins(string player)
        {
            return Records.Query().Where(x => x.Player.ToLower() == player.ToLower() && x.Win).Count();
        }
    }
}
