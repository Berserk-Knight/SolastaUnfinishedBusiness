﻿using System.Collections.Generic;
using System.Linq;
using SolastaCommunityExpansion.Builders;
using SolastaModApi;
using SolastaModApi.Extensions;
using SolastaModApi.Infrastructure;

namespace SolastaCommunityExpansion.CustomUI
{
    public class ReactionRequestWarcaster : ReactionRequest
    {
        public const string Name = "WarcasterReaction";
        public static ReactionDefinition ReactWarcasterDefinition;

        public ReactionRequestWarcaster(CharacterActionParams reactionParams)
            : base(Name, reactionParams)
        {
            BuildSuboptions();
            ReactionParams.StringParameter2 = "Warcaster";
        }

        public override int SelectedSubOption
        {
            get
            {
                var spell = (ReactionParams.RulesetEffect as RulesetEffectSpell)?.SpellDefinition;
                if (spell == null)
                {
                    return 0;
                }

                return ReactionParams.SpellRepertoire.KnownSpells.FindIndex(s => s == spell) + 1;
            }
        }


        public override string SuboptionTag => "Warcaster";

        public override bool IsStillValid
        {
            get
            {
                var targetCharacter = ReactionParams.TargetCharacters[0];
                return ServiceRepository.GetService<IGameLocationCharacterService>().ValidCharacters
                    .Contains(targetCharacter) && !targetCharacter.RulesetCharacter.IsDeadOrDyingOrUnconscious;
            }
        }

        public static void Initialize()
        {
            ReactWarcasterDefinition = ReactionDefinitionBuilder
                .Create(DatabaseHelper.ReactionDefinitions.OpportunityAttack, Name,
                    DefinitionBuilder.CENamespaceGuid)
                .SetGuiPresentation(Category.Reaction)
                .AddToDB();
        }

        private void BuildSuboptions()
        {
            SubOptionsAvailability.Clear();
            SubOptionsAvailability.Add(0, true);

            var battleManager = ServiceRepository.GetService<IGameLocationBattleService>() as GameLocationBattleManager;
            if (battleManager == null)
            {
                SelectSubOption(0);
                return;
            }

            var reactionParams = ReactionParams;
            var actingCharacter = reactionParams.ActingCharacter;
            var rulesetCharacter = actingCharacter.RulesetCharacter;

            // should not trigger if a wildshape form
            if (rulesetCharacter is not RulesetCharacterHero)
            {
                return;
            }

            //TODO: find better way to detect warcaster
            var affinities = rulesetCharacter.GetFeaturesByType<FeatureDefinitionMagicAffinity>();
            if (affinities == null || affinities.All(a => a.Name != "MagicAffinityWarCasterFeat"))
            {
                return;
            }

            var cantrips = new List<SpellDefinition>();
            rulesetCharacter.EnumerateReadyAttackCantrips(cantrips);

            cantrips.RemoveAll(cantrip =>
            {
                if (cantrip.ActivationTime != RuleDefinitions.ActivationTime.Action
                    && cantrip.ActivationTime != RuleDefinitions.ActivationTime.BonusAction)
                {
                    return true;
                }

                var attackParams = new BattleDefinitions.AttackEvaluationParams();
                var actionModifier = new ActionModifier();
                var targetCharacters = reactionParams.TargetCharacters;

                attackParams.FillForMagic(actingCharacter,
                    actingCharacter.LocationPosition,
                    cantrip.EffectDescription,
                    cantrip.Name,
                    targetCharacters[0],
                    targetCharacters[0].LocationPosition,
                    actionModifier);

                return !battleManager.IsValidAttackForReadiedAction(attackParams, false);
            });

            reactionParams.SpellRepertoire = new RulesetSpellRepertoire();

            var i = 1;
            foreach (var c in cantrips)
            {
                reactionParams.SpellRepertoire.KnownSpells.Add(c);
                SubOptionsAvailability.Add(i, true);
                i++;
            }

            SelectSubOption(0);
        }


        public override void SelectSubOption(int option)
        {
            ReactionParams.RulesetEffect?.Terminate(false);
            var reactionParams = ReactionParams;

            var targetCharacters = reactionParams.TargetCharacters;

            while (targetCharacters.Count > 1)
            {
                reactionParams.TargetCharacters.RemoveAt(targetCharacters.Count - 1);
                reactionParams.ActionModifiers.RemoveAt(reactionParams.ActionModifiers.Count - 1);
            }

            var actingCharacter = reactionParams.ActingCharacter;
            if (option == 0)
            {
                reactionParams.ActionDefinition = ServiceRepository.GetService<IGameLocationActionService>()
                    .AllActionDefinitions[ActionDefinitions.Id.AttackOpportunity];
                reactionParams.RulesetEffect = null;
                var attackParams = new BattleDefinitions.AttackEvaluationParams();
                var actionModifier = new ActionModifier();
                attackParams.FillForPhysicalReachAttack(actingCharacter,
                    actingCharacter.LocationPosition,
                    reactionParams.AttackMode,
                    reactionParams.TargetCharacters[0],
                    reactionParams.TargetCharacters[0].LocationPosition, actionModifier);
                reactionParams.ActionModifiers[0] = actionModifier;
            }
            else
            {
                reactionParams.ActionDefinition = ServiceRepository.GetService<IGameLocationActionService>()
                    .AllActionDefinitions[ActionDefinitions.Id.CastReaction];
                var spell = reactionParams.SpellRepertoire.KnownSpells[option - 1];
                var rulesService =
                    ServiceRepository.GetService<IRulesetImplementationService>();
                var rulesetCharacter = actingCharacter.RulesetCharacter;
                rulesetCharacter.CanCastSpell(spell, true, out var spellRepertoire);
                var spellEffect = rulesService.InstantiateEffectSpell(rulesetCharacter, spellRepertoire,
                    spell, spell.SpellLevel, false);
                ReactionParams.RulesetEffect = spellEffect;

                var spelltargets = spellEffect.ComputeTargetParameter();
                if (reactionParams.RulesetEffect.EffectDescription.IsSingleTarget && spelltargets > 0)
                {
                    var target = reactionParams.TargetCharacters.FirstOrDefault();
                    var mod = reactionParams.ActionModifiers.FirstOrDefault();

                    while (target != null && mod != null && reactionParams.TargetCharacters.Count < spelltargets)
                    {
                        reactionParams.TargetCharacters.Add(target);
                        // Technically casts after first might need to have different mods, but not by much since we attacking same target.
                        reactionParams.ActionModifiers.Add(mod);
                    }
                }
            }
        }

        public override string FormatDescription()
        {
            var target = new GuiCharacter(ReactionParams.TargetCharacters[0]);
            return Gui.Format(base.FormatDescription(), target.Name);
        }

        public override string FormatReactDescription()
        {
            return Gui.Format(base.FormatReactDescription(), "");
        }

        public override void OnSetInvalid()
        {
            base.OnSetInvalid();
            ReactionParams.RulesetEffect?.Terminate(false);
        }
    }
}
