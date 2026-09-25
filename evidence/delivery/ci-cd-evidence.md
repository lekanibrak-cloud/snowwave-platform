# Delivery evidence — CI/CD as an architecture control

Snowwave uses delivery automation as more than packaging.

The private repository documents:

- protected `main`;
- required pull-request checks;
- automatic non-production deployment on merge;
- GitHub Actions OIDC for Azure authentication;
- post-deployment validation;
- build identity checks;
- release-time catalogue seed/assert logic.

A useful example came from a false-green deployment. The response was not simply "rerun the pipeline." The architecture changed:

1. infrastructure and application artifact ownership were separated;
2. deployment concurrency remained as defense in depth;
3. loaded-binary identity was separated from configured intent;
4. tests were added so fresh configuration cannot impersonate a verified binary.

This is the portfolio's intended meaning of **evidence-driven engineering**: a failure becomes a durable mechanism rather than only a troubleshooting note.

**Current boundary:** individual release features are represented according to their verified state. In particular, the reviewed private README records the catalogue seed/assert release step as merged but not yet green at that snapshot.
