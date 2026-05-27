using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Gomoku
{
    public class MainForm : Form
    {
        // ── 版面常數 ─────────────────────────────────────────────────
        private const int Cell    = 38;
        private const int Pad     = 33;
        private const int RadPiece = 15;
        private const int Grid    = (GomokuGame.BoardSize - 1) * Cell;  // 532
        private const int BoardSz = Grid + Pad * 2;                     // 598
        private const int FormW   = 660;
        private const int TopH    = 76;
        private const int BotH    = 128;
        private const int FormH   = TopH + BoardSz + BotH;              // 802
        private const int BoardX  = (FormW - BoardSz) / 2;              // 31
        private const int CardW   = 158, CardH = 58;

        // ── 遊戲物件 ────────────────────────────────────────────────
        private readonly GomokuGame _game = new GomokuGame();
        private readonly GomokuAI   _ai   = new GomokuAI();
        private bool _aiMode = true, _aiThinking = false;
        private int  _gameVersion = 0;

        // ── 主題 ─────────────────────────────────────────────────────
        private bool _isDark = true;

        // ── 難度顏色 ─────────────────────────────────────────────────
        private static readonly Color[] DiffColors =
        {
            Color.FromArgb(46, 125, 50),
            Color.FromArgb(21, 101, 192),
            Color.FromArgb(204, 101,   0),
            Color.FromArgb(136,  14,  79)
        };
        private static readonly string[] DiffLabels = { "簡單", "中等", "困難", "大師" };
        private readonly RoundedButton[] _diffBtns = new RoundedButton[4];

        // ── 面板引用 ─────────────────────────────────────────────────
        private DoubleBufferedPanel _board;
        private Panel  _topPanel, _botPanel, _settingsPanel;
        private Panel  _leftCard, _rightCard;

        // ── 標籤引用（主題切換用） ────────────────────────────────────
        private Label  _lblTimer, _lblStatus, _lblWName;
        private Label  _lblVolPct, _lblDiffTitle;

        // ── 設定面板控制項 ────────────────────────────────────────────
        private TrackBar       _trackVol;
        private RoundedButton  _btnLightTheme, _btnDarkTheme;
        private Label          _lblSettingsTitle;
        private Label          _lblVolTitle, _lblThemeTitle;
        private Panel          _settingsSep1, _settingsSep2;

        // ── 計時器 / 懸停 ─────────────────────────────────────────────
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
            StartPosition   = FormStartPosition.CenterScreen;
            ClientSize      = new Size(FormW, FormH);
            KeyPreview      = true;
            Font            = new Font("Microsoft JhengHei UI", 9f);

            LoadEmbeddedIcon();

            // 頂部
            _topPanel = new Panel { Location = Point.Empty, Size = new Size(FormW, TopH) };
            Controls.Add(_topPanel);

            // 棋盤
            _board = new DoubleBufferedPanel
            {
                Location = new Point(BoardX, TopH),
                Size     = new Size(BoardSz, BoardSz),
                Cursor   = Cursors.Hand
            };
            _board.Paint      += Board_Paint;
            _board.MouseMove  += Board_MouseMove;
            _board.MouseClick += Board_MouseClick;
            _board.MouseLeave += (s, e) => { _hover = null; _board.Invalidate(); };
            Controls.Add(_board);

            // 底部
            _botPanel = new Panel { Location = new Point(0, TopH + BoardSz), Size = new Size(FormW, BotH) };
            Controls.Add(_botPanel);

            // 設定面板（直接加到 Form，懸浮在最上層）
            BuildSettingsPanel();

            // 計時器
            _clock = new System.Windows.Forms.Timer { Interval = 1000 };
            _clock.Tick += (s, e) => { _elapsed++; RefreshTimer(); };

            // 套用主題並建立子控制項
            ApplyTheme();

            ResumeLayout(false);

            MusicPlayer.Start();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 圖示（Base64 內嵌，不需外部檔案）
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        private void LoadEmbeddedIcon()
        {
            try
            {
                byte[] b = Convert.FromBase64String(IconBase64);
                using (MemoryStream ms = new MemoryStream(b))
                    this.Icon = new Icon(ms);
            }
            catch { }
        }

        private const string IconBase64 =
            "AAABAAEAICAAAAEAIACoEAAAFgAAACgAAAAgAAAAQAAAAAEAIAAAAAAAABAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA2+3+AHGm2gB7rNwAY5zaAFiW" +
            "1wBUktQAVpTSACB40QB2qd0ACA" +
            "gKAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "vNfxAGqh3ABuo9sA//9vAGKb2RhYltYsU5LTJFmU0Ak/htIAeKfTAGKd0gAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "hLLgAHGl3QB1p9wRY5/egFqb39RUl97rTJLa40aN1rBLjdI7AGHSAIax0AB2qd8AAFG" +
            "fAL7W/gAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "ibXhALrN2QJqpOGIZaTm/2im6P9ko+b/WZzi/0uS3P9Aidb" +
            "cQ4rOJkmP1wBIj9cATpHUAGSa0ABel9AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "///////////////////////4Af//8AH///AAP//wAA//8AAH/+AAB/+AAAf/AAAH/AAAB/AAAAfgAA" +
            "AH4AAAD+AAAA/gAAAP4AAAD+AAAA/gAAAP4AAAD/AAAA/8AAA//gAA//+AAf//wAf///Af///////////" +
            "///////8=";

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 主題
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        private Color ThemeBg()      => _isDark ? Color.FromArgb(12, 12, 20)   : Color.FromArgb(244,241,234);
        private Color ThemeTopBar()  => _isDark ? Color.FromArgb(18, 18, 28)   : Color.FromArgb(252,249,242);
        private Color ThemeBotBar()  => _isDark ? Color.FromArgb(15, 15, 24)   : Color.FromArgb(236,232,224);
        private Color ThemeCard()    => _isDark ? Color.FromArgb(28, 28, 42)   : Color.FromArgb(218,214,205);
        private Color ThemeCardAct() => _isDark ? Color.FromArgb(28, 56, 95)   : Color.FromArgb(185,205,228);
        private Color ThemeText()    => _isDark ? Color.White                   : Color.FromArgb(28, 25, 18);
        private Color ThemeSubText() => _isDark ? Color.FromArgb(135,132,158)  : Color.FromArgb(108,103, 90);
        private Color ThemeSettings()=> _isDark ? Color.FromArgb(22, 22, 35)   : Color.FromArgb(250,248,242);
        private Color ThemeSettingsSep() => _isDark ? Color.FromArgb(48, 48,66): Color.FromArgb(195,190,180);
        private Color ThemeDiffUnsel()=> _isDark ? Color.FromArgb(36, 36, 52)  : Color.FromArgb(195,190,180);

        private void ApplyTheme()
        {
            BackColor           = ThemeBg();
            _topPanel.BackColor = ThemeTopBar();
            _botPanel.BackColor = ThemeBotBar();

            _topPanel.Controls.Clear();
            _botPanel.Controls.Clear();
            BuildTopPanel(_topPanel);
            BuildBottomPanel(_botPanel);

            if (_settingsPanel != null) UpdateSettingsPanelTheme();

            _board?.Invalidate();   // 棋盤也要跟著主題重繪
            RefreshStatus();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 頂部面板（先加 Label 再加卡片，確保 z-order）
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        private void BuildTopPanel(Panel top)
        {
            const int cardY = 9;
            const int cX = 174, cW = 312;   // 中央安全區

            // 計時器（先加，z-order 最低）
            _lblTimer = new Label();
            _lblTimer.Text      = FormatTime(_elapsed);
            _lblTimer.Font      = new Font("Consolas", 20, FontStyle.Bold);
            _lblTimer.ForeColor = ThemeText();
            _lblTimer.Location  = new Point(cX, 8);
            _lblTimer.Size      = new Size(cW, 32);
            _lblTimer.TextAlign = ContentAlignment.MiddleCenter;
            _lblTimer.AutoSize  = false;
            top.Controls.Add(_lblTimer);

            _lblStatus = new Label();
            _lblStatus.Text      = "您的回合";
            _lblStatus.Font      = new Font("Microsoft JhengHei", 9f);
            _lblStatus.ForeColor = Color.FromArgb(120, 200, 120);
            _lblStatus.Location  = new Point(cX, 43);
            _lblStatus.Size      = new Size(cW, 22);
            _lblStatus.TextAlign = ContentAlignment.MiddleCenter;
            _lblStatus.AutoSize  = false;
            top.Controls.Add(_lblStatus);

            // 左卡（黑棋）—— 後加，z-order 在上
            _leftCard = MakeCard(top, 12, cardY, CardW, CardH, ThemeCardAct());
            MakeLbl(_leftCard, "♟",
                new Font("Segoe UI Symbol", 18), Color.FromArgb(130,200,130),
                new Point(4, 3), new Size(32, 52));
            MakeLbl(_leftCard, "玩家",
                new Font("Microsoft JhengHei", 11, FontStyle.Bold), ThemeText(),
                new Point(40, 5), new Size(114, 22));
            MakeLbl(_leftCard, "黑棋",
                new Font("Microsoft JhengHei", 7.5f), ThemeSubText(),
                new Point(68, 33), new Size(40, 18));
            _leftCard.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                DrawPieceSmall(e.Graphics, 51, 41, Player.Black, 11);
            };

            // 右卡（白棋）
            _rightCard = MakeCard(top, FormW - 12 - CardW, cardY, CardW, CardH, ThemeCard());
            // 機器人圖示改用 GDI+ 繪製（避免 emoji 方框問題）
            _lblWName = new Label();
            _lblWName.Text      = _aiMode ? "電腦" : "玩家 2";
            _lblWName.Font      = new Font("Microsoft JhengHei", 11, FontStyle.Bold);
            _lblWName.ForeColor = ThemeText();
            _lblWName.Location  = new Point(8, 5);
            _lblWName.Size      = new Size(114, 22);
            _lblWName.AutoSize  = false;
            _rightCard.Controls.Add(_lblWName);
            MakeLbl(_rightCard, "白棋",
                new Font("Microsoft JhengHei", 7.5f), ThemeSubText(),
                new Point(36, 33), new Size(40, 18));
            _rightCard.Paint += (s, e) => {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                DrawPieceSmall(e.Graphics, 22, 41, Player.White, 11);
                DrawRobotIcon(e.Graphics, CardW - 38, 3, Color.FromArgb(120, 165, 225));
            };
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 底部面板（所有按鈕使用 RoundedButton，移除音樂按鈕）
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        private void BuildBottomPanel(Panel bot)
        {
            // ── 難度按鈕列 ─────────────────────────────────────────
            const int dW = 100, dH = 30, dGap = 5;
            _lblDiffTitle = new Label();
            _lblDiffTitle.Text      = "電腦難度：";
            _lblDiffTitle.Font      = new Font("Microsoft JhengHei", 9f, FontStyle.Bold);
            _lblDiffTitle.ForeColor = ThemeSubText();
            _lblDiffTitle.Size      = new Size(76, dH);
            _lblDiffTitle.TextAlign = ContentAlignment.MiddleRight;
            _lblDiffTitle.AutoSize  = false;

            int diffW = _lblDiffTitle.Width + 8 + 4 * dW + 3 * dGap;
            int diffX = (FormW - diffW) / 2;
            _lblDiffTitle.Location = new Point(diffX, 15);
            bot.Controls.Add(_lblDiffTitle);

            for (int i = 0; i < 4; i++)
            {
                RoundedButton b = new RoundedButton();
                b.Text      = DiffLabels[i];
                b.Font      = new Font("Microsoft JhengHei", 9f, FontStyle.Bold);
                b.ForeColor = Color.White;
                b.Location  = new Point(diffX + _lblDiffTitle.Width + 8 + i * (dW + dGap), 15);
                b.Size      = new Size(dW, dH);
                b.Radius    = 8;
                b.Cursor    = Cursors.Hand;
                b.Tag       = i;
                b.Click    += DiffBtn_Click;
                bot.Controls.Add(b);
                _diffBtns[i] = b;
            }
            UpdateDiffBtns();

            // ── 操作按鈕列 ─────────────────────────────────────────
            const int aW = 178, aH = 36, aGap = 12;
            int ax = (FormW - 3 * aW - 2 * aGap) / 2;
            const int ay = 60;

            MakeRoundBtn(bot, "↺  新遊戲  (N)",  Color.FromArgb(22,128,52),  ax,             ay, aW, aH, 10, (s,e) => NewGame());
            MakeRoundBtn(bot, "↩  悔棋      (U)", Color.FromArgb(128,88,18),  ax+aW+aGap,    ay, aW, aH, 10, (s,e) => UndoMove());
            MakeRoundBtn(bot, "⇄  切換對戰模式", Color.FromArgb(64,64,155),  ax+2*(aW+aGap), ay, aW, aH, 10, (s,e) => ToggleMode());

            // ── 齒輪設定按鈕（右下角圓形）─────────────────────────
            RoundedButton gear = new RoundedButton();
            gear.Text      = "⚙";
            gear.Font      = new Font("Segoe UI Symbol", 18, FontStyle.Regular);
            gear.ForeColor = ThemeSubText();
            gear.BackColor = _isDark ? Color.FromArgb(46,46,62) : Color.FromArgb(185,178,164);
            gear.Location  = new Point(FormW - 48, (BotH - 40) / 2);  // 更靠右，垂直置中
            gear.Size      = new Size(40, 40);
            gear.Radius    = 20;  // 正圓
            gear.Cursor    = Cursors.Hand;
            gear.Click    += (s, e) =>
            {
                _settingsPanel.Visible = !_settingsPanel.Visible;
                if (_settingsPanel.Visible) _settingsPanel.BringToFront();
            };
            bot.Controls.Add(gear);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 設定面板（懸浮於 Form，預設隱藏）
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        private void BuildSettingsPanel()
        {
            const int sw = 222, sh = 170;   // 加高，讓底部有足夠空間
            int sx = FormW - sw - 12;
            int sy = TopH + BoardSz - sh - 6;

            _settingsPanel = new Panel();
            _settingsPanel.Location = new Point(sx, sy);
            _settingsPanel.Size     = new Size(sw, sh);
            _settingsPanel.Visible  = false;
            _settingsPanel.Paint   += SettingsPanel_Paint;

            // 圓角 Region
            using (GraphicsPath gp = RoundedPath(new Rectangle(0, 0, sw, sh), 12))
                _settingsPanel.Region = new System.Drawing.Region(gp);

            // ─ 標題列 ─────────────────────────────────────────────
            _lblSettingsTitle = new Label();
            _lblSettingsTitle.Text      = "⚙  設定";
            _lblSettingsTitle.Font      = new Font("Microsoft JhengHei", 10, FontStyle.Bold);
            _lblSettingsTitle.Location  = new Point(12, 6);
            _lblSettingsTitle.Size      = new Size(160, 24);
            _lblSettingsTitle.AutoSize  = false;
            _settingsPanel.Controls.Add(_lblSettingsTitle);

            // 關閉按鈕
            RoundedButton btnClose = new RoundedButton();
            btnClose.Text      = "✕";
            btnClose.Font      = new Font("Microsoft JhengHei", 9, FontStyle.Bold);
            btnClose.ForeColor = Color.FromArgb(200, 180, 180);
            btnClose.Location  = new Point(sw - 30, 6);
            btnClose.Size      = new Size(22, 22);
            btnClose.Radius    = 6;
            btnClose.Cursor    = Cursors.Hand;
            btnClose.Click    += (s, e) => _settingsPanel.Visible = false;
            _settingsPanel.Controls.Add(btnClose);

            // ─ 分隔線 ─────────────────────────────────────────────
            _settingsSep1 = new Panel { Location = new Point(8, 33), Size = new Size(sw - 16, 1) };
            _settingsPanel.Controls.Add(_settingsSep1);

            // ─ 音量 ───────────────────────────────────────────────
            _lblVolTitle = new Label();
            _lblVolTitle.Text      = "♪  音量";       // ♪ = BMP U+266A，不會顯示方框
            _lblVolTitle.Font      = new Font("Microsoft JhengHei", 8.5f);
            _lblVolTitle.Location  = new Point(12, 38);
            _lblVolTitle.Size      = new Size(100, 16);
            _lblVolTitle.AutoSize  = false;
            _settingsPanel.Controls.Add(_lblVolTitle);

            _lblVolPct = new Label();
            _lblVolPct.Text      = MusicPlayer.Volume + "%";
            _lblVolPct.Font      = new Font("Consolas", 8.5f, FontStyle.Bold);
            _lblVolPct.Location  = new Point(sw - 44, 38);
            _lblVolPct.Size      = new Size(36, 16);
            _lblVolPct.TextAlign = ContentAlignment.MiddleRight;
            _lblVolPct.AutoSize  = false;
            _settingsPanel.Controls.Add(_lblVolPct);

            _trackVol = new TrackBar();
            _trackVol.Minimum       = 0;
            _trackVol.Maximum       = 100;
            _trackVol.Value         = MusicPlayer.Volume;
            _trackVol.TickFrequency = 10;
            _trackVol.TickStyle     = TickStyle.None;   // 移除刻度，防止視覺溢出覆蓋下方文字
            _trackVol.Location      = new Point(6, 55);
            _trackVol.Size          = new Size(sw - 12, 26);
            _trackVol.Scroll       += (s, e) =>
            {
                _lblVolPct.Text = _trackVol.Value + "%";
                MusicPlayer.SetVolume(_trackVol.Value);
            };
            _settingsPanel.Controls.Add(_trackVol);

            // ─ 分隔線 ─────────────────────────────────────────────
            _settingsSep2 = new Panel { Location = new Point(8, 88), Size = new Size(sw - 16, 1) };
            _settingsPanel.Controls.Add(_settingsSep2);

            const int tbW = 95, tbH = 33;

            _btnLightTheme = new RoundedButton();
            _btnLightTheme.Text     = "◯  亮色";
            _btnLightTheme.Font     = new Font("Microsoft JhengHei", 9, FontStyle.Bold);
            _btnLightTheme.Location = new Point(8, 120);
            _btnLightTheme.Size     = new Size(tbW, tbH);
            _btnLightTheme.Radius   = 8;
            _btnLightTheme.Cursor   = Cursors.Hand;
            _btnLightTheme.Click   += (s, e) => { _isDark = false; ApplyTheme(); };
            _settingsPanel.Controls.Add(_btnLightTheme);

            _btnDarkTheme = new RoundedButton();
            _btnDarkTheme.Text     = "◉  暗色";
            _btnDarkTheme.Font     = new Font("Microsoft JhengHei", 9, FontStyle.Bold);
            _btnDarkTheme.Location = new Point(sw - tbW - 8, 120);
            _btnDarkTheme.Size     = new Size(tbW, tbH);
            _btnDarkTheme.Radius   = 8;
            _btnDarkTheme.Cursor   = Cursors.Hand;
            _btnDarkTheme.Click   += (s, e) => { _isDark = true; ApplyTheme(); };
            _settingsPanel.Controls.Add(_btnDarkTheme);

            // ─ 主題 Label 最後加入（z-order 最高，不會被其他控制項蓋住）─
            // AutoSize=true 讓標籤自動計算寬度，不被截斷
            _lblThemeTitle = new Label();
            _lblThemeTitle.Text      = "◈  主題";
            _lblThemeTitle.Font      = new Font("Microsoft JhengHei", 8.5f);
            _lblThemeTitle.ForeColor = ThemeSubText();
            _lblThemeTitle.BackColor = ThemeSettings();   // 明確設定，避免透明問題
            _lblThemeTitle.AutoSize  = true;              // 自動決定寬高，不截斷
            _lblThemeTitle.Location  = new Point(12, 96);
            _settingsPanel.Controls.Add(_lblThemeTitle);  // 最後加入 = 最高 z-order

            Controls.Add(_settingsPanel);
            UpdateSettingsPanelTheme();
        }

        private void SettingsPanel_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle r = new Rectangle(0, 0, _settingsPanel.Width, _settingsPanel.Height);
            using (GraphicsPath path = RoundedPath(r, 12))
            using (Pen pen = new Pen(_isDark ? Color.FromArgb(55,55,75) : Color.FromArgb(168,160,146), 1.5f))
                g.DrawPath(pen, path);
            // 主題 Label 以 Label 控制項（最高 z-order）呈現，不在此重複繪製
        }

        private void UpdateSettingsPanelTheme()
        {
            if (_settingsPanel == null) return;

            Color panelBg = ThemeSettings();
            Color textCol = ThemeText();
            Color subCol  = ThemeSubText();
            Color sepCol  = ThemeSettingsSep();

            _settingsPanel.BackColor = panelBg;
            _trackVol.BackColor      = panelBg;

            if (_lblSettingsTitle != null) { _lblSettingsTitle.BackColor = panelBg; _lblSettingsTitle.ForeColor = textCol; }
            if (_lblVolTitle   != null) { _lblVolTitle.BackColor   = panelBg; _lblVolTitle.ForeColor   = subCol; }
            if (_lblVolPct     != null) { _lblVolPct.BackColor     = panelBg; _lblVolPct.ForeColor     = textCol; }
            if (_lblThemeTitle != null) { _lblThemeTitle.BackColor = panelBg; _lblThemeTitle.ForeColor = subCol; }
            if (_settingsSep1  != null) _settingsSep1.BackColor = sepCol;
            if (_settingsSep2  != null) _settingsSep2.BackColor = sepCol;

            // 主題按鈕：選中=藍，未選中=中性
            Color selColor = Color.FromArgb(0, 115, 200);
            Color unselColor = _isDark ? Color.FromArgb(55, 55, 74) : Color.FromArgb(188, 181, 166);
            if (_btnLightTheme != null)
            {
                _btnLightTheme.BackColor = !_isDark ? selColor : unselColor;
                _btnLightTheme.ForeColor = !_isDark ? Color.White : subCol;
                _btnLightTheme.Invalidate();
            }
            if (_btnDarkTheme != null)
            {
                _btnDarkTheme.BackColor = _isDark ? selColor : unselColor;
                _btnDarkTheme.ForeColor = _isDark ? Color.White : subCol;
                _btnDarkTheme.Invalidate();
            }

            _settingsPanel.Invalidate();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // Helper 工廠
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        private static Panel MakeCard(Control parent, int x, int y, int w, int h, Color bg)
        {
            Panel p = new Panel { Location = new Point(x,y), Size = new Size(w,h), BackColor = bg };
            parent.Controls.Add(p);
            return p;
        }

        private static Label MakeLbl(Control parent, string text, Font font, Color color, Point loc, Size sz)
        {
            Label l = new Label { Text=text, Font=font, ForeColor=color, Location=loc, Size=sz,
                                   TextAlign=ContentAlignment.MiddleCenter, AutoSize=false };
            parent.Controls.Add(l);
            return l;
        }

        private static RoundedButton MakeRoundBtn(Control parent, string text, Color bg,
            int x, int y, int w, int h, int radius, EventHandler onClick)
        {
            RoundedButton b = new RoundedButton();
            b.Text      = text;
            b.Font      = new Font("Microsoft JhengHei", 9.5f, FontStyle.Bold);
            b.ForeColor = Color.White;
            b.BackColor = bg;
            b.Location  = new Point(x, y);
            b.Size      = new Size(w, h);
            b.Radius    = radius;
            b.Cursor    = Cursors.Hand;
            b.Click    += onClick;
            parent.Controls.Add(b);
            return b;
        }

        private static GraphicsPath RoundedPath(Rectangle rect, int radius)
        {
            int r2 = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(rect.X, rect.Y, r2, r2, 180, 90);
            path.AddArc(rect.Right - r2, rect.Y, r2, r2, 270, 90);
            path.AddArc(rect.Right - r2, rect.Bottom - r2, r2, r2, 0, 90);
            path.AddArc(rect.X, rect.Bottom - r2, r2, r2, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void DiffBtn_Click(object sender, EventArgs e)
        {
            int idx = (int)((RoundedButton)sender).Tag;
            _ai.Difficulty = (Difficulty)idx;
            UpdateDiffBtns();
        }

        private void UpdateDiffBtns()
        {
            int sel = (int)_ai.Difficulty;
            for (int i = 0; i < 4; i++)
            {
                if (_diffBtns[i] == null) continue;
                bool on = (i == sel);
                _diffBtns[i].BackColor = on ? DiffColors[i] : ThemeDiffUnsel();
                _diffBtns[i].ForeColor = on ? Color.White   : ThemeSubText();
                _diffBtns[i].Invalidate();
            }
        }

        private static Color Lighten(Color c, int d) =>
            Color.FromArgb(Math.Min(255,c.R+d), Math.Min(255,c.G+d), Math.Min(255,c.B+d));

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 棋盤繪圖
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        private void Board_Paint(object sender, PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode     = SmoothingMode.AntiAlias;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            DrawBackground(g, _isDark);
            DrawGrid(g, _isDark);
            DrawStarPoints(g, _isDark);
            DrawCoordinates(g, _isDark);
            DrawPieces(g);
            if (_hover.HasValue && !_game.IsGameOver && !_aiThinking) DrawHover(g, _hover.Value);
            if (_game.IsGameOver && _game.WinningCells != null) DrawWinLine(g);
        }

        private static void DrawBackground(Graphics g, bool isDark)
        {
            // 簡約主題：純色，無木紋
            Color bg     = isDark ? Color.FromArgb(10, 10, 18)  : Color.FromArgb(250, 248, 242);
            Color border = isDark ? Color.FromArgb(35, 35, 52)  : Color.FromArgb(165, 158, 145);

            using (SolidBrush sb = new SolidBrush(bg))
                g.FillRectangle(sb, 0, 0, BoardSz, BoardSz);

            using (Pen pen = new Pen(border, 1f))
                g.DrawRectangle(pen, 1, 1, BoardSz - 2, BoardSz - 2);
        }

        private static void DrawGrid(Graphics g, bool isDark)
        {
            Color grid = isDark ? Color.FromArgb(42, 42, 62) : Color.FromArgb(148, 142, 130);
            using (Pen pen = new Pen(grid, 0.8f))
                for (int i = 0; i < GomokuGame.BoardSize; i++)
                { g.DrawLine(pen,Pad+i*Cell,Pad,Pad+i*Cell,Pad+Grid); g.DrawLine(pen,Pad,Pad+i*Cell,Pad+Grid,Pad+i*Cell); }
        }

        private static void DrawStarPoints(Graphics g, bool isDark)
        {
            Color star = isDark ? Color.FromArgb(62, 62, 88) : Color.FromArgb(118, 112, 98);
            int[] pts = {3,7,11};
            using (SolidBrush b = new SolidBrush(star))
                foreach (int r in pts) foreach (int c in pts)
                    g.FillEllipse(b, Pad+c*Cell-4, Pad+r*Cell-4, 8, 8);
        }

        private static void DrawCoordinates(Graphics g, bool isDark)
        {
            Color coord = isDark ? Color.FromArgb(58, 58, 82) : Color.FromArgb(135, 128, 115);
            const string cols = "ABCDEFGHJKLMNOP";
            using (Font font = new Font("Consolas", 7.5f))
            using (SolidBrush brush = new SolidBrush(coord))
                for (int i = 0; i < GomokuGame.BoardSize; i++)
                {
                    int px=Pad+i*Cell, py=Pad+i*Cell;
                    SizeF cs = g.MeasureString(cols[i].ToString(), font);
                    g.DrawString(cols[i].ToString(),font,brush, px-cs.Width/2, Pad/2-cs.Height/2);
                    g.DrawString(cols[i].ToString(),font,brush, px-cs.Width/2, BoardSz-Pad/2-cs.Height/2);
                    string row = (GomokuGame.BoardSize-i).ToString();
                    SizeF rs = g.MeasureString(row, font);
                    g.DrawString(row,font,brush, Pad/2-rs.Width/2, py-rs.Height/2);
                    g.DrawString(row,font,brush, BoardSz-Pad/2-rs.Width/2, py-rs.Height/2);
                }
        }

        private void DrawPieces(Graphics g)
        {
            int[] last = _game.LastMove;
            for (int r=0;r<GomokuGame.BoardSize;r++) for (int c=0;c<GomokuGame.BoardSize;c++)
            {
                Player pl = _game.Board[r,c];
                if (pl == Player.None) continue;
                bool isLast = last!=null && last[0]==r && last[1]==c;
                DrawPiece(g, Pad+c*Cell, Pad+r*Cell, pl, isLast);
            }
        }

        private static void DrawPiece(Graphics g, int cx, int cy, Player player, bool isLast)
        {
            int R = RadPiece;
            Rectangle rc = new Rectangle(cx-R, cy-R, R*2, R*2);
            using (SolidBrush shadow = new SolidBrush(Color.FromArgb(55,0,0,0)))
                g.FillEllipse(shadow, cx-R+2, cy-R+3, R*2, R*2);
            if (player == Player.Black)
            {
                using (LinearGradientBrush fill = new LinearGradientBrush(rc,
                    Color.FromArgb(90,90,90), Color.FromArgb(4,4,4), LinearGradientMode.ForwardDiagonal))
                    g.FillEllipse(fill, rc);
            }
            else
            {
                using (LinearGradientBrush fill = new LinearGradientBrush(rc,
                    Color.FromArgb(255,255,255), Color.FromArgb(198,198,198), LinearGradientMode.ForwardDiagonal))
                    g.FillEllipse(fill, rc);
                using (Pen border = new Pen(Color.FromArgb(155,155,155), 1.2f))
                    g.DrawEllipse(border, rc);
            }
            int sh = R-4;
            if (sh > 0)
            {
                Rectangle shrc = new Rectangle(cx-R+4, cy-R+4, sh, sh);
                using (LinearGradientBrush shine = new LinearGradientBrush(shrc,
                    Color.FromArgb(player==Player.Black?60:95, 255,255,255),
                    Color.FromArgb(0,255,255,255), LinearGradientMode.ForwardDiagonal))
                    g.FillEllipse(shine, shrc);
            }
            if (isLast)
                using (SolidBrush mark = new SolidBrush(Color.FromArgb(230,50,50)))
                    g.FillEllipse(mark, cx-4, cy-4, 8, 8);
        }

        private static void DrawPieceSmall(Graphics g, int cx, int cy, Player player, int r)
        {
            Rectangle rc = new Rectangle(cx-r, cy-r, r*2, r*2);
            using (SolidBrush shadow = new SolidBrush(Color.FromArgb(50,0,0,0)))
                g.FillEllipse(shadow, cx-r+1, cy-r+2, r*2, r*2);
            if (player == Player.Black)
            {
                using (LinearGradientBrush fill = new LinearGradientBrush(rc,
                    Color.FromArgb(85,85,85), Color.FromArgb(8,8,8), LinearGradientMode.ForwardDiagonal))
                    g.FillEllipse(fill, rc);
            }
            else
            {
                using (LinearGradientBrush fill = new LinearGradientBrush(rc,
                    Color.White, Color.FromArgb(200,200,200), LinearGradientMode.ForwardDiagonal))
                    g.FillEllipse(fill, rc);
                using (Pen border = new Pen(Color.FromArgb(155,155,155), 1f))
                    g.DrawEllipse(border, rc);
            }
            int sh = r-3;
            if (sh > 0)
            {
                Rectangle shrc = new Rectangle(cx-r+3, cy-r+3, sh, sh);
                using (LinearGradientBrush shine = new LinearGradientBrush(shrc,
                    Color.FromArgb(player==Player.Black?55:90, 255,255,255),
                    Color.FromArgb(0,255,255,255), LinearGradientMode.ForwardDiagonal))
                    g.FillEllipse(shine, shrc);
            }
        }

        private void DrawHover(Graphics g, Point cell)
        {
            if (_game.Board[cell.Y, cell.X] != Player.None) return;
            int cx=Pad+cell.X*Cell, cy=Pad+cell.Y*Cell;
            Color hc = _game.CurrentPlayer==Player.Black
                ? Color.FromArgb(95,15,15,15) : Color.FromArgb(95,240,240,240);
            using (SolidBrush b = new SolidBrush(hc))
                g.FillEllipse(b, cx-RadPiece, cy-RadPiece, RadPiece*2, RadPiece*2);
        }

        /// <summary>
        /// 在頂部右卡 Paint 事件中繪製機器人圖示（GDI+，不依賴 emoji 字型）。
        /// x, y = 圖示區域左上角（於卡片座標系內）。
        /// </summary>
        private static void DrawRobotIcon(Graphics g, int x, int y, Color col)
        {
            // 繪製範圍約 30×48 px
            using (Pen pen = new Pen(col, 1.8f))
            using (SolidBrush brush = new SolidBrush(col))
            {
                // 天線
                int ax = x + 15;
                g.DrawLine(pen, ax, y + 9, ax, y + 2);
                g.FillEllipse(brush, ax - 3, y, 6, 5);

                // 頭（圓角矩形近似）
                Rectangle head = new Rectangle(x + 2, y + 9, 26, 19);
                g.DrawRectangle(pen, head);

                // 眼睛
                g.FillEllipse(brush, x + 7,  y + 14, 6, 6);   // 左眼
                g.FillEllipse(brush, x + 19, y + 14, 6, 6);   // 右眼

                // 嘴巴（小矩形）
                g.DrawRectangle(pen, x + 9, y + 22, 12, 4);

                // 身體
                g.DrawRectangle(pen, x + 6, y + 30, 18, 12);

                // 手臂
                g.DrawLine(pen, x + 2,  y + 34, x + 6,  y + 34);
                g.DrawLine(pen, x + 24, y + 34, x + 28, y + 34);
            }
        }

        private void DrawWinLine(Graphics g)
        {
            List<int[]> cells = _game.WinningCells;
            if (cells==null||cells.Count<2) return;
            cells.Sort((a,b) => a[1]!=b[1] ? a[1].CompareTo(b[1]) : a[0].CompareTo(b[0]));
            double x1=Pad+cells[0][1]*Cell, y1=Pad+cells[0][0]*Cell;
            double x2=Pad+cells[cells.Count-1][1]*Cell, y2=Pad+cells[cells.Count-1][0]*Cell;
            double dx=x2-x1, dy=y2-y1, len=Math.Sqrt(dx*dx+dy*dy);
            if (len>0) { const int ext=14; x1-=dx/len*ext; y1-=dy/len*ext; x2+=dx/len*ext; y2+=dy/len*ext; }
            using (Pen pen = new Pen(Color.FromArgb(215,255,55,55), 4.5f))
            { pen.StartCap=LineCap.Round; pen.EndCap=LineCap.Round; g.DrawLine(pen,(float)x1,(float)y1,(float)x2,(float)y2); }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 輸入
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        private void Board_MouseMove(object sender, MouseEventArgs e)
        {
            Point? c = ToCell(e.Location);
            if (!Equals(c, _hover)) { _hover = c; _board.Invalidate(); }
        }

        private async void Board_MouseClick(object sender, MouseEventArgs e)
        {
            if (_settingsPanel != null && _settingsPanel.Visible)
                _settingsPanel.Visible = false;

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
            if (e.KeyCode == Keys.Escape && _settingsPanel!=null) _settingsPanel.Visible = false;
        }

        private static Point? ToCell(Point p)
        {
            int c=(int)Math.Round((p.X-Pad)/(double)Cell);
            int r=(int)Math.Round((p.Y-Pad)/(double)Cell);
            if (c<0||c>=GomokuGame.BoardSize||r<0||r>=GomokuGame.BoardSize) return null;
            return new Point(c, r);
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 遊戲邏輯
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        private async Task AIMoveAsync()
        {
            int ver = _gameVersion;
            _aiThinking = true; RefreshStatus();
            int thinkMs;
            switch (_ai.Difficulty)
            {
                case Difficulty.Easy:   thinkMs = 900;   break;  // 慢速，像在猶豫
                case Difficulty.Hard:   thinkMs = 1800;  break;  // 明顯思考感
                case Difficulty.Master: thinkMs = 600;   break;  // minimax 本身耗時，加少一點
                default:                thinkMs = 1200;  break;  // Medium：1.2 秒
            }
            await Task.Delay(thinkMs);
            if (_gameVersion!=ver||_game.IsGameOver) { _aiThinking=false; RefreshStatus(); return; }
            int[] move = await Task.Run(() => _ai.GetBestMove(_game, Player.White));
            if (_gameVersion!=ver||_game.IsGameOver) { _aiThinking=false; RefreshStatus(); return; }
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
            _elapsed = 0; _started = false;
            _clock.Stop();
            RefreshTimer(); RefreshBoard(); RefreshStatus();
            SoundManager.PlayNewGame();
        }

        private void UndoMove()
        {
            if (_aiThinking || _game.MoveCount == 0) return;
            _game.Undo(_aiMode ? Math.Min(2,_game.MoveCount) : 1);
            RefreshBoard(); RefreshStatus();
            SoundManager.PlayUndo();
        }

        private void ToggleMode()
        {
            _aiMode = !_aiMode;
            // 重建頂部面板以更新電腦/玩家2名稱
            _topPanel.Controls.Clear();
            BuildTopPanel(_topPanel);
            NewGame();
        }

        private void GameOver()
        {
            _clock.Stop(); _board.Invalidate(); RefreshStatus();
            SoundManager.PlayWin();
            string msg;
            if (_game.Winner==Player.None) msg="棋盤已滿，平局！";
            else if (_game.Winner==Player.Black) msg=_aiMode?"🎉 恭喜您獲勝！":"⚫ 黑棋獲勝！";
            else msg=_aiMode?"電腦獲勝！再接再厲！":"⚪ 白棋獲勝！";
            string time = FormatTime(_elapsed);
            DialogResult ans = MessageBox.Show(msg+"\n\n遊戲時間："+time+"　共 "+_game.MoveCount+" 手\n\n要再玩一局嗎？",
                "遊戲結束", MessageBoxButtons.YesNo, MessageBoxIcon.Information);
            if (ans==DialogResult.Yes) NewGame();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // UI 更新
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        private void RefreshBoard()  { _board.Invalidate(); RefreshStatus(); }

        private void RefreshStatus()
        {
            Color active = ThemeCardAct(), inactive = ThemeCard();
            if (_leftCard  == null || _rightCard == null) return;

            if (_game.IsGameOver)
            {
                _leftCard.BackColor = inactive; _rightCard.BackColor = inactive;
                if (_game.Winner==Player.None)       { _lblStatus.Text="平局！";    _lblStatus.ForeColor=Color.FromArgb(220,200,80); }
                else if (_game.Winner==Player.Black) { _lblStatus.Text="黑棋獲勝！"; _lblStatus.ForeColor=Color.FromArgb(100,220,100); }
                else                                 { _lblStatus.Text="白棋獲勝！"; _lblStatus.ForeColor=Color.FromArgb(100,220,100); }
            }
            else if (_aiThinking)
            {
                _leftCard.BackColor=inactive; _rightCard.BackColor=active;
                _lblStatus.Text="電腦思考中…"; _lblStatus.ForeColor=Color.FromArgb(255,180,60);
            }
            else
            {
                bool blackTurn = (_game.CurrentPlayer==Player.Black);
                _leftCard.BackColor  = blackTurn ? active : inactive;
                _rightCard.BackColor = blackTurn ? inactive : active;
                _lblStatus.Text      = blackTurn ? "您的回合" : "電腦回合";
                _lblStatus.ForeColor = Color.FromArgb(120, 200, 120);
            }
            _leftCard.Invalidate(); _rightCard.Invalidate();
        }

        private void RefreshTimer()
        {
            if (_lblTimer != null) _lblTimer.Text = FormatTime(_elapsed);
        }

        private static string FormatTime(int sec)
            => string.Format("{0:D2}:{1:D2}", sec/60, sec%60);

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 關閉確認
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            if (e.CloseReason == CloseReason.UserClosing)
            {
                DialogResult res = MessageBox.Show("確定要關閉遊戲嗎？", "關閉遊戲",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question,
                    MessageBoxDefaultButton.Button2);
                if (res == DialogResult.No) e.Cancel = true;
                else MusicPlayer.Stop();
            }
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 雙緩衝面板
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    public sealed class DoubleBufferedPanel : Panel
    {
        public DoubleBufferedPanel()
        {
            DoubleBuffered = true;
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // 圓角按鈕（使用 Region 裁剪，真實圓角無方框）
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    public class RoundedButton : Button
    {
        private bool _hovered = false;
        private int  _radius  = 8;

        public int Radius
        {
            get { return _radius; }
            set { _radius = value; UpdateRegion(); Invalidate(); }
        }

        public RoundedButton()
        {
            SetStyle(ControlStyles.UserPaint |
                     ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer, true);
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderSize = 0;
        }

        protected override void OnResize(EventArgs e)
        { base.OnResize(e); UpdateRegion(); }

        private void UpdateRegion()
        {
            if (Width <= 0 || Height <= 0) return;
            int r = Math.Min(_radius * 2, Math.Min(Width, Height));
            using (GraphicsPath path = new GraphicsPath())
            {
                path.AddArc(0, 0, r, r, 180, 90);
                path.AddArc(Width - r, 0, r, r, 270, 90);
                path.AddArc(Width - r, Height - r, r, r, 0, 90);
                path.AddArc(0, Height - r, r, r, 90, 90);
                path.CloseFigure();
                Region = new System.Drawing.Region(path);
            }
        }

        protected override void OnMouseEnter(EventArgs e)
        { base.OnMouseEnter(e); _hovered = true; Invalidate(); }

        protected override void OnMouseLeave(EventArgs e)
        { base.OnMouseLeave(e); _hovered = false; Invalidate(); }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // hover 時略亮
            Color bg = _hovered
                ? Color.FromArgb(Math.Min(255,BackColor.R+22),
                                 Math.Min(255,BackColor.G+22),
                                 Math.Min(255,BackColor.B+22))
                : BackColor;

            // Region 已裁剪圓角，直接填矩形即可
            using (SolidBrush brush = new SolidBrush(bg))
                g.FillRectangle(brush, ClientRectangle);

            TextRenderer.DrawText(g, Text, Font, ClientRectangle, ForeColor,
                TextFormatFlags.HorizontalCenter |
                TextFormatFlags.VerticalCenter   |
                TextFormatFlags.SingleLine       |
                TextFormatFlags.NoPadding);   // 移除 GDI 文字內距，讓圖示在圓形正中央
        }
    }
}
