import { confirmarPresenca, registrarFrequencia } from '@/lib/api/frequencias';

function mockFetchOnce(status: number, body: unknown) {
  globalThis.fetch = jest.fn().mockResolvedValue({
    ok: status >= 200 && status < 300,
    status,
    json: () => Promise.resolve(body),
  }) as jest.Mock;
}

const registrosRetornados = [
  { matriculaId: 'm1', presente: true, confirmadoPeloAluno: null },
  { matriculaId: 'm2', presente: false, confirmadoPeloAluno: true },
];

describe('registrarFrequencia', () => {
  it('returns sucesso with the registros when the Api responds with 200', async () => {
    mockFetchOnce(200, registrosRetornados);

    const resultado = await registrarFrequencia('h1', '2026-08-20', [
      { matriculaId: 'm1', presente: true },
      { matriculaId: 'm2', presente: false },
    ]);

    expect(resultado).toEqual({ sucesso: true, registros: registrosRetornados });
  });

  it('posts registros to the horario/data-scoped route', async () => {
    mockFetchOnce(200, registrosRetornados);

    await registrarFrequencia('h1', '2026-08-20', [{ matriculaId: 'm1', presente: true }]);

    expect(globalThis.fetch).toHaveBeenCalledWith(
      expect.stringContaining('/professores/horarios/h1/aulas/2026-08-20/frequencias'),
      expect.objectContaining({
        method: 'POST',
        body: JSON.stringify({ registros: [{ matriculaId: 'm1', presente: true }] }),
      }),
    );
  });

  it('returns the Api error message when the Api rejects with 400 (matricula não alocada)', async () => {
    mockFetchOnce(400, { mensagem: 'A matrícula m1 não está alocada no horário h1.' });

    const resultado = await registrarFrequencia('h1', '2026-08-20', [{ matriculaId: 'm1', presente: true }]);

    expect(resultado).toEqual({
      sucesso: false,
      mensagem: 'A matrícula m1 não está alocada no horário h1.',
    });
  });

  it('returns a connection error message when fetch throws', async () => {
    globalThis.fetch = jest.fn().mockRejectedValue(new Error('network error')) as jest.Mock;

    const resultado = await registrarFrequencia('h1', '2026-08-20', [{ matriculaId: 'm1', presente: true }]);

    expect(resultado.sucesso).toBe(false);
  });
});

const confirmacaoRetornada = { aulaId: 'aula-1', matriculaId: 'm1', confirmadoPeloAluno: true };

describe('confirmarPresenca', () => {
  it('returns sucesso with the confirmacao when the Api responds with 200', async () => {
    mockFetchOnce(200, confirmacaoRetornada);

    const resultado = await confirmarPresenca('professor-1', 'h1', '2026-08-20');

    expect(resultado).toEqual({ sucesso: true, confirmacao: confirmacaoRetornada });
  });

  it('posts sem body ao horario/data-scoped confirmacao-presenca route', async () => {
    mockFetchOnce(200, confirmacaoRetornada);

    await confirmarPresenca('professor-1', 'h1', '2026-08-20');

    expect(globalThis.fetch).toHaveBeenCalledWith(
      expect.stringContaining('/professores/professor-1/horarios/h1/aulas/2026-08-20/confirmacao-presenca'),
      expect.objectContaining({ method: 'POST' }),
    );
  });

  it('returns the Api error message when the Api rejects with 400 (cancelada pelo próprio Aluno)', async () => {
    mockFetchOnce(400, { mensagem: 'A matrícula m1 já cancelou a aula aula-1 e não pode confirmar presença nela.' });

    const resultado = await confirmarPresenca('professor-1', 'h1', '2026-08-20');

    expect(resultado).toEqual({
      sucesso: false,
      mensagem: 'A matrícula m1 já cancelou a aula aula-1 e não pode confirmar presença nela.',
    });
  });

  it('returns a connection error message when fetch throws', async () => {
    globalThis.fetch = jest.fn().mockRejectedValue(new Error('network error')) as jest.Mock;

    const resultado = await confirmarPresenca('professor-1', 'h1', '2026-08-20');

    expect(resultado.sucesso).toBe(false);
  });
});
