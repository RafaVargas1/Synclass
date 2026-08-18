import { listarHorariosVagos, marcarHorario } from '@/lib/api/marcacoes';

function mockFetchOnce(status: number, body: unknown) {
  globalThis.fetch = jest.fn().mockResolvedValue({
    ok: status >= 200 && status < 300,
    status,
    json: () => Promise.resolve(body),
  }) as jest.Mock;
}

const alocacaoExistente = {
  id: 'a1',
  horarioId: 'h1',
  matriculaId: 'm1',
  createdAt: '2026-08-18T10:00:00Z',
};

const horarioVagoExistente = {
  id: 'h1',
  diaSemana: 2,
  horaInicio: '10:00:00',
  duracaoMinutos: 60,
  vagasRestantes: 1,
};

describe('listarHorariosVagos', () => {
  it('returns the list of horários vagos when the Api responds with 200', async () => {
    mockFetchOnce(200, [horarioVagoExistente]);

    const resultado = await listarHorariosVagos('professor-1');

    expect(resultado).toEqual({ sucesso: true, horarios: [horarioVagoExistente] });
  });

  it('queries the professor-scoped route, sem matriculaId (resolvida pela Api via sessão)', async () => {
    mockFetchOnce(200, []);

    await listarHorariosVagos('professor-1');

    expect(globalThis.fetch).toHaveBeenCalledWith(
      expect.stringContaining('/professores/professor-1/horarios/vagos'),
      expect.anything(),
    );
    const [urlChamada] = (globalThis.fetch as jest.Mock).mock.calls[0];
    expect(urlChamada).not.toContain('matriculaId');
  });

  it('returns the Api error message when the Api rejects com 400', async () => {
    mockFetchOnce(400, { mensagem: 'O modelo de agendamento atual não permite marcação livre.' });

    const resultado = await listarHorariosVagos('professor-1');

    expect(resultado).toEqual({
      sucesso: false,
      mensagem: 'O modelo de agendamento atual não permite marcação livre.',
    });
  });

  it('returns a connection error message when fetch throws', async () => {
    globalThis.fetch = jest.fn().mockRejectedValue(new Error('network error')) as jest.Mock;

    const resultado = await listarHorariosVagos('professor-1');

    expect(resultado.sucesso).toBe(false);
  });
});

describe('marcarHorario', () => {
  it('returns sucesso with the created alocacao when the Api responds with 200', async () => {
    mockFetchOnce(200, alocacaoExistente);

    const resultado = await marcarHorario('professor-1', 'h1');

    expect(resultado).toEqual({ sucesso: true, alocacao: alocacaoExistente });
  });

  it('posts sem body ao horario-scoped marcacoes route (matriculaId resolvida pela Api via sessão)', async () => {
    mockFetchOnce(200, alocacaoExistente);

    await marcarHorario('professor-1', 'h1');

    expect(globalThis.fetch).toHaveBeenCalledWith(
      expect.stringContaining('/professores/professor-1/horarios/h1/marcacoes'),
      expect.objectContaining({ method: 'POST' }),
    );
  });

  it('returns the Api error message when the Api rejects with 400 (horário lotado)', async () => {
    mockFetchOnce(400, { mensagem: 'O horário já atingiu o limite de Alunos alocados.' });

    const resultado = await marcarHorario('professor-1', 'h1');

    expect(resultado).toEqual({
      sucesso: false,
      mensagem: 'O horário já atingiu o limite de Alunos alocados.',
    });
  });

  it('returns a connection error message when fetch throws', async () => {
    globalThis.fetch = jest.fn().mockRejectedValue(new Error('network error')) as jest.Mock;

    const resultado = await marcarHorario('professor-1', 'h1');

    expect(resultado.sucesso).toBe(false);
  });
});
