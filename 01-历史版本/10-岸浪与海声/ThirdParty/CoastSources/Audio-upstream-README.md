# Audio assets

7 個循環音效，來源 CC0 / Public Domain。

統一處理：
mp3、128 kbps CBR、stereo、44.1 kHz、60–120 秒、首尾用 **2 秒 qsin（constant-power）crossfade self-loop**——
HTML audio `loop` 接到時尾巴與開頭內容相同，無接縫呼吸感。

| 檔案 | 來源 | 授權 | 處理 |
|---|---|---|---|
| ocean.mp3 | [Freesound 156598 — Rmutt: oceanwaves-10 (Point Reyes loop)](https://freesound.org/s/156598/) | CC0 1.0 | offset 2s, 92s 切窗 → 90s |
| rain.mp3 | [Freesound 81818 — Silencyo: Rain on Window Reverberant](https://freesound.org/s/81818/) | CC0 1.0 | offset 1s, 80s 切窗 → 78s |
| fireplace.mp3 | [archive.org Red_Library_Fire — R30-09 Fires Burns Wood and Metal Crackling](https://archive.org/details/Red_Library_Fire) | CC0 1.0 | offset 5s, 68s 切窗 → 66s |
| wind.mp3 | [archive.org GOLD_TAPE_55_56_Weather-Wind — G55-02 Chill Wind](https://archive.org/details/GOLD_TAPE_55_56_Weather-Wind) | CC0 1.0 | offset 2s, 92s 切窗 → 90s |
| birds.mp3 | [Freesound 578523 — SamsterBirdies: Birds singing, Dawn chorus](https://freesound.org/s/578523/) | CC0 1.0 | offset 30s, 92s 切窗 → 90s |
| stream.mp3 | [archive.org GOLD_TAPE_53_54_Water — G53-19a Brook or Creek](https://archive.org/details/GOLD_TAPE_53_54_Water) | CC0 1.0 | offset 5s, 68s 切窗 → 66s |
| thunder.mp3 | [archive.org 1HourThunderstorm](https://archive.org/details/1HourThunderstorm) | Public Domain | offset 1660s, 92s 切窗 → 90s（含 5 個雷聲尖峰）|

## 替換時的處理流程（ffmpeg）

```bash
# 從來源 src.mp3 切 D 秒（從 OFFSET 開始）+ 加 2s qsin self-loop crossfade
XF=2
ffmpeg -ss OFFSET -i src.mp3 -t D \
  -filter_complex "
    [0:a]asplit=3[s1][s2][s3];
    [s1]atrim=0:${XF},asetpts=PTS-STARTPTS[head];
    [s2]atrim=${XF}:$((D-XF)),asetpts=PTS-STARTPTS[mid];
    [s3]atrim=$((D-XF)):D,asetpts=PTS-STARTPTS[tail];
    [tail][head]acrossfade=d=${XF}:c1=qsin:c2=qsin[xfade];
    [xfade][mid]concat=n=2:v=0:a=1[out]
  " \
  -map "[out]" -ar 44100 -ac 2 -c:a libmp3lame -b:a 128k -map_metadata -1 out.mp3
# 結果長度 = D - XF，必須 >= 60s
```

檔名保持不變：`ocean.mp3 / rain.mp3 / fireplace.mp3 / wind.mp3 / birds.mp3 / stream.mp3 / thunder.mp3`。

## 注意事項

- **白噪音不在這裡**——`builtinSounds.ts` 把 white 標為 `type: 'synth'`，由 Tone.js `new Noise('white')` 即時合成
- archive.org 來源中：wind / stream 屬 USC GOLD_TAPE 系列，錄音前常有 **slate 人聲**（錄音師念片名 + take 號），offset 已避開；其餘檔案的 offset 是為了挑乾淨段落或事件密集的視窗
- thunder 是 1 小時雷雨錄音的中段，1660s 起點刻意挑了 5 個雷聲尖峰均勻分布的 92 秒視窗
