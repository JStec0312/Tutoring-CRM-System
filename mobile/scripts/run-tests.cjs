const { spawnSync } = require('node:child_process');
const path = require('node:path');
const project = path.resolve(__dirname, '..');
for (const args of [
  [require.resolve('typescript/bin/tsc'), '-p', 'tsconfig.tests.json'],
  ['--test', 'tests/session.test.cjs'],
]) {
  const result = spawnSync(process.execPath, args, { cwd: project, stdio: 'inherit' });
  if (result.status !== 0) process.exit(result.status ?? 1);
}
