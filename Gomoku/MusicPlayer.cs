using System;
using System.IO;
using System.Media;
using System.Runtime.InteropServices;
using System.Threading;

namespace Gomoku
{
    /// <summary>
    /// 背景執行緒合成 Lofi 音樂，PlayLooping 循環播放。
    /// 使用 waveOutSetVolume API 即時調整音量，不停止也不重建 SoundPlayer。
    /// </summary>
    public static class MusicPlayer
    {
        private static SoundPlayer  _player;
        private static MemoryStream _stream;
        private static short[]      _masterPcm;
        private static bool         _ready    = false;
        public  static bool         IsPlaying { get; private set; }
        public  static int          Volume    { get; private set; } = 80;

        static MusicPlayer()
        {
            Thread t = new Thread(Generate);
            t.IsBackground = true;
            t.Start();
        }

        // ── Public API ──────────────────────────────────────────────

        public static void Start()
        {
            if (!_ready || IsPlaying) return;
            try
            {
                _stream.Position = 0;
                _player.PlayLooping();
                IsPlaying = true;
                ApplyVolume();   // 啟動後立即套用音量
            }
            catch { }
        }

        public static void Stop()
        {
            try { _player?.Stop(); } catch { }
            IsPlaying = false;
        }

        public static void Toggle() { if (IsPlaying) Stop(); else Start(); }

        /// <summary>
        /// 即時調整音量（0–100）。
        /// 使用 waveOutSetVolume() 直接修改播放中的音量，不停止、不重建 SoundPlayer。
        /// </summary>
        public static void SetVolume(int pct)
        {
            Volume = Math.Max(0, Math.Min(100, pct));
            ApplyVolume();
        }

        // winmm.dll waveOutSetVolume：hwo=NULL 代表預設輸出裝置（Vista+ 是應用程式獨立音量）
        [DllImport("winmm.dll", SetLastError = false)]
        private static extern uint waveOutSetVolume(IntPtr hwo, uint dwVolume);

        private static void ApplyVolume()
        {
            try
            {
                uint vol = (uint)(0xFFFFu * Volume / 100);
                waveOutSetVolume(IntPtr.Zero, vol | (vol << 16));
            }
            catch { }
        }

        // ── 背景生成 ────────────────────────────────────────────────

        private static void Generate()
        {
            try
            {
                // ═══════════════════════════════════════════════════
                // 【使用自訂音樂】
                // bgm.wav 已放在專案資料夾中。
                // 在 Visual Studio 中，請對 bgm.wav 右鍵 → 屬性 → 
                //   複製到輸出目錄 = 「永遠複製」
                // 這樣每次建置時 bgm.wav 會自動複製到 bin/Debug/。
                // ═══════════════════════════════════════════════════

                // 搜尋 bgm.wav：先找 .exe 所在目錄，再找工作目錄
                string bgmPath = null;
                string[] searchDirs = new string[]
                {
                    AppDomain.CurrentDomain.BaseDirectory,
                    System.IO.Path.GetDirectoryName(
                        System.Reflection.Assembly.GetExecutingAssembly().Location),
                    System.IO.Directory.GetCurrentDirectory()
                };

                foreach (string dir in searchDirs)
                {
                    string candidate = System.IO.Path.Combine(dir, "bgm.wav");
                    if (System.IO.File.Exists(candidate)) { bgmPath = candidate; break; }
                }

                if (bgmPath != null)
                {
                    // 載入自訂 WAV 音樂
                    byte[] bytes = System.IO.File.ReadAllBytes(bgmPath);
                    _stream = new System.IO.MemoryStream(bytes);
                    _player = new SoundPlayer(_stream);
                    _player.Load();
                    _ready  = true;
                    Start();
                    return;
                }

                // bgm.wav 不存在 → 合成預設 Lofi 音樂
                _masterPcm = BuildLofiLoop();
                _stream    = BuildWav(_masterPcm, SR);
                _player    = new SoundPlayer(_stream);
                _player.Load();
                _ready     = true;
                Start();
            }
            catch { }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // Lofi 合成
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        private const int    SR   = 44100;
        private const double BPM  = 80.0;
        private const double BEAT = 60.0 / BPM;
        private const int    BEATS = 8 * 4;   // 8 bars × 4 beats = 32 beats (24 s)

        private static readonly double[][] CHORDS =
        {
            new[] { 174.61, 220.00, 261.63, 329.63 },  // Fmaj7
            new[] { 164.81, 196.00, 246.94, 293.66 },  // Em7
            new[] { 220.00, 261.63, 329.63, 392.00 },  // Am7
            new[] { 146.83, 174.61, 220.00, 261.63 }   // Dm7
        };
        private static readonly double[] BASS = { 87.31, 82.41, 110.00, 73.42 };
        private static readonly double[][] MELODY =
        {
            new[] { 0,698.46,1,  1,440.00,1,  2,523.25,2,  4,349.23,1,  5,440.00,1,  6,329.63,1 },
            new[] { 0,659.25,1,  1,587.33,1,  2,493.88,2,  4,392.00,1,  5,329.63,1,  6,293.66,1 },
            new[] { 0,440.00,1,  1,523.25,1,  2,659.25,2,  4,392.00,1,  5,440.00,1,  6,523.25,1 },
            new[] { 0,587.33,1,  1,349.23,1,  2,440.00,2,  4,293.66,1,  5,261.63,1,  6,220.00,1 }
        };

        private static short[] BuildLofiLoop()
        {
            int N = (int)(SR * BEATS * BEAT);
            double[] buf = new double[N];
            Random rng = new Random(7);
            double phraseSec = 8 * BEAT;

            for (int i = 0; i < N; i++)
            {
                double t     = (double)i / SR;
                int    cIdx  = (int)(t / phraseSec) % 4;
                double cStart = (int)(t / phraseSec) * phraseSec;

                // 和弦墊音
                foreach (double freq in CHORDS[cIdx])
                    buf[i] += PianoPad(freq, t - cStart, phraseSec * 0.97) * 0.10;

                // 旋律
                double[] motif = MELODY[cIdx];
                for (int m = 0; m < motif.Length; m += 3)
                    buf[i] += PianoMelody(motif[m+1], t - (cStart + motif[m]*BEAT), motif[m+2]*BEAT*0.9) * 0.13;

                // Bass
                int beat = (int)(t / BEAT);
                if (beat % 4 == 0 || beat % 4 == 2)
                    buf[i] += BassNote(BASS[cIdx], t - beat*BEAT, BEAT*1.6) * 0.28;

                // 鼓
                double bf = t - beat * BEAT;
                if (beat%4==0||beat%4==2) buf[i] += Kick(bf) * 0.45;
                if (beat%4==1||beat%4==3) buf[i] += Snare(bf, rng) * 0.22;
                buf[i] += HiHat(bf, rng) * 0.08;
                double hbf = t - (int)(t/(BEAT*0.5))*(BEAT*0.5);
                if ((int)(t/(BEAT*0.5))%2==1) buf[i] += HiHat(hbf, rng) * 0.05;

                // 黑膠雜音
                buf[i] += (rng.NextDouble()*2-1) * 0.012;
                if (rng.NextDouble() < 0.0004) buf[i] += (rng.NextDouble()*2-1) * 0.06;
            }

            // 低通濾波（溫暖感）
            double prev = 0;
            for (int i = 0; i < N; i++) { buf[i] = 0.72*prev + 0.28*buf[i]; prev = buf[i]; }

            // 正規化
            double peak = 0;
            foreach (double v in buf) if (Math.Abs(v) > peak) peak = Math.Abs(v);
            double scale = peak > 0 ? 0.82/peak : 1.0;

            short[] pcm = new short[N];
            for (int i = 0; i < N; i++)
            {
                double v = buf[i]*scale*short.MaxValue;
                pcm[i] = v > short.MaxValue ? short.MaxValue : v < short.MinValue ? short.MinValue : (short)v;
            }
            return pcm;
        }

        // ── 合成函式 ────────────────────────────────────────────────

        private static double PianoPad(double f, double t, double dur)
        {
            if (t<0||t>dur) return 0;
            double env = PadEnv(t,dur,0.05,0.5,0.55,0.5);
            return (Math.Sin(TW*f*t)+0.55*Math.Sin(TW*2*f*t)+0.28*Math.Sin(TW*3*f*t)
                   +0.12*Math.Sin(TW*4*f*t)+0.05*Math.Sin(TW*5*f*t))*env;
        }
        private static double PianoMelody(double f, double t, double dur)
        {
            if (t<0||t>dur) return 0;
            double env = PadEnv(t,dur,0.015,0.25,0.45,0.25);
            return (Math.Sin(TW*f*t)+0.50*Math.Sin(TW*2*f*t)+0.22*Math.Sin(TW*3*f*t)
                   +0.08*Math.Sin(TW*4*f*t))*env;
        }
        private static double BassNote(double f, double t, double dur)
        {
            if (t<0||t>dur) return 0;
            double env = PadEnv(t,dur,0.008,0.15,0.6,0.12);
            return (Math.Sin(TW*f*t)+0.3*Math.Sin(TW*2*f*t))*env;
        }
        private static double Kick(double t)
        {
            if (t<0||t>0.32) return 0;
            return Math.Sin(TW*(150*Math.Exp(-t*14)+55)*t)*Math.Exp(-t*9);
        }
        private static double Snare(double t, Random rng)
        {
            if (t<0||t>0.18) return 0;
            return (0.65*(rng.NextDouble()*2-1)+0.35*Math.Sin(TW*185*t))*Math.Exp(-t*22);
        }
        private static double HiHat(double t, Random rng)
        {
            if (t<0||t>0.055) return 0;
            return (rng.NextDouble()*2-1)*Math.Exp(-t*70);
        }
        private static double PadEnv(double t, double dur, double atk, double dec, double sus, double rel)
        {
            if (t<atk)          return t/atk;
            if (t<atk+dec)      return 1.0-(1.0-sus)*(t-atk)/dec;
            if (t<dur-rel)      return sus;
            double r = (dur-t)/rel;
            return r<0?0:sus*r;
        }
        private const double TW = 2*Math.PI;

        // ── WAV 建構 ────────────────────────────────────────────────

        private static MemoryStream BuildWav(short[] pcm, int sr)
        {
            int db = pcm.Length*2;
            MemoryStream ms = new MemoryStream(44+db);
            BinaryWriter w  = new BinaryWriter(ms);
            w.Write(new byte[]{(byte)'R',(byte)'I',(byte)'F',(byte)'F'});
            w.Write(36+db);
            w.Write(new byte[]{(byte)'W',(byte)'A',(byte)'V',(byte)'E'});
            w.Write(new byte[]{(byte)'f',(byte)'m',(byte)'t',(byte)' '});
            w.Write(16); w.Write((short)1); w.Write((short)1);
            w.Write(sr); w.Write(sr*2); w.Write((short)2); w.Write((short)16);
            w.Write(new byte[]{(byte)'d',(byte)'a',(byte)'t',(byte)'a'});
            w.Write(db);
            foreach (short s in pcm) w.Write(s);
            ms.Position = 0;
            return ms;
        }
    }
}
