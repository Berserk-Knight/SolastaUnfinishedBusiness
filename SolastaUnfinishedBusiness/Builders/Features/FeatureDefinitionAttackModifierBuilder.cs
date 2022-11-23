﻿using System;
using JetBrains.Annotations;
using UnityEngine.AddressableAssets;
using static RuleDefinitions;

namespace SolastaUnfinishedBusiness.Builders.Features;

[UsedImplicitly]
internal class FeatureDefinitionAttackModifierBuilder
    : DefinitionBuilder<FeatureDefinitionAttackModifier, FeatureDefinitionAttackModifierBuilder>
{
    internal FeatureDefinitionAttackModifierBuilder SetAbilityScoreReplacement(
        AbilityScoreReplacement replacement)
    {
        Definition.abilityScoreReplacement = replacement;
        return this;
    }

    internal FeatureDefinitionAttackModifierBuilder SetAttackRollModifier(
        int value = 0,
        AttackModifierMethod method = AttackModifierMethod.FlatValue)
    {
        Definition.attackRollModifierMethod = method;
        Definition.attackRollModifier = value;
        return this;
    }

    internal FeatureDefinitionAttackModifierBuilder SetDamageRollModifier(
        int value = 0,
        AttackModifierMethod method = AttackModifierMethod.FlatValue)
    {
        Definition.damageRollModifierMethod = method;
        Definition.damageRollModifier = value;
        return this;
    }

    internal FeatureDefinitionAttackModifierBuilder SetMagicalWeapon()
    {
        Definition.magicalWeapon = true;
        return this;
    }

    internal FeatureDefinitionAttackModifierBuilder SetAdditionalAttackTag(string tag)
    {
        Definition.additionalBonusUnarmedStrikeAttacksTag = tag;
        return this;
    }

    internal FeatureDefinitionAttackModifierBuilder SetImpactParticleReference(AssetReference asset)
    {
        Definition.impactParticleReference = asset;
        return this;
    }

    internal FeatureDefinitionAttackModifierBuilder SetRequiredProperty(
        RestrictedContextRequiredProperty property)
    {
        Definition.requiredProperty = property;
        return this;
    }

    #region Constructors

    protected FeatureDefinitionAttackModifierBuilder(string name, Guid namespaceGuid) : base(name, namespaceGuid)
    {
    }

    protected FeatureDefinitionAttackModifierBuilder(FeatureDefinitionAttackModifier original, string name,
        Guid namespaceGuid) : base(original, name, namespaceGuid)
    {
    }

    #endregion
}
