import { render, screen } from '@testing-library/react-native';

import { HistoricoFrequenciaCard } from './HistoricoFrequenciaCard';

function criarAula(status: 'NaoRegistrada' | 'Presente' | 'Ausente' | 'Cancelada') {
  return {
    horarioId: 'h1',
    data: '2026-08-04',
    diaSemana: 2,
    horaInicio: '10:00:00',
    status,
  };
}

describe('HistoricoFrequenciaCard', () => {
  it('shows the day, date and time formatted', async () => {
    await render(<HistoricoFrequenciaCard aula={criarAula('Presente')} />);

    expect(screen.getByText(/Terça/)).toBeTruthy();
    expect(screen.getByText(/04\/08\/2026/)).toBeTruthy();
    expect(screen.getByText(/10:00/)).toBeTruthy();
  });

  it('shows the "Presente" label for a StatusHistoricoFrequencia Presente', async () => {
    await render(<HistoricoFrequenciaCard aula={criarAula('Presente')} />);

    expect(screen.getByText('Presente')).toBeTruthy();
  });

  it('shows the "Ausente" label for a StatusHistoricoFrequencia Ausente', async () => {
    await render(<HistoricoFrequenciaCard aula={criarAula('Ausente')} />);

    expect(screen.getByText('Ausente')).toBeTruthy();
  });

  it('shows the "Não registrada" label for a StatusHistoricoFrequencia NaoRegistrada', async () => {
    await render(<HistoricoFrequenciaCard aula={criarAula('NaoRegistrada')} />);

    expect(screen.getByText('Não registrada')).toBeTruthy();
  });

  it('shows the "Cancelada" label for a StatusHistoricoFrequencia Cancelada', async () => {
    await render(<HistoricoFrequenciaCard aula={criarAula('Cancelada')} />);

    expect(screen.getByText('Cancelada')).toBeTruthy();
  });
});
