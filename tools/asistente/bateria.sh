#!/usr/bin/env bash
# Batería del asistente (bateria.v3 por defecto; BATERIA_ARCHIVO=bateria.v2.json para la anterior), tres
# corridas (K3 §2, R2.5): mínimo de las tres ≥ el de la versión (43/46; 40/43 en v2), cero cifras
# inventadas o atribuidas a otra categoría y, a ciegas, ninguna redactada peor que la fija. El modelo llega por el entorno y nunca se escribe en el repositorio:
#
#   OLLAMA_ENDPOINT=… OLLAMA_MODEL=… tools/asistente/bateria.sh [carpeta-de-salida]
#
# Sin OLLAMA_* corre la línea base (camino de la plantilla). Cada corrida deja corrida-N.jsonl
# (un registro por caso, recalculable) y resumen-N.json. Si la composición de la batería cambió
# entre corridas (hash distinto), el script se niega a sumar.
#
# Estabilidad: con temperatura 0 y la misma semilla las tres corridas salían idénticas palabra por
# palabra y no medían nada. La corrida 1 es la de producción (semilla 7, temperatura 0); las demás
# cambian semilla y temperatura (SEMILLAS, TEMPERATURAS). VARIAR=0 vuelve a tres corridas iguales.
set -euo pipefail

RAIZ="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
SALIDA="${1:-$RAIZ/resultados/bateria-$(date +%Y%m%d-%H%M%S)}"
CORRIDAS="${CORRIDAS:-3}"
mkdir -p "$SALIDA"

dotnet build "$RAIZ/tests/MovilidadUrbana.Asistente.Bateria" -c Release -warnaserror >/dev/null

VARIAR="${VARIAR:-1}"
read -r -a SEMILLAS <<<"${SEMILLAS:-7 11 23 31 47}"
read -r -a TEMPERATURAS <<<"${TEMPERATURAS:-0 0.5 0.5 0.7 0.7}"
for n in $(seq 1 "$CORRIDAS"); do
  if [ "$VARIAR" = 1 ]; then semilla="${SEMILLAS[$(( (n-1) % ${#SEMILLAS[@]} ))]}"; temperatura="${TEMPERATURAS[$(( (n-1) % ${#TEMPERATURAS[@]} ))]}"; else semilla=7; temperatura=0; fi
  Asistente__Semilla="$semilla" Asistente__Temperatura="$temperatura" \
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
peores = sum(r.get("comparacionACiegas", {}).get("peores", 0) for r in resumenes)
variacion = [{"corrida": r["corrida"], "semilla": r.get("semilla"), "temperatura": r.get("temperatura")} for r in resumenes]
# Estabilidad: cuántos casos redactados dieron el mismo texto en todas las corridas.
textos = {}
for i in range(1, n + 1):
    for l in (salida / f"corrida-{i}.jsonl").read_text(encoding="utf-8").splitlines():
        c = json.loads(l)
        textos.setdefault(c["id"], []).append((c["redactadaConIa"], c["texto"]))
conIa = {k: v for k, v in textos.items() if any(x[0] for x in v)}
identicos = sorted(k for k, v in conIa.items() if len({t for _, t in v}) == 1)
veredicto = min(ks) >= minimo and inventados == 0 and peores == 0
print(f"{resumenes[0]['bateria']} ({resumenes[0]['camino']}): k = {ks} / {m}; mínimo {min(ks)} (umbral {minimo}); inventados {inventados}; "
      f"redactadas con IA {[r.get('redactadas') for r in resumenes]}; "
      f"p50 {[r['p50ms'] for r in resumenes]} ms; p95 {[r['p95ms'] for r in resumenes]} ms")
print(f"variación {variacion}; a ciegas, redactada peor que la fija: {peores}; "
      f"casos con IA idénticos en todas las corridas: {len(identicos)} de {len(conIa)}")
print(f"casos con alguna falla: {', '.join(sorted(inestables)) or 'ninguno'}")
print("VEREDICTO:", "CUMPLE" if veredicto else "NO CUMPLE")
(salida / "veredicto.json").write_text(json.dumps({"k": ks, "m": m, "minimo": min(ks), "umbral": minimo, "inventados": inventados,
    "inestables": sorted(inestables), "peoresACiegas": peores, "variacion": variacion,
    "conIa": sorted(conIa), "identicosEntreCorridas": identicos, "cumple": veredicto}, ensure_ascii=False, indent=2), encoding="utf-8")
sys.exit(0 if veredicto else 1)
PY
