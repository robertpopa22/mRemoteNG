# Plan: identitate de versiune și actualizări FULL/MINI (portabil)

> Decizii maintainer, 2026-10-15: identitate de versiune clară dincolo de numărul de build; două canale,
> FULL rar și MINI per schimbare de cod; opțiune opt-in „instalează automat versiunile mini" (implicit oprită);
> aplicare atomică cu rollback, `Settings\` neatins; manifeste semnate. Context: trei deploy-uri prin
> WinRM/Tailscale eșuate pe 14.10, OneDrive cloud-only pe laptop, zip-ul self-contained de 99 MB livrat manual
> prin release-ul `nightly`.
>
> Design produs de un agent Opus pe 2026-10-15, read-only, cu fapte verificate în sursă. Status: propus,
> neimplementat. Pașii 1–3 din §H aduc valoare în zile; 4–7 trec întâi prin lab (D11).

## Ce există azi (verificat în sursă)
- `build.ps1` (l. 80-85) calculează `LocalBuildNumber = minute de la 2019-09-02 / 1000`. Valoarea crește o dată la circa 16,7 ore, deci se repetă pentru toate commiturile din aceeași zi. `mRemoteNG.csproj` (l. 720-727) setează `InformationalVersion = "$(LocalBuildVersion) (Local build N) $(Platform)"` și `IncludeSourceRevisionInInformationalVersion=false`.
- `GeneralAppInfo.GetApplicationVersion()` (l. 71-77) parsează `ProductVersion` după spații. Un `+sha` lipit de versiune strică parsarea, iar `AppUpdater.IsUpdateAvailable()` ar întoarce mereu `true`.
- Locurile care afișează versiunea: titlul ferestrei (`frmMain.cs` ~1927); `FrmAbout.cs:48`; `DebugReportBuilder.cs:85`; `FrmUnhandledException.cs:100,219`; `--version` în `ProgramRoot.cs:38`; `process_start` în `RuntimeDiagnostics.cs:81`, care scrie numai `AssemblyName.Version`, adică patru cifre, fără commit.
- Verificarea de update: `App/Checks/AppUpdater.cs` (namespace `mRemoteNG.App.Update`) citește `releases/latest` prin `UpdateReleaseInfo.cs`. Opțiunile sunt în `Properties.OptionsUpdatesPage` / `UpdatesPage.cs`. Rezultatul este numai o notificare.
- În portabil, `Settings\` stă **în folderul programului** (`SettingsFileInfo.PortableSettingsFolderName`). Un swap „rename de folder întreg program ↔ _staging" ar muta și Settings, deci nu se poate face așa. Mecanismul corect există deja în `scripts/Deploy-Portable.ps1` (l. 690-725): mută **fiecare element de nivel 1** prin rename în `_rollback\program-<stamp>`, sare peste `Settings`, `_rollback`, `_deploy-state`, scrie `_deploy-state\program-manifest.json` și verifică hash-urile profilului înainte și după.
- Copiile instalate prin MSI au markerul `mRemoteNG.installed` (`SettingsFileInfo.IsInstalledCopy`, #214).
- `BouncyCastle.Cryptography 2.7.0` este deja dependență, cu `Ed25519Signer`. Nu trebuie pachet nou.
- Tripwire-ul (`scripts/security-tripwire.sh`) acoperă căile `mRemoteNG/Security/`, `Crypto`, `Password`, `Credential` și `.github/workflows/`. **Nu** acoperă `App/Checks` sau update-ul. Trebuie adăugate explicit, iar orice editare a tripwire-ului cere el însuși review uman.
- Publish-ul self-contained are 350 de fișiere, 245 MB necomprimat: `mRemoteNG.dll` 2,8 MB; `ExternalConnectors.dll` 4,6 MB; 258 de satelite `*.resources.dll`.

## A. Identitate de versiune
- Țintă nouă `MrngBuildIdentity` în `mRemoteNG.csproj`, cu `BeforeTargets="GetAssemblyVersion;GenerateAssemblyInfo"` și condiția `'$(LocalBuildVersion)' != ''`:
  - `Exec git rev-parse --short=10 HEAD` cu `ConsoleToMSBuild` dă `MrngCommit`;
  - `git status --porcelain --untracked-files=no`; dacă iese ceva nevid, `MrngDirty=.dirty`;
  - fără git (de exemplu tarball), valoarea devine `nogit`. Build-ul nu pică.
- `InformationalVersion = 1.84.0+<sha>[.dirty] (Local build N) $(Platform)`. `FileVersion` și `AssemblyVersion` rămân `1.84.0.N`.
- `AssemblyMetadata` primește `CommitSha`, `BuildUtc` și `Dirty`, citite printr-o metodă nouă `GeneralAppInfo.BuildId`.
- `GetApplicationVersion()`: se taie la `+` înainte de parsare și se adaugă test unitar pe ambele formate.
- `build.ps1` nu se schimbă. Ținta citește singură git-ul.
- Afișare: titlul ferestrei `v1.84.0+a1b2c3d4e5` mereu, nu doar în DevMode; About; `--version`; raportul de crash și debug report; `process_start` primește câmpurile `build_id=<sha>[.dirty]` și `edition=portable|installed`.
- Toate liniile de log se leagă de build prin `process_start` din aceeași sesiune (`pid` și `session`). Nu se repetă sha-ul pe fiecare linie `rdp_shape`. `scripts/Analyze-RuntimeLog.ps1` grupează după `build_id`.
- **Risc de verificat la pasul 1:** dacă `InformationalVersion` se propagă și în cele 258 de satelite, fiecare commit schimbă toți sateliții și MINI crește. Același lucru face `FileVersion`-ul care se schimbă o dată la circa 16 ore. Remedierea: sateliții primesc `InformationalVersion`/`FileVersion` stabile (`1.84.0` / `1.84.0.0`), iar sha-ul rămâne doar în `mRemoteNG.dll`.

## B. Format pachete
- Asset-uri pe release: `mRemoteNG-full-<ver>-x64.zip`; `mRemoteNG-mini-<ver>-x64.zip`; `manifest-<ver>.json` + `manifest-<ver>.json.sig`; `<ver>` = `1.84.0+a1b2c3d4e5`, cu `+` înlocuit cu `-` în numele de fișier.
- Pe `nightly` se urcă și `manifest-latest.json` + `.sig`, care arată spre ultimul build. Clientul descarcă **numai** acest fișier mic, nu API-ul GitHub.
- Schema manifestului (JSON canonic, chei sortate, UTF-8 fără BOM, semnătura calculată peste octeții exacți): `schema:1`, `version`, `commit`, `channel: full|mini`, `rid: win-x64`, `buildUtc`; `baseFull: <ver>` (`null` la FULL); `minClientVersion`, `fullUrl`; `files: [{path, sha256, size}]` = lista **completă** a programului după aplicare; `delta: [{path, sha256, size}]` (doar la MINI) = fișierele din zip; `remove: [path]`. Căile sunt relative, cu `\`, fără `..`, rădăcină, `:` sau nume rezervate.
- Calculul MINI: diferența dintre hash-urile build-ului curent și `files` din manifestul FULL de bază, luat **de pe release**, nu de pe disc. Intră în `delta` căile noi sau cu hash diferit; în `remove` cele care lipsesc.
- Pachetul trece obligatoriu pe FULL (MINI refuzat de script) când se schimbă: runtime-ul .NET (`hostfxr.dll`, `coreclr.dll`, `System.Private.CoreLib.dll`, `*.runtimeconfig.json`, `*.deps.json`) sau versiunea SDK; `runtimes\**` (CEF, nativele); `PuTTYNG.exe`, `mstscax`, interop COM; versiunea `major.minor`. Pragul: `delta` peste 40% din FULL înseamnă FULL.
- Estimare pentru un fix tipic: `mRemoteNG.dll` (2,8 MB, circa 1,1 MB comprimat) plus eventual `mRemoteNG.exe` și `deps.json`: **1-2 MB zip**. Dacă sateliții se schimbă (riscul din A), circa 4-6 MB. Un FULL are circa 99 MB.

## C. Publicare locală: `scripts/Publish-Release.ps1`
- Parametri: `-Channel full|mini`, `-Tag nightly|vX.Y.Z`, `-BaseFull <ver>` (implicit: ultimul FULL de pe tag), `-DryRun`.
- Pași: (1) `git worktree add` curat la `HEAD`, în `%TEMP%`; (2) `build.ps1 -SelfContained`; (3) `scripts/verify-shipped-assemblies.ps1`; (4) testele (`run-tests.ps1 -Headless -NoBuild`); (5) hash-uri și manifest; (6) semnarea, cu cheia privată citită din Vault **în memorie**, niciodată pe disc; (7) auto-verificarea semnăturii cu cheia publică din cod; (8) zip; (9) `gh release upload <tag> ... --repo robertpopa22/mRemoteNG`, cu `--clobber` doar pentru `manifest-latest*`; (10) read-back: descarcă manifestul de pe release și verifică semnătura.
- Refuză: tree murdar sau `.dirty` în versiune; HEAD care nu e pe `origin/main` (sau nu e tagul `vX.Y.Z` pentru stabil); versiunea deja publicată; semnătura lipsă sau invalidă; MINI cu un fișier din lista „doar FULL"; MINI fără manifestul bazei pe release; tag `vX.Y.Z` care nu se potrivește cu `<Version>` din csproj.
- Nu scrie nimic în working tree-ul principal. Folosește `MREMOTENG_PORTABLE_CHAIN=1`, ca build-ul să nu facă deploy pe daily driver.

## D. Client
- Clase noi în `mRemoteNG/App/Update/`: `ReleaseManifest` + `ManifestVerifier` (parse, semnătură, validare căi); `UpdatePlanner` (decide `None`/`Mini`/`Full`); `UpdateDownloader`; `UpdateStager`.
- `AppUpdater` primește `GetManifestAsync()`. Vechiul `releases/latest` rămâne pentru changelog.
- Decizia `UpdatePlanner`: **None** când `manifest.commit` = commitul local sau versiunea e marcată „bad"; **Mini** când `baseFull` corespunde instalării locale (din `_deploy-state\program-manifest.json`, cu verificare la fața locului a hash-urilor fișierelor ce vor fi înlocuite) **și** copia e portabilă; **Full** în orice alt caz. FULL se afișează doar ca notificare.
- Downloader: `HttpClient`-ul existent, cu proxy din opțiuni; HTTPS obligatoriu; redirecturi doar `github.com` → `objects.githubusercontent.com` / `release-assets.githubusercontent.com`; `Range: bytes=n-` pe `.part`, cu `If-Range` pe ETag; retry cu backoff exponențial 30 s → 30 min, maximum 6 încercări per sesiune. În `PowerAwareness.LowPowerMode` descărcarea se oprește între chunk-uri și se reia la priză. Limita normală: 2 MB/s.
- Staging: `<exe>\_staging\<ver>\` cu `manifest.json` + `.sig` copiate exact, `payload\` dezarhivat cu validare de cale per intrare și hash per fișier, `ready` scris ultimul. Un hash greșit șterge staging-ul și marchează versiunea „bad" pentru 24 de ore.
- Bara de stare (`frmMain`, fără dialog): „Actualizare 1.84.0+abc descărcată, se aplică la închidere". Click deschide `UpdateWindow`.
- Opțiune nouă: `OptionsUpdatesPage.AutoInstallMiniUpdates` (bool, implicit `false`), în Options → Updates ca checkbox „Instalează automat versiunile mini". Dezactivată și explicată când copia e instalată prin MSI, când folderul nu permite scriere sau când `UpdatesForbidden()`. Oprită = exact calea de azi, doar notificare.

## E. Aplicare atomică
- Proiect nou `mRemoteNG.Update`: consolă NativeAOT, circa 3-5 MB, fără runtime partajat, copiat în folderul programului. La aplicare, aplicația îl copiază în `%TEMP%\mrng-upd-<guid>\` și îl lansează de acolo cu `--apply <dir> --pid <pid> --staging <ver>`, ca să nu stea în folderul pe care îl înlocuiește.
- Algoritmul este portarea în C# a `Deploy-Portable.ps1`: (1) așteaptă ieșirea PID-ului (60 s); (2) **re-verifică** semnătura manifestului din staging și toate hash-urile `payload`, cu cheia publică compilată în updater; (3) construiește arborele țintă complet în `_staging\<ver>\tree\`: hardlink-uri spre fișierele nemodificate, plus delta, minus `remove`; (4) verifică arborele contra `files`; (5) mută prin **rename** fiecare element de nivel 1 al programului în `_rollback\program-<stamp>`, fără `Settings`, `_rollback`, `_deploy-state`, `_staging`; (6) mută prin rename elementele din `tree` în folderul programului; (7) verifică hash-urile profilului `Settings` înainte și după, plus `confCons.xml`; (8) scrie `_deploy-state\update-probation.json`; (9) pornește `mRemoteNG.exe`.
- Probă de pornire: aplicația nouă scrie `probation-ok` după `process_start` și afișarea ferestrei principale. Fără `ok` în 90 s sau cu exit nenul, updater-ul face rollback (rename invers), scrie `_deploy-state\bad-updates.json` și repornește versiunea veche.
- Reziliență la crash: jurnal `_deploy-state\apply.json`, scris înaintea fiecărei faze. La următoarea pornire, dacă jurnalul e nefinalizat, se reia rollback-ul.
- Se refuză aplicarea pe volume fără hardlink sau rename atomic (FAT/exFAT, share UNC, placeholder OneDrive, reparse-check deja existent în `Deploy-Portable`); când există `mRemoteNG.installed`; când `!Runtime.IsPortableEdition`. Se păstrează ultimele 2 `_rollback`.

## F. Securitate
- Ed25519 prin BouncyCastle `Ed25519Signer` peste octeții manifestului. Cheia privată stă în Vault (item personal `mRemoteNG-Release-Ed25519`, placeholder până o completează Robert) și nu ajunge în repo sau pe disc. Cheia publică stă în `mRemoteNG/Security/UpdateSigningKey.cs` și în updater. Pentru rotație: `keyId` în `.sig`, plus o listă de chei acceptate.
- Reguli: URL-urile se construiesc din `version`, cu prefix fix `https://github.com/robertpopa22/mRemoteNG/releases/download/`; URL-urile din manifest se ignoră. Orice lipsă, semnătură greșită, `rid` greșit sau cale care evadează (normalizare `Path.GetFullPath` + `StartsWith(root + sep)`) duce la respingere. Rollback-ul de versiune e refuzat: `buildUtc` mai vechi decât cel local nu se aplică, decât dacă versiunea locală e marcată „bad". Nimic descărcat nu se execută înainte de verificarea în updater; updater-ul însuși este binarul deja instalat.
- Tripwire: codul nou intră sub `mRemoteNG/Security/` (manifest și verificare), iar `App/Update/`, `mRemoteNG.Update/` și `scripts/Publish-Release.ps1` se adaugă în `PATH_PATTERNS`. **Toate aceste commituri cer review uman cu `MRNG_SECURITY_REVIEWED=1`.** Corpul commitului spune ce proprietate a fost examinată.
- Amenințări: (1) cheie privată compromisă (rotație prin `keyId`, revocare în noul FULL); (2) MITM sau proxy corporate (HTTPS + semnătură); (3) downgrade/replay al unui manifest vechi semnat (`buildUtc` monoton); (4) path traversal sau zip-slip; (5) TOCTOU între descărcare și aplicare (re-verificare în updater); (6) DLL planting în `%TEMP%` (folder unic cu ACL implicit al userului; updater AOT nu încarcă DLL-uri din cwd); (7) arbore parțial după kill (jurnal + rollback); (8) Defender carantinează un fișier proaspăt, ca în #192 (hash-check după mutare, apoi rollback); (9) binare tot nesemnate Authenticode (`docs/CODE_SIGNING_POLICY.md`): semnătura Ed25519 dovedește proveniența față de updater, nu față de Windows; se spune public așa, fără a pretinde pachete „signed".

## G. Probă în lab (D11)
Script `lab-update.ps1`, după modelul `lab-msi.ps1`, pe guest-ul izolat. Serverul de release este simulat local pe guest (HTTPS cu certificat de test, override activ doar în build-ul de lab), cu o cheie de test. Scenarii:
1. Instalare FULL N și pornire; Settings și conexiunile seed prezente.
2. MINI N+1 peste N: aplicare la ieșire, `build_id` nou în titlu și în `process_start`, hash-uri Settings identice.
3. MINI care cere baza M ≠ N: planner-ul alege FULL, doar notificare, nimic aplicat.
4. Fișier corupt sau `.sig` alterat: respins, staging șters, versiunea locală intactă.
5. Kill al updater-ului la fiecare fază (după mutarea în `_rollback`, la jumătatea mutării): la repornire rollback, aplicația N pornește.
6. Aplicație nouă care cade la start (exe sabotat intenționat): rollback în 90 s, versiune marcată „bad", fără buclă.
7. Copie MSI (`mRemoteNG.installed`): opțiunea e dezactivată, nu se descarcă nimic.
8. Resume: rețeaua tăiată la 50%, reluare prin Range și hash corect.
9. Pe baterie (emulat): descărcarea se oprește.

## H. Plan de implementare
| # | Pas | Fișiere | Linii | Model |
|---|---|---|---|---|
| 1 | Identitate versiune + fix parser + `process_start build_id` + satelite stabile | `mRemoteNG.csproj`, `GeneralAppInfo.cs`, `frmMain.cs`, `FrmAbout.cs`, `RuntimeDiagnostics.cs`, test | ~80 | Sonnet (verificare satelite: Opus) |
| 2 | `Publish-Release.ps1` FULL + MINI nesemnat, manifest, zip, `-DryRun` | `scripts/` | ~300 | Sonnet |
| 3 | Aplicare manuală MINI prin `Deploy-Portable.ps1 -Mini <zip>`, care construiește arborele din baza locală și refolosește algoritmul dovedit | `scripts/Deploy-Portable.ps1` | ~120 | Opus |
| 4 | Semnare Ed25519 (script + `ManifestVerifier`) + tripwire paths, **review uman** | `Security/`, `security-tripwire.sh` | ~200 | Opus |
| 5 | Planner + downloader (Range, backoff, putere) + bara de stare + opțiunea | `App/Update/`, `UpdatesPage`, `Properties` | ~450 | Opus planner/downloader, Sonnet UI |
| 6 | Updater NativeAOT + jurnal + probă de pornire + rollback, **review uman** | `mRemoteNG.Update/` (proiect nou, `.sln` modificat de om) | ~500 | Opus |
| 7 | `lab-update.ps1` + scenariile G | root | ~300 | Sonnet scenarii, Opus judecata rezultatelor |

**Ce aduce valoare în câteva zile:** pașii 1-3. Fiecare log și crash spune exact commitul. MINI-ul de 1-2 MB se publică și se aplică manual prin script pe stațiile proprii, cu rollback-ul deja dovedit de `Deploy-Portable`. Pașii 4-7 vin după, fiecare trecut întâi prin lab (D11). Auto-instalarea rămâne oprită în produs până când scenariile G trec integral.
