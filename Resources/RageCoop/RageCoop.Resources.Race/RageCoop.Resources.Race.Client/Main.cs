using GTA;
using GTA.Math;
using GTA.Native;
using GTA.UI;
using RageCoop.Core.Scripting;
using RageCoop.Client.Scripting;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using API = RageCoop.Client.Scripting.APIBridge;
using Newtonsoft.Json;

namespace RageCoop.Resources.Race
{
    public class Main : ClientScript
    {
        private int _countdown = -1;
        private Sprite _fadeoutSprite;
        private readonly List<Vector3> _checkpoints = new List<Vector3>();
        private Blip _nextBlip = null;
        private Blip _secondBlip = null;
        private uint _raceStart;
        private uint _seconds;
        private int _lasttime = Environment.TickCount;
        private bool _isInRace = false;
        private Vector3? _lastCheckPoint;
        private Vehicle _vehicle;
        private int _vehicleHash;
        private byte _primaryColor;
        private byte _secondaryColor;
        private int _cheating = 0;
        private int _playerCount = 0;
        private int _rankingPotition = 0;
        private Settings _settings;
        private float _lastSpeed = 0;

        protected override void OnStart()
        {
            Notification.Show("Race resource loaded");

            API.RegisterCustomEventHandler(Events.CountDown, CountDown);
            API.RegisterCustomEventHandler(Events.StartCheckpointSequence, Checkpoints);
            API.RegisterCustomEventHandler(Events.JoinRace, JoinRace);
            API.RegisterCustomEventHandler(Events.LeaveRace, LeaveRace);
            API.RegisterCustomEventHandler(Events.PositionRanking, (e) => { _rankingPotition = (ushort)e.Args[0]; _playerCount = (ushort)e.Args[1]; });

            _settings = Settings.ReadSettings(Path.Combine(AppContext.BaseDirectory, CurrentResource.DataFolder, "Settings.json"));

            if (_settings.LoadMPMaps)
                Function.Call(Hash.ON_ENTER_MP);

            Aborted += (e) => { if (e.IsUnloading) Cleanup(); };
        }

        protected override void OnTick()
        {
            base.OnTick();
            var _player = Game.Player.Character;

            if (Environment.TickCount >= _lasttime + 1000)
            {
                _seconds++;
                _lasttime = Environment.TickCount;

                var veh = _player.CurrentVehicle;
                if (veh != null && _isInRace)
                {
                    if (veh.HeightAboveGround > 3f && !veh.IsInAir && !veh.IsInWater && veh.Speed == 0f)
                    {
                        _cheating++;
                        if (_cheating > 2)
                            API.SendCustomEvent(Events.Cheating);
                    }
                    else
                        _cheating = 0;

                    if (veh.Speed - _lastSpeed > 15f && veh.Speed > 30f && !veh.HasRocketBoost && !veh.IsAircraft)
                        API.SendCustomEvent(Events.Cheating);
                    _lastSpeed = veh.Speed;
                }
            }

            var res = ResolutionMaintainRatio;
            if (_countdown > -1 && _countdown <= 3)
            {
                new LemonUI.Elements.ScaledText(new Point((int)res.Width / 2, 260 * 1080 / 720), _countdown == 0 ? "GO" : _countdown.ToString())
                {
                    Alignment = Alignment.Center,
                    Scale = 2f,
                    Font = GTA.UI.Font.Pricedown,
                    Color = Color.White
                }.Draw();
                if (_fadeoutSprite?.Color.A > 2)
                {
                    _fadeoutSprite.Color = Color.FromArgb(_fadeoutSprite.Color.A - 2, _fadeoutSprite.Color.R, _fadeoutSprite.Color.G, _fadeoutSprite.Color.B);
                    _fadeoutSprite.Draw();
                }
            }

            var safe = SafezoneBounds;
            if (_isInRace)
            {
                new LemonUI.Elements.ScaledText(new Point((int)res.Width - safe.X - 180, (int)res.Height - safe.Y - 135), "Time")
                {
                    Scale = 0.3f,
                    Color = Color.White
                }.Draw();
                new LemonUI.Elements.ScaledText(new Point((int)res.Width - safe.X - 20, (int)res.Height - safe.Y - 147), FormatTime(_seconds - _raceStart))
                {
                    Alignment = Alignment.Right,
                    Scale = 0.5f,
                    Font = GTA.UI.Font.ChaletLondon,
                    Color = Color.White
                }.Draw();
                new Sprite("timerbars", "all_black_bg", new Size(150, 26), new Point((int)Screen.Width - safe.X - 145, (int)Screen.Height - safe.Y - 92), Color.FromArgb(200, 255, 255, 255)).Draw();

                if (_playerCount > 1)
                {
                    new LemonUI.Elements.ScaledText(new Point((int)res.Width - safe.X - 180, (int)res.Height - safe.Y - 180), "Position")
                    {
                        Scale = 0.3f,
                        Color = Color.White
                    }.Draw();
                    new LemonUI.Elements.ScaledText(new Point((int)res.Width - safe.X - 20, (int)res.Height - safe.Y - 192), $"{_rankingPotition}/{_playerCount}")
                    {
                        Alignment = Alignment.Right,
                        Scale = 0.5f,
                        Font = GTA.UI.Font.ChaletLondon,
                        Color = Color.White
                    }.Draw();
                    new Sprite("timerbars", "all_black_bg", new Size(150, 26), new Point((int)Screen.Width - safe.X - 145, (int)Screen.Height - safe.Y - 122), Color.FromArgb(200, 255, 255, 255)).Draw();
                }
            }

            if (_checkpoints.Count > 0)
            {
                World.DrawMarker(MarkerType.VerticalCylinder, _checkpoints[0], new Vector3(0, 0, 0), new Vector3(0, 0, 0), new Vector3(10f, 10f, 2f), Color.FromArgb(100, 241, 247, 57));
                if (_nextBlip == null)
                {
                    _nextBlip = World.CreateBlip(_checkpoints[0]);
                    _nextBlip.ShowRoute = true;
                }

                if (_checkpoints.Count > 1)
                {
                    if (_secondBlip == null)
                    {
                        _secondBlip = World.CreateBlip(_checkpoints[1]);
                        _secondBlip.Scale = 0.5f;
                        if (_checkpoints.Count == 2)
                            _secondBlip.Sprite = BlipSprite.RaceFinish;
                    }
                    Vector3 dir = _checkpoints[1] - _checkpoints[0];
                    dir.Normalize();
                    World.DrawMarker(MarkerType.ChevronUpx1, _checkpoints[0] + new Vector3(0f, 0f, 2f), dir, new Vector3(60f, 0f, 0f), new Vector3(4f, 4f, 4f), Color.FromArgb(200, 87, 193, 250));
                }
                else
                {
                    Vector3 dir = _player.Position - _checkpoints[0];
                    dir.Normalize();
                    World.DrawMarker(MarkerType.CheckeredFlagRect, _checkpoints[0] + new Vector3(0f, 0f, 2f), dir, new Vector3(0f, 0f, 0f), new Vector3(4f, 4f, 4f), Color.FromArgb(200, 87, 193, 250));
                    _nextBlip.Sprite = BlipSprite.RaceFinish;
                }

                if (_isInRace && _vehicle != null && _player.IsInVehicle(_vehicle) && _player.IsInRange(_checkpoints[0], 10f))
                {
                    Function.Call(Hash.REQUEST_SCRIPT_AUDIO_BANK, "HUD_MINI_GAME_SOUNDSET", true);
                    Function.Call(Hash.PLAY_SOUND_FRONTEND, 0, "CHECKPOINT_NORMAL", "HUD_MINI_GAME_SOUNDSET");
                    _lastCheckPoint = _checkpoints[0];
                    _checkpoints.RemoveAt(0);
                    API.SendCustomEvent(Events.CheckpointPassed, _checkpoints.Count);
                    ClearBlips();
                    if (_checkpoints.Count == 0)
                        _isInRace = false;
                }
            }

            Function.Call(Hash.DISABLE_CONTROL_ACTION, 0, (int)Control.VehicleCinCam);
            Function.Call(Hash.SET_PED_CAN_BE_KNOCKED_OFF_VEHICLE, _player, 1); // don't fall from bike
            Function.Call(Hash.SET_PED_CONFIG_FLAG, _player, 32, false); // don't fly through windshield
            Function.Call(Hash.SET_MINIMAP_HIDE_FOW, true);
            if (_player.Position.DistanceTo2D(new Vector2(4700f, -5145f)) < 2000f &&
                Function.Call<int>(Hash.GET_INTERIOR_FROM_ENTITY, _player) == 0)
            {
                Function.Call(Hash.SET_RADAR_AS_EXTERIOR_THIS_FRAME);
                Function.Call(Hash.SET_RADAR_AS_INTERIOR_THIS_FRAME, 0xc0a90510, 4700f, -5145f, 0, 0);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            if (API.IsChatFocused)
                return;
            if (Game.IsControlJustPressed(Control.VehicleCinCam))
                Respawn();
        }

        private void Respawn()
        {
            if (!_isInRace || _checkpoints.Count == 0 || !_lastCheckPoint.HasValue)
                return;
            Screen.FadeOut(1000);
            Task.Run(() =>
            {
                Thread.Sleep(1000);
                QueueAction(() =>
                {
                    var dir = _checkpoints[0] - _lastCheckPoint.Value;
                    var heading = (float)(-Math.Atan2(dir.X, dir.Y) * 180.0 / Math.PI);

                    // Stromberg will have some issues event after repair, so just spawn a new vehicle
                    var radio = Game.RadioStation;
                    _vehicle?.Delete();
                    var model = new Model(_vehicleHash);
                    model.Request(1000);
                    _vehicle = World.CreateVehicle(model, _lastCheckPoint.Value, heading);
                    model.MarkAsNoLongerNeeded();
                    if (_vehicle == null)
                    {
                        Screen.FadeIn(1000);
                        return false;
                    }
                    Function.Call(Hash.SET_VEHICLE_COLOURS, _vehicle, _primaryColor, _secondaryColor);
                    _vehicle.PlaceOnGround();
                    Game.Player.Character.SetIntoVehicle(_vehicle, VehicleSeat.Driver);
                    Game.RadioStation = radio;
                    Screen.FadeIn(1000);
                    return true;
                });
            });
        }

        private void CountDown(CustomEventReceivedArgs obj)
        {
            Task.Run(() =>
            {
                for (_countdown = 3; _countdown >= 0; _countdown--)
                {
                    QueueAction(() =>
                    {
                        var w = Convert.ToInt32(Screen.Width / 2);
                        _fadeoutSprite = new Sprite("mpinventory", "in_world_circle", new SizeF(200, 200), new PointF(w - 100, 200), _countdown == 0 ? Color.FromArgb(150, 49, 235, 126) : Color.FromArgb(150, 241, 247, 57));
                        Function.Call(Hash.REQUEST_SCRIPT_AUDIO_BANK, "HUD_MINI_GAME_SOUNDSET", true);
                        Function.Call(Hash.PLAY_SOUND_FRONTEND, 0, "CHECKPOINT_NORMAL", "HUD_MINI_GAME_SOUNDSET");
                    });
                    if (_countdown == 0)
                    {
                        StartRace();
                    }
                    Thread.Sleep(1000);
                }
            });
        }

        private void SaveVehicle()
        {
            _vehicle = Game.Player.Character.CurrentVehicle;
            if (_vehicle == null)
                return;

            _vehicleHash = _vehicle.Model.Hash;
            byte primaryColor = 0;
            byte secondaryColor = 0;
            unsafe
            {
                Function.Call<byte>(Hash.GET_VEHICLE_COLOURS, _vehicle, &primaryColor, &secondaryColor);
            }
            _primaryColor = primaryColor;
            _secondaryColor = secondaryColor;
        }

        private void Checkpoints(CustomEventReceivedArgs obj)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            QueueAction(() =>
            {
                SaveVehicle();
                if (_vehicle == null && sw.ElapsedMilliseconds < 10000)
                {
                    return false;
                }
                else
                {
                    _vehicle.PlaceOnGround();
                    sw.Stop();
                    return true;
                }
            });
            _checkpoints.Clear();
            _lastCheckPoint = null;
            foreach (var item in obj.Args)
                _checkpoints.Add((Vector3)item);
            QueueAction(ClearBlips);
            var sw2 = System.Diagnostics.Stopwatch.StartNew();
            QueueAction(() =>
            {
                if (sw2.ElapsedMilliseconds < 10000)
                {
                    Screen.ShowHelpTextThisFrame("Press ~INPUT_VEH_CIN_CAM~ to reset your vehicle");
                    return false;
                }
                else
                {
                    return true;
                }
            });
        }

        private void StartRace()
        {
            QueueAction(() => { _lastCheckPoint = _vehicle?.Position; });
            _raceStart = _seconds;
            _isInRace = true;
        }

        private void JoinRace(CustomEventReceivedArgs obj)
        {
            QueueAction(SaveVehicle);
            StartRace();
        }

        private void LeaveRace(CustomEventReceivedArgs obj)
        {
            _checkpoints.Clear();
            QueueAction(ClearBlips);
            _isInRace = false;
        }

        private void ClearBlips()
        {
            _nextBlip?.Delete();
            _secondBlip?.Delete();
            _nextBlip = null;
            _secondBlip = null;
        }

        void Cleanup()
        {
            _checkpoints.Clear();
            ClearBlips();
        }

        public string FormatTime(uint seconds)
        {
            var minutes = Convert.ToInt32(Math.Floor(seconds / 60f));
            var secs = seconds % 60;
            return string.Format("{0:00}:{1:00}", minutes, secs);
        }

        public static SizeF ResolutionMaintainRatio
        {
            get
            {
                // Get the game width and height
                int screenw = Screen.Resolution.Width;
                int screenh = Screen.Resolution.Height;
                // Calculate the ratio
                float ratio = (float)screenw / screenh;
                // And the width with that ratio
                float width = 1080f * ratio;
                // Finally, return a SizeF
                return new SizeF(width, 1080f);
            }
        }

        public static Point SafezoneBounds
        {
            get
            {
                // Get the size of the safezone as a float
                float t = Function.Call<float>(Hash.GET_SAFE_ZONE_SIZE);
                // Round the value with a max of 2 decimal places and do some calculations
                double g = Math.Round(Convert.ToDouble(t), 2);
                g = (g * 100) - 90;
                g = 10 - g;

                // Then, get the screen resolution
                int screenw = Screen.Resolution.Width;
                int screenh = Screen.Resolution.Height;
                // Calculate the ratio
                float ratio = (float)screenw / screenh;
                // And this thing (that I don't know what it does)
                float wmp = ratio * 5.4f;

                // Finally, return a new point with the correct resolution
                return new Point((int)Math.Round(g * wmp), (int)Math.Round(g * 5.4f));
            }
        }
    }

    public class Settings
    {
        public bool LoadMPMaps { get; set; } = true;

        public static Settings ReadSettings(string path)
        {
            Settings settings;
            try
            {
                settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(path));
            }
            catch
            {
                settings = new();
                File.WriteAllText(path, JsonConvert.SerializeObject(settings));
            }
            return settings;
        }
    }
}
