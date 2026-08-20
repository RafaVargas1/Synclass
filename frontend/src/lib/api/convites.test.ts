import { aceitarConvite, gerarConvite } from '@/lib/api/convites';

function mockFetchOnce(status: number, body: unknown) {
  globalThis.fetch = jest.fn().mockResolvedValue({
    ok: status >= 200 && status < 300,
    status,
    json: () => Promise.resolve(body),
  }) as jest.Mock;
}

describe('gerarConvite', () => {
  it('returns sucesso with token, codigo and expiraEm when the Api responds with 200', async () => {
    mockFetchOnce(200, {
      conviteId: 'convite-1',
      token: 'token-alta-entropia',
      codigo: '12345',
      expiraEm: '2026-08-20T00:00:00Z',
    });

    const resultado = await gerarConvite({ professorId: 'professor-1', contato: '11987654321' });

    expect(resultado).toEqual({
      sucesso: true,
      conviteId: 'convite-1',
      token: 'token-alta-entropia',
      codigo: '12345',
      expiraEm: '2026-08-20T00:00:00Z',
    });
  });

  it('posts to the professor-scoped route', async () => {
    mockFetchOnce(200, {
      conviteId: 'convite-1',
      token: 'token-1',
      expiraEm: '2026-08-20T00:00:00Z',
    });

    await gerarConvite({ professorId: 'professor-1', contato: '11987654321' });

    expect(globalThis.fetch).toHaveBeenCalledWith(
      expect.stringContaining('/professores/professor-1/convites'),
      expect.objectContaining({ method: 'POST' }),
    );
  });

  it('returns the Api error message when the Api responds with 400 (Aluno já vinculado)', async () => {
    mockFetchOnce(400, { mensagem: 'Este Aluno já está vinculado a este Professor.' });

    const resultado = await gerarConvite({ professorId: 'professor-1', contato: '11987654321' });

    expect(resultado).toEqual({
      sucesso: false,
      mensagem: 'Este Aluno já está vinculado a este Professor.',
    });
  });

  it('returns a connection error message when fetch throws', async () => {
    globalThis.fetch = jest.fn().mockRejectedValue(new Error('network error')) as jest.Mock;

    const resultado = await gerarConvite({ professorId: 'professor-1', contato: '11987654321' });

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

    const resultadoPromise = gerarConvite({ professorId: 'professor-1', contato: '11987654321' });
    await jest.runAllTimersAsync();
    const resultado = await resultadoPromise;

    expect(resultado.sucesso).toBe(false);
    jest.useRealTimers();
  });
});

describe('aceitarConvite', () => {
  it('returns sucesso with usuarioId, nome and papeis when the Api responds with 200', async () => {
    mockFetchOnce(200, { usuarioId: 'usuario-1', nome: 'João Pedro', papeis: ['Aluno'] });

    const resultado = await aceitarConvite({
      token: 'token-1',
      nome: 'João Pedro',
      contato: '11987654321',
    });

    expect(resultado).toEqual({
      sucesso: true,
      usuarioId: 'usuario-1',
      nome: 'João Pedro',
      papeis: ['Aluno'],
    });
  });

  it('posts to the token-scoped aceite route', async () => {
    mockFetchOnce(200, { usuarioId: 'usuario-1', nome: 'João Pedro', papeis: ['Aluno'] });

    await aceitarConvite({ token: 'token-1', nome: 'João Pedro', contato: '11987654321' });

    expect(globalThis.fetch).toHaveBeenCalledWith(
      expect.stringContaining('/convites/token-1/aceite'),
      expect.objectContaining({ method: 'POST' }),
    );
  });

  it('returns the Api error message when the Api responds with 400 (convite expirado)', async () => {
    mockFetchOnce(400, {
      mensagem: 'Este convite expirou. Peça ao Professor para gerar um novo link.',
    });

    const resultado = await aceitarConvite({
      token: 'token-1',
      nome: 'João Pedro',
      contato: '11987654321',
    });

    expect(resultado).toEqual({
      sucesso: false,
      mensagem: 'Este convite expirou. Peça ao Professor para gerar um novo link.',
    });
  });

  it('returns a connection error message when fetch throws', async () => {
    globalThis.fetch = jest.fn().mockRejectedValue(new Error('network error')) as jest.Mock;

    const resultado = await aceitarConvite({
      token: 'token-1',
      nome: 'João Pedro',
      contato: '11987654321',
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

    const resultadoPromise = aceitarConvite({
      token: 'token-1',
      nome: 'João Pedro',
      contato: '11987654321',
    });
    await jest.runAllTimersAsync();
    const resultado = await resultadoPromise;

    expect(resultado.sucesso).toBe(false);
    jest.useRealTimers();
  });
});
