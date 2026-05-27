using System;
using System.IO;
using System.Media;
using System.Threading;

namespace Gomoku
{
    /// <summary>
    /// 在程式啟動時於背景執行緒合成 Lofi 風格背景音樂，不依賴外部音訊檔案。
    /// 曲風：80 BPM、Fmaj7-Em7-Am7-Dm7 爵士和弦、鋼琴墊音、Bass、Boom-Bap 鼓組、黑膠雜音。
    /// </summary>
    public static class MusicPlayer
    {
        private static SoundPlayer  _player;
        private static MemoryStream _stream;
        private static bool         _ready   = false;
        public  static bool         IsPlaying { get; private set; }

        static MusicPlayer()
        {
            // 在背景執行緒生成，避免啟動時凍結 UI
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
            }
            catch { }
        }

        public static void Stop()
        {
            try { _player?.Stop(); } catch { }
            IsPlaying = false;
        }

        public static void Toggle()
        {
            if (IsPlaying) Stop();
            else Start();
        }

        // ── 背景生成 ────────────────────────────────────────────────

        private static void Generate()
        {
            try
            {
                short[] pcm = BuildLofiLoop();
                _stream = BuildWav(pcm, SR);
                _player = new SoundPlayer(_stream);
                _player.Load();
                _ready = true;
            }
            catch { }
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // Lofi 合成主流程
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        private const int    SR      = 44100;
        private const double BPM     = 80.0;
        private const double BEAT    = 60.0 / BPM;   // 0.75 s per beat
        private const int    BARS    = 8;
        private const int    BEATS   = BARS * 4;      // 32 beats = 24 s

        // Chord progression: Fmaj7 → Em7 → Am7 → Dm7  (2 bars / 8 beats each)
        private static readonly double[][] CHORDS =
        {
            new[] { 174.61, 220.00, 261.63, 329.63 },  // Fmaj7: F3 A3 C4 E4
            new[] { 164.81, 196.00, 246.94, 293.66 },  // Em7:   E3 G3 B3 D4
            new[] { 220.00, 261.63, 329.63, 392.00 },  // Am7:   A3 C4 E4 G4
            new[] { 146.83, 174.61, 220.00, 261.63 }   // Dm7:   D3 F3 A3 C4
        };

        // Bass root notes: F2 E2 A2 D2
        private static readonly double[] BASS = { 87.31, 82.41, 110.00, 73.42 };

        // Simple pentatonic melody (per-chord, 8-beat phrase)
        // Each row: [beat, freq, durationBeats] × N notes
        private static readonly double[][] MELODY =
        {
            // Fmaj7: F5 A4 C5 . F4 E4 .
            new[] { 0,698.46,1,  1,440.00,1,  2,523.25,2,  4,349.23,1,  5,440.00,1,  6,329.63,1 },
            // Em7:   E5 D5 B4 . G4 E4 .
            new[] { 0,659.25,1,  1,587.33,1,  2,493.88,2,  4,392.00,1,  5,329.63,1,  6,293.66,1 },
            // Am7:   A4 C5 E5 . G4 A4 .
            new[] { 0,440.00,1,  1,523.25,1,  2,659.25,2,  4,392.00,1,  5,440.00,1,  6,523.25,1 },
            // Dm7:   D5 F4 A4 . D4 C4 .
            new[] { 0,587.33,1,  1,349.23,1,  2,440.00,2,  4,293.66,1,  5,261.63,1,  6,220.00,1 }
        };

        private static short[] BuildLofiLoop()
        {
            int N   = (int)(SR * BEATS * BEAT);
            double[] buf = new double[N];
            Random rng = new Random(7);  // deterministic

            double phraseSec = 8 * BEAT;  // 8 beats per chord

            for (int i = 0; i < N; i++)
            {
                double t = (double)i / SR;

                // ── 和弦墊音（鋼琴） ──────────────────────────────
                int  cIdx   = (int)(t / phraseSec) % 4;
                double cStart = (int)(t / phraseSec) * phraseSec;
                double cDur   = phraseSec * 0.97;
                foreach (double freq in CHORDS[cIdx])
                    buf[i] += PianoPad(freq, t - cStart, cDur) * 0.10;

                // ── 旋律 ──────────────────────────────────────────
                double[] motif = MELODY[cIdx];
                for (int m = 0; m < motif.Length; m += 3)
                {
                    double mStart = cStart + motif[m] * BEAT;
                    double mDur   = motif[m + 2] * BEAT * 0.9;
                    buf[i] += PianoMelody(motif[m + 1], t - mStart, mDur) * 0.13;
                }

                // ── Bass ──────────────────────────────────────────
                int    beat       = (int)(t / BEAT);
                double beatStart  = beat * BEAT;
                double beatFrac   = t - beatStart;
                int    barBeat    = beat % 4;

                // 根音: 每拍第 1 和第 3 拍
                if (barBeat == 0 || barBeat == 2)
                {
                    int bIdx = ((int)(t / phraseSec) % 4);
                    buf[i] += BassNote(BASS[bIdx], beatFrac, BEAT * 1.6) * 0.28;
                }

                // ── 鼓組 ──────────────────────────────────────────
                if (barBeat == 0 || barBeat == 2)
                    buf[i] += Kick(beatFrac) * 0.45;          // 大鼓 beat 1,3
                if (barBeat == 1 || barBeat == 3)
                    buf[i] += Snare(beatFrac, rng) * 0.22;    // 小鼓 beat 2,4

                // Hi-hat: 每拍 + off-beat
                buf[i] += HiHat(beatFrac, rng) * 0.08;
                double halfBeatFrac = t - (int)(t / (BEAT * 0.5)) * (BEAT * 0.5);
                if ((int)(t / (BEAT * 0.5)) % 2 == 1)
                    buf[i] += HiHat(halfBeatFrac, rng) * 0.05;

                // ── 黑膠雜音 ─────────────────────────────────────
                buf[i] += (rng.NextDouble() * 2 - 1) * 0.012;
                if (rng.NextDouble() < 0.0004)
                    buf[i] += (rng.NextDouble() * 2 - 1) * 0.06;
            }

            // 簡單低通濾波（溫暖感）
            double prev = 0, alpha = 0.72;
            for (int i = 0; i < N; i++)
            {
                buf[i] = alpha * prev + (1 - alpha) * buf[i];
                prev = buf[i];
            }

            // 正規化
            double peak = 0;
            foreach (double v in buf) if (Math.Abs(v) > peak) peak = Math.Abs(v);
            double scale = peak > 0 ? 0.82 / peak : 1.0;

            short[] pcm = new short[N];
            for (int i = 0; i < N; i++)
            {
                double v = buf[i] * scale * short.MaxValue;
                if (v >  short.MaxValue) v =  short.MaxValue;
                if (v <  short.MinValue) v =  short.MinValue;
                pcm[i] = (short)v;
            }
            return pcm;
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // 合成函式
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        // 鋼琴墊音：多諧波 + 慢衰減
        private static double PianoPad(double freq, double localT, double dur)
        {
            if (localT < 0 || localT > dur) return 0;
            double env = PadEnv(localT, dur, 0.05, 0.5, 0.55, 0.5);
            double s = Math.Sin(TW * freq * localT)
                     + 0.55 * Math.Sin(TW * 2 * freq * localT)
                     + 0.28 * Math.Sin(TW * 3 * freq * localT)
                     + 0.12 * Math.Sin(TW * 4 * freq * localT)
                     + 0.05 * Math.Sin(TW * 5 * freq * localT);
            return s * env;
        }

        // 旋律音：稍亮、稍短的衰減
        private static double PianoMelody(double freq, double localT, double dur)
        {
            if (localT < 0 || localT > dur) return 0;
            double env = PadEnv(localT, dur, 0.015, 0.25, 0.45, 0.25);
            double s = Math.Sin(TW * freq * localT)
                     + 0.50 * Math.Sin(TW * 2 * freq * localT)
                     + 0.22 * Math.Sin(TW * 3 * freq * localT)
                     + 0.08 * Math.Sin(TW * 4 * freq * localT);
            return s * env;
        }

        // Bass：正弦 + 二次諧波
        private static double BassNote(double freq, double localT, double dur)
        {
            if (localT < 0 || localT > dur) return 0;
            double env = PadEnv(localT, dur, 0.008, 0.15, 0.6, 0.12);
            return (Math.Sin(TW * freq * localT) + 0.3 * Math.Sin(TW * 2 * freq * localT)) * env;
        }

        // 大鼓：頻率掃描 150→55 Hz + 指數衰減
        private static double Kick(double localT)
        {
            if (localT < 0 || localT > 0.32) return 0;
            double freq = 150 * Math.Exp(-localT * 14) + 55;
            double env  = Math.Exp(-localT * 9);
            return Math.Sin(TW * freq * localT) * env;
        }

        // 小鼓：雜音 + 短音調
        private static double Snare(double localT, Random rng)
        {
            if (localT < 0 || localT > 0.18) return 0;
            double env   = Math.Exp(-localT * 22);
            double noise = rng.NextDouble() * 2 - 1;
            double tone  = Math.Sin(TW * 185 * localT);
            return (0.65 * noise + 0.35 * tone) * env;
        }

        // Hi-hat：高頻雜音短爆音
        private static double HiHat(double localT, Random rng)
        {
            if (localT < 0 || localT > 0.055) return 0;
            return (rng.NextDouble() * 2 - 1) * Math.Exp(-localT * 70);
        }

        // ADSR 包絡
        private static double PadEnv(double t, double dur,
            double atk, double dec, double sus, double rel)
        {
            if (t < atk)              return t / atk;
            if (t < atk + dec)        return 1.0 - (1.0 - sus) * (t - atk) / dec;
            if (t < dur - rel)        return sus;
            double r = (dur - t) / rel;
            return r < 0 ? 0 : sus * r;
        }

        private const double TW = 2 * Math.PI;

        // ── WAV 建構 ────────────────────────────────────────────────

        private static MemoryStream BuildWav(short[] pcm, int sr)
        {
            int dataBytes = pcm.Length * 2;
            MemoryStream ms = new MemoryStream(44 + dataBytes);
            BinaryWriter w  = new BinaryWriter(ms);

            w.Write(new byte[] { (byte)'R',(byte)'I',(byte)'F',(byte)'F' });
            w.Write(36 + dataBytes);
            w.Write(new byte[] { (byte)'W',(byte)'A',(byte)'V',(byte)'E' });
            w.Write(new byte[] { (byte)'f',(byte)'m',(byte)'t',(byte)' ' });
            w.Write(16);  w.Write((short)1);  w.Write((short)1);
            w.Write(sr);  w.Write(sr * 2);    w.Write((short)2);  w.Write((short)16);
            w.Write(new byte[] { (byte)'d',(byte)'a',(byte)'t',(byte)'a' });
            w.Write(dataBytes);
            foreach (short s in pcm) w.Write(s);

            ms.Position = 0;
            return ms;
        }
    }
}
