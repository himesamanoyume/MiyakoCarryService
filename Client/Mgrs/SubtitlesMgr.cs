
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using EFT;
using EFT.UI;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Models;
using MiyakoCarryService.Client.Utils;
using UnityEngine;
using Il2CppInterop.Runtime.Injection;

namespace MiyakoCarryService.Client.Mgrs
{
    public class SubtitlesMgr : BaseMgr
    {
        public SubtitlesMgr(IntPtr pointer) : base(pointer)
        {
            ClassInjector.DerivedConstructorBody(this);
        }

        private SubtitlesView _view;
        private Transform _templateTransform;
        private readonly Dictionary<string, SubtitleLine> _lines = new();
        private readonly Dictionary<string, string> _knownNicknames = new();
        private string _renderedText = string.Empty;
        private bool _dirty;
        private float _nextViewTryAt;
        private Dictionary<EPhraseTrigger, string> _talkContents;
        private Dictionary<EPhraseTrigger, Func<string, McsMsg, Player, Player, string>> _phraseHandleMaps;
        private McsMgr McsMgr => field ??= MgrAccessor.Get<McsMgr>();

        private const float LineLifetime = 4f;
        private const float SamePhraseCooldown = 2f;

        private sealed class SubtitleLine
        {
            public string Nickname;
            public string Text;
            public float ExpireAt;
            public EPhraseTrigger LastPhraseTrigger;
            public float NextRepeatAt;
        }

        protected override void OnMgrStart()
        {
            base.OnMgrStart();
            _talkContents = new()
            {
                { EPhraseTrigger.None, "未知的回应，应进行反馈。" },
                { EPhraseTrigger.OnFirstContact, Locales.ONFIRSTCONTACT },
                { EPhraseTrigger.Roger, Locales.ROGER },
                { EPhraseTrigger.OnPosition, Locales.ONPOSITION },
                { EPhraseTrigger.GoLoot, Locales.GOLOOT },
                { EPhraseTrigger.OnLoot, Locales.ONLOOT },
                { EPhraseTrigger.LootGeneric, Locales.LOOTGENERIC },
                { EPhraseTrigger.Clear, Locales.CLEAR },
                { EPhraseTrigger.LeftFlank, Locales.LEFTFLANK },
                { EPhraseTrigger.RightFlank, Locales.RIGHTFLANK },
                { EPhraseTrigger.InTheFront, Locales.INTHEFRONT },
                { EPhraseTrigger.OnSix, Locales.ONSIX },
                { EPhraseTrigger.EnemyDown, Locales.ENEMYDOWN },
                { EPhraseTrigger.Going, Locales.GOING },
                { EPhraseTrigger.HoldPosition, Locales.HOLDPOSITION },
                { EPhraseTrigger.Regroup, Locales.REGROUP },
                { EPhraseTrigger.StartHeal, Locales.STARTHEAL },
                { EPhraseTrigger.OnFriendlyDown, Locales.ONFRIENDLYDOWN },
                { EPhraseTrigger.FollowMe, Locales.FOLLOWME },
                { EPhraseTrigger.Negative, Locales.NEGATIVE },
                { EPhraseTrigger.Mine, Locales.MINE },
                { EPhraseTrigger.OnFight, Locales.ONFIGHT },
                { EPhraseTrigger.OnOutOfAmmo, Locales.ONOUTOFAMMO },
                // 空短语、临时内容，用于传递任意Key实现任何对话内容
                { EPhraseTrigger.PhraseNone, "PhraseNone" }
            };

            _phraseHandleMaps = new()
            {
                { EPhraseTrigger.OnFirstContact, HandleOnFirstContact },
                { EPhraseTrigger.OnLoot, HandleOnLoot },
                { EPhraseTrigger.LootGeneric, HandleLootGeneric },
                { EPhraseTrigger.PhraseNone, HandlePhraseNone },
                { EPhraseTrigger.Mine, HandlePhraseMine },
                { EPhraseTrigger.Going, HandlePhraseWithOnFight },
                { EPhraseTrigger.Negative, HandlePhraseWithOnFight },
            };
        }

        public override void OnMgrDestroy()
        {
            base.OnMgrDestroy();
            if (_view != null)
            {
                Destroy(_view.gameObject);
                _view = null;
            }
            _templateTransform = null;
            _lines.Clear();
            _renderedText = string.Empty;
        }

        void Update()
        {
            if (_view == null)
            {
                if (Time.time < _nextViewTryAt)
                {
                    return;
                }

                _nextViewTryAt = Time.time + 1f;
                TryCreateView();
                if (_view == null)
                {
                    return;
                }
            }

            if (_templateTransform != null)
            {
                _view.transform.position = _templateTransform.position;
                SyncTemplateScale();
            }

            if (!MiyakoCarryServicePlugin.EnableSubtitles.Value)
            {
                if (_lines.Count > 0)
                {
                    _lines.Clear();
                    _dirty = true;
                }

                if (_dirty)
                {
                    Render();
                }

                return;
            }

            var now = Time.time;
            List<string> expired = null;
            foreach (var line in _lines)
            {
                if (now >= line.Value.ExpireAt)
                {
                    expired ??= new List<string>();
                    expired.Add(line.Key);
                }
            }

            if (expired != null)
            {
                foreach (var id in expired)
                {
                    _lines.Remove(id);
                }

                _dirty = true;
            }

            if (_dirty)
            {
                Render();
            }
        }

        private void SyncTemplateScale()
        {
            if (_view == null || _templateTransform == null)
            {
                return;
            }

            var templateScale = _templateTransform.lossyScale;
            var parent = _view.transform.parent;
            var parentScale = parent == null ? Vector3.one : parent.lossyScale;

            _view.transform.localScale = new Vector3(
                parentScale.x == 0f ? templateScale.x : templateScale.x / parentScale.x,
                parentScale.y == 0f ? templateScale.y : templateScale.y / parentScale.y,
                parentScale.z == 0f ? templateScale.z : templateScale.z / parentScale.z);
        }

        private void TryCreateView()
        {
            try
            {
                var controller = ItemUiContext.Instance?.SubtitlesController;
                var template = controller?._subtitlesView;
                if (template == null)
                {
                    return;
                }

                var clone = Instantiate(template.gameObject);
                clone.name = "McsBotPlayerSubtitles";

                var parent = template.transform.parent;
                if (parent != null && parent.parent != null)
                {
                    parent = parent.parent;
                }

                clone.transform.SetParent(parent, true);

                _view = clone.GetComponent<SubtitlesView>();
                if (_view == null)
                {
                    _view = clone.GetComponentInChildren<SubtitlesView>(true);
                }

                if (_view == null)
                {
                    Destroy(clone);
                    return;
                }

                _templateTransform = template.transform;

                _renderedText = string.Empty;
                _dirty = false;
                _view.HideGameObject();
            }
            catch (Exception ex)
            {
                McsLogger.LogError($"[SubtitlesMgr.TryCreateView] {ex}");
            }
        }

        private void Render()
        {
            _dirty = false;

            if (_view == null || _view._textField == null)
            {
                return;
            }

            if (_lines.Count == 0)
            {
                if (_renderedText.Length != 0)
                {
                    _renderedText = string.Empty;
                    _view._textField.text = string.Empty;
                }

                _view.HideGameObject();
                return;
            }

            var stringBuilder = new StringBuilder();
            foreach (var line in _lines.Values)
            {
                if (stringBuilder.Length > 0)
                {
                    stringBuilder.Append('\n');
                }

                stringBuilder.Append("<b>").Append(line.Nickname).Append("</b>: ").Append(line.Text);
            }

            var text = stringBuilder.ToString();
            if (text != _renderedText)
            {
                _renderedText = text;
                _view._textField.text = text;
            }

            _view.ShowGameObject();
        }

        public void TalkMsg(Player mcsLeadPlayer, Player mcsBotPlayer, McsMsg msg)
        {
            if (!ShouldSilencePhrases(mcsLeadPlayer))
            {
                mcsBotPlayer.AIData.BotOwner.BotTalk.TrySay(msg.PhraseTrigger);
            }
            ShowMsg(mcsLeadPlayer, mcsBotPlayer, msg);
        }

        private bool ShouldSilencePhrases(Player mcsLeadPlayer)
        {
            return ShouldSilencePhrases(mcsLeadPlayer.ProfileId);
        }

        public bool ShouldSilencePhrases(string mcsLeadPlayerId)
        {
            if (McsMgr != null && McsMgr.McsLeadPlayerConfigs.TryGetValue(mcsLeadPlayerId, out var mcsBotPlayerConfig))
            {
                return mcsBotPlayerConfig.PhrasesSilent;
            }
            return MiyakoCarryServicePlugin.PhrasesSilent.Value;
        }

        public bool ShouldShowSubtitles(string mcsLeadPlayerId)
        {
            if (McsMgr != null && McsMgr.McsLeadPlayerConfigs.TryGetValue(mcsLeadPlayerId, out var mcsBotPlayerConfig))
            {
                return mcsBotPlayerConfig.EnableSubtitles;
            }
            return true;
        }

        public bool ShouldSilenceBot(Player mcsBotPlayer)
        {
            if (!Tools.IsHost)
            {
                return false;
            }

            if (mcsBotPlayer == null)
            {
                return false;
            }

            if (McsMgr == null || !McsMgr.IsMcsBotPlayer(mcsBotPlayer.ProfileId))
            {
                return false;
            }

            var mcsLeadPlayer = McsMgr.GetMcsLeadPlayerByMcsBotPlayerId(mcsBotPlayer.ProfileId);
            if (mcsLeadPlayer == null)
            {
                return false;
            }

            return ShouldSilencePhrases(mcsLeadPlayer.ProfileId);
        }

        public string HandleOnFirstContact(string content, McsMsg msg, Player mcsLeadPlayer, Player mcsBotPlayer)
        {
            var toEnemy = msg.Position.Value - mcsLeadPlayer.Position;
            var flatToEnemy = new Vector3(toEnemy.x, 0, toEnemy.z);
            var flatDirection = new Vector3(mcsLeadPlayer.InteractionRay.direction.x, 0, mcsLeadPlayer.InteractionRay.direction.z);

            flatToEnemy.Normalize();
            flatDirection.Normalize();

            var dot = Vector3.Dot(flatDirection, flatToEnemy);
            var cross = Vector3.Cross(flatDirection, flatToEnemy);
            var angleThreshold = 0.707f;
            if (dot >= angleThreshold)
            {
                content += Locales.INTHEFRONT.McsLocalized();
            }
            else if (dot <= -angleThreshold)
            {
                content += Locales.ONSIX.McsLocalized();
            }
            else
            {
                if (cross.y > 0)
                {
                    content += Locales.RIGHTFLANK.McsLocalized();
                }
                else
                {
                    content += Locales.LEFTFLANK.McsLocalized();
                }
            }
            return content;
        }

        public string HandleOnLoot(string content, McsMsg msg, Player mcsLeadPlayer, Player mcsBotPlayer)
        {
            content = string.Format(content, msg.Keys[0].McsLocalized(), msg.Keys[1].McsLocalized());
            return content;
        }

        public string HandleLootGeneric(string content, McsMsg msg, Player mcsLeadPlayer, Player mcsBotPlayer)
        {
            content = string.Format(content, msg.Keys[0].McsLocalized());
            return content;
        }

        public string HandlePhraseNone(string content, McsMsg msg, Player mcsLeadPlayer, Player mcsBotPlayer)
        {
            var sb = new StringBuilder();
            foreach (var key in msg.Keys)
            {
                sb.Append(key.McsLocalized());
            }
            return sb.ToString();
        }

        public string HandlePhraseMine(string content, McsMsg msg, Player mcsLeadPlayer, Player mcsBotPlayer)
        {
            content = string.Format(content, msg.Keys[0], msg.Keys[1]);
            var healthStates = JsonSerializer.Deserialize<List<HealthState>>(msg.Keys[2], new JsonSerializerOptions() { IncludeFields = true });
            if (healthStates.Count > 0)
            {
                content += " " + string.Join(", ", healthStates.Select(h => $"{h.BodyPart.McsLocalized()} {h.EffectType.McsLocalized()}"));
            }
            return content;
        }

        public string HandlePhraseWithOnFight(string content, McsMsg msg, Player mcsLeadPlayer, Player mcsBotPlayer)
        {
            if (msg.Keys != null)
            {
                foreach (var key in msg.Keys)
                {
                    content += key.McsLocalized();
                }
            }
            return content;
        }

        public void ShowMsg(Player mcsLeadPlayer, Player mcsBotPlayer, McsMsg msg)
        {
            if (!MiyakoCarryServicePlugin.EnableSubtitles.Value)
            {
                return;
            }

            _talkContents.TryGetValue(msg.PhraseTrigger, out var talkContent);
            talkContent = talkContent.McsLocalized();
            if (string.IsNullOrEmpty(talkContent))
            {
                return;
            }

            if (_phraseHandleMaps.TryGetValue(msg.PhraseTrigger, out var action))
            {
                talkContent = action(talkContent, msg, mcsLeadPlayer, mcsBotPlayer);
            }

            var profileId = mcsBotPlayer.Profile.Id;
            var now = Time.time;

            if (_lines.TryGetValue(profileId, out var existing) && existing.LastPhraseTrigger == msg.PhraseTrigger && now < existing.NextRepeatAt)
            {
                return;
            }

            if (!_knownNicknames.TryGetValue(profileId, out var nickname))
            {
                nickname = mcsBotPlayer.Profile.McsNickname;
                _knownNicknames[profileId] = nickname;
            }

            _lines[profileId] = new SubtitleLine
            {
                Nickname = nickname,
                Text = talkContent,
                ExpireAt = now + LineLifetime,
                LastPhraseTrigger = msg.PhraseTrigger,
                NextRepeatAt = now + SamePhraseCooldown,
            };

            _dirty = true;
        }

        public void CreateSubTitle(Profile mcsBotPlayerProfile)
        {
            if (mcsBotPlayerProfile == null)
            {
                return;
            }

            _knownNicknames[mcsBotPlayerProfile.Id] = mcsBotPlayerProfile.McsNickname;
        }

        public override void OnRaidEnded()
        {
            base.OnRaidEnded();
            _lines.Clear();
            _dirty = true;
            Render();
        }
    }
}