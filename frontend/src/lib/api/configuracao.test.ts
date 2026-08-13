import {
  definirModeloAgendamento,
  obterConfiguracao,
  ModeloAgendamento,
} from '@/lib/api/configuracao';

function mockFetchOnce(status: number, body: unknown) {
  globalThis.fetch = jest.fn().mockResolvedValue({
    ok: status >= 200 && status < 300,
    status,
    json: () => Promise.resolve(body),
  }) as jest.Mock;
}

describe('obterConfiguracao', () => {
  it('returns the modelo already defined when the Api responds with 200', async () => {
    mockFetchOnce(200, { modeloAgendamento: ModeloAgendamento.Fixo });

    const resultado = await obterConfiguracao('professor-1');

    expect(resultado).toEqual({
      sucesso: true,
      definida: true,
      modeloAgendamento: ModeloAgendamento.Fixo,
    });
  });

  it('returns definida false when the Api responds with 404 (not yet defined)', async () => {
    mockFetchOnce(404, null);

    const resultado = await obterConfiguracao('professor-1');

    expect(resultado).toEqual({ sucesso: true, definida: false });
  });

  it('returns a connection error message when fetch throws', async () => {
    globalThis.fetch = jest.fn().mockRejectedValue(new Error('network error')) as jest.Mock;

    const resultado = await obterConfiguracao('professor-1');

    expect(resultado.sucesso).toBe(false);
  });
});

describe('definirModeloAgendamento', () => {
  it('returns the created/altered modelo when the Api responds with 200', async () => {
    mockFetchOnce(200, { modeloAgendamento: ModeloAgendamento.Hibrido });

    const resultado = await definirModeloAgendamento('professor-1', ModeloAgendamento.Hibrido);

    expect(resultado).toEqual({ sucesso: true, modeloAgendamento: ModeloAgendamento.Hibrido });
  });

  it('returns the Api error message when the Api rejects', async () => {
    mockFetchOnce(400, { mensagem: 'Modelo de agendamento inválido: 9.' });

    const resultado = await definirModeloAgendamento('professor-1', ModeloAgendamento.Vago);

    expect(resultado).toEqual({ sucesso: false, mensagem: 'Modelo de agendamento inválido: 9.' });
  });

  it('returns a connection error message when fetch throws', async () => {
    globalThis.fetch = jest.fn().mockRejectedValue(new Error('network error')) as jest.Mock;

    const resultado = await definirModeloAgendamento('professor-1', ModeloAgendamento.Vago);

    expect(resultado.sucesso).toBe(false);
  });
});
