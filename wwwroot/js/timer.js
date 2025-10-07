using Sudoku.Web.Services;
using Xunit;

public class SudokuTests {
    [Fact]
    public void GenerateBoard_ShouldNotBeNull() {
        var service = new SudokuService();
        var (board, fixedCells) = service.GenerateBoard(4, "easy");
        Assert.NotNull(board);
        Assert.Equal(4, board.GetLength(0));
    }
}
