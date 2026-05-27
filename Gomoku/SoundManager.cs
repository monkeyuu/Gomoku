using System;
using System.Threading;

namespace Gomoku
{
    /// <summary>
    /// 使用 Windows Kernel Beep（非 Wave Audio），不干擾 SoundPlayer 背景音樂。
    /// Console.Beep() 走 kernel32.Beep()，與 PlaySound/SoundPlayer 完全不同的音訊路徑。
    /// </summary>
    public static class SoundManager
    {
        // 落子音效：A4 / C5
        public static void PlayBlack()   => Fire(() => Console.Beep(440, 65));
        public static void PlayWhite()   => Fire(() => Console.Beep(523, 65));

        // 悔棋
        public static void PlayUndo()    => Fire(() => Console.Beep(330, 95));

        // 新遊戲
        public static void PlayNewGame() => Fire(() => Console.Beep(660, 80));

        // 勝利：C5-E5-G5 上行琶音
        public static void PlayWin()     => Fire(() =>
        {
            Console.Beep(523, 110);
            Console.Beep(659, 110);
            Console.Beep(784, 160);
        });

        /// <summary>在背景執行緒執行，不阻塞 UI，也不影響 Wave Audio 播放。</summary>
        private static void Fire(Action a)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try { a(); }
                catch { /* 無 PC 喇叭或不支援時靜默略過 */ }
            });
        }
    }
}
