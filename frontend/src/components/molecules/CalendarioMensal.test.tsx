import { fireEvent, render, screen } from '@testing-library/react-native';

import { CalendarioMensal } from './CalendarioMensal';

describe('CalendarioMensal', () => {
  it('shows the reference month and year', async () => {
    await render(
      <CalendarioMensal mesReferencia={new Date(2026, 7, 1)} onSelecionarDia={() => {}} onMudarMes={() => {}} />,
    );

    expect(screen.getByText('Agosto de 2026')).toBeTruthy();
  });

  it('renders every day of the reference month as a pressable', async () => {
    await render(
      <CalendarioMensal mesReferencia={new Date(2026, 1, 1)} onSelecionarDia={() => {}} onMudarMes={() => {}} />,
    );

    for (let dia = 1; dia <= 28; dia += 1) {
      expect(screen.getByText(String(dia))).toBeTruthy();
    }
  });

  it('calls onSelecionarDia with the yyyy-MM-dd of the pressed day', async () => {
    const onSelecionarDia = jest.fn();
    await render(
      <CalendarioMensal
        mesReferencia={new Date(2026, 7, 1)}
        onSelecionarDia={onSelecionarDia}
        onMudarMes={() => {}}
      />,
    );

    await fireEvent.press(screen.getByText('20'));

    expect(onSelecionarDia).toHaveBeenCalledWith('2026-08-20');
  });

  it('calls onMudarMes with the previous/next month when navigating', async () => {
    const onMudarMes = jest.fn();
    await render(
      <CalendarioMensal mesReferencia={new Date(2026, 7, 1)} onSelecionarDia={() => {}} onMudarMes={onMudarMes} />,
    );

    await fireEvent.press(screen.getByLabelText('Mês anterior'));
    await fireEvent.press(screen.getByLabelText('Próximo mês'));

    expect(onMudarMes).toHaveBeenNthCalledWith(1, new Date(2026, 6, 1));
    expect(onMudarMes).toHaveBeenNthCalledWith(2, new Date(2026, 8, 1));
  });

  it('marks the selected day', async () => {
    await render(
      <CalendarioMensal
        mesReferencia={new Date(2026, 7, 1)}
        dataSelecionada="2026-08-20"
        onSelecionarDia={() => {}}
        onMudarMes={() => {}}
      />,
    );

    expect(screen.getByRole('button', { name: '20', selected: true })).toBeTruthy();
  });
});
