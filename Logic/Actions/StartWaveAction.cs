using ExileCore.PoEMemory.Elements;
using ExileCore.Shared.Helpers;
using System.ComponentModel.Design;
using System.Numerics;


namespace AutoPOE.Logic.Actions
{
    public class StartWaveAction : IAction
    {

        private Random _random = new Random();
        private Navigation.Path? _currentPath;

        private Vector2 _lastPosition = Vector2.Zero;
        private DateTime _lastMovedAt = DateTime.Now;

        public StartWaveAction()
        {
            //Set an initial path.         
            _currentPath = Core.Map.FindPath(Core.GameController.Player.GridPosNum, SimulacrumState.MonolithPosition ?? Core.Map.GetSimulacrumCenter());
        }

        public async Task<ActionResultType> Tick()
        {
            var item = Core.Map.ClosestValidGroundItem;
            if (item != null) return ActionResultType.Failure;

            var playerPos = Core.GameController.Player.GridPosNum;
            var monolith = Core.GameController.EntityListWrapper.OnlyValidEntities.FirstOrDefault(I => I.Metadata.Contains("Objects/Afflictionator"));

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

            if (monolith != null && playerPos.Distance(monolith.GridPosNum) < Core.Settings.NodeSize * 2)
            {
                var screenPos = Controls.GetScreenByWorldPos(monolith.BoundsCenterPosNum);
                await Controls.ClickScreenPos(screenPos);
                return ActionResultType.Success;
            }
            else if (_currentPath == null || _currentPath.IsFinished)
                _currentPath = Core.Map.FindPath(Core.GameController.Player.GridPosNum, SimulacrumState.MonolithPosition ?? Core.Map.GetSimulacrumCenter());

            return ActionResultType.Running;
        }
        public void Render()
        {
            _currentPath?.Render();
        }
    }
}
