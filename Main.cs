
using AutoPOE.Logic;
using AutoPOE.Logic.Sequences;
using AutoPOE.Logic.Sequences;
using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Helpers;
using System.Windows.Forms;

namespace AutoPOE
{
    public class Main : BaseSettingsPlugin<Settings>
    {
        private bool _wasInSimulacrum = false;
        private ISequence _sequence;
        public override bool Initialise()
        {
            this.Name = "Auto POE";

            Core.Initialize(GameController, Settings, Graphics, this);


            _sequence = new SimulacrumSequence();

            Settings.ConfigureSkills();
            return base.Initialise();
        }



        public override Job Tick()
        {
            if (!Core.IsBotRunning || !Settings.Enable || !GameController.InGame || GameController.IsLoading)
                return null;

            if (Core.CanUseAction)
                _sequence?.Tick();
            return base.Tick();
        }

        public override void Render()
        {
            if (!Settings.Enable || !GameController.InGame || GameController.IngameState.Data == null)
                return;

            if (Settings.StartBot.PressedOnce())
                Core.IsBotRunning = !Core.IsBotRunning;

            _sequence?.Render();

            var drawPos = new System.Numerics.Vector2(100, 200);
            if (!GameController.Area.CurrentArea.IsHideout)
            {
                Graphics.DrawText($"Current Wave: {SimulacrumState.CurrentWave} / 15", drawPos, SharpDX.Color.White);
                drawPos.Y += 20;
                Graphics.DrawText($"Current Duration: {SimulacrumState.CurrentRunDuration:mm\\:ss}", drawPos, SharpDX.Color.White);
                drawPos.Y += 20;
            }

            Graphics.DrawText($"Total Runs: {SimulacrumState.TotalRunsCompleted}", drawPos, SharpDX.Color.White);
            drawPos.Y += 20;
            Graphics.DrawText($"Avg. Time: {TimeSpan.FromSeconds(SimulacrumState.AverageTimePerRun):mm\\:ss}", drawPos, SharpDX.Color.White);
        }

        public override void AreaChange(AreaInstance area)
        {
            var isInSimulacrum = !area.IsHideout;
            if (_wasInSimulacrum && !isInSimulacrum)
                SimulacrumState.RecordRun(SimulacrumState.CurrentWave, SimulacrumState.CurrentRunDuration.TotalSeconds);
            _wasInSimulacrum = isInSimulacrum;


            Core.AreaChanged();
            SimulacrumState.AreaChanged();
            Settings.ConfigureSkills();





        }
    }
}
