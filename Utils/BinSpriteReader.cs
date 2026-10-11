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

        // 프레임별 재생 시간 (단위: 1/60초, AnimData.xml의 Durations). 구버전 bin은 빈 배열입니다.
        public int[] Durations { get; set; } = Array.Empty<int>();
    }

    public static class BinSpriteReader
    {
        // v2 bin 파일은 맨 앞에 이 값을 기록합니다. (구버전은 첫 값이 양수인 스트립 수)
        public const int FORMAT_V2_MARKER = -2;

        // 스트립 헤더 정보 (PNG 데이터 직전까지 읽은 상태)
        private readonly struct StripHeader
        {
            public StripHeader(int fw, int fh, int[] durations, int byteLength)
            {
                FrameWidth = fw; FrameHeight = fh; Durations = durations; ByteLength = byteLength;
            }
            public int FrameWidth { get; }
            public int FrameHeight { get; }
            public int[] Durations { get; }
            public int ByteLength { get; }
        }

        // 파일 맨 앞을 읽어 (스트립 수, v2 여부)를 반환합니다.
        private static (int TotalStrips, bool IsV2) ReadFileHeader(BinaryReader reader)
        {
            int first = reader.ReadInt32();
            if (first == FORMAT_V2_MARKER) return (reader.ReadInt32(), true);
            return (first, false);
        }

        private static StripHeader ReadStripHeader(BinaryReader reader, bool isV2)
        {
            int fw = reader.ReadInt32();
            int fh = reader.ReadInt32();

            int[] durations = Array.Empty<int>();
            if (isV2)
            {
                int count = reader.ReadInt32();
                durations = new int[count];
                for (int d = 0; d < count; d++) durations[d] = reader.ReadInt32();
            }

            int byteLength = reader.ReadInt32();
            return new StripHeader(fw, fh, durations, byteLength);
        }

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
                    var (totalStrips, isV2) = ReadFileHeader(reader);
                    if (totalStrips <= 0) return null;

                    int targetIndex = PokemonState.ANIM_IDLE; // 기본 목표: Idle (7번)

                    // 🌟 예외 처리를 위한 대안(Fallback) 저장용 변수
                    long fallbackPosition = -1;
                    int fallbackFw = 0, fallbackFh = 0, fallbackLength = 0;

                    for (int i = 0; i < totalStrips; i++)
                    {
                        var header = ReadStripHeader(reader, isV2);
                        if (header.ByteLength == 0) continue; // 데이터가 없는 결번 프레임은 무시

                        // 🌟 유효한 첫 번째 프레임의 위치와 데이터를 기억해 둡니다 (대비용)
                        if (fallbackPosition == -1)
                        {
                            fallbackPosition = reader.BaseStream.Position;
                            fallbackFw = header.FrameWidth;
                            fallbackFh = header.FrameHeight;
                            fallbackLength = header.ByteLength;
                        }

                        if (i == targetIndex)
                        {
                            // 1. Idle 모션을 찾았다면 즉시 잘라서 반환!
                            byte[] pngBytes = reader.ReadBytes(header.ByteLength);
                            return CreateCroppedThumbnail(pngBytes, header.FrameWidth, header.FrameHeight);
                        }

                        // 2. Idle이 아니면 이미지를 읽지 않고 빠르게 다음으로 건너뛰기
                        reader.BaseStream.Seek(header.ByteLength, SeekOrigin.Current);
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
            BitmapImage bitmap = DecodePng(pngBytes);

            if (fw > 0 && fh > 0 && bitmap.PixelWidth >= fw && bitmap.PixelHeight >= fh)
            {
                var cropRect = new System.Windows.Int32Rect(0, 0, fw, fh);
                var cropped = new CroppedBitmap(bitmap, cropRect);
                cropped.Freeze();
                return cropped;
            }
            return bitmap;
        }

        private static BitmapImage DecodePng(byte[] pngBytes)
        {
            using (MemoryStream ms = new MemoryStream(pngBytes))
            {
                BitmapImage bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.StreamSource = ms;
                bitmap.EndInit();
                bitmap.Freeze();
                return bitmap;
            }
        }

        // 모션/방향이 바뀔 때마다 .bin 전체를 다시 디코딩하지 않도록 최근 파일 몇 개를 캐시합니다.
        private const int FRAME_CACHE_SIZE = 8;
        private static readonly LinkedList<(string Path, List<SpriteFrameData> Frames)> _frameCache = new();
        private static readonly object _cacheLock = new();

        /// <summary>
        /// 메인 애니메이션용 리더기: 이미지와 크기 정보를 리스트로 넘겨줍니다.
        /// </summary>
        public static List<SpriteFrameData> LoadFramesFromBin(string binFilePath)
        {
            lock (_cacheLock)
            {
                for (var node = _frameCache.First; node != null; node = node.Next)
                {
                    if (string.Equals(node.Value.Path, binFilePath, StringComparison.OrdinalIgnoreCase))
                    {
                        _frameCache.Remove(node);
                        _frameCache.AddFirst(node);
                        return node.Value.Frames;
                    }
                }
            }

            var frames = ReadFramesFromBin(binFilePath);
            if (frames.Count > 0)
            {
                lock (_cacheLock)
                {
                    _frameCache.AddFirst((binFilePath, frames));
                    if (_frameCache.Count > FRAME_CACHE_SIZE) _frameCache.RemoveLast();
                }
            }
            return frames;
        }

        // 스프라이트 업데이트로 bin 파일이 새로 만들어지면 캐시를 비워야 합니다.
        public static void ClearCache()
        {
            lock (_cacheLock) _frameCache.Clear();
        }

        // 모든 스트립 중 가장 큰 프레임 크기 (산책 창 크기 계산용)
        public static (int Width, int Height) GetMaxFrameSize(string binFilePath)
        {
            int w = 0, h = 0;
            foreach (var f in LoadFramesFromBin(binFilePath))
            {
                if (f == null) continue;
                w = Math.Max(w, f.FrameWidth);
                h = Math.Max(h, f.FrameHeight);
            }
            return (w, h);
        }

        private static List<SpriteFrameData> ReadFramesFromBin(string binFilePath)
        {
            List<SpriteFrameData> frames = new List<SpriteFrameData>();
            if (!File.Exists(binFilePath)) return frames;

            try
            {
                using (BinaryReader reader = new BinaryReader(File.Open(binFilePath, FileMode.Open, FileAccess.Read)))
                {
                    var (totalStrips, isV2) = ReadFileHeader(reader);
                    if (totalStrips <= 0) return frames;

                    for (int i = 0; i < totalStrips; i++)
                    {
                        var header = ReadStripHeader(reader, isV2);

                        if (header.ByteLength == 0)
                        {
                            frames.Add(null!);
                            continue;
                        }

                        byte[] pngBytes = reader.ReadBytes(header.ByteLength);
                        frames.Add(new SpriteFrameData
                        {
                            Image = DecodePng(pngBytes),
                            FrameWidth = header.FrameWidth,
                            FrameHeight = header.FrameHeight,
                            Durations = header.Durations
                        });
                    }
                }
            }
            catch { }

            return frames;
        }
    }
}
