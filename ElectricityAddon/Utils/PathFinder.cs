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
    public List<BlockPos> FindShortestPath(BlockPos start, BlockPos end, Network network, Dictionary<BlockPos, NetworkPart> parts)
    {
        var networkPositions = network.PartPositions;

        if (!networkPositions.Contains(start) || !networkPositions.Contains(end))
            return null!;

        var queue = new Queue<BlockPos>();
        queue.Enqueue(start);

        var cameFrom = new Dictionary<BlockPos, BlockPos>();  //хранит цепочку пути
        cameFrom[start] = null!;

        var facingFrom = new Dictionary<BlockPos, List<int>>();  //хранит цепочку посещенных граней
        facingFrom[start] = null!;

        bool first = true;
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (current.Equals(end))
                break;


            GetNeighbors(current, network, parts, first)


            foreach (var neighbor in )
            {
                if (!cameFrom.ContainsKey(neighbor))
                {
                    queue.Enqueue(neighbor);
                    cameFrom[neighbor] = current;
                }
            }


            first = false;
        }

        return ReconstructPath(start, end, cameFrom);
    }

    /// <summary>
    /// Вычисляет позиции соседей от текущего значения
    /// </summary>
    /// <param name="pos"></param>
    /// <param name="network"></param>
    /// <returns></returns>
    private (IEnumerable<BlockPos>, IEnumerable<int>) GetNeighbors(BlockPos pos, Network network, Dictionary<BlockPos, NetworkPart> parts, bool first, bool[] processFaces, int startFace)
    {
        var networkPositions = network.PartPositions;

        IEnumerable<BlockPos> Neighbors = Array.Empty<BlockPos>();  //координаты соседей
        IEnumerable<int> NeighborsFace = Array.Empty<int>();        //также с соседом передаем информацию о грани этого соседа с которым мы взаимодействовать будем

        var part = parts[pos];                     //текущий элемент
        var Connections = part.Connection;         //соединения этого элемента


        //нужно отобрать только те Connection, которые относятся к этой цепи
        bool[] faces = new bool[6] { false, false, false, false, false, false };
        
        int i = 0;
        foreach (var net in part.Networks)
        {
            if (net == network)
            {
                faces[i] = true;
            }
            i++;

        }


        bool[] faProcessed;


        if (first)
        {
            faProcessed=new bool[6] { false, false, false, false, false, false };
            startFace = 5;
        }
        else
        {
            faProcessed = processFaces;
        }





        //вместо этого предложили так 
        //оставляем только соединеия этой цепи, сразу выкидывая обработанные
        Facing hereConnections = Facing.None;
        Facing[] faceMasks = new Facing[]
        {
            Facing.NorthAll,  // 0: North
            Facing.EastAll,   // 1: East
            Facing.SouthAll,  // 2: South
            Facing.WestAll,   // 3: West
            Facing.UpAll,     // 4: Up
            Facing.DownAll    // 5: Down
        };


        for (i=0;i<faces.Length;i++)
        {
            if (!faProcessed[i])
            {
                hereConnections |= part.Connection & faceMasks[i];
            }
        }



        //теперь нужно выяснить с какой гранью мы работаем и соединены ли грани одной цепи

        int startFaceIndex = startFace; // Предположим, что startFace уже определён

        // Инициализация BFS
        var queue = new Queue<int>();
        queue.Enqueue(startFaceIndex);
        processFaces[startFaceIndex] = true;

        BlockFacing startBlockFacing = BlockFacing.FromFlag(startFaceIndex);
        BlockFacing oppositeBlockFacing = startBlockFacing.Opposite;

        // Поиск всех связанных граней
        while (queue.Count > 0)
        {
            int currentFaceIndex = queue.Dequeue();
            BlockFacing currentFace = BlockFacing.FromFlag(currentFaceIndex);

            // Получаем соединения текущей грани
            Facing currentFaceMask = FacingHelper.FromFace(currentFace);
            Facing connections = hereConnections & currentFaceMask;

            // Перебираем все направления соединений
            foreach (BlockFacing direction in FacingHelper.Directions(connections))
            {
                int targetFaceIndex = direction.Index;
                if (!processFaces[targetFaceIndex])
                {
                    processFaces[targetFaceIndex] = true;
                    queue.Enqueue(targetFaceIndex);
                }
            }
        }

        // Обновляем hereConnections, оставляя только связи найденных граней
        Facing validConnectionsMask = processFaces
            .Select((processed, index) => processed ? FacingHelper.FromFace(BlockFacing.FromFlag(index)) : Facing.None)
            .Aggregate(Facing.None, (mask, curr) => mask | curr);


        hereConnections &= validConnectionsMask;





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

                        if (networkPositions.Contains(neighborPosition))
                        {
                            Neighbors = Neighbors.AddItem<BlockPos>(neighborPosition);
                            NeighborsFace = NeighborsFace.AddItem<int>(face.Index);
                        }
                    }

                    if ((neighborPart.Connection & FacingHelper.From(direction.Opposite, face)) != 0)
                    {
                        if (networkPositions.Contains(neighborPosition))
                        {
                            Neighbors = Neighbors.AddItem<BlockPos>(neighborPosition);
                            NeighborsFace = NeighborsFace.AddItem<int>(direction.Opposite.Index);
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
                        if (networkPositions.Contains(neighborPosition))
                        {
                            Neighbors = Neighbors.AddItem<BlockPos>(neighborPosition);
                            NeighborsFace = NeighborsFace.AddItem<int>(direction.Opposite.Index);
                        }
                    }

                    if ((neighborPart.Connection & FacingHelper.From(face.Opposite, direction.Opposite)) != 0)
                    {
                        if (networkPositions.Contains(neighborPosition))
                        {
                            Neighbors = Neighbors.AddItem<BlockPos>(neighborPosition);
                            NeighborsFace = NeighborsFace.AddItem<int>(face.Opposite.Index);
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


        return (Neighbors, NeighborsFace);
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