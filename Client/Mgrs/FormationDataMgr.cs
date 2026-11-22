
using System.Collections.Generic;
using System.Text.Json;
using EFT.Communications;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Events;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Models;
using MiyakoCarryService.Client.Utils;
using System;
using Il2CppInterop.Runtime.Injection;
using UnityEngine;

namespace MiyakoCarryService.Client.Mgrs
{
    public class FormationDataMgr : DataMgr
    {
        public FormationDataMgr(IntPtr pointer) : base(pointer)
        {
            ClassInjector.DerivedConstructorBody(this);
        }

        protected override void OnMgrStart()
        {
            base.OnMgrStart();
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
                NotificationManager.DisplayMessageNotification(Locales.SAVEFORMATIONPRESET.McsLocalized(), textColor: new Il2CppSystem.Nullable<Color>());
            }
        }

        public void LoadFormationPreset()
        {
            var formationDataDtos = JsonSerializer.Deserialize<List<FormationDataDto>>(MiyakoCarryServicePlugin.FormationPresets.Value, new JsonSerializerOptions() { IncludeFields = true }) ?? new List<FormationDataDto>();

            _datas ??= new HashSet<BaseData>();
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
            MiyakoCarryServicePlugin.FormationPresets.Value = JsonSerializer.Serialize(formationDataDtos, new JsonSerializerOptions() { IncludeFields = true });
            LoadFormationPreset();
        }

        public void SaveFormationPreset(string id, string rename, string formationMatrix)
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

        public FormationData GetFormationData(string id)
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

        public void ApplyFormationData(string id)
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