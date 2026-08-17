import { criarHorario, listarHorarios, removerHorario } from '@/lib/api/horarios';

function mockFetchOnce(status: number, body: unknown) {
  globalThis.fetch = jest.fn().mockResolvedValue({
    ok: status >= 200 && status < 300,
    status,
    json: () => Promise.resolve(body),
  }) as jest.Mock;
}

describe('criarHorario', () => {
  it('returns sucesso with the created horario when the Api responds with 200', async () => {
    mockFetchOnce(200, { id: 'h1', diaSemana: 2, horaInicio: '10:00:00', duracaoMinutos: 60 });

    const resultado = await criarHorario('professor-1', {
      diaSemana: 2,
      horaInicio: '10:00:00',
      duracaoMinutos: 60,
      limiteAlunos: 1,
    });

    expect(resultado).toEqual({
      sucesso: true,
      horario: { id: 'h1', diaSemana: 2, horaInicio: '10:00:00', duracaoMinutos: 60 },
    });
  });

  it('returns the Api error message when the Api rejects with 400 (conflict)', async () => {
    mockFetchOnce(400, { mensagem: 'Horário conflita com um já cadastrado: Terca 10:00–11:00.' });

    const resultado = await criarHorario('professor-1', {
      diaSemana: 2,
      horaInicio: '10:30:00',
      duracaoMinutos: 60,
      limiteAlunos: 1,
    });

    expect(resultado).toEqual({
      sucesso: false,
      mensagem: 'Horário conflita com um já cadastrado: Terca 10:00–11:00.',
    });
  });

  it('returns a connection error message when fetch throws', async () => {
    globalThis.fetch = jest.fn().mockRejectedValue(new Error('network error')) as jest.Mock;

    const resultado = await criarHorario('professor-1', {
      diaSemana: 2,
      horaInicio: '10:00:00',
      duracaoMinutos: 60,
      limiteAlunos: 1,
    });

    expect(resultado.sucesso).toBe(false);
  });
});

describe('listarHorarios', () => {
  it('returns the list of horarios when the Api responds with 200', async () => {
    mockFetchOnce(200, [{ id: 'h1', diaSemana: 2, horaInicio: '10:00:00', duracaoMinutos: 60 }]);

    const resultado = await listarHorarios('professor-1');

    expect(resultado).toEqual({
      sucesso: true,
      horarios: [{ id: 'h1', diaSemana: 2, horaInicio: '10:00:00', duracaoMinutos: 60 }],
    });
  });

  it('returns sucesso false when fetch throws', async () => {
    globalThis.fetch = jest.fn().mockRejectedValue(new Error('network error')) as jest.Mock;

    const resultado = await listarHorarios('professor-1');

    expect(resultado.sucesso).toBe(false);
  });
});

describe('removerHorario', () => {
  it('returns sucesso true when the Api responds with 204', async () => {
    mockFetchOnce(204, null);

    const resultado = await removerHorario('professor-1', 'h1');

    expect(resultado).toEqual({ sucesso: true });
  });

  it('returns the Api error message when the Api rejects with 409 (alunos alocados)', async () => {
    mockFetchOnce(409, { mensagem: 'Não é possível remover o horário: existem Alunos alocados.' });

    const resultado = await removerHorario('professor-1', 'h1');

    expect(resultado).toEqual({
      sucesso: false,
      mensagem: 'Não é possível remover o horário: existem Alunos alocados.',
    });
  });

  it('returns sucesso false when the Api responds with 404', async () => {
    mockFetchOnce(404, null);

    const resultado = await removerHorario('professor-1', 'h1');

    expect(resultado.sucesso).toBe(false);
  });
});
