import { fireEvent, render, screen } from '@testing-library/react-native';

import { HorarioAlocacaoCard } from './HorarioAlocacaoCard';

const horario = { id: 'h1', diaSemana: 2, horaInicio: '10:00:00', duracaoMinutos: 60, limiteAlunos: 2 };

const alunos = [
  { matriculaId: 'a1', nome: 'Ana', identificador: 'ana@x.com' },
  { matriculaId: 'a2', nome: 'Bruno', identificador: 'bruno@x.com' },
];

const alocacoes = [{ id: 'al1', horarioId: 'h1', matriculaId: 'a1', createdAt: '2026-01-01T00:00:00Z' }];

describe('HorarioAlocacaoCard', () => {
  it('shows the day, start time and occupied/total vagas', async () => {
    await render(
      <HorarioAlocacaoCard
        horario={horario}
        alunos={alunos}
        alocacoes={alocacoes}
        onAlocar={jest.fn()}
        onDesalocar={jest.fn()}
      />,
    );

    expect(screen.getByText(/Terça/)).toBeTruthy();
    expect(screen.getByText(/10:00/)).toBeTruthy();
    expect(screen.getByText('1/2')).toBeTruthy();
  });

  it('lists the allocated alunos with a remove button', async () => {
    await render(
      <HorarioAlocacaoCard
        horario={{ ...horario, limiteAlunos: 1 }}
        alunos={[alunos[0]]}
        alocacoes={alocacoes}
        onAlocar={jest.fn()}
        onDesalocar={jest.fn()}
      />,
    );

    expect(screen.getByText('Ana')).toBeTruthy();
    expect(screen.getByText('Remover')).toBeTruthy();
  });

  it('calls onDesalocar with the horario and matricula id when remove is pressed', async () => {
    const onDesalocar = jest.fn();
    await render(
      <HorarioAlocacaoCard
        horario={horario}
        alunos={alunos}
        alocacoes={alocacoes}
        onAlocar={jest.fn()}
        onDesalocar={onDesalocar}
      />,
    );

    await fireEvent.press(screen.getByText('Remover'));

    expect(onDesalocar).toHaveBeenCalledWith('h1', 'a1');
  });

  it('offers only alunos not yet allocated in the selector', async () => {
    await render(
      <HorarioAlocacaoCard
        horario={horario}
        alunos={alunos}
        alocacoes={alocacoes}
        onAlocar={jest.fn()}
        onDesalocar={jest.fn()}
      />,
    );

    expect(screen.getByText('Bruno')).toBeTruthy();
  });

  it('calls onAlocar with the horario and selected matricula id when Alocar is pressed', async () => {
    const onAlocar = jest.fn();
    await render(
      <HorarioAlocacaoCard
        horario={horario}
        alunos={alunos}
        alocacoes={alocacoes}
        onAlocar={onAlocar}
        onDesalocar={jest.fn()}
      />,
    );

    await fireEvent.press(screen.getByText('Bruno'));
    await fireEvent.press(screen.getByText('Alocar'));

    expect(onAlocar).toHaveBeenCalledWith('h1', 'a2');
  });

  it('does not show the selector when every aluno is already allocated or there are none', async () => {
    await render(
      <HorarioAlocacaoCard
        horario={{ ...horario, limiteAlunos: 1 }}
        alunos={[alunos[0]]}
        alocacoes={alocacoes}
        onAlocar={jest.fn()}
        onDesalocar={jest.fn()}
      />,
    );

    expect(screen.queryByText('Alocar')).toBeNull();
  });
});
