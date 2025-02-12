using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using Vintagestory.API.MathTools;


namespace ElectricityAddon.Utils;

public class PathFinder
{
    /// <summary>
    /// Реализует обход в ширину для поиска кратчайшего пути
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="networkPositions"></param>
    /// <returns></returns>
    public List<BlockPos> FindShortestPath(BlockPos start, BlockPos end, HashSet<BlockPos> networkPositions, ref Dictionary<BlockPos, NetworkPart> parts)
    {
        if (!networkPositions.Contains(start) || !networkPositions.Contains(end))
            return null!;

        var queue = new Queue<BlockPos>();
        queue.Enqueue(start);

        var cameFrom = new Dictionary<BlockPos, BlockPos>();
        cameFrom[start] = null!;

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (current.Equals(end))
                break;


            foreach (var neighbor in GetNeighbors(current, networkPositions, ref parts))
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

    /// <summary>
    /// Вычисляет позиции соседей от текущего значения
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="network"></param>
    /// <returns></returns>
    private IEnumerable<BlockPos> GetNeighbors(BlockPos pos, HashSet<BlockPos> network, ref Dictionary<BlockPos, NetworkPart> parts)
    {
        //сразу делаем список возможных соседей по граням пока
        /*
        IEnumerable <BlockPos> Neighbors = new[]
        {
            new BlockPos(pos.X + 1, pos.Y, pos.Z),
            new BlockPos(pos.X - 1, pos.Y, pos.Z),
            new BlockPos(pos.X, pos.Y + 1, pos.Z),
            new BlockPos(pos.X, pos.Y - 1, pos.Z),
            new BlockPos(pos.X, pos.Y, pos.Z + 1),
            new BlockPos(pos.X, pos.Y, pos.Z - 1)
        }.Where(network.Contains);                 //берем тех, которые есть в этой же цепи
        */


        IEnumerable<BlockPos> Neighbors= Array.Empty<BlockPos>();



        var part = parts[pos];                     //текущий элемент
        var Connections = part.Connection;         //соединения этого элемента

        

        foreach (var direction in FacingHelper.Directions(Connections))
        {
            var directionFilter = FacingHelper.FromDirection(direction);
            var neighborPosition = part.Position.AddCopy(direction);

            if (parts.TryGetValue(neighborPosition, out var neighborPart))         //проверяет, если в той стороне сосед
            {
                foreach (var face in FacingHelper.Faces(Connections & directionFilter))
                {
                    if ((neighborPart.Connection & FacingHelper.From(face, direction.Opposite)) != 0)
                    {
                        
                        if (network.Contains(neighborPosition))  
                        {
                            Neighbors=Neighbors.AddItem<BlockPos>(neighborPosition);
                        }
                    }
                    
                    if ((neighborPart.Connection & FacingHelper.From(direction.Opposite, face)) != 0)
                    {
                        if (network.Contains(neighborPosition))  
                        {
                            Neighbors = Neighbors.AddItem<BlockPos>(neighborPosition);
                        }
                    }
                }
            }
        }

        foreach (var direction in FacingHelper.Directions(Connections))
        {
            var directionFilter = FacingHelper.FromDirection(direction);

            foreach (var face in FacingHelper.Faces(Connections & directionFilter))
            {
                var neighborPosition = part.Position.AddCopy(direction).AddCopy(face);

                if (parts.TryGetValue(neighborPosition, out var neighborPart))
                {
                    if ((neighborPart.Connection & FacingHelper.From(direction.Opposite, face.Opposite)) != 0)
                    {
                        if (network.Contains(neighborPosition))
                        {
                            Neighbors = Neighbors.AddItem<BlockPos>(neighborPosition);
                        }
                    }

                    if ((neighborPart.Connection & FacingHelper.From(face.Opposite, direction.Opposite)) != 0)
                    {
                        if (network.Contains(neighborPosition))
                        {
                            Neighbors = Neighbors.AddItem<BlockPos>(neighborPosition);
                        }
                    }
                }
            }
        }













        /*Facing[] hereFaces = Enum.GetValues<Facing>( parts[pos].Connection);  //все направления в этой координате


        foreach (Facing flag in Enum.GetValues<Facing>())
        {
            if (flag == Facing.None)
                continue;

            if ((parts[pos].Connection & flag) == flag)
            {
                
            }
        }




        return new[]
        {
            new BlockPos(pos.X + 1, pos.Y, pos.Z),
            new BlockPos(pos.X - 1, pos.Y, pos.Z),
            new BlockPos(pos.X, pos.Y + 1, pos.Z),
            new BlockPos(pos.X, pos.Y - 1, pos.Z),
            new BlockPos(pos.X, pos.Y, pos.Z + 1),
            new BlockPos(pos.X, pos.Y, pos.Z - 1)
        }.Where(network.Contains);

        */


        return Neighbors;
    }

    /// <summary>
    /// Реконструирует путь, который был найден
    /// </summary>
    /// <param name="start"></param>
    /// <param name="end"></param>
    /// <param name="cameFrom"></param>
    /// <returns></returns>
    private List<BlockPos> ReconstructPath(BlockPos start, BlockPos end, Dictionary<BlockPos, BlockPos> cameFrom)
    {
        if (!cameFrom.ContainsKey(end))
            return null!;

        var path = new List<BlockPos>();
        var current = end;

        while (current != null)
        {
            path.Add(current);
            current = cameFrom[current];
        }

        path.Reverse();
        return path[0].Equals(start) ? path : null!;
    }
}