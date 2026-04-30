const fs = require('fs');
const path = require('path');
const XLSX = require('xlsx');
const { Client } = require('pg');

const args = process.argv.slice(2);
let filePath, groupId, mode = 'dry-run';

for (let i = 0; i < args.length; i++) {
  if (args[i] === '--file' || args[i] === '-f') { filePath = args[i + 1]; i++; }
  else if (args[i] === '--group' || args[i] === '-g') { groupId = parseInt(args[i + 1]); i++; }
  else if (args[i] === '--apply') { mode = 'apply'; }
  else if (args[i] === '--dry-run') { mode = 'dry-run'; }
}

if (!filePath || !groupId) {
  console.error('Usage: node import.js --file <path> --group <id> [--dry-run|--apply]');
  process.exit(1);
}

const appsettingsPath = path.resolve(__dirname, '../../backend/appsettings.json');
const appsettings = JSON.parse(fs.readFileSync(appsettingsPath, 'utf8'));
const connString = appsettings.ConnectionStrings.DefaultConnection;
const connParts = {};
connString.split(';').forEach(part => {
  const [k, v] = part.split('=');
  if (k && v) connParts[k.toLowerCase()] = v;
});
const dbConfig = {
  host: connParts.host,
  port: parseInt(connParts.port) || 5432,
  database: connParts.database,
  user: connParts.username,
  password: connParts.password,
};

let students;
try {
  const wb = XLSX.readFile(filePath);
  const ws = wb.Sheets[wb.SheetNames[0]];
  const raw = XLSX.utils.sheet_to_json(ws);
  students = raw.map(row => {
    const r = {};
    Object.keys(row).forEach(k => {
      const lk = k.toLowerCase().replace(/\s/g, '');
      if (lk.includes('first') || lk.includes('имя')) r.firstName = String(row[k]).trim();
      else if (lk.includes('last') || lk.includes('фамилия')) r.lastName = String(row[k]).trim();
      else if (lk.includes('email') || lk.includes('почта')) r.email = row[k] ? String(row[k]).trim() : null;
    });
    return r;
  }).filter(s => s.firstName && s.lastName);
} catch (err) {
  console.error('Error reading xlsx:', err.message);
  process.exit(1);
}

const logLines = [];
function log(msg) {
  const line = `[${new Date().toISOString()}] ${msg}`;
  console.log(line);
  logLines.push(line);
}

async function main() {
  const isDryRun = mode === 'dry-run';
  log(`Mode: ${mode}`);
  log(`File: ${filePath}`);
  log(`Group ID: ${groupId}`);
  log(`Students in file: ${students.length}`);

  const client = new Client(dbConfig);
  await client.connect();
  log('DB connected');

  try {
    const grp = await client.query('SELECT id FROM groups WHERE id = $1', [groupId]);
    if (grp.rows.length === 0) { log(`ERROR: Group ${groupId} not found`); return; }

    let added = 0, updated = 0, skipped = 0;
    for (const s of students) {
      const existing = await client.query(
        'SELECT id, email FROM students WHERE LOWER(first_name) = LOWER($1) AND LOWER(last_name) = LOWER($2)',
        [s.firstName, s.lastName]
      );
      if (existing.rows.length > 0) {
        const cur = existing.rows[0];
        if (s.email && s.email !== cur.email) {
          if (!isDryRun) await client.query('UPDATE students SET email = $1, updated_at = NOW() WHERE id = $2', [s.email, cur.id]);
          log(`UPDATE: ${s.firstName} ${s.lastName} email ${cur.email || 'null'} -> ${s.email}${isDryRun ? ' (dry-run)' : ''}`);
          updated++;
        } else { log(`SKIP: ${s.firstName} ${s.lastName} (no change)`); skipped++; }
      } else {
        if (!isDryRun) await client.query(
          'INSERT INTO students (first_name, last_name, email, group_id, created_at, updated_at) VALUES ($1,$2,$3,$4,NOW(),NOW())',
          [s.firstName, s.lastName, s.email, groupId]
        );
        log(`ADD: ${s.firstName} ${s.lastName} (${s.email || 'no email'})${isDryRun ? ' (dry-run)' : ''}`);
        added++;
      }
    }
    log('--- Summary ---');
    log(`Read: ${students.length}, Added: ${added}, Updated: ${updated}, Skipped: ${skipped}`);
  } catch (err) {
    log(`ERROR: ${err.message}`);
  } finally {
    await client.end();
  }

  const logFile = path.join(__dirname, new Date().toISOString().replace(/[:.]/g, '-') + '.log');
  fs.writeFileSync(logFile, logLines.join('\n') + '\n');
  log(`Log: ${logFile}`);
}

main().catch(err => { console.error('Fatal:', err); process.exit(1); });
