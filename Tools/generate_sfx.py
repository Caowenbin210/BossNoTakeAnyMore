"""Deterministic procedural SFX. Python standard library, no external samples.

Run from any directory. Output: Assets/_Game/Audio/SFX, mono PCM16 / 44100 Hz.
"""
import json
import math
import random
import struct
import wave
from pathlib import Path

RATE = 44100
OUT = Path(__file__).resolve().parents[1] / 'Assets/_Game/Audio/SFX'


def pluck(t, start, frequency, decay=20, strength=1):
    elapsed = t - start
    if elapsed < 0:
        return 0
    attack = min(1, elapsed / 0.004)
    phase = 2 * math.pi * frequency * elapsed
    return strength * attack * math.exp(-decay * elapsed) * (math.sin(phase) + 0.16 * math.sin(2 * phase))


def synthesize(kind, t, noise):
    if kind == 'cargo_pickup':
        return pluck(t, 0, 620, 38) + pluck(t, 0.025, 920, 48, 0.28)
    if kind == 'cargo_rotate':
        return pluck(t, 0, 480, 32, 0.7) + pluck(t, 0.045, 720, 35, 0.7)
    if kind == 'cargo_place':
        # Warm low thump plus a quiet confirmation tick; no harsh impact sample.
        body = math.sin(2 * math.pi * (140 * t - 210 * t * t)) * math.exp(-28 * t)
        return min(1, t / 0.003) * (body + 0.24 * noise * math.exp(-55 * t)) + pluck(t, 0.025, 1046.5, 35, 0.21)
    if kind == 'cargo_invalid':
        return pluck(t, 0, 310, 27, 0.75) + pluck(t, 0.065, 220, 25, 0.65)
    if kind == 'cargo_return':
        return pluck(t, 0, 520, 40, 0.7) + 0.13 * noise * math.exp(-55 * t)
    if kind == 'level_complete':
        return sum(pluck(t, start, hz, 6.5, gain) for start, hz, gain in
                   [(0, 523.25, 0.7), (0.10, 659.25, 0.65), (0.20, 783.99, 0.6), (0.32, 1046.5, 0.6)])
    raise ValueError(kind)


def main():
    OUT.mkdir(parents=True, exist_ok=True)
    specs = [('cargo_pickup', 0.11, 0.27), ('cargo_rotate', 0.18, 0.27),
             ('cargo_place', 0.22, 0.40), ('cargo_invalid', 0.24, 0.26),
             ('cargo_return', 0.14, 0.25), ('level_complete', 0.95, 0.38)]
    manifest = {'generator': 'Tools/generate_sfx.py', 'sampleRate': RATE,
                'channels': 1, 'encoding': 'PCM16', 'source': 'procedural; no third-party samples', 'clips': []}
    for index, (kind, duration, peak) in enumerate(specs):
        rng = random.Random(8000 + index)
        samples, filtered = [], 0.0
        for i in range(round(duration * RATE)):
            t = i / RATE
            filtered = filtered * 0.75 + rng.uniform(-1, 1) * 0.25
            value = synthesize(kind, t, filtered)
            value *= min(1, t / 0.002) * min(1, (duration - t) / 0.025)
            samples.append(value)
        scale = peak / max(abs(v) for v in samples)
        samples = [round(v * scale * 32767) for v in samples]
        with wave.open(str(OUT / (kind + '.wav')), 'wb') as clip:
            clip.setnchannels(1)
            clip.setsampwidth(2)
            clip.setframerate(RATE)
            clip.writeframes(struct.pack('<' + 'h' * len(samples), *samples))
        rms = math.sqrt(sum((v / 32768) ** 2 for v in samples) / len(samples))
        manifest['clips'].append({'file': kind + '.wav', 'seconds': duration,
                                  'peak': round(max(abs(v) for v in samples) / 32768, 4), 'rms': round(rms, 4)})
        print(f'{kind}: {duration:.2f}s, peak={peak:.2f}, RMS={rms:.3f}')
    (OUT / 'SFXManifest.json').write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')


if __name__ == '__main__':
    main()
