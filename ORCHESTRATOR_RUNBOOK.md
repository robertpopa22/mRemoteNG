# Orchestrator Runbook (C3)

> **Referință rapidă pentru operarea orchestratorului Python (`iis_orchestrator.py`).**
> Citește ÎNAINTE de a diagnostica sau restartuì orchestratorul.

---

## Health Check — Procedura Standard

```bash
# 1. Procese active
ps aux | grep -i orchestrator | grep -v grep

# 2. Lock file
ls -la D:/github/mRemoteNG/.project-roadmap/scripts/orchestrator.lock 2>/dev/null

# 3. Ultimele log-uri
tail -50 D:/github/mRemoteNG/.project-roadmap/scripts/orchestrator.log

# 4. Status JSON
cat D:/github/mRemoteNG/.project-roadmap/scripts/orchestrator-status.json

# 5. Rate limits agenți
cat D:/github/mRemoteNG/.project-roadmap/scripts/_agent_rate_limits.json
```

---

## Procedura de Restart

1. **Verifică** că nu mai există instanțe active (vezi Health Check pas 1 și 2)
2. **Kill** orice proces rezidual: `kill <PID>`
3. **Șterge** lock file stale (doar dacă procesul nu mai există): `rm orchestrator.lock`
4. **Pornește**:
   ```bash
   cd D:/github/mRemoteNG/.project-roadmap/scripts
   python iis_orchestrator.py sync   # ÎNTOTDEAUNA sync ÎNTÂI
   python iis_orchestrator.py analyze # vezi ce e de făcut
   ```
5. **Monitorizează** primele 2-3 minute: `tail -f orchestrator.log`

---

## Bug-uri Cunoscute + Fix-uri Verificate

### 1. Phantom Processes / Instanțe Multiple
**Simptom:** Output garbled, conflicte git, lock file prezent dar orchestratorul "nu merge"
**Cauză:** Lock file nu s-a șters la crash anterior → instanță nouă pornită manual → 2 instanțe
**Fix:**
```bash
# Găsește TOATE instanțele
ps aux | grep -i orchestrator | grep -v grep
# Kill toate
kill <PID1> <PID2>
# Șterge lock
rm D:/github/mRemoteNG/.project-roadmap/scripts/orchestrator.lock
# Repornește UNA singură
```

### 2. Fix-uri Revertate de Git Checkout
**Simptom:** Bug reparat → reapare după ce orchestratorul procesează alt issue
**Cauză:** Orchestratorul face `git checkout`/`git restore` la eșec → revertează fix-uri uncommitted
**Fix:** **COMMIT IMEDIAT** după verificarea fix-ului. Nu lăsa NICIODATĂ un fix uncommitted.
```bash
git add <fișierele modificate>
git commit -m "fix(#NNNN): description"
```

### 3. Gemini Rate Limit Persistent
**Simptom:** Gemini refuză request-uri deși a trecut timpul de cooldown
**Cauză:** `_agent_rate_limits.json` are dată de reset incorectă sau nu se actualizează
**Fix:**
```bash
# Verifică starea
cat D:/github/mRemoteNG/.project-roadmap/scripts/_agent_rate_limits.json
# Dacă data de reset e în trecut, resetează manual:
# Editează fișierul și șterge entry-ul pentru gemini
```

### 4. Teste Phantom (completate în <10s)
**Simptom:** "2553 passed" dar în realitate testele nu au rulat
**Cauză:** DLL-uri lipsă, build incomplet, path greșit
**Fix:** Rebuild complet + verificare manuală:
```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "D:\github\mRemoteNG\build.ps1"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "D:\github\mRemoteNG\run-tests.ps1" -Headless
```

---

## Flag-uri / Comenzi Care NU Există

| NU folosi | De ce |
|-----------|-------|
| `--batch-size` | Nu e implementat în orchestrator |
| `dotnet build` | Eșuează pe COM references — folosește `build.ps1` |
| `git add -A` pe mRemoteNG | Poate include binare/artefacte — adaugă fișiere specific |

---

## Constante Critice (din orchestrator)

| Constantă | Valoare | Scop |
|-----------|---------|------|
| `TEST_MIN_DURATION_SECS` | 10 | Teste sub 10s = phantom |
| `TEST_MIN_COUNT` | 100 | Reject dacă prea puține teste |
| `IMPL_CONSECUTIVE_FAIL_LIMIT` | 5 | Stop după 5 eșecuri consecutive |
| `TEST_FIX_MAX_ATTEMPTS` | 2 | Max 2 încercări de fix teste |

---

## Fișiere Cheie

| Fișier | Rol |
|--------|-----|
| `iis_orchestrator.py` | Script principal |
| `orchestrator.log` | Log intern (citește ĂSTA, nu stdout) |
| `orchestrator-status.json` | Stare machine-readable |
| `orchestrator.lock` | Lock file (PID-based) |
| `_agent_rate_limits.json` | Rate limits persistente per agent |
| `_comment_rate.json` | Rate limit comentarii GitHub |
