import { fireEvent, render, screen } from '@testing-library/react-native';

import { ChipSelector, type ChipSelectorOption } from './ChipSelector';

const opcoes: readonly ChipSelectorOption<number>[] = [
  { valor: 1, rotulo: 'Um' },
  { valor: 2, rotulo: 'Dois' },
  { valor: 3, rotulo: 'Três' },
];

describe('ChipSelector', () => {
  it('renders every option with the label and the selected one marked accessible-selected', async () => {
    await render(<ChipSelector label="Escolha" opcoes={opcoes} valor={2} onChange={jest.fn()} />);

    expect(screen.getByText('Escolha')).toBeTruthy();
    expect(screen.getByRole('button', { name: 'Dois', selected: true })).toBeTruthy();
    expect(screen.getByRole('button', { name: 'Um', selected: false })).toBeTruthy();
    expect(screen.getByRole('button', { name: 'Três', selected: false })).toBeTruthy();
  });

  it('calls onChange with the value of the pressed option', async () => {
    const onChange = jest.fn();
    await render(<ChipSelector label="Escolha" opcoes={opcoes} valor={1} onChange={onChange} />);

    await fireEvent.press(screen.getByText('Três'));

    expect(onChange).toHaveBeenCalledWith(3);
  });

  it('gives every chip a touch target of at least 44x44 (Fitts/HIG), not just the small visual chip', async () => {
    await render(<ChipSelector label="Escolha" opcoes={opcoes} valor={2} onChange={jest.fn()} />);

    for (const opcao of opcoes) {
      expect(screen.getByRole('button', { name: opcao.rotulo })).toHaveStyle({
        minWidth: 44,
        minHeight: 44,
      });
    }
  });
});
