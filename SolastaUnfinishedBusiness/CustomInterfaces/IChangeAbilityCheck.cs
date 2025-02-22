﻿using System.Collections.Generic;
using JetBrains.Annotations;

namespace SolastaUnfinishedBusiness.CustomInterfaces;

/// <summary>
///     Implement on a FeatureDefinition to be able to change the min roll value on ability checks
/// </summary>
public interface IChangeAbilityCheck
{
    [UsedImplicitly]
    public int MinRoll(
        RulesetCharacter character,
        int baseBonus,
        int rollModifier,
        string abilityScoreName,
        string proficiencyName,
        List<RuleDefinitions.TrendInfo> advantageTrends,
        List<RuleDefinitions.TrendInfo> modifierTrends);
}
