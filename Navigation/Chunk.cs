using System.Numerics;

namespace AutoPOE.Navigation
{

    /// <summary>
    /// Intended use is to confirm map exploration % and to make choosing random destinations more efficent
    ///     Weight of a chunk is how many individual valid coordinates there are. Higher weight means it can see more of the walkable terrain by navigating to it.
    /// </summary>
    public class Chunk
    {
        public Vector2 Position { get; set; }

        public int Weight { get; set; }

        public bool IsRevealed { get; set; } = false;
    }
}
