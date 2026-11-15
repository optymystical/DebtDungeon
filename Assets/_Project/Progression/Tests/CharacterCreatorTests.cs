using System.Collections.Generic;
using DebtDungeon.Core;
using NUnit.Framework;
using UnityEditor;

namespace DebtDungeon.Progression.Tests
{
    /// <summary>
    /// Edit Mode tests for the creator's logic, using the real ProgressionCatalog asset
    /// (Trello: "Character creator UI").
    /// </summary>
    public class CharacterCreatorTests
    {
        private const string CatalogPath = "Assets/_Project/Data/Progression/ProgressionCatalog.asset";

        private ProgressionCatalog _catalog;
        private CharacterCreator _creator;

        [SetUp]
        public void SetUp()
        {
            _catalog = AssetDatabase.LoadAssetAtPath<ProgressionCatalog>(CatalogPath);
            Assert.IsNotNull(_catalog, $"Missing {CatalogPath}");
            _creator = new CharacterCreator(_catalog);
        }

        [Test]
        public void Catalog_OffersMartialAndMagic_WithTwoPresetsEach()
        {
            List<Archetype> archetypes = _catalog.GetArchetypes();

            Assert.AreEqual(2, archetypes.Count);
            Assert.IsTrue(archetypes.Contains(Archetype.Martial));
            Assert.IsTrue(archetypes.Contains(Archetype.Magic));
            Assert.AreEqual(2, _catalog.GetPresets(Archetype.Martial).Count);
            Assert.AreEqual(2, _catalog.GetPresets(Archetype.Magic).Count);
            Assert.IsNotNull(_catalog.LevelCurve);
        }

        [Test]
        public void EquipmentChoices_AreFilteredByArchetypeAndSlot()
        {
            Assert.AreEqual(0, _creator.GetEquipmentChoices(EquipmentSlot.Weapon).Count, "Nothing before an archetype is picked.");

            _creator.SelectArchetype(Archetype.Martial);

            Assert.AreEqual(2, _creator.GetEquipmentChoices(EquipmentSlot.Weapon).Count); // Sword, Greatsword
            Assert.AreEqual(0, _creator.GetEquipmentChoices(EquipmentSlot.Foci).Count);
        }

        [Test]
        public void PickingAPreset_BuildsALevelOneDraftWithItsGear()
        {
            _creator.SelectArchetype(Archetype.Martial);
            PresetDefinition vanguard = FindPreset("Preset_Vanguard");

            Assert.IsTrue(_creator.TrySelectPreset(vanguard, out string reason), reason);

            Assert.IsNotNull(_creator.Draft);
            Assert.AreEqual(1, _creator.Draft.Progress.Level);
            Assert.IsNotNull(_creator.Draft.Loadout.GetEquipped(EquipmentSlot.Weapon));
            Assert.IsNotNull(_creator.Draft.Loadout.GetEquipped(EquipmentSlot.Armor));
        }

        [Test]
        public void ChangingArchetype_ClearsPresetAndDraft()
        {
            _creator.SelectArchetype(Archetype.Martial);
            _creator.TrySelectPreset(FindPreset("Preset_Vanguard"), out _);

            _creator.SelectArchetype(Archetype.Magic);

            Assert.IsNull(_creator.Preset);
            Assert.IsNull(_creator.Draft);
        }

        [Test]
        public void PresetOfAnotherArchetype_IsRefused()
        {
            _creator.SelectArchetype(Archetype.Magic);

            Assert.IsFalse(_creator.TrySelectPreset(FindPreset("Preset_Vanguard"), out _));
            Assert.IsNull(_creator.Draft);
        }

        [Test]
        public void ConfirmBeforeAPreset_IsRefused()
        {
            _creator.SelectArchetype(Archetype.Martial);

            Assert.IsFalse(_creator.TryConfirm(out CharacterBlueprint blueprint, out _));
            Assert.IsNull(blueprint);
        }

        // Acceptance test (Trello): does the character in game match every choice made in the creator?
        [Test]
        public void BuiltCharacter_MatchesEveryCreatorChoice()
        {
            _creator.SelectArchetype(Archetype.Martial);
            _creator.TrySelectPreset(FindPreset("Preset_Vanguard"), out _);
            EquipmentDefinition sword = FindItem(Archetype.Martial, EquipmentSlot.Weapon, "sword");
            Assert.IsTrue(_creator.Draft.Loadout.TryEquip(sword, out string reason), reason); // swap the Greatsword
            _creator.SetName("Ada");

            Assert.IsTrue(_creator.TryConfirm(out CharacterBlueprint blueprint, out reason), reason);
            CharacterData built = blueprint.Build();

            Assert.AreEqual("Ada", built.Name);
            Assert.AreEqual(1, built.Progress.Level);
            Assert.AreSame(sword, built.Loadout.GetEquipped(EquipmentSlot.Weapon));
            Assert.AreSame(_creator.Draft.Loadout.GetEquipped(EquipmentSlot.Armor), built.Loadout.GetEquipped(EquipmentSlot.Armor));
            Assert.AreEqual(_creator.Draft.Loadout.BurdenSteps, built.Loadout.BurdenSteps);
            for (int i = 0; i < StatRules.StatsPerArchetype; i++)
            {
                StatType stat = (StatType)i; // Martial stats are 0-2
                Assert.AreEqual(_creator.Draft.Stats.GetStat(stat), built.Stats.GetStat(stat), $"{stat} differs");
            }
        }

        [Test]
        public void MagicAffixChoices_CarryIntoTheBuiltCharacter()
        {
            _creator.SelectArchetype(Archetype.Magic);
            _creator.TrySelectPreset(FindPreset("Preset_Stormcaller"), out _);
            EquipmentDefinition agate = FindItem(Archetype.Magic, EquipmentSlot.Affix, "agate");
            Assert.IsTrue(_creator.Draft.Loadout.TryAddAffix((AffixDefinition)agate, AffixForm.Offensive, out string reason), reason);

            _creator.TryConfirm(out CharacterBlueprint blueprint, out _);
            CharacterData built = blueprint.Build();

            Assert.AreEqual(1, built.Loadout.Affixes.Count);
            Assert.AreSame(agate, built.Loadout.Affixes[0].Affix);
            Assert.AreEqual(AffixForm.Offensive, built.Loadout.Affixes[0].Form);
        }

        [Test]
        public void EachBuild_IsAFreshCharacter()
        {
            _creator.SelectArchetype(Archetype.Martial);
            _creator.TrySelectPreset(FindPreset("Preset_Bladesworn"), out _);
            _creator.TryConfirm(out CharacterBlueprint blueprint, out _);

            CharacterData firstRun = blueprint.Build();
            firstRun.Progress.AddXp(500);
            CharacterData retry = blueprint.Build();

            Assert.AreNotSame(firstRun, retry);
            Assert.AreEqual(1, retry.Progress.Level);
        }

        private PresetDefinition FindPreset(string assetName)
        {
            foreach (Archetype archetype in _catalog.GetArchetypes())
            {
                foreach (PresetDefinition preset in _catalog.GetPresets(archetype))
                {
                    if (preset.name == assetName)
                    {
                        return preset;
                    }
                }
            }

            Assert.Fail($"No preset named {assetName} in the catalog.");
            return null;
        }

        private EquipmentDefinition FindItem(Archetype archetype, EquipmentSlot slot, string id)
        {
            foreach (EquipmentDefinition item in _catalog.GetEquipment(archetype, slot))
            {
                if (item.Id == id)
                {
                    return item;
                }
            }

            Assert.Fail($"No {archetype} {slot} with id {id} in the catalog.");
            return null;
        }
    }
}
