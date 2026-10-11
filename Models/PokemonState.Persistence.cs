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
        public static bool CheckSaveFileExists() => File.Exists(SaveFilePath);

        public void Save()
        {
            // 저장 직전에 메인 포켓몬의 최신 상태를 Party[0]에 반영합니다.
            SyncMainToLeader();
            try { var options = new JsonSerializerOptions { WriteIndented = true }; File.WriteAllText(SaveFilePath, JsonSerializer.Serialize(this, options)); }
            catch (Exception ex) { Console.WriteLine($"저장 실패: {ex.Message}"); }
        }

        public static PokemonState Load()
        {
            if (File.Exists(SaveFilePath))
            {
                try
                {
                    string json = File.ReadAllText(SaveFilePath);
                    var pet = JsonSerializer.Deserialize<PokemonState>(json);
                    if (pet != null)
                    {
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
                }
                catch { }
            }

            PokemonState newPet = new PokemonState();
            newPet.InitializeInventory();
            newPet.InitializeAfterLoad();
            return newPet;
        }
        #endregion
    }
}
