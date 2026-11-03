#!/usr/bin/env python3
"""Harvest frog/toad recording candidates (metadata + optional audio) into candidates.csv.

Sources: Freesound APIv2, xeno-canto API v3, iNaturalist v1. Stdlib only.
Keys via env: FREESOUND_TOKEN, XC_API_KEY (iNaturalist needs none).

  python harvest.py --out work --sources freesound,xenocanto,inat --download
  python harvest.py --out work --ship-only        # CC0 / CC BY only

Status: written against the documented APIs; NOT run live (the authoring sandbox
cannot reach these hosts). First live run should use --limit 5 and eyeball the CSV.
"""
import argparse, csv, json, os, re, sys, time, urllib.parse, urllib.request
from pathlib import Path

UA = "LWC-swamp-frog-audio-harvest/1.0"
HERE = Path(__file__).resolve().parent
SLOTS = json.loads((HERE.parent / "slots.v1.json").read_text())

FREESOUND_QUERIES = ["bullfrog", "frog growl", "frog scream", "frog distress", "toad croak",
                     "frog croak", "pig frog", "frog chorus night", "frog underwater", "toad trill"]
XC_TYPES = ["territorial call", "distress call", "aggressive call", "alarm call",
            "release call", "advertisement call"]
INAT_ANURA_TAXON = 20979  # Anura (frogs & toads)

FIELDS = ["source", "id", "title", "species", "call_type", "licence", "tier", "duration_s",
          "recordist", "page_url", "audio_url", "query", "local_file"]


def get_json(url, headers=None):
    req = urllib.request.Request(url, headers={"User-Agent": UA, **(headers or {})})
    with urllib.request.urlopen(req, timeout=30) as r:
        return json.loads(r.read().decode("utf-8"))


def tier(lic):
    l = (lic or "").lower()
    if "nc" in re.split(r"[-/ ]", l) or "noncommercial" in l or "remarc" in l:
        return "NC"
    if "zero" in l or "cc0" in l or "creative commons 0" in l or "publicdomain" in l:
        return "SHIP"
    if "by" in l or "attribution" in l:
        return "SHIP-ATTRIB"
    return "REF"


def freesound(limit, ship_only):
    tok = os.environ.get("FREESOUND_TOKEN")
    if not tok:
        print("skip freesound: FREESOUND_TOKEN not set", file=sys.stderr); return
    lic = ' license:("Creative Commons 0" OR "Attribution")' if ship_only else ""
    for q in FREESOUND_QUERIES:
        url = ("https://freesound.org/apiv2/search/text/?" + urllib.parse.urlencode({
            "query": q, "filter": f"duration:[0.1 TO 180]{lic}", "page_size": limit,
            "fields": "id,name,tags,license,duration,username,previews,url", "token": tok}))
        for s in get_json(url).get("results", []):
            yield dict(source="freesound", id=s["id"], title=s["name"], species="",
                       call_type=" ".join(s.get("tags", [])[:8]), licence=s["license"],
                       tier=tier(s["license"]), duration_s=round(s["duration"], 2),
                       recordist=s["username"], page_url=s["url"],
                       audio_url=s["previews"].get("preview-hq-mp3", ""), query=q)
        time.sleep(1)


def xenocanto(limit, ship_only):
    key = os.environ.get("XC_API_KEY")
    if not key:
        print("skip xeno-canto: XC_API_KEY not set", file=sys.stderr); return
    for t in XC_TYPES:
        q = f'grp:frogs type:"{t}"'
        url = "https://xeno-canto.org/api/3/recordings?" + urllib.parse.urlencode(
            {"query": q, "key": key, "per_page": max(limit, 50)})
        for r in get_json(url).get("recordings", [])[:limit]:
            lic = r.get("lic", "")
            if ship_only and tier(lic) not in ("SHIP", "SHIP-ATTRIB"):
                continue
            yield dict(source="xeno-canto", id=f"XC{r.get('id')}", title=r.get("en", ""),
                       species=f"{r.get('gen','')} {r.get('sp','')}".strip(),
                       call_type=r.get("type", ""), licence=lic, tier=tier(lic),
                       duration_s=r.get("length", ""), recordist=r.get("rec", ""),
                       page_url=f"https://xeno-canto.org/{r.get('id')}",
                       audio_url=r.get("file", ""), query=q)
        time.sleep(1)


def inat(limit, ship_only):
    lic = "cc0,cc-by" if ship_only else "cc0,cc-by,cc-by-nc,cc-by-sa,cc-by-nc-sa"
    url = "https://api.inaturalist.org/v1/observations?" + urllib.parse.urlencode({
        "taxon_id": INAT_ANURA_TAXON, "sounds": "true", "sound_license": lic,
        "quality_grade": "research", "per_page": min(limit * 4, 200), "order_by": "votes"})
    for o in get_json(url).get("results", []):
        tx = o.get("taxon") or {}
        for s in o.get("sounds", []):
            yield dict(source="inaturalist", id=f"inat-sound-{s.get('id')}",
                       title=tx.get("preferred_common_name", ""), species=tx.get("name", ""),
                       call_type="", licence=s.get("license_code", ""),
                       tier=tier(s.get("license_code", "")), duration_s="",
                       recordist=s.get("attribution", ""), page_url=o.get("uri", ""),
                       audio_url=s.get("file_url", ""), query=f"taxon {INAT_ANURA_TAXON}")


def download(row, dest):
    if not row["audio_url"]:
        return ""
    ext = os.path.splitext(urllib.parse.urlparse(row["audio_url"]).path)[1] or ".mp3"
    out = dest / f"{row['source']}_{re.sub(r'[^A-Za-z0-9_-]', '_', str(row['id']))}{ext}"
    if not out.exists():
        req = urllib.request.Request(row["audio_url"], headers={"User-Agent": UA})
        with urllib.request.urlopen(req, timeout=60) as r:
            out.write_bytes(r.read())
        time.sleep(0.5)
    return str(out)


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--out", default="work")
    ap.add_argument("--sources", default="freesound,xenocanto,inat")
    ap.add_argument("--limit", type=int, default=25, help="per query")
    ap.add_argument("--ship-only", action="store_true", help="CC0 / CC BY only")
    ap.add_argument("--download", action="store_true")
    a = ap.parse_args()
    out = Path(a.out); (out / "raw").mkdir(parents=True, exist_ok=True)
    fns = {"freesound": freesound, "xenocanto": xenocanto, "inat": inat}
    seen, rows = set(), []
    for name in a.sources.split(","):
        try:
            for row in fns[name.strip()](a.limit, a.ship_only):
                if row["id"] in seen:
                    continue
                seen.add(row["id"])
                row["local_file"] = download(row, out / "raw") if a.download else ""
                rows.append(row)
        except Exception as e:  # keep other sources going
            print(f"{name} failed: {e}", file=sys.stderr)
    with open(out / "candidates.csv", "w", newline="", encoding="utf-8") as f:
        w = csv.DictWriter(f, FIELDS); w.writeheader(); w.writerows(rows)
    print(f"{len(rows)} candidates -> {out/'candidates.csv'}")


if __name__ == "__main__":
    main()
