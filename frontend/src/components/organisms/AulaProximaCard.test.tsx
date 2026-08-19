import { fireEvent, render, screen } from '@testing-library/react-native';

import { AulaProximaCard } from './AulaProximaCard';

const aulaProximaCancelavel = {
  horarioId: 'h1',
  data: '2026-08-20',
  diaSemana: 4,
  horaInicio: '18:00:00',
  duracaoMinutos: 60,
  podeCancelar: true,
  cancelavelAte: '2026-08-19T18:00:00Z',
  prazoCancelamentoMinutos: 1440,
};

const aulaProximaForaDoPrazo = { ...aulaProximaCancelavel, podeCancelar: false };

describe('AulaProximaCard', () => {
  it('shows the day, start time and data', async () => {
    await render(
      <AulaProximaCard
        aulaProxima={aulaProximaCancelavel}
        confirmado={false}
        onCancelar={jest.fn()}
        onConfirmar={jest.fn()}
      />,
    );

    expect(screen.getByText(/Quinta/)).toBeTruthy();
    expect(screen.getByText(/18:00/)).toBeTruthy();
    expect(screen.getByText('2026-08-20')).toBeTruthy();
  });

  it('calls onCancelar with the horarioId and data when Cancelar is pressed', async () => {
    const onCancelar = jest.fn();
    await render(
      <AulaProximaCard
        aulaProxima={aulaProximaCancelavel}
        confirmado={false}
        onCancelar={onCancelar}
        onConfirmar={jest.fn()}
      />,
    );

    await fireEvent.press(screen.getByText('Cancelar'));

    expect(onCancelar).toHaveBeenCalledWith('h1', '2026-08-20');
  });

  it('disables o botão Cancelar e mostra o motivo quando podeCancelar é false', async () => {
    const onCancelar = jest.fn();
    await render(
      <AulaProximaCard
        aulaProxima={aulaProximaForaDoPrazo}
        confirmado={false}
        onCancelar={onCancelar}
        onConfirmar={jest.fn()}
      />,
    );

    expect(screen.getByText(/Prazo para cancelar esta aula já passou/)).toBeTruthy();
    await fireEvent.press(screen.getByText('Cancelar'));
    expect(onCancelar).not.toHaveBeenCalled();
  });

  it('calls onConfirmar with the horarioId and data when Confirmar presença is pressed', async () => {
    const onConfirmar = jest.fn();
    await render(
      <AulaProximaCard
        aulaProxima={aulaProximaCancelavel}
        confirmado={false}
        onCancelar={jest.fn()}
        onConfirmar={onConfirmar}
      />,
    );

    await fireEvent.press(screen.getByText('Confirmar presença'));

    expect(onConfirmar).toHaveBeenCalledWith('h1', '2026-08-20');
  });

  it('shows a confirmado indicator and hides the botão Confirmar presença when confirmado is true', async () => {
    await render(
      <AulaProximaCard
        aulaProxima={aulaProximaCancelavel}
        confirmado
        onCancelar={jest.fn()}
        onConfirmar={jest.fn()}
      />,
    );

    expect(screen.getByText(/Presença confirmada/)).toBeTruthy();
    expect(screen.queryByText('Confirmar presença')).toBeNull();
  });
});
