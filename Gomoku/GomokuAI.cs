using System;
using System.Collections.Generic;

namespace Gomoku
{
    public class GomokuAI
    {
        private readonly Random _rng = new Random();

        // ── Entry Point ────────────────────────────────────────────

        public int[] GetBestMove(GomokuGame game, Player aiPlayer)
        {
            Player[,] board = game.Board;
            int size = GomokuGame.BoardSize;
            Player human = aiPlayer == Player.Black ? Player.White : Player.Black;

            // Opening moves
            if (game.MoveCount == 0)
                return new int[] { size / 2, size / 2 };

            if (game.MoveCount == 1 && board[size / 2, size / 2] == Player.None)
                return new int[] { size / 2, size / 2 };

            List<int[]> candidates = GetCandidates(board, size);

            if (candidates.Count == 0)
            {
                for (int r = 0; r < size; r++)
                    for (int c = 0; c < size; c++)
                        if (board[r, c] == Player.None) return new int[] { r, c };
            }

            int bestScore = int.MinValue;
            List<int[]> bestMoves = new List<int[]>();

            foreach (int[] pos in candidates)
            {
                int r2 = pos[0], c2 = pos[1];
                if (board[r2, c2] != Player.None) continue;

                board[r2, c2] = aiPlayer;
                int atkScore = EvaluatePosition(board, r2, c2, aiPlayer, size);
                board[r2, c2] = Player.None;

                board[r2, c2] = human;
                int defScore = EvaluatePosition(board, r2, c2, human, size);
                board[r2, c2] = Player.None;

                int score;
                if (atkScore >= 100000)
                    score = atkScore * 2;
                else if (defScore >= 100000)
                    score = defScore;
                else
                    score = atkScore + defScore;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMoves.Clear();
                    bestMoves.Add(pos);
                }
                else if (score == bestScore)
                {
                    bestMoves.Add(pos);
                }
            }

            // Prefer move closest to centre among ties
            double bestDist = double.MaxValue;
            int[] result = bestMoves.Count > 0 ? bestMoves[0] : new int[] { size / 2, size / 2 };
            double cx = size / 2.0, cy = size / 2.0;

            foreach (int[] m in bestMoves)
            {
                double d = Math.Pow(m[0] - cy, 2) + Math.Pow(m[1] - cx, 2);
                if (d < bestDist) { bestDist = d; result = m; }
            }
            return result;
        }

        // ── Helpers ────────────────────────────────────────────────

        private static List<int[]> GetCandidates(Player[,] board, int size)
        {
            HashSet<string> keys = new HashSet<string>();
            List<int[]> list = new List<int[]>();

            for (int r = 0; r < size; r++)
                for (int c = 0; c < size; c++)
                {
                    if (board[r, c] == Player.None) continue;
                    for (int dr = -2; dr <= 2; dr++)
                        for (int dc = -2; dc <= 2; dc++)
                        {
                            int nr = r + dr, nc = c + dc;
                            if (nr >= 0 && nr < size && nc >= 0 && nc < size && board[nr, nc] == Player.None)
                            {
                                string key = nr + "," + nc;
                                if (!keys.Contains(key))
                                {
                                    keys.Add(key);
                                    list.Add(new int[] { nr, nc });
                                }
                            }
                        }
                }
            return list;
        }

        private static int EvaluatePosition(Player[,] board, int row, int col, Player player, int size)
        {
            int[][] dirs = { new[] { 0, 1 }, new[] { 1, 0 }, new[] { 1, 1 }, new[] { 1, -1 } };
            int max = 0;

            foreach (int[] d in dirs)
            {
                int count = 1, open = 0;

                int r = row + d[0], c = col + d[1];
                while (r >= 0 && r < size && c >= 0 && c < size && board[r, c] == player)
                { count++; r += d[0]; c += d[1]; }
                if (r >= 0 && r < size && c >= 0 && c < size && board[r, c] == Player.None) open++;

                r = row - d[0]; c = col - d[1];
                while (r >= 0 && r < size && c >= 0 && c < size && board[r, c] == player)
                { count++; r -= d[0]; c -= d[1]; }
                if (r >= 0 && r < size && c >= 0 && c < size && board[r, c] == Player.None) open++;

                int s = GetScore(count, open);
                if (s > max) max = s;
            }
            return max;
        }

        private static int GetScore(int count, int openEnds)
        {
            if (count >= 5) return 100000;
            if (openEnds == 0) return 0;
            if (count == 4) return openEnds == 2 ? 50000 : 5000;
            if (count == 3) return openEnds == 2 ? 3000 : 500;
            if (count == 2) return openEnds == 2 ? 300 : 100;
            if (count == 1) return openEnds == 2 ? 20 : 10;
            return 0;
        }
    }
}
