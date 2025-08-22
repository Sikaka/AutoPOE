using AutoPOE.Logic;
using ExileCore;
using ExileCore.Shared.Enums;
using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;

namespace AutoPOE.Logic.Actions
{
    public class ReviveMercenaryAction : IAction
    {
        public async Task<ActionResultType> Tick()
        {
            if (!Core.ShouldReviveMercenary()) return ActionResultType.Exception;
            var leagueElement = Core.GameController.IngameState.IngameUi.LeagueMechanicButtons.GetChildFromIndices(8, 1);
            if (leagueElement == null || !leagueElement.IsVisible)
                return ActionResultType.Failure;
            await Controls.ClickScreenPos(new Vector2(leagueElement.Center.X, leagueElement.Center.Y));
            await Task.Delay(500);

            Core.NextReviveMercAt = DateTime.Now.AddMinutes(2);
            return ActionResultType.Success;
        }

        public void Render() { }
    }
}
