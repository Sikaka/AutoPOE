using ExileCore.PoEMemory.Elements;
using ExileCore.Shared.Helpers;
using System.ComponentModel.Design;
using System.Numerics;


namespace AutoPOE.Logic.Actions
{
    public class StartWaveAction : IAction
    {

        private Navigation.Path? _currentPath;


        public async Task<ActionResultType> Tick()
        {
            var item = Core.Map.ClosestValidGroundItem;
            if (item != null) return ActionResultType.Failure;

            var playerPos = Core.GameController.Player.GridPosNum;
            var monolith = Core.GameController.EntityListWrapper.OnlyValidEntities.FirstOrDefault(I => I.Metadata.Contains("Objects/Afflictionator"));


            if (_currentPath == null || _currentPath.IsFinished)
                _currentPath = monolith == null ? Core.Map.FindPath(playerPos, Core.Map.GetSimulacrumCenter()) :
                    Core.Map.FindPath(playerPos, monolith.GridPosNum);


            if (_currentPath != null && !_currentPath.IsFinished)
                await _currentPath.FollowPath();

            if (monolith != null && playerPos.Distance(monolith.GridPosNum) < Core.Settings.NodeSize * 2)
            {
                var screenPos = Controls.GetScreenByWorldPos(monolith.BoundsCenterPosNum);
                await Controls.ClickScreenPos(screenPos);
                return ActionResultType.Success;
            }

            return ActionResultType.Running;
        }
        public void Render()
        {
            _currentPath?.Render();
        }
    }
}
