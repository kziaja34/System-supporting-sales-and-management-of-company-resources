import http from 'k6/http';
import { check, sleep } from 'k6';

// DOPASUJ bazowy URL
const BASE = 'http://localhost:5000';
const ENDPOINTS = [
  `${BASE}/api/reports/sales-by-branch`,
  `${BASE}/api/reports/sales-trend`,
];

const loginRes = http.post('http://localhost:5000/api/auth/login',
    JSON.stringify({ email: 'manager@example.com', password: 'hashed123' }),
    { headers: { 'Content-Type': 'application/json' } });

// Jeśli potrzebujesz tokena: wstaw go tutaj albo pobierz w setup()
const TOKEN = loginRes.json('jwt'); // 'eyJ...';  // albo zostaw null jeśli publiczne

export const options = { vus: 1, iterations: ENDPOINTS.length * 2 };

export default function () {
  const idx = __ITER; // 0..(2*len-1)
  const ep = ENDPOINTS[Math.floor(idx / 2)]; // co 2 iteracje zmieniamy endpoint

  const headers = TOKEN ? { Authorization: `Bearer ${TOKEN}` } : {};
  const t0 = Date.now();
  const res = http.get(ep, { headers });
  const elapsed = Date.now() - t0;

  check(res, { 'HTTP 200': r => r.status === 200 });

  const tag = (idx % 2 === 0) ? 'COLD' : 'WARM';
  console.log(`[${tag}] ${ep} -> ${elapsed} ms`);

  sleep(2); // przerwa, by "warm" trafił w cache
}

export function handleSummary(s) {
  return { 'k6-kpi-reports-summary.json': JSON.stringify(s, null, 2) };
}
