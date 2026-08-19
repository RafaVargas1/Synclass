import { atualizarNome, buscarPerfil } from '@/lib/api/usuarios';

function mockFetchOnce(status: number, body: unknown) {
  globalThis.fetch = jest.fn().mockResolvedValue({
    ok: status >= 200 && status < 300,
    status,
    json: () => Promise.resolve(body),
  }) as jest.Mock;
}

describe('buscarPerfil', () => {
  it('returns sucesso with the nome when the Api responds with 200', async () => {
    mockFetchOnce(200, { usuarioId: 'id-1', nome: 'Maria Silva' });

    const resultado = await buscarPerfil();

    expect(resultado).toEqual({ sucesso: true, usuarioId: 'id-1', nome: 'Maria Silva' });
  });

  it('returns a connection error message when fetch throws', async () => {
    globalThis.fetch = jest.fn().mockRejectedValue(new Error('network error')) as jest.Mock;

    const resultado = await buscarPerfil();

    expect(resultado.sucesso).toBe(false);
  });
});

describe('atualizarNome', () => {
  it('returns sucesso with the updated nome when the Api responds with 200', async () => {
    mockFetchOnce(200, { usuarioId: 'id-1', nome: 'Maria Souza' });

    const resultado = await atualizarNome('Maria Souza');

    expect(resultado).toEqual({ sucesso: true, nome: 'Maria Souza' });
  });

  it('returns the Api error message when the Api responds with 400', async () => {
    mockFetchOnce(400, { mensagem: 'Nome inválido: "". Esperado um nome não vazio.' });

    const resultado = await atualizarNome('');

    expect(resultado).toEqual({
      sucesso: false,
      mensagem: 'Nome inválido: "". Esperado um nome não vazio.',
    });
  });

  it('returns a connection error message when fetch throws', async () => {
    globalThis.fetch = jest.fn().mockRejectedValue(new Error('network error')) as jest.Mock;

    const resultado = await atualizarNome('Maria Souza');

    expect(resultado.sucesso).toBe(false);
  });
});
