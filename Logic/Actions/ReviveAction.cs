using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace AutoPOE.Logic.Actions
{
    public class ReviveAction : IAction
    {

        private Random random = new Random();
        public async Task<ActionResultType> Tick()
        {
            if (!Core.GameController.Player.IsDead) return ActionResultType.Success;
            var revivePanel = Core.GameController.IngameState.IngameUi.ResurrectPanel;
            var atCheckpoint = revivePanel.ResurrectAtCheckpoint;
            var center = revivePanel.ResurrectAtCheckpoint.GetClientRect().Center;
            await Task.Delay(random.Next(500, 1500));
            await Controls.ClickScreenPos(new System.Numerics.Vector2(center.X, center.Y));
            await Task.Delay(random.Next(500, 1500));
            return ActionResultType.Success;
        }
        public void Render() { }
    }
}
