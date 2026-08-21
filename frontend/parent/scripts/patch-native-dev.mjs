#!/usr/bin/env node
/**
 * Patches the generated native projects so HTTP API calls work during local development.
 * Run automatically after `cap sync` via npm scripts.
 */
import { existsSync, readFileSync, writeFileSync, mkdirSync } from 'node:fs';
import { join, dirname } from 'node:path';
import { fileURLToPath } from 'node:url';

const root = join(dirname(fileURLToPath(import.meta.url)), '..');

function patchAndroid() {
  const androidDir = join(root, 'android', 'app', 'src', 'main');
  const manifestPath = join(androidDir, 'AndroidManifest.xml');
  if (!existsSync(manifestPath)) {
    return;
  }

  const xmlDir = join(androidDir, 'res', 'xml');
  mkdirSync(xmlDir, { recursive: true });

  const networkConfigPath = join(xmlDir, 'network_security_config.xml');
  writeFileSync(
    networkConfigPath,
    `<?xml version="1.0" encoding="utf-8"?>
<network-security-config>
  <!-- Development only: allow HTTP to local API hosts. Remove or restrict for store releases. -->
  <base-config cleartextTrafficPermitted="true">
    <trust-anchors>
      <certificates src="system" />
    </trust-anchors>
  </base-config>
</network-security-config>
`
  );

  let manifest = readFileSync(manifestPath, 'utf8');
  if (!manifest.includes('networkSecurityConfig')) {
    manifest = manifest.replace(
      '<application',
      '<application android:networkSecurityConfig="@xml/network_security_config"'
    );
    writeFileSync(manifestPath, manifest);
    console.log('[patch-native-dev] Android: enabled cleartext HTTP for development');
  }
}

function patchIos() {
  const plistPath = join(root, 'ios', 'App', 'App', 'Info.plist');
  if (!existsSync(plistPath)) {
    return;
  }

  let plist = readFileSync(plistPath, 'utf8');
  if (plist.includes('NSAppTransportSecurity')) {
    return;
  }

  const atsBlock = `
\t<key>NSAppTransportSecurity</key>
\t<dict>
\t\t<key>NSAllowsArbitraryLoads</key>
\t\t<true/>
\t</dict>`;

  plist = plist.replace('</dict>\n</plist>', `${atsBlock}\n</dict>\n</plist>`);
  writeFileSync(plistPath, plist);
  console.log('[patch-native-dev] iOS: enabled arbitrary HTTP loads for development');
}

patchAndroid();
patchIos();
