import { alocarAluno, desalocarAluno, listarAlocacoes } from '@/lib/api/alocacoes';

function mockFetchOnce(status: number, body: unknown) {
  globalThis.fetch = jest.fn().mockResolvedValue({
    ok: status >= 200 && status < 300,
    status,
    json: () => Promise.resolve(body),
  }) as jest.Mock;
}

const alocacaoExistente = {
  id: 'a1',
  horarioId: 'h1',
  matriculaId: 'm1',
  createdAt: '2026-08-17T10:00:00Z',
};

describe('alocarAluno', () => {
  it('returns sucesso with the created alocacao when the Api responds with 200', async () => {
    mockFetchOnce(200, alocacaoExistente);

    const resultado = await alocarAluno('h1', 'm1');

    expect(resultado).toEqual({ sucesso: true, alocacao: alocacaoExistente });
  });

  it('posts matriculaId to the horario-scoped route', async () => {
    mockFetchOnce(200, alocacaoExistente);

    await alocarAluno('h1', 'm1');

    expect(globalThis.fetch).toHaveBeenCalledWith(
      expect.stringContaining('/professores/horarios/h1/alocacoes'),
      expect.objectContaining({ method: 'POST', body: JSON.stringify({ matriculaId: 'm1' }) }),
    );
  });

  it('returns the Api error message when the Api rejects with 400 (horário lotado)', async () => {
    mockFetchOnce(400, { mensagem: 'Este horário já atingiu o limite de Alunos.' });

    const resultado = await alocarAluno('h1', 'm1');

    expect(resultado).toEqual({
      sucesso: false,
      mensagem: 'Este horário já atingiu o limite de Alunos.',
    });
  });

  it('returns a connection error message when fetch throws', async () => {
    globalThis.fetch = jest.fn().mockRejectedValue(new Error('network error')) as jest.Mock;

    const resultado = await alocarAluno('h1', 'm1');

    expect(resultado.sucesso).toBe(false);
  });
});

describe('listarAlocacoes', () => {
  it('returns the list of alocacoes when the Api responds with 200', async () => {
    mockFetchOnce(200, [alocacaoExistente]);

    const resultado = await listarAlocacoes('h1');

    expect(resultado).toEqual({ sucesso: true, alocacoes: [alocacaoExistente] });
  });

  it('returns sucesso false when fetch throws', async () => {
    globalThis.fetch = jest.fn().mockRejectedValue(new Error('network error')) as jest.Mock;

    const resultado = await listarAlocacoes('h1');

    expect(resultado.sucesso).toBe(false);
  });
});

describe('desalocarAluno', () => {
  it('returns sucesso true when the Api responds with 204', async () => {
    mockFetchOnce(204, null);

    const resultado = await desalocarAluno('h1', 'm1');

    expect(resultado).toEqual({ sucesso: true });
  });

  it('deletes on the matricula-scoped route', async () => {
    mockFetchOnce(204, null);

    await desalocarAluno('h1', 'm1');

    expect(globalThis.fetch).toHaveBeenCalledWith(
      expect.stringContaining('/professores/horarios/h1/alocacoes/m1'),
      expect.objectContaining({ method: 'DELETE' }),
    );
  });

  it('returns sucesso false when the Api responds with 404', async () => {
    mockFetchOnce(404, null);

    const resultado = await desalocarAluno('h1', 'm1');

    expect(resultado.sucesso).toBe(false);
  });
});
