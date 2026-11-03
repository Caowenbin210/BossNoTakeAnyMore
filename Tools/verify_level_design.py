"""Validate prototype design data and solve the small boards (Python stdlib only)."""
import json
from pathlib import Path


def rotated(cells, turns):
    cells = [tuple(c) for c in cells]
    for _ in range(turns % 4):
        cells = [(y, -x) for x, y in cells]
    min_x = min(x for x, _ in cells)
    min_y = min(y for _, y in cells)
    return tuple(sorted((x - min_x, y - min_y) for x, y in cells))


def occupied(cells, anchor):
    return frozenset((x + anchor[0], y + anchor[1]) for x, y in cells)


def solve(level, shapes, fixed=None, initial_only=False):
    board = {(x, y) for x in range(level['width']) for y in range(level['height'])}
    board -= {tuple(c) for c in level['blocked']}
    fixed = fixed or {}
    used = set()
    for item in level['cargo']:
        if item['id'] in fixed:
            p = fixed[item['id']]
            cells = occupied(rotated(shapes[item['shape']], p['turns']), p['anchor'])
            if not cells <= board or cells & used:
                return None
            used.update(cells)
    remaining = sorted((i for i in level['cargo'] if i['id'] not in fixed),
                       key=lambda i: -len(shapes[i['shape']]))
    options = {}
    for item in remaining:
        choices, seen = [], set()
        turns = [item['initialTurns']] if initial_only else range(4)
        for turn in turns:
            shape = rotated(shapes[item['shape']], turn)
            if shape in seen:
                continue
            seen.add(shape)
            for y in range(level['height']):
                for x in range(level['width']):
                    cells = occupied(shape, (x, y))
                    if cells <= board:
                        choices.append((cells, {'anchor': [x, y], 'turns': turn}))
        options[item['id']] = choices

    def search(index, taken, result):
        if index == len(remaining):
            return result
        item_id = remaining[index]['id']
        for cells, placement in options[item_id]:
            if not cells & taken:
                answer = search(index + 1, taken | cells, {**result, item_id: placement})
                if answer is not None:
                    return answer
        return None

    return search(0, used, dict(fixed))


def main():
    source = Path(__file__).resolve().parents[1] / 'Docs' / 'Data' / 'PrototypeLevels.json'
    data = json.loads(source.read_text(encoding='utf-8'))
    shapes = data['shapes']
    for level in data['levels']:
        ref = level['reference']
        assert len({i['id'] for i in level['cargo']}) == len(level['cargo'])
        assert set(ref) == {i['id'] for i in level['cargo']}
        assert solve(level, shapes, fixed=ref) is not None, f"Invalid reference: {level['id']}"
        assert solve(level, shapes) is not None
        initial_answer = solve(level, shapes, initial_only=True)
        if level['id'] == 'level_001':
            assert initial_answer is not None
        if level['id'] == 'level_002':
            assert initial_answer is None, 'Level 2 must require rotation'
        if 'rearrangementExample' in level:
            example = level['rearrangementExample']
            assert solve(level, shapes, fixed=example['before']) is None
            assert solve(level, shapes, fixed=example['after']) is not None
        print(f"{level['id']}: reference OK; solvable; initial-orientation solution={initial_answer is not None}")
    print('All design checks passed. Unity runtime and touch behavior are not tested here.')


if __name__ == '__main__':
    main()
