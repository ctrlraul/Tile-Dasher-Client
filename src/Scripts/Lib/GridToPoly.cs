using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

namespace TD.Lib;

public abstract class GridToPoly
{
    public static List<Vector2[]> Generate(bool[,] grid)
    {
        return Merge(GetRectangles(grid));
    }

    private static List<Vector2[]> GetRectangles(bool[,] grid)
    {
        List<Vector2[]> polygons = new();
        int height = grid.GetLength(0);
        int width = grid.GetLength(1);

        bool[,] visited = new bool[height, width];

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (visited[y, x] || !grid[y, x])
                    continue;
                
                Vector2[] polygon = GetRectangleForCell(grid, y, x, visited);
                polygons.Add(polygon);
            }
        }

        return polygons;
    }

    private static Vector2[] GetRectangleForCell(bool[,] grid, int startY, int startX, bool[,] visited)
    {
        int gridHeight = grid.GetLength(0);
        int gridWidth = grid.GetLength(1);
        
        int width = 0;
        int height = 0;

        while (startX + width < gridWidth && grid[startY, startX + width] && !visited[startY, startX + width])
            width++;

        bool isRectangle = true;
        while (startY + height < gridHeight && isRectangle)
        {
            for (int i = 0; i < width; i++)
            {
                if (!grid[startY + height, startX + i] || visited[startY + height, startX + i])
                {
                    isRectangle = false;
                    break;
                }
            }
            
            if (isRectangle)
                height++;
        }

        for (int y = startY; y < startY + height; y++)
            for (int x = startX; x < startX + width; x++)
                visited[y, x] = true;

        return new Vector2[]
        {
            new(startX, startY),
            new(startX + width, startY), 
            new(startX + width, startY + height),
            new(startX, startY + height)
        };
    }
    
    
    // Merging
    
    private static List<Vector2[]> Merge(List<Vector2[]> polygons, uint targetPolygonsCount = 1)
    {
        HashSet<Vector2[]> alreadyMerged = new();
        bool hasChanges;

        do
        {
            hasChanges = false;

            List<Vector2[]> newPolygons = new();
            List<Vector2[]> remainingPolygons = new();

            for (int i = 0; i < polygons.Count; i++)
            {
                Vector2[] polygonA = polygons[i];

                if (alreadyMerged.Contains(polygonA))
                    continue;

                bool merged = false;

                for (int j = i + 1; j < polygons.Count; j++)
                {
                    Vector2[] polygonB = polygons[j];

                    if (alreadyMerged.Contains(polygonB) || !ArePolygonsTouching(polygonA, polygonB))
                        continue;
                    
                    Vector2[] mergedPolygon = GetMergedPolygonOrNull(polygonA, polygonB);
                    
                    if (mergedPolygon == null)
                        continue;
                    
                    newPolygons.Add(mergedPolygon);
                    alreadyMerged.Add(polygonA);
                    alreadyMerged.Add(polygonB);
                    
                    hasChanges = true;
                    merged = true;
                    
                    break;
                }

                if (!merged)
                    remainingPolygons.Add(polygonA);
            }

            polygons = remainingPolygons.Concat(newPolygons).ToList();

            if (polygons.Count <= targetPolygonsCount) // Good enough!
                break;

        } while (hasChanges);

        return polygons;
    }

    private static bool ArePolygonsTouching(Vector2[] polygonA, Vector2[] polygonB)
    {
        return (
            polygonA.Any(vertex => Geometry2D.IsPointInPolygon(vertex, polygonB)) ||
            polygonB.Any(vertex => Geometry2D.IsPointInPolygon(vertex, polygonA))
        );
    }

    private static Vector2[] GetMergedPolygonOrNull(Vector2[] polygonA, Vector2[] polygonB)
    {
        Array<Vector2[]> mergeResult = Geometry2D.MergePolygons(polygonA, polygonB);
        return mergeResult.Count == 1 ? mergeResult[0] : null;
    }
}