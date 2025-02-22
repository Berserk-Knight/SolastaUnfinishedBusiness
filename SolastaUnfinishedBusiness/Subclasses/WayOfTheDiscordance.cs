﻿using System.Collections.Generic;
using System.Linq;
using SolastaUnfinishedBusiness.Api.GameExtensions;
using SolastaUnfinishedBusiness.Builders;
using SolastaUnfinishedBusiness.Builders.Features;
using SolastaUnfinishedBusiness.CustomBehaviors;
using SolastaUnfinishedBusiness.CustomInterfaces;
using SolastaUnfinishedBusiness.CustomUI;
using SolastaUnfinishedBusiness.Properties;
using static RuleDefinitions;
using static SolastaUnfinishedBusiness.Api.DatabaseHelper;


namespace SolastaUnfinishedBusiness.Subclasses;

internal sealed class WayOfTheDiscordance : AbstractSubclass
{
    private const string Name = "WayOfTheDiscordance";

    internal WayOfTheDiscordance()
    {
        var conditionDiscordance = ConditionDefinitionBuilder
            .Create($"Condition{Name}Discordance")
            .SetGuiPresentation(Category.Condition, ConditionDefinitions.ConditionMarkedByBrandingSmite)
            .SetConditionType(ConditionType.Detrimental)
            .SetSilent(Silent.WhenRemoved)
            .AllowMultipleInstances()
            .SetPossessive()
            .AddToDB();

        var powerDiscordanceDamage = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}DiscordanceDamage")
            .SetGuiPresentationNoContent(true)
            .SetUsesFixed(ActivationTime.NoCost)
            .SetEffectDescription(
                EffectDescriptionBuilder
                    .Create()
                    .SetParticleEffectParameters(SpellDefinitions.Bane)
                    .SetEffectForms(
                        EffectFormBuilder
                            .Create()
                            .SetDamageForm(DamageTypePsychic, 1, DieType.D4)
                            .Build())
                    .Build())
            .AddToDB();

        var powerDiscordance = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}DiscordanceBase")
            .SetGuiPresentationNoContent(true)
            .SetUsesFixed(ActivationTime.OnAttackHitAuto)
            .SetReactionContext((ReactionTriggerContext)ExtraReactionContext.Custom)
            .SetCustomSubFeatures(
                new AfterAttackEffectDiscordance(conditionDiscordance, powerDiscordanceDamage))
            .AddToDB();

        var powerBurstOfDisharmonyPool = FeatureDefinitionPowerBuilder
            .Create($"Power{Name}BurstOfDisharmony")
            .SetGuiPresentation(Category.Feature,
                Sprites.GetSprite("PowerBurstOfDisharmony", Resources.PowerBurstOfDisharmony, 128))
            .SetUsesFixed(ActivationTime.BonusAction)
            .AddToDB();

        var powerBurstOfDisharmonyList = new List<FeatureDefinitionPower>();

        for (var i = 6; i >= 1; i--)
        {
            var a = i;

            var powerBurstOfDisharmony = FeatureDefinitionPowerSharedPoolBuilder
                .Create($"Power{Name}BurstOfDisharmony{i}")
                .SetGuiPresentation(
                    Gui.Format($"Feature/&Power{Name}SubBurstOfDisharmonyTitle", i.ToString()),
                    Gui.Format($"Feature/&Power{Name}SubBurstOfDisharmonyDescription",
                        i.ToString(),
                        (i + 2).ToString()))
                .SetSharedPool(ActivationTime.BonusAction, powerBurstOfDisharmonyPool)
                .SetEffectDescription(
                    EffectDescriptionBuilder
                        .Create()
                        .SetTargetingData(Side.Enemy, RangeType.Distance, 6, TargetType.Cube, 3, 3)
                        .SetDurationData(DurationType.Minute, 1)
                        .SetParticleEffectParameters(SpellDefinitions.DreadfulOmen)
                        .SetSavingThrowData(
                            false,
                            AttributeDefinitions.Constitution,
                            true,
                            EffectDifficultyClassComputation.AbilityScoreAndProficiency)
                        .SetEffectForms(
                            EffectFormBuilder
                                .Create()
                                .HasSavingThrow(EffectSavingThrowType.HalfDamage)
                                .SetDamageForm(DamageTypePsychic, 2 + i, DieType.D6)
                                .Build(),
                            EffectFormBuilder
                                .Create()
                                .SetConditionForm(
                                    conditionDiscordance,
                                    ConditionForm.ConditionOperation.Add)
                                .Build())
                        .Build())
                .SetCustomSubFeatures(
                    new ValidatorsPowerUse(
                        c => c.RemainingKiPoints >= a &&
                             c.TryGetAttributeValue(AttributeDefinitions.ProficiencyBonus) >= a))
                .AddToDB();

            powerBurstOfDisharmonyList.Add(powerBurstOfDisharmony);
        }

        PowerBundle.RegisterPowerBundle(
            powerBurstOfDisharmonyPool, false,
            powerBurstOfDisharmonyList
        );

        var featureSetDiscordance = FeatureDefinitionFeatureSetBuilder
            .Create($"FeatureSet{Name}Discordance")
            .SetGuiPresentation(Category.Feature)
            .AddFeatureSet(powerDiscordance)
            .AddToDB();

        var featureSetSchism = FeatureDefinitionFeatureSetBuilder
            .Create($"FeatureSet{Name}Schism")
            .SetGuiPresentation(Category.Feature)
            .AddToDB();

        /*
        Level 17 - Profound Turmoil
        Starting at 17th level, increase the damage of your Discordance and Burst of Disharmony by an amount equal to your Wisdom modifier.
        */

        Subclass = CharacterSubclassDefinitionBuilder
            .Create(Name)
            .SetGuiPresentation(Category.Subclass,
                Sprites.GetSprite("WayOfTheDiscordance", Resources.WayOfTheDiscordance, 256))
            .AddFeaturesAtLevel(3,
                featureSetDiscordance)
            .AddFeaturesAtLevel(6,
                featureSetSchism)
            .AddFeaturesAtLevel(11,
                powerBurstOfDisharmonyPool)
            .AddToDB();
    }

    internal override CharacterSubclassDefinition Subclass { get; }

    internal override FeatureDefinitionSubclassChoice SubclassChoice =>
        FeatureDefinitionSubclassChoices.SubclassChoiceMonkMonasticTraditions;

    // ReSharper disable once UnassignedGetOnlyAutoProperty
    internal override DeityDefinition DeityDefinition { get; }

    // apply the logic to add discordance conditions and to determine if it's time to explode
    private sealed class AfterAttackEffectDiscordance : IOnAfterActionFeature, IAfterAttackEffect
    {
        private const int DiscordanceLimit = 3;
        private readonly ConditionDefinition _conditionDefinition;
        private readonly FeatureDefinitionPower _featureDefinitionPower;

        public AfterAttackEffectDiscordance(
            ConditionDefinition conditionDefinition,
            FeatureDefinitionPower featureDefinitionPower)
        {
            _conditionDefinition = conditionDefinition;
            _featureDefinitionPower = featureDefinitionPower;
        }

        // only add condition if monk weapon or unarmed
        public void AfterOnAttackHit(
            GameLocationCharacter attacker,
            GameLocationCharacter defender,
            RollOutcome outcome,
            CharacterActionParams actionParams,
            RulesetAttackMode attackMode,
            ActionModifier attackModifier)
        {
            if (outcome is RollOutcome.Failure or RollOutcome.CriticalFailure)
            {
                return;
            }

            if (attackMode is { SourceDefinition: ItemDefinition item } &&
                item.WeaponDescription.IsMonkWeaponOrUnarmed())
            {
                ApplyCondition(attacker, defender, _conditionDefinition);
            }
        }

        public void OnAfterAction(CharacterAction action)
        {
            var gameLocationAttacker = action.ActingCharacter;
            var rulesetAttacker = gameLocationAttacker.RulesetCharacter;

            // force expend ki points depending on power level used
            if (action is CharacterActionUsePower characterActionUsePower &&
                characterActionUsePower.activePower.PowerDefinition.Name.StartsWith($"Power{Name}BurstOfDisharmony"))
            {
                var name = characterActionUsePower.activePower.PowerDefinition.Name;
                var kiPoints = int.Parse(name.Substring(name.Length - 1, 1));
                var kiPointsAltered = rulesetAttacker.KiPointsAltered;

                rulesetAttacker.ForceKiPointConsumption(kiPoints);
                kiPointsAltered?.Invoke(rulesetAttacker, rulesetAttacker.RemainingKiPoints);
            }

            // handle Schism behavior
            // if in the future we need to nerf this, gotta add a check for RemainingRounds == 1
            if (GetMonkLevel(rulesetAttacker) >= 6)
            {
                foreach (var gameLocationDefender in action.ActionParams.TargetCharacters
                             .Where(t => !t.RulesetCharacter.IsDeadOrDyingOrUnconscious &&
                                         t.RulesetCharacter.AllConditions
                                             .Any(x => x.ConditionDefinition ==
                                                       ConditionDefinitions.ConditionStunned_MonkStunningStrike &&
                                                       x.RemainingRounds <= 1))
                             .ToList()) // avoid changing enumerator
                {
                    ApplyCondition(gameLocationAttacker, gameLocationDefender, _conditionDefinition);
                }
            }

            // although it should be one target only, we better keep it compatible for any future feature
            foreach (var gameLocationDefender in action.ActionParams.TargetCharacters
                         .Select(gameLocationCharacter => new
                         {
                             gameLocationCharacter,
                             discordanceCount = gameLocationCharacter.RulesetCharacter.AllConditions
                                 .FindAll(x => x.ConditionDefinition == _conditionDefinition)
                                 .Count
                         })
                         .Where(t =>
                             !t.gameLocationCharacter.RulesetCharacter.IsDeadOrDyingOrUnconscious &&
                             t.discordanceCount >= DiscordanceLimit)
                         .Select(t => t.gameLocationCharacter)
                         .ToList()) // avoid changing enumerator
            {
                var rulesetDefender = gameLocationDefender?.RulesetCharacter;

                if (rulesetDefender == null)
                {
                    continue;
                }

                // remove conditions up to the limit to also support Schism scenario
                rulesetDefender.AllConditions
                    .FindAll(x => x.ConditionDefinition == _conditionDefinition)
                    .OrderBy(x => x.RemainingRounds)
                    .Take(DiscordanceLimit)
                    .ToList() // avoid changing enumerator
                    .ForEach(x => rulesetDefender.RemoveCondition(x));

                // setup explosion power and increase damage dice based on Monk progression
                var usablePower = new RulesetUsablePower(_featureDefinitionPower, null, null);
                var effectPower = new RulesetEffectPower(rulesetAttacker, usablePower);
                var damageForm = effectPower.EffectDescription.FindFirstDamageForm();
                var monkLevel = GetMonkLevel(rulesetAttacker);

                if (damageForm == null || monkLevel <= 0)
                {
                    continue;
                }

                damageForm.BonusDamage =
                    rulesetAttacker.TryGetAttributeValue(AttributeDefinitions.ProficiencyBonus) / 2;
                damageForm.DieType = FeatureDefinitionAttackModifiers.AttackModifierMonkMartialArtsImprovedDamage
                    .DieTypeByRankTable.Find(x => x.Rank == monkLevel).DieType;

                effectPower.EffectDescription.effectParticleParameters.targetParticleReference =
                    effectPower.EffectDescription.effectParticleParameters.conditionStartParticleReference;

                effectPower.ApplyEffectOnCharacter(rulesetDefender, true, gameLocationDefender.LocationPosition);
            }
        }

        private static void ApplyCondition(
            GameLocationCharacter attacker,
            GameLocationCharacter defender,
            ConditionDefinition conditionDefinition)
        {
            var rulesetCondition = RulesetCondition.CreateActiveCondition(
                defender.Guid,
                conditionDefinition,
                DurationType.Minute,
                1,
                TurnOccurenceType.EndOfTurn,
                attacker.Guid,
                attacker.RulesetCharacter.CurrentFaction.Name);

            defender.RulesetCharacter.AddConditionOfCategory(AttributeDefinitions.TagEffect, rulesetCondition);
        }

        // return the Monk level factoring in wildshape multiclass scenarios
        private static int GetMonkLevel(RulesetCharacter rulesetCharacter)
        {
            return rulesetCharacter.GetClassLevel(CharacterClassDefinitions.Monk);
        }
    }
}
