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
        private const int Cell    = 38;
        private const int Pad     = 33;
        private const int Radius  = 15;
        private const int Grid    = (GomokuGame.BoardSize - 1) * Cell;   // 532
        private const int BoardSz = Grid + Pad * 2;                      // 598
        private const int FormW   = 660;
        private const int TopH    = 76;
        private const int BotH    = 128;
        private const int FormH   = TopH + BoardSz + BotH;               // 802
        private const int BoardX  = (FormW - BoardSz) / 2;               // 31

        // ── 遊戲物件 ────────────────────────────────────────────────
        private readonly GomokuGame _game = new GomokuGame();
        private readonly GomokuAI   _ai   = new GomokuAI();
        private bool _aiMode      = true;
        private bool _aiThinking  = false;
        private int  _gameVersion = 0;

        // ── 難度設定 ────────────────────────────────────────────────
        private static readonly Color[] DiffColors =
        {
            Color.FromArgb(46, 125, 50),   // 簡單  — 綠
            Color.FromArgb(21, 101, 192),  // 中等  — 藍
            Color.FromArgb(204, 101, 0),   // 困難  — 橘
            Color.FromArgb(136, 14, 79)    // 大師  — 深紫紅
        };
        private static readonly string[] DiffNames  = { "簡單", "中等", "困難", "大師" };
        private static readonly string[] DiffLabels = { "●  簡單", "●  中等", "●  困難", "★  大師" };
        private readonly Button[] _diffBtns = new Button[4];

        // ── UI 控制項 ────────────────────────────────────────────────
        private DoubleBufferedPanel _board;
        private Panel  _leftCard;
        private Panel  _rightCard;
        private Label  _lblTimer;
        private Label  _lblStatus;
        private Label  _lblMoves;
        private Label  _lblWName;   // 右側玩家名稱（可切換「電腦」/「玩家 2」）

        // ── 計時器 / 懸停 ───────────────────────────────────────────
        private readonly System.Windows.Forms.Timer _clock;
        private int   _elapsed = 0;
        private bool  _started = false;
        private Point? _hover  = null;

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 建構子
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        public MainForm()
        {
            SuspendLayout();

            Text            = "五子棋  Gomoku";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox     = false;
            BackColor       = Color.FromArgb(24, 24, 32);
            StartPosition   = FormStartPosition.CenterScreen;
            ClientSize      = new Size(FormW, FormH);
            KeyPreview      = true;
            Font            = new Font("Microsoft JhengHei UI", 9f);

            // ── 頂部資訊列 ──────────────────────────────────────────
            Panel topPanel = new Panel();
            topPanel.Location  = Point.Empty;
            topPanel.Size      = new Size(FormW, TopH);
            topPanel.BackColor = Color.FromArgb(30, 30, 42);
            Controls.Add(topPanel);
            BuildTopPanel(topPanel);

            // ── 棋盤（置中） ────────────────────────────────────────
            _board = new DoubleBufferedPanel();
            _board.Location    = new Point(BoardX, TopH);
            _board.Size        = new Size(BoardSz, BoardSz);
            _board.Cursor      = Cursors.Hand;
            _board.Paint      += Board_Paint;
            _board.MouseMove  += Board_MouseMove;
            _board.MouseClick += Board_MouseClick;
            _board.MouseLeave += (s, e) => { _hover = null; _board.Invalidate(); };
            Controls.Add(_board);

            // ── 底部控制列 ─────────────────────────────────────────
            Panel botPanel = new Panel();
            botPanel.Location  = new Point(0, TopH + BoardSz);
            botPanel.Size      = new Size(FormW, BotH);
            botPanel.BackColor = Color.FromArgb(26, 26, 36);
            Controls.Add(botPanel);
            BuildBottomPanel(botPanel);

            // ── 遊戲計時器 ─────────────────────────────────────────
            _clock = new System.Windows.Forms.Timer();
            _clock.Interval = 1000;
            _clock.Tick += (s, e) => { _elapsed++; RefreshTimer(); };

            ResumeLayout(false);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 頂部面板
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        private void BuildTopPanel(Panel top)
        {
            const int cardW = 158, cardH = 58, cardY = 9;

            // ── 左側：玩家（黑棋） ─────────────────────────────────
            _leftCard = MakeCard(top, 12, cardY, cardW, cardH,
                                 Color.FromArgb(36, 68, 110));   // 預設高亮（黑棋先行）

            // 人物圖示
            Label lIcon = new Label();
            lIcon.Text      = "♟";
            lIcon.Font      = new Font("Segoe UI Symbol", 20, FontStyle.Regular);
            lIcon.ForeColor = Color.FromArgb(130, 190, 130);
            lIcon.Location  = new Point(5, 4);
            lIcon.Size      = new Size(38, 50);
            lIcon.TextAlign = ContentAlignment.MiddleCenter;
            lIcon.AutoSize  = false;
            _leftCard.Controls.Add(lIcon);

            Label lName = new Label();
            lName.Text      = "玩家";
            lName.Font      = new Font("Microsoft JhengHei", 11, FontStyle.Bold);
            lName.ForeColor = Color.White;
            lName.Location  = new Point(47, 6);
            lName.Size      = new Size(104, 22);
            lName.AutoSize  = false;
            _leftCard.Controls.Add(lName);

            // 黑棋 indicator
            PictureBox pbB = MakePieceBox(_leftCard, 47, 29, Player.Black);
            _leftCard.Controls.Add(pbB);

            Label lBLabel = new Label();
            lBLabel.Text      = "黑棋";
            lBLabel.Font      = new Font("Microsoft JhengHei", 7.5f);
            lBLabel.ForeColor = Color.FromArgb(180, 180, 200);
            lBLabel.Location  = new Point(72, 33);
            lBLabel.Size      = new Size(38, 18);
            lBLabel.AutoSize  = false;
            _leftCard.Controls.Add(lBLabel);

            // ── 中央：計時 + 狀態 ─────────────────────────────────
            // 計時器（大字）
            _lblTimer = new Label();
            _lblTimer.Text      = "00:00";
            _lblTimer.Font      = new Font("Consolas", 20, FontStyle.Bold);
            _lblTimer.ForeColor = Color.White;
            _lblTimer.Location  = new Point(0, 10);
            _lblTimer.Size      = new Size(FormW, 30);
            _lblTimer.TextAlign = ContentAlignment.MiddleCenter;
            _lblTimer.AutoSize  = false;
            top.Controls.Add(_lblTimer);

            // 狀態文字
            _lblStatus = new Label();
            _lblStatus.Text      = "您的回合";
            _lblStatus.Font      = new Font("Microsoft JhengHei", 9f);
            _lblStatus.ForeColor = Color.FromArgb(120, 200, 120);
            _lblStatus.Location  = new Point(0, 44);
            _lblStatus.Size      = new Size(FormW, 22);
            _lblStatus.TextAlign = ContentAlignment.MiddleCenter;
            _lblStatus.AutoSize  = false;
            top.Controls.Add(_lblStatus);

            // ── 右側：電腦（白棋） ─────────────────────────────────
            _rightCard = MakeCard(top, FormW - 12 - cardW, cardY, cardW, cardH,
                                  Color.FromArgb(44, 44, 58));  // 非高亮

            // 機器人圖示
            Label rIcon = new Label();
            rIcon.Text      = "⚙";
            rIcon.Font      = new Font("Segoe UI Symbol", 20, FontStyle.Regular);
            rIcon.ForeColor = Color.FromArgb(120, 160, 220);
            rIcon.Location  = new Point(cardW - 43, 4);
            rIcon.Size      = new Size(38, 50);
            rIcon.TextAlign = ContentAlignment.MiddleCenter;
            rIcon.AutoSize  = false;
            _rightCard.Controls.Add(rIcon);

            _lblWName = new Label();
            _lblWName.Text      = "電腦";
            _lblWName.Font      = new Font("Microsoft JhengHei", 11, FontStyle.Bold);
            _lblWName.ForeColor = Color.White;
            _lblWName.Location  = new Point(8, 6);
            _lblWName.Size      = new Size(104, 22);
            _lblWName.AutoSize  = false;
            _rightCard.Controls.Add(_lblWName);

            // 白棋 indicator
            PictureBox pbW = MakePieceBox(_rightCard, 8, 29, Player.White);
            _rightCard.Controls.Add(pbW);

            Label lWLabel = new Label();
            lWLabel.Text      = "白棋";
            lWLabel.Font      = new Font("Microsoft JhengHei", 7.5f);
            lWLabel.ForeColor = Color.FromArgb(180, 180, 200);
            lWLabel.Location  = new Point(33, 33);
            lWLabel.Size      = new Size(38, 18);
            lWLabel.AutoSize  = false;
            _rightCard.Controls.Add(lWLabel);

            // 步數（右側小字，底部中央）
            _lblMoves = new Label();
            _lblMoves.Text      = "第 0 手";
            _lblMoves.Font      = new Font("Microsoft JhengHei", 7.5f);
            _lblMoves.ForeColor = Color.FromArgb(110, 110, 130);
            _lblMoves.Location  = new Point(0, TopH - 14);
            _lblMoves.Size      = new Size(FormW, 12);
            _lblMoves.TextAlign = ContentAlignment.MiddleCenter;
            _lblMoves.AutoSize  = false;
            top.Controls.Add(_lblMoves);
        }

        private static Panel MakeCard(Control parent, int x, int y, int w, int h, Color bg)
        {
            Panel p = new Panel();
            p.Location  = new Point(x, y);
            p.Size      = new Size(w, h);
            p.BackColor = bg;
            parent.Controls.Add(p);
            return p;
        }

        private static PictureBox MakePieceBox(Control parent, int x, int y, Player player)
        {
            PictureBox pb = new PictureBox();
            pb.Location  = new Point(x, y);
            pb.Size      = new Size(24, 24);
            pb.BackColor = Color.Transparent;

            Player captured = player;           // capture for closure
            pb.Paint += (s, e) =>
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                DrawPieceSmall(e.Graphics, 12, 12, captured, 10);
            };
            return pb;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 底部面板
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        private void BuildBottomPanel(Panel bot)
        {
            // ── 難度列 ─────────────────────────────────────────────
            Label lblDiff = new Label();
            lblDiff.Text      = "AI 難度：";
            lblDiff.Font      = new Font("Microsoft JhengHei", 9f, FontStyle.Bold);
            lblDiff.ForeColor = Color.FromArgb(170, 170, 185);
            lblDiff.Size      = new Size(70, 30);
            lblDiff.TextAlign = ContentAlignment.MiddleRight;
            lblDiff.AutoSize  = false;

            // 計算難度按鈕總寬度，水平置中
            const int dBtnW = 100, dBtnH = 30, dGap = 5;
            int diffTotalW = 70 + 8 + 4 * dBtnW + 3 * dGap;
            int diffStartX = (FormW - diffTotalW) / 2;

            lblDiff.Location = new Point(diffStartX, 14);
            bot.Controls.Add(lblDiff);

            for (int i = 0; i < 4; i++)
            {
                Button b = new Button();
                b.Text      = DiffLabels[i];
                b.Font      = new Font("Microsoft JhengHei", 9f, FontStyle.Bold);
                b.ForeColor = Color.White;
                b.FlatStyle = FlatStyle.Flat;
                b.Location  = new Point(diffStartX + 78 + i * (dBtnW + dGap), 14);
                b.Size      = new Size(dBtnW, dBtnH);
                b.Cursor    = Cursors.Hand;
                b.FlatAppearance.BorderSize = 1;
                b.Tag    = i;
                b.Click += DiffBtn_Click;
                bot.Controls.Add(b);
                _diffBtns[i] = b;
            }
            UpdateDiffBtns();

            // ── 操作按鈕列 ─────────────────────────────────────────
            const int aBtnW = 178, aBtnH = 36, aGap = 12;
            int actStartX = (FormW - 3 * aBtnW - 2 * aGap) / 2;
            const int aBtnY = 58;

            Button btnNew = MakeActBtn(bot, "🔄  新遊戲  (N)",
                Color.FromArgb(22, 128, 52), actStartX, aBtnY, aBtnW, aBtnH);
            btnNew.Click += (s, e) => NewGame();

            Button btnUndo = MakeActBtn(bot, "↩  悔棋      (U)",
                Color.FromArgb(128, 88, 18), actStartX + aBtnW + aGap, aBtnY, aBtnW, aBtnH);
            btnUndo.Click += (s, e) => UndoMove();

            Button btnMode = MakeActBtn(bot, "🔀  切換對戰模式",
                Color.FromArgb(64, 64, 155), actStartX + 2 * (aBtnW + aGap), aBtnY, aBtnW, aBtnH);
            btnMode.Click += (s, e) => ToggleMode();
        }

        private static Button MakeActBtn(Control parent, string text, Color color,
                                         int x, int y, int w, int h)
        {
            Button b = new Button();
            b.Text      = text;
            b.Font      = new Font("Microsoft JhengHei", 9.5f, FontStyle.Bold);
            b.ForeColor = Color.White;
            b.BackColor = color;
            b.FlatStyle = FlatStyle.Flat;
            b.Location  = new Point(x, y);
            b.Size      = new Size(w, h);
            b.Cursor    = Cursors.Hand;
            b.FlatAppearance.BorderSize         = 0;
            b.FlatAppearance.MouseOverBackColor = Lighten(color, 25);
            parent.Controls.Add(b);
            return b;
        }

        private void DiffBtn_Click(object sender, EventArgs e)
        {
            int idx = (int)((Button)sender).Tag;
            _ai.Difficulty = (Difficulty)idx;
            UpdateDiffBtns();
        }

        private void UpdateDiffBtns()
        {
            int selected = (int)_ai.Difficulty;
            for (int i = 0; i < 4; i++)
            {
                if (_diffBtns[i] == null) continue;
                bool on = (i == selected);
                _diffBtns[i].BackColor = on ? DiffColors[i] : Color.FromArgb(46, 46, 60);
                _diffBtns[i].ForeColor = on ? Color.White   : Color.FromArgb(160, 160, 180);
                _diffBtns[i].FlatAppearance.BorderColor =
                    on ? Lighten(DiffColors[i], 30) : Color.FromArgb(68, 68, 85);
            }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 棋盤繪圖
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

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
                Color.FromArgb(216, 145, 62), Color.FromArgb(174, 108, 34), 40f))
                g.FillRectangle(bg, rect);

            // 木紋線
            Random rng = new Random(7);
            using (Pen grain = new Pen(Color.FromArgb(16, 0, 0, 0), 1.2f))
                for (int i = 0; i < 28; i++)
                {
                    int y0 = rng.Next(0, BoardSz), y1 = y0 + rng.Next(-18, 18);
                    g.DrawLine(grain, 0, y0, BoardSz, y1);
                }

            using (Pen border = new Pen(Color.FromArgb(116, 74, 18), 3))
                g.DrawRectangle(border, 2, 2, BoardSz - 5, BoardSz - 5);
        }

        private static void DrawGrid(Graphics g)
        {
            using (Pen pen = new Pen(Color.FromArgb(88, 52, 12), 1f))
                for (int i = 0; i < GomokuGame.BoardSize; i++)
                {
                    g.DrawLine(pen, Pad + i * Cell, Pad, Pad + i * Cell, Pad + Grid);
                    g.DrawLine(pen, Pad, Pad + i * Cell, Pad + Grid, Pad + i * Cell);
                }
        }

        private static void DrawStarPoints(Graphics g)
        {
            int[] pts = { 3, 7, 11 };
            using (SolidBrush b = new SolidBrush(Color.FromArgb(88, 52, 12)))
                foreach (int r in pts)
                    foreach (int c in pts)
                        g.FillEllipse(b, Pad + c * Cell - 4, Pad + r * Cell - 4, 8, 8);
        }

        private static void DrawCoordinates(Graphics g)
        {
            const string cols = "ABCDEFGHJKLMNOP";
            using (Font font  = new Font("Consolas", 8f))
            using (SolidBrush brush = new SolidBrush(Color.FromArgb(98, 60, 18)))
                for (int i = 0; i < GomokuGame.BoardSize; i++)
                {
                    int px = Pad + i * Cell, py = Pad + i * Cell;
                    SizeF cs = g.MeasureString(cols[i].ToString(), font);
                    g.DrawString(cols[i].ToString(), font, brush, px - cs.Width / 2, Pad / 2 - cs.Height / 2);
                    g.DrawString(cols[i].ToString(), font, brush, px - cs.Width / 2, BoardSz - Pad / 2 - cs.Height / 2);
                    string row = (GomokuGame.BoardSize - i).ToString();
                    SizeF rs = g.MeasureString(row, font);
                    g.DrawString(row, font, brush, Pad / 2 - rs.Width / 2, py - rs.Height / 2);
                    g.DrawString(row, font, brush, BoardSz - Pad / 2 - rs.Width / 2, py - rs.Height / 2);
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
            int R = Radius;
            Rectangle rc = new Rectangle(cx - R, cy - R, R * 2, R * 2);

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

            // 光澤
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

            if (isLast)
            {
                using (SolidBrush mark = new SolidBrush(Color.FromArgb(230, 50, 50)))
                    g.FillEllipse(mark, cx - 4, cy - 4, 8, 8);
            }
        }

        /// <summary>在頂部面板的棋子指示器中使用（較小尺寸）</summary>
        private static void DrawPieceSmall(Graphics g, int cx, int cy, Player player, int r)
        {
            Rectangle rc = new Rectangle(cx - r, cy - r, r * 2, r * 2);

            using (SolidBrush shadow = new SolidBrush(Color.FromArgb(50, 0, 0, 0)))
                g.FillEllipse(shadow, cx - r + 1, cy - r + 2, r * 2, r * 2);

            if (player == Player.Black)
            {
                using (LinearGradientBrush fill = new LinearGradientBrush(rc,
                    Color.FromArgb(85, 85, 85), Color.FromArgb(8, 8, 8),
                    LinearGradientMode.ForwardDiagonal))
                    g.FillEllipse(fill, rc);
            }
            else
            {
                using (LinearGradientBrush fill = new LinearGradientBrush(rc,
                    Color.White, Color.FromArgb(200, 200, 200),
                    LinearGradientMode.ForwardDiagonal))
                    g.FillEllipse(fill, rc);
                using (Pen border = new Pen(Color.FromArgb(155, 155, 155), 1f))
                    g.DrawEllipse(border, rc);
            }

            int sh = r - 3;
            if (sh > 0)
            {
                Rectangle shrc = new Rectangle(cx - r + 3, cy - r + 3, sh, sh);
                int alpha = player == Player.Black ? 55 : 90;
                using (LinearGradientBrush shine = new LinearGradientBrush(shrc,
                    Color.FromArgb(alpha, 255, 255, 255),
                    Color.FromArgb(0, 255, 255, 255),
                    LinearGradientMode.ForwardDiagonal))
                    g.FillEllipse(shine, shrc);
            }
        }

        private void DrawHover(Graphics g, Point cell)
        {
            if (_game.Board[cell.Y, cell.X] != Player.None) return;
            int cx = Pad + cell.X * Cell, cy = Pad + cell.Y * Cell;
            Color hc = _game.CurrentPlayer == Player.Black
                ? Color.FromArgb(95, 15, 15, 15) : Color.FromArgb(95, 240, 240, 240);
            using (SolidBrush b = new SolidBrush(hc))
                g.FillEllipse(b, cx - Radius, cy - Radius, Radius * 2, Radius * 2);
        }

        private void DrawWinLine(Graphics g)
        {
            List<int[]> cells = _game.WinningCells;
            if (cells == null || cells.Count < 2) return;

            cells.Sort((a, b) => a[1] != b[1] ? a[1].CompareTo(b[1]) : a[0].CompareTo(b[0]));
            double x1 = Pad + cells[0][1] * Cell,              y1 = Pad + cells[0][0] * Cell;
            double x2 = Pad + cells[cells.Count - 1][1] * Cell, y2 = Pad + cells[cells.Count - 1][0] * Cell;

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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 輸入處理
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 遊戲邏輯
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        private async Task AIMoveAsync()
        {
            int ver = _gameVersion;
            _aiThinking = true;
            RefreshStatus();

            // AI「思考」延遲（根據難度調整）
            int thinkMs;
            switch (_ai.Difficulty)
            {
                case Difficulty.Easy:   thinkMs = 200; break;
                case Difficulty.Hard:   thinkMs = 400; break;
                case Difficulty.Master: thinkMs = 100; break;  // minimax 本身已耗時
                default:                thinkMs = 300; break;
            }
            await Task.Delay(thinkMs);

            if (_gameVersion != ver || _game.IsGameOver)
            { _aiThinking = false; RefreshStatus(); return; }

            int[] move = await Task.Run(() => _ai.GetBestMove(_game, Player.White));

            if (_gameVersion != ver || _game.IsGameOver)
            { _aiThinking = false; RefreshStatus(); return; }

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
            _aiMode       = !_aiMode;
            _lblWName.Text = _aiMode ? "電腦" : "玩家 2";
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

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // UI 更新
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        private void RefreshBoard()
        {
            _lblMoves.Text = "第 " + _game.MoveCount + " 手";
            _board.Invalidate();
            RefreshStatus();
        }

        private void RefreshStatus()
        {
            // 卡片高亮（藍色=當前回合，暗色=等待）
            Color activeCard   = Color.FromArgb(36, 68, 110);
            Color inactiveCard = Color.FromArgb(44, 44, 58);

            if (_game.IsGameOver)
            {
                _leftCard.BackColor  = inactiveCard;
                _rightCard.BackColor = inactiveCard;

                if (_game.Winner == Player.None)
                { _lblStatus.Text = "平局！🤝"; _lblStatus.ForeColor = Color.FromArgb(220, 200, 80); }
                else if (_game.Winner == Player.Black)
                { _lblStatus.Text = "⚫ 黑棋獲勝！🏆"; _lblStatus.ForeColor = Color.FromArgb(100, 220, 100); }
                else
                { _lblStatus.Text = "⚪ 白棋獲勝！🏆"; _lblStatus.ForeColor = Color.FromArgb(100, 220, 100); }
            }
            else if (_aiThinking)
            {
                _leftCard.BackColor  = inactiveCard;
                _rightCard.BackColor = activeCard;
                _lblStatus.Text      = "AI 思考中…";
                _lblStatus.ForeColor = Color.FromArgb(255, 180, 60);
            }
            else
            {
                bool blackTurn = (_game.CurrentPlayer == Player.Black);
                _leftCard.BackColor  = blackTurn ? activeCard : inactiveCard;
                _rightCard.BackColor = blackTurn ? inactiveCard : activeCard;
                _lblStatus.Text      = blackTurn ? "您的回合" : "電腦回合";
                _lblStatus.ForeColor = Color.FromArgb(120, 200, 120);
            }
        }

        private void RefreshTimer()
        {
            _lblTimer.Text = string.Format("{0:D2}:{1:D2}", _elapsed / 60, _elapsed % 60);
        }

        private static Color Lighten(Color c, int d)
        {
            return Color.FromArgb(
                Math.Min(255, c.R + d),
                Math.Min(255, c.G + d),
                Math.Min(255, c.B + d));
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
