using AutoPOE.Logic;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using System;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;

namespace AutoPOE.Logic.Actions
{
    public class StoreItemsAction : IAction
    {
        public StoreItemsAction()
        {
            if (SimulacrumState.StashPosition == null) return;
            _currentPath = Core.Map.FindPath(Core.GameController.Player.GridPosNum, SimulacrumState.StashPosition.Value);
        }

        private Navigation.Path? _currentPath;

        private static readonly Dictionary<InventorySlotE, Vector2> EquipmentSlotPositions = new Dictionary<InventorySlotE, Vector2>
        {
            { InventorySlotE.BodyArmour1, new Vector2(1587, 296) },
            { InventorySlotE.Weapon1, new Vector2(1369, 233) },
            { InventorySlotE.Offhand1, new Vector2(1784, 232) },
            { InventorySlotE.Helm1, new Vector2(1585, 165) },
            { InventorySlotE.Amulet1, new Vector2(1690, 245) },
            { InventorySlotE.Ring1, new Vector2(1480, 300) },
            { InventorySlotE.Ring2, new Vector2(1687, 306) },
            { InventorySlotE.Gloves1, new Vector2(1453, 398) },
            { InventorySlotE.Boots1, new Vector2(1719, 391) },
            { InventorySlotE.Belt1, new Vector2(1584, 421) }
        };

        public async Task<ActionResultType> Tick()
        {
            if (SimulacrumState.StoreItemAttemptCount > 100)
            {
                Core.IsBotRunning = false;
                return ActionResultType.Exception;
            }

            if (SimulacrumState.StashPosition == null) return ActionResultType.Exception;

            var playerInventory = Core.GameController.IngameState.Data.ServerData.PlayerInventories[0].Inventory.InventorySlotItems;

            if (IsFinished)
            {
                await Controls.ClosePanels();
                return ActionResultType.Success;
            }

            if (_currentPath != null && !_currentPath.IsFinished)
            {
                await _currentPath.FollowPath();
                return ActionResultType.Running;
            }

            var playerPos = Core.GameController.Player.GridPosNum;
            if (playerPos.Distance(SimulacrumState.StashPosition.Value) > Core.Settings.NodeSize * 2)
            {
                _currentPath = Core.Map.FindPath(Core.GameController.Player.GridPosNum, SimulacrumState.StashPosition.Value);
                return ActionResultType.Running;
            }

            //Open stash if not open.
            if (!IsStashOpen)
            {
                var stashObj = Core.GameController.EntityListWrapper.OnlyValidEntities.FirstOrDefault(I => I.Metadata.Contains("Metadata/MiscellaneousObjects/Stash"));
                if (stashObj == null) return ActionResultType.Exception;

                await Controls.ClickScreenPos(Controls.GetScreenByGridPos(stashObj.GridPosNum));
                await Task.Delay(300);
            }

            //If stash failed to open, exit out.
            if (!IsStashOpen) return ActionResultType.Exception;

            foreach (var item in playerInventory)
            {
                if (!Core.GameController.IngameState.IngameUi.InventoryPanel.IsVisible) return ActionResultType.Failure;
                var center = item.GetClientRect().Center;
                await Controls.ClickScreenPos(new Vector2(center.X, center.Y), isLeft: true, exactPosition: false, holdCtrl: true);
                await Task.Delay(Core.Settings.ActionFrequency);
                SimulacrumState.StoreItemAttemptCount++;
            }

            if (Core.Settings.UseIncubators)
                while (await ApplyAnyIncubator())                
                    await Task.Delay(400);

            return ActionResultType.Success;
        }


        // Action is finished if inventory is empty and either no equipment can accept incubators OR no incubators remain in stash.
        bool IsFinished => Core.GameController.IngameState.Data.ServerData.PlayerInventories[0].Inventory.InventorySlotItems.Count == 0 && (FindEmptyEquipmentSlot() == null || FindIncubatorInStash() == null);
        bool IsStashOpen => Core.GameController.IngameState.IngameUi.InventoryPanel.IsVisible;

        private async Task<bool> ApplyAnyIncubator()
        {
            var incubatorToApply = FindIncubatorInStash();
            var targetSlot = FindEmptyEquipmentSlot();

            if (incubatorToApply == null || targetSlot == null)
                return false;

            await Controls.ClickScreenPos(incubatorToApply.Value, false, true);
            if (!await WaitForCursorState(MouseActionType.UseItem, 2000))
                return false;

            await Controls.ClickScreenPos(EquipmentSlotPositions[targetSlot.Value]);

            if(Core.GameController.IngameState.IngameUi.Cursor.Action == MouseActionType.HoldItem)
            {
                //We picked up our equipment. Emergency stop!
                Core.IsBotRunning = false;
                return false;
            }

            if (!await WaitForCursorState(MouseActionType.Free, 2000))
                return false;

            return true;
        }

        private async Task<bool> WaitForCursorState(MouseActionType expectedState, int timeoutMs)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            while (stopwatch.ElapsedMilliseconds < timeoutMs)
            {
                if (Core.GameController.IngameState.IngameUi.Cursor.Action == expectedState)
                {
                    return true;
                }
                await Task.Delay(Core.Settings.ActionFrequency);
            }
            return false;
        }

        private InventorySlotE? FindEmptyEquipmentSlot()
        {
            var equipment = Core.GameController.IngameState.ServerData.PlayerInventories
                .Where(inv => inv.Inventory.InventSlot >= InventorySlotE.BodyArmour1 && inv.Inventory.InventSlot <= InventorySlotE.Belt1 && inv.Inventory.Items.Count == 1)
                .Select(inv => inv.Inventory)
                .ToList();

            foreach (var equip in equipment)
            {
                var incubatorName = equip.Items.FirstOrDefault()?.GetComponent<Mods>()?.IncubatorName;
                if (string.IsNullOrEmpty(incubatorName))
                    return equip.InventSlot;
            }
            return null;
        }

        private Vector2? FindIncubatorInStash()
        {
            var center = Core.GameController.IngameState.IngameUi.StashElement.VisibleStash?.VisibleInventoryItems
                .FirstOrDefault(item => item.TextureName.Contains("Incubation/"))?.GetClientRect().Center;

            return center == null ? null : new Vector2(center.Value.X, center.Value.Y);
        }
        public void Render() { }
    }
}
