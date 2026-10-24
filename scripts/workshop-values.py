"""Read Workshop illustration values from mod source and installed Core definitions."""
import re
import xml.etree.ElementTree as ET
from pathlib import Path


def load_values(root, game_root):
    source=(root/'Source/Quality_Configuration.cs').read_text()
    section=source.split('internal static class QualityEffectCatalog',1)[1]
    arrays=re.findall(r'new\[\]\s*\{([^}]+)\}',section)
    curves=[[float(v.strip().rstrip('f')) for v in group.split(',')] for group in arrays[:3]]
    if len(curves)!=3 or any(len(values)!=7 for values in curves):
        raise ValueError('Preset source changed; review the illustration reader.')
    mild_cost,mild,medium=curves
    assert 'benefits.Select(factor => 1f / factor)' in section
    progression=(root/'Source/Quality_Progression.cs').read_text()
    assert 'Factors = QualityEffectCatalog.Curve(id)' in progression
    def factors(file, predicate):
        tree=ET.parse(game_root/'Data/Core/Defs/Stats'/file)
        stat=next(node for node in tree.getroot() if predicate(node))
        part=next(node for node in stat.findall('parts/li') if node.get('Class')=='StatPart_Quality')
        return [float(part.findtext('factor'+tier)) for tier in ['Awful','Poor','Normal','Good','Excellent','Masterwork','Legendary']]
    market=factors('Stats_Basics_General.xml',lambda n:n.get('Name')=='MarketValueBase')
    rest=factors('Stats_Building_Special.xml',lambda n:n.findtext('defName')=='BedRestEffectiveness')
    weapons=ET.parse(game_root/'Data/Core/Defs/ThingDefs_Misc/Weapons/RangedIndustrial.xml')
    rifle=next(n for n in weapons.getroot() if n.findtext('defName')=='Gun_AssaultRifle')
    cooldown=float(rifle.findtext('statBases/RangedWeapon_Cooldown'))
    # These adapters multiply their separate factors directly into the native stat.
    assert 'val *= QualityDurabilityProduction.Factor' in (root/'Source/Quality_DurabilityProduction.cs').read_text()
    assert 'val *= QualityEquipmentCombat.Factor' in (root/'Source/Quality_EquipmentCombat.cs').read_text()
    return dict(mild=mild,medium=medium,progression=mild[:],mild_cost=mild_cost,
                medium_cost=[1/v for v in medium],market=market,rest=rest,cooldown=cooldown)
