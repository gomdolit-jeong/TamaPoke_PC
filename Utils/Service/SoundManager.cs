using System;
using System.IO;
using System.Media;
using System.Threading.Tasks;

namespace TamaPoke.Utils.Service
{
    public static class SoundManager
    {
        // 🌟 오리지널 C++ audio.cpp의 주파수(Hz)와 시간(ms) 배열[cite: 10]
        public static readonly (int f, int ms)[] N_TAP = { (880, 35) };
        public static readonly (int f, int ms)[] N_EAT = { (660, 45), (0, 12), (660, 45) };
        public static readonly (int f, int ms)[] N_PLAY = { (784, 45), (988, 60) };
        public static readonly (int f, int ms)[] N_HEART = { (1047, 55), (1319, 90) };
        public static readonly (int f, int ms)[] N_HATCH = { (523, 80), (659, 80), (784, 110), (1047, 170) };
        public static readonly (int f, int ms)[] N_EVOLVE = { (523, 80), (659, 80), (784, 80), (1047, 90), (1319, 230) };
        public static readonly (int f, int ms)[] N_MEDAL = { (784, 70), (0, 25), (784, 70), (0, 25), (1047, 200) };
        public static readonly (int f, int ms)[] N_DENY = { (300, 110), (200, 170) };
        public static readonly (int f, int ms)[] N_BYE = { (784, 150), (659, 150), (523, 280) };
        public static readonly (int f, int ms)[] N_LEVEL = { (784, 70), (1047, 130) };

        /// <summary>
        /// 지정된 음표들을 바탕으로 메모리에서 WAV 오디오 데이터를 실시간으로 합성하여 재생합니다.
        /// </summary>
        public static void Play(params (int f, int ms)[] notes)
        {
            // UI가 멈추지 않도록 백그라운드에서 오디오를 합성하고 재생합니다.
            Task.Run(() =>
            {
                try
                {
                    byte[] wavData = GenerateSquareWave(notes);

                    using (MemoryStream ms = new MemoryStream(wavData))
                    using (SoundPlayer player = new SoundPlayer(ms))
                    {
                        player.PlaySync(); // 백그라운드 스레드이므로 Sync로 끝까지 재생되도록 보장
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[사운드 오류] 재생 실패: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// 주파수와 시간 데이터를 실제 WAV 파일 포맷(Byte Array)으로 변환합니다.
        /// </summary>
        private static byte[] GenerateSquareWave((int f, int ms)[] notes)
        {
            int sampleRate = 44100; // CD 음질 샘플레이트
            short amplitude = 8000; // 볼륨 크기 (0 ~ 32767)

            using (MemoryStream ms = new MemoryStream())
            using (BinaryWriter bw = new BinaryWriter(ms))
            {
                // WAV 헤더 공간 44바이트 비워두기
                bw.Write(new byte[44]);

                int dataLength = 0;

                foreach (var note in notes)
                {
                    int totalSamples = (int)(note.ms / 1000.0 * sampleRate);

                    if (note.f == 0)
                    {
                        // 주파수가 0이면 무음(0) 기록[cite: 10]
                        for (int i = 0; i < totalSamples; i++)
                        {
                            bw.Write((short)0);
                            dataLength += 2; // 16-bit = 2 bytes
                        }
                    }
                    else
                    {
                        // 사각파(Square Wave) 주기 계산[cite: 10]
                        int halfPeriod = sampleRate / note.f / 2;

                        for (int i = 0; i < totalSamples; i++)
                        {
                            // 절반은 양수, 절반은 음수로 진동하여 8비트 레트로 소리를 만듭니다.
                            short s = i / halfPeriod % 2 == 0 ? amplitude : (short)-amplitude;

                            // 🌟 오리지널 C++ 소스의 안티 클릭(Anti-click) 부드러운 페이드 효과 적용[cite: 10]
                            if (i < 64)
                                s = (short)(s * i / 64);
                            else if (i > totalSamples - 96)
                                s = (short)(s * (totalSamples - i) / 96);

                            bw.Write(s);
                            dataLength += 2;
                        }
                    }
                }

                // 파일 맨 앞으로 돌아가서 표준 WAV 헤더 정보 작성
                bw.Seek(0, SeekOrigin.Begin);
                bw.Write("RIFF".ToCharArray());
                bw.Write(36 + dataLength);        // 파일 전체 크기
                bw.Write("WAVE".ToCharArray());
                bw.Write("fmt ".ToCharArray());
                bw.Write(16);                     // 포맷 청크 크기
                bw.Write((short)1);               // 포맷 타입 (1 = PCM)
                bw.Write((short)1);               // 채널 수 (1 = Mono)
                bw.Write(sampleRate);             // 샘플 레이트
                bw.Write(sampleRate * 2);         // 바이트 레이트
                bw.Write((short)2);               // 블록 얼라인
                bw.Write((short)16);              // 샘플당 비트 수
                bw.Write("data".ToCharArray());
                bw.Write(dataLength);             // 데이터 크기

                return ms.ToArray();
            }
        }
    }
}