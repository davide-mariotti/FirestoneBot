namespace Firebot.Core;

/// <summary>
///     Which grid cell an instance takes, kept free of game types so the tests can link it. The first
///     instance (index 0) takes a span x span block in the top-left corner; the others fill the
///     remaining cells left to right, then top to bottom. With span 2 and 5 columns, 17 instances fill
///     a 5x4 grid exactly (4 + 16 cells).
/// </summary>
public static class GridCells
{
    public static (int Col, int Row) Of(int index, int columns, int span)
    {
        if (index <= 0) return (0, 0);
        for (int cell = 0, free = 0;; cell++)
        {
            int col = cell % columns, row = cell / columns;
            if (col < span && row < span) continue;
            if (++free == index) return (col, row);
        }
    }
}
