using ExileCore.PoEMemory.Elements;
using ExileCore.Shared.Helpers;
using System.ComponentModel.Design;
using System.Numerics;


namespace AutoPOE.Logic.Actions
{
    public class LootAction : IAction
    {

        private Navigation.Path? _currentPath;
        private uint? _targetItemId;
        private DateTime _actionTimeout = DateTime.Now.AddSeconds(2);


        public async Task<ActionResultType> Tick()
        {
            var item = Core.Map.ClosestValidGroundItem;
            var playerPos = Core.GameController.Player.GridPosNum;

            if (item == null)
                return ActionResultType.Success;

            if (item.Entity.Id != _targetItemId)
            {
                _targetItemId = item.Entity.Id;
                _currentPath = null;
            }

            if (DateTime.Now > _actionTimeout)
            {
                //The looting task timed out. We'll assume it failed to click and toggle the loot icons on and off.
                if (Core.Settings.LootItemsUnstick)
                {
                    await Controls.UseKey(Keys.Z);
                    await Task.Delay(Core.Settings.ActionFrequency * 2);
                    await Controls.UseKey(Keys.Z);
                    await Task.Delay(Core.Settings.ActionFrequency * 2);
                }
                return ActionResultType.Failure;
            }

            if (playerPos.Distance(item.Entity.GridPosNum) < Core.Settings.NodeSize)
            {
                _currentPath = null;
                var labelCenter = item.ClientRect.Center;
                await Controls.ClickScreenPos(new Vector2(labelCenter.X, labelCenter.Y));
                return ActionResultType.Success;
            }

            if (_currentPath == null)
            {
                _currentPath = Core.Map.FindPath(playerPos, item.Entity.GridPosNum);
                if (_currentPath == null)
                {
                    Core.Map.BlacklistItemId(_targetItemId.Value);
                    return ActionResultType.Failure;
                }
            }

            await _currentPath.FollowPath();
            return ActionResultType.Running;
        }

        public void Render()
        {
            var possibleItems = Core.GameController.IngameState.IngameUi.ItemsOnGroundLabelElement.VisibleGroundItemLabels
                .Where(item=>item != null &&
                        item.Label != null &&
                        item.Entity != null &&
                        item.Label.IsVisibleLocal &&
                        item.Label.Text != null &&
                        !item.Label.Text.EndsWith(" Gold"));

            foreach (var item in possibleItems)
                Core.Graphics.DrawCircle(new Vector2(item.ClientRect.Center.X, item.ClientRect.Center.Y), 10, SharpDX.Color.Pink);

            Core.Graphics.DrawText($"Count: {possibleItems.Count()}  ID: {_targetItemId} Timeout: {_actionTimeout}", new Vector2(125, 125));
            _currentPath?.Render();
            
        }
    }
}
