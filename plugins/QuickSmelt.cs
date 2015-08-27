using System;
using Oxide.Core.Plugins;
using Random = UnityEngine.Random;

namespace Oxide.Plugins
{
    [Info("QuickSmelt", "ApocDev", "1.0.8", ResourceId = 1067)]
    public class QuickSmelt : RustPlugin
    {
        public float ProductionModifier { get { return Config.Get<float>("ProductionModifier"); } }
        public float ChancePerConsumption { get { return Config.Get<float>("ChancePerConsumption"); } }
        public float CharcoalChanceModifier { get { return Config.Get<float>("CharcoalChanceModifier"); } }
        public float CharcoalProductionModifier { get { return Config.Get<float>("CharcoalProductionModifier"); } }
        public bool DontOvercookMeat { get { return Config.Get<bool>("DontOvercookMeat"); } }
        protected override void LoadConfig()
        {
            base.LoadConfig();

            // Use this "dirty" var for future usage.
            bool dirty = false;

            if (Config["CharcoalChanceModifier"] == null)
            {
                Puts("Updated CharcoalChanceModifier!");
                Config["CharcoalChanceModifier"] = 1.5f;
                dirty = true;
            }
            if (Config["DontOvercookMeat"] == null)
            {
                Puts("Updated DontOvercookMeat!");
                Config["DontOvercookMeat"] = true;
                dirty = true;
            }
            if (Config["CharcoalProductionModifier"] == null)
            {
                Puts("Updated CharcoalProductionModifier!");
                Config["CharcoalProductionModifier"] = 1f;
                dirty = true;
            }

            if (dirty)
                SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            // This is *roughly* x2 production rate.
            Config["ProductionModifier"] = 1f;
            Config["ChancePerConsumption"] = 0.5f;
            Config["CharcoalChanceModifier"] = 1.5f;
            Config["DontOvercookMeat"] = true;
            Config["CharcoalProductionModifier"] = 1f;

            SaveConfig();
        }

        [HookMethod("OnConsumeFuel")]
        private void OnConsumeFuel(BaseOven oven, Item fuel, ItemModBurnable burnable)
        {
            var byproductChance = burnable.byproductChance * CharcoalChanceModifier;

            if (oven.allowByproductCreation && burnable.byproductItem != null && Random.Range(0.0f, 1f) <= byproductChance)
            {
                Item obj = ItemManager.Create(burnable.byproductItem, (int) Math.Round(burnable.byproductAmount * CharcoalProductionModifier));
                if (!obj.MoveToContainer(oven.inventory))
                    obj.Drop(oven.inventory.dropPosition, oven.inventory.dropVelocity);
            }

            for (int i = 0; i < oven.inventorySlots; i++)
            {
                try
                {
                    var slotItem = oven.inventory.GetSlot(i);

                    if (slotItem == null || !slotItem.IsValid())
                        continue;

                    var cookable = slotItem.info.GetComponent<ItemModCookable>();

                    if (cookable == null)
                    {

                        continue;
                    }

                    if (cookable.becomeOnCooked.category == ItemCategory.Food && slotItem.info.shortname.Trim().EndsWith(".cooked") && DontOvercookMeat)
                    {
                        continue;
                    }

                    // Some simple math here.
                    // The chance of consumption is going to result in a 1 or 0.
                    // Anything * 0 == 0
                    // Which basically means... don't smelt anything yet.
                    var consumptionAmount = (int) Math.Ceiling(ProductionModifier * (Random.Range(0f, 1f) <= ChancePerConsumption ? 1 : 0));

                    // Check how many are actually in the furnace, before we try removing too many. :)
                    var inFurnaceAmount = slotItem.amount;
                    if (inFurnaceAmount < consumptionAmount)
                    {
                        consumptionAmount = inFurnaceAmount;
                    }
                    // Set consumption to however many we can pull from this actual stack.
                    consumptionAmount = TakeFromInventorySlot(oven.inventory, slotItem.info.itemid, consumptionAmount, i);

                    // If we took nothing, then... we can't create any.
                    if (consumptionAmount <= 0)
                    {
                        continue;
                    }

                    // Create the item(s) that are now smelted.
                    var smeltedItem = ItemManager.Create(cookable.becomeOnCooked, cookable.amountOfBecome * consumptionAmount);
                    if (!smeltedItem.MoveToContainer(oven.inventory))
                    {
                        smeltedItem.Drop(oven.inventory.dropPosition, oven.inventory.dropVelocity);
                    }
                }
                catch (InvalidOperationException)
                {
                    // Eat this. Modified collection. Blah blah.
                }
            }
        }

        private int TakeFromInventorySlot(ItemContainer container, int itemId, int amount, int slot)
        {
            var item = container.GetSlot(slot);
            if (item.info.itemid == itemId && !item.IsBlueprint())
            {
                if (item.amount > amount)
                {
                    item.MarkDirty();
                    item.amount -= amount;
                    return amount;
                }

                amount = item.amount;
                item.RemoveFromContainer();
                return amount;
            }

            return 0;
        }
    }
}