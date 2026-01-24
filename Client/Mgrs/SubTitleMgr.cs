
// using System.Collections.Generic;
// using EFT;
// using EFT.UI;
// using UnityEngine;

// namespace MiyakoCarryService.Client.Mgrs
// {
//     internal sealed class SubTitleMgr : BaseMgr<SubTitleMgr>
//     {
//         private GameObject _mcsDialogScreen;
//         private Transform _subsContainer;
//         private GameObject _subtitlesViewTemplate;
//         private List<SubTitleStruct> _subTitleStructs = new();
//         private List<SubTitleStruct> _toRemoveSubTitleStructs = new();
//         private List<SubTitleStruct> _toAddSubTitleStructs = new();
//         public sealed override void Start()
//         {
//             base.Start();
//         }

//         void Update()
//         {
//             _toRemoveSubTitleStructs.Clear();
//             foreach (var subTitle in _toAddSubTitleStructs)
//             {
//                 _subTitleStructs.Add(subTitle);
//             }
//             _toAddSubTitleStructs.Clear();
//             foreach (var subTitle in _subTitleStructs)
//             {
//                 if (Time.time - subTitle.CreationTime > 4)
//                 {
//                     Destroy(subTitle.SubtitlesViewGameObject);
//                     _toRemoveSubTitleStructs.Add(subTitle);
//                 }
//             }
//             foreach (var subTitle in _toRemoveSubTitleStructs)
//             {
//                 _subTitleStructs.Remove(subTitle);
//             }
//         }

//         protected override void Reset()
//         {
            
//         }

//         private void Init()
//         {
//             if (MonoBehaviourSingleton<CommonUI>.Instantiated)
//             {
//                 var traderDialogScreen = MonoBehaviourSingleton<CommonUI>.Instance.TraderDialogScreen;
//                 var oldTraderDialogScreenGameObject = traderDialogScreen.gameObject;
//                 _mcsDialogScreen = Instantiate(oldTraderDialogScreenGameObject);
//                 var _traderDialogScreen = _mcsDialogScreen.transform.GetComponentInChildren<TraderDialogScreen>();
//                 Destroy(_traderDialogScreen);
//                 for (int i = 0; i < _mcsDialogScreen.transform.childCount; i++)
//                 {
//                     var childContainer = _mcsDialogScreen.transform.GetChild(i);
//                     if (childContainer.transform.name == "DialogContainer")
//                     {
//                         Destroy(childContainer);
//                         break;
//                     }
//                 }
//                 _mcsDialogScreen.name = "Sub Title Screen";
//                 for (int i = 0; i < _mcsDialogScreen.transform.childCount; i++)
//                 {
//                     var childContainer = _mcsDialogScreen.transform.GetChild(i);
//                     if (childContainer.transform.name == "SubsContainer")
//                     {
//                         _subsContainer = childContainer;
//                         break;
//                     }
//                 }

//                 var subtitlesView = _subsContainer.GetChild(0).GetComponent<SubtitlesView>();
//                 _subtitlesViewTemplate = subtitlesView.gameObject;
//                 _subtitlesViewTemplate.transform.name = "McsBotPlayerSubtitle";
//                 _mcsDialogScreen.transform.SetParent(MonoBehaviourSingleton<CommonUI>.Instance.transform.GetChild(0));
//                 subtitlesView.HideGameObject();
//             }
//         }

//         public async void ShowMcsBotPlayerMsg(string str)
//         {
//             if (_mcsDialogScreen == null)
//             {
//                 Init();
//             }

//             var subtitleData = new List<GClass4073>
//             {
//                 new GClass4073()
//                 {
//                     Key = str
//                 }
//             };
//             var cloneSubtitleViewGameObject = Instantiate(_subtitlesViewTemplate);
//             var cloneSubtitleView = cloneSubtitleViewGameObject.GetComponentInChildren<SubtitlesView>();
            
//             cloneSubtitleViewGameObject.transform.SetParent(_subsContainer);
//             cloneSubtitleView.method_0(new GClass3564()
//             {
//                 SubtitlesSource = ESubtitlesSource.None,
//                 CcData = subtitleData
//             });
//             _toAddSubTitleStructs.Add(new SubTitleStruct(cloneSubtitleViewGameObject, cloneSubtitleView));
//             cloneSubtitleView.ShowGameObject();
//         }

//         public struct SubTitleStruct
//         {
//             public float CreationTime = Time.time;
//             public GameObject SubtitlesViewGameObject;
//             public SubtitlesView SubtitlesView;

//             public SubTitleStruct(GameObject subtitlesViewGameObject, SubtitlesView subtitlesView)
//             {
//                 SubtitlesViewGameObject = subtitlesViewGameObject;
//                 SubtitlesView = subtitlesView;
//             }
//         }
//     }
// }