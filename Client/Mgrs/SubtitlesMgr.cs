
using System;
using System.Collections;
using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.UI;
using HarmonyLib;
using MiyakoCarryService.Client.Events;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Models;
using MiyakoCarryService.Client.Utils;
using TMPro;
using UnityEngine;

namespace MiyakoCarryService.Client.Mgrs
{
    public sealed class SubtitlesMgr : BaseMgr<SubtitlesMgr>
    {
        private GameObject _mcsDialogScreen;
        private Transform _subsContainer;
        private GameObject _subtitlesViewTemplate;
        private Dictionary<MongoID, Subtitles> _subTitles = new();
        private Dictionary<EPhraseTrigger, string> _talkContents;
        private Dictionary<EPhraseTrigger, Func<string, McsMsg, Player, string>> _phraseHandleMaps;

        public sealed override void Start()
        {
            base.Start();
            StartCoroutine(Init());
            _talkContents = new()
            {
                {EPhraseTrigger.None, "未知的回应。应向Discord频道提出反馈。"},
                {EPhraseTrigger.OnFirstContact, Locales.ONFIRSTCONTACT},
                {EPhraseTrigger.Roger, Locales.ROGER},
                {EPhraseTrigger.OnPosition, Locales.ONPOSITION},
                {EPhraseTrigger.GoLoot, Locales.GOLOOT},
                {EPhraseTrigger.OnLoot, Locales.ONLOOT},
                {EPhraseTrigger.LootGeneric, Locales.LOOTGENERIC},
                {EPhraseTrigger.Clear, Locales.CLEAR},
                {EPhraseTrigger.LeftFlank, Locales.LEFTFLANK},
                {EPhraseTrigger.RightFlank, Locales.RIGHTFLANK},
                {EPhraseTrigger.InTheFront, Locales.INTHEFRONT},
                {EPhraseTrigger.OnSix, Locales.ONSIX},
                {EPhraseTrigger.EnemyDown, Locales.ENEMYDOWN},
                {EPhraseTrigger.Going, Locales.GOING},
                {EPhraseTrigger.HoldPosition, Locales.HOLDPOSITION},
                {EPhraseTrigger.Regroup, Locales.REGROUP},
                {EPhraseTrigger.StartHeal, Locales.STARTHEAL},
                {EPhraseTrigger.OnFriendlyDown, Locales.ONFRIENDLYDOWN},
                {EPhraseTrigger.FollowMe, Locales.FOLLOWME},
                {EPhraseTrigger.Negative, Locales.NEGATIVE},
                // 空短语、临时内容，用于传递任意Key实现任何对话内容
                {EPhraseTrigger.PhraseNone, "PhraseNone"}
            };

            _phraseHandleMaps = new()
            {
                {EPhraseTrigger.OnFirstContact, HandleOnFirstContact},
                {EPhraseTrigger.OnLoot, HandleOnLoot},
                {EPhraseTrigger.LootGeneric, HandleLootGeneric},
                {EPhraseTrigger.PhraseNone, HandlePhraseNone},
            };
        }

        private McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

        private IEnumerator Init()
        {
            var publicTime = new WaitForSeconds(1f);
            while (true)
            {
                yield return publicTime;
                if (MonoBehaviourSingleton<CommonUI>.Instantiated)
                {
                    var traderDialogScreen = MonoBehaviourSingleton<CommonUI>.Instance.TraderDialogScreen;
                    var oldTraderDialogScreenGameObject = traderDialogScreen.gameObject;
                    _mcsDialogScreen = Instantiate(oldTraderDialogScreenGameObject, MonoBehaviourSingleton<CommonUI>.Instance.transform.GetChild(0));
                    var _traderDialogScreen = _mcsDialogScreen.transform.GetComponentInChildren<TraderDialogScreen>();
                    Destroy(_traderDialogScreen);
                    _mcsDialogScreen.name = "Mcs Subtitles Screen";
                    for (int i = 0; i < _mcsDialogScreen.transform.childCount; i++)
                    {
                        var childContainer = _mcsDialogScreen.transform.GetChild(i);
                        if (childContainer.transform.name == "SubsContainer")
                        {
                            _subsContainer = childContainer;
                            break;
                        }
                    }

                    var subtitlesView = _subsContainer.GetChild(0).GetComponent<SubtitlesView>();
                    _subtitlesViewTemplate = subtitlesView.gameObject;
                    _subtitlesViewTemplate.transform.name = "McsBotPlayerSubtitles";
                    _mcsDialogScreen.SetActive(true);
                    subtitlesView.HideGameObject();
                    break;
                }
            }
        }

        public void TalkMsg(Player mcsLeadPlayer, Player mcsBotPlayer, McsMsg msg)
        {
            if (MiyakoCarryServicePlugin.FikaInstalled)
            {
                if (McsMgr.IsHost)
                {
                    var myPlayer = Singleton<GameWorld>.Instance.MainPlayer;
                    if (myPlayer != null && McsMgr.IsMyMcsBotPlayer(myPlayer.ProfileId, mcsBotPlayer.ProfileId))
                    {
                        mcsBotPlayer.AIData.BotOwner.BotTalk.TrySay(msg.PhraseTrigger);
                        ShowMsg(mcsLeadPlayer, mcsBotPlayer, msg);
                    }
                    else
                    {
                        EventMgr.Notify(new SubtitlesMgrHandleFikaEvent
                        {
                            McsLeadPlayerId = mcsLeadPlayer.ProfileId,
                            McsBotPlayerId = mcsBotPlayer.ProfileId,
                            Msg = msg
                        });
                    }
                }
            }
            else
            {
                mcsBotPlayer.AIData.BotOwner.BotTalk.TrySay(msg.PhraseTrigger);
                ShowMsg(mcsLeadPlayer, mcsBotPlayer, msg);
            }
        }

        public string HandleOnFirstContact(string content, McsMsg msg, Player mcsLeadPlayer)
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

        public string HandleOnLoot(string content, McsMsg msg, Player mcsLeadPlayer)
        {
            content = string.Format(Locales.ONLOOT.McsLocalized(), msg.Key.McsLocalized(), msg.Key2.McsLocalized());
            return content;
        }

        public string HandleLootGeneric(string content, McsMsg msg, Player mcsLeadPlayer)
        {
            content = string.Format(Locales.LOOTGENERIC.McsLocalized(), msg.Key.McsLocalized());
            return content;
        }

        public string HandlePhraseNone(string content, McsMsg msg, Player mcsLeadPlayer)
        {
            return msg.Key.McsLocalized();
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
                talkContent = action(talkContent, msg, mcsLeadPlayer);
            }

            if (!_subTitles.ContainsKey(mcsBotPlayer.Profile.Id))
            {
                CreateSubTitle(mcsBotPlayer.Profile);
            }

            if (_subTitles.TryGetValue(mcsBotPlayer.Profile.Id, out var subTitle))
            {
                if (subTitle.CurrentMsg() == talkContent)
                {
                    return;
                }

                subTitle.Show(talkContent, msg.PhraseTrigger);
            }
        }

        public void CreateSubTitle(Profile mcsBotPlayerProfile)
        {
            var cloneSubtitleViewGameObject = Instantiate(_subtitlesViewTemplate);
            var cloneSubtitleView = cloneSubtitleViewGameObject.GetComponentInChildren<SubtitlesView>();

            cloneSubtitleViewGameObject.transform.SetParent(_subsContainer);

            var cloneSubTitle = new Subtitles(_gameloop, cloneSubtitleView, mcsBotPlayerProfile);
            cloneSubTitle.Hide(0f);
            _subTitles[mcsBotPlayerProfile.Id] = cloneSubTitle;
        }

        protected override void OnRaidEnded()
        {
            base.OnRaidEnded();
            if (_subTitles != null)
            {
                foreach (var subTitle in _subTitles.Values)
                {
                    Destroy(subTitle.SubtitlesView.gameObject);
                }
                _subTitles.Clear();
            }
        }

        public class Subtitles
        {
            public SubtitlesView SubtitlesView;
            private TMP_Text _textField;
            private GameLoop _gameLoop;
            private Coroutine _coroutine;
            private EPhraseTrigger _lastPhraseTrigger;
            private Profile _mcsBotPlayerProfile;
            private float _colddown;

            public Subtitles(GameLoop gameLoop, SubtitlesView subtitlesView, Profile mcsBotPlayerProfile)
            {
                _lastPhraseTrigger = EPhraseTrigger.None;
                _mcsBotPlayerProfile = mcsBotPlayerProfile;
                _colddown = 0;
                SubtitlesView = subtitlesView;
                _gameLoop = gameLoop;
                var subtitlesViewTraverse = Traverse.Create(subtitlesView);
                _textField = subtitlesViewTraverse.Field<TMP_Text>("_textField").Value;
            }

            public void Show(string msg, EPhraseTrigger talkContentType)
            {
                if (_lastPhraseTrigger == talkContentType)
                {
                    if (Time.time < _colddown)
                    {
                        return;
                    }
                }

                Hide(0f);
                if (_coroutine != null)
                {
                    _gameLoop.StopCoroutine(_coroutine);
                }
                _textField.text = $"<b>{_mcsBotPlayerProfile.Nickname}</b>: " + msg;
                if (SubtitlesView != null)
                {
                    SubtitlesView.ShowGameObject();
                    _colddown = Time.time + 2f;
                    _lastPhraseTrigger = talkContentType;
                    _coroutine = _gameLoop.StartCoroutine(Hide(4f));
                }
            }

            public string CurrentMsg()
            {
                return _textField.text;
            }

            public IEnumerator Hide(float time)
            {
                yield return new WaitForSeconds(time);
                if (SubtitlesView != null)
                {
                    SubtitlesView.HideGameObject();
                    _textField.text = string.Empty;
                }
            }
        }
    }
}