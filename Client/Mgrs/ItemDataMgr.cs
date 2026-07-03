
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Utils;
using UnityEngine;

namespace MiyakoCarryService.Client.Mgrs
{
    public abstract class ItemDataMgr : DataMgr
    {
        public virtual void LoadItemData<K>() where K : BaseData
        {
            var datas = new HashSet<K>();
            foreach (var item in Tools.GetAllOwnerItemData())
            {
                if (item is K k)
                {
                    datas.Add(k);
                }
            }
            var dataLeft = _datas.Except(datas).ToList();
            var dataJoined = datas.Except(_datas).ToList();
            foreach (var data in dataLeft)
            {
                _datas.Remove(data);
            }
            foreach (var data in dataJoined)
            {
                _datas.Add(data);
            }
        }

        public virtual IEnumerator UpdateItemData(float time)
        {
            yield return new WaitForSeconds(time);
            var publicTime = new WaitForSeconds(.1f);
            if (Gameloop.IsVaildGameWorld)
            {
                var datasList = new List<BaseData>();
                if (_datas != null)
                {
                    datasList.AddRange(_datas);
                }
                int batchSize = Mathf.Clamp(Mathf.CeilToInt(_datas.Count / 10f), 8, 50);
                var baseDataBatches = new List<List<BaseData>>();
                for (int i = 0; i < _datas.Count; i += batchSize)
                {
                    int endIndex = Math.Min(i + batchSize, _datas.Count);
                    var batch = datasList.GetRange(i, endIndex - i);
                    baseDataBatches.Add(batch);
                }

                foreach (var batch in baseDataBatches)
                {
                    try
                    {
                        foreach (ItemData itemData in batch)
                        {
                            foreach (var mcsAILeadPlayer in McsMgr.GetAllMcsAILeadPlayer())
                            {
                                itemData.RefreshInteresting(mcsAILeadPlayer, false);
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Debug.LogException(e);
                    }
                    yield return publicTime;
                }
            }
            else
            {
                yield return null;
            }
        }
    }
}