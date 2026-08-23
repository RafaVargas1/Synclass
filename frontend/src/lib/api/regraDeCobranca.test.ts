import { definirRegraDeCobranca, obterRegraDeCobranca } from '@/lib/api/regraDeCobranca';

function mockFetchOnce(status: number, body: unknown) {
  globalThis.fetch = jest.fn().mockResolvedValue({
    ok: status >= 200 && status < 300,
    status,
    json: () => Promise.resolve(body),
  }) as jest.Mock;
}

describe('obterRegraDeCobranca', () => {
  it('returns the regra already defined when the Api responds with 200', async () => {
    const regra = { matriculaId: 'matricula-1', tipo: 'FixoMensal', valor: 300, frequenciaSemanalContratada: null };
    mockFetchOnce(200, regra);

    const resultado = await obterRegraDeCobranca('professor-1', 'matricula-1');

    expect(resultado).toEqual({ sucesso: true, definida: true, regra });
  });

  it('returns definida false when the Api responds with 404 (not yet defined)', async () => {
    mockFetchOnce(404, null);

    const resultado = await obterRegraDeCobranca('professor-1', 'matricula-1');

    expect(resultado).toEqual({ sucesso: true, definida: false });
  });

  it('returns a connection error message when fetch throws', async () => {
    globalThis.fetch = jest.fn().mockRejectedValue(new Error('network error')) as jest.Mock;

    const resultado = await obterRegraDeCobranca('professor-1', 'matricula-1');

    expect(resultado.sucesso).toBe(false);
  });
});

describe('definirRegraDeCobranca', () => {
  it('returns the created/altered regra when the Api responds with 200', async () => {
    const regra = { matriculaId: 'matricula-1', tipo: 'ValorPorAula', valor: 50, frequenciaSemanalContratada: 3 };
    mockFetchOnce(200, regra);

    const resultado = await definirRegraDeCobranca('professor-1', 'matricula-1', {
      tipo: 'ValorPorAula',
      valor: 50,
      frequenciaSemanalContratada: 3,
      baseDeContagemAula: 'Agendamento',
    });

    expect(resultado).toEqual({ sucesso: true, regra });
  });

  it('returns the Api error message when the Api rejects', async () => {
    mockFetchOnce(400, { mensagem: 'Frequência semanal contratada inválida: 9. Esperado um valor entre 1 e 7.' });

    const resultado = await definirRegraDeCobranca('professor-1', 'matricula-1', {
      tipo: 'ValorPorAula',
      valor: 50,
      frequenciaSemanalContratada: 9,
      baseDeContagemAula: 'Agendamento',
    });

    expect(resultado).toEqual({
      sucesso: false,
      mensagem: 'Frequência semanal contratada inválida: 9. Esperado um valor entre 1 e 7.',
    });
  });

  it('returns a connection error message when fetch throws', async () => {
    globalThis.fetch = jest.fn().mockRejectedValue(new Error('network error')) as jest.Mock;

    const resultado = await definirRegraDeCobranca('professor-1', 'matricula-1', {
      tipo: 'FixoMensal',
      valor: 300,
      frequenciaSemanalContratada: null,
      baseDeContagemAula: null,
    });

    expect(resultado.sucesso).toBe(false);
  });
});
