using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace TamaPoke.Utils.Service
{
    public static class Tpk2Decoder
    {
        // 🌟 targetActionId 매개변수 추가 (기본값 0: Idle)
        public static BitmapSource[]? LoadAnimation(string filePath, int targetActionId = 0)
        {
            if (!File.Exists(filePath)) return null;

            byte[] blob = File.ReadAllBytes(filePath);

            // 1. 헤더 검증 ("TPK2")[cite: 2]
            if (blob.Length < 7 || blob[0] != 'T' || blob[1] != 'P' || blob[2] != 'K' || blob[3] != '2')
                return null;

            int nActs = blob[4];
            int palCount = BitConverter.ToUInt16(blob, 5);

            // 2. 팔레트 읽기 및 변환 (RGB565 -> ARGB)[cite: 2, 3]
            List<Color> colors = new List<Color>();
            int offset = 7;
            for (int i = 0; i < palCount; i++)
            {
                ushort rgb565 = BitConverter.ToUInt16(blob, offset);
                offset += 2;

                byte r = (byte)((rgb565 >> 11 & 0x1F) * 255 / 31);
                byte g = (byte)((rgb565 >> 5 & 0x3F) * 255 / 63);
                byte b = (byte)((rgb565 & 0x1F) * 255 / 31);
                colors.Add(Color.FromArgb(255, r, g, b));
            }

            // 투명 픽셀 처리[cite: 3]
            while (colors.Count < 256) colors.Add(Colors.Transparent);
            colors[255] = Colors.Transparent;
            BitmapPalette palette = new BitmapPalette(colors);

            // 3. 액션 데이터 파싱[cite: 2]
            int currentPointer = 7 + palCount * 2;

            for (int i = 0; i < nActs; i++)
            {
                if (currentPointer + 4 > blob.Length) break;

                int id = blob[currentPointer++];
                int w = blob[currentPointer++];
                int h = blob[currentPointer++];
                int nf = blob[currentPointer++];

                // 🌟 요청한 애니메이션 ID와 일치할 때만 이미지를 추출합니다.
                if (id == targetActionId)
                {
                    // ms 데이터 건너뛰기[cite: 2]
                    currentPointer += nf * 2;

                    int bytesPerFrame = w * h;
                    BitmapSource[] frames = new BitmapSource[nf];

                    for (int f = 0; f < nf; f++)
                    {
                        byte[] pixels = new byte[bytesPerFrame];
                        Array.Copy(blob, currentPointer, pixels, 0, bytesPerFrame);
                        currentPointer += bytesPerFrame;

                        frames[f] = BitmapSource.Create(
                            w, h, 96, 96, PixelFormats.Indexed8, palette, pixels, w);
                    }
                    return frames;
                }
                else
                {
                    // 🌟 찾는 ID가 아니면, 해당 액션의 데이터(ms + 픽셀) 길이만큼 포인터를 건너뜁니다[cite: 2].
                    currentPointer += nf * 2 + w * h * nf;
                }
            }
            return null; // 원하는 액션을 찾지 못한 경우
        }
    }
}