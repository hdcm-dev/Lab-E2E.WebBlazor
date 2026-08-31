---
doc_id: GHF-00
doc_type: escenario-practico
title: 00 — Preparación del repositorio para GitHub Flow
status: vigente
origin: agente
confidence: media
owner: Lab-GitFlow
last_review: 2026-08-25
audience: [desarrollo, devops]
traces: [GHF-IDX, GF-08]
---

# 00 — Preparación

## Objetivo

Dejar el repositorio con la aplicación sembrada, las pruebas corriendo y la rama por defecto
protegida. Al terminar, un pull request no se puede mergear sin verificación en verde y una
aprobación: es la única barrera que este modelo tiene, y por eso se configura primero.

**Roles:** I1 siembra, I2 configura la protección, I3 comprueba que las pruebas corren en su
máquina.

## Precondición

- `Lab-GitFlow` existe y no tiene ramas de larga vida fuera de la principal. Si venís de la guía de
  GitFlow, el repositorio quedó con `release/1.0` y tags de versión: reiniciálo o usá otro.
- Los tres repositorios clonados **como hermanos** bajo un mismo directorio de trabajo:

  ```
  <directorio-de-trabajo>/
    Lab-GitFlow/                    ← el repositorio de práctica
    Lab-GitFlow.Documentacion/      ← este cuerpo documental
    Lab-E2E.WebBlazor/              ← la aplicación bajo prueba
  ```

- Docker instalado.
- `Lab-GitFlow` **privado y sin colaboradores externos**, por el runner autoalojado: en el evento
  `pull_request` se ejecuta el workflow *de la rama del pull request*, todavía sin revisar. Ver
  [el anexo de workflows](../Estandares-Modelo-Ramas-Guide/Anexos/workflows/README.md).

## Pasos

### 1. Sembrar la aplicación (I1)

```bash
cd <directorio-de-trabajo>/Lab-GitFlow
git checkout -b chore/1-sembrar-aplicacion

# Lista positiva de lo que la práctica necesita. Las exclusiones evitan arrastrar la salida de
# compilación: medido, 472 MB con `bin/` y `obj/` contra 524 KB sin ellos.
rsync -a --relative --exclude='bin/' --exclude='obj/' \
      ../Lab-E2E.WebBlazor/./{Lab-E2E.WebBlazor.sln,pruebas.runsettings,.gitignore,README.md} \
      ../Lab-E2E.WebBlazor/./{src,tests,scripts,.github} \
      ./

du -sh .          # esperado: menos de 1 MB
test -x scripts/pruebas.sh && test -f Lab-E2E.WebBlazor.sln && echo "siembra ok"

git add .gitignore README.md Lab-E2E.WebBlazor.sln pruebas.runsettings .github src tests scripts
git commit -m "chore: sembrar la aplicación de práctica y sus pruebas E2E"
git push -u origin chore/1-sembrar-aplicacion
```

Se integra por pull request, aunque todavía no haya nada que lo obligue: es el primer recorrido de
los seis pasos del modelo, y conviene hacerlo antes de que la protección lo imponga.

### 2. Comprobar que las pruebas corren (I3)

La suite es el proyecto .NET `tests/MovilidadUrbana.E2ETests`, configurado con
`pruebas.runsettings`.

```bash
scripts/pruebas.sh chromium   # publica la aplicación y corre las pruebas
```

**No hay que anteponer `scripts/publicar.sh`.** El fixture publica por su cuenta antes de la primera
prueba, dependiente del framework; si la carpeta ya tiene el binario autocontenido que deja
`publicar.sh`, la segunda publicación se superpone y el proceso muere con código 150 antes de
escuchar, con las 22 pruebas fallando en `OneTimeSetUp`. **[E: corrida local del 2026-08-24]** Para
ejercitar el artefacto autocontenido:

```bash
scripts/publicar.sh
PUBLICAR_ANTES_DE_PROBAR=false scripts/pruebas.sh chromium
```

### 3. El pipeline: acá no hay nada que agregar (I2)

Este es el paso donde más se nota el modelo, y consiste en **no hacer nada**. El `ci.yml` que viene
con la aplicación sembrada se dispara con `push` a `main`, con `pull_request` hacia `main` o
`develop`, y con `merge_group`. **[E]** Para GitHub Flow eso es exactamente la cobertura necesaria:
no hay ninguna otra rama de larga vida a la que proteger.

Los tres workflows del [anexo](../Estandares-Modelo-Ramas-Guide/Anexos/workflows/README.md) —que la
guía de GitFlow sí instala— aquí **sobran**, y conviene entender por qué:

| Workflow del anexo | Por qué no aplica |
|---|---|
| `ci.yml` | Agrega disparadores sobre `release/**`, ramas que este modelo no tiene |
| `release.yml` | Se dispara con un tag `v*`; GitHub Flow no define versionado **[F: GH-1]** |
| `auditoria-convergencia.yml` | Audita que toda corrección de una rama de release tenga equivalente en la principal. Sin ramas de release, no hay divergencia posible que auditar |

Si el equipo igual quiere tags de versión sobre la rama principal, es una decisión propia y hay que
declararla como tal: el modelo no la trae. **[C]**

### 4. Protección de la rama por defecto (I2)

En *Settings → Branches*, sobre `main`:

| Control | Valor |
|---|---|
| Require a pull request before merging | sí, con 1 aprobación |
| Require status checks to pass | sí, check obligatorio: `CI aprobada` |
| Require branches to be up to date | sí |
| Do not allow bypassing | sí, incluidos administradores |
| Automatically delete head branches | sí (*Settings → General*) |

La documentación del modelo menciona expresamente que la protección de rama puede impedir el merge
cuando no se cumplen los requisitos, por ejemplo una cantidad mínima de aprobaciones.
**[F: GH-1]** En GitHub Flow esa configuración no es un accesorio: es **el único** punto de control
del flujo, porque no hay ninguna otra etapa entre el merge y el despliegue.

## Qué observar

- Cuánto tarda el pipeline completo. Ese número es el costo fijo de cada cambio en este modelo, y
  el que decide si integrar varias veces por día es realista.
- Que el primer pull request corre el pipeline **antes** de que exista la protección: la diferencia
  entre «el pipeline informa» y «el pipeline bloquea» es el tema del escenario 03.
- Cuántos controles hicieron falta comparado con la preparación de la guía de GitFlow. Es la
  primera medición honesta de lo que cuesta cada modelo.

## Errores frecuentes

| Síntoma | Causa habitual |
|---|---|
| Los jobs quedan en cola para siempre | El runner no tiene la etiqueta `i7infra-dev`, o está apagado |
| El check obligatorio nunca aparece en la lista | El nombre configurado no coincide exactamente con el `name:` del job |
| Las 22 pruebas fallan en `OneTimeSetUp` con código 150 | Se corrió `publicar.sh` antes de `pruebas.sh` sin `PUBLICAR_ANTES_DE_PROBAR=false` |

## Verificación

1. `git ls-remote --heads origin` muestra solo `main`.
2. Un push directo a `main` es rechazado por el servidor.
3. Un pull request de prueba dispara el pipeline y el botón de merge queda bloqueado hasta que
   termina.
4. `scripts/pruebas.sh chromium` pasa en verde en la máquina de cada integrante.
5. No hay ningún workflow de release ni de auditoría en `.github/workflows/`.

---

Sigue: [01 — Funcionalidad nueva](01-Funcionalidad-Nueva.md).
