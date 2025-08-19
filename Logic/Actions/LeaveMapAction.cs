using ExileCore.Shared.Helpers;
using System;
using System.Numerics;
namespace AutoPOE.Logic.Actions
{
    public class LeaveMapAction : IAction
    {
        public LeaveMapAction()
        {
            if(SimulacrumState.PortalPosition == null) return;
            _currentPath = Core.Map.FindPath(Core.GameController.Player.GridPosNum, SimulacrumState.PortalPosition.Value);
        }

        private Random _random = new Random();
        private Navigation.Path? _currentPath;
        private Vector2 _lastPosition = Vector2.Zero;
        private DateTime _lastMovedAt = DateTime.Now;

        public async Task<ActionResultType> Tick()
        {
            //Cannot find portal in memory OR we have items we could be looting.
            if (Core.Map.ClosestValidGroundItem != null || SimulacrumState.PortalPosition == null) 
                return ActionResultType.Exception;

            var playerPos = Core.GameController.Player.GridPosNum;
            if (playerPos != _lastPosition)
            {
                _lastMovedAt = DateTime.Now;
                _lastPosition = playerPos;
            }


            if ((DateTime.Now - _lastMovedAt).TotalSeconds > 2)
            {
                var nextPos = Core.GameController.Player.GridPosNum + new Vector2(_random.Next(-50, 50), _random.Next(-50, 50));
                await Controls.UseKeyAtGridPos(nextPos, Core.Settings.GetNextMovementSkill());
                return ActionResultType.Failure;
            }

            if (_currentPath != null && !_currentPath.IsFinished)
            {
                await _currentPath.FollowPath();
                return ActionResultType.Running;
            }

            var townPortal = Core.GameController.EntityListWrapper.ValidEntitiesByType[ExileCore.Shared.Enums.EntityType.TownPortal]
                .Where(I => I.DistancePlayer < Core.Settings.NodeSize * 2)
                .FirstOrDefault();
            if (townPortal == null) return ActionResultType.Exception;

            if (playerPos.Distance(townPortal.GridPosNum) > Core.Settings.NodeSize * 2)
            {
                _currentPath = Core.Map.FindPath(Core.GameController.Player.GridPosNum, SimulacrumState.PortalPosition.Value);
                return ActionResultType.Running;
            }

            await Controls.ClickScreenPos(Controls.GetScreenByWorldPos(townPortal.BoundsCenterPosNum));
            return ActionResultType.Success;
        }
        public void Render() { }
    }
}
