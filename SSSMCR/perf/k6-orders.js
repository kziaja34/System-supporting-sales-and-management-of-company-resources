import http from 'k6/http';
import { check, sleep, fail } from 'k6';

export const options = {
  vus: 15,
  duration: '5m',
  thresholds: {
    http_req_duration: ['p(95)<400'],
    http_req_failed:   ['rate<0.01'],
  },
  summaryTrendStats: ['avg','min','med','p(90)','p(95)','p(99)','max'],
};

export function setup() {
  const loginRes = http.post('http://localhost:5000/api/auth/login',
    JSON.stringify({ email: 'manager@example.com', password: 'hashed123' }),
    { headers: { 'Content-Type': 'application/json' } });

  if (loginRes.status !== 200) {
    console.error(`Login failed: ${loginRes.status}\n${loginRes.body}`);
    fail('Auth failed – fix login endpoint/body or credentials.');
  }

  // próbujemy najczęstszych nazw
  let token =
    loginRes.json('token') ||
    loginRes.json('accessToken') ||
    loginRes.json('jwt');

  // fallback: próbujemy wyłuskać "x.y.z" regexem z całego body
  if (!token) {
    const m = loginRes.body.match(/[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+\.[A-Za-z0-9-_]+/);
    token = m ? m[0] : null;
  }

  if (!token || token.split('.').length !== 3) {
    console.error(`Login response body:\n${loginRes.body}`);
    fail('No valid JWT found in login response (expected x.y.z).');
  }

  return { token };
}

export default function (data) {
  const headers = { Authorization: `Bearer ${data.token}` };

  const res = http.get('http://localhost:5000/api/orders?page=0&size=50', { headers });

  check(res, { '200 OK': r => r.status === 200 });
  sleep(1);
}

export function handleSummary(s) {
  return { 'k6-orders-summary.json': JSON.stringify(s, null, 2) };
}
