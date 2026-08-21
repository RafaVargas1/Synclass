import { fireEvent, render, screen } from '@testing-library/react-native';

import { Button } from '@/components/atoms/Button';

describe('Button', () => {
  it('renders the given label', async () => {
    await render(<Button label="Continuar" onPress={() => {}} />);

    expect(screen.getByText('Continuar')).toBeTruthy();
  });

  it('calls onPress when tapped', async () => {
    const onPress = jest.fn();
    await render(<Button label="Continuar" onPress={onPress} />);

    fireEvent.press(screen.getByText('Continuar'));

    expect(onPress).toHaveBeenCalledTimes(1);
  });

  it('renders visually disabled and blocks onPress when disabled', async () => {
    const onPress = jest.fn();
    await render(<Button label="Continuar" onPress={onPress} disabled />);

    fireEvent.press(screen.getByText('Continuar'));

    expect(onPress).not.toHaveBeenCalled();
    expect(screen.getByRole('button')).toBeDisabled();
  });

  it('defaults to the primario variant (filled)', async () => {
    await render(<Button label="Continuar" onPress={() => {}} />);

    expect(screen.getByRole('button').props.className).toContain('bg-primary');
  });

  it('renders the secundario variant without a solid fill, for a lower-hierarchy CTA', async () => {
    await render(<Button label="Cadastrar como Aluno" onPress={() => {}} variante="secundario" />);

    const botao = screen.getByRole('button');
    expect(botao.props.className).not.toContain('bg-primary');
    expect(botao.props.className).toContain('border-text');
  });
});
