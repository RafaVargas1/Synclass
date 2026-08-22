import { chromium } from 'playwright';

const browser = await chromium.launch();
const page = await browser.newPage({ viewport: { width: 1440, height: 900 } });

await page.route('http://localhost:5005/**', async (route) => {
  const url = route.request().url();
  if (url.includes('/alunos/professores')) {
    return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify([{ professorId: 'p1', nome: 'Carlos Lima' }]) });
  }
  if (url.includes('proximas-aulas')) {
    return route.fulfill({ status: 200, contentType: 'application/json', body: JSON.stringify([{ horarioId: 'h1', data: '2026-08-25', diaSemana: 2, horaInicio: '14:00', duracaoMinutos: 50, podeCancelar: true, cancelavelAte: '2026-08-25T13:00:00Z', prazoCancelamentoMinutos: 60 }]) });
  }
  return route.fulfill({ status: 200, contentType: 'application/json', body: '{}' });
});

await page.goto('http://localhost:8112/painel', { waitUntil: 'domcontentloaded' });
await page.evaluate(() => {
  localStorage.setItem('synclass.sessao.token', 'fake-jwt-token');
  localStorage.setItem('synclass.sessao.papeis', JSON.stringify(['Aluno']));
});
await page.goto('http://localhost:8112/painel', { waitUntil: 'domcontentloaded' });
await page.waitForTimeout(1500);
await page.screenshot({ path: '/tmp/claude-1000/-home-rafael-Desktop-no-name/a25a2ed0-de0b-458f-8999-14a071640216/scratchpad/qa-165/painel-aluno.png' });

await browser.close();
