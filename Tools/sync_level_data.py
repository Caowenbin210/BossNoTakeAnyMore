"""Convert design JSON to JsonUtility-compatible runtime data (standard library only)."""
import json
from pathlib import Path

root = Path(__file__).resolve().parents[1]
source = json.loads((root / 'Docs/Data/PrototypeLevels.json').read_text(encoding='utf-8'))
levels = []
for level in source['levels']:
    cargo = []
    for item in level['cargo']:
        solution = level['reference'][item['id']]
        cargo.append({**item, 'cells': [{'x': x, 'y': y} for x, y in source['shapes'][item['shape']]],
                      'solutionX': solution['anchor'][0], 'solutionY': solution['anchor'][1],
                      'solutionTurns': solution['turns']})
    levels.append({'id': level['id'], 'title': level['title'], 'width': level['width'],
                   'height': level['height'], 'blocked': [{'x': x, 'y': y} for x, y in level['blocked']],
                   'cargo': cargo})
target = root / 'Assets/_Game/Resources/PrototypeLevels.json'
target.parent.mkdir(parents=True, exist_ok=True)
target.write_text(json.dumps({'levels': levels}, ensure_ascii=False, indent=2), encoding='utf-8')
print('Synced', len(levels), 'levels')
