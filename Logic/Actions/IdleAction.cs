using ExileCore.Shared.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace AutoPOE.Logic.Actions
{
    public class IdleAction : IAction
    {
        public IdleAction()
        {
            var playerPos = Core.GameController.Player.GridPosNum;
            var mapCenter = Core.Map.GetSimulacrumCenter();

            if (mapCenter == Vector2.Zero || playerPos.Distance(mapCenter) < Core.Settings.NodeSize * 2)
                return;

            _currentPath = Core.Map.FindPath(playerPos, mapCenter);
        }

        private Navigation.Path? _currentPath;
        public async Task<ActionResultType> Tick()
        {
            if (_currentPath != null && !_currentPath.IsFinished)
                await _currentPath.FollowPath();
            return ActionResultType.Running;
        }
        public void Render() { }
    }
}