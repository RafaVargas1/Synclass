import { listarVinculosAluno } from './vinculosAluno';

function mockFetchOnce(status: number, body: unknown) {
  globalThis.fetch = jest.fn().mockResolvedValue({
    ok: status >= 200 && status < 300,
    status,
    json: () => Promise.resolve(body),
  }) as jest.Mock;
}

describe('listarVinculosAluno', () => {
  it('returns the vinculos when the Api responds with 200', async () => {
    mockFetchOnce(200, [
      { professorId: 'p1', nome: 'Professor A' },
      { professorId: 'p2', nome: 'Professor B' },
    ]);

    const resultado = await listarVinculosAluno();

    expect(resultado).toEqual({
      sucesso: true,
      vinculos: [
        { professorId: 'p1', nome: 'Professor A' },
        { professorId: 'p2', nome: 'Professor B' },
      ],
    });
  });

  it('gets the /alunos/professores route without an id parameter', async () => {
    mockFetchOnce(200, []);

    await listarVinculosAluno();

    expect(globalThis.fetch).toHaveBeenCalledWith(
      expect.stringContaining('/alunos/professores'),
      expect.anything(),
    );
  });

  it('returns an empty vinculos list when the Api responds 200 with null body', async () => {
    mockFetchOnce(200, null);

    const resultado = await listarVinculosAluno();

    expect(resultado).toEqual({ sucesso: true, vinculos: [] });
  });

  it('returns sucesso false when fetch throws (erro de rede)', async () => {
    globalThis.fetch = jest.fn().mockRejectedValue(new Error('network error')) as jest.Mock;

    const resultado = await listarVinculosAluno();

    expect(resultado.sucesso).toBe(false);
  });

  it('returns sucesso false when the Api responds with an error status', async () => {
    mockFetchOnce(500, null);

    const resultado = await listarVinculosAluno();

    expect(resultado.sucesso).toBe(false);
  });
});
