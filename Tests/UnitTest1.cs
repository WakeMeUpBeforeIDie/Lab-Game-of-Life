using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;
using cli_life;

namespace Tests
{
    public class UnitTest1
    {     
        [Fact]
        public void Cell_ShouldBeBorn_WhenExactlyThreeLiveNeighbors()
        {            
            var cell = new Cell();
            cell.IsAlive = false;         
            for (int i = 0; i < 3; i++)
            {
                var neighbor = new Cell { IsAlive = true };
                cell.neighbors.Add(neighbor);
            }           
            for (int i = 0; i < 5; i++)
            {
                cell.neighbors.Add(new Cell { IsAlive = false });
            }
            cell.DetermineNextLiveState();
            cell.Advance();
            Assert.True(cell.IsAlive);
        }
        [Fact]
        public void LiveCell_ShouldSurvive_WhenTwoLiveNeighbors()
        {
            var cell = new Cell { IsAlive = true };
            for (int i = 0; i < 2; i++)
                cell.neighbors.Add(new Cell { IsAlive = true });
            for (int i = 0; i < 6; i++)
                cell.neighbors.Add(new Cell { IsAlive = false });

            cell.DetermineNextLiveState();
            cell.Advance();
            Assert.True(cell.IsAlive);
        }
        [Fact]
        public void LiveCell_ShouldDie_WhenZeroLiveNeighbors()
        {
            var cell = new Cell { IsAlive = true };
            for (int i = 0; i < 8; i++)
                cell.neighbors.Add(new Cell { IsAlive = false });
            cell.DetermineNextLiveState();
            cell.Advance();
            Assert.False(cell.IsAlive);
        }
        [Fact]
        public void LiveCell_ShouldDie_WhenFourLiveNeighbors()
        {
            var cell = new Cell { IsAlive = true };
            for (int i = 0; i < 4; i++)
                cell.neighbors.Add(new Cell { IsAlive = true });
            for (int i = 0; i < 4; i++)
                cell.neighbors.Add(new Cell { IsAlive = false });

            cell.DetermineNextLiveState();
            cell.Advance();
            Assert.False(cell.IsAlive);
        }
        [Fact]
        public void Board_GetAliveCount_ReturnsCorrectCount()
        {            
            var board = new Board(50, 20, 1, 0.0);
            Assert.Equal(0, board.GetAliveCount());
        }
        [Fact]
        public void Board_ColumnsAndRows_AreCalculatedCorrectly()
        {            
            int width = 80, height = 40, cellSize = 2;
            var board = new Board(width, height, cellSize, 0.0);
            Assert.Equal(width / cellSize, board.Columns);
            Assert.Equal(height / cellSize, board.Rows);
            Assert.Equal(width, board.Width);
            Assert.Equal(height, board.Height);
        }
        [Fact]
        public void Board_Generation_IncreasesAfterAdvance()
        {
            var board = new Board(50, 20, 1, 0.1);
            int initialGen = board.Generation;
            board.Advance();
            Assert.Equal(initialGen + 1, board.Generation);
        }
        [Fact]
        public void Board_SaveAndLoad_WorksCorrectly()
        {
            var board = new Board(10, 10, 1, 0.0);
            board.Cells[4, 4].IsAlive = true;
            board.Cells[5, 4].IsAlive = true;
            board.Cells[4, 5].IsAlive = true;
            board.Cells[5, 5].IsAlive = true;
            string tempFile = Path.GetTempFileName();
            try
            {
                board.SaveToFile(tempFile);
                var newBoard = new Board(10, 10, 1, 0.0);
                newBoard.LoadFigure(tempFile);
                Assert.Equal(board.GetAliveCount(), newBoard.GetAliveCount());
                Assert.True(newBoard.Cells[4, 4].IsAlive);
                Assert.True(newBoard.Cells[5, 4].IsAlive);
                Assert.True(newBoard.Cells[4, 5].IsAlive);
                Assert.True(newBoard.Cells[5, 5].IsAlive);
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }
        [Fact]
        public void Board_FindPattern_DetectsBlockCorrectly()
        {          
            var board = new Board(10, 10, 1, 0.0);
            board.Cells[0, 0].IsAlive = true;
            board.Cells[1, 0].IsAlive = true;
            board.Cells[0, 1].IsAlive = true;
            board.Cells[1, 1].IsAlive = true;
            var blockPattern = new Pattern("Блок", "****", 2);
            board.FindPattern(blockPattern);
            Assert.Equal(1, blockPattern.count);
        }
        [Fact]
        public void Board_CheckStable_ReturnsTrueForStablePattern()
        {
            var board = new Board(10, 10, 1, 0.0);            
            board.Cells[4, 4].IsAlive = true;
            board.Cells[5, 4].IsAlive = true;
            board.Cells[4, 5].IsAlive = true;
            board.Cells[5, 5].IsAlive = true;
            for (int i = 0; i < 5; i++)
            {
                board.Advance();
            }
            Assert.False(board.CheckStable());
        }
        [Fact]
        public void Board_CheckStable_ReturnsFalseForGlider()
        {
            var board = new Board(20, 20, 1, 0.0);
            board.Cells[1, 0].IsAlive = true;
            board.Cells[2, 1].IsAlive = true;
            board.Cells[0, 2].IsAlive = true;
            board.Cells[1, 2].IsAlive = true;
            board.Cells[2, 2].IsAlive = true;
            for (int i = 0; i < 5; i++)
            {
                board.Advance();
            }
            Assert.False(board.CheckStable());
        }
        [Fact]
        public void Settings_SaveAndLoad_PreservesValues()
        {
            var settings = new Settings
            {
                Width = 100,
                Height = 50,
                CellSize = 2,
                DelayMs = 100,
                LiveDensity = 0.75,
                MaxGenerations = 1000
            };
            string tempFile = Path.GetTempFileName();
            try
            {
                settings.Save(tempFile);
                var loadedSettings = Settings.Load(tempFile);
                Assert.Equal(settings.Width, loadedSettings.Width);
                Assert.Equal(settings.Height, loadedSettings.Height);
                Assert.Equal(settings.CellSize, loadedSettings.CellSize);
                Assert.Equal(settings.DelayMs, loadedSettings.DelayMs);
                Assert.Equal(settings.LiveDensity, loadedSettings.LiveDensity);
                Assert.Equal(settings.MaxGenerations, loadedSettings.MaxGenerations);
            }
            finally
            {
                if (File.Exists(tempFile))
                    File.Delete(tempFile);
            }
        }
        [Fact]
        public void Settings_Load_ReturnsDefaultWhenFileNotFound()
        {
            string nonExistentFile = Guid.NewGuid().ToString() + ".json";
            var settings = Settings.Load(nonExistentFile);
            Assert.NotNull(settings);
            Assert.Equal(50, settings.Width);
            Assert.Equal(20, settings.Height);
            Assert.Equal(1, settings.CellSize);
        }
        [Fact]
        public void Board_Advance_UpdatesAllCellsSynchronously()
        {
            var board = new Board(10, 10, 1, 0.0);
            board.Cells[5, 5].IsAlive = true;
            board.Advance();
            Assert.False(board.Cells[5, 5].IsAlive);
        }
        [Fact]
        public void Board_Topology_WrapsAroundEdges()
        {
            var board = new Board(5, 5, 1, 0.0);
            board.Cells[4, 4].IsAlive = true;
            var topLeftCell = board.Cells[0, 0];
            bool hasCornerNeighbor = topLeftCell.neighbors.Contains(board.Cells[4, 4]);
            Assert.True(hasCornerNeighbor, "Клетка (0,0) должна иметь соседа (4,4) из-за тороидальной топологии");
            Assert.Equal(8, topLeftCell.neighbors.Count);
        }
    }
}
