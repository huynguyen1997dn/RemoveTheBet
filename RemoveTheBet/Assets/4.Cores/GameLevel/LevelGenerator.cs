using System;
using System.Collections.Generic;
using UnityEngine;

public static class LevelGenerator
{
    private static readonly Vector2Int[] FourDirs = new Vector2Int[]
    {
        new Vector2Int(0, 1),   // Up
        new Vector2Int(1, 0),   // Right
        new Vector2Int(0, -1),  // Down
        new Vector2Int(-1, 0)    // Left
    };

    public static void Generate(GameLevelConfig config)
    {
        int seed = config.seed == 0 ? Guid.NewGuid().GetHashCode() : config.seed;
        System.Random rng = new System.Random(seed);

        int width = config.Width;
        int height = config.Height;
        int minSize = config.minSnakeSize;
        int maxSize = config.maxSnakeSize;
        int targetSnakeCount = config.snakeCount;

        int[,] grid = new int[width, height];
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                grid[x, y] = -1;

        config.ClearSegments();

        int snakeValue = 1;
        int maxAttempts = 50;
        int snakesPlaced = 0;

        List<Vector2Int> allEmptyPositions = new List<Vector2Int>();
        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                allEmptyPositions.Add(new Vector2Int(x, y));

        while (snakesPlaced < targetSnakeCount)
        {
            Shuffle(rng, allEmptyPositions);

            bool placed = false;
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                foreach (var startPos in allEmptyPositions)
                {
                    if (grid[startPos.x, startPos.y] != -1) continue;

                    int snakeSize = rng.Next(minSize, maxSize + 1);
                    bool isLShape = rng.NextDouble() > 0.5f;

                    List<Vector2Int> path;
                    if (isLShape)
                    {
                        path = TryGenerateLShape(startPos, snakeSize, width, height, grid, rng);
                    }
                    else
                    {
                        path = TryGenerateStraight(startPos, snakeSize, width, height, grid, rng);
                    }

                    if (path != null && path.Count >= minSize)
                    {
                        Vector2Int head = path[0];
                        Vector2Int tail = path[path.Count - 1];

                        int[] bodyIndices = new int[path.Count - 2];
                        for (int i = 1; i < path.Count - 1; i++)
                        {
                            bodyIndices[i - 1] = path[i].y * width + path[i].x;
                        }

                        foreach (var pos in path)
                        {
                            grid[pos.x, pos.y] = snakeValue;
                        }

                        int headIdx = head.y * width + head.x;
                        int tailIdx = tail.y * width + tail.x;
                        config.AddSegment(snakeValue, head.x, head.y, tail.x, tail.y, bodyIndices);

                        snakeValue++;
                        snakesPlaced++;
                        placed = true;
                        break;
                    }
                }

                if (placed) break;
            }

            if (!placed) break;
        }

        for (int x = 0; x < width; x++)
            for (int y = 0; y < height; y++)
                config.SetValue(x, y, grid[x, y]);
    }

    private static List<Vector2Int> TryGenerateStraight(Vector2Int start, int size, int width, int height, int[,] grid, System.Random rng)
    {
        Shuffle(rng, FourDirs);

        foreach (var dir in FourDirs)
        {
            var path = new List<Vector2Int>();
            path.Add(start);

            Vector2Int current = start;
            bool valid = true;

            for (int i = 1; i < size; i++)
            {
                current = new Vector2Int(current.x + dir.x, current.y + dir.y);

                if (current.x < 0 || current.x >= width || current.y < 0 || current.y >= height)
                {
                    valid = false;
                    break;
                }

                if (grid[current.x, current.y] != -1)
                {
                    valid = false;
                    break;
                }

                path.Add(current);
            }

            if (valid && path.Count >= size - 1)
            {
                return path;
            }
        }

        return null;
    }

    private static List<Vector2Int> TryGenerateLShape(Vector2Int start, int size, int width, int height, int[,] grid, System.Random rng)
    {
        int minPartSize = Mathf.CeilToInt(size / 2f);
        minPartSize = Mathf.Max(2, minPartSize);

        for (int part1Size = minPartSize; part1Size <= size - minPartSize; part1Size++)
        {
            int part2Size = size - part1Size;
            if (part2Size < 2) continue;

            Shuffle(rng, FourDirs);

            foreach (var dir1 in FourDirs)
            {
                Vector2Int perpDir = new Vector2Int(-dir1.y, dir1.x);
                Vector2Int[] turnDirs = new Vector2Int[] { perpDir, new Vector2Int(-perpDir.x, -perpDir.y) };

                foreach (var dir2 in turnDirs)
                {
                    var path = new List<Vector2Int>();
                    path.Add(start);

                    Vector2Int current = start;
                    bool valid = true;

                    for (int i = 1; i < part1Size; i++)
                    {
                        current = new Vector2Int(current.x + dir1.x, current.y + dir1.y);

                        if (current.x < 0 || current.x >= width || current.y < 0 || current.y >= height ||
                            grid[current.x, current.y] != -1)
                        {
                            valid = false;
                            break;
                        }
                        path.Add(current);
                    }

                    if (!valid) continue;

                    Vector2Int turnPoint = current;

                    for (int i = 1; i < part2Size; i++)
                    {
                        current = new Vector2Int(current.x + dir2.x, current.y + dir2.y);

                        if (current.x < 0 || current.x >= width || current.y < 0 || current.y >= height ||
                            grid[current.x, current.y] != -1)
                        {
                            valid = false;
                            break;
                        }
                        path.Add(current);
                    }

                    if (valid && path.Count >= 2)
                    {
                        return path;
                    }
                }
            }
        }

        return TryGenerateStraight(start, size, width, height, grid, rng);
    }

    private static void Shuffle<T>(System.Random rng, IList<T> list)
    {
        int n = list.Count;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T temp = list[k];
            list[k] = list[n];
            list[n] = temp;
        }
    }
}