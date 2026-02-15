# Current Plan: Automated Issue Resolution & Warning Cleanup

**Status:** COMPLETED (nullable warnings phase)
**Branch:** `main` (v1.81.0-beta.2)
**Last updated:** 2026-02-15
**Released:** `v1.81.0-beta.2` — https://github.com/robertpopa22/mRemoteNG/releases/tag/20260215-v1.81.0-beta.2-NB-(3396)
**Last commit:** `2597fafe` — fix(ci): read version from csproj instead of hardcoded values

---

## Obiectiv

Sistem automat care rezolva issues si warnings, condus de un orchestrator Python
care apeleaza Claude Code ca sub-agent. Totul automat, pas cu pas, cu monitoring.

## Arhitectura orchestratorului

```
orchestrate.py (Python — ruleaza continuu)
│
├── FLUX 1: Open Issues (upstream GitHub)
│   ├── Sync issues (Sync-Issues.ps1)
│   ├── AI Triage (claude -p → analizeaza fiecare issue)
│   │   └── Decizie: implement / wontfix / needs-info / duplicate
│   ├── Pentru fiecare "implement":
│   │   ├── claude -p "fix issue #N" (cu context din issue + cod)
│   │   ├── build.ps1 → verifica compilare
│   │   ├── dotnet test → verifica teste
│   │   ├── git commit -m "fix(#N): description"
│   │   ├── git push
│   │   └── gh issue comment #N → "Fixed, test in beta [link]"
│   └── Update issue status in IIS JSON
│
├── FLUX 2: Warning Cleanup (CS8xxx)
│   ├── build.ps1 → parseaza warnings
│   ├── Grupeaza pe fisier
│   ├── Pentru fiecare fisier:
│   │   ├── claude -p "fix warnings in file X"
│   │   ├── build.ps1 → verifica
│   │   ├── dotnet test → verifica
│   │   └── git commit
│   └── Actualizeaza metrici in CURRENT_PLAN.md
│
└── MONITORING (live)
    ├── orchestrator-status.json (stare masina)
    ├── orchestrator.log (log detaliat)
    └── Console: progress bar + indicatori
```

## Fisiere orchestrator

| Fisier | Scop |
|--------|------|
| `.project-roadmap/scripts/orchestrate.py` | Script principal — ruleaza tot |
| `.project-roadmap/scripts/orchestrator-status.json` | Stare live (citibil de orice agent/tool) |
| `.project-roadmap/scripts/orchestrator.log` | Log detaliat cu timestamps |

## Monitoring — orchestrator-status.json

```json
{
  "started_at": "2026-02-15T10:00:00",
  "running": true,
  "current_phase": "issues",
  "current_task": {
    "type": "issue_fix",
    "issue": 2735,
    "step": "building",
    "file": "Connection/Protocol/RDP/RdpProtocol.cs",
    "started_at": "2026-02-15T10:15:30"
  },
  "issues": {
    "total_synced": 42,
    "triaged": 35,
    "to_implement": 12,
    "implemented": 3,
    "failed": 1,
    "skipped_wontfix": 8,
    "skipped_duplicate": 4,
    "skipped_needs_info": 5,
    "commented_on_github": 3
  },
  "warnings": {
    "total_start": 4831,
    "total_now": 4200,
    "fixed_this_session": 631,
    "by_type": {
      "CS8618": {"start": 386, "now": 120, "fixed": 266},
      "CS8602": {"start": 846, "now": 700, "fixed": 146}
    }
  },
  "commits": [
    {"hash": "abc1234", "message": "fix(#2735): RDP focus", "tests_passed": true},
    {"hash": "def5678", "message": "chore: fix 15 CS8618 in UI/Controls", "tests_passed": true}
  ],
  "errors": [
    {"time": "10:25:00", "task": "issue_3044", "step": "test", "error": "2 tests failed"}
  ],
  "last_updated": "2026-02-15T10:20:00"
}
```

### Console output (live)

```
=== mRemoteNG Orchestrator ===
Phase: ISSUES  [=========>          ] 3/12 implemented
  Current: Issue #2735 — RDP SmartSize focus loss
  Step: TESTING (dotnet test... 2174/2179 passed)

Warnings: 4831 → 4200 (-631)  [============>       ] 13% fixed
  CS8618: 386→120  CS8602: 846→700

Commits: 7 (all green)  |  Errors: 1 (issue #3044 — test fail, skipped)
Last activity: 2s ago
```

---

## FLUX 1: Open Issues — detaliat

### Pas 1: Sync
```python
# Ruleaza Sync-Issues.ps1 — trage issues noi de pe upstream
subprocess.run(["powershell.exe", "-NoProfile", "-File", "Sync-Issues.ps1"])
```

### Pas 2: AI Triage (pentru fiecare issue cu `our_status == "new"`)
```python
# Claude analizeaza issue-ul si decide
prompt = f"""
Esti un triager pentru proiectul mRemoteNG (.NET 10, WinForms).
Analizeaza acest issue GitHub si decide ce facem:

Issue #{issue['number']}: {issue['title']}
Labels: {issue['labels']}
Body: {issue['body'][:2000]}
Comments: {last_3_comments}

Raspunde STRICT in JSON:
{{
  "decision": "implement|wontfix|duplicate|needs_info",
  "reason": "explicatie scurta",
  "priority": "P0|P1|P2|P3|P4",
  "estimated_files": ["path/to/file1.cs", "path/to/file2.cs"],
  "approach": "descriere scurta a fix-ului propus"
}}
"""
result = subprocess.run(["claude", "-p", prompt, "--output-format", "json"], capture_output=True)
```

### Pas 3: Implementare (pentru fiecare `decision == "implement"`)
```python
prompt = f"""
Proiect: mRemoteNG (.NET 10, WinForms, COM references)
Branch: main
Build: powershell.exe -NoProfile -ExecutionPolicy Bypass -File build.ps1

Fix issue #{issue['number']}: {issue['title']}
{issue['body'][:3000]}

Abordare recomandata de triage: {triage['approach']}
Fisiere probabile: {triage['estimated_files']}

REGULI:
- Citeste codul INAINTE de a modifica
- NU schimba comportament existent — doar fix-ul cerut
- NU crea teste interactive (dialog, MessageBox, notepad.exe)
- Dupa fix, ruleaza: powershell.exe -NoProfile -ExecutionPolicy Bypass -File build.ps1
- Dupa build, ruleaza: dotnet test mRemoteNGTests/bin/x64/Release/mRemoteNGTests.dll --filter "FullyQualifiedName!~UI&FullyQualifiedName!~CueBanner&FullyQualifiedName!~Tree.ConnectionTreeTests&FullyQualifiedName!~PasswordForm" -- NUnit.DefaultTimeout=5000

Fa DOAR fix-ul. Nimic altceva.
"""
result = subprocess.run(["claude", "-p", prompt], capture_output=True, timeout=600)
```

### Pas 4: Verificare (build + test, independent de Claude)
```python
# Orchestratorul ruleaza build SI test SINGUR — nu se bazeaza pe Claude
build_ok = run_build()   # powershell build.ps1
test_ok = run_tests()    # dotnet test ... --filter ...

if build_ok and test_ok:
    git_commit(f"fix(#{issue_num}): {short_description}")
    git_push()
    post_github_comment(issue_num, commit_hash)
    update_issue_status(issue_num, "testing")
else:
    log_error(issue_num, "build" if not build_ok else "test")
    # NU comite, trece la urmatorul issue
```

### Pas 5: Comentariu GitHub (dupa succes)
```python
comment = f"""
✅ **Fix available for testing**

**Commit:** [`{commit_hash[:8]}`](https://github.com/robertpopa22/mRemoteNG/commit/{commit_hash})
**Branch:** `main`
**What changed:** {short_description}

📥 **Download latest beta:** [v1.81.0-beta.2](https://github.com/robertpopa22/mRemoteNG/releases/tag/v1.81.0-beta.2)

Please test and report if this resolves your issue.

---
🤖 _Automated by mRemoteNG Issue Intelligence System_
"""
subprocess.run(["gh", "issue", "comment", str(issue_num),
                "--repo", "mRemoteNG/mRemoteNG", "--body", comment])
```

---

## FLUX 2: Warning Cleanup — detaliat

### Pas 1: Extrage warnings din build
```python
# Ruleaza build, capteaza output, parseaza warnings
# Grupeaza: {fisier: [{linie, cod_warning, mesaj}]}
build_output = run_build(capture=True)
warnings = parse_warnings(build_output)  # regex pe "CS8618", "CS8602" etc.
files_by_warning_count = group_by_file(warnings)  # sortat descrescator
```

### Pas 2: Fix pe fisier (grupat)
```python
for file_path, file_warnings in files_by_warning_count:
    prompt = f"""
    Proiect: mRemoteNG (.NET 10, WinForms)
    Fisier: {file_path}

    Fixeaza TOATE aceste warnings nullable:
    {format_warnings(file_warnings)}

    REGULI CRITICE:
    - Cand adaugi `?` la un field, verifica TOATE utilizarile — adauga `?.` si `?? default`
    - NU genera CS8602 noi — fixeaza cascada imediat
    - NU schimba logica/comportament — doar tipuri si null checks
    - Getter cu .Trim() → ?.Trim() ?? string.Empty
    - Foloseste `= null!` DOAR pentru fields initializate sigur in constructor/init
    """
    subprocess.run(["claude", "-p", prompt], timeout=300)

    # Verificare independenta
    if run_build() and run_tests():
        git_commit(f"chore: fix {len(file_warnings)} nullable warnings in {basename(file_path)}")
    else:
        git_restore(file_path)  # REVERT — nu lasa cod stricat
        log_error(file_path, "warning_fix_failed")
```

---

## Prioritizare executie

| Prioritate | Ce | De ce |
|------------|-----|-------|
| **1** | Open issues P0-P1 (critical/security) | Impact direct pe utilizatori |
| **2** | Open issues P2 (bugs) | Fix-uri concrete, feedback rapid |
| **3** | CS8618 warnings (386 ramase) | Cel mai mare impact pe code quality |
| **4** | CS8602 warnings (846) | Multe generate de fix-urile CS8618 |
| **5** | Open issues P3-P4 (enhancement/debt) | Nice-to-have |
| **6** | Restul warnings (CS8600, CS8604, etc.) | Cleanup gradual |
| **7** | CA1416 (1,795) | Cosmetic — suprima cu NoWarn |

---

## Ce s-a facut deja (2026-02-14)

### Sesiune Gemini CLI (esuata, dar munca salvata)
- Gemini CLI a rezolvat **466 din 852 CS8618** warnings
- Picat din cauza JSON malformat (backslash-uri Windows)
- Munca salvata in commit `d60d2b80`

### Fixuri post-Gemini (sesiune Claude Code)
- GetPropertyValue() crash fix (pattern matching safe)
- 10 .Trim() null-safety fixes in AbstractConnectionRecord.cs
- Revert CSV password masking (schimbare neautorizata de comportament)
- Sters ThemeSerializerTests.cs (test gresit)
- Cleanup: 25 branch-uri, 25 worktrees, .auto-claude/
- Comise in `a653e86f`

### Stare curenta (v1.81.0-beta.2, 2026-02-15)
- **Build:** compileaza fara erori
- **Teste:** 1926/1926 passed (headless), 0 failures
- **Nullable warnings:** 0 (2,554 fixed, 100% clean)
- **Release:** v1.81.0-beta.2 published, 6 assets, all 7 CI jobs passed

---

## Lectii invatate (CRITICE — citeste INAINTE de executie)

### 1. Efectul cascada CS8618 → CS8602
Cand fixezi CS8618 (adaugi `?`), genereaza CS8602 pe fiecare utilizare a field-ului.
**FIX:** Adauga `?.` si null checks pe TOATE utilizarile, nu doar pe declaratie.

### 2. NU schimba comportamentul — doar tipurile
Gemini a mascat parolele in CSV export. Orice schimbare de logica e interzisa.
**REGULA:** Doar declaratii de tip si null checks. Nimic altceva.

### 3. NU crea teste interactive
Teste cu dialog, MessageBox, notepad.exe BLOCHEAZA executia.
**REGULA:** Teste 100% automate, timeout 5s, mock pe orice UI dependency.

### 4. GetPropertyValue() — pattern critic
```csharp
// GRESIT: return (TPropertyType)reflection_result;  // crash pe null
// CORECT: return result is TPropertyType typed ? typed : value;
```

### 5. Verificare INDEPENDENTA de Claude
Orchestratorul ruleaza build si test SINGUR dupa fiecare task Claude.
NU te baza pe Claude ca a rulat build-ul corect — verifica mereu.

### 6. REVERT pe esec
Daca build sau test pica dupa fix, `git restore` imediat. Nu lasa cod stricat.
Logheaza eroarea si treci la urmatorul task.

---

## Metrici de progres

| Data | Agent | CS8618 | CS8602 | Total | Issues Fixed | Commit |
|------|-------|--------|--------|-------|-------------|--------|
| 2026-02-14 (baseline) | — | 852 | ~236 | ~4,200 | 0 | — |
| 2026-02-14 (Gemini) | Gemini | 386 | 846 | ~4,831 | 0 | d60d2b80 |
| 2026-02-14 (fixuri) | Claude | 386 | 846 | ~4,831 | 0 | a653e86f |
| 2026-02-14-15 (IIS) | Claude+Gemini | 0 | 0 | 0 | 0 | c935f161 |
| **FINAL** | **All** | **0** | **0** | **0** | **0** | **v1.81.0-beta.2** |

---

## Comentariu GitHub — template

Postat automat pe fiecare issue rezolvat:

```markdown
✅ **Fix available for testing**

**Commit:** [`{hash}`](https://github.com/robertpopa22/mRemoteNG/commit/{hash})
**Branch:** `main`
**What changed:** {description}

📥 **Download latest beta:** [v1.81.0-beta.2](https://github.com/robertpopa22/mRemoteNG/releases/tag/v1.81.0-beta.2)

Please test and report if this resolves your issue.

---
🤖 _Automated by mRemoteNG Issue Intelligence System_
```

---

## Alte probleme active (nu fac parte din orchestrator)

| Problema | Referinta | Status |
|----------|-----------|--------|
| CVE-2023-30367 SecureString migration | `.project-roadmap/CVE-2023-30367_ASSESSMENT.md` | Deferred to v1.81.0 |
| BinaryFormatter .NET 10 crash | `.project-roadmap/ISSUE_BINARYFORMATTER.md` | Fixed (workaround) |
| Upstream PR #3134 | GitHub | Awaiting maintainer approval |
