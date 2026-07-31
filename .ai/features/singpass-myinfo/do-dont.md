# Singpass MyInfo — Do and Don't

## DO ✅

1. **DO** check `MyInfoService.IsConfigured` BEFORE calling `CreateAuthorizationRequestAsync` or `GetPersonDataAsync`. The controller already does this (`MyInfoController.GetAuthorizeUrl` returns 400 when false). Replicate the pattern in any future endpoint that uses MyInfo.
2. **DO** treat `MyInfo:JwtClientAuthentication:PrivateJwksPath` as a secret. Mount the JWKS file at deploy time, NEVER commit a real production key. The shipped `private-jwks.json` is a placeholder for shape only.
3. **DO** set `MyInfo:JwtClientAuthentication:SigningKeyId` to the `kid` of the EC key whose `use = "sig"`, and `MyInfo:JwtClientAuthentication:EncryptionKeyId` to the `kid` of the EC key whose `use = "enc"`. The service throws if a key is found but its `use` does not match.
4. **DO** scope the `MyInfo:Scopes` (or `Attributes`) config to ONLY the attributes you actually need to display. The `DefaultScopeList` in the service is broad (`name sex race nationality dob birthcountry residentialstatus marital email mobileno regadd`) — narrow it for production to minimize PII surface.
5. **DO** rely on the discovery doc cache (1 hour) and issuer-signing-key cache (1 hour) — they are correct defaults for Singpass key rotation cadence. Do not lower these without coordinating with the IDP team.
6. **DO** keep state-record TTL at 10 minutes (`MyInfoStateLifetime` in `MyInfoController`). Singpass call timing leaves room; a longer window invites replay.
7. **DO** delete the cached state record on first use (`_distributedCache.RemoveAsync(cacheKey)` is already called inside `Callback` before token exchange). Single-use semantics prevent replay.
8. **DO** validate the `nonce` claim against the stored value (`MyInfoService.GetPersonDataAsync` does this on the ID token). Removing this check enables ID-token reuse.
9. **DO** use `DateTimeHelper.UtcOffsetNow` for `iat` / `exp` in the client assertion and DPoP proof. Singpass servers expect UTC unix-seconds; the helper handles the conversion.
10. **DO** log JWE decryption failures via `_logger.LogWarning(...)` and fall through to the manual ECDH-ES path (`ManualDecryptAndValidateAsync`). The library version of decryption is occasionally out of step with new Singpass enc algorithms; the manual fallback gives us coverage.

## DON'T ❌

1. **DON'T** push MyInfo state into the existing session DTO (`AuthSessionDto`). The flow is short-lived, single-use, and orthogonal to staff session — keeping it in `myinfo:state:` keeps cleanup simple.
2. **DON'T** persist `MyInfoPersonData` server-side without product approval. The spec returns it to the browser; whether you write it to your DB is a privacy/PII decision that requires DPA review.
3. **DON'T** loosen `ValidateLifetime = true` on the ID-token validation parameters. Singpass tokens are short-lived; accepting expired tokens defeats the freshness guarantee.
4. **DON'T** trust the FE to keep the state value secret — `state` is OAuth-grade opaque, but the controller still verifies that the cached state record matches the inbound state and that the nonce matches.
5. **DON'T** modify `RequiredOuterAlg` / `RequiredEnc` / `RequiredInnerAlg` to accept additional algorithms unless Singpass explicitly publishes new defaults. Hardcoded allowlists are the defense; widening them weakens it.
6. **DON'T** call MyInfo from a browser-side flow. The PKCE pair, DPoP private key, and client assertion all require server-side cryptography. The FE only redirects to Singpass and posts the auth code back.
7. **DON'T** confuse `MyInfo` with `Auth`. Singpass MyInfo returns **citizen attributes**, not a staff session. The user is still authenticated to the staff portal via the regular `authentication` flow; MyInfo is supplementary data.
8. **DON'T** log the inner JWS payload, the AccessToken, or the IdToken. The `_logger.LogInformation("MyInfo JWE header: alg={Alg}...")` line logs the header alg/enc/kid only — that is intentional and safe.
9. **DON'T** insert the user's MyInfo NRIC/FIN into `AuditLog.UserId` or `AuditLog.EntityId`. Use a hash or a generated correlation id if you must audit a MyInfo verification event.
10. **DON'T** keep MyInfo "enabled" in non-production environments by pointing at the production discovery URL. Use `BaseUrl = "https://stg-id.singpass.gov.sg"` (or `"https://test.api.myinfo.gov.sg"`) and a registered staging client id.
