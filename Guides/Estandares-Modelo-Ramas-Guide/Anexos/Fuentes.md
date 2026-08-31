---
doc_id: GF-AX-FU
doc_type: anexo
title: Anexo — fuentes y fuerza de la evidencia
status: vigente
origin: agente
confidence: alta
owner: Lab-GitFlow
last_review: 2026-08-23
audience: [desarrollo, qa, devops, po]
traces: [GF-04, GF-05, GF-06, GF-07, GF-08]
---

# Fuentes

Toda la guía marca sus afirmaciones con **[F]** —fundamentada en una fuente de esta tabla— o **[C]**
—convención de este equipo, discutible y cambiable—. La separación es lo que permite discutir una
decisión propia sin tener que discutir el estándar que la rodea, y viceversa.

## Verificables en línea

Las URL se comprobaron accesibles el **2026-08-23**; se registra el código de respuesta obtenido.

| ID | Fuente | URL | Estado |
|---|---|---|---|
| DORA-1 | DORA — *Trunk-based development* | https://dora.dev/capabilities/trunk-based-development/ | 200 |
| TBD-1 | Trunk Based Development — *Branch for release* | https://trunkbaseddevelopment.com/branch-for-release/ | 200 |
| TBD-2 | Trunk Based Development — *You're doing it wrong* | https://trunkbaseddevelopment.com/youre-doing-it-wrong/ | citada por el insumo |
| GOOG-1 | Google Engineering Practices — *Small CLs* | https://google.github.io/eng-practices/review/developer/small-cls.html | 200 |
| GOOG-2 | Google Engineering Practices — *Speed of Code Reviews* | https://google.github.io/eng-practices/review/reviewer/speed.html | citada por el insumo |
| SRE-1 · SRE-2 · SRE-3 | Google SRE Book — *Release Engineering* | https://sre.google/sre-book/release-engineering/ | citada por el insumo |
| GL-1 | GitLab — *GitLab Flow best practices* | https://about.gitlab.com/topics/version-control/what-are-gitlab-flow-best-practices/ | 200 |
| NVIE-1 | Vincent Driessen — *A successful Git branching model*, con la nota de reflexión de marzo de 2020 | https://nvie.com/posts/a-successful-git-branching-model/ | 200, texto leído |
| GH-1 | GitHub Docs — *GitHub flow* | https://docs.github.com/en/get-started/using-github/github-flow | 200, texto leído |
| GHA-1 | GitHub Docs — *Reusing workflows* | https://docs.github.com/en/actions/using-workflows/reusing-workflows | 200 |
| PW-1 | Playwright — *Continuous Integration* | https://playwright.dev/docs/ci | 200 |
| PYT-1 | PyTorch — Release tracker con criterios de cherry-pick | https://github.com/pytorch/pytorch/issues/113962 | citada por el insumo |
| NIST-1 | NIST SP 800-218 — Secure Software Development Framework | https://www.cisa.gov/resources-tools/resources/nist-sp-800-218-secure-software-development-framework-v11-recommendations-mitigating-risk-software | citada por el insumo |
| ISO-9241 | ISO 9241-210 — Diseño centrado en el ser humano | https://www.iso.org/standard/77520.html | citada por el insumo |
| SWEBOK-1 | IEEE Computer Society — SWEBOK v4.0 | https://www.computer.org/education/bodies-of-knowledge/software-engineering | citada por el insumo |
| SEMVER-1 | Semantic Versioning | https://semver.org/ | 200 |
| CC-1 | Conventional Commits | https://www.conventionalcommits.org/ | 200 |

«Citada por el insumo» significa que la afirmación proviene del documento
`Flujo-De-Trabajo-Ramas.md` del equipo, que la respalda con esa fuente, y que en esta ejecución **no**
se volvió a abrir la fuente original. Quien necesite apoyarse fuerte en una de esas afirmaciones
debería verificarla de primera mano.

## Referencias normativas de acceso pago

| ID | Norma | Uso en la guía |
|---|---|---|
| ISO-12207 | ISO/IEC/IEEE 12207 — Procesos del ciclo de vida del software | Gestión de configuración, líneas base, control de cambios |
| ISO-29119 | ISO/IEC/IEEE 29119 parte 3 — Documentación de pruebas | Requisitos del ambiente de prueba, incluida la fidelidad |
| ITIL-1 | ITIL 4 — Práctica de habilitación de cambios | Autoridad según riesgo, cambios estándar preaprobados, revisión posterior |
| ISTQB-1 | Esquema de certificación ISTQB | Diferenciación de funciones dentro de QA |

De estas cuatro solo son públicamente accesibles el alcance y el índice; el texto completo requiere
licencia. Las afirmaciones que se apoyan en ellas provienen del documento del equipo, no de una
lectura directa de la norma en esta ejecución.

## Sobre la fuerza de la evidencia

Las fuentes no son homogéneas y conviene no tratarlas como si lo fueran.

**DORA** aporta datos empíricos de encuesta, con diseño transversal y autorreporte: establece
correlación, no causalidad. Sirve para orientar, no para zanjar una discusión.

**Google, GitLab, PyTorch y Trunk Based Development** aportan práctica documentada de organizaciones
concretas. Es experiencia validada a escala, no investigación controlada, y viene con el contexto de
esas organizaciones pegado.

**ISO, IEEE, NIST e ITIL** aportan marcos normativos de proceso: definen qué debe existir y quién
responde, no qué modelo de ramas usar. Ninguno prescribe ramas.

**SWEBOK** aporta consenso académico sobre las áreas de conocimiento de la disciplina.

Ninguna de ellas prescribe literalmente el modelo de esta guía. Lo que la guía hace es componer un
modelo concreto a partir de ellas, y las decisiones de esa composición están marcadas **[C]** para
que se puedan discutir por separado de su fundamento.

## Insumo del equipo

| Documento | Rol en esta guía |
|---|---|
| `Flujo-De-Trabajo-Ramas.md` | Propuesta de flujo del equipo. Es el origen del modelo de [06](../06-Modelo-Adoptado.md), de los criterios de [07](../07-Integracion-Y-Versionado.md) y de buena parte del anexo de preguntas |
| `Lab-E2E.WebBlazor` | Aplicación bajo prueba y origen del pipeline de E2E que usa la [guía práctica](../../GitFlow-Practice-Guide/README.md) |
