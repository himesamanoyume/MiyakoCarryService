
using System.Collections.Generic;
using EFT;
using EFT.UI;
using UnityEngine;

namespace MiyakoCarryService.Client.Mgrs
{
    internal sealed class SubTitleMgr : BaseMgr<SubTitleMgr>
    {
        private GameObject _mcsDialogScreen;
        private Transform _subsContainer;
        private GameObject _subtitlesViewTemplate;
        private Dictionary<MongoID, SubTitle> _subTitles = new();
        public sealed override void Start()
        {
            base.Start();
        }

        void Update()
        {
            foreach (var subTitle in _subTitles.Values)
            {
                if (Time.time - subTitle.ShowTime > 4)
                {
                    subTitle.Hide();
                }
            }
        }

        private void Init()
        {
            if (MonoBehaviourSingleton<CommonUI>.Instantiated)
            {
                var traderDialogScreen = MonoBehaviourSingleton<CommonUI>.Instance.TraderDialogScreen;
                var oldTraderDialogScreenGameObject = traderDialogScreen.gameObject;
                _mcsDialogScreen = Instantiate(oldTraderDialogScreenGameObject);
                _mcsDialogScreen.transform.position = traderDialogScreen.transform.position;
                _mcsDialogScreen.transform.localScale = new(1, 1, 1);
                var _traderDialogScreen = _mcsDialogScreen.transform.GetComponentInChildren<TraderDialogScreen>();
                Destroy(_traderDialogScreen);
                for (int i = 0; i < _mcsDialogScreen.transform.childCount; i++)
                {
                    var childContainer = _mcsDialogScreen.transform.GetChild(i);
                    if (childContainer.transform.name == "DialogContainer")
                    {
                        childContainer.gameObject.SetActive(false);
                        // childContainer.SetParent(null);
                        // Destroy(childContainer);
                        break;
                    }
                }
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
                _mcsDialogScreen.transform.SetParent(MonoBehaviourSingleton<CommonUI>.Instance.transform.GetChild(0));
                _mcsDialogScreen.SetActive(true);
                subtitlesView.HideGameObject();
            }
        }

        public void ShowMcsBotPlayerMsg(MongoID mcsBotPlayerId, string msg)
        {
            var subtitleData = new List<GClass4073>
            {
                new GClass4073()
                {
                    Key = msg
                }
            };

            if (_subTitles.TryGetValue(mcsBotPlayerId, out var subTitle))
            {
                subTitle.SubtitlesView.method_0(new GClass3564()
                {
                    SubtitlesSource = ESubtitlesSource.None,
                    CcData = subtitleData
                });
                subTitle.Show();
            }
        }

        public void CreateSubTitle(MongoID mcsBotPlayerId)
        {
            if (_mcsDialogScreen == null)
            {
                Init();
            }

            var cloneSubtitleViewGameObject = Instantiate(_subtitlesViewTemplate);
            var cloneSubtitleView = cloneSubtitleViewGameObject.GetComponentInChildren<SubtitlesView>();

            cloneSubtitleViewGameObject.transform.SetParent(_subsContainer);

            var cloneSubTitle = new SubTitle(cloneSubtitleView);
            cloneSubTitle.Hide();
            _subTitles[mcsBotPlayerId] = cloneSubTitle;
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
            public float ShowTime = Time.time;
            public SubtitlesView SubtitlesView;

            public SubTitle(SubtitlesView subtitlesView)
            {
                SubtitlesView = subtitlesView;
            }

            public void Show()
            {
                ShowTime = Time.time;
                SubtitlesView.ShowGameObject();
            }

            public void Hide()
            {
                SubtitlesView.HideGameObject();
            }
        }
    }
}