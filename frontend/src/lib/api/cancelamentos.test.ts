import { cancelarAula, listarProximasAulas } from '@/lib/api/cancelamentos';

function mockFetchOnce(status: number, body: unknown) {
  globalThis.fetch = jest.fn().mockResolvedValue({
    ok: status >= 200 && status < 300,
    status,
    json: () => Promise.resolve(body),
  }) as jest.Mock;
}

const cancelamentoExistente = {
  id: 'c1',
  aulaId: 'aula-1',
  matriculaId: 'm1',
  canceladoEm: '2026-08-18T12:00:00Z',
};

const aulaProximaExistente = {
  horarioId: 'h1',
  data: '2026-08-20',
  diaSemana: 4,
  horaInicio: '18:00:00',
  duracaoMinutos: 60,
  podeCancelar: true,
  cancelavelAte: '2026-08-19T18:00:00Z',
  prazoCancelamentoMinutos: 1440,
};

describe('listarProximasAulas', () => {
  it('returns the list of próximas aulas when the Api responds with 200', async () => {
    mockFetchOnce(200, [aulaProximaExistente]);

    const resultado = await listarProximasAulas('professor-1');

    expect(resultado).toEqual({ sucesso: true, aulas: [aulaProximaExistente] });
  });

  it('queries the professor-scoped route, sem matriculaId (resolvida pela Api via sessão)', async () => {
    mockFetchOnce(200, []);

    await listarProximasAulas('professor-1');

    expect(globalThis.fetch).toHaveBeenCalledWith(
      expect.stringContaining('/professores/professor-1/horarios/proximas-aulas'),
      expect.anything(),
    );
    const [urlChamada] = (globalThis.fetch as jest.Mock).mock.calls[0];
    expect(urlChamada).not.toContain('matriculaId');
  });

  it('returns the Api error message when the Api rejects com 400', async () => {
    mockFetchOnce(400, { mensagem: 'Matrícula não vinculada a este Professor.' });

    const resultado = await listarProximasAulas('professor-1');

    expect(resultado).toEqual({
      sucesso: false,
      mensagem: 'Matrícula não vinculada a este Professor.',
    });
  });

  it('returns a connection error message when fetch throws', async () => {
    globalThis.fetch = jest.fn().mockRejectedValue(new Error('network error')) as jest.Mock;

    const resultado = await listarProximasAulas('professor-1');

    expect(resultado.sucesso).toBe(false);
  });
});

describe('cancelarAula', () => {
  it('returns sucesso with the created cancelamento when the Api responds with 200', async () => {
    mockFetchOnce(200, cancelamentoExistente);

    const resultado = await cancelarAula('professor-1', 'h1', '2026-08-20');

    expect(resultado).toEqual({ sucesso: true, cancelamento: cancelamentoExistente });
  });

  it('posts sem body ao horario/data-scoped cancelamentos route (matriculaId resolvida pela Api via sessão)', async () => {
    mockFetchOnce(200, cancelamentoExistente);

    await cancelarAula('professor-1', 'h1', '2026-08-20');

    expect(globalThis.fetch).toHaveBeenCalledWith(
      expect.stringContaining('/professores/professor-1/horarios/h1/aulas/2026-08-20/cancelamentos'),
      expect.objectContaining({ method: 'POST' }),
    );
  });

  it('returns the Api error message when the Api rejects with 400 (fora do prazo)', async () => {
    mockFetchOnce(400, { mensagem: 'O prazo para cancelar esta aula expirou.' });

    const resultado = await cancelarAula('professor-1', 'h1', '2026-08-20');

    expect(resultado).toEqual({
      sucesso: false,
      mensagem: 'O prazo para cancelar esta aula expirou.',
    });
  });

  it('returns a connection error message when fetch throws', async () => {
    globalThis.fetch = jest.fn().mockRejectedValue(new Error('network error')) as jest.Mock;

    const resultado = await cancelarAula('professor-1', 'h1', '2026-08-20');

    expect(resultado.sucesso).toBe(false);
  });
});
