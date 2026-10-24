"""Build small, deterministic original Workshop Match UI/SFX WAV drafts.

No sampled/third-party sounds are used. These are editable procedural source
sounds; they are intentionally not wired into Unity automatically.
"""

from __future__ import annotations

import math
import random
import wave
from pathlib import Path


RATE = 22050
ROOT = Path(__file__).resolve().parents[1] / "Assets/Art/UI/WorkshopMatchProduction/Group09_Audio"
ROOT.mkdir(parents=True, exist_ok=True)
RNG = random.Random(245061)


def canvas(seconds: float) -> list[float]:
    return [0.0] * round(seconds * RATE)


def envelope(t: float, duration: float, attack: float = 0.008, decay: float = 9.0) -> float:
    if t < 0 or t >= duration:
        return 0.0
    return min(1.0, t / max(attack, 0.0001)) * math.exp(-decay * t / duration)


def tone(
    out: list[float], start: float, duration: float, f0: float, f1: float,
    amp: float = 0.5, decay: float = 9.0, shape: str = "sine",
) -> None:
    begin = round(start * RATE)
    count = min(round(duration * RATE), len(out) - begin)
    phase = 0.0
    for i in range(max(0, count)):
        t = i / RATE
        freq = f0 + (f1 - f0) * (t / duration)
        phase += 2 * math.pi * freq / RATE
        wave_value = math.sin(phase)
        if shape == "triangle":
            wave_value = 2 / math.pi * math.asin(wave_value)
        out[begin + i] += amp * envelope(t, duration, decay=decay) * wave_value


def noise(
    out: list[float], start: float, duration: float, amp: float = 0.3,
    decay: float = 12.0, smooth: float = 0.0,
) -> None:
    begin = round(start * RATE)
    count = min(round(duration * RATE), len(out) - begin)
    previous = 0.0
    for i in range(max(0, count)):
        t = i / RATE
        raw = RNG.uniform(-1.0, 1.0)
        previous = smooth * previous + (1 - smooth) * raw
        out[begin + i] += amp * envelope(t, duration, decay=decay) * previous


def save(name: str, out: list[float]) -> None:
    peak = max(max(abs(x) for x in out), 1e-6)
    scale = min(0.84 / peak, 1.0)
    pcm = bytearray()
    for value in out:
        sample = round(max(-1.0, min(1.0, value * scale)) * 32767)
        pcm += sample.to_bytes(2, "little", signed=True)
    with wave.open(str(ROOT / f"{name}.wav"), "wb") as f:
        f.setnchannels(1)
        f.setsampwidth(2)
        f.setframerate(RATE)
        f.writeframes(pcm)


def make() -> None:
    s = canvas(0.22)
    tone(s, 0, 0.18, 660, 1010, 0.43, 11)
    tone(s, 0.015, 0.15, 1320, 1740, 0.19, 13)
    noise(s, 0, 0.055, 0.14, 18)
    save("candy_pop", s)

    s = canvas(0.32)
    noise(s, 0, 0.22, 0.20, 3.8, 0.65)
    save("rocket_ignite", s)

    s = canvas(0.55)
    noise(s, 0, 0.53, 0.3, 2.8, 0.80)
    tone(s, 0, 0.20, 420, 620, 0.045, 5.0)
    save("rocket_flight", s)

    s = canvas(0.55)
    tone(s, 0, 0.34, 170, 62, 0.52, 9)
    noise(s, 0, 0.38, 0.53, 8, 0.3)
    for offset in [0.025, 0.06, 0.10, 0.15]:
        noise(s, offset, 0.035, 0.13, 12, 0.1)
    save("rocket_burst", s)

    s = canvas(0.35)
    tone(s, 0, 0.30, 145, 68, 0.65, 12, "triangle")
    noise(s, 0, 0.11, 0.25, 19, 0.55)
    tone(s, 0.025, 0.20, 360, 280, 0.15, 12)
    save("hammer_hit", s)

    s = canvas(0.66)
    tone(s, 0, 0.62, 350, 805, 0.34, 2.6)
    tone(s, 0, 0.60, 353, 799, 0.31, 2.6)
    noise(s, 0.08, 0.48, 0.10, 4.0, 0.75)
    save("magnet_pull", s)

    s = canvas(0.32)
    tone(s, 0, 0.28, 1000, 490, 0.43, 7)
    tone(s, 0.04, 0.19, 1500, 720, 0.19, 9)
    save("magnet_close", s)

    s = canvas(0.30)
    tone(s, 0, 0.24, 190, 120, 0.36, 8, "triangle")
    tone(s, 0.025, 0.18, 410, 260, 0.20, 10)
    noise(s, 0.02, 0.10, 0.12, 12, 0.88)
    save("oil_squish", s)

    s = canvas(0.29)
    noise(s, 0, 0.21, 0.43, 9, 0.20)
    tone(s, 0.01, 0.17, 980, 460, 0.25, 12)
    tone(s, 0.09, 0.12, 1310, 640, 0.17, 12)
    save("frozen_oil_crack", s)

    s = canvas(0.40)
    tone(s, 0, 0.32, 1120, 1025, 0.34, 7)
    tone(s, 0, 0.34, 2240, 2060, 0.12, 9)
    save("bolt_ping", s)

    s = canvas(1.2)
    for i in range(15):
        t = i * 0.08
        tone(s, t, 0.075, 105, 92, 0.21, 6, "triangle")
        noise(s, t, 0.035, 0.085, 12, 0.5)
    save("motor_run_loop_draft", s)

    s = canvas(0.78)
    tone(s, 0, 0.55, 150, 44, 0.70, 7, "triangle")
    noise(s, 0, 0.58, 0.54, 5.5, 0.32)
    for t in (0.08, 0.19, 0.28):
        tone(s, t, 0.18, 1060, 460, 0.15, 10)
    save("motor_break", s)

    s = canvas(1.0)
    for i in range(13):
        t = i / 13
        tone(s, t, 0.055, 360, 280, 0.13, 8, "triangle")
        noise(s, t, 0.025, 0.08, 10, 0.6)
    save("gear_spin_loop_draft", s)

    s = canvas(0.13)
    tone(s, 0, 0.11, 780, 560, 0.26, 15)
    noise(s, 0, 0.045, 0.14, 20)
    save("ui_click", s)

    s = canvas(0.26)
    tone(s, 0, 0.22, 620, 790, 0.34, 9)
    tone(s, 0.055, 0.18, 930, 1150, 0.18, 10)
    save("map_select", s)

    s = canvas(0.19)
    tone(s, 0, 0.16, 570, 820, 0.28, 11)
    noise(s, 0, 0.05, 0.08, 20)
    save("settings_toggle", s)

    s = canvas(0.85)
    for t, f in ((0, 523), (0.15, 659), (0.30, 784), (0.45, 1046)):
        tone(s, t, 0.35, f, f, 0.21, 5)
        tone(s, t, 0.27, f * 2, f * 2, 0.08, 8)
    save("level_win", s)

    s = canvas(0.70)
    tone(s, 0, 0.30, 450, 310, 0.33, 6)
    tone(s, 0.26, 0.41, 310, 185, 0.33, 5)
    save("level_lose", s)

    s = canvas(0.27)
    tone(s, 0, 0.23, 720, 1080, 0.38, 8)
    tone(s, 0.03, 0.19, 1080, 1400, 0.16, 10)
    save("chain_step", s)


if __name__ == "__main__":
    make()
    print(f"Generated {len(list(ROOT.glob('*.wav')))} WAV drafts in {ROOT}")
