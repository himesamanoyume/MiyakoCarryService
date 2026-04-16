
using System;
using System.Collections;
using System.Collections.Generic;
using Comfort.Common;
using EFT;
using EFT.UI;
using HarmonyLib;
using MiyakoCarryService.Client.Enums;
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
        private Dictionary<ETalkContentType, string> _talkContents;
        public Action<MongoID, MongoID, ETalkContentType, Vector3?> HandleFikaEvent;

        public sealed override void Start()
        {
            base.Start();
            StartCoroutine(Init());
            _talkContents = new()
            {
                {ETalkContentType.EnemySpotted, Locales.ENEMYSPOTTED},
                {ETalkContentType.CopyThat, Locales.COPYTHAT},
                {ETalkContentType.FoundHighValueLoot, Locales.FOUNDHIGHVALUELOOT},
                {ETalkContentType.Regrouping, Locales.REGROUPING},
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

        public void TalkMsg(Profile mcsLeadPlayerProfile, Profile mcsBotPlayerProfile, ETalkContentType talkContentType, Vector3? position = null)
        {
            if (MiyakoCarryServicePlugin.FikaInstalled)
            {
                if (McsMgr.IsHost)
                {
                    if (McsMgr.IsMyMcsBotPlayer(Singleton<GameWorld>.Instance.MainPlayer.ProfileId, mcsBotPlayerProfile.Id))
                    {
                        ShowMsg(mcsLeadPlayerProfile, mcsBotPlayerProfile, talkContentType, position);
                    }
                    else
                    {
                        HandleFikaEvent(mcsLeadPlayerProfile.Id, mcsBotPlayerProfile.Id, talkContentType, position);
                    }
                }
            }
            else
            {
                ShowMsg(mcsLeadPlayerProfile, mcsBotPlayerProfile, talkContentType);
            }
        }

        public void ShowMsg(Profile mcsLeadPlayerProfile, Profile mcsBotPlayerProfile, ETalkContentType talkContentType, Vector3? position = null)
        {
            _talkContents.TryGetValue(talkContentType, out var msg);
            msg = msg.McsLocalized();
            if (string.IsNullOrEmpty(msg))
            {
                return;
            }
            
            if (_subTitles.TryGetValue(mcsBotPlayerProfile.Id, out var subTitle))
            {
                if (subTitle.CurrentMsg() == msg)
                {
                    return;
                }

                subTitle.Show(msg, talkContentType);
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
            private ETalkContentType _lastTalkContentType;
            private Profile _mcsBotPlayerProfile;
            private float _colddown;

            public SubTitle(GameLoop gameLoop, SubtitlesView subtitlesView, Profile mcsBotPlayerProfile)
            {
                _lastTalkContentType = ETalkContentType.EnemySpotted;
                _mcsBotPlayerProfile = mcsBotPlayerProfile;
                _colddown = 0;
                SubtitlesView = subtitlesView;
                _gameLoop = gameLoop;
                var subtitlesViewTraverse = Traverse.Create(subtitlesView);
                _textField = subtitlesViewTraverse.Field<TMP_Text>("_textField").Value;
            }

            public void Show(string msg, ETalkContentType talkContentType)
            {
                if (_lastTalkContentType == talkContentType)
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
                _lastTalkContentType = talkContentType;
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