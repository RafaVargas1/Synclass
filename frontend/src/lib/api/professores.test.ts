import { cadastrarProfessor, verificarContatoProfessor } from '@/lib/api/professores';

function mockFetchOnce(status: number, body: unknown) {
  globalThis.fetch = jest.fn().mockResolvedValue({
    ok: status >= 200 && status < 300,
    status,
    json: () => Promise.resolve(body),
  }) as jest.Mock;
}

describe('cadastrarProfessor', () => {
  it('returns sucesso when the Api responds with 200', async () => {
    mockFetchOnce(200, { usuarioId: 'id-1', nome: 'Maria Silva' });

    const resultado = await cadastrarProfessor({
      nome: 'Maria Silva',
      contato: 'maria@exemplo.com',
    });

    expect(resultado).toEqual({ sucesso: true, nome: 'Maria Silva' });
  });

  it('returns the Api error message when the Api responds with 400', async () => {
    mockFetchOnce(400, { mensagem: 'O contato já está cadastrado como Professor.' });

    const resultado = await cadastrarProfessor({
      nome: 'Maria Silva',
      contato: 'maria@exemplo.com',
    });

    expect(resultado).toEqual({
      sucesso: false,
      mensagem: 'O contato já está cadastrado como Professor.',
    });
  });

  it('returns a connection error message when fetch throws', async () => {
    globalThis.fetch = jest.fn().mockRejectedValue(new Error('network error')) as jest.Mock;

    const resultado = await cadastrarProfessor({
      nome: 'Maria Silva',
      contato: 'maria@exemplo.com',
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

    const resultadoPromise = cadastrarProfessor({
      nome: 'Maria Silva',
      contato: 'maria@exemplo.com',
    });
    await jest.runAllTimersAsync();
    const resultado = await resultadoPromise;

    expect(resultado.sucesso).toBe(false);
    jest.useRealTimers();
  });
});

describe('verificarContatoProfessor', () => {
  it('returns identidadeExistente com o nome quando o contato já está cadastrado', async () => {
    mockFetchOnce(200, { identidadeExistente: true, nome: 'Maria Silva' });

    const resultado = await verificarContatoProfessor('maria@exemplo.com');

    expect(resultado).toEqual({ identidadeExistente: true, nome: 'Maria Silva' });
  });

  it('returns identidadeExistente false quando o contato é novo', async () => {
    mockFetchOnce(200, { identidadeExistente: false, nome: null });

    const resultado = await verificarContatoProfessor('novo@exemplo.com');

    expect(resultado).toEqual({ identidadeExistente: false, nome: null });
  });

  it('returns identidadeExistente false quando a requisição falha (fail-open)', async () => {
    globalThis.fetch = jest.fn().mockRejectedValue(new Error('network error')) as jest.Mock;

    const resultado = await verificarContatoProfessor('maria@exemplo.com');

    expect(resultado).toEqual({ identidadeExistente: false, nome: null });
  });
});
