using LiteDB;
using Newtonsoft.Json;
using RageCoop.Core;
using JsonReader = Newtonsoft.Json.JsonReader;
using JsonSerializer = Newtonsoft.Json.JsonSerializer;
using JsonWriter = Newtonsoft.Json.JsonWriter;

namespace RageCoop.Resources.Management
{
	public class ManagementStore : IDisposable
	{
		private readonly LiteDatabase _db;
		private readonly ILiteCollection<Member> _members;
		private readonly ILiteCollection<BanRecord> _banned;
		public Config Config { get; set; }
		public ManagementStore(string dataFolder, Logger logger)
		{
			var configPath = Path.Combine(dataFolder, "Config.json");
			if (!File.Exists(configPath))
			{
				Config = new Config();
				File.WriteAllText(configPath, JsonConvert.SerializeObject(Config, Formatting.Indented));
			}
			else
			{
				try
				{
					Config = JsonConvert.DeserializeObject<Config>(File.ReadAllText(configPath));
				}
				catch (Exception ex)
				{
					logger.Warning("Failed to parse Config.json, overwritting with default values");
					logger.Error(ex);
					Config = new Config();
					File.WriteAllText(configPath, JsonConvert.SerializeObject(Config, Formatting.Indented));
				}
			}
			if (File.Exists(Path.Combine(dataFolder, "Members.db")))
			{
				logger?.Warning($"You're using legacy databse system that's no longer supported, please migrate your data and delete \"{Path.Combine(dataFolder, "Members.db")}\"");
			}
			_db = new LiteDatabase(@$"Filename={Path.Combine(dataFolder, "ManagementStore.db")}; Connection=Shared;");
			_members = _db.GetCollection<Member>();
			_banned = _db.GetCollection<BanRecord>();
			if (_members.Count() == 0)
			{
				UpdateMember("Sausage", "FB6F796DFD477E6361A19057EE0235E88820060EC786343229B4D839C01B47C3", "Admin");
			}
		}
		public string GetBanned(string ip)
		{
			return _banned.Query().Where(x => x.Address == ip).FirstOrDefault()?.Reason;
		}
		public Member GetMember(string name)
		{
			return _members.Query().Where(x => x.Username.ToLower() == name.ToLower()).FirstOrDefault();
		}
		public bool UpdateMember(string username, string passHash, string role)
		{
			var m = new Member() { Username = username.ToLower(), PassHash = passHash, Role = role };
			if (_members.Update(m))
			{
				return true;
			}
			_members.Insert(m);
			return false;
		}
		public bool SetRole(string username, string role)
		{
			var m = _members.Query().Where(x => x.Username.ToLower() == username.ToLower()).FirstOrDefault();
			if (m != null)
			{
				m.Role = role;
				_members.Update(m);
				return true;
			}
			return false;
		}
		public bool RemoveMember(string username)
		{
			return _members.Delete(_members.FindOne(x => x.Username.ToLower() == username.ToLower()).Id);
		}
		public bool Ban(string ip, string username, string reason = "Unspecified")
		{

			var m = _banned.FindOne(x => x.Address == ip);
			if (m != null)
			{
				m.Username = username.ToLower();
				m.Reason = reason;
				_banned.Update(m);
				return false;
			}
			else
			{
				_banned.Insert(new BanRecord() { Username = username.ToLower(), Address = ip, Reason = reason });
				return true;
			}
		}
		public void Unban(string ipOrUserName, out List<string> unbanned)
		{
			unbanned = new List<string>();
			var records = _banned.Find(x => x.Username.ToLower() == ipOrUserName.ToLower() || x.Address.ToLower() == ipOrUserName.ToLower());
			foreach (var record in records)
			{
				_banned.Delete(record.Id);
				unbanned.Add(record.Username);
			}
		}

		public void Dispose()
		{
			_db.Commit();
			_db.Dispose();
		}
	}
	public class Member
	{
		public int Id { get; set; }

		public string Username { get; set; }
		public string PassHash { get; set; }
		public string Role { get; set; }
	}
	public class BanRecord
	{
		public int Id { get; set; }

		public string Username { get; set; }
		public string Address { get; set; }
		public string Reason { get; set; }
	}

	[Flags]
	public enum PermissionFlags : ulong
	{
		None = 0,
		Kick = 1 << 1,
		Ban = 1 << 2,
		Register = 1 << 3,
		All = Kick | Ban | Register
	}
	public class Config
	{
		public bool AllowGuest = true;
		/// <summary>
		/// The role to assign to a newly registered user
		/// </summary>
		public string DefaultRole = "User";
		public readonly Dictionary<string, Role> Roles = new()
		{
			{ "Admin", new Role() { Permissions=PermissionFlags.All, CommandFilteringMode=1 } },
			{ "User", new Role() { Permissions=PermissionFlags.Register, CommandFilteringMode=1 } },
			{ "Guest", new Role() { Permissions=PermissionFlags.Register, CommandFilteringMode=0, WhiteListedCommands=new(){"register"} } }
		};
	}
	public class Role
	{
		[JsonConverter(typeof(PermissionConverter))]
		public PermissionFlags Permissions { get; set; } = PermissionFlags.None;
		/// <summary>
		/// 0:whitelist (block all by default), 1:blacklist (allow all by default).
		/// </summary>
		public byte CommandFilteringMode = 1;
		public HashSet<string> WhiteListedCommands { get; set; } = new HashSet<string>();
		public HashSet<string> BlackListedCommands { get; set; } = new HashSet<string>();

	}
	public class PermissionConverter : JsonConverter
	{
		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			writer.WriteValue(((PermissionFlags)value).ToString());
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			return Enum.Parse(typeof(PermissionFlags), reader.ReadAsString());
		}

		public override bool CanRead
		{
			get { return false; }
		}

		public override bool CanConvert(Type objectType)
		{
			return objectType == typeof(PermissionFlags);
		}
	}
}