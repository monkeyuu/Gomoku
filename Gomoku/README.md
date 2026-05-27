# 五子棋 Gomoku — Windows Programming (II) HW2

使用 C# WinForms (.NET Framework) 實作的五子棋遊戲，具備 AI 對手、音效與木質棋盤介面。

---

## 📸 遊戲截圖

> 執行後截圖填於此處。

---

## 🎮 玩法說明

| 操作 | 方法 |
|------|------|
| 落子 | 棋盤交叉點**滑鼠左鍵**點擊 |
| 新遊戲 | 點「🔄 新遊戲」或按鍵盤 **N** |
| 悔棋 | 點「↩ 悔棋」或按鍵盤 **U** |
| 切換模式 | 點「🤖 切換對戰模式」— 人機 / 雙人 |

勝利條件：任一方在橫、直、斜方向連成 **五子** 即獲勝。

---

## 🚀 如何在 Visual Studio 建立專案

1. 開啟 Visual Studio → 新增專案
2. 選 **Windows Forms App (.NET Framework)**，名稱填 `Gomoku`
3. 刪除預設的 `Form1.cs`（右鍵 → 刪除）
4. 將以下 .cs 檔加入專案（專案右鍵 → 加入 → 現有項目）：
   - `Program.cs`
   - `GomokuGame.cs`
   - `GomokuAI.cs`
   - `SoundManager.cs`
   - `MainForm.cs`
5. 按 **F5** 執行

---

## ✨ 功能特色

- 精緻木質棋盤（漸層木紋 + 座標標示 + 天元星位）
- 高畫質棋子（漸層 + 光澤高光 + 陰影）
- 最後落子紅點標記、勝利五子紅線高亮
- AI 對手：威脅評分啟發式演算法，攻守平衡
- 自動合成 PCM WAV 音效（不需外部音訊檔案）
- 遊戲計時器 + 步數計數
- 悔棋（人機模式一次退回兩步）

---

## 📁 專案結構

```
Gomoku/
├── Program.cs        # 程式進入點
├── GomokuGame.cs     # 遊戲邏輯（棋盤、落子、五連判定）
├── GomokuAI.cs       # AI 對手（啟發式威脅評分）
├── SoundManager.cs   # 音效（動態合成 WAV）
├── MainForm.cs       # 主介面（GDI+ 繪圖 + 事件）
├── .gitignore
└── README.md
```

---

## 📚 參考資料

- [五子棋規則 — Wikipedia](https://zh.wikipedia.org/wiki/五子棋)
- [WinForms GDI+ 繪圖文件](https://learn.microsoft.com/zh-tw/dotnet/desktop/winforms/advanced/graphics-and-drawing-in-windows-forms)
