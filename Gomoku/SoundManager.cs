using System;
using System.Collections.Generic;
using System.IO;
using System.Media;

namespace Gomoku
{
    /// <summary>
    /// 在程式啟動時動態合成 PCM WAV 音效，不需要任何外部音訊檔案。
    /// </summary>
    public static class SoundManager
    {
        // Keep MemoryStream references alive so SoundPlayer can read during Play()
        private static readonly List<MemoryStream> _streams = new List<MemoryStream>();

        private static readonly SoundPlayer _black   = Build(440, 80,  0.45);  // A4 落子聲
        private static readonly SoundPlayer _white   = Build(523, 80,  0.45);  // C5 落子聲
        private static readonly SoundPlayer _undo    = Build(330, 140, 0.35);  // E4 悔棋聲
        private static readonly SoundPlayer _newGame = Build(660, 120, 0.40);  // E5 新局聲
        private static readonly SoundPlayer _win     = BuildWin();             // 勝利琶音

        // ── Public API ─────────────────────────────────────────────

        public static void PlayBlack()   { try { _black?.Play();   } catch { } }
        public static void PlayWhite()   { try { _white?.Play();   } catch { } }
        public static void PlayUndo()    { try { _undo?.Play();    } catch { } }
        public static void PlayNewGame() { try { _newGame?.Play(); } catch { } }
        public static void PlayWin()     { try { _win?.Play();     } catch { } }

        // ── Builders ───────────────────────────────────────────────

        private static SoundPlayer Build(int freq, int ms, double vol)
        {
            try
            {
                MemoryStream stream = WavStream(SingleTone(freq, ms, vol));
                _streams.Add(stream);
                SoundPlayer p = new SoundPlayer(stream);
                p.Load();
                return p;
            }
            catch { return null; }
        }

        private static SoundPlayer BuildWin()
        {
            // C5 → E5 → G5 上行琶音
            int[] freqs = { 523, 659, 784 };
            try
            {
                int segLen = 44100 * 180 / 1000;
                short[] pcm = new short[segLen * freqs.Length];
                for (int i = 0; i < freqs.Length; i++)
                {
                    short[] seg = SingleTone(freqs[i], 180, 0.45);
                    Array.Copy(seg, 0, pcm, i * segLen, segLen);
                }
                MemoryStream stream = WavStream(pcm);
                _streams.Add(stream);
                SoundPlayer p = new SoundPlayer(stream);
                p.Load();
                return p;
            }
            catch { return null; }
        }

        // ── PCM generation ─────────────────────────────────────────

        private const int SampleRate = 44100;

        private static short[] SingleTone(int freq, int durationMs, double volume)
        {
            int n = SampleRate * durationMs / 1000;
            short[] pcm = new short[n];
            for (int i = 0; i < n; i++)
            {
                double t   = (double)i / SampleRate;
                double env = Math.Pow(1.0 - (double)i / n, 0.4);
                double v   = Math.Sin(2 * Math.PI * freq * t) * env * volume * short.MaxValue;
                // .NET Framework 沒有 Math.Clamp，用 Math.Max / Math.Min 代替
                int vi = (int)v;
                if (vi < short.MinValue) vi = short.MinValue;
                if (vi > short.MaxValue) vi = short.MaxValue;
                pcm[i] = (short)vi;
            }
            return pcm;
        }

        private static MemoryStream WavStream(short[] pcm)
        {
            int dataBytes = pcm.Length * 2;
            MemoryStream ms = new MemoryStream(44 + dataBytes);
            BinaryWriter w  = new BinaryWriter(ms);

            // RIFF 標頭
            w.Write(new byte[] { (byte)'R', (byte)'I', (byte)'F', (byte)'F' });
            w.Write(36 + dataBytes);
            w.Write(new byte[] { (byte)'W', (byte)'A', (byte)'V', (byte)'E' });
            // fmt chunk
            w.Write(new byte[] { (byte)'f', (byte)'m', (byte)'t', (byte)' ' });
            w.Write(16);
            w.Write((short)1);   // PCM
            w.Write((short)1);   // 單聲道
            w.Write(SampleRate);
            w.Write(SampleRate * 2);
            w.Write((short)2);
            w.Write((short)16);
            // data chunk
            w.Write(new byte[] { (byte)'d', (byte)'a', (byte)'t', (byte)'a' });
            w.Write(dataBytes);
            foreach (short s in pcm) w.Write(s);

            ms.Position = 0;
            return ms;
        }
    }
}
