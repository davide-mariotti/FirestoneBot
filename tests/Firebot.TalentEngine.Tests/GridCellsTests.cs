using System.Linq;
using Firebot.Core;
using Xunit;

namespace Firebot.TalentEngine.Tests;

// GridCells is linked in from src/Core: it has no game dependencies.
public class GridCellsTests
{
    [Fact]
    public void Span1_IsThePlainRowMajorGrid()
    {
        for (var i = 0; i < 20; i++) Assert.Equal((i % 5, i / 5), GridCells.Of(i, 5, 1));
    }

    [Fact]
    public void Span2_FillsAFiveByFourGridWithSeventeenInstances()
    {
        var cells = Enumerable.Range(1, 16).Select(i => GridCells.Of(i, 5, 2)).ToList();

        Assert.Equal((2, 0), cells[0]);   // Steam-1 right of Steam-0's block
        Assert.Equal((0, 2), cells[6]);   // Steam-7 starts the third row
        Assert.Equal((4, 3), cells[15]);  // Steam-16 in the last cell
        Assert.Equal(16, cells.Distinct().Count());
        Assert.DoesNotContain(cells, c => c.Col < 2 && c.Row < 2);
    }
}
