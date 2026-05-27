using System;
using System.Collections.Generic;

namespace Gomoku
{
    public enum Player { None = 0, Black = 1, White = 2 }

    public class GomokuGame
    {
        public const int BoardSize = 15;

        public Player[,] Board { get; private set; }
        public Player CurrentPlayer { get; private set; }
        public Player Winner { get; private set; }
        public bool IsGameOver { get; private set; }
        public List<int[]> WinningCells { get; private set; }  // each int[]{row, col}
        public int MoveCount { get { return _history.Count; } }

        private readonly List<int[]> _history = new List<int[]>(); // each int[]{row, col}

        public GomokuGame()
        {
            Board = new Player[BoardSize, BoardSize];
            Reset();
        }

        // ── Public API ─────────────────────────────────────────────

        public void Reset()
        {
            Board = new Player[BoardSize, BoardSize];
            CurrentPlayer = Player.Black;
            Winner = Player.None;
            IsGameOver = false;
            WinningCells = null;
            _history.Clear();
        }

        public bool PlacePiece(int row, int col)
        {
            if (IsGameOver) return false;
            if (!InBounds(row, col)) return false;
            if (Board[row, col] != Player.None) return false;

            Board[row, col] = CurrentPlayer;
            _history.Add(new int[] { row, col });

            List<int[]> winning = FindWinningCells(row, col);
            if (winning != null)
            {
                Winner = CurrentPlayer;
                IsGameOver = true;
                WinningCells = winning;
            }
            else if (IsBoardFull())
            {
                IsGameOver = true;
            }
            else
            {
                CurrentPlayer = Opponent(CurrentPlayer);
            }

            return true;
        }

        public bool Undo(int count = 1)
        {
            count = Math.Min(count, _history.Count);
            if (count <= 0) return false;

            for (int i = 0; i < count; i++)
            {
                int[] last = _history[_history.Count - 1];
                Board[last[0], last[1]] = Player.None;
                _history.RemoveAt(_history.Count - 1);
            }

            CurrentPlayer = _history.Count % 2 == 0 ? Player.Black : Player.White;
            Winner = Player.None;
            IsGameOver = false;
            WinningCells = null;
            return true;
        }

        public int[] LastMove
        {
            get { return _history.Count > 0 ? _history[_history.Count - 1] : null; }
        }

        // ── Internals ──────────────────────────────────────────────

        private static Player Opponent(Player p)
        {
            return p == Player.Black ? Player.White : Player.Black;
        }

        private static bool InBounds(int r, int c)
        {
            return r >= 0 && r < BoardSize && c >= 0 && c < BoardSize;
        }

        private bool IsBoardFull()
        {
            for (int r = 0; r < BoardSize; r++)
                for (int c = 0; c < BoardSize; c++)
                    if (Board[r, c] == Player.None) return false;
            return true;
        }

        private List<int[]> FindWinningCells(int row, int col)
        {
            Player p = Board[row, col];
            int[][] dirs = { new[] { 0, 1 }, new[] { 1, 0 }, new[] { 1, 1 }, new[] { 1, -1 } };

            foreach (int[] d in dirs)
            {
                List<int[]> cells = new List<int[]>();
                cells.Add(new int[] { row, col });

                for (int s = 1; s <= 4; s++)
                {
                    int r = row + s * d[0], c = col + s * d[1];
                    if (!InBounds(r, c) || Board[r, c] != p) break;
                    cells.Add(new int[] { r, c });
                }
                for (int s = 1; s <= 4; s++)
                {
                    int r = row - s * d[0], c = col - s * d[1];
                    if (!InBounds(r, c) || Board[r, c] != p) break;
                    cells.Add(new int[] { r, c });
                }

                if (cells.Count >= 5) return cells;
            }
            return null;
        }
    }
}
