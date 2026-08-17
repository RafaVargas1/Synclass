import { fireEvent, render, screen } from '@testing-library/react-native';

import { HorarioCard } from './HorarioCard';

const horario = { id: 'h1', diaSemana: 2, horaInicio: '10:00:00', duracaoMinutos: 60, limiteAlunos: 1 };

describe('HorarioCard', () => {
  it('shows the day, start time and duration', async () => {
    await render(<HorarioCard horario={horario} onRemover={jest.fn()} />);

    expect(screen.getByText(/Terça/)).toBeTruthy();
    expect(screen.getByText(/10:00/)).toBeTruthy();
    expect(screen.getByText(/60 min/)).toBeTruthy();
  });

  it('shows "Individual" when limiteAlunos is 1', async () => {
    await render(<HorarioCard horario={horario} onRemover={jest.fn()} />);

    expect(screen.getByText(/Individual/)).toBeTruthy();
  });

  it('shows "Grupo até N" when limiteAlunos is greater than 1', async () => {
    const horarioEmGrupo = { ...horario, limiteAlunos: 4 };
    await render(<HorarioCard horario={horarioEmGrupo} onRemover={jest.fn()} />);

    expect(screen.getByText(/Grupo até 4/)).toBeTruthy();
  });

  it('calls onRemover with the horario id when the remove button is pressed', async () => {
    const onRemover = jest.fn();
    await render(<HorarioCard horario={horario} onRemover={onRemover} />);

    await fireEvent.press(screen.getByText('Remover'));

    expect(onRemover).toHaveBeenCalledWith('h1');
  });
});
