using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System;

namespace Oxide.Plugins
{
    [Info("Better Chat", "LaserHydra", "3.1.0", ResourceId = 979)]
    [Description("Change colors, formatting, prefix and more of the chat.")]
    class BetterChat : RustPlugin
    {
        void Loaded()
        {
			LoadDefaultConfig();
			
            if (!permission.PermissionExists("betterchat.formatting")) permission.RegisterPermission("betterchat.formatting", this);

            foreach (var group in Config)
            {
                string groupName = group.Key;
				if(groupName == "WordFilter") continue;
                permission.RegisterPermission(Config[groupName, "Permission"].ToString(), this);

                if (groupName == "player") permission.GrantGroupPermission("player", Config[groupName, "Permission"].ToString(), this);
                else if (groupName == "mod" || groupName == "moderator") permission.GrantGroupPermission("moderator", Config[groupName, "Permission"].ToString(), this);
                else if (groupName == "owner") permission.GrantGroupPermission("admin", Config[groupName, "Permission"].ToString(), this);
            }
        }

        protected override void LoadDefaultConfig()
        {
			SetConfig("WordFilter", "Enabled", false);
			SetConfig("WordFilter", "FilterList", new List<string>{"fuck", "bitch", "faggot"});
			
            SetConfig("player", "Formatting", "{Title} {Name}<color={TextColor}>:</color> {Message}");
            SetConfig("player", "ConsoleFormatting", "{Title} {Name}: {Message}");
            SetConfig("player", "Permission", "color_player");
            SetConfig("player", "Title", "[Player]");
            SetConfig("player", "TitleColor", "blue");
            SetConfig("player", "NameColor", "blue");
            SetConfig("player", "TextColor", "white");
            SetConfig("player", "Rank", 1);

            SetConfig("mod", "Formatting", "{Title} {Name}<color={TextColor}>:</color> {Message}");
            SetConfig("mod", "ConsoleFormatting", "{Title} {Name}: {Message}");
            SetConfig("mod", "Permission", "color_mod");
            SetConfig("mod", "Title", "[Mod]");
            SetConfig("mod", "TitleColor", "yellow");
            SetConfig("mod", "NameColor", "blue");
            SetConfig("mod", "TextColor", "white");
            SetConfig("mod", "Rank", 2);

            SetConfig("owner", "Formatting", "{Title} {Name}<color={TextColor}>:</color> {Message}");
            SetConfig("owner", "ConsoleFormatting", "{Title} {Name}: {Message}");
            SetConfig("owner", "Permission", "color_owner");
            SetConfig("owner", "Title", "[Owner]");
            SetConfig("owner", "TitleColor", "red");
            SetConfig("owner", "NameColor", "blue");
            SetConfig("owner", "TextColor", "white");
            SetConfig("owner", "Rank", 3);
			
			SaveConfig();
        }

        Dictionary<string, string> GetPlayerFormatting(BasePlayer player)
        {
            string uid = player.userID.ToString();
            Dictionary<string, string> playerData = new Dictionary<string, string>();
            playerData["GroupRank"] = "0";
            foreach (var group in Config)
            {
                string groupName = group.Key;
				
				if(groupName == "WordFilter") continue;
				
                if (permission.UserHasPermission(uid, Config[groupName, "Permission"].ToString()))
                {
                    if (Convert.ToInt32(Config[groupName, "Rank"].ToString()) > Convert.ToInt32(playerData["GroupRank"].ToString()))
                    {
                        playerData["Formatting"] = Config[groupName, "Formatting"].ToString();
                        playerData["ConsoleFormatting"] = Config[groupName, "ConsoleFormatting"].ToString();
                        playerData["GroupRank"] = Config[groupName, "Rank"].ToString();
                        playerData["Title"] = Config[groupName, "Title"].ToString();
                        playerData["TitleColor"] = Config[groupName, "TitleColor"].ToString();
                        playerData["NameColor"] = Config[groupName, "NameColor"].ToString();
                        playerData["TextColor"] = Config[groupName, "TextColor"].ToString();
                    }
                }
            }

            return playerData;
        }
		
		string GetFilteredMesssage(string msg)
		{
			foreach(var word in Config["WordFilter", "FilterList"] as List<object>)
			{
				MatchCollection matches = new Regex(@"((?i)(?:\S+)?" + word + @"?\S+)").Matches(msg);
				
				foreach(Match match in matches)
				{
					
					if(match.Success)
					{
						string found = match.Groups[1].ToString();
						string replaced = "";
						
						for(int i = 0; i < found.Length; i++) replaced = replaced + "*";
						
						msg = msg.Replace(found, replaced);
					}
					else
					{
						break;
					}
				}
			}
			
			return msg;
		}
		
        [ChatCommand("colors")]
        void ColorList(BasePlayer player)
        {
            List<string> colorList = new List<string> { "aqua", "black", "blue", "brown", "darkblue", "green", "grey", "lightblue", "lime", "magenta", "maroon", "navy", "olive", "orange", "purple", "red", "silver", "teal", "white", "yellow" };
            string colors = "";
            foreach (string color in colorList)
            {
                if (colors == "")
                {
                    colors = "<color=" + color + ">" + color.ToUpper() + "</color>";
                }
                else
                {
                    colors = colors + ", " + "<color=" + color + ">" + color.ToUpper() + "</color>";
                }
            }
            SendChatMessage(player, "<b><size=25>Available colors:</size></b><size=20>\n " + colors + "</size>");
        }
		
        bool OnPlayerChat(ConsoleSystem.Arg arg)
        {
            BasePlayer player = (BasePlayer) arg.connection.player;
            string message = arg.GetString(0, "text");
			if((bool)Config["WordFilter", "Enabled"]) message = GetFilteredMesssage(message);
            string uid = player.userID.ToString();
            var ChatMute = plugins.Find("chatmute");

			if(message == "" || message == null) return false;
			
            if (message.Contains("<color=") || message.Contains("</color>") || message.Contains("<size=") || message.Contains("</size>") || message.Contains("<b>") || message.Contains("<\b>") || message.Contains("<i>") || message.Contains("</i>"))
            {
                if (!permission.UserHasPermission(uid, "betterchat.formatting"))
                {
                    SendChatMessage(player, "CHAT", "You may not use formatting tags!");
                    return false;
                }
            }

            if (ChatMute != null)
            {
				bool isMuted = (bool) ChatMute.Call("IsMuted", player);
                if (isMuted) return false;
            }

            Dictionary<string, string> playerData = GetPlayerFormatting(player);
			
            playerData["FormattedOutput"] = playerData["Formatting"];
            playerData["FormattedOutput"] = playerData["FormattedOutput"].Replace("{Rank}", playerData["GroupRank"]);
            playerData["FormattedOutput"] = playerData["FormattedOutput"].Replace("{Title}", "<color=" + playerData["TitleColor"] + ">" + playerData["Title"] + "</color>");
            playerData["FormattedOutput"] = playerData["FormattedOutput"].Replace("{TitleColor}", playerData["TitleColor"]);
            playerData["FormattedOutput"] = playerData["FormattedOutput"].Replace("{NameColor}", playerData["NameColor"]);
            playerData["FormattedOutput"] = playerData["FormattedOutput"].Replace("{TextColor}", playerData["TextColor"]);
            playerData["FormattedOutput"] = playerData["FormattedOutput"].Replace("{Name}", "<color=" + playerData["NameColor"] + ">" + player.displayName + "</color>");
            playerData["FormattedOutput"] = playerData["FormattedOutput"].Replace("{ID}", player.userID.ToString());
            playerData["FormattedOutput"] = playerData["FormattedOutput"].Replace("{Message}", "<color=" + playerData["TextColor"] + ">" + message + "</color>");

			playerData["ConsoleOutput"] = playerData["ConsoleFormatting"];
			playerData["ConsoleOutput"] = playerData["ConsoleOutput"].Replace("{Rank}", playerData["GroupRank"]);
			playerData["ConsoleOutput"] = playerData["ConsoleOutput"].Replace("{Title}", playerData["Title"]);
			playerData["ConsoleOutput"] = playerData["ConsoleOutput"].Replace("{TitleColor}", playerData["TitleColor"]);
			playerData["ConsoleOutput"] = playerData["ConsoleOutput"].Replace("{NameColor}", playerData["NameColor"]);
			playerData["ConsoleOutput"] = playerData["ConsoleOutput"].Replace("{TextColor}", playerData["TextColor"]);
			playerData["ConsoleOutput"] = playerData["ConsoleOutput"].Replace("{Name}", player.displayName);
			playerData["ConsoleOutput"] = playerData["ConsoleOutput"].Replace("{ID}", player.userID.ToString());
			playerData["ConsoleOutput"] = playerData["ConsoleOutput"].Replace("{Message}", message);

            ChatSay(playerData["FormattedOutput"], uid);
            Puts(playerData["ConsoleOutput"]);

            return false;
        }


        #region UsefulMethods
        //------------------------------>   Config   <------------------------------//
		
		void SetConfig(string GroupName, string DataName, object Data)
        {
			Config[GroupName, DataName] = Config[GroupName, DataName] ?? Data;
        }

        //---------------------------->   Converting   <----------------------------//

        string ArrayToString(string[] array, int first, string seperator)
        {
            return String.Join(seperator, array.Skip(first).ToArray());;
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
            if (msg != null)
            {
                SendReply(player, "<color=orange>" + prefix + "</color>: " + msg);
            }
            else
            {
                msg = prefix;
                SendReply(player, msg);
            }
        }
			
		public void ChatSay(string message, string userid = "0")
        {
            ConsoleSystem.Broadcast("chat.add", userid, message, 1.0);
        }

        //---------------------------------------------------------------------------//
        #endregion
    }
}