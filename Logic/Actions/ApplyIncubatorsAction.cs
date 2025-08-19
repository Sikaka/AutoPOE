using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using System.Numerics;

namespace AutoPOE.Logic.Actions
{
    /// <summary>
    /// Checks for available incubators in the stash and applies them to empty equipment slots.
    /// This action is designed to apply ONE incubator per execution.
    /// </summary>
    public class ApplyIncubatorsAction : IAction
    {
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
            //Path to stash if required
            if (SimulacrumState.StashPosition == null) return ActionResultType.Failure;
            if (Core.GameController.Player.GridPosNum.Distance(SimulacrumState.StashPosition.Value) > Core.Settings.NodeSize)
            {
                _currentPath ??= Core.Map.FindPath(Core.GameController.Player.GridPosNum, SimulacrumState.StashPosition.Value);
                if (_currentPath != null && !_currentPath.IsFinished)
                {
                    await _currentPath.FollowPath();
                    return ActionResultType.Running;
                }
            }

            //Open stash if not already opened
            var stashPanel = Core.GameController.IngameState.IngameUi.StashElement;
            if (!stashPanel.IsVisible)
            {
                var stashObj = Core.GameController.EntityListWrapper.OnlyValidEntities.FirstOrDefault(I => I.Metadata.Contains("Metadata/MiscellaneousObjects/Stash"));
                if (stashObj == null) return ActionResultType.Failure;

                await Controls.ClickScreenPos(Controls.GetScreenClampedGridPos(stashObj.GridPosNum));
                await Task.Delay(500);
                return ActionResultType.Running;
            }

            //Find incubator and target item to apply it to.
            var incubatorToApply = FindIncubatorInStash();
            var targetSlot = FindEmptyEquipmentSlot();

            // If there's nothing to do, the action is a success.
            if (incubatorToApply == null || targetSlot == null)
            {
                await Controls.ClosePanels();
                return ActionResultType.Success;
            }

            await Controls.ClickScreenPos(incubatorToApply.Value, false, true);
            var cursorUpdated = await WaitForCursorState(MouseActionType.UseItem, 2000);
            if (!cursorUpdated) return ActionResultType.Failure;

            await Controls.ClickScreenPos(EquipmentSlotPositions[targetSlot.Value]);

            // Wait for the cursor to clear. If the item state is still wrong then we need to stop immediately.
            var cursorCleared = await WaitForCursorState(MouseActionType.Free, 2000);
            if (!cursorCleared)
            {
                switch (Core.GameController.IngameState.IngameUi.Cursor.Action)
                {
                    //Incubator, can continue and just press escape.
                    case MouseActionType.UseItem:
                        await Controls.ClosePanels();
                        break;
                    //Picked up our equipment. Emergency stop!
                    case MouseActionType.HoldItem:
                        Core.IsBotRunning = false;
                        break;
                }
            }

            await Controls.ClosePanels();

            return ActionResultType.Success;
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

            return center == null? null: new Vector2(center.Value.X, center.Value.Y);
        }

        public void Render()
        {
            _currentPath?.Render();
        }
    }
}
