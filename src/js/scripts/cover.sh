node --expose-gc ./node_modules/vitest/vitest.mjs run --silent --pool=vmThreads \
  --coverage.enabled --coverage.thresholds.100 --coverage.include=**/sideload/*.mjs \
  --coverage.exclude=**/dotnet.* --coverage.allowExternal
