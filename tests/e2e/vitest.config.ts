import { defineConfig } from 'vitest/config';

export default defineConfig({
    test: {
        reporters: ['verbose', 'html', 'json'],
        outputFile: {
            html: './reports/index.html',
            json: './reports/test-results.json'
        },
        coverage: {
            provider: 'v8',
            reporter: ['text', 'json', 'html'],
            reportsDirectory: './reports/coverage'
        }
    }
});
