using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.Shared.Attributes;
using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;
using System;
using static AutoPOE.Settings.Skill;

namespace AutoPOE
{
    public class Settings : ISettings
    {

        private readonly Random _random = new Random();


        public ToggleNode Enable { get; set; } = new ToggleNode(false);
        public HotkeyNode StartBot { get; set; } = (HotkeyNode)Keys.Insert;


        [Menu("Action Frequency", "What is the minimm time between inputs?")]
        public RangeNode<int> ActionFrequency { get; set; } = new RangeNode<int>(100, 25, 500);

        [Menu("Clamp Size")]
        public RangeNode<int> ClampSize { get; set; } = new RangeNode<int>(400, 100, 1000);

        [Menu("Combat Distance", "How close to stand to monsters to fight them.")]
        public RangeNode<int> CombatDistance { get; set; } = new RangeNode<int>(15, 5, 50);


        [Menu("Pathfinding Node Size", "How close to a pathfinding node before we consider it 'complete'.")]
        public RangeNode<int> NodeSize { get; set; } = new RangeNode<int>(20, 10, 100);


        [Menu("View Distance", "How close must we be to consider objects as visible. Used to explore maps.")]
        public RangeNode<int> ViewDistance { get; set; } = new RangeNode<int>(80, 10, 500);

        [Menu("Simulacrum Wave Delay", "Minimum time (in seconds) after wave ends before starting a new one.")]
        public RangeNode<int> Simulacrum_MinimumWaveDelay { get; set; } = new RangeNode<int>(5, 1, 20);


        [Menu("Use Incubators", "Should we apply incubators (from stash) to our equipment?")]
        public ToggleNode UseIncubators { get; set; } = new ToggleNode(true);

        [Menu("Revive Mercenary", "Should we revive our mercenary if they die?")]
        public ToggleNode ReviveMercenary { get; set; } = new ToggleNode(false);


        [Menu("Hide/Show Loot (Unstuck)", "Should the bot double press the Z key to reposition loot icons? Can help if loot icons are off screen or unable to be clicked.")]
        public ToggleNode LootItemsUnstick { get; set; } = new ToggleNode(false);

        [Menu("Store Item Threshold", "How many items before we dump to stash.")]
        public RangeNode<int> StoreItemThreshold { get; set; } = new RangeNode<int>(20, 5, 60);


        [Menu("Detonate Mines Key")]
        public HotkeyNode DetonateMinesKey { get; set; } = (HotkeyNode)Keys.D;
        public ToggleNode ShouldDetonateMines { get; set; } = new ToggleNode(false);

        /// <summary>
        /// Finds an available movement skill that is ready to be cast.
        /// </summary>
        public Keys GetNextMovementSkill()
        {
            var usableSkillNames = GetUsableSkillNames();
            var allSkills = new List<Skill> { Skill1, Skill2, Skill3, Skill4, Skill5, Skill6 };

            var validMovementSkills = allSkills
                .Where(s => s.IsMovementKey.Value && usableSkillNames.Contains(s.SkillName.Value) && DateTime.Now > s.NextCast)
                .ToList();

            if (!validMovementSkills.Any()) return Keys.E;

            var selected = validMovementSkills[_random.Next(validMovementSkills.Count)];
            selected.NextCast = DateTime.Now.AddMilliseconds(selected.MinimumDelay.Value);
            return selected.Hotkey.Value;
        }

        public List<Skill> GetAvailableMonsterTargetingSkills()
        {
            var usableSkillNames = GetUsableSkillNames();
            var allSkills = new List<Skill> { Skill1, Skill2, Skill3, Skill4, Skill5, Skill6 };

            return allSkills
                .Where(I => DateTime.Now > I.NextCast &&
                usableSkillNames.Contains(I.SkillName) &&
                (I.CastType == CastTypeSort.TargetMonster.ToString() || I.CastType == CastTypeSort.TargetRareMonster.ToString() || I.CastType == CastTypeSort.TargetUniqueMonster.ToString()))
                .ToList();
        }

        /// <summary>
        /// Finds an available combat skill for a specific target type.
        /// </summary>
        public Skill GetNextCombatSkill(Skill.CastTypeSort targetType)
        {
            var usableSkillNames = GetUsableSkillNames();
            var allSkills = new List<Skill> { Skill1, Skill2, Skill3, Skill4, Skill5, Skill6 };

            var validCombatSkills = allSkills
                .Where(s => s.CastType.Value == targetType.ToString() &&
                        usableSkillNames.Contains(s.SkillName.Value) &&
                        DateTime.Now > s.NextCast &&
                        (string.IsNullOrEmpty(s.Buff.Value) || Core.GameController.Player.Buffs.FirstOrDefault(buff => buff.Name == s.Buff.Value) == null))
                .ToList();

            if (!validCombatSkills.Any()) return null;

            var selected = validCombatSkills[_random.Next(validCombatSkills.Count)];
            selected.NextCast = DateTime.Now.AddMilliseconds(selected.MinimumDelay.Value);
            return selected;
        }

        private HashSet<string> GetUsableSkillNames()
        {
            return Core.GameController.Player.GetComponent<Actor>()?.ActorSkills
                .Where(s => s.CanBeUsed && !string.IsNullOrEmpty(s.Name))
                .Select(s => s.Name)
                .ToHashSet() ?? new HashSet<string>();
        }

        public Skill Skill1 { get; set; } = new Skill { Hotkey = (HotkeyNode)Keys.Q };
        public Skill Skill2 { get; set; } = new Skill { Hotkey = (HotkeyNode)Keys.W };
        public Skill Skill3 { get; set; } = new Skill { Hotkey = (HotkeyNode)Keys.E };
        public Skill Skill4 { get; set; } = new Skill { Hotkey = (HotkeyNode)Keys.R };
        public Skill Skill5 { get; set; } = new Skill { Hotkey = (HotkeyNode)Keys.T };
        public Skill Skill6 { get; set; } = new Skill { Hotkey = (HotkeyNode)Keys.F };


        /// <summary>
        /// Set all skill slots to the proper spell name. Lets us look it up later easily. 
        /// </summary>
        public void ConfigureSkills()
        {
            var skillOptions = Core.GameController.Player.GetComponent<Actor>()?.ActorSkills?
                .Where(I => I.Name.Trim().Length > 1 && !I.Name.StartsWith("?") && I.IsOnSkillBar)
                .Select(I => I.Name)
                .Distinct()
                .ToList();

            Skill1.SkillName.SetListValues(skillOptions);
            Skill2.SkillName.SetListValues(skillOptions);
            Skill3.SkillName.SetListValues(skillOptions);
            Skill4.SkillName.SetListValues(skillOptions);
            Skill5.SkillName.SetListValues(skillOptions);
            Skill6.SkillName.SetListValues(skillOptions);

            var allSkills = new List<Skill> { Skill1, Skill2, Skill3, Skill4, Skill5, Skill6 };

            foreach (var skill in allSkills.Where(I => I.SkillName == "Move"))
                skill.MinimumDelay.Value = 100;


            foreach (var skill in allSkills.Where(I => I.SkillName == "Righteous Fire"))
                skill.Buff.Value = "righteous_fire";
        }
        

        [Submenu(CollapsedByDefault = true)]
        public class Skill
        {
            public Skill()
            {
                CastType.SetListValues([.. Enum.GetNames(typeof(CastTypeSort))]);
            }

            [Menu("Hotkey", "Physical key that the skill is bound to.")]
            public HotkeyNode Hotkey { get; set; } = (HotkeyNode)Keys.Q;



            [Menu("Is Movement Skill", "Should this hotkey be used for navigation")]
            public ToggleNode IsMovementKey { get; set; } = new ToggleNode(false);


            [Menu("Skill Name", "Name of skill.")]
            public ListNode SkillName { get; set; } = new ListNode() { Value = "None"};


            [Menu("Buff Name", "Name of buff to block skill from re-casting.")]
            public TextNode Buff { get; set; } = "";


            [Menu("Cast Type", "Targeting behavior for skill casting.")]
            public ListNode CastType { get; set; } = new ListNode() { Value = CastTypeSort.TargetMonster.ToString() };

            [Menu("Minimum Delay", "Minimum time (in milliseconds) between casting")]
            public RangeNode<int> MinimumDelay { get; set; } = new RangeNode<int>(1000, 100, 60000);            

            /// <summary>
            /// Used to track when the next skill can be used (from minimum delay).
            /// </summary>
            public DateTime NextCast = DateTime.MinValue;

            public enum CastTypeSort
            {
                DoNotUse,
                TargetMonster,
                TargetRareMonster,
                TargetUniqueMonster,
                TargetSelf,
                TargetMercenary
            }
        }
    }
}


