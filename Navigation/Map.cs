using AStar;
using AStar.Options;
using ExileCore;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using ExileCore.Shared.Interfaces;
using GameOffsets;
using System.Numerics;

namespace AutoPOE.Navigation
{
    public class Map
    {
        private List<uint> _blacklistItemIds = [];
        private readonly WorldGrid _worldGrid;
        private readonly PathFinder _pathFinder;
        private Chunk[,] _chunks;

        public IReadOnlyList<Chunk> Chunks { get; private set; }

        public Map()
        {
            TerrainData terrain = Core.GameController.IngameState.Data.Terrain;

            int gridWidth = ((int)terrain.NumCols - 1) * 23;
            int gridHeight = ((int)terrain.NumRows - 1) * 23;

            if (gridWidth % 2 != 0)
            {
                gridWidth++;
            }

            _worldGrid = new WorldGrid(gridHeight, gridWidth + 1);

            _pathFinder = new PathFinder(_worldGrid, new PathFinderOptions()
            {
                PunishChangeDirection = false,
                UseDiagonals = true,
                SearchLimit = gridWidth * gridHeight
            });

            PopulateWorldGrid(terrain, _worldGrid, Core.GameController.Memory);
            InitializeChunks(10, _worldGrid.Width, _worldGrid.Height);

            // Initialize the read-only Chunks list once after _chunks array is populated.
            Chunks = _chunks.Cast<Chunk>().ToList().AsReadOnly();
        }


        /// <summary>
        /// Populates the WorldGrid based on terrain melee layer data.
        /// </summary>
        /// <param name="terrain">The terrain data.</param>
        /// <param name="worldGrid">The world grid to populate.</param>
        /// <param name="memory">The memory accessor.</param>
        private static void PopulateWorldGrid(TerrainData terrain, WorldGrid worldGrid, IMemory memory)
        {
            byte[] layerMeleeBytes = memory.ReadBytes(terrain.LayerMelee.First, terrain.LayerMelee.Size);
            int currentByteOffset = 0;

            for (int row = 0; row < worldGrid.Height; ++row)
            {
                for (int column = 0; column < worldGrid.Width; column += 2)
                {
                    if (currentByteOffset + (column >> 1) >= layerMeleeBytes.Length) break;

                    byte tileValue = layerMeleeBytes[currentByteOffset + (column >> 1)];
                    worldGrid[row, column] = (short)((tileValue & 0xF) > 0 ? 1 : 0);
                    if (column + 1 < worldGrid.Width)
                        worldGrid[row, column + 1] = (short)((tileValue >> 4) > 0 ? 1 : 0);
                }
                currentByteOffset += terrain.BytesPerRow;
            }
        }

        /// <summary>
        /// Initializes the chunk grid for map exploration.
        /// </summary>
        /// <param name="chunkResolution">The resolution of each chunk.</param>
        /// <param name="worldGridWidth">The width of the world grid.</param>
        /// <param name="worldGridHeight">The height of the world grid.</param>
        private void InitializeChunks(int chunkResolution, int worldGridWidth, int worldGridHeight)
        {
            int chunksX = (int)Math.Ceiling((double)worldGridWidth / chunkResolution);
            int chunksY = (int)Math.Ceiling((double)worldGridHeight / chunkResolution);
            _chunks = new Chunk[chunksX, chunksY];

            for (int x = 0; x < chunksX; ++x)
            {
                for (int y = 0; y < chunksY; ++y)
                {
                    int chunkStartX = x * chunkResolution;
                    int chunkStartY = y * chunkResolution;
                    int chunkEndX = Math.Min(chunkStartX + chunkResolution, worldGridWidth);
                    int chunkEndY = Math.Min(chunkStartY + chunkResolution, worldGridHeight);

                    int totalWeight = 0;
                    for (int col = chunkStartX; col < chunkEndX; ++col)
                    {
                        for (int row = chunkStartY; row < chunkEndY; ++row)
                        {
                            totalWeight += _worldGrid[row, col];
                        }
                    }

                    _chunks[x, y] = new Chunk()
                    {
                        Position = new Vector2(
                            (float)chunkStartX + (chunkResolution / 2f),
                            (float)chunkStartY + (chunkResolution / 2f)
                        ),
                        Weight = totalWeight
                    };
                }
            }
        }

        public void ResetAllChunks()
        {
            foreach (var chunk in _chunks)
                chunk.IsRevealed = false;
        }

        public void UpdateRevealedChunks()
        {
            var playerPos = Core.GameController.Player.GridPosNum;
            foreach (var chunk in Chunks.Where(c => !c.IsRevealed))
            {
                if (playerPos.Distance(chunk.Position) < Core.Settings.ViewDistance)
                {
                    chunk.IsRevealed = true;
                }
            }
        }

        public Chunk? GetNextUnrevealedChunk()
        {
            return Chunks
                .Where(c => !c.IsRevealed && c.Weight > 0) 
                .OrderBy(c => c.Position.Distance(Core.GameController.Player.GridPosNum))
                .ThenByDescending(c => c.Weight) 
                .FirstOrDefault();
        }


        public Path? FindPath(Vector2 start, Vector2 end)
        {
            Point[] pathPoints = _pathFinder.FindPath(new Point((int)start.X, (int)start.Y), new Point((int)end.X, (int)end.Y));
            if (pathPoints == null || pathPoints.Length == 0)
            {
                return null;
            }

            // Convert Point[] to List<Vector2> directly
            List<Vector2> pathVectors = new List<Vector2>(pathPoints.Length);
            foreach (Point p in pathPoints)
                pathVectors.Add(new Vector2((float)p.X, (float)p.Y));


            var cleanedNodes = new List<Vector2> { pathVectors[0] };
            var lastKeptNode = pathVectors[0];

            for (int i = 1; i < pathVectors.Count - 1; i++)
            {
                var currentNode = pathVectors[i];
                if (Vector2.Distance(currentNode, lastKeptNode) >= Core.Settings.NodeSize)
                {
                    cleanedNodes.Add(currentNode);
                    lastKeptNode = currentNode;
                }
            }
            cleanedNodes.Add(pathVectors.Last());
            pathVectors = cleanedNodes;
            return new Path(pathVectors);
        }



        private T? FindClosestGeneric<T>(IEnumerable<T> source, Func<T, bool> predicate, Func<T, float> distanceSelector) where T : class
        {
            return source.Where(predicate).MinBy(distanceSelector);
        }




        public Entity? ClosestTargetableMonster =>
            FindClosestGeneric(Core.GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Monster],
            monster => monster.IsAlive && monster.IsTargetable && monster.IsHostile && !monster.IsDead,
            monster => monster.DistancePlayer);

        public (Vector2 Position, float Weight) FindBestFightingPosition()
        {
            var playerPos = Core.GameController.Player.GridPosNum;
            var bestPos = playerPos;
            var bestWeight = GetPositionFightWeight(bestPos);

            var candidateMonsters = Core.GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Monster]
                .Where(m => m.IsHostile && m.IsAlive && m.GridPosNum.Distance(playerPos) >Core.Settings.CombatDistance.Value * 1);

            foreach (var monster in candidateMonsters)
            {
                var testPos = monster.GridPosNum;
                var testWeight = GetPositionFightWeight(testPos);

                if (testWeight > bestWeight)
                {
                    bestWeight = testWeight;
                    bestPos = testPos;
                }
            }

            return (bestPos, bestWeight);
        }

        public float GetPositionFightWeight(Vector2 position)
        {
            return Core.GameController.EntityListWrapper.ValidEntitiesByType[EntityType.Monster]
                .Where(m => m.IsHostile && m.IsAlive && !m.IsDead && m.GridPosNum.Distance(position) < Core.Settings.CombatDistance)
                .Sum(m => GetMonsterRarityWeight(m.Rarity));
        }

        public static int GetMonsterRarityWeight(MonsterRarity rarity)
        {
            return rarity switch
            {
                MonsterRarity.Magic => 3,
                MonsterRarity.Rare => 10,
                MonsterRarity.Unique => 25,
                _ => 1,
            };
        }


        /// <summary>
        /// Blacklist an item so it is not considered when using ClosestValidGroundItem.
        ///     This collection is cleared between area changes. 
        /// </summary>
        /// <param name="id"></param>
        public void BlacklistItemId(uint id)
        {
            if (!_blacklistItemIds.Contains(id))
                _blacklistItemIds.Add(id);
        }


        public ItemsOnGroundLabelElement.VisibleGroundItemDescription? ClosestValidGroundItem =>
            FindClosestGeneric(Core.GameController.IngameState.IngameUi.ItemsOnGroundLabelElement.VisibleGroundItemLabels,
                item => item != null &&
                        item.Label != null &&
                        item.Entity != null &&
                        item.Label.IsVisibleLocal &&
                        item.Label.Text != null &&
                        !item.Label.Text.EndsWith(" Gold") &&
                        !_blacklistItemIds.Contains(item.Entity.Id),
                item => item.Entity.DistancePlayer);


        public Vector2 GetSimulacrumCenter()
        {
            switch (Core.GameController.Area.CurrentArea.Name)
            {
                case "The Bridge Enraptured":
                    return new Vector2(551, 624);
                case "Oriath Delusion":
                    return new Vector2(587, 253);
                case "The Syndrome Encampment":
                    return new Vector2(316, 253);
                case "Hysteriagate":
                    return new Vector2(183, 269);
                case "Lunacy's Watch":
                    return new Vector2(270, 687);
                default: return Vector2.Zero;
            }
        }
    }
}