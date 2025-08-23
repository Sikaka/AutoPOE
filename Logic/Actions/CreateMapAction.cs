using AutoPOE.Logic;
using ExileCore;
using ExileCore.Shared.Enums;
using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace AutoPOE.Logic.Actions
{
    public class CreateMapAction : IAction
    {
        private Random _random = new Random();
        private Navigation.Path? _currentPath;
        bool isMapOpened = false;

        public async Task<ActionResultType> Tick()
        {
            if (!Core.GameController.Area.CurrentArea.IsHideout)
                return ActionResultType.Success;

            var playerPos = Core.GameController.Player.GridPosNum;

            if (DateTime.Now > SimulacrumState.LastMovedAt.AddSeconds(4))
            {
                var nextPos = Core.GameController.Player.GridPosNum + new Vector2(_random.Next(-50, 50), _random.Next(-50, 50));
                await Controls.UseKeyAtGridPos(nextPos, Core.Settings.GetNextMovementSkill());
                return ActionResultType.Exception;
            }

            if (isMapOpened)
            {
                var portal = Core.GameController.EntityListWrapper.ValidEntitiesByType[EntityType.TownPortal]
                    .OrderBy(e => e.DistancePlayer)
                    .FirstOrDefault();

                if (portal == null) return ActionResultType.Running;

                await Controls.ClickScreenPos(Controls.GetScreenByWorldPos(portal.BoundsCenterPosNum));
                await Task.Delay(750);
                return ActionResultType.Running;
            }

            var mapDeviceEntity = Core.GameController.EntityListWrapper.OnlyValidEntities.FirstOrDefault(I => I.Type == EntityType.IngameIcon && I.Path.EndsWith("MappingDevice"));
            if (mapDeviceEntity == null)
                return ActionResultType.Exception;

            var mapDeviceWindow = Core.GameController.IngameState.IngameUi.MapDeviceWindow;
            if (!mapDeviceWindow.IsVisible)
            {
                if (_currentPath == null)
                    _currentPath = Core.Map.FindPath(playerPos, mapDeviceEntity.GridPosNum);
                if (_currentPath != null && !_currentPath.IsFinished)                
                    await _currentPath.FollowPath();                
                else
                {
                    await Controls.ClickScreenPos(Controls.GetScreenByWorldPos(mapDeviceEntity.BoundsCenterPosNum));
                    await Task.Delay(500);
                }
                return ActionResultType.Running;
            }
            else
            {
                var activateButton = mapDeviceWindow.ActivateButton;
                if (activateButton.IsActive)
                {
                    var center = activateButton.GetClientRect().Center;
                    await Controls.ClickScreenPos(new Vector2(center.X, center.Y));
                    await Task.Delay(5000);
                    isMapOpened = true;
                }
                else
                {
                    var mapStashPanel = mapDeviceWindow.GetChildFromIndices(0, 1);
                    var anySimulacrum = mapStashPanel?.Children.FirstOrDefault(I => I.TextureName.EndsWith("DeliriumFragment.dds"));

                    if (anySimulacrum == null)
                    {
                        SimulacrumState.DebugText = "No simulacrums found";
                        Core.IsBotRunning = false;
                        return ActionResultType.Exception;
                    }

                    var center = anySimulacrum.GetClientRect().Center;
                    await Controls.ClickScreenPos(new Vector2(center.X, center.Y), true, false, true);
                    await Task.Delay(500);
                }
            }

            return ActionResultType.Running;
        }

        public void Render()
        {
            _currentPath?.Render();
        }
    }
}
