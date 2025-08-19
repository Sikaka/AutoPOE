using AutoPOE.Logic;
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

        public async Task<ActionResultType> Tick()
        {
            if(SimulacrumState.StoreItemAttemptCount > 100)
            {
                Core.IsBotRunning = false;
                return ActionResultType.Exception;
            }

            if (SimulacrumState.StashPosition == null) return ActionResultType.Exception;

            var playerInventory = Core.GameController.IngameState.Data.ServerData.PlayerInventories[0].Inventory.InventorySlotItems;
            bool hasItems = playerInventory != null && playerInventory.Count > 0;

            if (!hasItems)
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

            return ActionResultType.Success;
        }


        bool IsStashOpen => Core.GameController.IngameState.IngameUi.InventoryPanel.IsVisible;
        public void Render() { }
    }
}
