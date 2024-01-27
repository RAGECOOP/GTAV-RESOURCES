using Newtonsoft.Json;
using RageCoop.Client.Scripting;
using System.Collections.Generic;
using System.IO;
using API = RageCoop.Client.Scripting.APIBridge;
using GTA;

namespace RageCoop.Resources.HandlingEnforcer.Client
{
    public class Main : ClientScript
    {
        private readonly Dictionary<int, HandlingData> HandlingDatamn = new Dictionary<int, HandlingData>();
        private readonly Dictionary<GTA.HandlingData, HandlingData> ModifiedHandlings = new Dictionary<GTA.HandlingData, HandlingData>();
        protected override void OnStart()
        {
            API.RequestSharedFile("handling.json", Load);
            KeyDown += (e) =>
            {
                if (e.KeyCode == Keys.U)
                {
                    ExportAll();
                    GTA.UI.Notification.Show("handling.json exported to working directory");
                }
            };
        }

        private void Load(string s)
        {
            if (s == null)
            {

                Logger.Info("null!");
            }
            Logger.Info("Reading handling data from " + s);

            foreach (var l in File.ReadAllLines(s))
            {
                var data = JsonConvert.DeserializeObject<HandlingData>(l);
                if (!HandlingDatamn.ContainsKey(data.Hash))
                {
                    HandlingDatamn.Add(data.Hash, data);
                    Logger.Trace("loaded data:" + data.Hash);
                }
            }
            API.Events.OnVehicleSpawned += ApplyHandling;
            QueueAction(() =>
            {
                foreach (var v in GTA.World.GetAllVehicles())
                {
                    ApplyHandling(v);
                }
            });
        }
        private void ApplyHandling(object sender, RageCoop.Client.SyncedVehicle e)
        {
            ApplyHandling(e?.MainVehicle);
        }

        private void ApplyHandling(GTA.Vehicle v)
        {
            lock (ModifiedHandlings)
            {
                if (v == null) { return; }
                // Logger.Debug("Vehicle spawnd: "+e.MainVehicle.DisplayName.ToUpper());
                if (HandlingDatamn.TryGetValue(v.Model.Hash, out var data))
                {
                    var h = v.HandlingData;
                    if (!ModifiedHandlings.ContainsKey(h))
                    {
                        // Logger.Debug("Applying handling data to: "+v.DisplayName+" hash:"+data.Hash);

                        // Copy and store unmodified handling data
                        ModifiedHandlings.Add(h, new HandlingData(h, v.Model.Hash));
                        Logger.Trace(JsonConvert.SerializeObject(data, Newtonsoft.Json.Formatting.Indented));
                        Logger.Trace(JsonConvert.SerializeObject(new HandlingData(h, v.Model.Hash), Newtonsoft.Json.Formatting.Indented));

                        data.ApplyTo(h);
                    }

                }

            }
        }
        public static void ExportAll(string path = "handling.json")
        {
            using (var w = new StreamWriter(path))
            {
                foreach (var m in GTA.Vehicle.GetAllModels())
                {
                    w.WriteLine(JsonConvert.SerializeObject(new HandlingData(GTA.HandlingData.GetByVehicleModel(m), ((GTA.Model)m).Hash), Newtonsoft.Json.Formatting.None));
                }
            }
        }
        protected override void OnAborted(GTA.AbortedEventArgs args)
        {
            // Restore modified handling data
            foreach (var p in ModifiedHandlings)
            {
                p.Value.ApplyTo(p.Key);
            }
            ModifiedHandlings.Clear();
        }
    }
}