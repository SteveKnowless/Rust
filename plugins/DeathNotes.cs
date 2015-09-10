using System.Collections.Generic;
using System.Reflection;
using System;
using System.Data;
using UnityEngine;
using Oxide.Core;
using ProtoBuf;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Death Notes", "LaserHydra", "3.1.41", ResourceId = 819)]
    [Description("Broadcasts players/animals deaths to chat")]
    class DeathNotes : RustPlugin
    {
		#region Settings
		string prefix = "";
		string profile = "0";
		List<string> metabolism = new List<string>();
		List<string> playerDamageTypes = new List<string>();
		List<string> barricadeDamageTypes = new List<string>();
		List<string> traps = new List<string>();
		#endregion
		
		#region Hooks
        void Loaded()
        {
            LoadDefaultConfig();
			
			prefix = "<color=" + Config["Colors", "Prefix"].ToString() + ">" + Config["Settings", "Prefix"].ToString() + "</color>";
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
            StringConfig("Messages", "Radiation", "{victim} did not know that radiation kills.");
            StringConfig("Messages", "Hunger", "{victim} starved to death.");
            StringConfig("Messages", "Thirst", "{victim} died dehydrated.");
            StringConfig("Messages", "Drowned", "{victim} thought he could swim.");
            StringConfig("Messages", "Cold", "{victim} froze to death.");
            StringConfig("Messages", "Heat", "{victim} burned to death.");
            StringConfig("Messages", "Fall", "{victim} fell to his death.");
            StringConfig("Messages", "Bleeding", "{victim} bled to death.");
            StringConfig("Messages", "Explosion", "{victim} got blown up.");
            StringConfig("Messages", "Poision", "{victim} died poisoned.");
            StringConfig("Messages", "Suicide", "{victim} committed suicide.");
            StringConfig("Messages", "Generic", "{victim} died.");
            StringConfig("Messages", "Trap", "{victim} stepped on a {attacker}.");
            StringConfig("Messages", "Barricade", "{victim} died stuck on a {attacker}.");
            StringConfig("Messages", "Stab", "{attacker} stabbed {victim} to death with a {weapon} and hit the {bodypart}.");
            StringConfig("Messages", "StabSleep", "{attacker} stabbed {victim}, while he slept.");
            StringConfig("Messages", "Slash", "{attacker} sliced {victim} into pieces with a {weapon} and hit the {bodypart}.");
            StringConfig("Messages", "SlashSleep", "{attacker} stabbed {victim}, while he slept.");
            StringConfig("Messages", "Blunt", "{attacker} killed {victim} with a {weapon} and hit the {bodypart}.");
            StringConfig("Messages", "BluntSleep", "{attacker} killed {victim} with a {weapon}, while he slept.");
            StringConfig("Messages", "Bullet", "{attacker} killed {victim} with a {weapon}, hitting the {bodypart} from {distance}m.");
            StringConfig("Messages", "BulletSleep", "{attacker} killed {victim}, while sleeping. (In the {bodypart} with a {weapon}, from {distance}m)");
            StringConfig("Messages", "Arrow", "{attacker} killed {victim} with an arrow at {distance}m, hitting the {bodypart}.");
            StringConfig("Messages", "ArrowSleep", "{attacker} killed {victim} with an arrow from {distance}m, while he slept.");
            StringConfig("Messages", "Bite", "A {attacker} killed {victim}.");
            StringConfig("Messages", "BiteSleep", "A {attacker} killed {victim}, while he slept.");
            StringConfig("Messages", "AnimalDeath", "{attacker} killed a {victim} with a {weapon} from {distance}m.");
        }
		
        void OnEntityDeath(BaseCombatEntity vic, HitInfo hitInfo)
        {
			try
			{			
				if(hitInfo == null) return;
				
				string weapon = "";
				string msg = null;
				
				string dmg = FirstUpper(vic.lastDamage.ToString());
				if(dmg == null || dmg == "") dmg = "None";

				string bodypart = GetFormattedBodypart(StringPool.Get(hitInfo.HitBone), true);
				if(bodypart == null || bodypart == "") bodypart = "None";
				
				string victim = "";
				string attacker = null;
				bool sleeping = false;
				
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
					ConVar.Server.Log("oxide/logs/DeathNotes_ErrorLog.txt", "Failed at getting attacker: " + ex.Message.ToString());
					return;
				}
				
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
								else if(dmg == "Explosion" || dmg == "Stab" && (bool)Config["Settings", "ShowExplosionDeaths"])
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
					ConVar.Server.Log("oxide/logs/DeathNotes_ErrorLog.txt", "Failed at getting victim: " + ex.Message.ToString());
					return;
				}

				if(msg != null)
				{
					weapon = hitInfo?.Weapon?.GetItem().info?.displayName?.english?.ToString();
					if(weapon.Contains("Semi-Automatic Pistol")) weapon = "Semi-Automatic Pistol";
					if(hitInfo.Weapon.children != null)
					{
						foreach(var cur in hitInfo.Weapon.children)
						{
							ProjectileWeaponMod curr = (ProjectileWeaponMod)cur;
							if(curr.isSilencer)
							{
								weapon = "Silenced " + weapon;
								break;
							}	
						}
					}

					string formattedDistance = "";

					if(hitInfo.Initiator != null) 
					formattedDistance = GetFormattedDistance(GetDistance(vic, hitInfo.Initiator));

					string formattedVictim = GetFormattedVictim(victim, false);
					string formattedAttacker = GetFormattedAttacker(attacker, false);
					string formattedAnimal = GetFormattedAnimal(attacker);
					string formattedBodypart = GetFormattedBodypart(bodypart, false);
					string formattedWeapon = GetFormattedWeapon(weapon);

					string rawVictim = GetFormattedVictim(victim, true);
					string rawAttacker = GetFormattedAttacker(attacker, true);
					string rawAnimal = attacker;
					string rawBodypart = GetFormattedBodypart(bodypart, true);
					string rawWeapon = weapon;
					


					string deathmsg = Config["Messages", msg].ToString();
					string rawmsg = Config["Messages", msg].ToString();
					
					deathmsg = deathmsg.Replace("{victim}", formattedVictim);
					rawmsg = rawmsg.Replace("{victim}", rawVictim);

					
					if(hitInfo.Initiator != null)
					{
						if(msg == "Bite") deathmsg = deathmsg.Replace("{attacker}", formattedAnimal);
						else deathmsg = deathmsg.Replace("{attacker}", formattedAttacker);
						
						if(msg == "Bite") rawmsg = rawmsg.Replace("{attacker}", rawAnimal);
						else rawmsg = rawmsg.Replace("{attacker}", rawAttacker);
					}

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
						ConVar.Server.Log("oxide/logs/DeathNotes_ErrorLog.txt", "Failed at checking for victim & attacker: " + ex.Message.ToString());
						return;
					}
					
					
					if(formattedBodypart != null) deathmsg = deathmsg.Replace("{bodypart}", formattedBodypart);
					if(hitInfo.Initiator != null) deathmsg = deathmsg.Replace("{distance}", formattedDistance);
					if(hitInfo.Weapon != null) deathmsg = deathmsg.Replace("{weapon}", formattedWeapon);
					

					if(formattedBodypart != null) rawmsg = rawmsg.Replace("{bodypart}", rawBodypart);
					if(hitInfo.Initiator != null) rawmsg = rawmsg.Replace("{distance}", GetDistance(vic, hitInfo.Initiator));
					if(hitInfo.Weapon != null) rawmsg = rawmsg.Replace("{weapon}", rawWeapon);
					
					try
					{
						if(msg != "AnimalDeath") AddNewToConfig(rawBodypart, weapon);
						BroadcastDeath(prefix + " " + GetFormattedMessage(deathmsg), rawmsg, vic);
					}
					catch (Exception ex)
					{
						ConVar.Server.Log("oxide/logs/DeathNotes_ErrorLog.txt", "Failed at sending Message & new2Config: " + ex.Message.ToString());
						return;
					}
				}
			}
			catch(Exception ex)
			{
				ConVar.Server.Log("oxide/logs/DeathNotes_ErrorLog.txt", "Failed at unknown position in OnEntityDeath: " + ex.Message.ToString());
			}
        }
		#endregion
		
		#region FormattingMethods		
		string FirstUpper(string s)
		{
			if (string.IsNullOrEmpty(s))
			{
				return string.Empty;
			}
			
			return char.ToUpper(s[0]) + s.Substring(1);
		}
		
		string GetFormattedAttacker(string attacker, bool raw)
		{
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
			victim = victim.Replace(".prefab", "");
			if(Config["Animals", victim] != null) victim = (string)Config["Animals", victim];
			if(!raw) victim = "<color=" + Config["Colors", "Victim"].ToString() + ">" + victim + "</color>";
			return victim;
		}
		
		string GetFormattedDistance(string distance)
		{
			distance = "<color=" + Config["Colors", "Distance"].ToString() + ">" + distance + "</color>";
			return distance;
		}
		
		string GetFormattedMessage(string message)
		{
			message = "<color=" + Config["Colors", "Message"].ToString() + ">" + message + "</color>";
			return message;
		}
		
		string GetFormattedWeapon(string weapon)
		{
			ConfigWeapon(weapon);
			weapon = "<color=" + Config["Colors", "Weapon"].ToString() + ">" + Config["Weapons", weapon] + "</color>";
			return weapon;
		}
		
		string GetFormattedAnimal(string animal)
		{
			animal = animal.Replace(".prefab", "");
			animal = "<color=" + Config["Colors", "Animal"].ToString() + ">" + Config["Animals", animal] + "</color>";
			return animal;
		}
		
		string GetFormattedBodypart(string bodypart, bool raw)
		{
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
        //------------------------------>   Config   <------------------------------//
		
		void AddNewToConfig(string bodypart, string weapon)
		{
			ConfigWeapon(weapon);
			ConfigBodypart(bodypart);
			
			SaveConfig();
		}
		
		void ConfigWeapon(string weapon)
        {
            if (Config["Weapons", weapon] == null) Config["Weapons", weapon] = weapon;
            if (Config["Weapons", weapon].ToString() != weapon) return;
        }
		
		void ConfigBodypart(string bodypart)
        {
            if (Config["Bodyparts", bodypart] == null) Config["Bodyparts", bodypart] = bodypart;
            if (Config["Bodyparts", bodypart].ToString() != bodypart) return;
        }
		
        void StringConfig(string GroupName, string DataName, string Data)
        {
            if (Config[GroupName, DataName] == null) Config[GroupName, DataName] = Data;
            if (Config[GroupName, DataName].ToString() != Data) return;
        }

        void BoolConfig(string GroupName, string DataName, bool Data)
        {
            if (Config[GroupName, DataName] == null) Config[GroupName, DataName] = Data;
            if ((bool)Config[GroupName, DataName] != Data) return;
        }

        void IntConfig(string GroupName, string DataName, int Data)
        {
            if (Config[GroupName, DataName] == null) Config[GroupName, DataName] = Data;
            if (Convert.ToInt32(Config[GroupName, DataName]) != Data) return;
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

        void SendChatMessage(BasePlayer player, string prefix, string msg = null)
        {
            if(msg != null)
            {
                SendReply(player, "<color=orange>" + prefix + "</color>: " + msg);
            }
            else
            {
                msg = prefix;
                SendReply(player, msg);
            }
        }
		
		void BroadcastDeath(string deathmessage, string rawmessage, BaseEntity victim)
		{
			if((bool)Config["Settings", "MessageInRadius"])
			{
				foreach(BasePlayer player in BasePlayer.activePlayerList)
				{
					if(Convert.ToInt32(GetDistance(player, victim)) <= (int)Config["Settings", "MessageRadius"]) player.SendConsoleCommand("chat.add", profile, deathmessage, 1.0);
				}
			}
			else ConsoleSystem.Broadcast("chat.add", profile, deathmessage, 1.0);
			
			if((bool)Config["Settings", "BroadcastToConsole"]) Puts(rawmessage);
			ConVar.Server.Log("oxide/logs/DeathNotes_Kills.txt", rawmessage + "\n");
		}

        //---------------------------------------------------------------------------//
        #endregion
    }
}
