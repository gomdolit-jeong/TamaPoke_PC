using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Media.Imaging;
using TamaPoke.Models;

namespace TamaPoke.Utils
{
    // 🌟 이미지와 정확한 크기 정보를 함께 담아 전달하는 전용 클래스
    public class SpriteFrameData
    {
        public BitmapSource Image { get; set; } = null!;
        public int FrameWidth { get; set; }
        public int FrameHeight { get; set; }
    }

    public static class BinSpriteReader
    {
        /// <summary>
        /// 도감 및 파티 화면용 리더기: Idle 모션을 찾고, 없다면 첫 번째 유효한 프레임(예: Walk)을 반환합니다.
        /// </summary>
        public static BitmapSource? LoadPokedexThumbnail(string binFilePath)
        {
            if (!File.Exists(binFilePath)) return null;

            try
            {
                using (BinaryReader reader = new BinaryReader(File.Open(binFilePath, FileMode.Open, FileAccess.Read)))
                {
                    int totalStrips = reader.ReadInt32();
                    if (totalStrips <= 0) return null;

                    int targetIndex = PokemonState.ANIM_IDLE; // 기본 목표: Idle (7번)

                    // 🌟 예외 처리를 위한 대안(Fallback) 저장용 변수
                    long fallbackPosition = -1;
                    int fallbackFw = 0, fallbackFh = 0, fallbackLength = 0;

                    for (int i = 0; i < totalStrips; i++)
                    {
                        int fw = reader.ReadInt32();
                        int fh = reader.ReadInt32();
                        int byteLength = reader.ReadInt32();

                        if (byteLength == 0) continue; // 데이터가 없는 결번 프레임은 무시

                        // 🌟 유효한 첫 번째 프레임의 위치와 데이터를 기억해 둡니다 (대비용)
                        if (fallbackPosition == -1)
                        {
                            fallbackPosition = reader.BaseStream.Position;
                            fallbackFw = fw;
                            fallbackFh = fh;
                            fallbackLength = byteLength;
                        }

                        if (i == targetIndex)
                        {
                            // 1. Idle 모션을 찾았다면 즉시 잘라서 반환!
                            byte[] pngBytes = reader.ReadBytes(byteLength);
                            return CreateCroppedThumbnail(pngBytes, fw, fh);
                        }
                        else
                        {
                            // 2. Idle이 아니면 이미지를 읽지 않고 빠르게 다음으로 건너뛰기
                            reader.BaseStream.Seek(byteLength, SeekOrigin.Current);
                        }
                    }

                    // 🌟 3. 루프를 다 돌았는데 Idle 모션이 없었다면? 
                    // 아까 기억해둔 첫 번째 유효 모션(예: Walk 등)으로 돌아가서 썸네일로 사용합니다!
                    if (fallbackPosition != -1)
                    {
                        reader.BaseStream.Seek(fallbackPosition, SeekOrigin.Begin);
                        byte[] pngBytes = reader.ReadBytes(fallbackLength);
                        return CreateCroppedThumbnail(pngBytes, fallbackFw, fallbackFh);
                    }
                }
            }
            catch { }
            return null;
        }

        // 🌟 중복 코드를 줄이기 위한 내부 자르기 헬퍼 함수
        private static BitmapSource? CreateCroppedThumbnail(byte[] pngBytes, int fw, int fh)
        {
            using (MemoryStream ms = new MemoryStream(pngBytes))
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = ms;
                bitmap.EndInit();
                bitmap.Freeze();

                if (fw > 0 && fh > 0 && bitmap.PixelWidth >= fw && bitmap.PixelHeight >= fh)
                {
                    var cropRect = new System.Windows.Int32Rect(0, 0, fw, fh);
                    var cropped = new System.Windows.Media.Imaging.CroppedBitmap(bitmap, cropRect);
                    cropped.Freeze();
                    return cropped;
                }
                return bitmap;
            }
        }

        /// <summary>
        /// 메인 애니메이션용 리더기: 이미지와 크기 정보를 리스트로 넘겨줍니다.
        /// </summary>
        public static List<SpriteFrameData> LoadFramesFromBin(string binFilePath)
        {
            List<SpriteFrameData> frames = new List<SpriteFrameData>();
            if (!File.Exists(binFilePath)) return frames;

            try
            {
                using (BinaryReader reader = new BinaryReader(File.Open(binFilePath, FileMode.Open, FileAccess.Read)))
                {
                    int totalStrips = reader.ReadInt32();
                    if (totalStrips <= 0) return frames;

                    for (int i = 0; i < totalStrips; i++)
                    {
                        int fw = reader.ReadInt32();
                        int fh = reader.ReadInt32();
                        int byteLength = reader.ReadInt32();

                        if (byteLength == 0)
                        {
                            frames.Add(null!);
                            continue;
                        }

                        byte[] pngBytes = reader.ReadBytes(byteLength);

                        using (MemoryStream ms = new MemoryStream(pngBytes))
                        {
                            BitmapImage bitmap = new BitmapImage();
                            bitmap.BeginInit();
                            bitmap.CacheOption = BitmapCacheOption.OnLoad;
                            bitmap.StreamSource = ms;
                            bitmap.EndInit();
                            bitmap.Freeze();

                            frames.Add(new SpriteFrameData { Image = bitmap, FrameWidth = fw, FrameHeight = fh });
                        }
                    }
                }
            }
            catch { }

            return frames;
        }
    }
}