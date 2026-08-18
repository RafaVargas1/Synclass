import { fireEvent, render, screen } from '@testing-library/react-native';

import { HorarioVagoCard } from './HorarioVagoCard';

const horarioVago = {
  id: 'h1',
  diaSemana: 2,
  horaInicio: '10:00:00',
  duracaoMinutos: 60,
  vagasRestantes: 2,
};

describe('HorarioVagoCard', () => {
  it('shows the day, start time and vagas restantes', async () => {
    await render(<HorarioVagoCard horarioVago={horarioVago} onMarcar={jest.fn()} />);

    expect(screen.getByText(/Terça/)).toBeTruthy();
    expect(screen.getByText(/10:00/)).toBeTruthy();
    expect(screen.getByText(/2 vaga/)).toBeTruthy();
  });

  it('calls onMarcar with the horario id when Marcar is pressed', async () => {
    const onMarcar = jest.fn();
    await render(<HorarioVagoCard horarioVago={horarioVago} onMarcar={onMarcar} />);

    await fireEvent.press(screen.getByText('Marcar'));

    expect(onMarcar).toHaveBeenCalledWith('h1');
  });
});
