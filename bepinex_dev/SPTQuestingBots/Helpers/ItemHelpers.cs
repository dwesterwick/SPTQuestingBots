using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Comfort.Common;
using EFT.Interactive;
using EFT.InventoryLogic;
using EFT;
using SPTQuestingBots.Controllers;

namespace SPTQuestingBots.Helpers
{
    [Flags]
    public enum WeaponClass
    {
        none = 0,
        assaultCarbine = 1,
        assaultRifle = 2,
        grenadeLauncher = 4,
        machinegun = 8,
        marksmanRifle = 16,
        pistol = 32,
        shotgun = 64,
        smg = 128,
        sniperRifle = 256,
        specialWeapon = 512,
    }

    public static class ItemHelpers
    {
        public static InventoryController GetInventoryController(this BotOwner bot)
        {
            Type playerType = typeof(Player);

            FieldInfo inventoryControllerField = playerType.GetField("_inventoryController", BindingFlags.NonPublic | BindingFlags.Instance);
            return (InventoryController)inventoryControllerField.GetValue(bot.GetPlayer);
        }

        public static IEnumerable<WeaponClass> ToWeaponClasses(this IEnumerable<string> weaponClassNames)
        {
            return weaponClassNames.Select(n => (WeaponClass)Enum.Parse(typeof(WeaponClass), n));
        }

        public static bool HasAnyRequiredWeapon(this BotOwner botOwner, IEnumerable<WeaponClass> requiredClasses)
        {
            foreach (WeaponClass weaponClass in botOwner.GetEquippedWeaponClasses())
            {
                if (requiredClasses.Contains(weaponClass))
                {
                    return true;
                }
            }

            return false;
        }

        public static bool HasAnyAllowedWeapon(this BotOwner botOwner, IEnumerable<WeaponClass> forbiddenClasses)
        {
            foreach (WeaponClass weaponClass in botOwner.GetEquippedWeaponClasses())
            {
                if (!forbiddenClasses.Contains(weaponClass))
                {
                    return true;
                }
            }

            return false;
        }

        public static IEnumerable<WeaponClass> GetEquippedWeaponClasses(this BotOwner botOwner)
        {
            return botOwner.GetEquippedWeaponClassNames().ToWeaponClasses();
        }

        public static IEnumerable<string> GetEquippedWeaponClassNames(this BotOwner botOwner)
        {
            return botOwner.GetEquippedWeapons().Select(w => w.WeapClass);
        }

        public static float GetMaxWeaponSightingRange(this BotOwner botOwner)
        {
            return botOwner.GetEquippedWeapons().Max(w => w.SightingRange);
        }

        public static List<Weapon> GetEquippedWeapons(this BotOwner botOwner)
        {
            InventoryController inventoryControllerClass = GetInventoryController(botOwner);

            Weapon holsterWeapon = inventoryControllerClass.Inventory.Equipment.GetSlot(EquipmentSlot.Holster).ContainedItem as Weapon;
            Weapon primaryWeapon = inventoryControllerClass.Inventory.Equipment.GetSlot(EquipmentSlot.FirstPrimaryWeapon).ContainedItem as Weapon;
            Weapon secondaryWeapon = inventoryControllerClass.Inventory.Equipment.GetSlot(EquipmentSlot.SecondPrimaryWeapon).ContainedItem as Weapon;

            List<Weapon> weapons = new List<Weapon>();
            if (holsterWeapon != null) weapons.Add(holsterWeapon);
            if (primaryWeapon != null) weapons.Add(primaryWeapon);
            if (secondaryWeapon != null) weapons.Add(secondaryWeapon);

            return weapons;
        }

        public static float HearingMultiplier(this BotOwner botOwner)
        {
            InventoryController inventoryControllerClass = GetInventoryController(botOwner);

            Item headset = inventoryControllerClass.Inventory.Equipment.GetSlot(EquipmentSlot.Earpiece).ContainedItem;
            Item helmet = inventoryControllerClass.Inventory.Equipment.GetSlot(EquipmentSlot.Headwear).ContainedItem;
            
            float multiplier = 1;

            if (headset != null)
            {
                //LoggingController.LogInfo(botOwner.GetText() + " is wearing a headset");
                multiplier *= ConfigController.Config.Questing.BotQuestingRequirements.HearingSensor.LoudnessMultiplierHeadset;
            }

            ArmoredEquipmentTemplateClass helmetTemplate = helmet?.Template as ArmoredEquipmentTemplateClass;
            switch (helmetTemplate?.DeafStrength)
            {
                case EDeafStrength.Low:
                    //LoggingController.LogInfo(botOwner.GetText() + " is wearing a helmet with low deaf strength");
                    multiplier *= ConfigController.Config.Questing.BotQuestingRequirements.HearingSensor.LoudnessMultiplierHelmetLowDeaf;
                    break;
                case EDeafStrength.High:
                    //LoggingController.LogInfo(botOwner.GetText() + " is wearing a helmet with high deaf strength");
                    multiplier *= ConfigController.Config.Questing.BotQuestingRequirements.HearingSensor.LoudnessMultiplierHelmetHighDeaf;
                    break;
            }

            return multiplier;
        }

        public static StashItemClass CreateFakeStash(InventoryController inventoryController, string stashName)
        {
            StashItemClass stashItemClass = Singleton<ItemFactoryClass>.Instance.CreateFakeStash(null);
            StashGridClass stashGridClass = new StashGridClass(stashName, 15, 15, false, Array.Empty<ItemFilter>(), stashItemClass);
            stashItemClass.Grids[0] = stashGridClass;
            stashItemClass.CurrentAddress = inventoryController.CreateItemAddress();

            return stashItemClass;
        }

        public static bool TryAddToFakeStash(this Item item, InventoryController inventoryController, string stashName)
        {
            StashItemClass stashItemClass = CreateFakeStash(inventoryController, stashName);

            var addItemToStashOperationResult = stashItemClass.Grids[0].AddAnywhere(item, EErrorHandlingType.Ignore);
            if (addItemToStashOperationResult.Failed)
            {
                return false;
            }

            return true;
        }

        public static bool TryAddToFakeStash(this Item item, BotOwner botOwner, string stashName)
        {
            InventoryController inventoryController = GetInventoryController(botOwner);

            return item.TryAddToFakeStash(inventoryController, stashName);
        }

        public static bool TryTransferItem(this BotOwner botOwner, Item item)
        {
            try
            {
                InventoryController inventoryController = GetInventoryController(botOwner);

                // Enumerate all possible equipment slots into which the key can be transferred
                List<EquipmentSlot> possibleSlots = new List<EquipmentSlot>();
                if (Controllers.BotRegistrationManager.IsBotAPMC(botOwner))
                {
                    possibleSlots.Add(EquipmentSlot.SecuredContainer);
                }
                possibleSlots.AddRange(new EquipmentSlot[] { EquipmentSlot.Backpack, EquipmentSlot.TacticalVest, EquipmentSlot.ArmorVest, EquipmentSlot.Pockets });

                // Try to find an available grid in the equipment slots to which the key can be transferred
                ItemAddress locationForItem = botOwner.FindLocationForItem(item, possibleSlots, inventoryController);
                if (locationForItem == null)
                {
                    LoggingController.LogError("Cannot find any location to put key " + item.LocalizedName() + " for " + botOwner.GetText());
                    return false;
                }

                // Initialize the transation to transfer the key to the bot
                var moveResult = InteractionsHandlerClass.Move(item, locationForItem, inventoryController, true);
                if (!moveResult.Succeeded)
                {
                    LoggingController.LogError("Cannot move key " + item.LocalizedName() + " to inventory of " + botOwner.GetText());
                    return false;
                }

                Action<IResult> callbackAction = (result) => 
                {
                    if (result.Succeed)
                    {
                        LoggingController.LogInfo("Moved key to inventory of " + botOwner.GetText());
                    }

                    if (result.Failed)
                    {
                        LoggingController.LogError("Could not move key to inventory of " + botOwner.GetText());
                    }
                };

                // Execute the transation to transfer the key to the bot
                Callback callback = new Callback(callbackAction);
                inventoryController.TryRunNetworkTransaction(moveResult, callback);

                return true;
            }
            catch (Exception e)
            {
                LoggingController.LogError(e.Message);
                LoggingController.LogError(e.StackTrace);

                throw;
            }
        }

        public static ItemAddress FindLocationForItem(this BotOwner botOwner, Item item, IEnumerable<EquipmentSlot> possibleSlots, InventoryController botInventoryController)
        {
            foreach (EquipmentSlot slot in possibleSlots)
            {
                //LoggingController.LogInfo("Checking " + slot.ToString() + " for " + BotOwner.GetText() + "...");

                // Search through all grids in the equipment slot
                CompoundItem equipmentSlot = botInventoryController.Inventory.Equipment.GetSlot(slot).ContainedItem as CompoundItem;
                foreach (StashGridClass grid in (equipmentSlot?.Grids ?? (new StashGridClass[0])))
                {
                    //LoggingController.LogInfo("Checking grid " + grid.ID + " (" + grid.GridWidth.Value + "x" + grid.GridHeight.Value + ") in " + slot.ToString() + " for " + BotOwner.GetText() + "...");

                    // Check if the grid has enough free space to fit the item
                    LocationInGrid locationInGrid = grid.FindFreeSpace(item);
                    if (locationInGrid != null)
                    {
                        LoggingController.LogInfo(botOwner.GetText() + " will receive " + item.LocalizedName() + " in its " + slot.ToString() + "...");

                        return grid.CreateItemAddress(locationInGrid);
                    }
                }
            }

            return null;
        }

        public static bool IsBundleLoaded(this Item item)
        {
            try
            {
                IEasyBundle data = Singleton<IEasyAssets>.Instance.System.GetNode(item.Prefab.path).Data;
                if (data.LoadState.Value == Diz.DependencyManager.ELoadState.Loaded)
                {
                    return true;
                }
            }
            catch (Exception e)
            {
                LoggingController.LogError(e.Message);
                LoggingController.LogError(e.StackTrace);

                throw;
            }

            return false;
        }

        public static DependencyGraphClass<IEasyBundle>.GClass3901 LoadBundle(this Item item)
        {
            try
            {
                if (item == null)
                {
                    throw new ArgumentNullException(nameof(item));
                }

                if (item.Prefab.path.Length == 0)
                {
                    throw new InvalidOperationException("The prefab path for " + item.LocalizedName() + " is empty");
                }

                return Singleton<IEasyAssets>.Instance.Retain(new string[] { item.Prefab.path }, null, default);
            }
            catch (Exception e)
            {
                LoggingController.LogError(e.Message);
                LoggingController.LogError(e.StackTrace);

                throw;
            }
        }

        public static KeyComponent FindKeyComponent(this BotOwner botOwner, WorldInteractiveObject door)
        {
            try
            {
                InventoryController inventoryControllerClass = GetInventoryController(botOwner);

                IEnumerable<KeyComponent> matchingKeys = inventoryControllerClass.Inventory.Equipment
                    .GetItemComponentsInChildren<KeyComponent>(false)
                    .Where(k => k.Template.KeyId == door.KeyId);

                if (!matchingKeys.Any())
                {
                    return null;
                }

                return matchingKeys.First();
            }
            catch (Exception e)
            {
                LoggingController.LogError(e.Message);
                LoggingController.LogError(e.StackTrace);

                throw;
            }
        }
    }
}
