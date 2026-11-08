const { defineConfig } = require('eslint/config');
const expoConfig = require('eslint-config-expo/flat');
module.exports = defineConfig([
  expoConfig,
  { ignores: ['dist/**', '.test-build/**'] },
  {
    files: ['src/shared/**/*.{ts,tsx}'],
    rules: { 'no-restricted-imports': ['error', { patterns: ['@/features/*', '@/bootstrap/*', '**/features/**', '**/bootstrap/**'] }] },
  },
  {
    files: ['src/features/**/*.{ts,tsx}'],
    rules: { 'no-restricted-imports': ['error', { patterns: ['@/bootstrap/*', '**/bootstrap/**'] }] },
  },
]);
