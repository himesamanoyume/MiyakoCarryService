
using System;
using System.Collections;
using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.UI;
using HarmonyLib;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Utils;
using TMPro;
using UnityEngine;

namespace MiyakoCarryService.Client.Mgrs
{
    public sealed class SubTitleMgr : BaseMgr<SubTitleMgr>
    {
        private GameObject _mcsDialogScreen;
        private Transform _subsContainer;
        private GameObject _subtitlesViewTemplate;
        private Dictionary<MongoID, SubTitle> _subTitles = new();
        private Dictionary<EPhraseTrigger, string> _talkContents;
        private Dictionary<EPhraseTrigger, Func<string, Vector3, Player, string>> _phrasePosHandleMaps;
        public Action<MongoID, MongoID, EPhraseTrigger, Vector3?> HandleFikaEvent;

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
                {EPhraseTrigger.Clear, Locales.CLEAR},
                {EPhraseTrigger.LeftFlank, Locales.LEFTFLANK},
                {EPhraseTrigger.RightFlank, Locales.RIGHTFLANK},
                {EPhraseTrigger.InTheFront, Locales.INTHEFRONT},
                {EPhraseTrigger.OnSix, Locales.ONSIX},
                {EPhraseTrigger.EnemyDown, Locales.ENEMYDOWN},
                {EPhraseTrigger.Going, Locales.GOING},
                {EPhraseTrigger.HoldPosition, Locales.HOLDPOSITION},
                // {EPhraseTrigger.LootBody, Locales.FOUNDHIGHVALUELOOT},
                // {EPhraseTrigger.LootContainer , Locales.FOUNDHIGHVALUELOOT},
                // {EPhraseTrigger.LootGeneric , Locales.FOUNDHIGHVALUELOOT},
                // {EPhraseTrigger.LootNothing , Locales.FOUNDHIGHVALUELOOT},
                {EPhraseTrigger.Regroup, Locales.REGROUP},
                {EPhraseTrigger.StartHeal, Locales.STARTHEAL}
            };

            _phrasePosHandleMaps = new()
            {
                {EPhraseTrigger.OnFirstContact, HandleOnFirstContact}
            };
        }

        private McsMgr McsMgr
        {
            get
            {
                return field ??= GameLoop.Instance.GetMgr<McsMgr>();
            }
        }

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
                    _mcsDialogScreen.name = "Mcs SubTitle Screen";
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
                    _subtitlesViewTemplate.transform.name = "McsBotPlayerSubtitle";
                    _mcsDialogScreen.SetActive(true);
                    subtitlesView.HideGameObject();
                    break;
                }
            }
        }

        public void TalkMsg(Player mcsLeadPlayer, Player mcsBotPlayer, EPhraseTrigger phraseTrigger, Vector3? position = null)
        {
            if (MiyakoCarryServicePlugin.FikaInstalled)
            {
                if (McsMgr.IsHost)
                {
                    if (McsMgr.IsMyMcsBotPlayer(Singleton<GameWorld>.Instance.MainPlayer.ProfileId, mcsBotPlayer.ProfileId))
                    {
                        ShowMsg(mcsLeadPlayer, mcsBotPlayer, phraseTrigger, position);
                    }
                    else
                    {
                        HandleFikaEvent(mcsLeadPlayer.ProfileId, mcsBotPlayer.ProfileId, phraseTrigger, position);
                    }
                }
            }
            else
            {
                ShowMsg(mcsLeadPlayer, mcsBotPlayer, phraseTrigger);
            }
        }

        public string HandleOnFirstContact(string content, Vector3 enemyPos, Player mcsLeadPlayer)
        {
            var toEnemy = enemyPos - mcsLeadPlayer.Position;
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

        public void ShowMsg(Player mcsLeadPlayer, Player mcsBotPlayer, EPhraseTrigger phraseTrigger, Vector3? position = null)
        {
            _talkContents.TryGetValue(phraseTrigger, out var talkContent);
            talkContent = talkContent.McsLocalized();
            if (string.IsNullOrEmpty(talkContent))
            {
                return;
            }

            if (_phrasePosHandleMaps.TryGetValue(phraseTrigger, out var action))
            {
                talkContent = action(talkContent, position.Value, mcsLeadPlayer);
            }
            
            if (_subTitles.TryGetValue(mcsBotPlayer.Profile.Id, out var subTitle))
            {
                if (subTitle.CurrentMsg() == talkContent)
                {
                    return;
                }

                subTitle.Show(talkContent, phraseTrigger);
            }
        }

        public void CreateSubTitle(Profile mcsBotPlayerProfile)
        {
            var cloneSubtitleViewGameObject = Instantiate(_subtitlesViewTemplate);
            var cloneSubtitleView = cloneSubtitleViewGameObject.GetComponentInChildren<SubtitlesView>();

            cloneSubtitleViewGameObject.transform.SetParent(_subsContainer);

            var cloneSubTitle = new SubTitle(_gameloop, cloneSubtitleView, mcsBotPlayerProfile);
            cloneSubTitle.Hide(0f);
            _subTitles[mcsBotPlayerProfile.Id] = cloneSubTitle;
        }

        protected override void OnRaidEnded()
        {
            base.OnRaidEnded();
            foreach (var subTitle in _subTitles.Values)
            {
                Destroy(subTitle.SubtitlesView.gameObject);
            }
            _subTitles.Clear();
        }

        public class SubTitle
        {
            public SubtitlesView SubtitlesView;
            private TMP_Text _textField;
            private GameLoop _gameLoop;
            private Coroutine _coroutine;
            private EPhraseTrigger _lastPhraseTrigger;
            private Profile _mcsBotPlayerProfile;
            private float _colddown;

            public SubTitle(GameLoop gameLoop, SubtitlesView subtitlesView, Profile mcsBotPlayerProfile)
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
                SubtitlesView.ShowGameObject();
                _colddown = Time.time + 2f;
                _lastPhraseTrigger = talkContentType;
                _coroutine = _gameLoop.StartCoroutine(Hide(4f));
            }

            public string CurrentMsg()
            {
                return _textField.text;
            }

            public IEnumerator Hide(float time)
            {
                yield return new WaitForSeconds(time);
                SubtitlesView.HideGameObject();
                _textField.text = string.Empty;
            }
        }
    }
}