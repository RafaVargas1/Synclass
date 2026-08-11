import { horariosSeSobrepoe } from '@/lib/horarioConflito';

describe('horariosSeSobrepoe', () => {
  it('returns true when same day and intervals cross', () => {
    const existente = { diaSemana: 2, horaInicio: '10:00:00', duracaoMinutos: 60 };
    const novo = { diaSemana: 2, horaInicio: '10:30:00', duracaoMinutos: 60 };

    expect(horariosSeSobrepoe(novo, existente)).toBe(true);
  });

  it('returns false when edges only touch', () => {
    const existente = { diaSemana: 2, horaInicio: '10:00:00', duracaoMinutos: 60 };
    const novo = { diaSemana: 2, horaInicio: '11:00:00', duracaoMinutos: 30 };

    expect(horariosSeSobrepoe(novo, existente)).toBe(false);
  });

  it('returns false when days differ', () => {
    const existente = { diaSemana: 2, horaInicio: '10:00:00', duracaoMinutos: 60 };
    const novo = { diaSemana: 3, horaInicio: '10:00:00', duracaoMinutos: 60 };

    expect(horariosSeSobrepoe(novo, existente)).toBe(false);
  });
});
