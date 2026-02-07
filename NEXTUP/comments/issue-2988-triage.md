Triage update from fork validation:

- Audit of C# runtime code paths (`*.cs`) found no active `BinaryFormatter` / `SoapFormatter` deserialization usage in executable logic.
- XML import/deserialization entry points are using secure XML loading (`SecureXmlHelper`) and importer guards for missing/invalid file input (`MRemoteNGXmlImporter`, `MRemoteNGCsvImporter`).
- Most scanner hits appear to come from `.resx` metadata headers (`BinaryFormatter`/`SoapFormatter` schema declarations), which are common resource format markers and not direct runtime object-deserialization sinks.

Please share a concrete PoC path on latest branch (input file + entry point + affected class) if an exploitable sink remains. Without a reproducible sink, this looks like a false-positive/obsolete AI finding and can be closed accordingly.
