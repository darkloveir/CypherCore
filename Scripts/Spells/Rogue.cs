﻿/*
 * Copyright (C) 2012-2017 CypherCore <http://github.com/CypherCore>
 * 
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with this program.  If not, see <http://www.gnu.org/licenses/>.
 */

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Spells;
using System.Collections.Generic;

namespace Scripts.Spells.Rogue
{
    public struct SpellIds
    {
        public const uint BladeFlurryExtraAttack = 22482;
        public const uint CheatDeathCooldown = 31231;
        public const uint GlyphOfPreparation = 56819;
        public const uint KillingSpree = 51690;
        public const uint KillingSpreeTeleport = 57840;
        public const uint KillingSpreeWeaponDmg = 57841;
        public const uint KillingSpreeDmgBuff = 61851;
        public const uint PreyOnTheWeak = 58670;
        public const uint ShivTriggered = 5940;
        public const uint TricksOfTheTradeDmgBoost = 57933;
        public const uint TricksOfTheTradeProc = 59628;
        public const uint SerratedBladesR1 = 14171;
        public const uint Rupture = 1943;
        public const uint HonorAmongThievesEnergize = 51699;
        public const uint T52pSetBonus = 37169;
    }
    
    [Script] // 51690 - Killing Spree
    class spell_rog_killing_spree : SpellScriptLoader
    {
        public spell_rog_killing_spree() : base("spell_rog_killing_spree") { }

        class spell_rog_killing_spree_SpellScript : SpellScript
        {
            void FilterTargets(List<WorldObject> targets)
            {
                if (targets.Empty() || GetCaster().GetVehicleBase())
                    FinishCast(SpellCastResult.OutOfRange);
            }

            void HandleDummy(uint effIndex)
            {
                Aura aura = GetCaster().GetAura(SpellIds.KillingSpree);
                if (aura != null)
                {
                    var script = aura.GetScript<spell_rog_killing_spree_AuraScript>(nameof(spell_rog_killing_spree_AuraScript));
                    if (script != null)
                        script.AddTarget(GetHitUnit());
                }
            }

            public override void Register()
            {
                OnObjectAreaTargetSelect.Add(new ObjectAreaTargetSelectHandler(FilterTargets, 1, Targets.UnitDestAreaEnemy));
                OnEffectHitTarget.Add(new EffectHandler(HandleDummy, 1, SpellEffectName.Dummy));
            }
        }

        public override SpellScript GetSpellScript()
        {
            return new spell_rog_killing_spree_SpellScript();
        }

        public class spell_rog_killing_spree_AuraScript : AuraScript
        {
            public override bool Validate(SpellInfo spellInfo)
            {
                return ValidateSpellInfo(SpellIds.KillingSpreeTeleport, SpellIds.KillingSpreeWeaponDmg, SpellIds.KillingSpreeDmgBuff);
            }

            void HandleApply(AuraEffect aurEff, AuraEffectHandleModes mode)
            {
                GetTarget().CastSpell(GetTarget(), SpellIds.KillingSpreeDmgBuff, true);
            }

            void HandleEffectPeriodic(AuraEffect aurEff)
            {
                while (!_targets.Empty())
                {
                    ObjectGuid guid = _targets.SelectRandom();
                    Unit target = Global.ObjAccessor.GetUnit(GetTarget(), guid);
                    if (target)
                    {
                        GetTarget().CastSpell(target, SpellIds.KillingSpreeTeleport, true);
                        GetTarget().CastSpell(target, SpellIds.KillingSpreeWeaponDmg, true);
                        break;
                    }
                    else
                        _targets.Remove(guid);
                }
            }

            void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
            {
                GetTarget().RemoveAurasDueToSpell(SpellIds.KillingSpreeDmgBuff);
            }

            public override void Register()
            {
                AfterEffectApply.Add(new EffectApplyHandler(HandleApply, 0, AuraType.Dummy, AuraEffectHandleModes.Real));
                OnEffectPeriodic.Add(new EffectPeriodicHandler(HandleEffectPeriodic, 0, AuraType.PeriodicDummy));
                AfterEffectRemove.Add(new EffectApplyHandler(HandleRemove, 0, AuraType.PeriodicDummy, AuraEffectHandleModes.Real));
            }

            public void AddTarget(Unit target)
            {
                _targets.Add(target.GetGUID());
            }

            List<ObjectGuid> _targets = new List<ObjectGuid>();
        }

        public override AuraScript GetAuraScript()
        {
            return new spell_rog_killing_spree_AuraScript();
        }
    }

    [Script] // 70805 - Rogue T10 2P Bonus -- THIS SHOULD BE REMOVED WITH NEW PROC SYSTEM.
    class spell_rog_t10_2p_bonus : SpellScriptLoader
    {
        public spell_rog_t10_2p_bonus() : base("spell_rog_t10_2p_bonus") { }

        class spell_rog_t10_2p_bonus_AuraScript : AuraScript
        {
            bool CheckProc(ProcEventInfo eventInfo)
            {
                return eventInfo.GetActor() == eventInfo.GetActionTarget();
            }

            public override void Register()
            {
                DoCheckProc.Add(new CheckProcHandler(CheckProc));
            }
        }

        public override AuraScript GetAuraScript()
        {
            return new spell_rog_t10_2p_bonus_AuraScript();
        }
    }
    
    [Script] // 2098 - Eviscerate
    class spell_rog_eviscerate : SpellScriptLoader
    {
        public spell_rog_eviscerate() : base("spell_rog_eviscerate") { }

        class spell_rog_eviscerate_SpellScript : SpellScript
        {
            void CalculateDamage(uint effIndex)
            {
                int damagePerCombo = (int)(GetCaster().GetTotalAttackPowerValue(WeaponAttackType.BaseAttack) * 0.559f);
                AuraEffect t5 = GetCaster().GetAuraEffect(SpellIds.T52pSetBonus, 0);
                if (t5 != null)
                    damagePerCombo += t5.GetAmount();

                SetEffectValue(GetEffectValue() + damagePerCombo * GetCaster().GetPower(PowerType.ComboPoints));
            }

            public override void Register()
            {
                OnEffectLaunchTarget.Add(new EffectHandler(CalculateDamage, 0, SpellEffectName.SchoolDamage));
            }
        }

        public override SpellScript GetSpellScript()
        {
            return new spell_rog_eviscerate_SpellScript();
        }
    }
    
    [Script] // 32645 - Envenom
    class spell_rog_envenom : SpellScriptLoader
    {
        public spell_rog_envenom() : base("spell_rog_envenom") { }

        class spell_rog_envenom_SpellScript : SpellScript
        {
            void CalculateDamage(uint effIndex)
            {
                int damagePerCombo = (int)(GetCaster().GetTotalAttackPowerValue(WeaponAttackType.BaseAttack) * 0.417f);
                AuraEffect t5 = GetCaster().GetAuraEffect(SpellIds.T52pSetBonus, 0);
                if (t5 != null)
                    damagePerCombo += t5.GetAmount();

                SetEffectValue(GetEffectValue() + damagePerCombo * GetCaster().GetPower(PowerType.ComboPoints));
            }

            public override void Register()
            {
                OnEffectLaunchTarget.Add(new EffectHandler(CalculateDamage, 0, SpellEffectName.SchoolDamage));
            }
        }

        public override SpellScript GetSpellScript()
        {
            return new spell_rog_envenom_SpellScript();
        }
    }
}
