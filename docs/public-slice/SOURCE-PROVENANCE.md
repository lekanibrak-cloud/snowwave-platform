# Source provenance for the public slice

This slice is not greenfield portfolio code. It was extracted from the private Snowwave implementation and then reduced so that one ingestion path can be inspected without publishing the full application.

The table records the private-source blob SHA reviewed during extraction. Public files are allowed to differ: namespaces were isolated, unrelated contract fields/dependencies were removed, comments were tightened, and tests were reduced to the behaviors this slice is intended to prove.

| Public file | Private source | Reviewed private blob SHA | Extraction note |
|---|---|---|---|
| `ManifestCsvParser.cs` | `src/snowwave-ops-func/Ops/ManifestCsvParser.cs` | `0cc1bdef30eb35f001b63d3ed64d5f30792d6506` | Logic retained; namespace/comments reduced. |
| `ManifestJob.cs` | `src/snowwave-ops-func/Ops/ManifestJob.cs` | `2ad4aac45d8f569ba963f85275d614c9006f9e0e` | Ledger/checkpoint/ownership shape retained. |
| `ManifestJobStore.cs` | `src/snowwave-ops-func/Ops/ManifestJobStore.cs` | `8c637fdefd9d2cac9691b47d82edf47aa0101c72` | Point-read, `IfMatchEtag`, 412 handling, and due-work query retained; unrelated list API removed. |
| `ManifestRowError.cs` | `src/snowwave-ops-func/Ops/ManifestRowError.cs` | `f65ab57867f7a3313f9ee1521e3914a6c1a824ed` | Shape retained. |
| `ManifestRowErrorStore.cs` | `src/snowwave-ops-func/Ops/ManifestRowErrorStore.cs` | `017f367f9724d6beed99f3bc0633731f43e3154b` | Write path retained; unrelated read endpoint support removed. |
| `ParcelIngestQueueService.cs` | `src/snowwave-ops-func/ParcelIngestQueueService.cs` | `71c8799ccde8ffb84b6936b854e7ae25691b24ea` | Batch publish and stable MessageId behavior retained; single-message method removed. |
| `ManifestParseService.cs` | `src/snowwave-ops-func/Ops/ManifestParseService.cs` | `26932a90ddc36f049e877bb1c9ad68f3d5e0e4e2` | State machine, CAS ownership, retry/failure behavior and publish path retained; comments reduced. |
| `ManifestParseWorker.cs` | `src/snowwave-ops-func/Ops/ManifestParseWorker.cs` | `76ae02a7a3ab9508cdf5fdf3c2edbffd49071c60` | Change Feed trigger retained. |
| `ManifestRetrySweeper.cs` | `src/snowwave-ops-func/Ops/ManifestRetrySweeper.cs` | `8eb31c2e8c6df703237e411112ca34432a9d917f` | Timer safety-net behavior retained. |
| `Contracts.cs` | `src/snowwave-ops-func/Contracts.cs` | `300b5eed56fb1b48c9723173d1de49a188f3a0a9` | Only event fields required by this slice retained from the much larger private contract surface. |
| public tests | `src/snowwave-ops-func.Tests/ManifestParseTests.cs` | `d0e495d6bbf0efd922a3c7a2a72c9fd4d0e012df` | Selected behaviors re-expressed as a compact public suite. |
| test fakes | `src/snowwave-ops-func.Tests/ManifestParseTestFakes.cs` | `061521eff03c4557cbf115212c15189361a6c9f4` | CAS, Blob and Service Bus fakes reduced to the public tests. |
| `infra/manifest-ingestion.bicep` | `infra/bicep/ops-nonprod/main.bicep` | `ac390bf1c290b6299ab72eff586ad80df632c830` | New scoped public module derived from the private resource model; private names and unrelated infrastructure omitted. |

## What provenance does not prove

A matching private-source SHA proves which implementation was reviewed during extraction. It does not by itself prove the public slice compiles or its tests pass. Build/test evidence must come from the public CI workflow after the candidate is validated and published.
