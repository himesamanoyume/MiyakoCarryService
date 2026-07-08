
using System.Linq;
using EFT.InventoryLogic;

namespace MiyakoCarryService.Client.Extensions
{
    public static class BotFirstAidClassExtensions
    {
        extension(BotFirstAid botFirstAid)
        {
            public void McsRefreshMeds()
            {
                if (!botFirstAid.botOwner_0.Settings.FileSettings.Mind.CAN_USE_MEDS)
                {
                    return;
                }

                var healthController = botFirstAid.botOwner_0.GetPlayer.HealthController;
                botFirstAid.list_0.Clear();
                botFirstAid.botOwner_0.GetPlayer.InventoryController.GetAcceptableItemsNonAlloc(BotMedecine.anySlots, botFirstAid.list_0, null, null);

                if (botFirstAid.list_0.Count == 0)
                {
                    return;
                }

                if (healthController.FindExistingEffect<HeavyBleedEffect>(EBodyPart.Common) != null)
                {
                    var med = botFirstAid.FindMedForEffect(EDamageEffectType.HeavyBleeding);
                    if (med != null)
                    {
                        botFirstAid.botOwner_0.Medecine.FirstAid.CurUsingMeds = med;
                        botFirstAid.CurUsingMeds = med;
                        return;
                    }
                }

                if (healthController.FindExistingEffect<LightBleedEffect>(EBodyPart.Common) != null)
                {
                    var med = botFirstAid.FindMedForEffect(EDamageEffectType.LightBleeding);
                    if (med != null)
                    {
                        botFirstAid.botOwner_0.Medecine.FirstAid.CurUsingMeds = med;
                        botFirstAid.CurUsingMeds = med;
                        return;
                    }
                }

                var medKitItemClasses = botFirstAid.list_0.OfType<MedKit>().ToList();

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

            private Meds FindMedForEffect(EDamageEffectType effect)
            {
                foreach (var med in botFirstAid.list_0)
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
