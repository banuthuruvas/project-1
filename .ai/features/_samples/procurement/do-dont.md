# Procurement — Do and Don't

> Procurement is a **reference sample**. The do/don't here is about how to use it as a learning surface, not about extending it.

## DO ✅

1. **Read** the procurement code to learn how the template wants new features built.
2. **Copy** patterns into your own entities — controller skeleton, service shape, Mapster mapping style, DTO flattening, sidebar nav wiring, FE service-then-page split.
3. **Compare** your status enum against `EPurchaseOrderStatus` — make sure you mirror BE/FE the same way.
4. **Run** task 0003 to remove procurement once your real entities exist. Don't leave stale code around.

## DON'T ❌

1. **Don't** extend procurement with project-specific rules. If your business needs purchase orders, copy procurement into your project's namespace and rename — then delete the original.
2. **Don't** treat procurement as production code. The seed data uses fake names ("Tech Solutions Pte Ltd", "Devi Anggraini") and the approval chain is hardcoded for demo purposes.
3. **Don't** delete only some procurement files — partial removal breaks the build. Run task 0003 atomically.
4. **Don't** modify procurement and ship the change downstream — improvements to the reference sample belong in the **template repo**, not in derived repos.
5. **Don't** add new access functions inside `Procurement*` namespace in your project. Use your own namespace (`Api.YourFeatureRead`).
