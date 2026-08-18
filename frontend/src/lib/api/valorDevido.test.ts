import { listarValorDevido } from '@/lib/api/valorDevido';

function mockFetchOnce(status: number, body: unknown) {
  globalThis.fetch = jest.fn().mockResolvedValue({
    ok: status >= 200 && status < 300,
    status,
    json: () => Promise.resolve(body),
  }) as jest.Mock;
}

describe('listarValorDevido', () => {
  it('returns the list of valoresDevidos when the Api responds with 200', async () => {
    mockFetchOnce(200, [
      { matriculaId: 'm1', alunoUsuarioId: null, nome: 'Ana', valor: 300, semRegraDefinida: false },
    ]);

    const resultado = await listarValorDevido('professor-1');

    expect(resultado).toEqual({
      sucesso: true,
      valoresDevidos: [
        { matriculaId: 'm1', alunoUsuarioId: null, nome: 'Ana', valor: 300, semRegraDefinida: false },
      ],
    });
  });

  it('gets the professor-scoped route without query string when periodo is omitted', async () => {
    mockFetchOnce(200, []);

    await listarValorDevido('professor-1');

    expect(globalThis.fetch).toHaveBeenCalledWith(
      expect.stringContaining('/professores/professor-1/valor-devido'),
      expect.anything(),
    );
    expect(globalThis.fetch).not.toHaveBeenCalledWith(expect.stringContaining('?'), expect.anything());
  });

  it('appends inicio/fim to the query string when periodo is informed', async () => {
    mockFetchOnce(200, []);

    await listarValorDevido('professor-1', { inicio: '2026-08-01', fim: '2026-09-01' });

    expect(globalThis.fetch).toHaveBeenCalledWith(
      expect.stringContaining('/professores/professor-1/valor-devido?inicio=2026-08-01&fim=2026-09-01'),
      expect.anything(),
    );
  });

  it('returns sucesso false when fetch throws', async () => {
    globalThis.fetch = jest.fn().mockRejectedValue(new Error('network error')) as jest.Mock;

    const resultado = await listarValorDevido('professor-1');

    expect(resultado.sucesso).toBe(false);
  });

  it('returns sucesso false when the Api responds with an error status', async () => {
    mockFetchOnce(500, null);

    const resultado = await listarValorDevido('professor-1');

    expect(resultado.sucesso).toBe(false);
  });
});
