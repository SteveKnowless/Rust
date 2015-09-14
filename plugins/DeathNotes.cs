using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.Data;
using UnityEngine;
using Oxide.Core;
using Oxide.Core.Plugins;
using Oxide.Core.Libraries;
using ProtoBuf;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Death Notes", "LaserHydra", "3.3.3", ResourceId = 819)]
    [Description("Broadcasts players/animals deaths to chat")]
    class DeathNotes : RustPlugin
    {
		#region Settings
		bool debugging = false;
		string prefix = "";
		string profile = "0";
		List<string> metabolism = new List<string>();
		List<string> playerDamageTypes = new List<string>();
		List<string> barricadeDamageTypes = new List<string>();
		List<string> traps = new List<string>();
		
		[PluginReference]
        Plugin PopupNotifications;
		
		private readonly WebRequests webRequests = Interface.GetMod().GetLibrary<WebRequests>("WebRequests");
		#endregion
		
		#region Commands
		//	Toggle Debug-Mode
		[ConsoleCommand("deathnotes.debug")]
		void ToggleDebug(ConsoleSystem.Arg arg)
		{
			if(arg?.connection?.authLevel < 2) return;
			if(debugging) debugging = false;
			else debugging = true;
		}
		
		//	Info Commands
		[ChatCommand("deathnotes")]
		void DeathnotesInfo(BasePlayer player, string cmd, string[] args)
		{
			ShowInfo(player);
		}
		#endregion
		
		#region Hooks
        void Loaded()
        {
			if(!permission.PermissionExists("deathnotes.debug")) permission.RegisterPermission("deathnotes.debug", this);
			
            LoadDefaultConfig();
			
			if(!PopupNotifications)
			{
				Puts("Popup Notifications can only be used if PopupNotifications is installed! Get it here: http://oxidemod.org/plugins/popup-notifications.1252/");
			}
			
			prefix = "<color=" + Config["Colors", "Prefix"].ToString() + ">" + Config["Settings", "Prefix"].ToString() + "</color> ";
			if((bool)Config["Settings", "EnablePluginIcon"]) profile = "76561198206240711";
			metabolism = "Drowned Heat Cold Thirst Poison Hunger Radiation Bleeding Fall Generic".Split(' ').ToList();
			playerDamageTypes = "Slash Blunt Stab Bullet".Split(' ').ToList();
			barricadeDamageTypes = "Slash Stab".Split(' ').ToList();
			traps = "Landmine.prefab Beartrap.prefab Floor_spikes.prefab".Split(' ').ToList();
        }

        protected override void LoadDefaultConfig()
        {
            //  Settings
            StringConfig("Settings", "Prefix", "DEATH NOTES<color=white>:</color>");
            BoolConfig("Settings", "BroadcastToConsole", true);
			BoolConfig("Settings", "UsePopupNotifications", false);
            BoolConfig("Settings", "ShowSuicides", true);
            BoolConfig("Settings", "ShowMetabolismDeaths", true);
            BoolConfig("Settings", "ShowExplosionDeaths", true);
            BoolConfig("Settings", "ShowTrapDeaths", true);
            BoolConfig("Settings", "ShowAnimalDeaths", false);
            BoolConfig("Settings", "ShowBarricadeDeaths", true);
            BoolConfig("Settings", "ShowPlayerKills", true);
            BoolConfig("Settings", "ShowAnimalKills", true);
            BoolConfig("Settings", "MessageInRadius", false);
            IntConfig("Settings", "MessageRadius", 300);
            BoolConfig("Settings", "EnablePluginIcon", true);

			//  Animals
            StringConfig("Animals", "Stag", "Stag");
            StringConfig("Animals", "Boar", "Boar");
            StringConfig("Animals", "Bear", "Bear");
            StringConfig("Animals", "Chicken", "Chicken");
            StringConfig("Animals", "Wolf", "Wolf");
            StringConfig("Animals", "Horse", "Horse");
			
            //  Colors
            StringConfig("Colors", "Message", "#E0E0E0");
            StringConfig("Colors", "Prefix", "grey");
            StringConfig("Colors", "Animal", "#4B75FF");
            StringConfig("Colors", "Bodypart", "#4B75FF");
            StringConfig("Colors", "Weapon", "#4B75FF");
            StringConfig("Colors", "Victim", "#4B75FF");
            StringConfig("Colors", "Attacker", "#4B75FF");
            StringConfig("Colors", "Distance", "#4B75FF");

            //  Messages
			MessageConfig("Radiation", new List<string>{"{victim} did not know that radiation kills."});
            MessageConfig("Hunger", new List<string>{"{victim} starved to death."});
            MessageConfig("Thirst", new List<string>{"{victim} died dehydrated."});
            MessageConfig("Drowned", new List<string>{"{victim} thought he could swim."});
            MessageConfig("Cold", new List<string>{"{victim} froze to death."});
            MessageConfig("Heat", new List<string>{"{victim} burned to death."});
            MessageConfig("Fall", new List<string>{"{victim} fell to his death."});
            MessageConfig("Bleeding", new List<string>{"{victim} bled to death."});
            MessageConfig("Explosion", new List<string>{"{victim} got blown up."});
            MessageConfig("Poision", new List<string>{"{victim} died by poison."});
            MessageConfig("Suicide", new List<string>{"{victim} committed suicide."});
            MessageConfig("Generic", new List<string>{"{victim} died."});
			MessageConfig("Unknown", new List<string>{"{victim} died by an unknown reason."});
            MessageConfig("Trap", new List<string>{"{victim} stepped on a {attacker}."});
            MessageConfig("Barricade", new List<string>{"{victim} died stuck on a {attacker}."});
            MessageConfig("Stab", new List<string>{"{attacker} stabbed {victim} to death with a {weapon} and hit the {bodypart}."});
            MessageConfig("StabSleep", new List<string>{"{attacker} stabbed {victim}, while he slept."});
            MessageConfig("Slash", new List<string>{"{attacker} sliced {victim} into pieces with a {weapon} and hit the {bodypart}."});
            MessageConfig("SlashSleep", new List<string>{"{attacker} stabbed {victim}, while he slept."});
            MessageConfig("Blunt", new List<string>{"{attacker} killed {victim} with a {weapon} and hit the {bodypart}."});
            MessageConfig("BluntSleep", new List<string>{"{attacker} killed {victim} with a {weapon}, while he slept."});
            MessageConfig("Bullet", new List<string>{"{attacker} killed {victim} with a {weapon}, hitting the {bodypart} from {distance}m."});
            MessageConfig("BulletSleep", new List<string>{"{attacker} killed {victim}, while sleeping. (In the {bodypart} with a {weapon}, from {distance}m)"});
            MessageConfig("Arrow", new List<string>{"{attacker} killed {victim} with an arrow at {distance}m, hitting the {bodypart}."});
            MessageConfig("ArrowSleep", new List<string>{"{attacker} killed {victim} with an arrow from {distance}m, while he slept."});
            MessageConfig("Bite", new List<string>{"A {attacker} killed {victim}."});
            MessageConfig("BiteSleep", new List<string>{"A {attacker} killed {victim}, while he slept."});
            MessageConfig("AnimalDeath", new List<string>{"{attacker} killed a {victim} with a {weapon} from {distance}m."});
        }
		
        void OnEntityDeath(BaseCombatEntity vic, HitInfo hitInfo)
        {
			string weapon = "Unknown";
			string msg = "Unknown";
			string bodypart = "Unknown";
			string dmg = "Unknown";
			string victim = "Unknown";
			string attacker = "Unknown";
			bool sleeping = false;
			string codeLocation = "OnEntityDeath Beginning";
			
			//###################################################//
			//##############  LOCATION: Beginning  ##############//
			//###################  PART: Main  ##################//
			//###################################################//
			
			DebugMessage("PART: Main | LOCATION: Beginning");
			
			//	OnEntityDeath Beginning | Main: Declaration
			codeLocation = "OnEntityDeath Beginning | Main: Declaration";
			DebugMessage("AT: " + codeLocation);
			try
			{
				if(hitInfo == null) return;
				
				dmg = FirstUpper(vic.lastDamage.ToString() ?? "Unknown");
				if((bool) string.IsNullOrEmpty(dmg)) dmg = "Unknown";
				
				bodypart = StringPool.Get(hitInfo.HitBone) ?? "Unknown";
				if((bool) string.IsNullOrEmpty(bodypart)) bodypart = "Unknown";
			}
			catch(Exception ex)
			{
				LogError(codeLocation, ex);
				return;
			}
			
			//	OnEntityDeath Beginning | Main: Getting Attacker
			codeLocation = "OnEntityDeath End | Message: Sending & New2Config";
			DebugMessage("AT: " + codeLocation);
			try
			{
				if(hitInfo.Initiator != null)
				{
					if(hitInfo.Initiator.ToPlayer() != null)
					{
						attacker = hitInfo.Initiator.ToPlayer().displayName;
					}
					else
					{
						attacker = FirstUpper(hitInfo.Initiator.LookupShortPrefabName());
					}
				}
				else
				{
					attacker = "None";
				}
			}
			catch (Exception ex)
			{
				LogError(codeLocation, ex);
				return;
			}
			
			//###################################################//
			//################  LOCATION: Middle  ###############//
			//###################################################//
			
			DebugMessage("PART: Main | LOCATION: Middle");
			
			//	OnEntityDeath Middle | Main: Getting Victim
			codeLocation = "OnEntityDeath Middle | Main: Getting Victim";
			DebugMessage("AT: " + codeLocation);
			try
			{
				if(!vic.ToString().Contains("corpse"))
				{
					if(vic != null)
					{
						if(vic.ToPlayer() != null)
						{
							victim = vic.ToPlayer().displayName;
							sleeping = (bool)vic.ToPlayer().IsSleeping();
							
							//	Is it Suicide or Metabolism?
							if(dmg == "Suicide" && (bool)Config["Settings", "ShowSuicides"]) msg = dmg;
							if(metabolism.Contains(dmg) && (bool)Config["Settings", "ShowMetabolismDeaths"]) msg = dmg;
							
							//	Is Attacker a Player?
							if(hitInfo.Initiator != null && hitInfo.Initiator.ToPlayer() != null && playerDamageTypes.Contains(dmg) && hitInfo.WeaponPrefab.ToString().Contains("grenade") == false)
							{
								if(hitInfo.WeaponPrefab.ToString().Contains("hunting") || hitInfo.WeaponPrefab.ToString().Contains("bow"))
								{
									if(sleeping) msg = "ArrowSleep";
									else msg = "Arrow";
								}
								else
								{
									if(sleeping) msg = dmg + "Sleep";
									else msg = dmg;
								}
							}
							//	Is Attacker an explosive?
							else if(hitInfo.WeaponPrefab != null && hitInfo.WeaponPrefab.ToString().Contains("grenade") || dmg == "Explosion" && (bool)Config["Settings", "ShowExplosionDeaths"])
							{
								msg = "Explosion";
							}
							//	Is Attacker a trap?
							else if(traps.Contains(attacker) && (bool)Config["Settings", "ShowTrapDeaths"])
							{
								msg = "Trap";
							}
							//	Is Attacker a Barricade?
							else if(barricadeDamageTypes.Contains(dmg) && (bool)Config["Settings", "ShowBarricadeDeaths"])
							{
								msg = "Barricade";
							}
							//	Is Attacker an Animal?
							else if(dmg == "Bite" && (bool)Config["Settings", "ShowAnimalKills"])
							{
								if(sleeping) msg = "BiteSleep";
								else msg = "Bite";
							}
						}
						//	Victim is an Animal
						else if(vic.ToString().Contains("animals") && (bool)Config["Settings", "ShowAnimalDeaths"])
						{
							victim = FirstUpper(vic.LookupShortPrefabName());	
							msg = "AnimalDeath";
							if(dmg == "Explosion") msg = "Explosion";
						}
					}
				}
			}
			catch (Exception ex)
			{
				LogError(codeLocation, ex);
				return;
			}
			
			//###################################################//
			//#################  LOCATION: End  #################//
			//#################  PART: Message  #################//
			//###################################################//
			
			DebugMessage("PART: Message | LOCATION: End");
			
			if(msg != null)
			{
				//	OnEntityDeath End | Message: Silencer Check
				codeLocation = "OnEntityDeath End | Message: Silencer Check";
				DebugMessage("AT: " + codeLocation);
				try
				{
					weapon = hitInfo?.Weapon?.GetItem().info?.displayName?.english?.ToString();
					if(weapon != null && weapon.Contains("Semi-Automatic Pistol")) weapon = "Semi-Automatic Pistol";
					if(hitInfo?.Weapon?.children != null)
					{
						foreach(var cur in hitInfo?.Weapon?.children as List<BaseEntity>)
						{
							ProjectileWeaponMod curr = (ProjectileWeaponMod)cur;
							if((bool)curr?.isSilencer)
							{
								weapon = "Silenced " + weapon;
								break;
							}	
						}
					}
				}
				catch(Exception ex)
				{
					LogError(codeLocation, ex);
					return;
				}
				
				string formattedDistance = "0";
				string formattedVictim = "Unknown";
				string formattedAttacker = "Unknown";
				string formattedAnimal = "Unknown";
				string formattedBodypart = "Unknown";
				string formattedWeapon = "Unknown";
				string rawVictim = "Unknown";
				string rawAttacker = "Unknown";
				string rawAnimal = "Unknown";
				string rawBodypart = "Unknown";
				string rawWeapon = "Unknown";
				
				string deathmsg = "";
				string rawmsg = "";
				
				//	OnEntityDeath End | Message: Declaration
				codeLocation = "OnEntityDeath End | Message: Declaration";
				DebugMessage("AT: " + codeLocation);
				try
				{
					if(hitInfo.WeaponPrefab != null && hitInfo.WeaponPrefab.ToString().Contains("f1")) weapon = "F1 Grenade";
					else if(hitInfo.WeaponPrefab != null && hitInfo.WeaponPrefab.ToString().Contains("beancan")) weapon = "Beancan Grenade";
					else if(hitInfo.WeaponPrefab != null && hitInfo.WeaponPrefab.ToString().Contains("timed")) weapon = "Timed Explosive Charge";
					else if(hitInfo.WeaponPrefab != null && hitInfo.WeaponPrefab.ToString().Contains("rocket")) weapon = "Rocket Launcher";
					
					if(hitInfo.Initiator != null) formattedDistance = GetFormattedDistance(GetDistance(vic, hitInfo.Initiator) ?? "0");
					formattedVictim = GetFormattedVictim(victim ?? "Unknown", false);
					if(hitInfo.Initiator != null) formattedAttacker = GetFormattedAttacker(attacker ?? "Unknown", false);
					formattedAnimal = GetFormattedAnimal(attacker ?? "Unknown", false);
					if(hitInfo.Initiator != null) formattedBodypart = GetFormattedBodypart(bodypart ?? "Unknown", false);
					if(hitInfo.Initiator != null) formattedWeapon = GetFormattedWeapon(weapon ?? "Unknown");
					rawVictim = GetFormattedVictim(victim ?? "Unknown", true);
					if(hitInfo.Initiator != null) rawAttacker = GetFormattedAttacker(attacker ?? "Unknown", true);
					rawAnimal = GetFormattedAnimal(attacker ?? "Unknown", true);
					if(hitInfo.Initiator != null) rawBodypart = GetFormattedBodypart(bodypart ?? "Unknown", true);
					if(hitInfo.Initiator != null) rawWeapon = weapon ?? "Unknown";
					
					deathmsg = GetRandomMessage(msg) ?? GetRandomMessage("Unknown");
					rawmsg = GetRandomMessage(msg) ?? GetRandomMessage("Unknown");
					
					deathmsg = deathmsg.Replace("{victim}", formattedVictim);
					rawmsg = rawmsg.Replace("{victim}", rawVictim);
					
					if(hitInfo.Initiator != null)
					{
						if(msg == "Bite") deathmsg = deathmsg.Replace("{attacker}", formattedAnimal);
						else deathmsg = deathmsg.Replace("{attacker}", formattedAttacker);
						
						if(msg == "Bite") rawmsg = rawmsg.Replace("{attacker}", rawAnimal);
						else rawmsg = rawmsg.Replace("{attacker}", rawAttacker);
					}
				}
				catch(Exception ex)
				{
					LogError(codeLocation, ex);
					return;
				}
				
				//	OnEntityDeath End | Message: Check for needed vars
				codeLocation = "OnEntityDeath End | Message: Check for needed vars";
				DebugMessage("AT: " + codeLocation);
				try
				{
					if (vic.ToString().Contains("animals") && hitInfo.Initiator == null)
					{
						return;
					}
					
					if (vic.ToString().Contains("animals") && hitInfo.Initiator.ToString().Contains("animals"))
					{
						return;
					}
					
					if(vic.ToPlayer() == null && hitInfo.Initiator == null)
					{
						return;
					}
				}
				catch (Exception ex)
				{
					LogError(codeLocation, ex);
					return;
				}
				
				if(formattedBodypart != null) deathmsg = deathmsg.Replace("{bodypart}", formattedBodypart);
				if(hitInfo.Initiator != null) deathmsg = deathmsg.Replace("{distance}", formattedDistance);
				if(hitInfo.Weapon != null || hitInfo.WeaponPrefab != null) deathmsg = deathmsg.Replace("{weapon}", formattedWeapon);
				
				if(formattedBodypart != null) rawmsg = rawmsg.Replace("{bodypart}", rawBodypart);
				if(hitInfo.Initiator != null) rawmsg = rawmsg.Replace("{distance}", GetDistance(vic, hitInfo.Initiator));
				if(hitInfo.Weapon != null || hitInfo.WeaponPrefab != null) rawmsg = rawmsg.Replace("{weapon}", rawWeapon);
				
				//	OnEntityDeath End | Message: Sending & New2Config
				codeLocation = "OnEntityDeath End | Message: Sending & New2Config";
				DebugMessage("AT: " + codeLocation);
				try
				{
					if(victim == "Unknown" && BasePlayer.Find(victim) == null) return;
					if((bool) string.IsNullOrEmpty(rawBodypart)) rawBodypart = "Unknown";
					if((bool) string.IsNullOrEmpty(weapon)) weapon = "Unknown";
					if(msg != "AnimalDeath") AddNewToConfig(rawBodypart, weapon);
					BroadcastDeath(prefix + GetFormattedMessage(deathmsg), rawmsg, vic);
				}
				catch (Exception ex)
				{
					LogError(codeLocation, ex);
					return;
				}
			}
		}
		#endregion
	
		#region FormattingMethods		
		string GetRandomMessage(string msg)
		{
			DebugMessage("GetRandomMessage(" + msg + ")");
			List<object> messages = Config["Messages", msg] as List<object>;
			
			string rndmMsg = "";
			int rndm = Convert.ToInt32(Oxide.Core.Random.Range(0, Convert.ToInt32(messages.Count())));
			rndmMsg = messages[rndm]?.ToString() ?? "{victim} died by an unknown reason";
			return rndmMsg;
		}
		
		string FirstUpper(string s)
		{
			DebugMessage("FirstUpper(" + s + ")");
			if (string.IsNullOrEmpty(s))
			{
				return string.Empty;
			}
			
			return char.ToUpper(s[0]) + s.Substring(1);
		}
		
		string GetFormattedAttacker(string attacker, bool raw)
		{
			DebugMessage("GetFormattedAttacker(" + attacker + ", " + raw.ToString() + ")");
			attacker = attacker.Replace(".prefab", "");
			attacker = attacker.Replace("Beartrap", "Bear Trap");
			attacker = attacker.Replace("Floor_spikes", "Floor Spike Trap");
			attacker = attacker.Replace("Barricade.woodwire", "Wired Wooden Barricade");
			attacker = attacker.Replace("Wall.external.high.wood", "High External Wooden Wall");
			attacker = attacker.Replace("Barricade.wood", "Wooden Barricade");
			attacker = attacker.Replace("Barricade.metal", "Metal Barricade");
			if(!raw) attacker = "<color=" + Config["Colors", "Attacker"].ToString() + ">" + attacker + "</color>";
			return attacker;
		}
		
		string GetFormattedVictim(string victim, bool raw)
		{
			DebugMessage("GetFormattedVictim(" + victim + ", " + raw.ToString() + ")");
			victim = victim.Replace(".prefab", "");
			if(Config["Animals", victim] != null) victim = (string)Config["Animals", victim];
			if(!raw) victim = "<color=" + Config["Colors", "Victim"].ToString() + ">" + victim + "</color>";
			return victim;
		}
		
		string GetFormattedDistance(string distance)
		{
			DebugMessage("GetFormattedDistance(" + distance + ")");
			distance = "<color=" + Config["Colors", "Distance"].ToString() + ">" + distance + "</color>";
			return distance;
		}
		
		string GetFormattedMessage(string message)
		{
			DebugMessage("GetFormattedMessage(" + message + ")");
			message = "<color=" + Config["Colors", "Message"].ToString() + ">" + message + "</color>";
			return message;
		}
		
		string GetFormattedWeapon(string weapon)
		{
			DebugMessage("GetFormattedWeapon(" + weapon + ")");
			ConfigWeapon(weapon);
			weapon = "<color=" + Config["Colors", "Weapon"].ToString() + ">" + Config["Weapons", weapon] + "</color>";
			return weapon;
		}
		
		string GetFormattedAnimal(string animal, bool raw)
		{
			DebugMessage("GetFormattedAnimal(" + animal + ")");
			animal = animal.Replace(".prefab", "");
			if(!raw) animal = "<color=" + Config["Colors", "Animal"].ToString() + ">" + Config["Animals", animal] + "</color>";
			return animal;
		}
		
		string GetFormattedBodypart(string bodypart, bool raw)
		{
			DebugMessage("GetFormattedBodypart(" + bodypart + ", " + raw.ToString() + ")");
			for(int i = 0; i < 10; i++)
			{
				bodypart = bodypart.Replace(i.ToString(), "");
			}
			bodypart = bodypart.Replace(".prefab", "");
			bodypart = bodypart.Replace("L", "");
			bodypart = bodypart.Replace("R", "");
			bodypart = bodypart.Replace("_", "");
			bodypart = bodypart.Replace(".", "");
			bodypart = bodypart.Replace("right", "");
			bodypart = bodypart.Replace("left", "");
			bodypart = bodypart.Replace("tranform", "");
			bodypart = bodypart.Replace("lowerjaweff", "jaw");
			bodypart = bodypart.Replace("rarmpolevector", "arm");
			bodypart = bodypart.Replace("connection", "");
			bodypart = bodypart.Replace("uppertight", "tight");
			bodypart = bodypart.Replace("fatjiggle", "");
			bodypart = bodypart.Replace("fatend", "");
			bodypart = bodypart.Replace("seff", "");
			
			bodypart = FirstUpper(bodypart);
			
			ConfigBodypart(bodypart);
			
			if(!raw) bodypart = "<color=" + Config["Colors", "Bodypart"].ToString() + ">" + Config["Bodyparts", bodypart].ToString() + "</color>";
			
			return bodypart;
		}
		#endregion
		
        #region UsefulMethods
		//--------------------------->   Webrequest   <---------------------------//
		
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
			
			SendChatMessage(player, "<size=25><color=grey>Death Notes</color></size><color=grey> by LaserHydra\nInstalled Version:</color> " + this.Version + "\n<color=grey>Latest Version:</color> " + version, null, profile);
		}
		
        //------------------------------>   Config   <------------------------------//
		
		void AddNewToConfig(string bodypart, string weapon)
		{
			ConfigWeapon(weapon);
			ConfigBodypart(bodypart);
			
			SaveConfig();
		}
		
		void MessageConfig(string MsgName, List<string> Data)
        {
            if (Config["Messages", MsgName] == null) Config["Messages", MsgName] = Data;
        }
		
		void ConfigWeapon(string weapon)
        {
            if (Config["Weapons", weapon] == null) Config["Weapons", weapon] = weapon;
        }
		
		void ConfigBodypart(string bodypart)
        {
            if (Config["Bodyparts", bodypart] == null) Config["Bodyparts", bodypart] = bodypart;
        }
		
        void StringConfig(string GroupName, string DataName, string Data)
        {
            if (Config[GroupName, DataName] == null) Config[GroupName, DataName] = Data;
        }

        void BoolConfig(string GroupName, string DataName, bool Data)
        {
            if (Config[GroupName, DataName] == null) Config[GroupName, DataName] = Data;
        }

        void IntConfig(string GroupName, string DataName, int Data)
        {
            if (Config[GroupName, DataName] == null) Config[GroupName, DataName] = Data;
        }

		//------------------------------>   Vector3   <------------------------------//
		
		string GetDistance(BaseCombatEntity victim, BaseEntity attacker)
		{
			string distance = Convert.ToInt32(Vector3.Distance(victim.transform.position, attacker.transform.position)).ToString();
			return distance;
		}
		
        //---------------------------->   Chat Sending   <----------------------------//

        void BroadcastChat(string prefix, string msg = null)
        {

            if (msg != null)
            {
                PrintToChat("<color=orange>" + prefix + "</color>: " + msg);
            }
            else
            {
                msg = prefix;
                PrintToChat(msg);
            }
        }

        void SendChatMessage(BasePlayer player, string prefix, string msg = null, string userid = "0")
        {
            if (player?.net == null) return;
            if (msg != null)
            {
                player.SendConsoleCommand("chat.add", userid, "<color=orange>" + prefix + "</color>: " + msg, 1.0);
            }
            else
            {
                msg = prefix;
                player.SendConsoleCommand("chat.add", userid, msg, 1.0);
            }
        }
		
		void BroadcastDeath(string deathmessage, string rawmessage, BaseEntity victim)
		{
			DebugMessage("Chatmsg: " + deathmessage + ", Rawmsg: " + rawmessage);
			if((bool)Config["Settings", "MessageInRadius"])
			{
				foreach(BasePlayer player in BasePlayer.activePlayerList)
				{
					if(Convert.ToInt32(GetDistance(player, victim)) <= (int)Config["Settings", "MessageRadius"]) player.SendConsoleCommand("chat.add", profile, deathmessage, 1.0);
				}
			}
			else ConsoleSystem.Broadcast("chat.add", profile, deathmessage, 1.0);
			
			if((bool)Config["Settings", "UsePopupNotifications"]) PopupNotifications?.Call("CreatePopupNotification", deathmessage);
			
			if((bool)Config["Settings", "BroadcastToConsole"]) Puts(rawmessage);
			ConVar.Server.Log("oxide/logs/DeathNotes_Kills.txt", rawmessage);
		}
		
		void LogError(string where, Exception ex)
		{
			ConVar.Server.Log("oxide/logs/DeathNotes_ErrorLog.txt", "FAILED AT: " + where + " | " + ex.ToString() + "\n");
			//Puts("FAILED AT: " + where + " | " + ex.ToString());
		}
		
		void DebugMessage(string message)
		{
			if(debugging == false) return;
			Puts(message);
			
			foreach(var cur in BasePlayer.activePlayerList)
			{
				if(cur.displayName == "John[BOT]") continue;
				if(permission.UserHasPermission(cur.userID.ToString(), "deathnotes.debug")) cur.SendConsoleCommand("echo '<color=red>" + message + "</color>'");
			}
		}

        //---------------------------------------------------------------------------//
        #endregion
    }
}