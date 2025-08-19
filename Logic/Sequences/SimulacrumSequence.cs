using AutoPOE.Logic.Actions;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.Shared.Enums;
using System.Numerics;

namespace AutoPOE.Logic.Sequences
{
    public class SimulacrumSequence : ISequence
    {
        private Task<ActionResultType> _currentTask;
        private IAction _currentAction;

        public void Tick()
        {
            SimulacrumState.Tick();
            var nextAction = GetNextAction();
            if (_currentAction == null || _currentAction.GetType() != nextAction.GetType())
            {
                _currentAction = nextAction;
                _currentTask = _currentAction.Tick();
            }
            if (_currentTask == null || _currentTask.IsCompleted)
            {
                if (_currentTask?.Result == ActionResultType.Success || _currentTask?.Result == ActionResultType.Failure)
                    _currentAction = GetNextAction();
                _currentTask = _currentAction.Tick();
            }
        }

        public void Render()
        {
            var actionName = _currentAction?.GetType().Name ?? "None";
            Core.Graphics.DrawText($"Running: {Core.IsBotRunning} Current Action: {actionName}. {SimulacrumState.DebugText}", new Vector2(100, 100), SharpDX.Color.White);
            _currentAction?.Render();
        }


        private IAction GetNextAction()
        {

            if (!Core.GameController.Player.IsAlive && Core.GameController.IngameState.IngameUi.ResurrectPanel.IsVisible)
                return new ReviveAction();

            if (Core.GameController.Area.CurrentArea.IsHideout)
                return new CreateMapAction();

            if (!SimulacrumState.MonolithPosition.HasValue)
                return new ExploreAction();


            if (Core.Map.ClosestValidGroundItem != null)
                return new LootAction();
            
            if (!SimulacrumState.IsWaveActive && SimulacrumState.StashPosition.HasValue && 
                (GetStorableInventoryCount >= Core.Settings.StoreItemThreshold || _currentTask?.GetType() == typeof(StoreItemsAction)))
                return new StoreItemsAction();

            if (Core.Settings.UseIncubators && !SimulacrumState.IsWaveActive && SimulacrumState.HasAvailableIncubators && HasEmptyIncubatorSlots())
                return new ApplyIncubatorsAction();

            if (SimulacrumState.IsWaveActive)
                return Core.Map.ClosestTargetableMonster != null ? new CombatAction() : new ExploreAction();

            else if (DateTime.Now > SimulacrumState.CanStartWaveAt)
                return SimulacrumState.CurrentWave < 15 ? new StartWaveAction() : new LeaveMapAction();


            return new IdleAction();
        }


        private static int GetStorableInventoryCount => Core.GameController.IngameState.Data.ServerData.PlayerInventories[0].Inventory.InventorySlotItems.Count;

        private bool HasEmptyIncubatorSlots()
        {
            var equipment = Core.GameController.IngameState.ServerData.PlayerInventories
                .Where(inv => inv.Inventory.InventSlot >= InventorySlotE.BodyArmour1 && inv.Inventory.InventSlot <= InventorySlotE.Belt1 && inv.Inventory.Items.Count == 1)
                .Select(inv => inv.Inventory);

            foreach (var equip in equipment)
            {
                var incubatorName = equip.Items.FirstOrDefault()?.GetComponent<Mods>()?.IncubatorName;
                if (string.IsNullOrEmpty(incubatorName))
                {
                    return true; // Found an empty slot
                }
            }
            return false; // No empty slots
        }
    }
}
