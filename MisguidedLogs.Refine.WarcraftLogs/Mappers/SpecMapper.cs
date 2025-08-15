using MisguidedLogs.Refine.WarcraftLogs.Model;
using TalentSpec = MisguidedLogs.Refine.WarcraftLogs.Model.Result.TalentSpec;

namespace MisguidedLogs.Refine.WarcraftLogs.Mappers;

public static class SpecMapper
{
    public static TalentSpec GetClassSpecc(Class @class, List<Talent> talents)
    {
        if (talents[0].Id >= 31)
        {
            return @class switch
            {
                Class.Priest => TalentSpec.Discipline,
                Class.Warlock => TalentSpec.Affliction,
                Class.Mage => TalentSpec.Arcane,
                Class.Rogue => TalentSpec.Assasination,
                Class.Druid => TalentSpec.Balance,
                Class.Hunter => TalentSpec.BeastMastery,
                Class.Shaman => TalentSpec.Elemental,
                Class.Paladin => TalentSpec.Holy,
                Class.Warrior => TalentSpec.Arms,
                Class.DeathKnight => TalentSpec.Blood,
                Class.Monk => throw new NotImplementedException(),
                Class.DemonHunter => throw new NotImplementedException(),
                Class.Evoker => throw new NotImplementedException(),
                _ => throw new NotImplementedException()
            };
        }
        if (talents[1].Id >= 31)
        {
            return @class switch
            {
                Class.Priest => TalentSpec.Holy,
                Class.Warlock => TalentSpec.Demonlogy,
                Class.Mage => TalentSpec.Fire,
                Class.Rogue => TalentSpec.Combat,
                Class.Druid => TalentSpec.Feral,
                Class.Hunter => TalentSpec.Marksmanship,
                Class.Shaman => TalentSpec.Enhancement,
                Class.Paladin => TalentSpec.Protection,
                Class.Warrior => TalentSpec.Fury,
                Class.DeathKnight => TalentSpec.Frost,
                Class.Monk => throw new NotImplementedException(),
                Class.DemonHunter => throw new NotImplementedException(),
                Class.Evoker => throw new NotImplementedException(),
                _ => throw new NotImplementedException()
            };
        }
        if (talents[2].Id >= 31)
        {
            return @class switch
            {
                Class.Priest => TalentSpec.Shadow,
                Class.Warlock => TalentSpec.Destruction,
                Class.Mage => TalentSpec.Frost,
                Class.Rogue => TalentSpec.Subtlety,
                Class.Druid => TalentSpec.Restoration,
                Class.Hunter => TalentSpec.Survival,
                Class.Shaman => TalentSpec.Restoration,
                Class.Paladin => TalentSpec.Retribution,
                Class.Warrior => TalentSpec.Protection,
                Class.DeathKnight => TalentSpec.Unholy,
                Class.Monk => throw new NotImplementedException(),
                Class.DemonHunter => throw new NotImplementedException(),
                Class.Evoker => throw new NotImplementedException(),
                _ => throw new NotImplementedException()
            };
        }
        return TalentSpec.Hybrid;
    }
}