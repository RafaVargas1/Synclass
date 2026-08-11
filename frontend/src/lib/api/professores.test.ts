import { cadastrarProfessor } from '@/lib/api/professores';

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
});
