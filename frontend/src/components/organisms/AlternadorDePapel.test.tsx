import { fireEvent, render, screen } from '@testing-library/react-native';

import { AlternadorDePapel } from './AlternadorDePapel';

describe('AlternadorDePapel', () => {
  it('renders a tab for each papel when there is more than one', async () => {
    await render(
      <AlternadorDePapel
        papeis={['Professor', 'Aluno']}
        papelAtivo="Professor"
        onSelecionarPapel={jest.fn()}
      />,
    );

    expect(screen.getByRole('button', { name: 'Professor', selected: true })).toBeTruthy();
    expect(screen.getByRole('button', { name: 'Aluno', selected: false })).toBeTruthy();
  });

  it('calls onSelecionarPapel with the pressed papel', async () => {
    const onSelecionarPapel = jest.fn();
    await render(
      <AlternadorDePapel
        papeis={['Professor', 'Aluno']}
        papelAtivo="Professor"
        onSelecionarPapel={onSelecionarPapel}
      />,
    );

    await fireEvent.press(screen.getByText('Aluno'));

    expect(onSelecionarPapel).toHaveBeenCalledWith('Aluno');
  });

  it('renders nothing with a single papel', async () => {
    const { toJSON } = await render(
      <AlternadorDePapel papeis={['Professor']} papelAtivo="Professor" onSelecionarPapel={jest.fn()} />,
    );

    expect(toJSON()).toBeNull();
  });

  it('renders nothing with an empty papeis list', async () => {
    const { toJSON } = await render(
      <AlternadorDePapel papeis={[]} papelAtivo={undefined} onSelecionarPapel={jest.fn()} />,
    );

    expect(toJSON()).toBeNull();
  });
});
