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
        var tree = new List<List<TreeNode>>(); // Список уровней дерева
        var visited = new HashSet<BlockPos>(); // Посещенные узлы на всех уровнях
        var queue = new Queue<TreeNode>();     // Очередь для BFS

        // Инициализация: добавляем начальные позиции в первый уровень
        var initialLevel = new List<TreeNode>();
        foreach (var startPos in startPositions)
        {
            if (network.PartPositions.Contains(startPos) && !visited.Contains(startPos))
            {
                var startNode = new TreeNode(startPos);
                queue.Enqueue(startNode);
                visited.Add(startPos);
                initialLevel.Add(startNode);
            }
        }
        tree.Add(initialLevel); // Добавляем первый уровень в дерево

        // Обход в ширину (BFS) только по общим граням
        while (queue.Count > 0)
        {
            var currentLevelSize = queue.Count; // Количество узлов на текущем уровне
            var nextLevel = new List<TreeNode>(); // Узлы следующего уровня
            var visitedThisLevel = new HashSet<BlockPos>(); // Посещенные узлы на текущем уровне

            for (int i = 0; i < currentLevelSize; i++)
            {
                var currentNode = queue.Dequeue();

                // Проверяем соседей с общей гранью
                foreach (var neighborPos in GetFaceNeighbors(currentNode.Position))
                {
                    if (network.PartPositions.Contains(neighborPos) && !visited.Contains(neighborPos))
                    {
                        visited.Add(neighborPos);
                        visitedThisLevel.Add(neighborPos);
                        var neighborNode = new TreeNode(neighborPos);
                        currentNode.Children.Add(neighborNode); // Добавляем в дочерние узлы
                        queue.Enqueue(neighborNode);
                        nextLevel.Add(neighborNode); // Добавляем в следующий уровень
                    }
                    else if (network.PartPositions.Contains(neighborPos) && visitedThisLevel.Contains(neighborPos))
                    {
                        // Если узел был посещен на текущем уровне, добавляем его в Children
                        var neighborNode = new TreeNode(neighborPos);
                        currentNode.Children.Add(neighborNode);
                    }
                }
            }

            // Если следующий уровень не пуст, добавляем его в дерево
            if (nextLevel.Count > 0)
            {
                tree.Add(nextLevel);
            }
        }

        return tree;
    }

    private static IEnumerable<BlockPos> GetFaceNeighbors(BlockPos pos)
    {
        // Соседи по граням (6 штук)
        yield return new BlockPos(pos.X + 1, pos.Y, pos.Z);
        yield return new BlockPos(pos.X - 1, pos.Y, pos.Z);
        yield return new BlockPos(pos.X, pos.Y + 1, pos.Z);
        yield return new BlockPos(pos.X, pos.Y - 1, pos.Z);
        yield return new BlockPos(pos.X, pos.Y, pos.Z + 1);
        yield return new BlockPos(pos.X, pos.Y, pos.Z - 1);
    }
}
