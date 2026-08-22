import { render, screen } from '@testing-library/react-native';

import { ResumoFrequenciaCard } from './ResumoFrequenciaCard';

describe('ResumoFrequenciaCard', () => {
  it('shows "3 presenças · 1 falta" for 3 presentes and 1 ausente', async () => {
    await render(<ResumoFrequenciaCard presentes={3} ausentes={1} />);

    expect(screen.getByText(/3 presenças · 1 falta/)).toBeTruthy();
  });

  it('shows the empty message when both counts are zero', async () => {
    await render(<ResumoFrequenciaCard presentes={0} ausentes={0} />);

    expect(
      screen.getByText(
        'Você ainda não tem frequência registrada nos últimos 30 dias. Suas aulas aparecem aqui assim que alguma presença ou falta for marcada.',
      ),
    ).toBeTruthy();
    expect(screen.queryByText(/presenças/)).toBeNull();
  });

  it('leaves the empty state and shows "0 presenças · 2 faltas" when there are only faltas', async () => {
    await render(<ResumoFrequenciaCard presentes={0} ausentes={2} />);

    expect(screen.getByText(/0 presenças · 2 faltas/)).toBeTruthy();
  });

  it('shows "1 presença · 0 faltas" with singular de "presença"', async () => {
    await render(<ResumoFrequenciaCard presentes={1} ausentes={0} />);

    expect(screen.getByText(/1 presença · 0 faltas/)).toBeTruthy();
  });
});
