using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Reflection;
using System.Linq;
using System;
using System.Data;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Libraries;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("Death Notes", "LaserHydra", "4.0.3", ResourceId = 819)]
    [Description("Description")]
    class DeathNotes : RustPlugin
    {
		#region Class
		class KillData
		{
			public string attacker;
			public string victim;
			public int distance;
			public string weapon;
			public string bodypart;
			public string damage;
			public string message;
			public string attachments;
			
			public KillData()
			{
			}
		}
		#endregion
		
		#region Setup
		List<string> metabolismTypes = new List<string>();										//	Metabolism types
		List<string> playerDamageTypes = new List<string>();									//	Player Damage types
		List<string> barricadeDamageTypes = new List<string>();									//	Barricade Damage types
		Dictionary<string, string> traps = new Dictionary<string, string>();					//	Trap types
		Dictionary<string, string> attackerNames = new Dictionary<string, string>(); 			//	Attacker names
		Dictionary<BasePlayer, HitInfo> lastHitInfo = new Dictionary<BasePlayer, HitInfo>();	//	Save last HitInfo
		
		private readonly WebRequests webRequests = Interface.GetMod().GetLibrary<WebRequests>("WebRequests");	//	Initialize WebRequests
		
		bool debugging = false;																	//	Enable Debugging?
		
		string prefix;																			//	Initialize Prefix
		string prefixcolor;																		//	Initialize Prefixcolor
		string account;																			//	Initialize Account
		
		string codelocation = "Unknown Position";												//	What code is the plugin currently running?
		
		[PluginReference]
        Plugin PopupNotifications;
		
		//	On plugin Loaded
		void Loaded()	
		{
			UpdateConfig();			//	Update the config file
			
			prefix = Config["Settings", "Prefix"].ToString();																							//	Initialize Prefix
			prefixcolor = Config["Colors", "Prefix"].ToString();																						//	Initialize Prefixcolor
			account = Config["Settings", "Plugin Icon"].ToString();																						//	Initialize Prefixcolor
			
			metabolismTypes = new List<string>{"Drowned", "Heat", "Cold", "Thirst", "Poison", "Hunger", "Radiation", "Bleeding", "Fall", "Generic"};	//	Initialize Metabolism Types
			playerDamageTypes = new List<string>{"Slash", "Blunt", "Stab", "Bullet"};																	//	Initialize Player Damage Types
			barricadeDamageTypes  = new List<string>{"Slash", "Stab"};																					//	Initialize Barricade Damage Types
			traps = new Dictionary<string, string>()																									//	Initialize Trap Types
			{
				{"Landmine", "Landmine"},
				{"Beartrap","Bear Trap"},
				{"Floor_spikes", "Floor Spike Trap"}
			};
			
			attackerNames = new Dictionary<string, string>()																							//	Initialize Trap Types
			{
				{"Barricade.woodwire", "Wired Wooden Barricade"},
				{"Wall.external.high.wood", "High External Wooden Wall"},
				{"Barricade.wood", "Wooden Barricade"},
				{"Barricade.metal", "Metal Barricade"},
				{"Gates.external.high.wood", "High External Wooden Gate"},
				{"Wall.external.high.stone", "High External Stone Wall"}
			};
			
			if(PopupNotifications == null && (bool) Config["Settings", "Use Popup Notifications"])
			{
				Puts("Popup Notifications can only be used if the PopupNotifications plugin is installed! Get it here: http://oxidemod.org/plugins/popup-notifications.1252/");
			}
		}
		#endregion
		
		#region Config
		//	On new config generated
		void LoadDefaultConfig()
		{
			Puts("No config found! Generating new config...");	//	Tell that new config is being generated
			UpdateConfig();
			Config.Save();
			Config.Load();										//	Reload the config to prevent failures at loading the new config.
		}
		
		//	Update Config
		void UpdateConfig()
		{
			//  Settings
            SetConfig("Settings", "Prefix", "DEATH NOTES<color=white>:</color> ");
            SetConfig("Settings", "Broadcast To Console", true);
			SetConfig("Settings", "Broadcast To Chat", true);
			SetConfig("Settings", "Use Popup Notifications", false);
            SetConfig("Settings", "Show Suicides", true);
            SetConfig("Settings", "Show Metabolism Deaths", true);
            SetConfig("Settings", "Show Explosion Deaths", true);
            SetConfig("Settings", "Show Trap Deaths", true);
            SetConfig("Settings", "Show Animal Deaths", false);
            SetConfig("Settings", "Show Barricade Deaths", true);
            SetConfig("Settings", "Show Player Kills", true);
            SetConfig("Settings", "Show Animal Kills", true);
            SetConfig("Settings", "Message In Radius", false);
            SetConfig("Settings", "Message Radius", 300);
            SetConfig("Settings", "Plugin Icon", "76561198206240711");

			//  Animals
            SetConfig("Animals", "Stag", "Stag");
            SetConfig("Animals", "Boar", "Boar");
            SetConfig("Animals", "Bear", "Bear");
            SetConfig("Animals", "Chicken", "Chicken");
            SetConfig("Animals", "Wolf", "Wolf");
            SetConfig("Animals", "Horse", "Horse");
			
            //  Colors
            SetConfig("Colors", "Message", "#E0E0E0");
            SetConfig("Colors", "Prefix", "grey");
            SetConfig("Colors", "Animal", "#4B75FF");
            SetConfig("Colors", "Bodypart", "#4B75FF");
            SetConfig("Colors", "Weapon", "#4B75FF");
            SetConfig("Colors", "Victim", "#4B75FF");
            SetConfig("Colors", "Attacker", "#4B75FF");
            SetConfig("Colors", "Distance", "#4B75FF");

            //  Messages
			SetConfig("Messages", "Radiation", new List<string>{"{victim} did not know that radiation kills."});
            SetConfig("Messages", "Hunger", new List<string>{"{victim} starved to death."});
            SetConfig("Messages", "Thirst", new List<string>{"{victim} died dehydrated."});
            SetConfig("Messages", "Drowned", new List<string>{"{victim} thought he could swim."});
            SetConfig("Messages", "Cold", new List<string>{"{victim} froze to death."});
            SetConfig("Messages", "Heat", new List<string>{"{victim} burned to death."});
            SetConfig("Messages", "Fall", new List<string>{"{victim} fell to his death."});
            SetConfig("Messages", "Bleeding", new List<string>{"{victim} bled to death."});
            SetConfig("Messages", "Explosion", new List<string>{"{victim} got blown up by {attacker}'s {weapon}."});
			SetConfig("Messages", "Explosion Sleep", new List<string>{"{victim} was dreaming while he got blown up by {attacker}'s {weapon}."});
            SetConfig("Messages", "Poison", new List<string>{"{victim} died by poison."});
            SetConfig("Messages", "Suicide", new List<string>{"{victim} committed suicide."});
            SetConfig("Messages", "Generic", new List<string>{"{victim} died."});
			SetConfig("Messages", "Unknown", new List<string>{"{victim} died by an unknown reason."});
            SetConfig("Messages", "Trap", new List<string>{"{victim} stepped on a {attacker}."});
            SetConfig("Messages", "Barricade", new List<string>{"{victim} died stuck on a {attacker}."});
            SetConfig("Messages", "Stab", new List<string>{"{attacker} stabbed {victim} to death with a {weapon} and hit the {bodypart}."});
            SetConfig("Messages", "Stab Sleep", new List<string>{"{attacker} stabbed {victim}, while he slept."});
            SetConfig("Messages", "Slash", new List<string>{"{attacker} sliced {victim} into pieces with a {weapon} and hit the {bodypart}."});
            SetConfig("Messages", "Slash Sleep", new List<string>{"{attacker} stabbed {victim}, while he slept."});
            SetConfig("Messages", "Blunt", new List<string>{"{attacker} killed {victim} with a {weapon} and hit the {bodypart}."});
            SetConfig("Messages", "Blunt Sleep", new List<string>{"{attacker} killed {victim} with a {weapon}, while he slept."});
            SetConfig("Messages", "Bullet", new List<string>{"{attacker} killed {victim} with a {weapon}, hitting the {bodypart} from {distance}m."});
            SetConfig("Messages", "Bullet Sleep", new List<string>{"{attacker} killed {victim}, while sleeping. (In the {bodypart} with a {weapon}, from {distance}m)"});
            SetConfig("Messages", "Arrow", new List<string>{"{attacker} killed {victim} with an arrow at {distance}m, hitting the {bodypart}."});
            SetConfig("Messages", "Arrow Sleep", new List<string>{"{attacker} killed {victim} with an arrow from {distance}m, while he slept."});
            SetConfig("Messages", "Bite", new List<string>{"A {attacker} killed {victim}."});
            SetConfig("Messages", "Bite Sleep", new List<string>{"A {attacker} killed {victim}, while he slept."});
            SetConfig("Messages", "Animal Death", new List<string>{"{attacker} killed a {victim} with a {weapon} from {distance}m."});
			SetConfig("Messages", "Helicopter", new List<string>{"{victim} was shot down by a {attacker}."});
		}
		#endregion
		
		#region Commands
		[ChatCommand("deathnotes")]
		void DeathnotesInfo(BasePlayer player, string cmd, string[] args)
		{
			ShowInfo(player);
		}
		#endregion
		
		#region On Death
		//	On Entity died / Call DeathNote
		void OnEntityDeath(BaseCombatEntity vic, HitInfo hitInfo)
		{
			if(!HasNeeded(vic, hitInfo))		//	Does death contain needed info?
			{
				hitInfo = TryGetLastHit(vic);	//	Try to get the last availiable info
			}
			
			KillData data = new KillData();																				//	Initialize new KillData
			data.attacker = GetAttacker(hitInfo);																		//	Save Attacker in KillData
			data.victim = GetVictim(vic);																				//	Save Victim in KillData
			data.distance = GetDistance(vic, hitInfo);																	//	Save Distance in KillData
			data.weapon = GetWeapon(hitInfo);																			//	Save Weapon in KillData
			data.bodypart = GetBodypart(hitInfo);																		//	Save Bodypart in KillData
			data.damage = FirstUpper(vic.lastDamage.ToString() ?? "Unknown Damage");									//	Save Damage Type in KillData
			data.attachments = GetAttachments(hitInfo);																	//	Save Attachments in KillData
			data.message = GetDeathType(hitInfo, data.damage, data.attacker, vic?.ToPlayer()?.IsSleeping() ?? false);	//	Save Death Type in KillData
			if((Config["Animals"] as Dictionary<string, object>).ContainsKey(data.victim)) data.message = "Animal Death";
			
			BroadcastDeath(data, hitInfo?.Initiator);
		}
		#endregion
		
		#region Checking
		bool HasNeeded(BaseCombatEntity vic, HitInfo hitInfo)
		{
			if(vic == null) return false;																		//	Stop here if victim does not exist
			if(hitInfo == null && metabolismTypes.Contains(vic.lastDamage.ToString()) == false) return false;	// 	Stop here if hitInfo does not exist and death is not metabolism
			
			return true;	//	Else return true
		}
		#endregion
		
		#region Get Data
		void OnEntityTakeDamage(BaseCombatEntity vic, HitInfo hitInfo)
		{
			if(vic == null || hitInfo == null || vic.ToPlayer() == null) return;
			
			lastHitInfo[vic.ToPlayer()] = hitInfo;
		}
		
		HitInfo TryGetLastHit(BaseCombatEntity vic)
		{
			if(vic == null || vic.ToPlayer() == null) return null;
			if(lastHitInfo.ContainsKey(vic.ToPlayer())) return lastHitInfo[vic.ToPlayer()];
			return null;
		}
		
		string GetAttachments(HitInfo hitInfo)
		{
			string attachments = "";
			
			if (hitInfo?.Weapon?.GetItem()?.contents?.itemList != null)
			{
				List<string> contents = new List<string>();
				foreach (var content in hitInfo?.Weapon?.GetItem().contents?.itemList as List<Item>)
				{
					contents.Add(content?.info?.displayName?.english);
				}
				
				attachments = ListToString(contents, 0, " | ");
			}
			
			if(string.IsNullOrEmpty(attachments)) attachments = string.Empty;
			else attachments = $" ({attachments})";
			
			return attachments;
		}
		
		string GetDeathType(HitInfo hitInfo, string dmg, string attacker, bool sleeping)
		{
			string msg = "";
			
			//	Is it Suicide or Metabolism?
			if(dmg == "Suicide" && (bool)Config["Settings", "Show Suicides"])
			{
				msg = dmg;
				DebugMsg("Death by suicide");
			}
			if(metabolismTypes.Contains(dmg) && (bool)Config["Settings", "Show Metabolism Deaths"])
			{
				msg = dmg;
				DebugMsg("Death by metabolism");
			}
			
			if(hitInfo != null)
			{
				//	Is Attacker a Player?
				if(hitInfo.Initiator != null && hitInfo.Initiator.ToPlayer() != null && playerDamageTypes.Contains(dmg) && hitInfo.WeaponPrefab.ToString().Contains("grenade") == false && hitInfo.WeaponPrefab.ToString().Contains("survey") == false)
				{
					DebugMsg("Death by player");
					if(hitInfo.WeaponPrefab.ToString().Contains("hunting") || hitInfo.WeaponPrefab.ToString().Contains("bow"))
					{
						msg = "Arrow";
					}
					else
					{
						msg = dmg;
					}
				}
				//	Is Attacker a explosive?
				else if(hitInfo.WeaponPrefab != null || dmg == "Explosion" && (bool)Config["Settings", "Show Explosion Deaths"])
				{
					DebugMsg("Death by explosion");
					
					if(traps.ContainsValue(attacker)) msg = "Trap";
					else msg = "Explosion";
				}
				//	Is Attacker a trap?
				else if(traps.ContainsValue(attacker) && (bool)Config["Settings", "Show Trap Deaths"])
				{
					DebugMsg("Death by trap");
					msg = "Trap";
				}
				//	Is Attacker a Barricade?
				else if(barricadeDamageTypes.Contains(dmg) && (bool)Config["Settings", "Show Barricade Deaths"])
				{
					DebugMsg("Death by barricade");
					msg = "Barricade";
				}
				//	Is Attacker an Animal?
				else if(dmg == "Bite" && (bool)Config["Settings", "Show Animal Kills"])
				{
					DebugMsg("Death by animal");
					msg = "Bite";
				}
			}
			
			if(sleeping) msg = msg + " Sleep";
			if(attacker.Contains("helicopter")) msg = "Helicopter";
			
			return msg;
		}
		
		string GetAttacker(HitInfo hitInfo)
		{
			string attacker = "Unknown Attacker";
			BaseEntity attackEntity = hitInfo?.Initiator;
			
			if(attackEntity != null)
			{
				if(attackEntity.ToPlayer() != null)
				{
					attacker = attackEntity.ToPlayer().displayName;
				}
				else
				{
					attacker = FirstUpper(hitInfo?.Initiator?.LookupShortPrefabName() ?? "Unknown Attacker");
					attacker = attacker.Replace(".prefab", "");
					
					if(traps.ContainsKey(attacker)) attacker = traps[attacker];
					if(attackerNames.ContainsKey(attacker)) attacker = attackerNames[attacker];
				}
			}
			
			return attacker;
		}
		
		string GetVictim(BaseCombatEntity vic)
		{
			string victim = "Unknown Victim";
			
			if(vic != null)
			{
				if(vic.ToPlayer() != null)
				{
					victim = vic.ToPlayer().displayName;
				}
				else
				{
					victim = FirstUpper(vic.LookupShortPrefabName() ?? "Unknown Victim");
					victim = victim.Replace(".prefab", "");
				}
			}
			
			return victim;
		}		
		int GetDistance(BaseCombatEntity vic, HitInfo hitInfo)
		{
			float distance = 0;
			
			distance = Vector3.Distance(vic.transform.position, hitInfo?.Initiator?.transform.position ?? vic.transform.position);
			
			return Convert.ToInt32(distance);
		}
		
		string GetWeapon(HitInfo hitInfo)
		{
			string weapon = "Unknown Weapon";

			weapon = hitInfo?.Weapon?.GetItem()?.info?.displayName?.english ?? "Unknown Weapon";
			
			if(hitInfo != null && hitInfo.WeaponPrefab != null && hitInfo.WeaponPrefab.ToString().Contains("f1") && hitInfo.WeaponPrefab.ToString().Contains("grenade")) weapon = "F1 Grenade";
			else if(hitInfo != null && hitInfo.WeaponPrefab != null && hitInfo.WeaponPrefab.ToString().Contains("beancan") && hitInfo.WeaponPrefab.ToString().Contains("grenade")) weapon = "Beancan Grenade";
			else if(hitInfo != null && hitInfo.WeaponPrefab != null && hitInfo.WeaponPrefab.ToString().Contains("timed") && hitInfo.WeaponPrefab.ToString().Contains("explosive")) weapon = "Timed Explosive Charge";
			else if(hitInfo != null && hitInfo.WeaponPrefab != null && hitInfo.WeaponPrefab.ToString().Contains("rocket")) weapon = "Rocket Launcher";
			else if(hitInfo != null && hitInfo.WeaponPrefab != null && hitInfo.WeaponPrefab.ToString().Contains("survey") && hitInfo.WeaponPrefab.ToString().Contains("charge")) weapon = "Survey Charge";
			else if(hitInfo != null && hitInfo.WeaponPrefab != null && hitInfo.WeaponPrefab.ToString().Contains("stone") && hitInfo.WeaponPrefab.ToString().Contains("spear")) weapon = "Stone Spear";
			else if(hitInfo != null && hitInfo.WeaponPrefab != null && hitInfo.WeaponPrefab.ToString().Contains("wood") && hitInfo.WeaponPrefab.ToString().Contains("spear")) weapon = "Wooden Spear";
			
			SetConfig("Weapons", weapon, weapon);
			SaveConfig();
			
			return Config["Weapons", weapon].ToString();
		}
		
		string GetBodypart(HitInfo hitInfo)
		{
			string bodypart = "Unknown Bodypart";
			
			BaseCombatEntity hitEntity = hitInfo?.HitEntity as BaseCombatEntity;
			SkeletonProperties.BoneProperty boneProperty = hitEntity?.skeletonProperties?.FindBone(hitInfo.HitBone);
			
			bodypart = boneProperty?.name?.english ?? "Unknown Bodypart";
			
			SetConfig("Bodyparts", FirstUpper(bodypart), FirstUpper(bodypart));
			SaveConfig();
			
			return FirstUpper(Config["Bodyparts", FirstUpper(bodypart)].ToString());
		}
		#endregion
		
		#region Formatting
		string FirstUpper(string s)
		{
			DebugMsg("FirstUpper(" + s + ")");
			
			if (string.IsNullOrEmpty(s)) return string.Empty;
			
			string phrase = "";
			
			foreach(string word in s.Split(' ')) phrase = phrase + char.ToUpper(word[0]) + word.Substring(1) + " ";
			
			if(phrase.EndsWith(" ")) phrase = phrase.Substring(0, phrase.Length - 1);
			
			return phrase;
		}
		
		string GetFormattedMessage(KillData data)
		{
			List<object> messages = Config["Messages", data.message] as List<object>;
			string msg = messages[UnityEngine.Random.Range(0, messages.Count)].ToString();
			
			msg = msg.Replace("{victim}", $"<color={Config["Colors", "Victim"].ToString()}>{data.victim}</color>");
			msg = msg.Replace("{attacker}", $"<color={Config["Colors", "Attacker"].ToString()}>{data.attacker}</color>");
			msg = msg.Replace("{distance}", $"<color={Config["Colors", "Distance"].ToString()}>{data.distance}</color>");
			msg = msg.Replace("{weapon}", $"<color={Config["Colors", "Weapon"].ToString()}>{data.weapon}{data.attachments}</color>");
			msg = msg.Replace("{bodypart}", $"<color={Config["Colors", "Bodypart"].ToString()}>{data.bodypart}</color>");
			
			msg = $"<color={Config["Colors", "Message"].ToString()}>{msg}</color>";
			
			return msg;
		}
		#endregion
		
        #region UsefulMethods
        //--------------------------->   Player finding   <---------------------------//

		BasePlayer GetPlayer(string searchedPlayer, BasePlayer executer, string prefix)
        {
            BasePlayer targetPlayer = null;
            List<string> foundPlayers = new List<string>();
            string searchedLower = searchedPlayer.ToLower();
            
			foreach(BasePlayer player in BasePlayer.activePlayerList)
			{
				if(player.displayName.ToLower().Contains(searchedLower)) foundPlayers.Add(player.displayName);
			}
			
			switch(foundPlayers.Count)
			{
				case 0:
					SendChatMessage(executer, prefix, "The Player can not be found.");
					break;
					
				case 1:
					targetPlayer = BasePlayer.Find(foundPlayers[0]);
					break;
				
				default:
					string players = ListToString(foundPlayers, 0, ", ");
					SendChatMessage(executer, prefix, "Multiple matching players found: \n" + players);
					break;
			}
			
            return targetPlayer;
        }

        //---------------------------->   Converting   <----------------------------//

        string ListToString(List<string> list, int first, string seperator)
        {
            return String.Join(seperator, list.Skip(first).ToArray());
        }

        //------------------------------>   Config   <------------------------------//

        void SetConfig(string GroupName, string DataName, object Data)
        {
            Config[GroupName, DataName] = Config[GroupName, DataName] ?? Data;
        }
		
		//---------------------------->   WebRequests   <----------------------------//
		
		void ShowInfo(BasePlayer player)
		{
			webRequests.EnqueueGet("http://oxidemod.org/plugins/death-notes.819/", (code, response) => VersionRecieved(code, response, player), this);
		}
		
		void VersionRecieved(int code, string response, BasePlayer player)
		{
			if (response == null || code != 200)
            {
                Puts("Web Request failed.");
			}
			
			string version = "0.0.0";
			Match match = new Regex(@"<h\d>Version (\d+(?:\.\d+){1,3})<\/h\d>").Match(response);
			if(match.Success) version = match.Groups[1].ToString();
			
			player.SendConsoleCommand("chat.add", account, "<size=25><color=#4B75FF>Death Notes</color></size><color=grey> by LaserHydra\nInstalled Version:</color> " + this.Version + "\n<color=grey>Latest Version:</color> " + version, 1.0);
		}
		
		//---------------------------->   Oxide Related   <----------------------------//
		
		void ReloadPlugin()
		{
			Interface.Oxide.UnloadPlugin("DeathNotes");
			Interface.Oxide.LoadPlugin("DeathNotes");
		}

		void UnloadPlugin()
		{
			Interface.Oxide.UnloadPlugin("DeathNotes");
		}		

        //---------------------------->   Chat Sending   <----------------------------//
		
		void DebugMsg(string msg)
		{
			if(debugging)
			{
				Puts($"[DEBUG] AT: {codelocation} | {msg}");
				foreach(BasePlayer player in BasePlayer.activePlayerList)
				{
					if(permission.UserHasPermission(player.userID.ToString(), "deathnotes.debug")) player.ConsoleMessage($"<color=yellow><b>[DEBUG]</b></color> <i>AT: {codelocation} | {msg}</i>");
				}
			}
		}
		
		void BroadcastDeath(KillData data, BaseEntity attacker)
        {
			if(data.message.Contains("Sleep") == false && BasePlayer.Find(data.victim) == null && (Config["Animals"] as Dictionary<string, object>).ContainsKey(data.victim) == false) return;
			else if(data.message == "Animal Death" && (bool)Config["Settings", "Show Animal Deaths"] == false) return;
			else if((data.message == "Bite" || data.message == "Bite Sleep") && (bool)Config["Settings", "Show Animal Kills"] == false) return;
			else if(data.message == "Barricade" && (bool)Config["Settings", "Show Barricade Deaths"] == false) return;
			else if((data.message == "Explosion" || data.message == "Explosion Sleep") && (bool)Config["Settings", "Show Explosion Deaths"] == false) return;
			else if(metabolismTypes.Contains(data.message) && (bool)Config["Settings", "Show Metabolism Deaths"] == false) return;
			else if((data.message == "Trap" || data.message == "Explosion Sleep") && (bool)Config["Settings", "Show Trap Deaths"] == false) return;
			else if((data.message == "Suicide" || data.message == "Explosion Sleep") && (bool)Config["Settings", "Show Suicides"] == false) return;
			else if(playerDamageTypes.Contains(data.message) && (Config["Animals"] as Dictionary<string, object>).ContainsKey(data.victim) == false && (bool)Config["Settings", "Show Player Kills"] == false) return;
			
			string msg = GetFormattedMessage(data);
			string unformatted = msg.Replace("</color>", "");
			
			var matches = new Regex(@"(<color\=.+?>)", RegexOptions.IgnoreCase).Matches(unformatted);
			foreach(Match match in matches)
			{
				if(match.Success) unformatted = unformatted.Replace(match.Groups[1].ToString(), "");
			}
			
			if((bool)Config["Settings", "Broadcast To Chat"])
			{
				if((bool)Config["Settings", "Message In Radius"])
				{
					foreach(BasePlayer player in BasePlayer.activePlayerList)
					{
						if(Vector3.Distance(player.transform.position, attacker.transform.position) <= (int)Config["Settings", "Message Radius"]);
					}
				}
				else
				{
					if((bool)Config["Settings", "Broadcast To Chat"]) ConsoleSystem.Broadcast("chat.add", account, $"<color={prefixcolor}>{prefix}</color>{msg}", 1.0);
				}
			}
			
			if((bool)Config["Settings", "Broadcast To Console"]) Puts(unformatted);
			if((bool)Config["Settings", "Use Popup Notifications"]) PopupNotifications?.Call("CreatePopupNotification", $"<color={prefixcolor}>{prefix}</color>{msg}");
        }
		
        void BroadcastChat(string prefix, string msg = null)
        {

            if (msg != null)
            {
                PrintToChat($"<color=#00FF8D>{prefix}</color>: {msg}");
            }
            else
            {
                msg = prefix;
                PrintToChat(msg);
            }
        }

        void SendChatMessage(BasePlayer player, string prefix, string msg = null)
        {
            if(msg != null)
            {
                SendReply(player, $"<color=#00FF8D>{prefix}</color>: {msg}");
            }
            else
            {
                msg = prefix;
                SendReply(player, msg);
            }
        }

        //---------------------------------------------------------------------------//
        #endregion
    }
}
