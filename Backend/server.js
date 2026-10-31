const express = require('express');
const os = require('os');

const app = express();
const PORT = process.env.PORT || 3000;
const HOST = process.env.HOST || '0.0.0.0';

/** @type {{ seed: number, x: number, y: number, z: number }[]} */
const falls = [];

/** Shared procedural seed for all devices on this backend instance. */
let sharedSeed = Number(process.env.LEVEL_SEED);
if (!Number.isFinite(sharedSeed) || sharedSeed === 0) {
  sharedSeed = Math.floor(Math.random() * 2147483646) + 1;
}

app.use(express.json());

app.use((req, res, next) => {
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Methods', 'GET,POST,OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type');
  if (req.method === 'OPTIONS') {
    res.sendStatus(204);
    return;
  }
  next();
});

app.get('/api/session', (_req, res) => {
  res.json({ seed: sharedSeed });
});

app.post('/api/fall', (req, res) => {
  const body = req.body || {};
  const seed = Number(body.seed);
  const x = Number(body.x);
  const y = Number(body.y);
  const z = Number(body.z);

  if (![seed, x, y, z].every(Number.isFinite)) {
    res.status(400).json({ error: 'Expected numeric seed, x, y, z' });
    return;
  }

  const echo = { seed, x, y, z };
  falls.push(echo);
  res.status(201).json({ ok: true, echo, count: falls.length });
});

app.get('/api/echoes', (req, res) => {
  const seedParam = req.query.seed;
  let echoes = falls;

  if (seedParam !== undefined && seedParam !== null && String(seedParam).length > 0) {
    const seed = Number(seedParam);
    if (!Number.isFinite(seed)) {
      res.status(400).json({ error: 'Expected numeric seed query' });
      return;
    }
    echoes = falls.filter((entry) => entry.seed === seed);
  }

  res.json({ echoes });
});

function listLanAddresses() {
  const nets = os.networkInterfaces();
  const addresses = [];
  for (const entries of Object.values(nets)) {
    if (!entries) continue;
    for (const entry of entries) {
      if (entry.family === 'IPv4' && !entry.internal) {
        addresses.push(entry.address);
      }
    }
  }
  return addresses;
}

app.listen(PORT, HOST, () => {
  console.log(`Echoes backend listening on http://${HOST}:${PORT}`);
  console.log(`Shared level seed: ${sharedSeed}`);
  for (const address of listLanAddresses()) {
    console.log(`  LAN: http://${address}:${PORT}`);
  }
});
