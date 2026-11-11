#!/usr/bin/env bash
# Batería del asistente (bateria.v2 por defecto; BATERIA_ARCHIVO=bateria.v1.json para la anterior), tres
# corridas (K3 §2, R2.5): mínimo de las tres ≥ el de la versión (40/43; 34/37 en v1) y cero cifras
# inventadas o atribuidas a otra categoría. El modelo llega por el entorno y nunca se escribe en el repositorio:
#
#   OLLAMA_ENDPOINT=… OLLAMA_MODEL=… tools/asistente/bateria.sh [carpeta-de-salida]
#
# Sin OLLAMA_* corre la línea base (camino de la plantilla). Cada corrida deja corrida-N.jsonl
# (un registro por caso, recalculable) y resumen-N.json. Si la composición de la batería cambió
# entre corridas (hash distinto), el script se niega a sumar.
set -euo pipefail

RAIZ="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
SALIDA="${1:-$RAIZ/resultados/bateria-$(date +%Y%m%d-%H%M%S)}"
CORRIDAS="${CORRIDAS:-3}"
mkdir -p "$SALIDA"

dotnet build "$RAIZ/tests/MovilidadUrbana.Asistente.Bateria" -c Release -warnaserror >/dev/null

for n in $(seq 1 "$CORRIDAS"); do
  BATERIA_CORRIDA="$n" BATERIA_SALIDA="$SALIDA" \
    dotnet test "$RAIZ/tests/MovilidadUrbana.Asistente.Bateria" -c Release --no-build \
      --logger "console;verbosity=normal" | grep -E '^ *(BATERIA|FALLA)' || true
done

python3 - "$SALIDA" "$CORRIDAS" <<'PY'
import json, sys, pathlib
salida, n = pathlib.Path(sys.argv[1]), int(sys.argv[2])
resumenes = [json.loads((salida / f"resumen-{i}.json").read_text(encoding="utf-8")) for i in range(1, n + 1)]
hashes = {r["hash"] for r in resumenes}
if len(hashes) != 1:
    sys.exit(f"La composición de la batería cambió entre corridas ({', '.join(sorted(hashes))}): no se suma.")
# Recálculo del veredicto desde los registros por caso: tiene que dar lo mismo que el resumen.
for i, r in enumerate(resumenes, 1):
    casos = [json.loads(l) for l in (salida / f"corrida-{i}.jsonl").read_text(encoding="utf-8").splitlines()]
    k = sum(1 for c in casos if c["ok"])
    if k != r["k"]:
        sys.exit(f"Corrida {i}: el recálculo da {k} y el resumen {r['k']}.")
ks = [r["k"] for r in resumenes]
m = resumenes[0]["m"]
inventados = sum(r["inventados"] for r in resumenes)
inestables = set()
for i in range(1, n + 1):
    for l in (salida / f"corrida-{i}.jsonl").read_text(encoding="utf-8").splitlines():
        c = json.loads(l)
        if not c["ok"]:
            inestables.add(c["id"])
minimo = resumenes[0].get("minimo", 34)
veredicto = min(ks) >= minimo and inventados == 0
print(f"{resumenes[0]['bateria']} ({resumenes[0]['camino']}): k = {ks} / {m}; mínimo {min(ks)} (umbral {minimo}); inventados {inventados}; "
      f"redactadas con IA {[r.get('redactadas') for r in resumenes]}; "
      f"p50 {[r['p50ms'] for r in resumenes]} ms; p95 {[r['p95ms'] for r in resumenes]} ms")
print(f"casos con alguna falla: {', '.join(sorted(inestables)) or 'ninguno'}")
print("VEREDICTO:", "CUMPLE" if veredicto else "NO CUMPLE")
(salida / "veredicto.json").write_text(json.dumps({"k": ks, "m": m, "minimo": min(ks), "umbral": minimo, "inventados": inventados,
    "inestables": sorted(inestables), "cumple": veredicto}, ensure_ascii=False, indent=2), encoding="utf-8")
sys.exit(0 if veredicto else 1)
PY
