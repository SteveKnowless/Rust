using System;
using System.Reflection;
using System.Collections.Generic;

using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("AutoDoorCloser", "Bombardir", "2.0.2", ResourceId = 800)]
    class AutoDoorCloser : RustPlugin
    {
        const string DataFile = "AutoDoorCloserData";
        static readonly MethodInfo UpdateLayerMethod = typeof(BuildingBlock).GetMethod("UpdateLayer", (BindingFlags.Instance | BindingFlags.NonPublic));

        static Dictionary<Door, Timer> ActiveTimers = new Dictionary<Door, Timer>();
        static Dictionary<ulong, float> PlayersData;

        #region Logic

        void OnDoorOpened(Door door, BasePlayer player)
        {
            float time;
            if (!PlayersData.TryGetValue(player.userID, out time))
                time = DefaultTime;

            if (time <= 0)
                return;

            Timer ActiveTimer;
            if (ActiveTimers.TryGetValue(door, out ActiveTimer))
            {
                ActiveTimer.Destroy();
                ActiveTimers.Remove(door);
            }

            ActiveTimers[door] = timer.Once(time, () =>
            {
                ActiveTimers.Remove(door);
                if (door == null || !door.IsOpen())
                    return;

                door.SetFlag(BaseEntity.Flags.Open, false);
                UpdateLayerMethod.Invoke(door, null);
                door.SendNetworkUpdateImmediate(false);
            });
        }

        #endregion

        #region Chat cmds

        [ChatCommand("ad")]
        void ad(BasePlayer player, string command, string[] args)
        {
            if (args.Length == 0)
            {
                player.ChatMessage(Syntax);
                return;
            }

            string arg = args[0];
            float time = 0;

            if (arg != "off")
            {
                if (!float.TryParse(arg, out time))
                {
                    player.ChatMessage(Number);
                    return;
                }

                if (time > MaxTime || time < MinTime)
                {
                    player.ChatMessage(Time);
                    return;
                }

                player.ChatMessage(string.Format(Succes, time));
            }
            else
                player.ChatMessage(SuccesOff);

            PlayersData[player.userID] = time;
            Interface.Oxide.DataFileSystem.WriteObject(DataFile, PlayersData);
        }

        #endregion

        #region Config | Data load

        #region Config Vars

        static float DefaultTime = 3;
        static float MaxTime = 10;
        static float MinTime = 0.01f;

        static string Syntax = "Error! Syntax: /ad [time|off]";
        static string Number = "Error! Incorrect number!";
        static string Time = "Error! Your time should be between {0} and {1}!";
        static string Succes = "Your doors will close automatically after {0} sec.";
        static string SuccesOff = "You turn off the automatic closing doors!!";

        #endregion

        void LoadDefaultConfig() { }

        void Init()
        {
            CheckCfg<float>("Default close time", ref DefaultTime);
            CheckCfg<float>("Max close time", ref MaxTime);
            CheckCfg<float>("Min close time", ref MinTime);
            if (MinTime < 0)
                MinTime = 0.01f;

            CheckCfg<string>("Syntax error", ref Syntax);
            CheckCfg<string>("Number error", ref Number);
            CheckCfg<string>("Time error", ref Time);

            CheckCfg<string>("Succes msg", ref Succes);
            CheckCfg<string>("Succes off msg", ref SuccesOff);
            SaveConfig();

            Time = string.Format(Time, MinTime, MaxTime);

            try { PlayersData = Interface.Oxide.DataFileSystem.ReadObject<Dictionary<ulong, float>>(DataFile); } catch { }
            if (PlayersData == null) PlayersData = new Dictionary<ulong, float>();
        }

        void CheckCfg<T>(string Key, ref T var)
        {
            if (Config[Key] == null)
                Config[Key] = var;
            else
                try { var = (T)Convert.ChangeType(Config[Key], typeof(T)); }
                catch { Config[Key] = var; }
        }

        #endregion
    }
}