using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Gomoku
{
    public class MainForm : Form
    {
        // ── 版面配置常數 ────────────────────────────────────────────
        private const int Cell   = 42;    // 格子像素大小
        private const int Pad    = 38;    // 棋盤邊緣留白（用於座標標示）
        private const int Radius = 18;    // 棋子半徑
        private const int Grid   = (GomokuGame.BoardSize - 1) * Cell;
        private const int BoardSz = Grid + Pad * 2;
        private const int SideW  = 200;

        // ── 遊戲物件 ────────────────────────────────────────────────
        private readonly GomokuGame _game = new GomokuGame();
        private readonly GomokuAI   _ai   = new GomokuAI();
        private bool _aiMode      = true;
        private bool _aiThinking  = false;
        private int  _gameVersion = 0;   // 每次新局 +1，用來取消舊的 AI Task

        // ── UI 控制項 ────────────────────────────────────────────────
        private DoubleBufferedPanel _board;
        private Label  _lblStatus;
        private Label  _lblTimer;
        private Label  _lblMoves;
        private Label  _lblMode;
        private Label  _lblWPlayer;
        private Button _btnNew;
        private Button _btnUndo;
        private Button _btnMode;

        // ── 計時器 / 滑鼠懸停 ──────────────────────────────────────
        private readonly System.Windows.Forms.Timer _clock;
        private int   _elapsed = 0;
        private bool  _started = false;
        private Point? _hover  = null;   // (col, row) 棋盤座標

        // ── 建構子 ──────────────────────────────────────────────────

        public MainForm()
        {
            SuspendLayout();

            Text            = "五子棋  Gomoku";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox     = false;
            BackColor       = Color.FromArgb(28, 28, 34);
            StartPosition   = FormStartPosition.CenterScreen;
            ClientSize      = new Size(BoardSz + SideW, BoardSz);
            KeyPreview      = true;
            Font            = new Font("Microsoft JhengHei UI", 9f);

            // 棋盤面板（雙緩衝，防閃爍）
            _board = new DoubleBufferedPanel();
            _board.Location     = Point.Empty;
            _board.Size         = new Size(BoardSz, BoardSz);
            _board.Cursor       = Cursors.Hand;
            _board.Paint       += Board_Paint;
            _board.MouseMove   += Board_MouseMove;
            _board.MouseClick  += Board_MouseClick;
            _board.MouseLeave  += (s, e) => { _hover = null; _board.Invalidate(); };
            Controls.Add(_board);

            // 側邊面板
            Panel side = new Panel();
            side.Location  = new Point(BoardSz, 0);
            side.Size      = new Size(SideW, BoardSz);
            side.BackColor = Color.FromArgb(35, 35, 42);
            Controls.Add(side);
            BuildSidePanel(side);

            // 遊戲計時器
            _clock = new System.Windows.Forms.Timer();
            _clock.Interval = 1000;
            _clock.Tick += (s, e) => { _elapsed++; RefreshTimer(); };

            ResumeLayout(false);
        }

        // ── 側邊面板建置 ─────────────────────────────────────────────

        private void BuildSidePanel(Panel p)
        {
            int y = 18, w = SideW;

            // 標題
            MakeLbl(p, "五子棋", new Font("Microsoft JhengHei", 22, FontStyle.Bold),
                Color.FromArgb(255, 200, 50), new Rectangle(0, y, w, 46), ContentAlignment.MiddleCenter);
            y += 48;
            MakeLbl(p, "G O M O K U", new Font("Segoe UI", 8.5f),
                Color.FromArgb(130, 130, 145), new Rectangle(0, y, w, 18), ContentAlignment.MiddleCenter);
            y += 26; MakeSep(p, ref y);

            // 計時器
            MakeLbl(p, "⏱  遊戲計時", new Font("Microsoft JhengHei", 8f),
                Color.FromArgb(110, 110, 125), new Rectangle(14, y, w, 16), ContentAlignment.MiddleLeft);
            y += 19;
            _lblTimer = MakeLbl(p, "00:00", new Font("Consolas", 21, FontStyle.Bold),
                Color.White, new Rectangle(0, y, w, 38), ContentAlignment.MiddleCenter);
            y += 48; MakeSep(p, ref y);

            // 狀態方塊
            _lblStatus = new Label();
            _lblStatus.Text      = "⚫ 黑棋落子";
            _lblStatus.Font      = new Font("Microsoft JhengHei", 11, FontStyle.Bold);
            _lblStatus.ForeColor = Color.White;
            _lblStatus.BackColor = Color.FromArgb(50, 50, 62);
            _lblStatus.TextAlign = ContentAlignment.MiddleCenter;
            _lblStatus.Location  = new Point(10, y);
            _lblStatus.Size      = new Size(w - 20, 38);
            _lblStatus.AutoSize  = false;
            p.Controls.Add(_lblStatus);
            y += 50;

            // 玩家卡片
            Label dummy;
            AddPlayerCard(p, "⚫  黑棋 先手", "玩家 1",   ref y, true,  out dummy);
            AddPlayerCard(p, "⚪  白棋 後手", "電腦 AI",  ref y, false, out _lblWPlayer);
            y += 2;

            // 步數
            _lblMoves = MakeLbl(p, "第 0 手", new Font("Microsoft JhengHei", 8.5f),
                Color.FromArgb(120, 120, 135), new Rectangle(0, y, w, 18), ContentAlignment.MiddleCenter);
            y += 26; MakeSep(p, ref y);

            // 按鈕
            _btnNew  = MakeBtn(p, "🔄  新遊戲  (N)", Color.FromArgb(25, 130, 55),  ref y);
            _btnUndo = MakeBtn(p, "↩  悔棋      (U)", Color.FromArgb(130, 90, 20), ref y);
            _btnMode = MakeBtn(p, "🤖  切換對戰模式",  Color.FromArgb(70, 70, 160), ref y);
            y += 6;

            _lblMode = MakeLbl(p, "模式：人機對戰", new Font("Microsoft JhengHei", 8.5f),
                Color.FromArgb(100, 200, 110), new Rectangle(0, y, w, 18), ContentAlignment.MiddleCenter);

            _btnNew.Click  += (s, e) => NewGame();
            _btnUndo.Click += (s, e) => UndoMove();
            _btnMode.Click += (s, e) => ToggleMode();
        }

        // ── Helper 工廠方法 ───────────────────────────────────────────

        private static Label MakeLbl(Control parent, string text, Font font, Color color,
            Rectangle bounds, ContentAlignment align)
        {
            Label l = new Label();
            l.Text      = text;
            l.Font      = font;
            l.ForeColor = color;
            l.Location  = bounds.Location;
            l.Size      = bounds.Size;
            l.TextAlign = align;
            l.AutoSize  = false;
            parent.Controls.Add(l);
            return l;
        }

        private static void MakeSep(Control parent, ref int y)
        {
            Panel sep = new Panel();
            sep.Location  = new Point(12, y);
            sep.Size      = new Size(SideW - 24, 1);
            sep.BackColor = Color.FromArgb(60, 60, 72);
            parent.Controls.Add(sep);
            y += 12;
        }

        private static void AddPlayerCard(Control parent, string title, string name,
            ref int y, bool isBlack, out Label nameLbl)
        {
            Panel card = new Panel();
            card.Location  = new Point(10, y);
            card.Size      = new Size(SideW - 20, 44);
            card.BackColor = Color.FromArgb(44, 44, 54);

            Panel bar = new Panel();
            bar.Location  = Point.Empty;
            bar.Size      = new Size(4, 44);
            bar.BackColor = isBlack ? Color.FromArgb(70, 70, 70) : Color.FromArgb(190, 190, 190);
            card.Controls.Add(bar);

            Label lbl1 = new Label();
            lbl1.Text      = title;
            lbl1.Font      = new Font("Microsoft JhengHei", 8.5f);
            lbl1.ForeColor = Color.FromArgb(165, 165, 175);
            lbl1.Location  = new Point(14, 5);
            lbl1.Size      = new Size(SideW - 40, 16);
            lbl1.AutoSize  = false;
            card.Controls.Add(lbl1);

            nameLbl = new Label();
            nameLbl.Text      = name;
            nameLbl.Font      = new Font("Microsoft JhengHei", 10, FontStyle.Bold);
            nameLbl.ForeColor = Color.White;
            nameLbl.Location  = new Point(14, 22);
            nameLbl.Size      = new Size(SideW - 40, 18);
            nameLbl.AutoSize  = false;
            card.Controls.Add(nameLbl);

            parent.Controls.Add(card);
            y += 52;
        }

        private static Button MakeBtn(Control parent, string text, Color color, ref int y)
        {
            Button b = new Button();
            b.Text      = text;
            b.Font      = new Font("Microsoft JhengHei", 9.5f, FontStyle.Bold);
            b.ForeColor = Color.White;
            b.BackColor = color;
            b.FlatStyle = FlatStyle.Flat;
            b.Location  = new Point(10, y);
            b.Size      = new Size(SideW - 20, 36);
            b.Cursor    = Cursors.Hand;
            b.FlatAppearance.BorderSize = 0;
            b.FlatAppearance.MouseOverBackColor = Lighten(color, 30);
            parent.Controls.Add(b);
            y += 42;
            return b;
        }

        private static Color Lighten(Color c, int d)
        {
            return Color.FromArgb(
                Math.Min(255, c.R + d),
                Math.Min(255, c.G + d),
                Math.Min(255, c.B + d));
        }

        // ── 繪圖 ────────────────────────────────────────────────────

        private void Board_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            DrawBackground(g);
            DrawGrid(g);
            DrawStarPoints(g);
            DrawCoordinates(g);
            DrawPieces(g);

            if (_hover.HasValue && !_game.IsGameOver && !_aiThinking)
                DrawHover(g, _hover.Value);

            if (_game.IsGameOver && _game.WinningCells != null)
                DrawWinLine(g);
        }

        private static void DrawBackground(Graphics g)
        {
            Rectangle rect = new Rectangle(0, 0, BoardSz, BoardSz);

            using (LinearGradientBrush bg = new LinearGradientBrush(rect,
                Color.FromArgb(214, 143, 62), Color.FromArgb(172, 106, 33), 40f))
            {
                g.FillRectangle(bg, rect);
            }

            // 木紋線條（固定亂數種子，確保每次相同）
            Random rng = new Random(7);
            using (Pen grain = new Pen(Color.FromArgb(16, 0, 0, 0), 1.2f))
            {
                for (int i = 0; i < 28; i++)
                {
                    int y0 = rng.Next(0, BoardSz);
                    int y1 = y0 + rng.Next(-18, 18);
                    g.DrawLine(grain, 0, y0, BoardSz, y1);
                }
            }

            // 外框線
            using (Pen border = new Pen(Color.FromArgb(115, 72, 18), 3))
            {
                g.DrawRectangle(border, 2, 2, BoardSz - 5, BoardSz - 5);
            }
        }

        private static void DrawGrid(Graphics g)
        {
            using (Pen pen = new Pen(Color.FromArgb(88, 52, 12), 1f))
            {
                for (int i = 0; i < GomokuGame.BoardSize; i++)
                {
                    int x = Pad + i * Cell;
                    int y = Pad + i * Cell;
                    g.DrawLine(pen, x, Pad, x, Pad + Grid);
                    g.DrawLine(pen, Pad, y, Pad + Grid, y);
                }
            }
        }

        private static void DrawStarPoints(Graphics g)
        {
            int[] pts = { 3, 7, 11 };
            using (SolidBrush b = new SolidBrush(Color.FromArgb(88, 52, 12)))
            {
                foreach (int r in pts)
                    foreach (int c in pts)
                        g.FillEllipse(b, Pad + c * Cell - 4, Pad + r * Cell - 4, 8, 8);
            }
        }

        private static void DrawCoordinates(Graphics g)
        {
            const string cols = "ABCDEFGHJKLMNOP";
            using (Font font  = new Font("Consolas", 8.5f))
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(98, 60, 18)))
            {
                for (int i = 0; i < GomokuGame.BoardSize; i++)
                {
                    int px = Pad + i * Cell;
                    int py = Pad + i * Cell;

                    SizeF cs = g.MeasureString(cols[i].ToString(), font);
                    g.DrawString(cols[i].ToString(), font, brush,
                        px - cs.Width / 2, Pad / 2 - cs.Height / 2);
                    g.DrawString(cols[i].ToString(), font, brush,
                        px - cs.Width / 2, BoardSz - Pad / 2 - cs.Height / 2);

                    string row = (GomokuGame.BoardSize - i).ToString();
                    SizeF rs = g.MeasureString(row, font);
                    g.DrawString(row, font, brush,
                        Pad / 2 - rs.Width / 2, py - rs.Height / 2);
                    g.DrawString(row, font, brush,
                        BoardSz - Pad / 2 - rs.Width / 2, py - rs.Height / 2);
                }
            }
        }

        private void DrawPieces(Graphics g)
        {
            int[] last = _game.LastMove;
            for (int r = 0; r < GomokuGame.BoardSize; r++)
                for (int c = 0; c < GomokuGame.BoardSize; c++)
                {
                    Player pl = _game.Board[r, c];
                    if (pl == Player.None) continue;
                    bool isLast = last != null && last[0] == r && last[1] == c;
                    DrawPiece(g, Pad + c * Cell, Pad + r * Cell, pl, isLast);
                }
        }

        private static void DrawPiece(Graphics g, int cx, int cy, Player player, bool isLast)
        {
            int R  = Radius;
            Rectangle rc = new Rectangle(cx - R, cy - R, R * 2, R * 2);

            // 陰影
            using (SolidBrush shadow = new SolidBrush(Color.FromArgb(55, 0, 0, 0)))
                g.FillEllipse(shadow, cx - R + 2, cy - R + 3, R * 2, R * 2);

            if (player == Player.Black)
            {
                using (LinearGradientBrush fill = new LinearGradientBrush(rc,
                    Color.FromArgb(90, 90, 90), Color.FromArgb(4, 4, 4),
                    LinearGradientMode.ForwardDiagonal))
                    g.FillEllipse(fill, rc);
            }
            else
            {
                using (LinearGradientBrush fill = new LinearGradientBrush(rc,
                    Color.FromArgb(255, 255, 255), Color.FromArgb(198, 198, 198),
                    LinearGradientMode.ForwardDiagonal))
                    g.FillEllipse(fill, rc);

                using (Pen border = new Pen(Color.FromArgb(155, 155, 155), 1.2f))
                    g.DrawEllipse(border, rc);
            }

            // 高光
            int sh = R - 4;
            if (sh > 0)
            {
                Rectangle shrc = new Rectangle(cx - R + 4, cy - R + 4, sh, sh);
                int alpha = player == Player.Black ? 60 : 95;
                using (LinearGradientBrush shine = new LinearGradientBrush(shrc,
                    Color.FromArgb(alpha, 255, 255, 255),
                    Color.FromArgb(0, 255, 255, 255),
                    LinearGradientMode.ForwardDiagonal))
                    g.FillEllipse(shine, shrc);
            }

            // 最後落子紅點標記
            if (isLast)
            {
                using (SolidBrush mark = new SolidBrush(Color.FromArgb(230, 50, 50)))
                    g.FillEllipse(mark, cx - 4, cy - 4, 8, 8);
            }
        }

        private void DrawHover(Graphics g, Point cell)
        {
            if (_game.Board[cell.Y, cell.X] != Player.None) return;
            int cx = Pad + cell.X * Cell;
            int cy = Pad + cell.Y * Cell;
            int R  = Radius;
            Color hc = _game.CurrentPlayer == Player.Black
                ? Color.FromArgb(95, 15, 15, 15)
                : Color.FromArgb(95, 240, 240, 240);
            using (SolidBrush b = new SolidBrush(hc))
                g.FillEllipse(b, cx - R, cy - R, R * 2, R * 2);
        }

        private void DrawWinLine(Graphics g)
        {
            List<int[]> cells = _game.WinningCells;
            if (cells == null || cells.Count < 2) return;

            // 以 col 排序找兩端點，對角線與直線皆正確
            cells.Sort((a, b) => a[1] != b[1] ? a[1].CompareTo(b[1]) : a[0].CompareTo(b[0]));
            int[] start = cells[0];
            int[] end   = cells[cells.Count - 1];

            double x1 = Pad + start[1] * Cell;
            double y1 = Pad + start[0] * Cell;
            double x2 = Pad + end[1]   * Cell;
            double y2 = Pad + end[0]   * Cell;

            double dx = x2 - x1, dy = y2 - y1;
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len > 0)
            {
                const int ext = 14;
                x1 -= dx / len * ext; y1 -= dy / len * ext;
                x2 += dx / len * ext; y2 += dy / len * ext;
            }

            using (Pen pen = new Pen(Color.FromArgb(215, 255, 55, 55), 4.5f))
            {
                pen.StartCap = LineCap.Round;
                pen.EndCap   = LineCap.Round;
                g.DrawLine(pen, (float)x1, (float)y1, (float)x2, (float)y2);
            }
        }

        // ── 輸入處理 ─────────────────────────────────────────────────

        private void Board_MouseMove(object sender, MouseEventArgs e)
        {
            Point? c = ToCell(e.Location);
            if (!Equals(c, _hover)) { _hover = c; _board.Invalidate(); }
        }

        private async void Board_MouseClick(object sender, MouseEventArgs e)
        {
            if (_game.IsGameOver || _aiThinking) return;
            if (_aiMode && _game.CurrentPlayer == Player.White) return;

            Point? cell = ToCell(e.Location);
            if (!cell.HasValue) return;
            if (!_game.PlacePiece(cell.Value.Y, cell.Value.X)) return;

            if (!_started) { _started = true; _clock.Start(); }
            SoundManager.PlayBlack();
            RefreshBoard();

            if (_game.IsGameOver) { GameOver(); return; }
            if (_aiMode) await AIMoveAsync();
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.N) NewGame();
            if (e.KeyCode == Keys.U) UndoMove();
        }

        private static Point? ToCell(Point p)
        {
            int c = (int)Math.Round((p.X - Pad) / (double)Cell);
            int r = (int)Math.Round((p.Y - Pad) / (double)Cell);
            if (c < 0 || c >= GomokuGame.BoardSize || r < 0 || r >= GomokuGame.BoardSize) return null;
            return new Point(c, r);
        }

        // ── 遊戲邏輯 ─────────────────────────────────────────────────

        private async Task AIMoveAsync()
        {
            int ver = _gameVersion;
            _aiThinking = true;
            RefreshStatus();

            await Task.Delay(280);

            if (_gameVersion != ver || _game.IsGameOver)
            {
                _aiThinking = false;
                RefreshStatus();
                return;
            }

            int[] move = await Task.Run(() => _ai.GetBestMove(_game, Player.White));

            if (_gameVersion != ver || _game.IsGameOver)
            {
                _aiThinking = false;
                RefreshStatus();
                return;
            }

            _game.PlacePiece(move[0], move[1]);
            SoundManager.PlayWhite();
            _aiThinking = false;
            RefreshBoard();
            if (_game.IsGameOver) GameOver();
        }

        private void NewGame()
        {
            _gameVersion++;
            _aiThinking = false;
            _game.Reset();
            _elapsed = 0;
            _started = false;
            _clock.Stop();
            RefreshTimer();
            RefreshBoard();
            RefreshStatus();
            SoundManager.PlayNewGame();
        }

        private void UndoMove()
        {
            if (_aiThinking || _game.MoveCount == 0) return;
            int undoCount = _aiMode ? Math.Min(2, _game.MoveCount) : 1;
            _game.Undo(undoCount);
            RefreshBoard();
            RefreshStatus();
            SoundManager.PlayUndo();
        }

        private void ToggleMode()
        {
            _aiMode = !_aiMode;
            _lblMode.Text    = _aiMode ? "模式：人機對戰" : "模式：雙人對戰";
            _lblWPlayer.Text = _aiMode ? "電腦 AI" : "玩家 2";
            NewGame();
        }

        private void GameOver()
        {
            _clock.Stop();
            _board.Invalidate();
            RefreshStatus();
            SoundManager.PlayWin();

            string msg;
            if (_game.Winner == Player.None)
                msg = "棋盤已滿，平局！";
            else if (_game.Winner == Player.Black)
                msg = _aiMode ? "🎉 恭喜您獲勝！" : "⚫ 黑棋獲勝！";
            else
                msg = _aiMode ? "💻 AI 獲勝！再接再厲！" : "⚪ 白棋獲勝！";

            string time = string.Format("{0:D2}:{1:D2}", _elapsed / 60, _elapsed % 60);
            DialogResult ans = MessageBox.Show(
                msg + "\n\n遊戲時間：" + time + "　共 " + _game.MoveCount + " 手\n\n要再玩一局嗎？",
                "遊戲結束", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

            if (ans == DialogResult.Yes) NewGame();
        }

        // ── 更新 UI ──────────────────────────────────────────────────

        private void RefreshBoard()
        {
            _lblMoves.Text = "第 " + _game.MoveCount + " 手";
            _board.Invalidate();
            RefreshStatus();
        }

        private void RefreshStatus()
        {
            if (_game.IsGameOver)
            {
                if (_game.Winner == Player.None)
                {
                    _lblStatus.Text      = "🤝 平局！";
                    _lblStatus.BackColor = Color.FromArgb(55, 95, 55);
                }
                else if (_game.Winner == Player.Black)
                {
                    _lblStatus.Text      = "⚫ 黑棋獲勝！🏆";
                    _lblStatus.BackColor = Color.FromArgb(55, 95, 55);
                }
                else
                {
                    _lblStatus.Text      = "⚪ 白棋獲勝！🏆";
                    _lblStatus.BackColor = Color.FromArgb(55, 95, 55);
                }
            }
            else if (_aiThinking)
            {
                _lblStatus.Text      = "🤖 AI 思考中…";
                _lblStatus.BackColor = Color.FromArgb(75, 55, 25);
            }
            else
            {
                string who = _game.CurrentPlayer == Player.Black ? "⚫ 黑棋" : "⚪ 白棋";
                _lblStatus.Text      = who + " 落子";
                _lblStatus.BackColor = Color.FromArgb(50, 50, 62);
            }
        }

        private void RefreshTimer()
        {
            _lblTimer.Text = string.Format("{0:D2}:{1:D2}", _elapsed / 60, _elapsed % 60);
        }
    }

    // ── 雙緩衝面板（消除棋盤閃爍）────────────────────────────────────
    public sealed class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            DoubleBuffered = true;
            SetStyle(
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint |
                ControlStyles.OptimizedDoubleBuffer, true);
        }
    }
}
