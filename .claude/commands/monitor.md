# Monitor Orchestrator

Verifică starea orchestratorului și rezolvă probleme automat.

## Pași (execută în ordine):

### 1. Verifică instanțe active
```bash
ps aux | grep -i orchestrator | grep -v grep
ls -la D:/github/mRemoteNG/.project-roadmap/scripts/orchestrator.lock 2>/dev/null
```
- Dacă există mai multe instanțe → kill duplicatele (păstrează cea mai veche)
- Dacă există lock file fără proces activ → șterge lock file-ul stale

### 2. Verifică erori recente din log
```bash
tail -100 D:/github/mRemoteNG/.project-roadmap/scripts/orchestrator.log
```
- Caută: `ERROR`, `FATAL`, `rate.limit`, `stuck`, `phantom`, `lock`
- Raportează ultimele erori cu timestamp

### 3. Verifică erori din baza de date (dacă accesibilă)
```sql
SELECT TOP 20 * FROM app_log WHERE level='ERROR' ORDER BY timestamp DESC
```

### 4. Verifică starea curentă
```bash
cat D:/github/mRemoteNG/.project-roadmap/scripts/orchestrator-status.json
cat D:/github/mRemoteNG/.project-roadmap/scripts/_agent_rate_limits.json
```

### 5. Sumar
Prezintă un raport concis:
- Status: Running / Dead / Multiple instances
- Ultimele erori (dacă există)
- Rate limits active pe agenți
- Recomandări de acțiune

## REGULI:
- **NU folosi `--batch-size`** — nu există
- **NU restartuiește** fără confirmarea user-ului
- Dacă orchestratorul e mort, întreabă user-ul dacă vrea restart
- La restart, verifică ÎNTÂI că nu mai sunt instanțe active
