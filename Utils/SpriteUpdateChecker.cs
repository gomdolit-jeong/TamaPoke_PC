using System.IO;
using System.Net.Http;
using System.Text.Json;

namespace TamaPoke.Utils
{
    // 스프라이트 원본 레포에 새 이미지가 올라왔는지 확인합니다.
    // 레포가 거의 매일 바뀌고 전체 다운로드가 크므로, 확인은 하루 한 번, 안내는 한 달에 한 번까지만 합니다.
    public static class SpriteUpdateChecker
    {
        public class State
        {
            public string? Sha { get; set; }                 // 지금 받아 둔 이미지의 원본 커밋
            public DateTime? LastCheckUtc { get; set; }
            public DateTime? LastNotifiedUtc { get; set; }
        }

        private const string LatestCommitUrl = "https://api.github.com/repos/PMDCollab/SpriteCollab/commits?sha=master&path=sprite&per_page=1";
        private static readonly TimeSpan CheckInterval = TimeSpan.FromHours(24);
        private static readonly TimeSpan NotifyInterval = TimeSpan.FromDays(30);

        private static readonly HttpClient client = CreateClient();

        private static HttpClient CreateClient()
        {
            var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("TamaPoke"); // GitHub API는 User-Agent가 없으면 거절합니다.
            return http;
        }

        // 스프라이트 폴더에 저장합니다. (Data 폴더는 릴리즈 zip으로 덮어써지므로 쓰지 않습니다)
        private static string StatePath => Path.Combine(AppPaths.SpriteFolder, "sprite_version.json");

        public static State LoadState()
        {
            try
            {
                if (File.Exists(StatePath)) return JsonSerializer.Deserialize<State>(File.ReadAllText(StatePath)) ?? new State();
            }
            catch { }
            return new State();
        }

        private static void SaveState(State state)
        {
            try { File.WriteAllText(StatePath, JsonSerializer.Serialize(state)); }
            catch { }
        }

        // sprite 폴더를 마지막으로 바꾼 커밋 (실패하면 null)
        public static async Task<string?> GetLatestShaAsync()
        {
            try
            {
                return ParseLatestSha(await client.GetStringAsync(LatestCommitUrl));
            }
            catch
            {
                return null;
            }
        }

        // GitHub 커밋 목록 응답에서 첫 번째(최신) 커밋 번호를 꺼냅니다.
        public static string? ParseLatestSha(string json)
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement[0].GetProperty("sha").GetString();
        }

        // 인터넷 확인이 필요한 때인지 (마지막 확인 뒤 하루가 지났는지)
        public static bool IsCheckDue(State state, DateTime nowUtc) =>
            state.LastCheckUtc == null || nowUtc - state.LastCheckUtc.Value >= CheckInterval;

        // 확인 결과를 상태에 반영하고, 안내해야 하면 true를 돌려줍니다.
        // 기록이 없으면(이 기능 이전에 받은 이미지) 언제 받았는지 모르므로 지금을 기준으로 삼고 안내하지 않습니다.
        public static bool ApplyCheckResult(State state, string latestSha, DateTime nowUtc)
        {
            state.LastCheckUtc = nowUtc;
            if (string.IsNullOrEmpty(state.Sha))
            {
                state.Sha = latestSha;
                return false;
            }

            if (state.Sha == latestSha) return false;
            if (state.LastNotifiedUtc != null && nowUtc - state.LastNotifiedUtc.Value < NotifyInterval) return false;

            state.LastNotifiedUtc = nowUtc;
            return true;
        }

        // 새 이미지가 있어서 안내해야 하면 true (인터넷이 안 되면 다음에 다시 확인합니다)
        public static async Task<bool> CheckForUpdateAsync()
        {
            var state = LoadState();
            if (!IsCheckDue(state, DateTime.UtcNow)) return false;

            string? latestSha = await GetLatestShaAsync();
            if (latestSha == null) return false;

            bool notify = ApplyCheckResult(state, latestSha, DateTime.UtcNow);
            SaveState(state);
            return notify;
        }

        // 스프라이트를 새로 받은 뒤 기록합니다. (latestSha: 다운로드 직전에 확인한 커밋)
        public static void MarkInstalled(string? latestSha)
        {
            var state = LoadState();
            state.Sha = latestSha;
            state.LastCheckUtc = latestSha != null ? DateTime.UtcNow : null;
            state.LastNotifiedUtc = null;
            SaveState(state);
        }
    }
}
