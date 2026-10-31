// Run as root inside a disposable container, never on a real VPS.
import { mkdirSync, writeFileSync, copyFileSync, readFileSync, rmSync, readdirSync } from 'node:fs';
import { spawnSync } from 'node:child_process';
import assert from 'node:assert/strict';
import { createHash } from 'node:crypto';
mkdirSync('/tmp/bin', { recursive: true });
const stub = `#!/usr/local/bin/node
const fs=require('fs'); const a=process.argv.slice(2); fs.appendFileSync('/tmp/calls',a.join(' ')+'\\n');
if(a[0]==='inspect') { const name=a.at(-1),s=name.split('-').at(-2); if(a.includes('{{.Image}}')) console.log('sha256:old-'+s); else if(a[2].includes('project'))console.log('forfit-phase1');else console.log(s); }
else if(a[0]==='port')console.log('127.0.0.1:18081');
else if(a[0]==='info')console.log('/tmp');
else if(a[0]==='image')console.log('linux/amd64');
else if(a[0]==='compose') {const f=a[a.indexOf('-f')+1];if(a.includes('config')) {if(a.includes('--services')) console.log(fs.readFileSync('/tmp/old-services','utf8'));else if(!a.includes('--quiet'))console.log(fs.readFileSync(f,'utf8'));}
 if(a.includes('up') && a.at(-1)==='ssr' && fs.existsSync('/tmp/fail-once')){fs.unlinkSync('/tmp/fail-once');process.exit(1);}}
`;
writeFileSync('/tmp/bin/docker', stub, { mode: 0o755 });
writeFileSync('/tmp/bin/curl', '#!/bin/sh\ncase "$*" in *"%{http_code}"*) printf 200;; *sitemap*) printf "<urlset></urlset>";; *) printf \'<h1>fixture</h1><script id="public-data"></script>\';; esac\n', { mode: 0o755 });
mkdirSync('/opt/forfit/incoming', { recursive: true }); mkdirSync('/etc/forfit', { recursive: true }); mkdirSync('/run/lock', { recursive: true });
writeFileSync('/etc/forfit/appsettings.Production.json', '{}');
for (const [oldSSR, fail] of [[false, false], [false, true], [true, true]]) {
  rmSync('/opt/forfit/releases', { recursive: true, force: true });
  const folder = '/tmp/bundle'; mkdirSync(folder, { recursive: true });
  for (const [src, dst] of [['update-release.sh','update.sh'],['rollback-release.sh','rollback.sh'],['compose.full-v3.yaml','compose.yaml']])copyFileSync('/source/'+src,folder+'/'+dst);
  writeFileSync(folder+'/images.tar', 'fixture'); writeFileSync(folder+'/SHA256SUMS', createHash('sha256').update('fixture').digest('hex')+'  images.tar\n');
  writeFileSync('/opt/forfit/incoming/compose.images.yaml', 'services:\n  api:\n    image: old-api\n  web:\n    image: old-web\n'+(oldSSR?'  ssr:\n    image: old-ssr\n':''));
  writeFileSync('/tmp/old-services', oldSSR?'api\nssr\nweb':'api\nweb'); writeFileSync('/tmp/calls','');
  if(fail)writeFileSync('/tmp/fail-once','1');
  const run=spawnSync('bash',[folder+'/update.sh'],{env:{...process.env,PATH:'/tmp/bin:'+process.env.PATH},encoding:'utf8'});
  assert.equal(run.status,fail?1:0,run.stdout+run.stderr);
  assert.match(run.stdout,fail?/Previous ForFit restored/:/full-v3 updated/);
  const calls=readFileSync('/tmp/calls','utf8');
  assert.doesNotMatch(calls,/\b(prune|down|novastay-db|restart)\b/);
  if(fail)assert.match(calls,/rollback-images.yaml.*--force-recreate web/);
  const backup='/opt/forfit/releases/'+readdirSync('/opt/forfit/releases')[0];
  assert.match(readFileSync(backup+'/rollback-images.yaml','utf8'),/sha256:old-api/);
  if(!fail){const rollback=spawnSync('bash',[folder+'/rollback.sh',backup],{env:{...process.env,PATH:'/tmp/bin:'+process.env.PATH},encoding:'utf8'});assert.equal(rollback.status,0,rollback.stderr);}
  console.log('Updater mock: '+(oldSSR?'3→3':'2→3')+' '+(fail?'failure rollback':'success + manual rollback')+' OK');
}
