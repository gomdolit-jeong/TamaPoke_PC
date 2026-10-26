using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using TamaPoke.Utils;
using TamaPoke.Utils.Service;

namespace TamaPoke.Models
{
    public partial class PokemonState
    {
        #region 저장 및 불러오기 (Save & Load)
        public void InitializeAfterLoad()
        {
            IsPartyOpen = false;

            IsProfileOpen = false; IsFeedMenuOpen = false; IsPlayMenuOpen = false; IsDexOpen = false; IsBattleOpen = false;
            IsBallGameOpen = false; IsCatchGameOpen = false; IsMemoGameOpen = false; IsCleanGameOpen = false;
            IsBathing = false; IsPetting = false;

            if (Ceremony == 4) Ceremony = 0;
            // 이별/가출/방생 연출(10초) 도중 종료된 경우: 타이머가 다시 돌지 않으므로 바로 새 알로 넘어갑니다.
            else if (Ceremony != 0) PrepareNewEgg();
            if (UnlockedPokemon == null) UnlockedPokemon = new List<int>();
            if (UnlockedPokemon.Count == 0 && SpeciesId > 0) UnlockedPokemon.Add(SpeciesId);

            IsFarewellPostponed = false;

            MegaShards ??= new Dictionary<string, int>();
            MegaStones ??= new List<string>();
            TryGrantMegaRing(); // 메가링 기능 전에 배지를 모은 세이브
            // 예전 버전에서 메가/거다이맥스 등 배틀 전용 폼으로 태어난 포켓몬은 기본 폼으로 되돌립니다.
            if (SpeciesId > 0 && SpriteForms.IsBattleOnlyForm(_spriteFileName)) RefreshSpriteFileName();
            foreach (var member in Party)
            {
                if (member.SpeciesId > 0 && SpriteForms.IsBattleOnlyForm(member.SpriteFileName))
                    member.SpriteFileName = SpriteForms.ApplyVariants(AppPaths.GetSpriteForms(member.SpeciesId), SpriteForms.BaseFileName(member.SpeciesId), member.IsShiny, member.IsFemale);
            }

            if (DexTable.Count == 0)
            {
                InitializeEvolutionTable();
            }

            RepairLeaderSlot();
            UpdatePartyFirstFlags();

            InitializeBadgesList();
            RefreshPokedex(); UpdateDayNightCycle(); CheckDailyStreak(); UpdateBackgroundImage();

            EnsureMainLoopTimer();
            CheckStateAndAnimate(); _mainLoopTimer.Start(); Save();
        }

        private static readonly string SaveFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "save.json");
        private static readonly string BackupFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "save.bak.json");
        private static readonly string TempFilePath = SaveFilePath + ".tmp";
        public static bool CheckSaveFileExists() => File.Exists(SaveFilePath);

        public enum SaveLoadResult { NewGame, Loaded, RestoredFromBackup, Corrupted }

        // 마지막 Load() 결과와, 손상된 세이브를 옮겨 둔 파일 이름 (시작 화면에서 안내용)
        public static SaveLoadResult LastLoadResult { get; private set; }
        public static string? CorruptSaveFileName { get; private set; }

        public void Save()
        {
            // 저장 직전에 메인 포켓몬의 최신 상태를 Party[0]에 반영합니다.
            SyncMainToLeader();
            try
            {
                // 임시 파일에 먼저 쓰고 교체합니다. 저장 중 꺼져도 기존 세이브는 깨지지 않고, 직전 세이브는 save.bak.json으로 남습니다.
                var options = new JsonSerializerOptions { WriteIndented = true };
                File.WriteAllText(TempFilePath, JsonSerializer.Serialize(this, options));
                if (!File.Exists(SaveFilePath)) File.Move(TempFilePath, SaveFilePath);
                else
                {
                    try { File.Replace(TempFilePath, SaveFilePath, BackupFilePath, true); }
                    catch (IOException) // 교체를 지원하지 않는 드라이브: 백업 복사 후 덮어쓰기
                    {
                        File.Copy(SaveFilePath, BackupFilePath, true);
                        File.Copy(TempFilePath, SaveFilePath, true);
                        File.Delete(TempFilePath);
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine($"저장 실패: {ex.Message}"); }
        }

        public static PokemonState Load()
        {
            LastLoadResult = SaveLoadResult.NewGame;
            CorruptSaveFileName = null;

            if (File.Exists(SaveFilePath))
            {
                var pet = TryLoadFrom(SaveFilePath);
                if (pet != null)
                {
                    LastLoadResult = SaveLoadResult.Loaded;
                    return pet;
                }

                // 세이브가 깨졌으면 덮어쓰지 않도록 옮겨 두고, 직전 백업으로 복구를 시도합니다.
                CorruptSaveFileName = PreserveCorruptFile(SaveFilePath);
                pet = File.Exists(BackupFilePath) ? TryLoadFrom(BackupFilePath) : null;
                if (pet != null)
                {
                    LastLoadResult = SaveLoadResult.RestoredFromBackup;
                    return pet;
                }
                if (File.Exists(BackupFilePath)) PreserveCorruptFile(BackupFilePath);
                LastLoadResult = SaveLoadResult.Corrupted;
            }

            PokemonState newPet = new PokemonState();
            newPet.InitializeInventory();
            newPet.InitializeAfterLoad();
            return newPet;
        }

        // 세이브 파일을 읽어 초기화까지 마칩니다. 읽을 수 없으면 null.
        private static PokemonState? TryLoadFrom(string path)
        {
            try
            {
                string json = File.ReadAllText(path);
                var pet = JsonSerializer.Deserialize<PokemonState>(json);
                if (pet == null) return null;

                // 아이템은 배틀로만 얻습니다. 가방이 비어 있어도 다시 채우지 않고,
                // 가방 기능이 생기기 전의 세이브(Inventory 키 자체가 없음)만 기본 아이템을 지급합니다.
                bool hasInventoryKey;
                using (var doc = JsonDocument.Parse(json)) hasInventoryKey = doc.RootElement.TryGetProperty(nameof(Inventory), out _);
                if (!hasInventoryKey || pet.Inventory == null)
                {
                    pet.Inventory ??= new ObservableCollection<ItemInfo>();
                    pet.InitializeInventory();
                }
                else
                {
                    pet.MigrateLegacyInventory();
                }

                pet.InitializeAfterLoad();
                return pet;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"세이브 불러오기 실패 ({Path.GetFileName(path)}): {ex.Message}");
                return null;
            }
        }

        // 손상된 파일을 save.corrupt-날짜시간.json 형태로 옮겨 보관하고, 그 파일 이름을 돌려줍니다.
        private static string? PreserveCorruptFile(string path)
        {
            try
            {
                string name = $"{Path.GetFileNameWithoutExtension(path)}.corrupt-{DateTime.Now:yyyyMMdd-HHmmss}.json";
                File.Move(path, Path.Combine(Path.GetDirectoryName(path)!, name), true);
                return name;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"손상된 세이브 보관 실패: {ex.Message}");
                return null;
            }
        }
        #endregion
    }
}
