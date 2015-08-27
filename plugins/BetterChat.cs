using System.Collections.Generic;
using System;

namespace Oxide.Plugins
{
    [Info("Better Chat", "LaserHydra", "3.0.2", ResourceId = 979)]
    [Description("Change colors, formatting, prefix and more of the chat.")]
    class BetterChat : RustPlugin
    {
        void Loaded()
        {
            if (!permission.PermissionExists("betterchat.formatting")) permission.RegisterPermission("betterchat.formatting", this);

            foreach (var group in Config)
            {
                string groupName = group.Key;

                permission.RegisterPermission(Config[groupName, "Permission"].ToString(), this);

                if (groupName == "player") permission.GrantGroupPermission("player", Config[groupName, "Permission"].ToString(), this);
                else if (groupName == "mod" || groupName == "moderator") permission.GrantGroupPermission("moderator", Config[groupName, "Permission"].ToString(), this);
                else if (groupName == "owner") permission.GrantGroupPermission("admin", Config[groupName, "Permission"].ToString(), this);

                LoadDefaultConfig();
            }
        }

        protected override void LoadDefaultConfig()
        {
            StringConfig("player", "Formatting", "{Title} {Name}<color={TextColor}>:</color> {Message}");
            StringConfig("player", "ConsoleFormatting", "{Title} {Name}: {Message}");
            StringConfig("player", "Permission", "color_player");
            StringConfig("player", "Title", "[Player]");
            StringConfig("player", "TitleColor", "lime");
            StringConfig("player", "NameColor", "lime");
            StringConfig("player", "TextColor", "white");
            IntConfig("player", "Rank", 1);

            StringConfig("mod", "Formatting", "{Title} {Name}<color={TextColor}>:</color> {Message}");
            StringConfig("mod", "ConsoleFormatting", "{Title} {Name}: {Message}");
            StringConfig("mod", "Permission", "color_mod");
            StringConfig("mod", "Title", "[Mod]");
            StringConfig("mod", "TitleColor", "yellow");
            StringConfig("mod", "NameColor", "lime");
            StringConfig("mod", "TextColor", "white");
            IntConfig("mod", "Rank", 2);

            StringConfig("owner", "Formatting", "{Title} {Name}<color={TextColor}>:</color> {Message}");
            StringConfig("owner", "ConsoleFormatting", "{Title} {Name}: {Message}");
            StringConfig("owner", "Permission", "color_owner");
            StringConfig("owner", "Title", "[Owner]");
            StringConfig("owner", "TitleColor", "orange");
            StringConfig("owner", "NameColor", "lime");
            StringConfig("owner", "TextColor", "white");
            IntConfig("owner", "Rank", 3);
        }

        Dictionary<string, string> GetPlayerFormatting(BasePlayer player)
        {
            string uid = player.userID.ToString();
            Dictionary<string, string> playerData = new Dictionary<string, string>();
            playerData["GroupRank"] = "0";
            foreach (var group in Config)
            {
                string groupName = group.Key;
                if (permission.UserHasPermission(uid, Config[groupName, "Permission"].ToString()))
                {
                    if (Convert.ToInt32(Config[groupName, "Rank"]) > Convert.ToInt32(playerData["GroupRank"]))
                    {
                        playerData["Formatting"] = Config[groupName, "Formatting"].ToString();
                        playerData["ConsoleOutput"] = Config[groupName, "ConsoleFormatting"].ToString();
                        playerData["GroupRank"] = Config[groupName, "GroupRank"].ToString();
                        playerData["Title"] = Config[groupName, "Title"].ToString();
                        playerData["TitleColor"] = Config[groupName, "TitleColor"].ToString();
                        playerData["NameColor"] = Config[groupName, "NameColor"].ToString();
                        playerData["TextColor"] = Config[groupName, "TextColor"].ToString();
                    }
                }
            }

            return playerData;
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

            Dictionary<string, string> playerData = new Dictionary<string, string>();
            playerData["GroupRank"] = "0";
            foreach (var group in Config)
            {
                string groupName = group.Key.ToString();
                if (permission.UserHasPermission(uid, Config[groupName, "Permission"].ToString()))
                {
                    if (Convert.ToInt32(Config[groupName, "Rank"]) > Convert.ToInt32(playerData["GroupRank"]))
                    {
						playerData["Formatting"] = Config[groupName, "Formatting"].ToString();
                        playerData["ConsoleFormatting"] = Config[groupName, "ConsoleFormatting"].ToString();
                        playerData["GroupRank"] = Config[groupName, "Rank"].ToString();
                        playerData["Title"] = Config[groupName, "Title"].ToString();
                        playerData["TitleColor"] = Config[groupName, "TitleColor"].ToString();
                        playerData["NameColor"] = Config[groupName, "NameColor"].ToString();
                        playerData["TextColor"] = Config[groupName, "TextColor"].ToString();

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
                    }
                }
            }

            ChatSay(playerData["FormattedOutput"], uid);
            Puts(playerData["ConsoleOutput"]);

            return false;
        }


        #region UsefulMethods
        //--------------------------->   Player finding   <---------------------------//

        BasePlayer GetPlayer(string searchedPlayer, BasePlayer executer, string prefix)
        {
            BasePlayer targetPlayer = null;
            List<string> foundPlayers = new List<string>();
            string searchedLower = searchedPlayer.ToLower();
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                string display = player.displayName;
                string displayLower = display.ToLower();

                if (!displayLower.Contains(searchedLower))
                {
                    continue;
                }
                if (displayLower.Contains(searchedLower))
                {
                    foundPlayers.Add(display);
                }
            }
            var matchingPlayers = foundPlayers.ToArray();

            if (matchingPlayers.Length == 0)
            {
                SendChatMessage(executer, prefix, "No matching players found!");
            }

            if (matchingPlayers.Length > 1)
            {
                SendChatMessage(executer, prefix, "Multiple players found:");
                string multipleUsers = "";
                foreach (string matchingplayer in matchingPlayers)
                {
                    if (multipleUsers == "")
                    {
                        multipleUsers = "<color=yellow>" + matchingplayer + "</color>";
                        continue;
                    }

                    if (multipleUsers != "")
                    {
                        multipleUsers = multipleUsers + ", " + "<color=yellow>" + matchingplayer + "</color>";
                    }

                }
                SendChatMessage(executer, prefix, multipleUsers);
            }

            if (matchingPlayers.Length == 1)
            {
                targetPlayer = BasePlayer.Find(matchingPlayers[0]);
            }
            return targetPlayer;
        }

        //------------------------------>   Config   <------------------------------//

        void StringConfig(string GroupName, string DataName, string Data)
        {
            if (Config[GroupName, DataName] == null) Config[GroupName, DataName] = Data;
            if (Config[GroupName, DataName].ToString() != Data) return;
        }

        void IntConfig(string GroupName, string DataName, int Data)
        {
            if (Config[GroupName, DataName] == null) Config[GroupName, DataName] = Data;
            if (Convert.ToInt32(Config[GroupName, DataName]) != Data) return;
        }

        //---------------------------->   Converting   <----------------------------//

        string ArrayToString(string[] array, int first)
        {
            int count = 0;
            string output = array[first];
            foreach (string current in array)
            {
                if (count <= first)
                {
                    count++;
                    continue;
                }

                output = output + " " + current;
                count++;
            }
            return output;
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