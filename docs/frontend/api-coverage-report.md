# API coverage — source và client evidence

06/11/2026. [Inventory](api-inventory.json), [matrix](api-to-feature-map.md), [static function audit](api-consumer-audit.json).

| Measure | Kết quả |
| --- | --- |
| HTTP source census | 19 controllers / 122 actions + 1 health = 123 |
| Client functions + named consumers | 122 controller actions mapped; existing module/function audit PASS |
| Health diagnostic | Actual 200 + exact response PASS through proxy |
| Client regression | Full128 PASS + final focused2 PASS; HTTP simulations labelled |
| Real negative boundaries | 4 PASS; not business acceptance |
| Successful business endpoints | NEEDS_VERIFICATION; no per-endpoint runtime PASS asserted |
| Successful account journey | 1 SKIPPED by explicit user choice while credentials absent |
| SignalR | 18 events mapped; client/unit evidence; authenticated transport NEEDS_VERIFICATION |

Source coverage is not frontend completion or runtime API coverage. Dynamic Admin list adapter covers users/groups/reports/appeals/audit; consumer sections are validated. Appeal transport is isolated. File preview/download use authorized blobs. Backend source/contracts were not changed.
