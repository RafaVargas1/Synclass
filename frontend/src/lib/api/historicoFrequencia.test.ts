import { listarHistoricoFrequenciaDoAluno } from '@/lib/api/historicoFrequencia';

function mockFetchOnce(status: number, body: unknown) {
  globalThis.fetch = jest.fn().mockResolvedValue({
    ok: status >= 200 && status < 300,
    status,
    json: () => Promise.resolve(body),
  }) as jest.Mock;
}

const aulaNaoRegistrada = {
  horarioId: 'h1',
  data: '2026-08-04',
  diaSemana: 2,
  horaInicio: '10:00:00',
  status: 'NaoRegistrada',
};

describe('listarHistoricoFrequenciaDoAluno', () => {
  it('returns the historico grouped by Professor when the Api responds with 200', async () => {
    mockFetchOnce(200, [{ professorId: 'p1', nomeProfessor: 'Professor A', aulas: [aulaNaoRegistrada] }]);

    const resultado = await listarHistoricoFrequenciaDoAluno();

    expect(resultado).toEqual({
      sucesso: true,
      historico: [{ professorId: 'p1', nomeProfessor: 'Professor A', aulas: [aulaNaoRegistrada] }],
    });
  });

  it('gets the /alunos/historico-frequencia route without an id parameter', async () => {
    mockFetchOnce(200, []);

    await listarHistoricoFrequenciaDoAluno();

    expect(globalThis.fetch).toHaveBeenCalledWith(
      expect.stringContaining('/alunos/historico-frequencia'),
      expect.anything(),
    );
    expect(globalThis.fetch).not.toHaveBeenCalledWith(expect.stringContaining('?'), expect.anything());
  });

  it('appends inicio/fim to the query string when periodo is informed', async () => {
    mockFetchOnce(200, []);

    await listarHistoricoFrequenciaDoAluno({ inicio: '2026-08-01', fim: '2026-09-01' });

    expect(globalThis.fetch).toHaveBeenCalledWith(
      expect.stringContaining('/alunos/historico-frequencia?inicio=2026-08-01&fim=2026-09-01'),
      expect.anything(),
    );
  });

  it('returns sucesso false when fetch throws', async () => {
    globalThis.fetch = jest.fn().mockRejectedValue(new Error('network error')) as jest.Mock;

    const resultado = await listarHistoricoFrequenciaDoAluno();

    expect(resultado.sucesso).toBe(false);
  });

  it('returns sucesso false when the Api responds with an error status', async () => {
    mockFetchOnce(500, null);

    const resultado = await listarHistoricoFrequenciaDoAluno();

    expect(resultado.sucesso).toBe(false);
  });
});
