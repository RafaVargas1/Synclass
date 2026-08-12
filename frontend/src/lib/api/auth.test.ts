import { confirmarCodigo, solicitarCodigo } from '@/lib/api/auth';

function mockFetchOnce(status: number, body: unknown) {
  globalThis.fetch = jest.fn().mockResolvedValue({
    ok: status >= 200 && status < 300,
    status,
    json: () => Promise.resolve(body),
  }) as jest.Mock;
}

describe('solicitarCodigo', () => {
  it('returns sucesso when the Api responds with 200', async () => {
    mockFetchOnce(200, { enviado: true });

    const resultado = await solicitarCodigo({ contato: 'maria@exemplo.com' });

    expect(resultado).toEqual({ sucesso: true });
  });

  it('returns the Api error message when the contato has no full identity', async () => {
    mockFetchOnce(400, { mensagem: 'Nenhuma conta encontrada para esse contato.' });

    const resultado = await solicitarCodigo({ contato: 'naoexiste@exemplo.com' });

    expect(resultado).toEqual({
      sucesso: false,
      mensagem: 'Nenhuma conta encontrada para esse contato.',
    });
  });

  it('returns a connection error message when fetch throws', async () => {
    globalThis.fetch = jest.fn().mockRejectedValue(new Error('network error')) as jest.Mock;

    const resultado = await solicitarCodigo({ contato: 'maria@exemplo.com' });

    expect(resultado.sucesso).toBe(false);
  });
});

describe('confirmarCodigo', () => {
  it('returns the token and papeis when the Api responds with 200', async () => {
    mockFetchOnce(200, {
      token: 'token-jwt',
      usuarioId: 'id-1',
      nome: 'Maria Silva',
      papeis: ['Professor'],
    });

    const resultado = await confirmarCodigo({ contato: 'maria@exemplo.com', codigo: '123456' });

    expect(resultado).toEqual({
      sucesso: true,
      token: 'token-jwt',
      nome: 'Maria Silva',
      papeis: ['Professor'],
    });
  });

  it('returns the Api error message when the code is incorrect', async () => {
    mockFetchOnce(400, { mensagem: 'Código inválido ou expirado. Solicite um novo código.' });

    const resultado = await confirmarCodigo({ contato: 'maria@exemplo.com', codigo: '000000' });

    expect(resultado).toEqual({
      sucesso: false,
      mensagem: 'Código inválido ou expirado. Solicite um novo código.',
    });
  });

  it('returns a connection error message when fetch throws', async () => {
    globalThis.fetch = jest.fn().mockRejectedValue(new Error('network error')) as jest.Mock;

    const resultado = await confirmarCodigo({ contato: 'maria@exemplo.com', codigo: '123456' });

    expect(resultado.sucesso).toBe(false);
  });

  it('returns sucesso: false when the Api responds 200 with an unparseable body', async () => {
    globalThis.fetch = jest.fn().mockResolvedValue({
      ok: true,
      status: 200,
      json: () => Promise.reject(new Error('invalid json')),
    }) as jest.Mock;

    const resultado = await confirmarCodigo({ contato: 'maria@exemplo.com', codigo: '123456' });

    expect(resultado.sucesso).toBe(false);
  });
});
