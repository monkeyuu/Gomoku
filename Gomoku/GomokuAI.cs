using System;
using System.Collections.Generic;

namespace Gomoku
{
    public enum Difficulty { Easy, Medium, Hard, Master }

    public class GomokuAI
    {
        public Difficulty Difficulty { get; set; } = Difficulty.Medium;

        private readonly Random _rng = new Random();
        private const int Size = GomokuGame.BoardSize;

        // ── Entry point ────────────────────────────────────────────

        public int[] GetBestMove(GomokuGame game, Player aiPlayer)
        {
            if (game.MoveCount == 0)
                return new int[] { Size / 2, Size / 2 };
            if (game.MoveCount == 1 && game.Board[Size / 2, Size / 2] == Player.None)
                return new int[] { Size / 2, Size / 2 };

            switch (Difficulty)
            {
                case Difficulty.Easy:   return EasyMove(game.Board, aiPlayer);
                case Difficulty.Hard:   return HeuristicMove(game.Board, aiPlayer, defWeight: 130);
                case Difficulty.Master: return MasterMove(game.Board, aiPlayer);
                default:                return HeuristicMove(game.Board, aiPlayer, defWeight: 100);
            }
        }

        // ── Easy: 50 % random, only blocks/takes 4-in-a-row ────────

        private int[] EasyMove(Player[,] board, Player ai)
        {
            Player human = Opp(ai);
            List<int[]> cands = GetCandidates(board);
            if (cands.Count == 0) return Center();

            // Always take an immediate win
            foreach (int[] m in cands)
            {
                if (board[m[0], m[1]] != Player.None) continue;
                board[m[0], m[1]] = ai;
                bool win = HasWon(board, m[0], m[1], ai);
                board[m[0], m[1]] = Player.None;
                if (win) return m;
            }

            // 50 % chance: pick a random valid candidate
            if (_rng.NextDouble() < 0.50)
            {
                List<int[]> valid = EmptyFrom(cands, board);
                if (valid.Count > 0) return valid[_rng.Next(valid.Count)];
            }

            // Block only if human has an open four
            foreach (int[] m in cands)
            {
                if (board[m[0], m[1]] != Player.None) continue;
                board[m[0], m[1]] = human;
                int s = EvalPos(board, m[0], m[1], human);
                board[m[0], m[1]] = Player.None;
                if (s >= 5000) return m;
            }

            // Random fallback
            List<int[]> fallback = EmptyFrom(cands, board);
            return fallback.Count > 0 ? fallback[_rng.Next(fallback.Count)] : Center();
        }

        // ── Medium / Hard: 1-ply heuristic ────────────────────────

        private int[] HeuristicMove(Player[,] board, Player ai, int defWeight)
        {
            Player human = Opp(ai);
            List<int[]> cands = GetCandidates(board);
            if (cands.Count == 0) return Center();

            int best = int.MinValue;
            List<int[]> bestMoves = new List<int[]>();

            foreach (int[] m in cands)
            {
                if (board[m[0], m[1]] != Player.None) continue;

                board[m[0], m[1]] = ai;
                int atk = EvalPos(board, m[0], m[1], ai);
                board[m[0], m[1]] = Player.None;

                board[m[0], m[1]] = human;
                int def = EvalPos(board, m[0], m[1], human);
                board[m[0], m[1]] = Player.None;

                int score;
                if (atk >= 100000)      score = atk * 2;
                else if (def >= 100000) score = def;
                else                    score = atk + def * defWeight / 100;

                if (score > best)
                {
                    best = score;
                    bestMoves.Clear();
                    bestMoves.Add(m);
                }
                else if (score == best)
                {
                    bestMoves.Add(m);
                }
            }

            return PreferCenter(bestMoves) ?? Center();
        }

        // ── Master: 2-ply minimax with alpha-beta pruning ──────────

        private int[] MasterMove(Player[,] board, Player ai)
        {
            Player human = Opp(ai);
            // Pre-sort candidates so best moves are tried first (improves pruning)
            List<int[]> cands = SortedCandidates(board, ai, human, topN: 16);
            if (cands.Count == 0) return Center();

            int bestScore = int.MinValue;
            int[] bestMove = cands[0];
            int alpha = int.MinValue;

            foreach (int[] m in cands)
            {
                if (board[m[0], m[1]] != Player.None) continue;
                board[m[0], m[1]] = ai;

                // Immediate win — no need to search further
                if (HasWon(board, m[0], m[1], ai))
                {
                    board[m[0], m[1]] = Player.None;
                    return m;
                }

                int score = Minimax(board, depth: 2, isMax: false, ai, human,
                                    alpha, beta: int.MaxValue);
                board[m[0], m[1]] = Player.None;

                if (score > bestScore)
                {
                    bestScore = score;
                    bestMove  = m;
                    alpha     = Math.Max(alpha, bestScore);
                }
            }

            return bestMove;
        }

        private int Minimax(Player[,] board, int depth, bool isMax,
                            Player ai, Player human, int alpha, int beta)
        {
            if (depth == 0) return EvalBoard(board, ai, human);

            Player mover = isMax ? ai : human;
            List<int[]> cands = GetCandidates(board);
            int limit = Math.Min(cands.Count, 12);   // cap for speed
            int best  = isMax ? int.MinValue : int.MaxValue;

            for (int i = 0; i < limit; i++)
            {
                int[] m = cands[i];
                if (board[m[0], m[1]] != Player.None) continue;
                board[m[0], m[1]] = mover;

                if (HasWon(board, m[0], m[1], mover))
                {
                    board[m[0], m[1]] = Player.None;
                    int terminal = isMax ? 80000 + depth : -(80000 + depth);
                    return terminal;   // prune immediately on terminal node
                }

                int score = Minimax(board, depth - 1, !isMax, ai, human, alpha, beta);
                board[m[0], m[1]] = Player.None;

                if (isMax)
                {
                    if (score > best)  best  = score;
                    if (best  > alpha) alpha = best;
                }
                else
                {
                    if (score < best) best = score;
                    if (best  < beta) beta = best;
                }
                if (beta <= alpha) break;   // alpha-beta cutoff
            }

            return (best == int.MinValue || best == int.MaxValue) ? 0 : best;
        }

        // ── Board evaluation (used by minimax leaf nodes) ──────────

        private static int EvalBoard(Player[,] board, Player ai, Player human)
        {
            int aiMax = 0, huMax = 0;
            for (int r = 0; r < Size; r++)
                for (int c = 0; c < Size; c++)
                {
                    if (board[r, c] == ai)
                    {
                        int s = EvalPos(board, r, c, ai);
                        if (s > aiMax) aiMax = s;
                    }
                    else if (board[r, c] == human)
                    {
                        int s = EvalPos(board, r, c, human);
                        if (s > huMax) huMax = s;
                    }
                }
            return aiMax - huMax;
        }

        // ── Single-cell threat evaluation ─────────────────────────

        private static int EvalPos(Player[,] board, int row, int col, Player player)
        {
            int[][] dirs = { new[] { 0, 1 }, new[] { 1, 0 }, new[] { 1, 1 }, new[] { 1, -1 } };
            int max = 0;
            foreach (int[] d in dirs)
            {
                int count = 1, open = 0;
                int r = row + d[0], c = col + d[1];
                while (r >= 0 && r < Size && c >= 0 && c < Size && board[r, c] == player)
                { count++; r += d[0]; c += d[1]; }
                if (r >= 0 && r < Size && c >= 0 && c < Size && board[r, c] == Player.None) open++;
                r = row - d[0]; c = col - d[1];
                while (r >= 0 && r < Size && c >= 0 && c < Size && board[r, c] == player)
                { count++; r -= d[0]; c -= d[1]; }
                if (r >= 0 && r < Size && c >= 0 && c < Size && board[r, c] == Player.None) open++;
                int s = Threat(count, open);
                if (s > max) max = s;
            }
            return max;
        }

        private static int Threat(int count, int open)
        {
            if (count >= 5) return 100000;
            if (open == 0)  return 0;
            if (count == 4) return open == 2 ? 50000 : 5000;
            if (count == 3) return open == 2 ? 3000  : 500;
            if (count == 2) return open == 2 ? 300   : 100;
            return open == 2 ? 20 : 10;
        }

        // ── Win detection (lightweight, used in minimax) ───────────

        private static bool HasWon(Player[,] board, int row, int col, Player player)
        {
            int[][] dirs = { new[] { 0, 1 }, new[] { 1, 0 }, new[] { 1, 1 }, new[] { 1, -1 } };
            foreach (int[] d in dirs)
            {
                int count = 1;
                for (int s = 1; s <= 4; s++) { int r = row + s * d[0], c = col + s * d[1]; if (r < 0 || r >= Size || c < 0 || c >= Size || board[r, c] != player) break; count++; }
                for (int s = 1; s <= 4; s++) { int r = row - s * d[0], c = col - s * d[1]; if (r < 0 || r >= Size || c < 0 || c >= Size || board[r, c] != player) break; count++; }
                if (count >= 5) return true;
            }
            return false;
        }

        // ── Candidate move generation ──────────────────────────────

        private static List<int[]> GetCandidates(Player[,] board)
        {
            HashSet<string> seen = new HashSet<string>();
            List<int[]> list = new List<int[]>();
            for (int r = 0; r < Size; r++)
                for (int c = 0; c < Size; c++)
                {
                    if (board[r, c] == Player.None) continue;
                    for (int dr = -2; dr <= 2; dr++)
                        for (int dc = -2; dc <= 2; dc++)
                        {
                            int nr = r + dr, nc = c + dc;
                            if (nr >= 0 && nr < Size && nc >= 0 && nc < Size && board[nr, nc] == Player.None)
                            {
                                string key = nr + "," + nc;
                                if (!seen.Contains(key)) { seen.Add(key); list.Add(new int[] { nr, nc }); }
                            }
                        }
                }
            return list;
        }

        /// <summary>Returns up to <paramref name="topN"/> candidates, sorted by threat score descending.</summary>
        private List<int[]> SortedCandidates(Player[,] board, Player ai, Player human, int topN)
        {
            List<int[]> raw = GetCandidates(board);
            List<KeyValuePair<int, int[]>> scored = new List<KeyValuePair<int, int[]>>();
            foreach (int[] m in raw)
            {
                if (board[m[0], m[1]] != Player.None) continue;
                board[m[0], m[1]] = ai;    int atk = EvalPos(board, m[0], m[1], ai);    board[m[0], m[1]] = Player.None;
                board[m[0], m[1]] = human; int def = EvalPos(board, m[0], m[1], human); board[m[0], m[1]] = Player.None;
                scored.Add(new KeyValuePair<int, int[]>(Math.Max(atk, def), m));
            }
            scored.Sort((a, b) => b.Key.CompareTo(a.Key));
            List<int[]> top = new List<int[]>();
            for (int i = 0; i < Math.Min(topN, scored.Count); i++) top.Add(scored[i].Value);
            return top;
        }

        // ── Utilities ──────────────────────────────────────────────

        private static Player Opp(Player p) { return p == Player.Black ? Player.White : Player.Black; }
        private static int[] Center() { return new int[] { Size / 2, Size / 2 }; }

        private static List<int[]> EmptyFrom(List<int[]> list, Player[,] board)
        {
            List<int[]> result = new List<int[]>();
            foreach (int[] m in list) if (board[m[0], m[1]] == Player.None) result.Add(m);
            return result;
        }

        private static int[] PreferCenter(List<int[]> moves)
        {
            if (moves.Count == 0) return null;
            double cx = Size / 2.0, best = double.MaxValue;
            int[] result = moves[0];
            foreach (int[] m in moves)
            {
                double d = (m[0] - cx) * (m[0] - cx) + (m[1] - cx) * (m[1] - cx);
                if (d < best) { best = d; result = m; }
            }
            return result;
        }
    }
}
