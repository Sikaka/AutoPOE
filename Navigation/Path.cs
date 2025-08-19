using AStar;
using ExileCore.PoEMemory.Components;
using ExileCore.Shared.Helpers;
using System.Numerics;

namespace AutoPOE.Navigation
{
    public class Path
    {
        public bool IsFinished => _nodes.Count == 0;
        private readonly List<Vector2> _nodes;
        public Vector2? Next => _nodes.Count > 0 ? _nodes.First() : null;
        public Vector2 Destination => _nodes.LastOrDefault();
        public DateTime ExpiresAt { get; internal set; } = DateTime.Now.AddSeconds(5);

        public Path(List<Vector2> nodes)
        {
            _nodes = nodes ?? new List<Vector2>();
            if (_nodes.Count < 2)
                return;


            const float Epsilon = 0.001f;
            for (int i = _nodes.Count - 2; i >= 0; i--)
            {
                if (i + 2 < _nodes.Count)
                {
                    var currentSegmentDirection = Vector2.Normalize(_nodes[i + 1] - _nodes[i]);
                    var nextSegmentDirection = Vector2.Normalize(_nodes[i + 2] - _nodes[i + 1]);
                    var dotProduct = Vector2.Dot(currentSegmentDirection, nextSegmentDirection);
                    // If the segments are almost collinear (dot product close to 1), remove the intermediate node.
                    if (Math.Abs(dotProduct - 1.0f) < Epsilon)
                        _nodes.RemoveAt(i + 1);
                }
            }
        }



        public async Task FollowPath()
        {
            //Forciby end the path so it must be re-calculated.
            if(DateTime.Now > ExpiresAt)
            {
                _nodes.Clear();
                return;
            }

            if (IsFinished || Next == null) return;

            var playerPos = Core.GameController.Player.GridPosNum;

            if (_nodes.Count > 1 &&  playerPos.Distance(Next.Value) < 2)
                _nodes.RemoveAt(0);           


            var skill = Core.Settings.GetNextMovementSkill();
            
            await Controls.UseKeyAtGridPos(_nodes.First(), skill);

            if (Core.GameController.Player.GridPosNum.Distance(_nodes.First()) < Core.Settings.NodeSize.Value)
                _nodes.RemoveAt(0);
        }
        public void Render()
        {
            if (IsFinished) return;
            var camera = Core.GameController.IngameState.Camera;
            for (int i = 0; i < _nodes.Count - 1; i++)
            {
                
                var p1 = Controls.GetScreenByGridPos(_nodes[i]);
                var p2 = Controls.GetScreenByGridPos(_nodes[i + 1]);
                Core.Graphics.DrawLine(p1, p2, 2, SharpDX.Color.White);
            }
        }

    }
}
