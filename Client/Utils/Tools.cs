

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Comfort.Common;
using EFT;
using EFT.Ballistics;
using EFT.Counters;
using EFT.InventoryLogic;
using MiyakoCarryService.Client.Datas;
using MiyakoCarryService.Client.Enums;
using MiyakoCarryService.Client.Extensions;
using MiyakoCarryService.Client.Mgrs;
using UnityEngine;
using UnityEngine.AI;

namespace MiyakoCarryService.Client.Utils
{
    public static class Tools
    {
        private static McsMgr McsMgr => MgrAccessor.Get<McsMgr>();

        public static bool IsHost => McsMgr.IsHost;
        private static ConcurrentDictionary<string, int> _formationOpenCells = new();

        public static bool IsPlayerInventory(string stringTemplateId)
        {
            return stringTemplateId == ItemTpl.DefaultInventory;
        }

        public static bool IsBlockItem(EBlockItemType blockItemType, LootData lootData)
        {
            var itemType = lootData.ItemType;
            if (blockItemType == 0)
            {
                return false;
            }

            if (blockItemType.HasFlag(EBlockItemType.All))
            {
                return true;
            }

            if (blockItemType.HasFlag(EBlockItemType.Other) && (itemType == EItemType.Special || itemType == EItemType.All || itemType == EItemType.None))
            {
                return true;
            }

            return itemType switch
            {
                EItemType.Ammo => blockItemType.HasFlag(EBlockItemType.Ammo),
                EItemType.Barter => blockItemType.HasFlag(EBlockItemType.Barter),
                EItemType.Container => blockItemType.HasFlag(EBlockItemType.Container),
                EItemType.Food => blockItemType.HasFlag(EBlockItemType.Food),
                EItemType.Backpack => blockItemType.HasFlag(EBlockItemType.Backpack),
                EItemType.Goggles => blockItemType.HasFlag(EBlockItemType.Goggles),
                EItemType.Rig => blockItemType.HasFlag(EBlockItemType.Rig),
                EItemType.Armor => blockItemType.HasFlag(EBlockItemType.Armor),
                EItemType.Equipment => lootData.Item is Headphones ? blockItemType.HasFlag(EBlockItemType.Headphone) : blockItemType.HasFlag(EBlockItemType.TacticalVest),
                EItemType.Grenade => blockItemType.HasFlag(EBlockItemType.Grenade),
                EItemType.Info => blockItemType.HasFlag(EBlockItemType.Info),
                EItemType.Keys => blockItemType.HasFlag(EBlockItemType.Keys),
                EItemType.Knife => blockItemType.HasFlag(EBlockItemType.Knife),
                EItemType.Magazine => blockItemType.HasFlag(EBlockItemType.Magazine),
                EItemType.Meds => blockItemType.HasFlag(EBlockItemType.Meds),
                EItemType.Mod => blockItemType.HasFlag(EBlockItemType.Mod),
                EItemType.Special => blockItemType.HasFlag(EBlockItemType.Special),
                EItemType.Weapon => blockItemType.HasFlag(EBlockItemType.Weapon),
                _ => false
            };
        }

        public static string GetBodyPartTypeLocales(BodyPartType bodyPartType)
        {
            return bodyPartType switch
            {
                BodyPartType.head => Locales.HEAD,
                BodyPartType.body => Locales.BODY,
                BodyPartType.leftArm => Locales.LEFTARM,
                BodyPartType.leftLeg => Locales.LEFTLEG,
                BodyPartType.rightArm => Locales.RIGHTARM,
                BodyPartType.rightLeg => Locales.RIGHTLEG,
                _ => ""
            };
        }

        public static List<ItemData> GetAllOwnerItemData()
        {
            var result = new List<ItemData>();
            var itemOwners = Singleton<GameWorld>.Instance.ItemOwners;

            foreach (var owner in itemOwners)
            {
                var itemData = owner.Key.RootItem.GetData();

                if (itemData != null)
                {
                    result.Add(itemData);
                }
            }

            return result;
        }

        public static List<ItemData> GetRangeOwnerItemData(Vector3 playerPos, float distance)
        {
            var result = new List<ItemData>();
            var itemOwners = Singleton<GameWorld>.Instance.ItemOwners;

            foreach (var owner in itemOwners)
            {
                var itemData = owner.Key.RootItem.GetData();

                if (itemData == null || itemData.RootTransform == null)
                {
                    continue;
                }

                if (itemData.RootTransform.position.McsSqrDistance(playerPos) >= distance * distance)
                {
                    continue;
                }

                if (itemData is PlayerData playerData)
                {
                    if (McsMgr.IsMcsLeadPlayer(playerData.Player.ProfileId) || playerData.Player.HealthController.IsAlive)
                    {
                        continue;
                    }
                }
                result.Add(itemData);
            }
            return result;
        }

        /// <summary>
        /// 借鉴LootingBots
        /// </summary>
        public static bool BetterDestination(float maxDistance, Vector3 destination, out Vector3 betterDestination)
        {
            var pointNearbyContainer = NavMesh.SamplePosition(
                destination,
                out NavMeshHit navMeshAlignedPoint,
                maxDistance,
                NavMesh.AllAreas
            ) ? navMeshAlignedPoint.position : Vector3.zero;

            var padding = destination - pointNearbyContainer;
            padding.y = 0;
            padding.Normalize();

            betterDestination = NavMesh.SamplePosition(
                destination - (padding * 1.5f),
                out navMeshAlignedPoint,
                maxDistance,
                navMeshAlignedPoint.mask
            ) ? navMeshAlignedPoint.position : pointNearbyContainer;

            return betterDestination != Vector3.zero;
        }

        public static Vector3? GetPosNearTarget(Vector3 targetPos, BotOwner botOwner = null)
        {
            Vector3? result = null;
            var xOffset = MyExtensions.Random(1f, 4f) * MyExtensions.RandomSing();
            var zOffset = MyExtensions.Random(1f, 4f) * MyExtensions.RandomSing();
            var newPos = targetPos + new Vector3(xOffset, 0f, zOffset);

            for (int attempt = 0; attempt < 30; attempt++)
            {
                if (BetterDestination(3f, newPos, out var pos))
                {
                    if (Mathf.Abs(pos.y - targetPos.y) <= 2f)
                    {
                        result = pos;
                        break;
                    }
                }
            }

            if (result == null && NavMesh.SamplePosition(newPos, out var navMeshHit, 3f, -1))
            {
                result = navMeshHit.position;
            }

            if (result.HasValue)
            {
                var leadDir = new Vector3();
                if (botOwner != null)
                {
                    leadDir = botOwner.Position - result.Value;
                    leadDir.y = 0;
                    leadDir = leadDir.normalized * 2f;
                }

                if (NavMesh.Raycast(result.Value, leadDir + result.Value, out var rayHit, -1))
                {
                    result = rayHit.position;
                }
                else
                {
                    result = leadDir + result.Value;
                }

                return result;
            }
            return targetPos;
        }

        public static void HandleSharedExperience(Player killedPlayer, IPlayer aggressor, bool fikaSharedKillExp = false, bool fikaSharedBossExp = false)
        {
            if (McsMgr.IsMcsMemberPlayer(aggressor.ProfileId, out var mcsLeadPlayer))
            {
                if (mcsLeadPlayer == null)
                {
                    return;
                }

                if (!mcsLeadPlayer.HealthController.IsAlive)
                {
                    return;
                }

                var settings = killedPlayer.Profile.Info.Settings;
                var countAsBoss = settings.Role.CountAsBossForStatistics() && !(settings.Role is WildSpawnType.pmcUSEC or WildSpawnType.pmcBEAR);
                var experience = settings.Experience;
                var sessionCounters = mcsLeadPlayer.Profile.EftStats.SessionCounters;

                if (experience <= 0)
                {
                    experience = Singleton<GlobalConfiguration>.Instance.Experience.Kill.VictimBotLevelExp;
                }

                if (!countAsBoss)
                {
                    sessionCounters.AddLong(1L, PredefinedCounters.Kills);
                    sessionCounters.AddInt(fikaSharedKillExp ? experience - experience / 2 : experience, PredefinedCounters.ExpKillBase);
                }

                if (countAsBoss)
                {
                    sessionCounters.AddLong(1L, PredefinedCounters.Kills);
                    sessionCounters.AddInt(fikaSharedBossExp ? experience - experience / 2 : experience, PredefinedCounters.ExpKillBase);
                }
            }
        }

        public static void HandleSharedQuestCondition(Player killedPlayer, IPlayer aggressor, DamageInfo damageInfo, EBodyPart bodyPart, bool easyKillConditions = true)
        {
            if (!easyKillConditions)
            {
                return;
            }

            if (McsMgr.IsMcsMemberPlayer(aggressor.ProfileId, out var mcsLeadPlayer))
            {
                if (mcsLeadPlayer == null)
                {
                    return;
                }

                if (!mcsLeadPlayer.HealthController.IsAlive)
                {
                    return;
                }

                var settings = killedPlayer.Profile.Info.Settings;
                var playerSide = killedPlayer.Side;

                if (settings.Role != WildSpawnType.pmcBEAR)
                {
                    if (settings.Role == WildSpawnType.pmcUSEC)
                    {
                        playerSide = EPlayerSide.Usec;
                    }
                }
                else
                {
                    playerSide = EPlayerSide.Bear;
                }

                List<string> list = ["Any"];
                switch (playerSide)
                {
                    case EPlayerSide.Usec:
                        list.Add("Usec");
                        list.Add("AnyPmc");
                        list.Add("Enemy");
                        break;
                    case EPlayerSide.Bear:
                        list.Add("Bear");
                        list.Add("AnyPmc");
                        list.Add("Enemy");
                        break;
                    case EPlayerSide.Savage:
                        list.Add("Savage");
                        list.Add("Bot");
                        break;
                }

                foreach (var target in list)
                {
                    mcsLeadPlayer.QuestController.CheckKillConditionCounter(target, killedPlayer.ProfileId,
                        killedPlayer.Inventory.EquippedInSlotsTemplateIds, damageInfo.Weapon, bodyPart, mcsLeadPlayer.Location,
                        Vector3.Distance(aggressor.Position, killedPlayer.Position), settings.Role.ToStringNoBox(),
                        mcsLeadPlayer.CurrentHour, killedPlayer.HealthController.BodyPartEffects,
                        aggressor.HealthController.BodyPartEffects, killedPlayer.TriggerZones, aggressor.HealthController.ActiveBuffsNames());
                }
            }
        }

        private static bool SimulateToHorizontalDistance(Vector3 firePort, Vector3 aimDir, float muzzleVelocity, AmmoTemplate ammo, float targetHorizDist, out float timeOfFlight, out float bulletY)
        {
            timeOfFlight = -1f;
            bulletY = firePort.y;

            var calc = new TrajectoryCalculator();
            try
            {
                calc.Initialize(firePort, aimDir.normalized * muzzleVelocity, ammo.BulletMassGram, ammo.BulletDiameterMilimeters, ammo.BallisticCoeficient, true);

                var prev = calc.Current;
                for (int i = 0; i < 800; i++)
                {
                    var cur = calc.Next();
                    var horiz = new Vector3(cur.position.x - firePort.x, 0f, cur.position.z - firePort.z).magnitude;
                    if (horiz >= targetHorizDist)
                    {
                        var prevHoriz = new Vector3(prev.position.x - firePort.x, 0f, prev.position.z - firePort.z).magnitude;
                        var t = (horiz - prevHoriz) < 1e-4f ? 0f : Mathf.InverseLerp(prevHoriz, horiz, targetHorizDist);
                        timeOfFlight = Mathf.Lerp(prev.time, cur.time, t);
                        bulletY = Mathf.Lerp(prev.position.y, cur.position.y, t);
                        return true;
                    }
                    prev = cur;
                }
                timeOfFlight = prev.time;
                bulletY = prev.position.y;
                return false;
            }
            finally
            {
                calc.ClearClass();
            }
        }

        public static Vector3 GetPredictedAimPoint(Vector3 firePort, Vector3 targetPos, Vector3 targetVelocity, AmmoTemplate ammo, float muzzleVelocity)
        {
            if (ammo == null || muzzleVelocity <= 0f)
            {
                return targetPos;
            }

            var predicted = targetPos;
            for (int i = 0; i < 5; i++)
            {
                var dir = predicted - firePort;
                var horiz = new Vector3(dir.x, 0f, dir.z).magnitude;
                if (horiz < 0.01f)
                {
                    break;
                }
                if (!SimulateToHorizontalDistance(firePort, dir, muzzleVelocity, ammo, horiz, out var tof, out _) || tof <= 0f)
                {
                    return targetPos;
                }
                predicted = targetPos + targetVelocity * tof;
            }

            var lead = predicted - targetPos;
            if (lead.magnitude > 8f)
            {
                predicted = targetPos + lead.normalized * 8f;
            }

            var pdir = predicted - firePort;
            var phoriz = new Vector3(pdir.x, 0f, pdir.z).magnitude;
            if (phoriz >= 0.01f && SimulateToHorizontalDistance(firePort, pdir, muzzleVelocity, ammo, phoriz, out _, out var bulletY))
            {
                var drop = predicted.y - bulletY;
                predicted += Vector3.up * drop;
            }

            return predicted;
        }

        public static Vector3? ComputeTarget(Player mcsLeadPlayer, Vector3 basePos, int botIndex, int[] matrix, float spacing)
        {
            if (mcsLeadPlayer == null || botIndex < 5 || botIndex > 8 || matrix == null)
            {
                return null;
            }

            var leadRow = 3;
            var leadCol = 3;
            var leadCell = Array.IndexOf(matrix, -1);
            if (leadCell >= 0)
            {
                leadRow = leadCell / 7;
                leadCol = leadCell % 7;
            }

            var cell = -1;
            for (int i = 0; i < matrix.Length; i++)
            {
                if (matrix[i] == botIndex)
                {
                    cell = i;
                    break;
                }
            }

            if (cell < 0)
            {
                return null;
            }

            var row = cell / 7;
            var col = cell % 7;
            var dCol = col - leadCol;
            var dRow = row - leadRow;

            var forward = mcsLeadPlayer.LookDirection;
            forward.y = 0f;
            if (forward.sqrMagnitude < 0.0001f)
            {
                forward = mcsLeadPlayer.Transform.forward;
                forward.y = 0f;
            }
            forward.Normalize();

            var rot = Quaternion.LookRotation(forward, Vector3.up);
            var offset = rot * new Vector3(dCol * spacing, 0f, -dRow * spacing);
            return basePos + offset;
        }

        public static int GetMcsBotPlayerIndex(MongoID mcsBotPlayerId, bool sequentialFill = false)
        {
            return McsMgr.GetMcsBotPlayerIndex(mcsBotPlayerId, sequentialFill);
        }

        public static string ResetFormationMatrix()
        {
            var arr = new int[7 * 7];
            arr[3 * 7 + 3] = -1;
            arr[2 * 7 + 1] = 5;
            arr[2 * 7 + 2] = 6;
            arr[2 * 7 + 4] = 7;
            arr[2 * 7 + 5] = 8;
            return SerializeFormationMatrix(arr);
        }

        public static int[] ParseFormationMatrix(string raw)
        {
            var arr = new int[7 * 7];
            if (string.IsNullOrEmpty(raw))
            {
                return arr;
            }

            var parts = raw.Split(',');
            for (int i = 0; i < arr.Length && i < parts.Length; i++)
            {
                if (int.TryParse(parts[i], out var v) && (v == -1 || v == 0 || (v >= 5 && v <= 8)))
                {
                    arr[i] = v;
                }
            }
            return arr;
        }

        public static string SerializeFormationMatrix(int[] arr) => string.Join(",", arr);

        public static void FormationMatrixSetCell(int[] arr, int index, int value)
        {
            if (value != -1 && value != 0 && (value < 5 || value > 8))
            {
                return;
            }

            if (arr[index] == value)
            {
                return;
            }

            if (value != 0)
            {
                for (int i = 0; i < arr.Length; i++)
                {
                    if (i != index && arr[i] == value)
                    {
                        arr[i] = 0;
                    }
                }
            }
            arr[index] = value;
        }

        public static void RemoveFormationOpenCell(string key)
        {
            _formationOpenCells.Remove(key, out var index);
        }

        public static string DrawFormationMatrix(string key, string formationMatrix)
        {
            var arr = ParseFormationMatrix(formationMatrix);
            var changed = false;

            GUILayout.BeginVertical();
            for (int row = 0; row < 7; row++)
            {
                GUILayout.BeginHorizontal();
                for (int col = 0; col < 7; col++)
                {
                    var index = row * 7 + col;
                    var label = arr[index] == -1 ? "★" : arr[index].ToString();

                    if (GUILayout.Button(label, GUILayout.Width(25), GUILayout.Height(25)))
                    {
                        _formationOpenCells.AddOrUpdate(key, index, (key, oldIndex) => oldIndex == index ? -1 : index);
                    }
                }
                GUILayout.EndHorizontal();
            }

            if (_formationOpenCells.GetOrAdd(key, -1) >= 0)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"#{_formationOpenCells.GetOrAdd(key, -1)}", GUILayout.Width(25), GUILayout.Height(25));

                var options = new[] { "★", "0", "5", "6", "7", "8" };
                var optionValues = new[] { -1, 0, 5, 6, 7, 8 };
                var current = arr[_formationOpenCells.GetOrAdd(key, -1)];
                var currentIndex = Array.IndexOf(optionValues, current);
                if (currentIndex < 0)
                {
                    currentIndex = 0;
                }

                var newIndex = GUILayout.SelectionGrid(currentIndex, options, options.Length);
                if (newIndex != currentIndex)
                {
                    FormationMatrixSetCell(arr, _formationOpenCells.GetOrAdd(key, -1), optionValues[newIndex]);
                    _formationOpenCells.AddOrUpdate(key, -1, (key, oldIndex) => -1);
                    changed = true;
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.EndVertical();

            if (changed)
            {
                return SerializeFormationMatrix(arr);
            }
            return formationMatrix;
        }
    }
}