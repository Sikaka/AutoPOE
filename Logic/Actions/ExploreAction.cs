using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Numerics;
using AutoPOE.Navigation;
using ExileCore.Shared.Helpers;

namespace AutoPOE.Logic.Actions
{    
    public class ExploreAction : IAction
    {
        public ExploreAction()
        {
            //Set all chunks to not having been revealed yet. Start exploring from 0.
            Core.Map.ResetAllChunks();

            //Set our starting path to be to walk to center of the map.
            _currentPath = Core.Map.FindPath(Core.GameController.Player.GridPosNum, Core.Map.GetSimulacrumCenter());
        }

        private Random _random = new Random();
        private Navigation.Path? _currentPath;

        private Vector2 _lastPosition = Vector2.Zero;
        private DateTime _lastMovedAt = DateTime.Now;


        public async Task<ActionResultType> Tick()
        {
            Core.Map.UpdateRevealedChunks();
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

            var nextChunk = Core.Map.GetNextUnrevealedChunk();
            if (nextChunk == null)
                return ActionResultType.Success;

            _currentPath = Core.Map.FindPath(Core.GameController.Player.GridPosNum, nextChunk.Position);
            return ActionResultType.Running;
        }

        public void Render()
        {
            Core.Graphics.DrawText($"Area Name: '{Core.GameController.Area.CurrentArea.Name}' Path: {_currentPath?.Next}", new Vector2(115, 115));
            _currentPath?.Render();
        }
    }
}
