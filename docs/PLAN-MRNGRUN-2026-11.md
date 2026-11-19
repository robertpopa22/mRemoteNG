# Plan de aliniere IIS și Fork Radar

## Scop și criteriu de închidere

IIS este automatizarea pentru backlogul de issue-uri și avertismente. Fork Radar
colectează și evaluează schimbări din forkuri. Alinierea permite fiecăruia să reia
loturi limitate, cu starea și efectele verificabile. Planul urmează deciziile
D3, D4, D6, D7 și D9–D15 din [Carta proiectului](../CHARTER.md).

Lucrarea este gata când fiecare regim activat trece testele de regresie și o
probă limitată pe revizia exactă a runtime-ului. Proba dovedește că indexul,
modificările locale și procesele altor sesiuni sunt păstrate. Orice candidat IIS
are verificări înainte de integrare. Fork Radar produce evaluări și rapoarte,
iar importul unui SHA rămâne o acțiune separată, autorizată explicit.
Reluarea unui lot, integrarea pe `main` și publicarea au dovezi și mandate
distincte. În etapa curentă este aprobată redactarea planului.

## Obiective și starea verificată

Bază de analiză `77a7e5f26`. Constatările de mai jos provin din citirea codului.
Tabelul documentează baza istorică. La implementare, baza a fost `304cf8622`,
iar starea curentă este consemnată în checkpointurile de mai jos.

| Cod | Obiectiv | Stare și dovadă |
| --- | --- | --- |
| MRNGRUN-O1 | Inspecție fără efecte și simulare fidelă | În așteptare. În IIS, `main` rulează test hygiene înainte și după lotul de issue-uri chiar și cu `--dry-run`. În `flux_issues`, reluarea unui item poate apela implementarea înainte de controlul `dry_run`. Simularea scrie și stare, iar unele rute pornesc build sau sincronizare. În supervisor, `--check` poate apela recuperarea. [IIS](../.project-roadmap/scripts/iis_orchestrator.py) · [Supervisor](../.project-roadmap/scripts/orchestrator_supervisor.py) |
| MRNGRUN-O2 | Izolare și revenire fără atingerea lucrului local | În așteptare. IIS folosește o cale fixă, pregătește commituri pe directoare întregi, poate curăța sursele și poate rescrie commituri prin squash. Recuperarea supervisorului poate face stash, restore, clean și opriri după numele executabilului, inclusiv după un stash nereușit. [IIS](../.project-roadmap/scripts/iis_orchestrator.py) · [Supervisor](../.project-roadmap/scripts/orchestrator_supervisor.py) |
| MRNGRUN-O3 | Agenți și monitorizare conform D12–D15 | În așteptare. IIS mai expune Gemini și modele implicite în rute de execuție. IIS și Radar folosesc căi cu timeout fix pe subprocess. Supervisorul poate decide blocaj din vechimea sarcinii sau din lipsa unui commit. Aceste semnale nu dovedesc că transcriptul s-a oprit. [IIS](../.project-roadmap/scripts/iis_orchestrator.py) · [Supervisor](../.project-roadmap/scripts/orchestrator_supervisor.py) · [Fork Radar](../.project-roadmap/fork-intel/fork_intel.py) |
| MRNGRUN-O4 | Verificare și integrare IIS legate de candidatul exact | În așteptare. `run_tests` poate accepta sub 100% teste trecute, exit 96 cu grupuri lipsă sau exit 0 fără rezultate interpretabile. Rutele de succes pot comite și împinge imediat și apoi marca itemul `testing`. Funcția numită `post_github_comment` generează deja un draft local. [IIS](../.project-roadmap/scripts/iis_orchestrator.py) · [Fluxul de răspuns](ISSUE-RESPONSE-WORKFLOW.md) |
| MRNGRUN-O5 | Triage, preaprobare și coadă Radar conforme D3/D6 | În așteptare. `cmd_preapprove` poate reveni la panelul complet dacă rotația lasă mai puțin de două familii. `consensus_decision` respinge explicit `False`, dar poate accepta o aliniere absentă. Auditul selectat verifică votul, iar raportul poate reutiliza verdictul vechi și afișa comenzi care ar face cherry-pick cu commit imediat. [Fork Radar](../.project-roadmap/fork-intel/fork_intel.py) · [Teste Radar](../.project-roadmap/fork-intel/test_fork_intel.py) |
| MRNGRUN-O6 | Reluare graduală și trasabilă | În așteptare. Skill-urile sunt actualizate. Reluarea depinde de corecțiile runtime, de verificarea live a modelelor și drepturilor și de probele limitate. [IIS](../.agents/skills/mremoteng-pipeline/SKILL.md) · [Fork Radar](../.agents/skills/mremoteng-fork-radar/SKILL.md) |

## În lucru

| Cod | Pas | Responsabil | Checkpoint 2026-11-19 18:56 EET |
| --- | --- | --- | --- |
| MRNGRUN-P04 | Izolarea IIS și a recuperării | Coordonator | 80%. Candidatul folosește worktree separat, claim pe bază și item și index temporar. Proba Git a păstrat indexul și a blocat o cale protejată. Pornirea și recuperarea automată ale supervisorului rămân dezactivate până la o probă operațională. |
| MRNGRUN-P05 | Agenți CLI și progresul transcriptului | Coordonator | 75%. Rută comună pentru CLI, selecție explicită, verificarea modelului și jurnalul unei lansări. IIS exclude Gemini. Proba cu proces simulat a depășit intervalul vechi fără întrerupere cât timp au venit evenimente. Atenționare deschisă pentru contractul, accesul, costul și outputul CLI native. |
| MRNGRUN-P06 | Verificarea candidatului IIS | Coordonator | 75%. Difful și buildul/suita sunt legate prin SHA-256. Cazurile exit 96, test eșuat, grup lipsă, crash, zero teste și diff schimbat sunt blocate. Validarea a folosit un runner simulat. Verificările de produs, laboratorul aplicabil și integrarea coordonată rămân criterii înainte de reluare. |

## De făcut

Estimare de execuție tehnică de aproximativ 1–2 zile de lucru, în funcție de
probele de laborator și disponibilitatea rutelor CLI. Intervalele sunt pentru
checkpointuri. Ele nu întrerup un agent al cărui transcript avansează.

| Cod | Pas | Responsabil | Time-box și rezultat de acceptat |
| --- | --- | --- | --- |
| MRNGRUN-P09 | Reia gradual loturile după aprobarea probei operaționale | Coordonator | Checkpoint după fiecare probă. Radar începe cu cel mult un candidat benign. IIS începe cu un singur item benign fără integrare sau publicare. Citește rezultatele și verifică starea locală, procesele și limitele de cost. Activează separat fiecare mod și, ulterior, coordonarea automată numai în mandatul verificat. |
| MRNGRUN-P10 | Încheie și curăță resursele proprii | Coordonator | 30 min. Închide procesele, worktree-urile și lockurile deținute de probe. Păstrează rapoartele, consemnează modurile efectiv activate și mută restanțele în registrul proiectului. Confirmă starea finală a checkoutului și a registrelor. |

### Matrice de acceptare

| Probă | Intrare | Rezultat necesar |
| --- | --- | --- |
| Simulare IIS | Modurile de preview, inclusiv issue cu retry, date lipsă și o stare veche de supervisor | Planul arată ce ar selecta și care date sunt insuficiente. Preflight și recovery au zero apeluri. HEAD, indexul, issue DB și cache-ul nu se modifică. |
| Izolare și reluare | WIP în surse și metadate, index pregătit, două procese și întrerupere în fiecare tranziție relevantă | Aceleași valori și hashuri ale fișierelor la final. Fără schimbări în procesele altui lot. Reluarea recunoaște identitatea și starea jobului și nu dă din nou efecte confirmate. |
| D14/D15 | Agent sănătos trece de vechiul timeout, agent fără progres, model diferit de cel cerut și exit 0 fără răspuns valid | Agentul sănătos termină. Celelalte situații ajung la stări explicite, cu dovadă. Nu se lansează duplicate și un rezultat incomplet nu este aprobare. |
| Verificare IIS | Difful se schimbă după test, rezultate cu un test eșuat, grup lipsă, zero teste, tripwire blocat și scenariu lab aplicabil lipsă | Fiecare caz oprește integrarea. Candidatul acceptat are build, suită canonică, security lens, tripwire și laborator aplicabil. Verificările suplimentare urmează [ghidul local](FREE-VERIFICATION.md), în funcție de produs și codul atins. |
| Panel Radar | Două familii eligibile, triere din mai multe familii, vot absent, aliniere absentă, divergență, audit absent sau negativ, diff trunchiat și flag de securitate | Numai consensul eligibil cu dovezi complete produce preaprobare. Cache-ul poate fi reluat fără să transforme o lipsă de dovadă în vot și fără dublarea apelurilor finalizate. Nu se efectuează import. |
| Livrare | Bază schimbată, commit străin în interval, retrimitere după răspuns incert, nightly lipsă sau alt SHA | Integrarea sau acțiunea externă afectată rămâne în așteptare până la reconciliere. Draftul și rezultatul verificării rămân înregistrate. Starea `testing` se folosește când există build verificat pentru retest. |

## Rezolvate

| Cod | Pas | Ora locală și rezultat |
| --- | --- | --- |
| MRNGRUN-P01 | Inventarul inițial și planul | 2026-11-19 18:22 EET. Punctele din IIS, supervisor și Radar au fost reconciliate pe baza codului. Defectele sunt constatate static. Probele runtime rămân în pașii următori. |
| MRNGRUN-P02 | Baza și probele de regresie | 2026-11-19 18:56 EET. Testele rulează într-o copie temporară, cu răspunsuri de agenți simulate. Un test al PID-ului viu pe Windows a confirmat că existența unui proces trebuie verificată printr-un handle Win32 de citire. |
| MRNGRUN-P03 | Status, health check și preview IIS | 2026-11-19 18:56 EET. Probele CLI pe cache au păstrat hashurile statusului, logurilor, lockurilor, cozii și indexului. `--check` a raportat lockul vechi și l-a păstrat. Testul cu item în retry confirmă absența apelurilor de execuție. |
| MRNGRUN-P07 | Eligibilitatea panelului și cache-ul Radar | 2026-11-19 18:56 EET. Un flux simulat a dovedit trierea, rotația, votul orb, auditul, reluarea din cache fără al doilea apel și trecerea verdictului în istoric după schimbarea patchului. Raportul indică SHA-ul și procedura de import. |
| MRNGRUN-P08 | Acceptare izolată | 2026-11-19 18:56 EET. Suita țintită a trecut cu 157 de teste și 6 subteste. Proba într-un worktree Git real a inclus fișiere tracked și untracked, tripwire și comparația indexului. Agenții nativi, buildul de produs și laboratorul nu au fost porniți. |

## Decizii și limite de operare

| Cod | Decizie | Dispoziție |
| --- | --- | --- |
| MRNGRUN-D1 | Ordinea reluării | Recomandat în ordinea inspecție și preview, candidatură izolată, apoi operațiuni autorizate separat. Blocajele IIS și Radar se eliberează independent când trec probele fiecăruia. |
| MRNGRUN-D2 | Rute CLI și cost | Aplică D6 și D12–D15. Înaintea lotului real, verifică termenii pentru utilizarea în lot a abonamentului, accesul și cota. Confirmă modelul efectiv înainte ca răspunsul să fie folosit. Folosește abonamentele aprobate în limitele verificate. Achizițiile și costurile facturate suplimentar rămân în afara planului. |
| MRNGRUN-D3 | Limita integrării și a publicării | În prima probă, doar candidat și dovezi locale. Pentru schimbări protejate, se aplică revizuirea umană cerută de Cartă. Integrarea, push-ul, nightly și răspunsurile urmează mandatul aplicabil și verificarea rezultatului. Pentru Radar, aprobare explicită pe SHA pentru orice import. |
| MRNGRUN-D4 | Oprirea lotului și revenirea | Oprește modul afectat la scriere în WIP străin, model neconfirmat, rezultate neinterpretabile, execuții duplicate, verificare obligatorie incompletă sau cost neclar. Păstrează probele, oprește numai resursele identificate ca proprii și revino la raportarea prin citire până la remediere. |

## #ponytail

Reutilizează codul existent pentru coadă, fixture-uri, apelurile Git și
verificare. Înainte de modificare identifică apelanții, datele vechi și starea
afectată. Corectează aceeași cauză o singură dată în traseul comun, cu cel mai mic
diff lizibil care păstrează comportamentul valid. Păstrează tripwire, validarea,
tratarea erorilor și limitele de acces. Pentru logică netrivială adaugă o probă
țintită folosind testele existente unde se potrivesc. Marchează o scurtătură
acceptată cu `shortcut: <limită>, <când se înlocuiește>` și consemnează orice
restanță. Briefurile de implementare și închiderea lucrului pe cod poartă
`#ponytail`.

## Curățenie și istoric

Coordonatorul inventariază la închidere fiecare proces, lock, worktree și fișier
temporar al probelor conform pasului MRNGRUN-P10. Dovezile cu date locale rămân
în afara repo-ului public.

- 2026-11-19 18:22 EET. Plan deschis după inventarul static. Ordinea propusă începe cu inspectarea fără efecte și izolarea resurselor.
- 2026-11-19 18:56 EET. Implementarea locală și probele izolate sunt înregistrate. Reluarea operațională rămâne la pasul MRNGRUN-P09.
- Copiile temporare ale probelor rămân în afara checkoutului. Curățarea lor recursivă a fost blocată de controlul de execuție.
