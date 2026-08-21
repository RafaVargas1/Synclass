import { render, screen } from '@testing-library/react-native';

import { SeletorDePeriodoDuplo } from './SeletorDePeriodoDuplo';

describe('SeletorDePeriodoDuplo', () => {
  it('renders both Início and Fim selectors and forwards selection', async () => {
    const onSelecionarInicio = jest.fn();
    const onSelecionarFim = jest.fn();

    await render(
      <SeletorDePeriodoDuplo
        inicio={undefined}
        fim={undefined}
        onSelecionarInicio={onSelecionarInicio}
        onSelecionarFim={onSelecionarFim}
      />,
    );

    expect(screen.getByText('Início')).toBeTruthy();
    expect(screen.getByText('Fim')).toBeTruthy();
  });

  it('shows the selected dates when given', async () => {
    await render(
      <SeletorDePeriodoDuplo
        inicio="2026-08-01"
        fim="2026-08-31"
        onSelecionarInicio={jest.fn()}
        onSelecionarFim={jest.fn()}
      />,
    );

    expect(screen.queryByText('Selecionar data')).toBeNull();
  });
});
