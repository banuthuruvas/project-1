#!/usr/bin/env node
/**
 * OpenAPI TypeScript Client Generator
 *
 * Generates TypeScript types from the running API's Swagger/OpenAPI spec.
 * Usage: pnpm run generate:api
 *
 * Prerequisites: API must be running on localhost:5002
 */

import { execSync } from "node:child_process";
import { writeFileSync, mkdirSync, existsSync } from "node:fs";
import { resolve, dirname } from "node:path";
import { fileURLToPath } from "node:url";

const __dirname = dirname(fileURLToPath(import.meta.url));
const outputDir = resolve(__dirname, "packages/shared/src/api-types");

if (!existsSync(outputDir)) {
  mkdirSync(outputDir, { recursive: true });
}

const apiUrl = process.env.API_URL || "http://localhost:5002";

console.log(`Fetching OpenAPI spec from ${apiUrl}/swagger/v1/swagger.json ...`);

try {
  execSync(
    `npx openapi-typescript ${apiUrl}/swagger/v1/swagger.json -o ${resolve(outputDir, "api.d.ts")}`,
    { stdio: "inherit", cwd: resolve(__dirname) },
  );
  console.log(`\n✓ Types generated at packages/shared/src/api-types/api.d.ts`);
} catch (error) {
  console.error(
    "\n✗ Failed to generate types. Is the API running on",
    apiUrl,
    "?",
  );
  process.exit(1);
}
