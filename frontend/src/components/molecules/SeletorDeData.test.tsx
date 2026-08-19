import { fireEvent, render, screen } from '@testing-library/react-native';

import { SeletorDeData } from './SeletorDeData';

describe('SeletorDeData', () => {
  it('shows the placeholder when there is no value selected', async () => {
    await render(<SeletorDeData label="Início" valor={undefined} onSelecionar={() => {}} />);

    expect(screen.getByText('Início')).toBeTruthy();
    expect(screen.getByText('Selecionar data')).toBeTruthy();
  });

  it('shows the selected date formatted as dd/mm/aaaa', async () => {
    await render(<SeletorDeData label="Início" valor="2026-08-20" onSelecionar={() => {}} />);

    expect(screen.getByText('20/08/2026')).toBeTruthy();
  });

  it('opens the calendar when pressed, and closes it after picking a day', async () => {
    const onSelecionar = jest.fn();
    await render(<SeletorDeData label="Início" valor={undefined} onSelecionar={onSelecionar} />);

    await fireEvent.press(screen.getByText('Selecionar data'));
    expect(screen.getByLabelText('Próximo mês')).toBeTruthy();

    await fireEvent.press(screen.getByText('1'));

    expect(onSelecionar).toHaveBeenCalledWith(expect.stringMatching(/^\d{4}-\d{2}-01$/));
    expect(screen.queryByLabelText('Próximo mês')).toBeNull();
  });
});
