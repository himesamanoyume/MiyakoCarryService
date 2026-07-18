
using System.Linq;
using EFT.InventoryLogic;

namespace MiyakoCarryService.Client.Extensions
{
    public static class BotFirstAidClassExtensions
    {
        extension(BotFirstAidClass botFirstAidClass)
        {
            public void McsRefreshMeds()
            {
                if (!botFirstAidClass.BotOwner_0.Settings.FileSettings.Mind.CAN_USE_MEDS)
                {
                    return;
                }

                var healthController = botFirstAidClass.BotOwner_0.GetPlayer.HealthController;
                botFirstAidClass.List_0.Clear();
                botFirstAidClass.BotOwner_0.GetPlayer.InventoryController.GetAcceptableItemsNonAlloc(BotMedecine.anySlots, botFirstAidClass.List_0, null, null);

                if (botFirstAidClass.List_0.Count == 0)
                {
                    return;
                }

                if (healthController.FindExistingEffect<HeavyBleedEffect>(EBodyPart.Common) != null)
                {
                    var med = botFirstAidClass.FindMedForEffect(EDamageEffectType.HeavyBleeding);
                    if (med != null)
                    {
                        botFirstAidClass.BotOwner_0.Medecine.FirstAid.CurUsingMeds = med;
                        botFirstAidClass.CurUsingMeds = med;
                        return;
                    }
                }

                if (healthController.FindExistingEffect<LightBleedEffect>(EBodyPart.Common) != null)
                {
                    var med = botFirstAidClass.FindMedForEffect(EDamageEffectType.LightBleeding);
                    if (med != null)
                    {
                        botFirstAidClass.BotOwner_0.Medecine.FirstAid.CurUsingMeds = med;
                        botFirstAidClass.CurUsingMeds = med;
                        return;
                    }
                }

                if (healthController.FindExistingEffect<Fracture>(EBodyPart.Common) != null)
                {
                    var med = botFirstAidClass.FindMedForEffect(EDamageEffectType.Fracture);
                    if (med != null)
                    {
                        botFirstAidClass.BotOwner_0.Medecine.FirstAid.CurUsingMeds = med;
                        botFirstAidClass.CurUsingMeds = med;
                        return;
                    }
                }

                var medKitItemClasses = botFirstAidClass.List_0.OfType<MedKitItemClass>().ToList();

                var medKitItemClass = medKitItemClasses.FirstOrDefault((kit) =>
                {
                    var healthEffectsComponent = kit.HealthEffectsComponent;
                    var array = new EDamageEffectType[2];
                    array[0] = EDamageEffectType.LightBleeding;
                    return healthEffectsComponent.AffectsAny(array);
                });

                if (medKitItemClass != null)
                {
                    botFirstAidClass.CurUsingMeds = medKitItemClass;
                    return;
                }
                botFirstAidClass.CurUsingMeds = medKitItemClasses.FirstOrDefault();
            }

            private MedsItemClass FindMedForEffect(EDamageEffectType effect)
            {
                foreach (var med in botFirstAidClass.List_0)
                {
                    if (botFirstAidClass.CanTreatEffect(med, effect))
                    {
                        return med;
                    }
                }
                return null;
            }

            private bool CanTreatEffect(MedsItemClass med, EDamageEffectType effect)
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
