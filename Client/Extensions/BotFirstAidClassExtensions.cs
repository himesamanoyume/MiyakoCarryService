
using System.Linq;
using EFT.HealthSystem;
using EFT.InventoryLogic;

namespace MiyakoCarryService.Client.Extensions
{
    public static class BotFirstAidClassExtensions
    {
        extension(BotFirstAid botFirstAid)
        {
            public void McsRefreshMeds()
            {
                if (!botFirstAid._owner.Settings.FileSettings.Mind.CAN_USE_MEDS)
                {
                    return;
                }

                var healthController = botFirstAid._owner.GetPlayer.HealthController;
                botFirstAid._medsList.Clear();
                botFirstAid._owner.GetPlayer.InventoryController.GetAcceptableItemsNonAlloc(BotMedecine.anySlots, botFirstAid._medsList, null, null);

                if (botFirstAid._medsList.Count == 0)
                {
                    return;
                }

                if (healthController.FindExistingEffect<IHeavyBleeding>(EBodyPart.Common) != null)
                {
                    var med = botFirstAid.FindMedForEffect(EDamageEffectType.HeavyBleeding);
                    if (med != null)
                    {
                        botFirstAid._owner.Medecine.FirstAid.CurUsingMeds = med;
                        botFirstAid.CurUsingMeds = med;
                        return;
                    }
                }

                if (healthController.FindExistingEffect<ILightBleeding>(EBodyPart.Common) != null)
                {
                    var med = botFirstAid.FindMedForEffect(EDamageEffectType.LightBleeding);
                    if (med != null)
                    {
                        botFirstAid._owner.Medecine.FirstAid.CurUsingMeds = med;
                        botFirstAid.CurUsingMeds = med;
                        return;
                    }
                }

                if (botFirstAid.McsIsFullHp())
                {
                    if (healthController.FindExistingEffect<IFracture>(EBodyPart.Common) != null)
                    {
                        var med = botFirstAid.McsFindSplint();
                        if (med != null)
                        {
                            botFirstAid._owner.Medecine.FirstAid.CurUsingMeds = med;
                            botFirstAid.CurUsingMeds = med;
                            return;
                        }
                    }
                }

                var medKitItemClasses = botFirstAid._medsList.OfType<MedKit>().ToList();

                var medKitItemClass = medKitItemClasses.FirstOrDefault((kit) =>
                {
                    var healthEffectsComponent = kit.HealthEffectsComponent;
                    var array = new EDamageEffectType[2];
                    array[0] = EDamageEffectType.LightBleeding;
                    return healthEffectsComponent.AffectsAny(array);
                });

                if (medKitItemClass != null)
                {
                    botFirstAid.CurUsingMeds = medKitItemClass;
                    return;
                }
                botFirstAid.CurUsingMeds = medKitItemClasses.FirstOrDefault();
            }

            public bool McsIsFullHp()
            {
                var healthController = botFirstAid._owner.GetPlayer.HealthController;
                foreach (var part in botFirstAid.parts)
                {
                    var health = healthController.GetBodyPartHealth(part, false);
                    if (health.Current < health.Maximum)
                    {
                        return false;
                    }
                }
                return true;
            }

            public Meds McsFindSplint()
            {
                foreach (var med in botFirstAid._medsList)
                {
                    if (!med.TryGetItemComponent(out HealthEffectsComponent healthEffectsComponent))
                    {
                        continue;
                    }
                    if (healthEffectsComponent.DamageEffects.ContainsKey(EDamageEffectType.DestroyedPart))
                    {
                        continue;
                    }
                    if (botFirstAid.CanTreatEffect(med, EDamageEffectType.Fracture))
                    {
                        return med;
                    }
                }
                return null;
            }

            private Meds FindMedForEffect(EDamageEffectType effect)
            {
                foreach (var med in botFirstAid._medsList)
                {
                    if (botFirstAid.CanTreatEffect(med, effect))
                    {
                        return med;
                    }
                }
                return null;
            }

            private bool CanTreatEffect(Meds med, EDamageEffectType effect)
            {
                if (!med.TryGetItemComponent(out HealthEffectsComponent healthComponent))
                {
                    return false;
                }
                if (!healthComponent.DamageEffects.TryGetValue(effect, out var damageEffect))
                {
                    return false;
                }
                if (med.TryGetItemComponent(out MedKitComponent medKit))
                {
                    return medKit.HpResource >= damageEffect.Cost;
                }
                return true;
            }
        }
    }
}
