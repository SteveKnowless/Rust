using System;
using System.Collections.Generic;
using UnityEngine;
using Rust;

namespace Oxide.Plugins
{
    [Info("RotateOnUpgrade", "KeyboardCavemen", "1.2.0")]
    class RotateOnUpgrade : RustPlugin
    {
        private bool allowAdminRotate;
        private bool allowDemolish;
        private bool allowRotate;
        private bool allowDemolishDoors;
        private bool allowRotateDoors;
        private int amountOfMinutesAfterUpgrade = 0;
        private List<int> instanceIDs = new List<int>();
        private List<string> timesOfUpgrade = new List<string>();

        private int timerInterval = 60;
        private DateTime lastTimerTick;

        //Oxide Hook
        void OnServerInitialized()
        {
            checkConfig();

            this.allowAdminRotate = Config.Get<bool>("allowAdminRotate");
            this.allowDemolish = Config.Get<bool>("allowDemolish");
            this.allowRotate = Config.Get<bool>("allowRotate");
            this.allowDemolishDoors = Config.Get<bool>("allowDemolishDoors");
            this.allowRotateDoors = Config.Get<bool>("allowRotateDoors");
            this.amountOfMinutesAfterUpgrade = Config.Get<int>("amountOfMinutesAfterUpgrade");
            this.instanceIDs = Config.Get<List<int>>("instanceIDs");
            this.timesOfUpgrade = Config.Get<List<string>>("timesOfUpgrade");

            timer.Every(timerInterval, () => timerTickHandler());
        }

        //Oxide Hook
        protected override void LoadDefaultConfig()
        {
            Config["allowAdminRotate"] = false;
            Config["allowDemolish"] = true;
            Config["allowRotate"] = true;
            Config["allowDemolishDoors"] = false;
            Config["allowRotateDoors"] = true;
            Config["amountOfMinutesAfterUpgrade"] = 10;
            Config["configVersion"] = this.Version.ToString();
            Config["instanceIDs"] = new List<int>();
            Config["timesOfUpgrade"] = new List<string>();
            Config.Save(Manager.ConfigPath + "\\" + this.Name + ".json");

            Puts("Created new default config.");
        }

        //Oxide Hook
        void OnPlayerInput(BasePlayer player, InputState input)
        {
            if (this.allowAdminRotate && player.IsAdmin() && player.GetActiveItem() != null && player.GetActiveItem().info.shortname.Equals("hammer"))
            {
                if (input.WasJustPressed(BUTTON.FIRE_PRIMARY))
                {
                    RaycastHit hit;
                    if (Physics.Raycast(player.eyes.position, (player.eyes.rotation * Vector3.forward), out hit, 2f, Layers.Server.Buildings))
                    {
                        BaseEntity baseEntity = hit.collider.gameObject.ToBaseEntity();
                        if (baseEntity != null)
                        {
                            BuildingBlock block = baseEntity.GetComponent<BuildingBlock>();
                            if (block != null && block.blockDefinition.canRotate && !this.instanceIDs.Contains(block.GetInstanceID()))
                            {
                                block.SetFlag(BaseEntity.Flags.Reserved1, true);
                                addBlockToLists(block.GetInstanceID(), DateTime.Now.AddMinutes(-this.amountOfMinutesAfterUpgrade).ToString());

                                int remainingSeconds = timerInterval - DateTime.Now.Subtract(lastTimerTick).Seconds;
                                SendReply(player, "<color=green>You can now rotate this " + block.blockDefinition.info.name.english + " for " + remainingSeconds +  " seconds.</color>");
                            }
                        }
                    }
                }
            }
        }

        //Oxide Hook
        void OnStructureUpgrade(BuildingBlock block, BasePlayer player, BuildingGrade.Enum grade)
        {
            if (block.grade == BuildingGrade.Enum.Twigs)
            {
                if (allowDemolish)
                {
                    block.SetFlag(BaseEntity.Flags.Reserved2, true);

                    if (this.amountOfMinutesAfterUpgrade > 0)
                    {
                        addBlockToLists(block.GetInstanceID(), DateTime.Now.ToString());
                    }
                }

                if (allowRotate && block.blockDefinition.canRotate)
                {

                    block.SetFlag(BaseEntity.Flags.Reserved1, true);

                    if (this.amountOfMinutesAfterUpgrade > 0)
                    {
                        addBlockToLists(block.GetInstanceID(), DateTime.Now.ToString());
                    }
                }
            }

            else if (block.name.Contains("build/door.hinged") && block.grade == BuildingGrade.Enum.Wood)
            {
                if (allowDemolishDoors)
                {
                    block.SetFlag(BaseEntity.Flags.Reserved2, true);

                    if (this.amountOfMinutesAfterUpgrade > 0)
                    {
                        addBlockToLists(block.GetInstanceID(), DateTime.Now.ToString());
                    }
                }

                if (allowRotateDoors)
                {
                    block.SetFlag(BaseEntity.Flags.Reserved1, true);

                    if (this.amountOfMinutesAfterUpgrade > 0)
                    {
                        addBlockToLists(block.GetInstanceID(), DateTime.Now.ToString());
                    }
                }
            }
        }

        private void checkConfig()
        {
            string configVersion = Config.Get<string>("configVersion");
            if (configVersion == null || configVersion != this.Version.ToString())
            {
                //Back it up to a .old file
                Config.Save(Manager.ConfigPath + "\\" + this.Name + ".old.json");
                Puts("Config out of date, backuped it to " + this.Name + ".old.json.");

                //Read the building parts from the old config.
                this.instanceIDs = Config.Get<List<int>>("instanceIDs");
                this.timesOfUpgrade = Config.Get<List<string>>("timesOfUpgrade");

                //Create the new config.
                LoadDefaultConfig();

                //If any old building parts got loaded, import them into the new config.
                if (this.instanceIDs.Count > 0 && this.timesOfUpgrade.Count > 0)
                {
                    Config["instanceIDs"] = this.instanceIDs;
                    Config["timesOfUpgrade"] = this.timesOfUpgrade;
                    SaveConfig();
                    Puts("Imported the old building parts into the new config.");
                } 
            }
        }

        private void timerTickHandler()
        {
            this.lastTimerTick = DateTime.Now;

            for (int i = 0; i < this.timesOfUpgrade.Count; i++)
            {
                DateTime timeOfUpgrade = DateTime.Parse(this.timesOfUpgrade[i]);
                if (DateTime.Now >= timeOfUpgrade.AddMinutes(amountOfMinutesAfterUpgrade))
                {
                    List<BuildingBlock> allBuildingBlocks = new List<BuildingBlock>();
                    allBuildingBlocks.AddRange(UnityEngine.GameObject.FindObjectsOfType<BuildingBlock>());

                    if (allBuildingBlocks.Count > 0)
                    {
                        BuildingBlock blockToModify = allBuildingBlocks.Find(x => x.GetInstanceID().Equals(instanceIDs[i]));
                        if (blockToModify != null)
                        {
                            blockToModify.SetFlag(BaseEntity.Flags.Reserved2, false);
                            blockToModify.SetFlag(BaseEntity.Flags.Reserved1, false);
                        }
                    }

                    this.instanceIDs.RemoveAt(i);
                    this.timesOfUpgrade.RemoveAt(i);
                    i--;
                    updateConfig();
                }
            }
        }

        private void addBlockToLists(int instanceID, string dateTimeString)
        {
            if (!this.instanceIDs.Contains(instanceID) && !this.timesOfUpgrade.Contains(dateTimeString))
            {
                this.instanceIDs.Add(instanceID);
                this.timesOfUpgrade.Add(dateTimeString);
                updateConfig();
            }
        }

        private void updateConfig()
        {
            Config["instanceIDs"] = this.instanceIDs;
            Config["timesOfUpgrade"] = this.timesOfUpgrade;
            SaveConfig();
        }
    }
}