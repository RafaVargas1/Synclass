import { cadastrarAlunoProvisorio, listarAlunosProvisorios } from '@/lib/api/alunosProvisorios';

function mockFetchOnce(status: number, body: unknown) {
  globalThis.fetch = jest.fn().mockResolvedValue({
    ok: status >= 200 && status < 300,
    status,
    json: () => Promise.resolve(body),
  }) as jest.Mock;
}

describe('cadastrarAlunoProvisorio', () => {
  it('returns sucesso when the Api responds with 200', async () => {
    mockFetchOnce(200, { matriculaId: 'id-1', nome: 'João Pedro', identificador: '2024-013' });

    const resultado = await cadastrarAlunoProvisorio({
      nome: 'João Pedro',
      identificador: '2024-013',
    });

    expect(resultado).toEqual({ sucesso: true, nome: 'João Pedro', identificador: '2024-013' });
  });

  it('posts to the professor-scoped route', async () => {
    mockFetchOnce(200, { matriculaId: 'id-1', nome: 'João Pedro', identificador: '2024-013' });

    await cadastrarAlunoProvisorio({
      nome: 'João Pedro',
      identificador: '2024-013',
    });

    expect(globalThis.fetch).toHaveBeenCalledWith(
      expect.stringContaining('/professores/alunos-provisorios'),
      expect.objectContaining({ method: 'POST' }),
    );
  });

  it('returns the Api error message when the Api responds with 400', async () => {
    mockFetchOnce(400, {
      mensagem: 'Já existe um Aluno provisório com o identificador "2024-013" para este Professor.',
    });

    const resultado = await cadastrarAlunoProvisorio({
      nome: 'João Pedro',
      identificador: '2024-013',
    });

    expect(resultado).toEqual({
      sucesso: false,
      mensagem: 'Já existe um Aluno provisório com o identificador "2024-013" para este Professor.',
    });
  });

  it('returns a connection error message when fetch throws', async () => {
    globalThis.fetch = jest.fn().mockRejectedValue(new Error('network error')) as jest.Mock;

    const resultado = await cadastrarAlunoProvisorio({
      nome: 'João Pedro',
      identificador: '2024-013',
    });

    expect(resultado.sucesso).toBe(false);
  });

  it('returns a connection error message when the request hangs past the timeout', async () => {
    jest.useFakeTimers();
    globalThis.fetch = jest.fn(
      (_url: string, options?: RequestInit) =>
        new Promise((_resolve, reject) => {
          options?.signal?.addEventListener('abort', () => reject(new Error('aborted')));
        }),
    ) as jest.Mock;

    const resultadoPromise = cadastrarAlunoProvisorio({
      nome: 'João Pedro',
      identificador: '2024-013',
    });
    await jest.runAllTimersAsync();
    const resultado = await resultadoPromise;

    expect(resultado.sucesso).toBe(false);
    jest.useRealTimers();
  });
});

describe('listarAlunosProvisorios', () => {
  it('returns the list of alunos when the Api responds with 200', async () => {
    mockFetchOnce(200, [{ matriculaId: 'm1', nome: 'João Pedro', identificador: '2024-013' }]);

    const resultado = await listarAlunosProvisorios();

    expect(resultado).toEqual({
      sucesso: true,
      alunos: [{ matriculaId: 'm1', nome: 'João Pedro', identificador: '2024-013' }],
    });
  });

  it('gets the professor-scoped route', async () => {
    mockFetchOnce(200, []);

    await listarAlunosProvisorios();

    expect(globalThis.fetch).toHaveBeenCalledWith(
      expect.stringContaining('/professores/alunos-provisorios'),
      expect.anything(),
    );
  });

  it('returns sucesso false when fetch throws', async () => {
    globalThis.fetch = jest.fn().mockRejectedValue(new Error('network error')) as jest.Mock;

    const resultado = await listarAlunosProvisorios();

    expect(resultado.sucesso).toBe(false);
  });
});
