import { render, screen } from '@testing-library/react-native';

import { ResumoProximoHorario } from './ResumoProximoHorario';

describe('ResumoProximoHorario', () => {
  it('shows the Professor, data and hora quando proximoHorario está preenchido', async () => {
    await render(
      <ResumoProximoHorario
        proximoHorario={{ professorNome: 'Professor A', data: '2026-08-20', horaInicio: '18:00:00' }}
      />,
    );

    expect(screen.getByText('Professor A — 20/08/2026 às 18:00:00')).toBeTruthy();
  });

  it('shows o estado vazio quando proximoHorario é null', async () => {
    await render(<ResumoProximoHorario proximoHorario={null} />);

    expect(screen.getByText('Nenhum horário marcado no momento.')).toBeTruthy();
  });
});
