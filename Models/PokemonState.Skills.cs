using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using TamaPoke.Utils;

namespace TamaPoke.Models
{
    public partial class PokemonState
    {
        #region 포켓몬 스킬 시스템 (Skills)
        private int[] _skills = new int[4] { 0, 0, 0, 0 };
        public int[] Skills
        {
            get => _skills;
            set => SetProperty(ref _skills, value);
        }

        public bool KnowsSkill(int skillId) => skillId != 0 && Array.Exists(Skills, s => s == skillId);
        [JsonIgnore] public int SkillCount => Skills.Count(s => s != 0);

        // 올바른 레벨업 디자인과 밸런스가 적용된 스킬 학습 시스템
        public void RelearnFromLevel()
        {
            for (int i = 0; i < 4; i++) Skills[i] = 0;

            if (IsEgg)
            {
                OnPropertyChanged(nameof(CurrentSkills));
                return;
            }

            var d = PokemonDex.AllPokemons.FirstOrDefault(x => x.Id == SpeciesId);
            if (d == null) return;

            int lvl = Level;
            int n = SkillDex.GetLearnCount(SpeciesId);

            if (n == 0)
            {
                Skills[0] = 1; // 몸통박치기
                int fallbackMove = SkillDex.GetFallbackMove(d.Type1);
                if (fallbackMove != 1) Skills[1] = fallbackMove;

                OnPropertyChanged(nameof(CurrentSkills));
                return;
            }

            int[] score = new int[4] { 0, 0, 0, 0 };

            // 1패스는 자력기(레벨업), 2패스는 TM(기술머신)으로 분리하여 순차적으로 채웁니다.
            for (int pass = 0; pass < 2; pass++)
            {
                bool tmPass = (pass == 1);
                for (int i = 0; i < n; i++)
                {
                    int at = SkillDex.GetLearnLevel(SpeciesId, i);

                    if (tmPass != (at == 0)) continue;
                    if (!tmPass && at > lvl) continue;
                    if (tmPass && lvl < 30) continue; // 강력한 TM은 30레벨 이상

                    int mv = SkillDex.GetLearnMove(SpeciesId, i);
                    if (mv == 0 || KnowsSkill(mv)) continue; // 🌟 MoveTable.Length 검사 삭제!

                    var m = SkillDex.GetSkill(mv);
                    if (m == null) continue; // 여기서 스킬이 도감에 있는지 완벽하게 검사합니다.

                    int sc = (m.Category == SkillCategory.Status) ? 10 : m.Power + 20;

                    if (m.Category != SkillCategory.Status && (m.Type == d.Type1 || m.Type == d.Type2)) sc += 40;

                    sc += (at == 0 ? 50 : at);

                    int slot = -1;
                    for (int s = 0; s < 4; s++)
                    {
                        if (sc > score[s]) { slot = s; break; }
                    }
                    if (slot < 0) continue;

                    for (int s = 3; s > slot; s--)
                    {
                        score[s] = score[s - 1];
                        Skills[s] = Skills[s - 1];
                    }
                    score[slot] = sc;
                    Skills[slot] = mv;
                }
            }

            // 만약 공격 스킬이 하나도 없다면 강제 배정
            bool hasAttack = false;
            for (int i = 0; i < 4; i++)
            {
                if (Skills[i] != 0 && SkillDex.GetSkill(Skills[i])?.Category != SkillCategory.Status)
                {
                    hasAttack = true; break;
                }
            }

            if (!hasAttack)
            {
                int slotToFill = 0;
                for (int i = 0; i < 4; i++)
                {
                    if (Skills[i] == 0) { slotToFill = i; break; }
                }

                int fallback = SkillDex.GetFallbackMove(d.Type1);
                Skills[slotToFill] = fallback > 0 ? fallback : 1;

                if (slotToFill < 3 && !KnowsSkill(1)) Skills[slotToFill + 1] = 1;
            }

            OnPropertyChanged(nameof(CurrentSkills));
        }

        // 🌟 UI 바인딩용 스킬 컬렉션
        [JsonIgnore]
        public ObservableCollection<SkillInfo> CurrentSkills
        {
            get
            {
                var list = new ObservableCollection<SkillInfo>();
                foreach (int skillId in Skills)
                {
                    if (skillId != 0)
                    {
                        var skill = SkillDex.GetSkill(skillId);
                        if (skill != null) list.Add(skill);
                    }
                }
                return list;
            }
        }
        #endregion

        #region 스킬 학습 및 교체 시스템 로직 (Skill Learning Logic)
        private bool _isSkillLearnMenuOpen = false;
        public bool IsSkillLearnMenuOpen { get => _isSkillLearnMenuOpen; set => SetProperty(ref _isSkillLearnMenuOpen, value); }

        private bool _isSkillReplaceMenuOpen = false;
        public bool IsSkillReplaceMenuOpen { get => _isSkillReplaceMenuOpen; set => SetProperty(ref _isSkillReplaceMenuOpen, value); }

        private SkillInfo? _recommendedSkill1;
        public SkillInfo? RecommendedSkill1 { get => _recommendedSkill1; set { SetProperty(ref _recommendedSkill1, value); OnPropertyChanged(nameof(HasRecommendedSkill1)); } }
        public bool HasRecommendedSkill1 => RecommendedSkill1 != null;

        private SkillInfo? _recommendedSkill2;
        public SkillInfo? RecommendedSkill2 { get => _recommendedSkill2; set { SetProperty(ref _recommendedSkill2, value); OnPropertyChanged(nameof(HasRecommendedSkill2)); } }
        public bool HasRecommendedSkill2 => RecommendedSkill2 != null;

        private SkillInfo? _skillToLearn;
        public void SelectRecommendedSkill(int optionNumber)
        {
            _skillToLearn = (optionNumber == 1) ? RecommendedSkill1 : RecommendedSkill2;
            if (_skillToLearn == null) return;

            if (SkillCount < 4)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (Skills[i] == 0) { Skills[i] = _skillToLearn.Id; break; }
                }
                OnPropertyChanged(nameof(CurrentSkills));
                FinishSkillLearning($"{_skillToLearn.Name}을(를) 깨우쳤다!");
            }
            else
            {
                IsSkillLearnMenuOpen = false;
                IsSkillReplaceMenuOpen = true;
                BattleMessage = $"기술이 4개라 꽉 찼다!\n{_skillToLearn.Name}을(를) 위해 어떤 기술을 지울까?";
            }
        }

        public void ReplaceExistingSkill(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex > 3 || _skillToLearn == null) return;

            var oldSkill = SkillDex.GetSkill(Skills[slotIndex]);
            string oldSkillName = oldSkill?.Name ?? "기술";

            Skills[slotIndex] = _skillToLearn.Id;
            OnPropertyChanged(nameof(CurrentSkills));

            FinishSkillLearning($"1, 2, 3... 짠!\n{oldSkillName}을(를) 잊고\n{_skillToLearn.Name}을(를) 배웠다!");
        }

        public void SkipSkillLearning()
        {
            FinishSkillLearning("새로운 스킬을 배우는 것을 포기했다.");
        }

        private async void FinishSkillLearning(string finalMessage)
        {
            IsSkillLearnMenuOpen = false;
            IsSkillReplaceMenuOpen = false;

            _ = ShowEventMessageAsync(finalMessage);
            await Task.Delay(2000);

            ProceedToCatchOrEnd();
        }

        #endregion
    }
}