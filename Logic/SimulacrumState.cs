using ExileCore.PoEMemory.Components;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;

namespace AutoPOE.Logic
{
    public static class SimulacrumState
    {
        private static readonly Stopwatch _runTimer = new Stopwatch();
        public static TimeSpan CurrentRunDuration => _runTimer.Elapsed;
        private static readonly List<double> _runTimes = new List<double>();
        private static readonly List<int> _wavesCompleted = new List<int>();
        public static int TotalRunsCompleted => _runTimes.Count;
        public static double AverageTimePerRun => TotalRunsCompleted > 0 ? _runTimes.Average() : 0;
        public static double AverageWavesCompleted => TotalRunsCompleted > 0 ? _wavesCompleted.Average() : 0;
        public static void RecordRun(int wavesCompleted, double durationSeconds)
        {
            _wavesCompleted.Add(wavesCompleted);
            _runTimes.Add(durationSeconds);
        }



        public static string DebugText = "";


        public static Vector2? MonolithPosition { get; set; }
        public static Vector2? StashPosition { get; set; }
        public static Vector2? PortalPosition { get; set; }
        public static bool IsWaveActive { get; set; }
        public static int CurrentWave { get; set; }
        private static DateTime LastUpdatedAt { get; set; }
        public static DateTime CanStartWaveAt { get; private set; }

        public static int StoreItemAttemptCount = 0;

        public static Vector2 LastPosition = Vector2.Zero;
        public static DateTime LastMovedAt = DateTime.Now;
        public static DateTime LastToggledLootAt = DateTime.Now;

        private static void WaveChanged()
        {
            StoreItemAttemptCount = 0;
        }

        public static void AreaChanged()
        {
            MonolithPosition = null;
            StashPosition = null;
            PortalPosition = null;
            IsWaveActive = false;
            CurrentWave = 0;
            LastUpdatedAt = DateTime.MinValue;
            CanStartWaveAt = DateTime.MinValue;
            _runTimer.Restart();

            WaveChanged();
        }

        public static void Tick()
        {

            var playerPos = Core.GameController.Player.GridPosNum;

            if(playerPos != LastPosition)
            {
                LastPosition = playerPos;
                LastMovedAt = DateTime.Now;
            }

            if (Core.GameController.IngameState.IngameUi.StashElement.VisibleStash != null)
                Core.HasIncubators = Core.GameController.IngameState.IngameUi.StashElement.VisibleStash?.VisibleInventoryItems
                       .Any(item => item.TextureName.Contains("Incubation/")) ?? false;

            var townPortal = Core.GameController.EntityListWrapper.ValidEntitiesByType[ExileCore.Shared.Enums.EntityType.TownPortal]
                    .OrderBy(portal => portal.DistancePlayer)
                    .FirstOrDefault();

            if (townPortal != null) PortalPosition = townPortal.GridPosNum;

            var monolith = Core.GameController.EntityListWrapper.OnlyValidEntities.FirstOrDefault(I => I.Metadata.Contains("Objects/Afflictionator"));

            if (monolith != null)
            {
                MonolithPosition = monolith.GridPosNum;
                var state = monolith.GetComponent<StateMachine>();
                if (state != null)
                {
                    var isWaveActive = state.States.FirstOrDefault(s => s.Name == "active")?.Value > 0 &&
                        state.States.FirstOrDefault(s => s.Name == "goodbye")?.Value == 0;
                    var currentWave = (int)(state.States.FirstOrDefault(s => s.Name == "wave")?.Value ?? 0);

                    if (IsWaveActive && !isWaveActive)
                        CanStartWaveAt = DateTime.Now.AddSeconds(Core.Settings.Simulacrum_MinimumWaveDelay);

                    if (currentWave != CurrentWave)
                        WaveChanged();

                    IsWaveActive = isWaveActive;
                    CurrentWave = currentWave;
                    LastUpdatedAt = DateTime.Now;
                }
            }
            else if (DateTime.Now > LastUpdatedAt.AddSeconds(10))
                IsWaveActive = false;

            if (!StashPosition.HasValue)
            {
                var stash = Core.GameController.EntityListWrapper.OnlyValidEntities.FirstOrDefault(I => I.Metadata.Contains("Metadata/MiscellaneousObjects/Stash"));
                if (stash != null) StashPosition = stash.GridPosNum;
            }
        }
    }
}
