using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.MathTools;


namespace ElectricityAddon.Utils;
public class TreeNode
{
    public BlockPos Position { get; set; } // Координаты узла
    public List<TreeNode> Children { get; set; } = new List<TreeNode>(); // Дочерние узлы

    public TreeNode(BlockPos position)
    {
        Position = position;
    }
}

public class ChainTreeBuilder
{
    public static List<List<TreeNode>> BuildTree(Network network, BlockPos[] startPositions)
    {
        var tree = new List<List<TreeNode>>();
        var visited = new HashSet<BlockPos>();
        var queue = new Queue<TreeNode>();
        var nodeMap = new Dictionary<BlockPos, TreeNode>(); // Хранит все созданные узлы

        // Инициализация начальных узлов
        var initialLevel = new List<TreeNode>();
        foreach (var pos in startPositions)
        {
            if (network.PartPositions.Contains(pos))
            {
                var node = new TreeNode(pos);
                nodeMap[pos] = node;
                queue.Enqueue(node);
                initialLevel.Add(node);
            }
        }
        tree.Add(initialLevel);

        // Связываем начальные узлы-соседи
        foreach (var node in initialLevel)
        {
            visited.Add(node.Position);
            foreach (var neighborPos in GetFaceNeighbors(node.Position))
            {
                if (nodeMap.TryGetValue(neighborPos, out var neighborNode) &&
                    network.PartPositions.Contains(neighborPos))
                {
                    node.Children.Add(neighborNode);
                }
            }
        }

        // Обход BFS для остальных узлов
        while (queue.Count > 0)
        {
            var currentLevelSize = queue.Count;
            var nextLevel = new List<TreeNode>();

            for (int i = 0; i < currentLevelSize; i++)
            {
                var currentNode = queue.Dequeue();

                foreach (var neighborPos in GetFaceNeighbors(currentNode.Position))
                {
                    if (!network.PartPositions.Contains(neighborPos) || visited.Contains(neighborPos))
                        continue;

                    // Создаем новый узел и связываем
                    var neighborNode = new TreeNode(neighborPos);
                    nodeMap[neighborPos] = neighborNode;
                    currentNode.Children.Add(neighborNode);

                    visited.Add(neighborPos);
                    queue.Enqueue(neighborNode);
                    nextLevel.Add(neighborNode);
                }
            }

            if (nextLevel.Count > 0)
                tree.Add(nextLevel);
        }

        return tree;
    }

    private static IEnumerable<BlockPos> GetFaceNeighbors(BlockPos pos)
    {
        yield return new BlockPos(pos.X + 1, pos.Y, pos.Z);
        yield return new BlockPos(pos.X - 1, pos.Y, pos.Z);
        yield return new BlockPos(pos.X, pos.Y + 1, pos.Z);
        yield return new BlockPos(pos.X, pos.Y - 1, pos.Z);
        yield return new BlockPos(pos.X, pos.Y, pos.Z + 1);
        yield return new BlockPos(pos.X, pos.Y, pos.Z - 1);
    }
}