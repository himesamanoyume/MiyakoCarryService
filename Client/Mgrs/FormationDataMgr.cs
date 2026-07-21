
using System.Collections.Generic;
using EFT;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Events;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Models;
using MiyakoCarryService.Client.Utils;
using SPT.Common.Utils;

namespace MiyakoCarryService.Client.Mgrs
{
    public class FormationDataMgr : DataMgr
    {
        public override void Start()
        {
            base.Start();
            LoadFormationPreset();
        }

        public override void OnGameWorldEnded(GameWorldEndedEvent @event)
        {
            OnRaidEnded();
        }

        void Update()
        {
            if (KeyInput.BetterIsDown(MiyakoCarryServicePlugin.SaveFormationPresetHotKey.Value))
            {
                AddFormation("New Formation", MiyakoCarryServicePlugin.FormationMatrix.Value);
                NotificationManagerClass.DisplayMessageNotification(Locales.SAVEFORMATIONPRESET.McsLocalized());
            }
        }

        public void LoadFormationPreset()
        {
            var formationDataDtos = Json.Deserialize<List<FormationDataDto>>(MiyakoCarryServicePlugin.FormationPresets.Value);
            DataClear();
            foreach (var formationDataDto in formationDataDtos)
            {
                _datas.Add(new FormationData(formationDataDto.Id, formationDataDto.Name, formationDataDto.FormationMatrix));
            }
        }

        public void SaveFormationPresets()
        {
            List<FormationDataDto> formationDataDtos = new();
            foreach (FormationData formationData in _datas)
            {
                var formationDataDto = new FormationDataDto
                {
                    Id = formationData.Id,
                    Name = formationData.Name,
                    FormationMatrix = formationData.FormationMatrix
                };
                formationDataDtos.Add(formationDataDto);
            }
            MiyakoCarryServicePlugin.FormationPresets.Value = Json.Serialize(formationDataDtos);
            LoadFormationPreset();
        }

        public void SaveFormationPreset(MongoID id, string rename, string formationMatrix)
        {
            foreach (FormationData formationData in _datas)
            {
                if (formationData.Id == id)
                {
                    formationData.Name = rename;
                    formationData.FormationMatrix = formationMatrix;
                    break;
                }
            }
            SaveFormationPresets();
        }

        public void AddFormation(string name, string formationMatrix)
        {
            _datas.Add(new FormationData(name, formationMatrix));
            SaveFormationPresets();
        }

        public void DeleteFormation(FormationData formationData)
        {
            _datas.Remove(formationData);
            SaveFormationPresets();
        }

        public FormationData GetFormationData(MongoID id)
        {
            foreach (FormationData formationData in _datas)
            {
                if (formationData.Id == id)
                {
                    return formationData;
                }
            }
            return null;
        }

        public void ApplyFormationData(MongoID id)
        {
            var formationData = GetFormationData(id);
            if (formationData == null)
            {
                return;
            }
            MiyakoCarryServicePlugin.FormationMatrix.Value = formationData.FormationMatrix;
        }
    }
}