using System;
using System.IO;
using System.Media;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace Gomoku
{
    /// <summary>
    /// 播放 bgm.wav 背景音樂。
    /// 將 bgm.wav 加入 Visual Studio 專案，設「複製到輸出目錄 = 永遠複製」即可自動播放。
    /// </summary>
    public static class MusicPlayer
    {
        private static SoundPlayer  _player;
        private static MemoryStream _stream;
        private static bool         _ready    = false;
        public  static bool         IsPlaying { get; private set; }
        public  static int          Volume    { get; private set; } = 50;

        static MusicPlayer()
        {
            Thread t = new Thread(Generate) { IsBackground = true };
            t.Start();
        }

        // ── Public API ──────────────────────────────────────────────

        public static void Start()
        {
            if (!_ready || IsPlaying) return;
            try { _stream.Position = 0; _player.PlayLooping(); IsPlaying = true; ApplyVolume(); }
            catch { }
        }

        public static void Stop()
        {
            try { _player?.Stop(); } catch { }
            IsPlaying = false;
        }

        public static void Toggle() { if (IsPlaying) Stop(); else Start(); }

        /// <summary>即時調整音量（0–100），使用 waveOutSetVolume，不中斷播放。</summary>
        public static void SetVolume(int pct)
        {
            Volume = Math.Max(0, Math.Min(100, pct));
            ApplyVolume();
        }

        [DllImport("winmm.dll", SetLastError = false)]
        private static extern uint waveOutSetVolume(IntPtr hwo, uint dwVolume);

        private static void ApplyVolume()
        {
            try { uint v = (uint)(0xFFFFu * Volume / 100); waveOutSetVolume(IntPtr.Zero, v | (v << 16)); }
            catch { }
        }

        // ── 載入 bgm.wav ────────────────────────────────────────────

        private static void Generate()
        {
            try
            {
                string path = FindBgm();
                if (path == null) return;   // 找不到檔案 → 靜音，不報錯

                byte[] bytes = File.ReadAllBytes(path);
                _stream = new MemoryStream(bytes);
                _player = new SoundPlayer(_stream);
                _player.Load();
                _ready  = true;
                Start();
            }
            catch { }
        }

        private static string FindBgm()
        {
            string[] dirs =
            {
                AppDomain.CurrentDomain.BaseDirectory,
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                Directory.GetCurrentDirectory()
            };
            foreach (string dir in dirs)
            {
                if (string.IsNullOrEmpty(dir)) continue;
                string p = Path.Combine(dir, "bgm.wav");
                if (File.Exists(p)) return p;
            }
            return null;
        }
    }
}
