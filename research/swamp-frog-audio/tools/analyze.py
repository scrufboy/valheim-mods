#!/usr/bin/env python3
""""Tone check" pass: measure every clip and propose a creature slot.

Deps: numpy, scipy, soundfile (pip install numpy scipy soundfile).
  python analyze.py work/raw --out work/features.csv [--candidates work/candidates.csv]

Splits long files into call events (energy gate), measures each event and writes one
row per event. Slot guesses are heuristics for a human/AI ear pass to confirm -
they are suggestions, not decisions.
"""
import argparse, csv, json, sys
from pathlib import Path
import numpy as np
import soundfile as sf
from scipy.signal import resample_poly, stft

SR = 22050


def load(p):
    x, sr = sf.read(str(p), always_2d=True)
    x = x.mean(axis=1)
    if sr != SR:
        g = np.gcd(int(sr), SR)
        x = resample_poly(x, SR // g, int(sr) // g)
    return x.astype(np.float32)


def events(x, frame=512, gate_db=-30, min_len=0.08, merge=0.15):
    n = len(x) // frame
    if n == 0:
        return []
    rms = np.sqrt((x[: n * frame].reshape(n, frame) ** 2).mean(axis=1) + 1e-12)
    db = 20 * np.log10(rms / (rms.max() + 1e-12))
    on = db > gate_db
    segs, start = [], None
    for i, v in enumerate(np.append(on, False)):
        if v and start is None:
            start = i
        elif not v and start is not None:
            segs.append([start, i]); start = None
    merged = []
    for s in segs:
        if merged and (s[0] - merged[-1][1]) * frame / SR < merge:
            merged[-1][1] = s[1]
        else:
            merged.append(s)
    return [(a * frame, b * frame) for a, b in merged if (b - a) * frame / SR >= min_len]


def f0_autocorr(seg, fmin=40, fmax=1500):
    seg = seg - seg.mean()
    if len(seg) < SR // fmin * 2:
        return 0.0, 0.0
    ac = np.correlate(seg, seg, "full")[len(seg) - 1:]
    ac /= ac[0] + 1e-12
    lo, hi = SR // fmax, min(SR // fmin, len(ac) - 1)
    k = lo + int(np.argmax(ac[lo:hi]))
    return SR / k, float(ac[k])  # pitch, periodicity (0 = noisy/rough, 1 = tonal)


def measure(seg):
    dur = len(seg) / SR
    peak = float(np.abs(seg).max() + 1e-12)
    rms = float(np.sqrt((seg ** 2).mean()) + 1e-12)
    f, _, Z = stft(seg, SR, nperseg=1024, noverlap=768)
    mag = np.abs(Z) + 1e-12
    spec = mag.mean(axis=1)
    centroid = float((f * spec).sum() / spec.sum())
    flatness = float(np.exp(np.log(spec).mean()) / spec.mean())
    low_ratio = float(spec[f < 400].sum() / spec.sum())
    env = mag.sum(axis=0)
    attack = float(np.argmax(env) * 256 / SR)
    pulses = int(((env[1:-1] > env[:-2]) & (env[1:-1] > env[2:]) & (env[1:-1] > env.max() * 0.5)).sum())
    f0, period = f0_autocorr(seg[: min(len(seg), SR)])
    return dict(dur_s=round(dur, 3), peak_db=round(20 * np.log10(peak), 1),
                crest_db=round(20 * np.log10(peak / rms), 1), centroid_hz=round(centroid),
                flatness=round(flatness, 3), low_ratio=round(low_ratio, 3),
                f0_hz=round(f0), periodicity=round(period, 2), attack_s=round(attack, 3),
                pulse_rate_hz=round(pulses / dur, 1) if dur else 0)


def guess(m):
    """Heuristic slot proposals, best first."""
    d, c, lo, per, pr = m["dur_s"], m["centroid_hz"], m["low_ratio"], m["periodicity"], m["pulse_rate_hz"]
    g = []
    if d > 15: g.append("ambience_bed")
    if d >= 0.8 and c > 1500 and per < 0.6: g.append("death")
    if 0.2 <= d <= 0.8 and c > 1500: g.append("hurt")
    if d <= 0.8 and c > 2000 and m["attack_s"] < 0.05: g.append("alarmed")
    rough = per < 0.7 or pr > 6 or m["flatness"] > 0.2
    if c < 1200 and rough and 0.3 <= d <= 2.5: g.append("battle_cry")
    if c < 1200 and d <= 1.5: g.append("hunt")
    if d <= 0.6 and per >= 0.5: g.append("exclaim")
    if 0.2 <= d <= 1.0 and per >= 0.5: g.append("investigate")
    if 0.3 <= d <= 4.0 and per >= 0.6: g.extend(["respond", "night_chatter"])
    out = []
    [out.append(s) for s in g if s not in out]
    return out[:3] or ["unsorted"]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("folder")
    ap.add_argument("--out", default="features.csv")
    ap.add_argument("--candidates", help="candidates.csv from harvest.py (adds licence/tier)")
    a = ap.parse_args()
    meta = {}
    if a.candidates:
        for r in csv.DictReader(open(a.candidates, encoding="utf-8")):
            if r.get("local_file"):
                meta[Path(r["local_file"]).name] = r
    rows = []
    for p in sorted(Path(a.folder).glob("*")):
        if p.suffix.lower() not in (".wav", ".flac", ".ogg", ".mp3", ".aif", ".aiff"):
            continue
        try:
            x = load(p)
        except Exception as e:
            print(f"skip {p.name}: {e}", file=sys.stderr); continue
        for i, (s, e) in enumerate(events(x) or [(0, len(x))]):
            m = measure(x[s:e])
            rows.append(dict(file=p.name, event=i, start_s=round(s / SR, 3), end_s=round(e / SR, 3),
                             **m, slot_guess="|".join(guess(m)),
                             licence=meta.get(p.name, {}).get("licence", ""),
                             tier=meta.get(p.name, {}).get("tier", ""),
                             ear_verdict="", keep=""))
    if not rows:
        print("no audio found"); return
    with open(a.out, "w", newline="", encoding="utf-8") as f:
        w = csv.DictWriter(f, list(rows[0].keys())); w.writeheader(); w.writerows(rows)
    print(f"{len(rows)} events -> {a.out}")


if __name__ == "__main__":
    main()
