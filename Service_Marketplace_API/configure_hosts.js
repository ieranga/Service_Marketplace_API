const fs = require('fs');
const path = require('path');

const uiDir = 'F:/Program Files/My pro/project/Developments/Service Marketplace system/Service_Marketplace_UI/Service_Marketplace_UI';
const angularJsonPath = path.join(uiDir, 'angular.json');
const serverTsPath = path.join(uiDir, 'src/server.ts');

let raw = fs.readFileSync(angularJsonPath, 'utf8');
if (raw.charCodeAt(0) === 0xFEFF) {
  raw = raw.slice(1);
}
const angularJson = JSON.parse(raw);
const project = angularJson.projects.Service_Marketplace_UI;

if (!project.architect.serve.options) {
  project.architect.serve.options = {};
}
// Set allowedHosts as an array of allowed hosts for dev server
project.architect.serve.options.allowedHosts = [
  'localhost',
  '127.0.0.1',
  '192.168.8.140'
];

if (!project.architect.build.options.security) {
  project.architect.build.options.security = {};
}
project.architect.build.options.security.allowedHosts = [
  'localhost',
  '127.0.0.1',
  '192.168.8.140'
];

fs.writeFileSync(angularJsonPath, JSON.stringify(angularJson, null, 2), 'utf8');
console.log('Successfully updated angular.json');

// 2. Update src/server.ts
const serverTsContent = `import {
  AngularNodeAppEngine,
  createNodeRequestHandler,
  isMainModule,
  writeResponseToNodeResponse,
} from '@angular/ssr/node';
import { AngularAppEngine } from '@angular/ssr';
import express from 'express';
import { join } from 'node:path';

// Disable SSR host checks for local network development / mobile testing
(AngularAppEngine as any).ɵdisableAllowedHostsCheck = true;

const browserDistFolder = join(import.meta.dirname, '../browser');

const app = express();
const angularApp = new AngularNodeAppEngine({
  allowedHosts: ['localhost', '127.0.0.1', '192.168.8.140']
});

/**
 * Serve static files from /browser
 */
app.use(
  express.static(browserDistFolder, {
    maxAge: '1y',
    index: false,
    redirect: false,
  }),
);

/**
 * Handle all other requests by rendering the Angular application.
 */
app.use((req, res, next) => {
  angularApp
    .handle(req)
    .then((response) =>
      response ? writeResponseToNodeResponse(response, res) : next(),
    )
    .catch(next);
});

/**
 * Start the server if this module is the main entry point, or it is ran via PM2.
 * The server listens on the port defined by the \`PORT\` environment variable, or defaults to 4000.
 */
if (isMainModule(import.meta.url) || process.env['pm_id']) {
  const port = process.env['PORT'] || 4000;
  app.listen(port, (error) => {
    if (error) {
      throw error;
    }

    console.log(\`Node Express server listening on http://localhost:\${port}\`);
  });
}

/**
 * Request handler used by the Angular CLI (for dev-server and during build) or Firebase Cloud Functions.
 */
export const reqHandler = createNodeRequestHandler(app);
`;

fs.writeFileSync(serverTsPath, serverTsContent, 'utf8');
console.log('Successfully updated src/server.ts');
