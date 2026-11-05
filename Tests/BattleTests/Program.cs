// 전투 규칙 회귀 테스트. 실행 방법은 BattleTests.csproj 참고
using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using TamaPoke.Models;
using System.Linq;
using TamaPoke.Utils;

static class T
{
    const BindingFlags F = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    static int fails = 0;
    static PokemonState New(int me, int foe)
    {
        var p = new PokemonState { SpeciesId = me, AgeMinutes = 35 * 60 };
        p.EnemySpeciesId = foe; p.EnemyLevel = 35;
        p.IsBattleOpen = true; p.PlayerMaxHp = 500; p.PlayerHp = 500; p.EnemyMaxHp = 500; p.EnemyHp = 500;
        return p;
    }
    static Task PlayerAct(PokemonState p, SkillInfo s) => (Task)typeof(PokemonState).GetMethod("PlayerActAsync", F)!.Invoke(p, new object[] { s, new Random() })!;
    static Task EnemyAct(PokemonState p, SkillInfo s) => (Task)typeof(PokemonState).GetMethod("EnemyActAsync", F)!.Invoke(p, new object[] { s, false, new Random() })!;
    static int[] Stages(PokemonState p, string n) => (int[])typeof(PokemonState).GetField(n, F)!.GetValue(p)!;
    static SkillInfo Sure(int id) { var m = SkillDex.GetSkill(id)!; return new SkillInfo { Id = m.Id, NameKo = m.NameKo, Type = m.Type, Category = m.Category, Power = m.Power, Accuracy = 0, Ailment = m.Ailment, AilmentChance = m.Ailment != SkillAilment.None ? 100 : 0, StatChanges = m.StatChanges, StatTarget = m.StatTarget, StatChance = m.StatChanges != null ? 100 : 0, Priority = m.Priority, MinHits = m.MinHits, MaxHits = m.MaxHits, Drain = m.Drain, CritRate = m.CritRate, FlinchChance = m.FlinchChance }; }
    static void Check(string name, bool ok, string detail) { Console.WriteLine($"{(ok ? "PASS" : "FAIL")}  {name}  [{detail}]"); if (!ok) fails++; }

    // 소수점 버림 때문에 절반 값은 1 차이까지 허용하지 않고, 원래 값의 절반 버림 또는 올림과 비교합니다.
    static int BurnHalf(int full) => full <= 1 ? 1 : full / 2;
    const int SANDSHREW = 27, GASTLY = 92, PIKACHU = 25, RATTATA = 19;
    static async Task Run()
    {
        // 화상: 물리 데미지만 절반 (같은 조건에서 화상 유무 비교)
        async Task<int> Mine(int move, SkillAilment a) { while (true) { var q = New(RATTATA, PIKACHU); q.PlayerAilment = a; await PlayerAct(q, Sure(move)); if (!q.BattleMessage.Contains("급소")) return 500 - q.EnemyHp; } }
        async Task<int> Foes(int move, SkillAilment a) { while (true) { var q = New(PIKACHU, RATTATA); q.EnemyAilment = a; await EnemyAct(q, Sure(move)); if (!q.BattleMessage.Contains("급소")) return 500 - q.PlayerHp; } }
        int a1 = await Mine(33, SkillAilment.None), a2 = await Mine(33, SkillAilment.Burn);
        Check("내 물리기 화상 절반 (몸통박치기)", a2 == BurnHalf(a1), $"{a1} → {a2}");
        int s1 = await Mine(55, SkillAilment.None), s2 = await Mine(55, SkillAilment.Burn);
        Check("내 특수기 화상 영향 없음 (물대포)", s1 == s2, $"{s1} → {s2}");
        int b1 = await Foes(33, SkillAilment.None), b2 = await Foes(33, SkillAilment.Burn);
        Check("상대 물리기 화상 절반 (몸통박치기)", b2 == BurnHalf(b1), $"{b1} → {b2}");
        int f1 = await Foes(55, SkillAilment.None), f2 = await Foes(55, SkillAilment.Burn);
        Check("상대 특수기 화상 영향 없음 (물대포)", f1 == f2, $"{f1} → {f2}");
        // ---- 스프라이트 새 버전 확인 ----
        {
            var now = new DateTime(2026, 11, 4, 0, 0, 0, DateTimeKind.Utc);
            var st = new SpriteUpdateChecker.State();
            Check("기록 없으면 기준만 잡고 안내 안 함", !SpriteUpdateChecker.ApplyCheckResult(st, "A", now) && st.Sha == "A" && st.LastNotifiedUtc == null, $"{st.Sha}");
            Check("같은 커밋이면 안내 안 함", !SpriteUpdateChecker.ApplyCheckResult(st, "A", now), "");
            Check("바뀌면 안내", SpriteUpdateChecker.ApplyCheckResult(st, "B", now) && st.LastNotifiedUtc == now && st.Sha == "A", "받기 전이므로 기록은 그대로");
            Check("한 달 안에는 또 바뀌어도 안내 안 함", !SpriteUpdateChecker.ApplyCheckResult(st, "C", now.AddDays(29)), "29일");
            Check("한 달이 지나면 다시 안내", SpriteUpdateChecker.ApplyCheckResult(st, "D", now.AddDays(59)), "59일");
            var due = new SpriteUpdateChecker.State { LastCheckUtc = now };
            Check("확인은 하루 한 번", !SpriteUpdateChecker.IsCheckDue(due, now.AddHours(23)) && SpriteUpdateChecker.IsCheckDue(due, now.AddHours(24)) && SpriteUpdateChecker.IsCheckDue(new SpriteUpdateChecker.State(), now), "");

            // 실제 GitHub 응답(curl로 저장)과 파일 저장. 이 환경은 .NET 네트워크가 막혀 있어 네트워크 실패 경로도 함께 확인합니다.
            string json = System.IO.File.ReadAllText(System.IO.Path.Combine(AppContext.BaseDirectory, "gh_commits.json"));
            string? real = SpriteUpdateChecker.ParseLatestSha(json);
            Check("실제 GitHub 응답에서 커밋 번호를 읽음", real?.Length == 40, real ?? "null");
            System.IO.Directory.CreateDirectory(AppPaths.SpriteFolder);
            string file = System.IO.Path.Combine(AppPaths.SpriteFolder, "sprite_version.json");
            SpriteUpdateChecker.MarkInstalled(real);
            var saved = SpriteUpdateChecker.LoadState();
            Check("받은 뒤 기록하고 파일에서 다시 읽음", saved.Sha == real && saved.LastNotifiedUtc == null && saved.LastCheckUtc != null, "");
            SpriteUpdateChecker.MarkInstalled(null);
            Check("커밋을 못 받았으면 기록을 비움", SpriteUpdateChecker.LoadState().Sha == null && SpriteUpdateChecker.LoadState().LastCheckUtc == null, "");
            System.IO.File.WriteAllText(file, "깨진 파일");
            Check("기록 파일이 깨져도 멈추지 않음", SpriteUpdateChecker.LoadState().Sha == null, "");
            SpriteUpdateChecker.MarkInstalled("옛날커밋");
            var offline = SpriteUpdateChecker.LoadState(); offline.LastCheckUtc = null;
            System.IO.File.WriteAllText(file, System.Text.Json.JsonSerializer.Serialize(offline));
            bool ahead = await SpriteUpdateChecker.CheckForUpdateAsync();
            bool again = await SpriteUpdateChecker.CheckForUpdateAsync();
            Check("실제 서버: 레포가 앞서면 안내하고, 바로 다시 확인하면 안내 안 함", ahead && !again && SpriteUpdateChecker.LoadState().Sha == "옛날커밋", $"{ahead}, {again}");
            System.IO.File.Delete(file);
        }

        // ---- 프로그램 새 버전 안내 기록 ----
        {
            string notice = System.IO.Path.Combine(AppContext.BaseDirectory, "update_notice.txt");
            if (System.IO.File.Exists(notice)) System.IO.File.Delete(notice);
            Check("새 버전은 처음 한 번 안내", AppVersionUpdater.TryMarkNotified("9.9.9.9"), "");
            Check("같은 버전은 다시 안내 안 함", !AppVersionUpdater.TryMarkNotified("9.9.9.9"), "");
            Check("더 새 버전은 다시 안내", AppVersionUpdater.TryMarkNotified("9.9.9.10"), "");
            System.IO.File.Delete(notice);
            // 현재 버전을 1.0.0.0으로 만들어 서버 버전이 항상 새 버전이 되게 합니다. (빌드가 테스트 폴더에 실제 version.json을 복사합니다)
            string localVersionFile = System.IO.Path.Combine(AppContext.BaseDirectory, "version.json");
            if (System.IO.File.Exists(localVersionFile)) System.IO.File.Delete(localVersionFile);
            var live = await AppVersionUpdater.CheckForNewVersionToNotifyAsync();
            Check("실제 서버: 새 버전을 받아 한 번 안내", live != null && new Version(live.Version) > new Version("1.0.0.0") && System.IO.File.ReadAllText(notice) == live.Version, live?.Version ?? "null");
            Check("실제 서버: 다시 확인하면 안내 안 함", await AppVersionUpdater.CheckForNewVersionToNotifyAsync() == null, "");
            System.IO.File.Delete(notice);
            string? liveSha = await SpriteUpdateChecker.GetLatestShaAsync();
            Check("실제 서버: 스프라이트 레포 최신 커밋", liveSha?.Length == 40, liveSha ?? "null");
        }

        // ---- 웹 버전에서 나온 버그 확인 ----
        {
            var g = New(RATTATA, PIKACHU); g.IsGymBattle = true; g.EnemySpeciesId = RATTATA;
            int gymEnemy = g.EnemySpeciesId; int gymHp = g.EnemyHp;
            g.StartWildBattle(autoEncounter: true);
            g.StartWildBattle();
            Check("체육관 배틀 중에는 야생 배틀이 시작되지 않음", g.IsGymBattle && g.IsBattleOpen && g.EnemySpeciesId == gymEnemy && g.EnemyHp == gymHp, $"상대 {g.EnemySpeciesId}");
            Check("체육관 배틀 문구", g.MoodText == Loc.T("mood.gymBattle"), g.MoodText);
            g.IsGymBattle = false;
            Check("야생 배틀 문구", g.MoodText == Loc.T("mood.battle"), g.MoodText);

            bool CanAuto(PokemonState q) => (bool)typeof(PokemonState).GetProperty("CanAutoEncounter", F)!.GetValue(q)!;
            var m = new PokemonState { SpeciesId = RATTATA, AgeMinutes = 60 };
            Check("메뉴가 없으면 자동 출현 가능", CanAuto(m), "");
            m.IsPartyOpen = true; Check("파티 창이 열려 있으면 자동 출현 안 함", !CanAuto(m), ""); m.IsPartyOpen = false;
            m.IsDexOpen = true; Check("도감이 열려 있으면 자동 출현 안 함", !CanAuto(m), ""); m.IsDexOpen = false;
            m.IsInventoryOpen = true; Check("가방이 열려 있으면 자동 출현 안 함", !CanAuto(m), ""); m.IsInventoryOpen = false;
            PokemonState.IsModalWindowOpen = true; Check("설정 창이 열려 있으면 자동 출현 안 함", !CanAuto(m), ""); PokemonState.IsModalWindowOpen = false;

            var tauros = new PartyMember { SpeciesId = 128, SpriteFileName = "p0128_paldea.bin" };
            var plain = new PartyMember { SpeciesId = 128, SpriteFileName = "p0128.bin" };
            Check("파티: 폼에 맞는 타입", tauros.Type1Display == "FIGHTING" && tauros.Type2Display == "", $"{tauros.Type1Display}/{tauros.Type2Display}");
            Check("파티: 기본 폼은 원래 타입", plain.Type1Display == "NORMAL" && plain.Type2Display == "", $"{plain.Type1Display}/{plain.Type2Display}");
        }

        // ---- 배틀의 폼 타입 ----
        {
            PokemonInfo Info(PokemonState q, string prop) => (PokemonInfo)typeof(PokemonState).GetProperty(prop, F)!.GetValue(q)!;
            // SpriteFileName은 실제 파일이 있을 때만 그 이름을 돌려주므로 테스트용 빈 파일을 둡니다.
            System.IO.Directory.CreateDirectory(AppPaths.SpriteFolder);
            System.IO.File.WriteAllBytes(AppPaths.SpritePath("p0128_paldea.bin"), Array.Empty<byte>());
            var mine = New(128, RATTATA); mine.SpriteFileName = "p0128_paldea.bin";
            Check("내 폼 타입 (팔데아 켄타로스 = 격투)", Info(mine, "MyBattleInfo").Type1 == PokemonType.Fighting && Info(mine, "MyBattleInfo").Type2 == PokemonType.None, $"{Info(mine, "MyBattleInfo").Type1}");
            var plainMe = New(128, RATTATA); plainMe.SpriteFileName = "p0128.bin";
            Check("기본 폼은 원래 타입 (노말)", Info(plainMe, "MyBattleInfo").Type1 == PokemonType.Normal, $"{Info(plainMe, "MyBattleInfo").Type1}");
            Check("폼이 달라도 종족값은 그대로", Info(mine, "MyBattleInfo").BaseAtk == Info(plainMe, "MyBattleInfo").BaseAtk && Info(mine, "MyBattleInfo").BaseAtk > 0, "");

            var foe = New(RATTATA, 128); SetF(foe, "_enemySpriteFileName", "p0128_paldea.bin");
            Check("상대 폼 타입", Info(foe, "EnemyBattleInfo").Type1 == PokemonType.Fighting, $"{Info(foe, "EnemyBattleInfo").Type1}");
            await PlayerAct(foe, Sure(16));
            int formHit = 500 - foe.EnemyHp;
            var foe2 = New(RATTATA, 128); SetF(foe2, "_enemySpriteFileName", "p0128.bin");
            await PlayerAct(foe2, Sure(16));
            Check("비행 기술이 격투 폼에 효과가 굉장함", foe.BattleMessage.Contains("굉장") && !foe2.BattleMessage.Contains("굉장"), $"{formHit} / {500 - foe2.EnemyHp}");

            var ghostFoe = New(128, GASTLY); ghostFoe.SpriteFileName = "p0128_paldea.bin";
            await PlayerAct(ghostFoe, Sure(2));
            Check("내 격투 폼의 격투 기술 자속 + 고스트에 0배", ghostFoe.EnemyHp == 500, $"HP {ghostFoe.EnemyHp}");
            var normalMove = New(128, GASTLY); normalMove.SpriteFileName = "p0128_paldea.bin";
            bool stabOn = BattleCalculator.StabMultiplier(PokemonType.Fighting, Info(normalMove, "MyBattleInfo")) == 1.5 && BattleCalculator.StabMultiplier(PokemonType.Normal, Info(normalMove, "MyBattleInfo")) == 1.0;
            Check("폼 타입으로 자속 계산 (격투 1.5배, 노말 1배)", stabOn, "");
            System.IO.File.Delete(AppPaths.SpritePath("p0128_paldea.bin"));
        }

        // ---- 대체 모션 ----
        {
            var img = System.Windows.Media.Imaging.BitmapSource.Create(1, 1, 96, 96, System.Windows.Media.PixelFormats.Bgra32, null, new byte[4], 4); img.Freeze();
            SpriteFrameData F1() => new SpriteFrameData { Image = img, FrameWidth = 1, FrameHeight = 1 };
            var basic = new List<SpriteFrameData?>(); for (int i = 0; i <= 12; i++) basic.Add(F1());
            int R(IReadOnlyList<SpriteFrameData?> l, int a) => PokemonState.ResolveMotion(l, a);
            Check("모션이 있으면 그대로", R(basic, PokemonState.ANIM_ATTACK) == PokemonState.ANIM_ATTACK, "");
            Check("먹기 → 연속", R(basic, PokemonState.ANIM_EAT) == PokemonState.ANIM_DOUBLE, "");
            Check("포즈 → 깡총", R(basic, PokemonState.ANIM_POSE) == PokemonState.ANIM_HOP, "");
            Check("심호흡 → 기 모으기", R(basic, PokemonState.ANIM_DEEPBREATH) == PokemonState.ANIM_CHARGE, "");
            Check("잠깐 나오는 아픔 모션 → 맞는 모션", R(basic, PokemonState.ANIM_PAIN) == PokemonState.ANIM_HURT && R(basic, PokemonState.ANIM_SINK) == PokemonState.ANIM_HURT, "");
            int RC(IReadOnlyList<SpriteFrameData?> l, int a) => PokemonState.ResolveMotion(l, a, continuous: true);
            Check("계속 유지되는 상태는 대기 (응가, 배고픔, 우울, 기운 없음)", RC(basic, PokemonState.ANIM_PAIN) == PokemonState.ANIM_IDLE && RC(basic, PokemonState.ANIM_CRINGE) == PokemonState.ANIM_IDLE && RC(basic, PokemonState.ANIM_SINK) == PokemonState.ANIM_IDLE && RC(basic, PokemonState.ANIM_EVENTSLEEP) == PokemonState.ANIM_IDLE, "");
            Check("다른 모션은 계속 재생이어도 대체 적용", RC(basic, PokemonState.ANIM_EAT) == PokemonState.ANIM_DOUBLE && RC(basic, PokemonState.ANIM_FLOAT) == PokemonState.ANIM_ROTATE, "");
            Check("기운 없음 → 잠", R(basic, PokemonState.ANIM_EVENTSLEEP) == PokemonState.ANIM_SLEEP, "");
            Check("대체가 없는 모션 → 대기", R(basic, PokemonState.ANIM_SIT) == PokemonState.ANIM_IDLE, "");
            var mid = new List<SpriteFrameData?>(basic); mid[PokemonState.ANIM_SWING] = null;
            Check("대체 모션도 없으면 그다음 (당기기 → 휘두르기 → 공격)", R(mid, PokemonState.ANIM_PULL) == PokemonState.ANIM_ATTACK, "");
            var only = new List<SpriteFrameData?> { null, null, null, F1() };
            Check("대기도 없으면 첫 번째 모션", R(only, PokemonState.ANIM_EAT) == 3, "");
            Check("아무것도 없으면 -1", R(new List<SpriteFrameData?> { null }, PokemonState.ANIM_EAT) == -1, "");
            var full = new List<SpriteFrameData?>(); for (int i = 0; i <= 34; i++) full.Add(F1());
            Check("전용 모션이 있으면 전용 모션", R(full, PokemonState.ANIM_EAT) == PokemonState.ANIM_EAT, "");
            Check("전용 모션이 있으면 계속 유지 상태도 전용 모션", RC(full, PokemonState.ANIM_PAIN) == PokemonState.ANIM_PAIN, "");
        }

        // ---- 작은 문제 수정 ----
        {
            var ds2 = SkillDex.GetSkill(3)!; var kick = SkillDex.GetSkill(24)!; var roll = SkillDex.GetSkill(205)!; var plainS = SkillDex.GetSkill(33)!;
            Check("AI 기대 위력: 일반 기술은 그대로", BattleCalculator.ExpectedPower(plainS) == plainS.Power, $"{plainS.Power}");
            Check("AI 기대 위력: 2~5회는 평균 3.1배", BattleCalculator.ExpectedPower(ds2) == (int)(ds2.Power * 3.1), $"{ds2.Power} → {BattleCalculator.ExpectedPower(ds2)}");
            Check("AI 기대 위력: 고정 2회는 2배", BattleCalculator.ExpectedPower(kick) == kick.Power * 2, $"{kick.Power} → {BattleCalculator.ExpectedPower(kick)}");
            Check("AI 기대 위력: 구르기는 3배", BattleCalculator.ExpectedPower(roll) == roll.Power * 3, $"{BattleCalculator.ExpectedPower(roll)}");
            int Motion(PokemonState q) => (int)typeof(PokemonState).GetField("_enemyMotion", F)!.GetValue(q)!;
            int Cur(PokemonState q) => (int)typeof(PokemonState).GetField("_currentActionId", F)!.GetValue(q)!;
            int EnemyTimer(PokemonState q) => (int)typeof(PokemonState).GetField("_enemyTempActionTimer", F)!.GetValue(q)!;
            var faintA = New(RATTATA, PIKACHU); await PlayerAct(faintA, Sure(33));
            Check("상대가 살아 있으면 맞는 모션", Motion(faintA) == PokemonState.ANIM_HURT, $"{Motion(faintA)}");
            var faintB = New(RATTATA, PIKACHU); faintB.EnemyHp = 1; await PlayerAct(faintB, Sure(33));
            Check("상대가 쓰러지면 기절 모션 유지", Motion(faintB) == PokemonState.ANIM_FAINT && EnemyTimer(faintB) == 0, $"{Motion(faintB)}, 타이머 {EnemyTimer(faintB)}");
            var faintC = New(RATTATA, PIKACHU); faintC.PlayerHp = 1; await EnemyAct(faintC, Sure(33));
            Check("내 포켓몬이 쓰러지면 기절 모션", Cur(faintC) == PokemonState.ANIM_FAINT, $"{Cur(faintC)}");
            faintC.CloseBattle();
            Check("배틀이 끝나면 기절 모션 해제", Cur(faintC) != PokemonState.ANIM_FAINT, $"{Cur(faintC)}");
            Check("버전 확인: 실제 서버 응답", (await AppVersionUpdater.CheckForUpdatesAsync()).Succeeded, "");
            var newer = AppVersionUpdater.ParseServerVersion("{\"Version\":\"9.0.0.0\",\"DownloadUrl\":\"u\"}", "1.0.0.0");
            Check("버전 확인: 새 버전 있음", newer.Succeeded && newer.NewVersion?.Version == "9.0.0.0", "");
            var sameVersion = AppVersionUpdater.ParseServerVersion("{\"Version\":\"1.0.0.0\"}", "1.0.0.0");
            Check("버전 확인: 최신 버전", sameVersion.Succeeded && sameVersion.NewVersion == null, "");
            var broken = AppVersionUpdater.ParseServerVersion("{\"Version\":\"abc\"}", "1.0.0.0");
            Check("버전 확인: 깨진 버전은 실패", !broken.Succeeded && broken.NewVersion == null, "");
            Check("버전 확인: 깨진 응답은 실패", !AppVersionUpdater.ParseServerVersion("not json", "1.0.0.0").Succeeded, "");

            Check("AI 확실한 위력: 구르기는 첫 타격만", BattleCalculator.SurePower(roll) == roll.Power, $"{BattleCalculator.SurePower(roll)}");
            Check("AI 확실한 위력: 2~5회는 2회만큼", BattleCalculator.SurePower(ds2) == ds2.Power * 2, $"{BattleCalculator.SurePower(ds2)}");
            Check("AI 확실한 위력: 고정 2회와 일반 기술", BattleCalculator.SurePower(kick) == kick.Power * 2 && BattleCalculator.SurePower(plainS) == plainS.Power, "");
            var koFoe = New(RATTATA, 74); koFoe.EnemySkills = new[] { 205, 33, 0, 0 }; koFoe.EnemyLevel = 50; koFoe.AgeMinutes = 60;
            int ChoosePick() => ((SkillInfo)typeof(PokemonState).GetMethod("ChooseEnemySkill", F)!.Invoke(koFoe, null)!).Id;
            koFoe.PlayerHp = 1; int rollAtOneHp = Count(() => ChoosePick() == 205 || ChoosePick() == 33, 50);
            Check("AI 선택이 예외 없이 동작", rollAtOneHp == 50, "");

            var faintTick = New(RATTATA, PIKACHU); faintTick.EnemyHp = 1; await PlayerAct(faintTick, Sure(33));
            typeof(PokemonState).GetMethod("MainLoopTimer_Tick", F)!.Invoke(faintTick, new object?[] { null, EventArgs.Empty });
            Check("쓰러진 상대는 타이머로 일어서지 않음", Motion(faintTick) == PokemonState.ANIM_FAINT, $"{Motion(faintTick)}");
        }

        // ---- 메가진화 상태로 패배 ----
        {
            var mg = New(RATTATA, PIKACHU); mg.PlayerHp = 1;
            typeof(PokemonState).GetProperty("ActiveMegaForm", F)!.SetValue(mg, new MegaFormInfo { SpeciesId = RATTATA, Form = "mega" });
            await EnemyAct(mg, Sure(33));
            int timerBefore = (int)typeof(PokemonState).GetField("_tempActionTimer", F)!.GetValue(mg)!;
            mg.CloseBattle();
            int timerAfter = (int)typeof(PokemonState).GetField("_tempActionTimer", F)!.GetValue(mg)!;
            int actionAfter = (int)typeof(PokemonState).GetField("_currentActionId", F)!.GetValue(mg)!;
            Check("메가진화로 져도 메가가 풀림", mg.ActiveMegaForm == null, "");
            Check("메가진화로 져도 기절 모션이 남지 않음", timerBefore > 1000 && timerAfter == 0 && actionAfter != PokemonState.ANIM_FAINT, $"타이머 {timerBefore} → {timerAfter}, 모션 {actionAfter}");
            var nm = New(RATTATA, PIKACHU); nm.PlayerHp = 1;
            await EnemyAct(nm, Sure(33)); nm.CloseBattle();
            Check("메가가 아니어도 기절 모션 해제", (int)typeof(PokemonState).GetField("_tempActionTimer", F)!.GetValue(nm)! == 0, "");
        }

        // ---- 기술 효과 6종 ----
        PokemonState p;
        var rnd = new Random(1);
        int Count(Func<bool> f, int n = 24000) { int c = 0; for (int i = 0; i < n; i++) if (f()) c++; return c; }
        double r0 = Count(() => BattleCalculator.RollCrit(new SkillInfo(), rnd)) / 24000.0, r1 = Count(() => BattleCalculator.RollCrit(new SkillInfo { CritRate = 1 }, rnd)) / 24000.0, r6 = Count(() => BattleCalculator.RollCrit(new SkillInfo { CritRate = 6 }, rnd)) / 24000.0;
        Check("급소 확률 (단계0 약 4.2%, 단계1 약 12.5%, 단계6 100%)", Math.Abs(r0 - 1 / 24.0) < 0.006 && Math.Abs(r1 - 0.125) < 0.01 && r6 == 1, $"{r0:P1}, {r1:P1}, {r6:P0}");
        Check("급소 데미지 1.5배", BattleCalculator.RawDamage(100, 50, 1, 1, 1, true, 100) == 225 && BattleCalculator.RawDamage(100, 50, 1, 1, 1, false, 100) == 150, "150 → 225");
        var counts = new int[7]; var ds = new SkillInfo { MinHits = 2, MaxHits = 5 }; for (int i = 0; i < 20000; i++) counts[BattleCalculator.RollHitCount(ds, rnd)]++;
        Check("연속 횟수 분포 35/35/15/15", counts[1] == 0 && counts[6] == 0 && Math.Abs(counts[2] / 200.0 - 35) < 2 && Math.Abs(counts[3] / 200.0 - 35) < 2 && Math.Abs(counts[4] / 200.0 - 15) < 2 && Math.Abs(counts[5] / 200.0 - 15) < 2, $"2:{counts[2]} 3:{counts[3]} 4:{counts[4]} 5:{counts[5]}");
        Check("고정 2회/단일 기술", BattleCalculator.RollHitCount(SkillDex.GetSkill(24)!, rnd) == 2 && BattleCalculator.RollHitCount(SkillDex.GetSkill(33)!, rnd) == 1, $"두번차기 {SkillDex.GetSkill(24)!.MaxHits}");

        // 연속 공격: 실제 턴
        p = New(RATTATA, PIKACHU); await PlayerAct(p, Sure(3));
        Check("연속뺨치기: 2~5번 맞힘", System.Text.RegularExpressions.Regex.IsMatch(p.BattleMessage, "^[2-5]번 맞혔다") && p.EnemyHp < 500, $"\"{p.BattleMessage}\", HP {p.EnemyHp}");
        p = New(RATTATA, PIKACHU); p.EnemyHp = 1; await PlayerAct(p, Sure(3));
        Check("연속 공격 중 쓰러지면 멈춤", p.EnemyHp == 0 && p.BattleMessage.StartsWith("1번"), $"\"{p.BattleMessage}\"");

        // 흡수 / 반동
        p = New(RATTATA, PIKACHU); p.PlayerHp = 100; int e0 = p.EnemyHp; await PlayerAct(p, Sure(72));
        int dmg = e0 - p.EnemyHp; Check("메가드레인: 입힌 만큼의 50% 회복", p.PlayerHp == 100 + Math.Max(1, dmg / 2), $"데미지 {dmg}, HP 100 → {p.PlayerHp}");
        p = New(RATTATA, PIKACHU); e0 = p.EnemyHp; await PlayerAct(p, Sure(38));
        dmg = e0 - p.EnemyHp; Check("이판사판태클: 33% 반동", p.PlayerHp == 500 - Math.Max(1, dmg * 33 / 100), $"데미지 {dmg}, HP 500 → {p.PlayerHp}, \"{p.BattleMessage}\"");
        p = New(PIKACHU, RATTATA); p.EnemyHp = 100; int m0 = p.PlayerHp; await EnemyAct(p, Sure(72));
        dmg = m0 - p.PlayerHp; Check("상대 메가드레인도 회복", p.EnemyHp == 100 + Math.Max(1, dmg / 2), $"데미지 {dmg}, 상대 HP 100 → {p.EnemyHp}");
        p = New(RATTATA, PIKACHU); p.PlayerHp = 1; await PlayerAct(p, Sure(38));
        Check("반동으로 내가 쓰러질 수 있음", p.PlayerHp == 0, $"HP {p.PlayerHp}");

        // 풀죽음
        var bite = Sure(44); bite.FlinchChance = 100;
        p = New(RATTATA, PIKACHU); await PlayerAct(p, bite); int hp0 = p.PlayerHp; await EnemyAct(p, Sure(33));
        Check("먼저 맞은 상대는 풀죽어 못 움직임", p.PlayerHp == hp0 && p.BattleMessage.Contains("풀이 죽어"), $"\"{p.BattleMessage}\"");
        await (Task)typeof(PokemonState).GetMethod("FinishTurnAsync", F)!.Invoke(p, null)!; await EnemyAct(p, Sure(33));
        Check("다음 턴에는 다시 움직임", p.PlayerHp < hp0, $"HP {hp0} → {p.PlayerHp}");
        p = New(PIKACHU, RATTATA); await EnemyAct(p, bite); int eh0 = p.EnemyHp; await PlayerAct(p, Sure(33));
        Check("내 포켓몬도 풀죽음", p.EnemyHp == eh0 && p.BattleMessage.Contains("풀이 죽어"), $"\"{p.BattleMessage}\"");

        // 우선도
        bool First(PokemonState q, SkillInfo a, SkillInfo b) => (bool)typeof(PokemonState).GetMethod("DecidePlayerFirst", F)!.Invoke(q, new object[] { a, b, rnd })!;
        p = New(RATTATA, PIKACHU); var tackle = SkillDex.GetSkill(33)!; var quick = SkillDex.GetSkill(98)!; var focus = SkillDex.GetSkill(264)!;
        Check("우선도 높으면 항상 먼저 (전광석화)", Count(() => First(p, quick, tackle), 300) == 300 && Count(() => First(p, tackle, quick), 300) == 0, $"우선도 {quick.Priority}");
        Check("우선도 낮으면 항상 나중 (힘껏펀치)", Count(() => First(p, focus, tackle), 300) == 0, $"{focus.NameKo} 우선도 {focus.Priority}");
        int same = Count(() => First(p, tackle, tackle), 3000); Check("같은 우선도는 스피드 + 20% 규칙", same > 0 && same < 3000, $"{same}/3000");

        // 속이기: 첫 행동에만 성공
        var fake = Sure(252); Check("속이기 데이터", fake.Priority == 3 && fake.FlinchChance == 100 && fake.IsFirstTurnOnly && SkillDex.GetSkill(660)!.Priority == 2, $"{fake.NameKo}, {SkillDex.GetSkill(660)!.NameKo}");
        p = New(RATTATA, PIKACHU); await PlayerAct(p, fake); int fh = p.EnemyHp; bool ok1 = fh < 500;
        await (Task)typeof(PokemonState).GetMethod("FinishTurnAsync", F)!.Invoke(p, null)!; await PlayerAct(p, fake);
        Check("속이기: 첫 번째 성공, 두 번째 실패", ok1 && p.EnemyHp == fh && p.BattleMessage.Contains("실패"), $"HP 500 → {fh} → {p.EnemyHp}, \"{p.BattleMessage}\"");
        p = New(RATTATA, PIKACHU); p.EnemySkills = new[] { 252, 33, 0, 0 }; typeof(PokemonState).GetField("_enemyHasMoved", F)!.SetValue(p, true);
        var choose = typeof(PokemonState).GetMethod("ChooseEnemySkill", F)!;
        Check("상대 AI는 첫 턴 뒤 속이기를 안 고름", Count(() => ((SkillInfo)choose.Invoke(p, null)!).Id == 252, 200) == 0, "200회");

        // ---- 조건이 있는 기술 ----
        void SetF(PokemonState q, string n, object? v) => typeof(PokemonState).GetField(n, F)!.SetValue(q, v);
        Task Finish(PokemonState q) => (Task)typeof(PokemonState).GetMethod("FinishTurnAsync", F)!.Invoke(q, null)!;
        var dream = Sure(138); var snore = Sure(173); var focusP = Sure(264); var trap = Sure(704); var sucker = Sure(389); var clap = Sure(909);
        // 꿈먹기
        p = New(RATTATA, PIKACHU); await PlayerAct(p, dream);
        Check("꿈먹기: 깨어 있으면 실패", p.EnemyHp == 500 && p.BattleMessage.Contains("실패"), $"HP {p.EnemyHp}");
        p = New(RATTATA, PIKACHU); p.EnemyAilment = SkillAilment.Sleep; p.PlayerHp = 100; await PlayerAct(p, dream);
        Check("꿈먹기: 잠들어 있으면 데미지+회복", p.EnemyHp < 500 && p.PlayerHp > 100, $"상대 {p.EnemyHp}, 나 {p.PlayerHp}");
        // 코골기
        p = New(RATTATA, PIKACHU); await PlayerAct(p, snore);
        Check("코골기: 깨어 있으면 실패", p.EnemyHp == 500 && p.BattleMessage.Contains("실패"), $"HP {p.EnemyHp}");
        p = New(RATTATA, PIKACHU); p.PlayerAilment = SkillAilment.Sleep; SetF(p, "_playerAilmentTurns", 5); await PlayerAct(p, snore);
        Check("코골기: 잠든 채로 공격", p.EnemyHp < 500 && p.PlayerAilment == SkillAilment.Sleep, $"HP {p.EnemyHp}, {p.PlayerAilment}");
        p = New(RATTATA, PIKACHU); p.PlayerAilment = SkillAilment.Sleep; SetF(p, "_playerAilmentTurns", 5); await PlayerAct(p, Sure(33));
        Check("대조: 잠들면 다른 기술은 못 씀", p.EnemyHp == 500, $"HP {p.EnemyHp}");
        p = New(PIKACHU, RATTATA); p.EnemyAilment = SkillAilment.Sleep; SetF(p, "_enemyAilmentTurns", 5); await EnemyAct(p, snore);
        Check("상대 코골기: 잠든 채로 공격", p.PlayerHp < 500, $"HP {p.PlayerHp}");
        // 힘껏펀치 / 트랩셸
        p = New(RATTATA, PIKACHU); await EnemyAct(p, Sure(33)); await PlayerAct(p, focusP);
        Check("힘껏펀치: 먼저 맞으면 실패", p.EnemyHp == 500 && p.BattleMessage.Contains("집중"), $"\"{p.BattleMessage}\"");
        p = New(RATTATA, PIKACHU); await EnemyAct(p, Sure(45)); await PlayerAct(p, focusP);
        Check("힘껏펀치: 변화 기술만 받으면 성공", p.EnemyHp < 500, $"HP {p.EnemyHp}");
        p = New(RATTATA, PIKACHU); await PlayerAct(p, trap);
        Check("트랩셸: 안 맞으면 실패", p.EnemyHp == 500 && p.BattleMessage.Contains("실패"), $"HP {p.EnemyHp}");
        p = New(RATTATA, PIKACHU); await EnemyAct(p, Sure(55)); await PlayerAct(p, trap);
        Check("트랩셸: 특수 공격을 맞아도 실패", p.EnemyHp == 500, $"HP {p.EnemyHp}");
        p = New(RATTATA, PIKACHU); await EnemyAct(p, Sure(33)); await PlayerAct(p, trap);
        Check("트랩셸: 물리 공격을 맞으면 성공", p.EnemyHp < 500, $"HP {p.EnemyHp}");
        await Finish(p); int th = p.EnemyHp; await PlayerAct(p, trap);
        Check("트랩셸: 다음 턴에는 기록이 지워짐", p.EnemyHp == th, $"HP {th} → {p.EnemyHp}");
        // 기습 / 질풍신뢰
        p = New(RATTATA, PIKACHU); SetF(p, "_enemyChoice", SkillDex.GetSkill(45)); await PlayerAct(p, sucker);
        Check("기습: 상대가 변화 기술이면 실패", p.EnemyHp == 500 && p.BattleMessage.Contains("실패"), $"HP {p.EnemyHp}");
        p = New(RATTATA, PIKACHU); SetF(p, "_enemyChoice", SkillDex.GetSkill(33)); await PlayerAct(p, sucker);
        Check("기습: 상대가 공격 기술이면 성공", p.EnemyHp < 500, $"HP {p.EnemyHp}");
        p = New(RATTATA, PIKACHU); await PlayerAct(p, clap);
        Check("질풍신뢰: 상대 선택이 없으면 실패", p.EnemyHp == 500, $"HP {p.EnemyHp}");
        p = New(PIKACHU, RATTATA); await EnemyAct(p, sucker);
        Check("상대 기습: 내가 회피/아이템이면 실패", p.PlayerHp == 500 && p.BattleMessage.Contains("실패"), $"HP {p.PlayerHp}");
        // 상대 AI
        var pick = typeof(PokemonState).GetMethod("ChooseEnemySkill", F)!;
        p = New(RATTATA, PIKACHU); p.EnemySkills = new[] { 138, 173, 33, 0 };
        Check("상대 AI: 조건 안 맞으면 꿈먹기/코골기 안 고름", Count(() => ((SkillInfo)pick.Invoke(p, null)!).Id != 33, 200) == 0, "200회");
        p = New(RATTATA, PIKACHU); p.EnemySkills = new[] { 138, 0, 0, 0 }; p.PlayerAilment = SkillAilment.Sleep;
        Check("상대 AI: 내가 잠들면 꿈먹기 사용", ((SkillInfo)pick.Invoke(p, null)!).Id == 138, "");

        // ---- 여러 턴 기술과 남은 조건 ----
        object? Lk(PokemonState q, string side) => typeof(PokemonState).GetField(side, F)!.GetValue(q);
        SkillInfo? Forced(object lk) => (SkillInfo?)lk.GetType().GetField("Forced")!.GetValue(lk);
        var tackleS = Sure(33); var solar = Sure(76); var fly = Sure(19); var beam = Sure(63); var outrage = Sure(200);

        p = New(RATTATA, PIKACHU); await PlayerAct(p, solar);
        Check("솔라빔 1턴: 모으기만", p.EnemyHp == 500 && p.BattleMessage.Contains("모으") && Forced(Lk(p, "_playerLock")!)?.Id == 76, $"HP {p.EnemyHp}, {p.BattleMessage}");
        await PlayerAct(p, solar);
        Check("솔라빔 2턴: 공격 후 해제", p.EnemyHp < 500 && Forced(Lk(p, "_playerLock")!) == null, $"HP {p.EnemyHp}");

        p = New(RATTATA, PIKACHU); await PlayerAct(p, fly); int fh0 = p.PlayerHp;
        Check("공중날기 1턴: 숨음", !p.IsPlayerVisible && p.BattleMessage.Contains("숨"), $"보임 {p.IsPlayerVisible}");
        await EnemyAct(p, tackleS);
        Check("숨은 동안 공격이 빗나감", p.PlayerHp == fh0 && p.BattleMessage.Contains("빗나"), p.BattleMessage);
        await PlayerAct(p, fly);
        Check("공중날기 2턴: 나와서 공격", p.IsPlayerVisible && p.EnemyHp < 500, $"HP {p.EnemyHp}");
        await EnemyAct(p, tackleS);
        Check("나온 뒤에는 맞음", p.PlayerHp < fh0, $"HP {p.PlayerHp}");
        p = New(RATTATA, PIKACHU); await EnemyAct(p, fly);
        Check("상대도 숨음", !p.IsEnemyVisible, "");
        await PlayerAct(p, tackleS); Check("숨은 상대에게 빗나감", p.EnemyHp == 500, $"HP {p.EnemyHp}");
        await PlayerAct(p, Sure(14)); Check("숨은 상대가 있어도 칼춤은 성공", Stages(p, "_playerStages")[0] == 2, $"공격 단계 {Stages(p, "_playerStages")[0]}");

        p = New(RATTATA, PIKACHU); await PlayerAct(p, beam); int bh = p.EnemyHp;
        Check("파괴광선: 맞히면 재충전 예약", bh < 500 && Forced(Lk(p, "_playerLock")!)?.Id == 63, $"HP {bh}");
        await PlayerAct(p, beam);
        Check("다음 턴은 쉼", p.EnemyHp == bh && p.BattleMessage.Contains("반동") && Forced(Lk(p, "_playerLock")!) == null, p.BattleMessage);
        await PlayerAct(p, beam);
        Check("그다음 턴은 다시 사용 가능", p.EnemyHp < bh, $"HP {p.EnemyHp}");

        p = New(RATTATA, GASTLY); await PlayerAct(p, beam);
        Check("0배로 안 맞으면 재충전 없음", Forced(Lk(p, "_playerLock")!) == null, "");

        var locks = new List<int>();
        for (int i = 0; i < 12; i++)
        {
            p = New(RATTATA, PIKACHU); p.EnemyMaxHp = 99999; p.EnemyHp = 99999;
            int turns = 0; do { await PlayerAct(p, outrage); turns++; } while (Forced(Lk(p, "_playerLock")!) != null && turns < 9);
            locks.Add(turns);
        }
        Check("역린: 2~3턴 고정 후 풀림", locks.All(t => t is 2 or 3), string.Join(",", locks));
        p = New(RATTATA, PIKACHU); await PlayerAct(p, outrage); p.PlayerAilment = SkillAilment.Sleep; SetF(p, "_playerAilmentTurns", 5); await PlayerAct(p, outrage);
        Check("역린 중 잠들면 끊김", Forced(Lk(p, "_playerLock")!) == null, "");
        p = New(RATTATA, PIKACHU); await PlayerAct(p, fly); p.PlayerAilment = SkillAilment.Sleep; SetF(p, "_playerAilmentTurns", 5); await PlayerAct(p, fly);
        Check("숨은 뒤 잠들면 나타남", p.IsPlayerVisible && Forced(Lk(p, "_playerLock")!) == null, "");

        // 턴 흐름 전체: 메뉴 없이 자동 진행
        p = New(RATTATA, PIKACHU); p.EnemySkills = new[] { 33, 0, 0, 0 }; p.IsPlayerTurn = true;
        await p.ExecuteSkillTurnAsync(Sure(76));
        Check("전체 턴: 솔라빔이 자동으로 2턴째까지 진행", p.EnemyHp < 500 && p.IsPlayerTurn && Forced(Lk(p, "_playerLock")!) == null && p.BattleMessage == Loc.T("battle.choose"), $"HP {p.EnemyHp}, 메뉴 {p.IsPlayerTurn}");
        p = New(RATTATA, PIKACHU); p.EnemySkills = new[] { 63, 0, 0, 0 };
        var ch = (SkillInfo)pick.Invoke(p, null)!;
        await EnemyAct(p, Sure(63)); var ch2 = (SkillInfo)pick.Invoke(p, null)!;
        Check("상대 AI: 재충전 턴은 그 기술로 고정", ch.Id == 63 && ch2.Id == 63 && Forced(Lk(p, "_enemyLock")!) != null, "");

        // 싱크로노이즈
        p = New(RATTATA, PIKACHU); await PlayerAct(p, Sure(485));
        Check("싱크로노이즈: 타입 다르면 실패", p.EnemyHp == 500 && p.BattleMessage.Contains("실패"), $"HP {p.EnemyHp}");
        p = New(RATTATA, RATTATA); await PlayerAct(p, Sure(485));
        Check("싱크로노이즈: 타입 같으면 성공", p.EnemyHp < 500, $"HP {p.EnemyHp}");
        // 불사르기
        const int CHARMANDER = 4;
        p = New(RATTATA, PIKACHU); await PlayerAct(p, Sure(682));
        Check("불사르기: 불꽃 타입 아니면 실패", p.EnemyHp == 500, $"HP {p.EnemyHp}");
        p = New(CHARMANDER, PIKACHU); await PlayerAct(p, Sure(682)); int bu = p.EnemyHp;
        await PlayerAct(p, Sure(682));
        Check("불사르기: 맞히면 불꽃을 잃어 다시 못 씀", bu < 500 && p.EnemyHp == bu && p.BattleMessage.Contains("실패"), $"HP 500 → {bu} → {p.EnemyHp}");
        var info = (PokemonInfo)typeof(PokemonState).GetProperty("MyBattleInfo", F)!.GetValue(p)!;
        Check("잃은 타입이 상성 계산에 반영", info.Type1 == PokemonType.None && BattleCalculator.TypeMultiplier(PokemonType.Water, info) == 1.0, $"물 → {BattleCalculator.TypeMultiplier(PokemonType.Water, info)}배");
        // 비장의무기
        p = New(RATTATA, PIKACHU); p.Skills = new[] { 387, 33, 45, 0 }; await PlayerAct(p, Sure(387));
        Check("비장의무기: 다른 기술을 안 썼으면 실패", p.EnemyHp == 500, $"HP {p.EnemyHp}");
        await PlayerAct(p, tackleS); await PlayerAct(p, Sure(45)); int lr = p.EnemyHp; await PlayerAct(p, Sure(387));
        Check("비장의무기: 다 쓰면 성공", p.EnemyHp < lr, $"HP {lr} → {p.EnemyHp}");
        p = New(RATTATA, PIKACHU); p.Skills = new[] { 387, 0, 0, 0 }; await PlayerAct(p, Sure(387));
        Check("비장의무기: 다른 기술이 없으면 실패", p.EnemyHp == 500, $"HP {p.EnemyHp}");
        // 트림, 아이언롤러
        Check("트림/아이언롤러는 후보에서 제외", !SkillDex.GetSkill(562)!.IsSupported && !SkillDex.GetSkill(798)!.IsSupported, "");
        p = New(RATTATA, PIKACHU); await PlayerAct(p, Sure(562));
        Check("이미 배운 트림은 실패", p.EnemyHp == 500 && p.BattleMessage.Contains("실패"), $"HP {p.EnemyHp}");

        // ---- 구르기, 소란피기, 묶기 ----
        int Rolls(PokemonState q) { var l = Lk(q, "_playerLock")!; return (int)l.GetType().GetField("RollCount")!.GetValue(l)!; }
        var rollout = Sure(205); var uproar = Sure(253); var bind = Sure(20);
        p = New(RATTATA, PIKACHU); p.EnemyMaxHp = 999999; p.EnemyHp = 999999;
        var rollDmg = new List<int>(); int rt = 0;
        do { int before = p.EnemyHp; await PlayerAct(p, rollout); rollDmg.Add(before - p.EnemyHp); rt++; } while (Forced(Lk(p, "_playerLock")!) != null && rt < 9);
        Check("구르기: 5턴 고정, 위력이 계속 커짐", rt == 5 && Rolls(p) == 5 && rollDmg[4] > rollDmg[0] * 4, string.Join(" → ", rollDmg));
        p = New(RATTATA, PIKACHU); await PlayerAct(p, rollout); await EnemyAct(p, fly); await PlayerAct(p, rollout);
        Check("구르기: 빗나가면 끊기고 위력 초기화", Forced(Lk(p, "_playerLock")!) == null && Rolls(p) == 0, p.BattleMessage);

        p = New(RATTATA, PIKACHU); rt = 0;
        do { await PlayerAct(p, uproar); rt++; } while (Forced(Lk(p, "_playerLock")!) != null && rt < 9);
        Check("소란피기: 3턴 고정", rt == 3, $"{rt}턴");
        p = New(RATTATA, PIKACHU); await PlayerAct(p, uproar); await EnemyAct(p, Sure(47));
        Check("소란피기 중에는 잠들지 않음", p.PlayerAilment == SkillAilment.None, $"{p.PlayerAilment}, {p.BattleMessage}");
        p = New(RATTATA, PIKACHU); p.EnemyAilment = SkillAilment.Sleep; await PlayerAct(p, uproar);
        Check("소란피기: 잠든 상대를 깨움", p.EnemyAilment == SkillAilment.None, $"{p.EnemyAilment}");
        p = New(RATTATA, PIKACHU); await PlayerAct(p, uproar); await PlayerAct(p, uproar); await PlayerAct(p, uproar); await Finish(p); await EnemyAct(p, Sure(47));
        Check("소란피기가 끝나면 다시 잠듦", p.PlayerAilment == SkillAilment.Sleep, $"{p.PlayerAilment}");

        p = New(RATTATA, PIKACHU); await PlayerAct(p, bind); int bh0 = p.EnemyHp;
        int trapTurns = (int)typeof(PokemonState).GetField("_enemyTrapTurns", F)!.GetValue(p)!;
        Check("조이기: 4~5턴 묶임", trapTurns is 4 or 5 && p.BattleMessage.Contains("갇혔"), $"{trapTurns}턴, {p.BattleMessage}");
        await Finish(p);
        Check("조이기: 턴이 끝나면 최대 체력 1/8 데미지", p.EnemyHp == bh0 - 62, $"HP {bh0} → {p.EnemyHp}");
        for (int i = 1; i < trapTurns; i++) await Finish(p);
        int afterAll = p.EnemyHp; await Finish(p);
        Check("조이기: 턴이 다 되면 풀림", afterAll == bh0 - 62 * trapTurns && p.EnemyHp == afterAll, $"HP {bh0} → {afterAll} → {p.EnemyHp}");
        p = New(GASTLY, RATTATA); await EnemyAct(p, bind);
        Check("0배면 묶이지 않음", (int)typeof(PokemonState).GetField("_playerTrapTurns", F)!.GetValue(p)! == 0, "");
        p = New(RATTATA, PIKACHU); await EnemyAct(p, bind); p.IsPlayerTurn = true;
        await p.ExecuteTurnAsync(BattleAction.Run);
        Check("묶이면 도망칠 수 없고 턴도 안 씀", p.IsBattleOpen && p.IsPlayerTurn && p.BattleMessage.Contains("도망칠 수 없"), p.BattleMessage);
        p = New(RATTATA, PIKACHU); p.EnemyHp = 1; await PlayerAct(p, bind);
        Check("쓰러뜨리면 묶지 않음", (int)typeof(PokemonState).GetField("_enemyTrapTurns", F)!.GetValue(p)! == 0, "");
        Check("묶기 기술 번호 확인", new[] { 20, 35, 83, 128, 250, 328, 463 }.All(i => SkillDex.GetSkill(i)?.IsTrapping == true), string.Join(",", new[] { 20, 35, 83, 128, 250, 328, 463 }.Select(i => SkillDex.GetSkill(i)?.NameKo)));

        // 1, 2: 내 공격 0배 → 데미지 0, 추가 효과 없음
        p = New(PIKACHU, SANDSHREW);
        await PlayerAct(p, Sure(85)); // 10만볼트 → 땅
        Check("내 공격 0배 (10만볼트→모래두지)", p.EnemyHp == 500 && p.EnemyAilment == SkillAilment.None, $"HP {p.EnemyHp}, {p.EnemyAilment}, \"{p.BattleMessage}\"");
        p = New(RATTATA, GASTLY);
        await PlayerAct(p, Sure(33)); // 몸통박치기 → 고스트
        Check("내 공격 0배 (몸통박치기→고오스)", p.EnemyHp == 500, $"HP {p.EnemyHp}, \"{p.BattleMessage}\"");
        // 상대 공격 0배
        p = New(SANDSHREW, PIKACHU);
        await EnemyAct(p, Sure(85));
        Check("상대 공격 0배 (10만볼트→내 모래두지)", p.PlayerHp == 500 && p.PlayerAilment == SkillAilment.None, $"HP {p.PlayerHp}, {p.PlayerAilment}, \"{p.BattleMessage}\"");
        // 대조군: 일반 공격은 그대로
        p = New(PIKACHU, RATTATA);
        await PlayerAct(p, Sure(85));
        Check("대조: 10만볼트→꼬렛은 데미지+마비", p.EnemyHp < 500 && p.EnemyAilment == SkillAilment.Paralysis, $"HP {p.EnemyHp}, {p.EnemyAilment}");
        // 3: 전기자석파 → 땅 무효, 다른 타입은 걸림
        p = New(PIKACHU, SANDSHREW);
        await PlayerAct(p, Sure(86));
        Check("전기자석파→땅 무효", p.EnemyAilment == SkillAilment.None, $"{p.EnemyAilment}, \"{p.BattleMessage}\"");
        p = New(SANDSHREW, PIKACHU);
        await EnemyAct(p, Sure(86));
        Check("상대 전기자석파→내 땅 무효", p.PlayerAilment == SkillAilment.None, $"{p.PlayerAilment}, \"{p.BattleMessage}\"");
        p = New(PIKACHU, RATTATA);
        await PlayerAct(p, Sure(86));
        Check("대조: 전기자석파→꼬렛 마비", p.EnemyAilment == SkillAilment.Paralysis, $"{p.EnemyAilment}");
        // 다른 변화 기술은 원작대로 0배 타입에도 통함
        p = New(RATTATA, GASTLY);
        await PlayerAct(p, Sure(47));
        Check("노래하기→고스트는 통함", p.EnemyAilment == SkillAilment.Sleep, $"{p.EnemyAilment}");
        p = New(RATTATA, GASTLY);
        await PlayerAct(p, Sure(45));
        Check("울음소리→고스트는 통함", Stages(p, "_enemyStages")[(int)BattleStat.Attack] == -1, $"공격 단계 {Stages(p, "_enemyStages")[0]}");
        // 4: 쓰러진 상대에게 상태이상 안 걸림
        p = New(PIKACHU, RATTATA); p.EnemyHp = 1;
        await PlayerAct(p, Sure(85));
        Check("쓰러진 상대에게 안 걸림", p.EnemyHp == 0 && p.EnemyAilment == SkillAilment.None, $"HP {p.EnemyHp}, {p.EnemyAilment}");
        p = New(RATTATA, PIKACHU); p.PlayerHp = 1;
        await EnemyAct(p, Sure(85));
        Check("쓰러진 내 포켓몬에게 안 걸림", p.PlayerHp == 0 && p.PlayerAilment == SkillAilment.None, $"HP {p.PlayerHp}, {p.PlayerAilment}");
    }

    [STAThread]
    static int Main()
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        var app = new System.Windows.Application { ShutdownMode = System.Windows.ShutdownMode.OnExplicitShutdown };
        Loc.Instance.SetLanguage(Loc.Korean);
        PokemonDex.LoadPokemonData(); SkillDex.LoadSkillData();
        Console.WriteLine($"moves {SkillDex.MoveDatabase.Count}, pikachu {PokemonDex.Get(PIKACHU)?.Type1}, sandshrew {PokemonDex.Get(SANDSHREW)?.Type1}, gastly {PokemonDex.Get(GASTLY)?.Type1}/{PokemonDex.Get(GASTLY)?.Type2}");
        try { Task.Run(Run).GetAwaiter().GetResult(); }
        catch (Exception ex) { Console.WriteLine(ex); fails++; }
        Console.WriteLine(fails == 0 ? "ALL PASS" : $"{fails} FAILED");
        return fails;
    }
}
