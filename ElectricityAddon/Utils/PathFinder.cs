using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.MathTools;


namespace ElectricityAddon.Utils;

public class PathFinder
{
    public List<BlockPos> FindShortestPath(BlockPos start, BlockPos end, HashSet<BlockPos> network)
    {
        if (!network.Contains(start) || !network.Contains(end))
            return null;

        var queue = new Queue<BlockPos>();
        queue.Enqueue(start);

        var cameFrom = new Dictionary<BlockPos, BlockPos>();
        cameFrom[start] = null;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (current.Equals(end))
                break;

            foreach (var neighbor in GetNeighbors(current, network))
            {
                if (!cameFrom.ContainsKey(neighbor))
                {
                    queue.Enqueue(neighbor);
                    cameFrom[neighbor] = current;
                }
            }
        }

        return ReconstructPath(start, end, cameFrom);
    }

    private IEnumerable<BlockPos> GetNeighbors(BlockPos pos, HashSet<BlockPos> network)
    {
        return new[]
        {
            new BlockPos(pos.X + 1, pos.Y, pos.Z),
            new BlockPos(pos.X - 1, pos.Y, pos.Z),
            new BlockPos(pos.X, pos.Y + 1, pos.Z),
            new BlockPos(pos.X, pos.Y - 1, pos.Z),
            new BlockPos(pos.X, pos.Y, pos.Z + 1),
            new BlockPos(pos.X, pos.Y, pos.Z - 1)
        }.Where(network.Contains);
    }

    private List<BlockPos> ReconstructPath(BlockPos start, BlockPos end, Dictionary<BlockPos, BlockPos> cameFrom)
    {
        if (!cameFrom.ContainsKey(end))
            return null;

        var path = new List<BlockPos>();
        var current = end;

        while (current != null)
        {
            path.Add(current);
            current = cameFrom[current];
        }

        path.Reverse();
        return path[0].Equals(start) ? path : null;
    }
}