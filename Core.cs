using AutoPOE.Logic.Actions;
using AutoPOE.Navigation;
using ExileCore;
using System.ComponentModel.Design;
using System.Reflection.Metadata.Ecma335;
using Graphics = ExileCore.Graphics;

namespace AutoPOE
{
    public static class Core
    {
        public static bool IsBotRunning = false;
        private static DateTime _nextAction = DateTime.Now;
        public static GameController GameController { get; private set; }
        public static Settings Settings { get; private set; }
        public static Graphics Graphics { get; private set; }
        public static Main Plugin { get; private set; }
        public static Map Map { get; private set; }

        /// <summary>
        /// Initializes the core components. This must be called once when the plugin starts.
        /// </summary>
        public static void Initialize(GameController controller, Settings settings, Graphics graphics, Main plugin)
        {
            GameController = controller;
            Settings = settings;
            Graphics = graphics;
            Plugin = plugin;

            AreaChanged();
        }



        public static bool CanUseAction => DateTime.Now > _nextAction;
        public static void ActionPerformed()
        {
            _nextAction = DateTime.Now.AddMilliseconds(Settings.ActionFrequency);
        }


        public static void AreaChanged()
        {
            Map = new Map();
        }


        public static DateTime NextReviveMercAt = DateTime.Now;
        public static bool ShouldReviveMercenary()
        {

            var leagueElement = GameController.IngameState.IngameUi.LeagueMechanicButtons.GetChildFromIndices(8, 1);
            if (leagueElement == null || !leagueElement.IsVisible)
                return false;
            return leagueElement.Tooltip.GetTextWithNoTags(256).Contains("Revive this Mercenary");
        }
    }
}
